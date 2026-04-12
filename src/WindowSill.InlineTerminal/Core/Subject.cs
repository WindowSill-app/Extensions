namespace WindowSill.InlineTerminal.Core;

/// <summary>
/// A thread-safe, multi-subscriber event broadcaster.
/// <para>
/// This class acts as a bridge between a producer (which pushes data in via <see cref="OnNext"/>)
/// and one or more consumers (which receive data by calling <see cref="Subscribe"/>).
/// </para>
/// <para>
/// New subscribers only receive items emitted after they subscribe.
/// </para>
/// <para>
/// <b>Terminal states:</b> Once <see cref="OnCompleted"/> or <see cref="OnError"/> is called,
/// the stream is finished — no further items will be delivered. Late subscribers receive
/// the terminal notification immediately.
/// </para>
/// </summary>
/// <typeparam name="T">The type of items being broadcast.</typeparam>
internal sealed class Subject<T> : IObservable<T>, IObserver<T>, IDisposable
{
    private readonly Lock _lock = new();
    private readonly HashSet<Subscription> _subscriptions = [];

    private bool _disposed;

    /// <summary>
    /// Registers a new subscriber. The subscriber receives only items emitted after this call.
    /// Dispose the returned <see cref="IDisposable"/> to stop receiving items.
    /// </summary>
    /// <param name="observer">The subscriber to register.</param>
    /// <returns>A handle that, when disposed, unsubscribes the observer.</returns>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

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
            if (_disposed)
            {
                return;
            }

            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Observer.OnNext(value);
            }
        }
    }

    /// <summary>
    /// Signals that the stream has finished successfully. All current subscribers are notified,
    /// and any future subscribers will receive the completion signal immediately.
    /// </summary>
    public void OnCompleted()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Observer.OnCompleted();
            }
        }
    }

    /// <summary>
    /// Signals that the stream has terminated due to an error. All current subscribers are notified,
    /// and any future subscribers will receive the error immediately.
    /// </summary>
    /// <param name="error">The exception that caused the failure.</param>
    public void OnError(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Observer.OnError(error);
            }
        }
    }

    /// <summary>
    /// Disposes the subject, clearing all subscriptions.
    /// After disposal, new subscriptions will throw <see cref="ObjectDisposedException"/>.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _subscriptions.Clear();
        }
    }

    private void Remove(Subscription subscription)
    {
        lock (_lock)
        {
            _subscriptions.Remove(subscription);
        }
    }

    private sealed class Subscription(Subject<T> subject, IObserver<T> observer) : IDisposable
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
