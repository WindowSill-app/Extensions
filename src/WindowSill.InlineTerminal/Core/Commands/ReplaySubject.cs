namespace WindowSill.InlineTerminal.Core.Commands;

/// <summary>
/// A thread-safe, multi-subscriber event broadcaster that remembers all past items.
/// <para>
/// This class acts as a bridge between a producer (which pushes data in via <see cref="OnNext"/>)
/// and one or more consumers (which receive data by calling <see cref="Subscribe"/>).
/// </para>
/// <para>
/// <b>Replay behavior:</b> When a new subscriber joins, it immediately receives all previously
/// emitted items in order, then continues receiving live items going forward. This is useful
/// when a UI component (e.g., a popup) opens after a command has already started producing output.
/// </para>
/// <para>
/// <b>Terminal states:</b> Once <see cref="OnCompleted"/> or <see cref="OnError"/> is called,
/// the stream is finished — no further items will be delivered. Late subscribers still receive
/// the full replay buffer followed by the terminal notification.
/// </para>
/// </summary>
/// <typeparam name="T">The type of items being broadcast.</typeparam>
internal sealed class ReplaySubject<T> : IObservable<T>, IObserver<T>, IDisposable
{
    private readonly Lock _lock = new();
    private readonly List<T> _buffer = [];
    private readonly HashSet<Subscription> _subscriptions = [];

    private bool _completed;
    private bool _hasError;
    private Exception? _error;
    private bool _disposed;

    /// <summary>
    /// Registers a new subscriber. The subscriber immediately receives all previously emitted items,
    /// then receives future items as they are pushed. Dispose the returned <see cref="IDisposable"/>
    /// to stop receiving items.
    /// </summary>
    /// <param name="observer">The subscriber to register.</param>
    /// <returns>A handle that, when disposed, unsubscribes the observer.</returns>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Replay buffered items.
            for (int i = 0; i < _buffer.Count; i++)
            {
                observer.OnNext(_buffer[i]);
            }

            // If already terminal, deliver terminal notification and return a no-op disposable.
            if (_hasError)
            {
                observer.OnError(_error!);
                return Disposable.Empty;
            }

            if (_completed)
            {
                observer.OnCompleted();
                return Disposable.Empty;
            }

            var subscription = new Subscription(this, observer);
            _subscriptions.Add(subscription);
            return subscription;
        }
    }

    /// <summary>
    /// Pushes a new item to all current subscribers and stores it in the replay buffer
    /// for future subscribers. Ignored if the stream has already completed or errored.
    /// </summary>
    /// <param name="value">The item to broadcast.</param>
    public void OnNext(T value)
    {
        lock (_lock)
        {
            if (_disposed || _completed || _hasError)
            {
                return;
            }

            _buffer.Add(value);

            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Observer.OnNext(value);
            }
        }
    }

    /// <summary>
    /// Signals that the stream has finished successfully. All current subscribers are notified,
    /// and any future subscribers will receive the full replay buffer followed by the completion signal.
    /// After this call, <see cref="OnNext"/> and <see cref="OnError"/> are ignored.
    /// </summary>
    public void OnCompleted()
    {
        lock (_lock)
        {
            if (_disposed || _completed || _hasError)
            {
                return;
            }

            _completed = true;

            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Observer.OnCompleted();
            }

            _subscriptions.Clear();
        }
    }

    /// <summary>
    /// Signals that the stream has terminated due to an error. All current subscribers are notified,
    /// and any future subscribers will receive the full replay buffer followed by the error.
    /// After this call, <see cref="OnNext"/> and <see cref="OnCompleted"/> are ignored.
    /// </summary>
    /// <param name="error">The exception that caused the failure.</param>
    public void OnError(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        lock (_lock)
        {
            if (_disposed || _completed || _hasError)
            {
                return;
            }

            _hasError = true;
            _error = error;

            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Observer.OnError(error);
            }

            _subscriptions.Clear();
        }
    }

    /// <summary>
    /// Disposes the subject, clearing the replay buffer and all subscriptions.
    /// After disposal, new subscriptions will throw <see cref="ObjectDisposedException"/>.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _subscriptions.Clear();
            _buffer.Clear();
        }
    }

    private void Remove(Subscription subscription)
    {
        lock (_lock)
        {
            _subscriptions.Remove(subscription);
        }
    }

    private sealed class Subscription(ReplaySubject<T> subject, IObserver<T> observer) : IDisposable
    {
        internal IObserver<T> Observer { get; } = observer;

        public void Dispose()
        {
            subject.Remove(this);
        }
    }

    private static class Disposable
    {
        internal static readonly IDisposable Empty = new EmptyDisposable();

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
