using System.ComponentModel;

using FluentAssertions;

using UnitTests.Fakes;

using WindowSill.FileHelper.Core;
using WindowSill.FileHelper.Services;

namespace UnitTests.FileHelper.Services;

/// <summary>
/// Tests for <see cref="FileOperationQueue"/>'s progress aggregation and state/count bookkeeping.
/// </summary>
/// <remarks>
/// <see cref="FileOperationQueue.RunAsync"/> and <see cref="FileOperationTaskItem.RunAsync"/> marshal
/// state changes through <c>WindowSill.API.ThreadHelper.RunOnUIThreadAsync</c>, which requires a live WinUI
/// <c>DispatcherQueue</c> that isn't available in this unit test host (a pre-existing testability constraint of the
/// <c>WindowSill.API</c> package, not something introduced here). These tests therefore exercise the queue's
/// progress-aggregation and count/state bookkeeping directly — by manipulating <see cref="FileOperationTaskItem"/>
/// properties and calling <see cref="FileOperationTaskItem.MarkCanceled"/> — instead of driving them via
/// <c>RunAsync</c>.
/// </remarks>
public class FileOperationQueueTests
{
    public FileOperationQueueTests()
    {
        // FileOperationQueue.FinalizeState resolves its terminal ProgressText via GetLocalizedString(),
        // which throws if the Localizer singleton was never initialized (normally done by the WinUI host app).
        LocalizerSetup.EnsureInitialized();
    }

    [Fact]
    internal void Progress_ReachesOneHundred_WhenEveryTaskReachesFullProgress_IncludingFailedAndCanceledOnes()
    {
        using var queue = new FileOperationQueue(Operations("a.docx", "b.docx", "c.docx"), "Working");

        // Simulate one task succeeding, one failing, and one being canceled — all of which force their own
        // Progress to 1.0 upon reaching a terminal state (see FileOperationTaskItem.RunAsync).
        SetTerminalState(queue.Tasks[0], succeeded: true);
        SetTerminalState(queue.Tasks[1], succeeded: false);
        queue.Tasks[2].MarkCanceled();

        queue.Progress.Should().Be(100.0);
    }

    [Fact]
    internal void Progress_ReflectsPartialCompletion_WhenSomeTasksHaveNotFinished()
    {
        using var queue = new FileOperationQueue(Operations("a.docx", "b.docx"), "Working");

        SetTerminalState(queue.Tasks[0], succeeded: true);
        // Tasks[1] left at its initial Progress (0.0) — not yet finished.

        queue.Progress.Should().Be(50.0);
    }

    [Fact]
    internal void Progress_IsZero_ForANewlyCreatedQueue()
    {
        using var queue = new FileOperationQueue(Operations("a.docx"), "Working");

        queue.Progress.Should().Be(0.0);
        queue.State.Should().Be(FileOperationQueueState.Pending);
    }

    [Fact]
    internal void MarkCanceled_SetsIsCanceledAndFullProgress_WithoutMarkingSucceededOrFailed()
    {
        var task = new FileOperationTaskItem(new NeverRunOperation("source.docx"));

        task.MarkCanceled();

        task.IsCanceled.Should().BeTrue();
        task.IsSucceeded.Should().BeFalse();
        task.IsFailed.Should().BeFalse();
        task.IsRunning.Should().BeFalse();
        task.Progress.Should().Be(1.0);
    }

    [Fact]
    internal void OutputPaths_OnlyIncludesSucceededTasksWithANonNullOutputPath()
    {
        using var queue = new FileOperationQueue(Operations("a.docx", "b.docx"), "Working");

        SetTerminalState(queue.Tasks[0], succeeded: true);
        queue.Tasks[1].MarkCanceled();

        queue.OutputPaths.Should().BeEmpty(); // SetTerminalState doesn't set OutputPath (private setter, only
                                               // reachable via ConvertAsync); this test documents/guards the
                                               // filter logic (IsSucceeded && OutputPath is not null) itself.
    }

