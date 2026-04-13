using FluentAssertions;
using WindowSill.InlineTerminal.Models;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="CommandRun"/>.
/// </summary>
public class CommandRunTests
{
    [Fact]
    internal void Constructor_InitializesCorrectly()
    {
        var run = new CommandRun();

        run.Id.Should().NotBeEmpty();
        run.State.Should().Be(CommandState.Running);
        run.Output.Should().BeEmpty();
        run.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        run.CompletedAt.Should().BeNull();
    }

    [Fact]
    internal void AppendOutput_AddsToOutput()
    {
        var run = new CommandRun();

        run.AppendOutput("line 1");
        run.AppendOutput("line 2");

        run.Output.Should().Contain("line 1");
        run.Output.Should().Contain("line 2");
    }

    [Fact]
    internal void AppendOutput_BroadcastsToSubscribers()
    {
        var run = new CommandRun();
        var received = new List<string>();
        run.OutputLines.Subscribe(new TestObserver<string>(onNext: v => received.Add(v)));

        run.AppendOutput("hello");
        run.AppendOutput("world");

        received.Should().Equal("hello", "world");
    }

    [Fact]
    internal void Complete_SetsStateAndTimestamp()
    {
        var run = new CommandRun();

        run.Complete(CommandState.Completed);

        run.State.Should().Be(CommandState.Completed);
        run.CompletedAt.Should().NotBeNull();
        run.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(CommandState.Completed)]
    [InlineData(CommandState.Cancelled)]
    internal void Complete_NonFailure_CompletesSubject(CommandState state)
    {
        var run = new CommandRun();
        bool completed = false;
        run.OutputLines.Subscribe(new TestObserver<string>(onCompleted: () => completed = true));

        run.Complete(state);

        completed.Should().BeTrue();
    }

    [Fact]
    internal void Complete_Failed_ErrorsSubject()
    {
        var run = new CommandRun();
        bool errored = false;
        run.OutputLines.Subscribe(new TestObserver<string>(onError: _ => errored = true));

        run.Complete(CommandState.Failed);

        errored.Should().BeTrue();
    }

    [Fact]
    internal void OutputTrimmed_TrimsWhitespace()
    {
        var run = new CommandRun();
        run.AppendOutput("  hello  ");

        run.OutputTrimmed.Should().StartWith("hello");
    }

    [Fact]
    internal void Dispose_StopsOutputBroadcast()
    {
        var run = new CommandRun();
        var received = new List<string>();
        run.OutputLines.Subscribe(new TestObserver<string>(onNext: v => received.Add(v)));

        run.AppendOutput("before");
        run.Dispose();
        run.AppendOutput("after");

        received.Should().Equal("before");
    }

    private sealed class TestObserver<T>(
        Action<T>? onNext = null,
        Action? onCompleted = null,
        Action<Exception>? onError = null) : IObserver<T>
    {
        public void OnNext(T value) => onNext?.Invoke(value);
        public void OnCompleted() => onCompleted?.Invoke();
        public void OnError(Exception error) => onError?.Invoke(error);
    }
}
