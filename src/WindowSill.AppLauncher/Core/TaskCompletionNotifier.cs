using System.ComponentModel;
using CommunityToolkit.Diagnostics;
using WindowSill.API;

namespace WindowSill.AppLauncher.Core;

/// <summary>
/// Watches a task and raises property-changed notifications when the task completes.
/// </summary>
/// <typeparam name="TResult">The type of the result of the task.</typeparam>
public sealed class TaskCompletionNotifier<TResult> : INotifyPropertyChanged
{
    private readonly Func<Task<TResult>?>? _taskFactory;
    private readonly Func<ValueTask<TResult>?>? _valueTaskFactory;
    private readonly bool _ranTaskWhenCreated;

    /// <summary>
    /// Gets the task being watched. This property never changes and is never <c>null</c>.
    /// </summary>
    public Task<TResult>? Task { get; private set; }

    /// <summary>
    /// Gets the result of the task. Returns the default value of TResult if the task has not completed successfully.
    /// </summary>
    public TResult? Result
    {
        get
        {
            if (!_ranTaskWhenCreated && Task is null)
            {
                Reset();
            }

            return (Task != null && Task.Status == TaskStatus.RanToCompletion) ? Task.Result : default;
        }
    }

    /// <summary>
    /// Gets whether the task has completed.
    /// </summary>
    public bool IsCompleted => Task == null || Task.IsCompleted;

    /// <summary>
    /// Gets whether the task has completed successfully.
    /// </summary>
    public bool IsSuccessfullyCompleted => Task == null || Task.Status == TaskStatus.RanToCompletion;

    /// <summary>
    /// Gets whether the task has been canceled.
    /// </summary>
    public bool IsCanceled => Task != null && Task.IsCanceled;

    /// <summary>
    /// Gets whether the task has faulted.
    /// </summary>
    public bool IsFaulted => Task != null && Task.IsFaulted;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initialize a new instance of the <see cref="TaskCompletionNotifier{TResult}"/> class.
    /// </summary>
    /// <param name="valueTaskFactory">The <see cref="ValueTask"/> to run.</param>
    /// <param name="runTaskImmediately">Whether to run the task immediately upon construction.</param>
    public TaskCompletionNotifier(Func<ValueTask<TResult>?> valueTaskFactory, bool runTaskImmediately = true)
    {
        Guard.IsNotNull(valueTaskFactory);
        _valueTaskFactory = valueTaskFactory;
        _taskFactory = null;

        if (runTaskImmediately)
        {
            _ranTaskWhenCreated = true;
            RunTask();
        }
    }

    /// <summary>
    /// Initialize a new instance of the <see cref="TaskCompletionNotifier{TResult}"/> class.
    /// </summary>
    /// <param name="taskFactory">The <see cref="Task"/> to run.</param>
    /// <param name="runTaskImmediately">Whether to run the task immediately upon construction.</param>
    public TaskCompletionNotifier(Func<Task<TResult>?> taskFactory, bool runTaskImmediately = true)
    {
        Guard.IsNotNull(taskFactory);
        _valueTaskFactory = null;
        _taskFactory = taskFactory;

        if (runTaskImmediately)
        {
            _ranTaskWhenCreated = true;
            RunTask();
        }
    }

    /// <summary>
    /// Resets the object's state and notifies listeners of a property change.
    /// </summary>
    public void Reset()
    {
        RunTask();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    private void RunTask()
    {
        try
        {
            if (_valueTaskFactory is null && _taskFactory is null)
            {
                throw new InvalidOperationException("Either a task factory or a value task factory must be provided.");
            }

            Task<TResult>? task = _taskFactory is null ? _valueTaskFactory!()!.Value.AsTask() : _taskFactory();
            Task = task;
            if (task != null && !task.IsCompleted)
            {
                TaskScheduler? scheduler = (SynchronizationContext.Current == null) ? TaskScheduler.Current : TaskScheduler.FromCurrentSynchronizationContext();

                task
                    .ContinueWith(
                        async t =>
                        {
                            PropertyChangedEventHandler? propertyChanged = PropertyChanged;
                            if (propertyChanged != null)
                            {
                                await ThreadHelper.RunOnUIThreadAsync(() =>
                                {
                                    propertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
                                    if (t.IsCanceled)
                                    {
                                        propertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsCanceled)));
                                    }
                                    else if (t.IsFaulted)
                                    {
                                        propertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsFaulted)));
                                    }
                                    else
                                    {
                                        propertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuccessfullyCompleted)));
                                        propertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
                                    }
                                });
                            }
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        scheduler)
                    .Forget();
            }
        }
        catch
        {
            ThreadHelper.RunOnUIThreadAsync(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFaulted)));
            }).Forget();
        }
    }
}