    [Fact]
    internal void FinalizeState_ResultsInCompleted_WhenCancellationWasRequestedLate_AfterEveryTaskAlreadySucceeded()
    {
        // Simulates a "late" cancellation: the cancellation token is signaled only after every task has already
        // been processed (nextTaskIndex has reached Tasks.Count), so there is nothing left to cancel.
        using var queue = new FileOperationQueue(Operations("a.docx", "b.docx"), "Working");
        SetTerminalState(queue.Tasks[0], succeeded: true);
        SetTerminalState(queue.Tasks[1], succeeded: true);
        queue.SucceededCount = 2;

        queue.FinalizeState(nextTaskIndex: queue.Tasks.Count);

        queue.CanceledCount.Should().Be(0);
        queue.State.Should().Be(FileOperationQueueState.Completed);
    }

    [Fact]
    internal void FinalizeState_ResultsInCanceled_WhenSomeTasksNeverGotAChanceToRun()
    {
        using var queue = new FileOperationQueue(Operations("a.docx", "b.docx", "c.docx"), "Working");
        SetTerminalState(queue.Tasks[0], succeeded: true);
        queue.SucceededCount = 1;
        // Tasks[1] and Tasks[2] never ran — RunAsync stopped early due to a cancellation request.

        queue.FinalizeState(nextTaskIndex: 1);

        queue.Tasks[1].IsCanceled.Should().BeTrue();
        queue.Tasks[2].IsCanceled.Should().BeTrue();
        queue.CanceledCount.Should().Be(2);
        queue.State.Should().Be(FileOperationQueueState.Canceled);
    }

    [Fact]
    internal void FinalizeState_ResultsInCanceled_WhenATaskWasCanceledMidConversion_EvenIfItWasTheLastTask()
    {
        using var queue = new FileOperationQueue(Operations("a.docx", "b.docx"), "Working");
        SetTerminalState(queue.Tasks[0], succeeded: true);
        queue.SucceededCount = 1;
        // Tasks[1] ran and was itself canceled mid-conversion (e.g. ConvertAsync observed the cancellation
        // token), so the loop in RunAsync already incremented CanceledCount for it before reaching the end.
        queue.Tasks[1].MarkCanceled();
        queue.CanceledCount = 1;

        // nextTaskIndex == Tasks.Count here: every task was already visited by the loop, so there's nothing left
        // for FinalizeState to mark canceled itself — but the queue must still end up Canceled because
        // CanceledCount already reflects the mid-run cancellation.
        queue.FinalizeState(nextTaskIndex: queue.Tasks.Count);

        queue.CanceledCount.Should().Be(1);
        queue.State.Should().Be(FileOperationQueueState.Canceled);
    }

    [Fact]
    internal void FileOperationQueueState_HasADistinctCanceledValue_SeparateFromFailedAndCompleted()
    {
        FileOperationQueueState.Canceled.Should().NotBe(FileOperationQueueState.Failed);
        FileOperationQueueState.Canceled.Should().NotBe(FileOperationQueueState.Completed);
        FileOperationQueueState.Canceled.Should().NotBe(FileOperationQueueState.Pending);
        FileOperationQueueState.Canceled.Should().NotBe(FileOperationQueueState.InProgress);
    }

    [Fact]
    internal void ErrorMessage_DefaultsToNull_AndIsSettable()
    {
        var task = new FileOperationTaskItem(new NeverRunOperation("source.docx"));

        task.ErrorMessage.Should().BeNull();

        task.ErrorMessage = "Something went wrong.";

        task.ErrorMessage.Should().Be("Something went wrong.");
    }

    /// <summary>
    /// Forces a <see cref="FileOperationTaskItem"/> into a terminal succeeded/failed state the same way
    /// <see cref="FileOperationTaskItem.RunAsync"/> would (minus the <c>ThreadHelper</c> marshaling,
    /// which isn't available in this test host), by driving its plain <see cref="INotifyPropertyChanged"/>
    /// properties directly.
    /// </summary>
    private static void SetTerminalState(FileOperationTaskItem task, bool succeeded)
    {
        task.IsRunning = false;
        task.IsSucceeded = succeeded;
        task.IsFailed = !succeeded;
        task.ErrorMessage = succeeded ? null : "Simulated conversion failure.";
        task.Progress = 1.0;
    }

    private static IReadOnlyList<IFileOperation> Operations(params string[] displayNames)
        => [.. displayNames.Select(name => new NeverRunOperation(name))];

    private sealed class NeverRunOperation(string displayName) : IFileOperation
    {
        public string DisplayName => displayName;

        public Task<string?> ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
            => throw new InvalidOperationException("This operation stub should never be invoked by these tests, which exercise queue bookkeeping without calling RunAsync.");
    }
}
