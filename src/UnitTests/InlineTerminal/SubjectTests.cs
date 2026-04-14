using FluentAssertions;
using WindowSill.InlineTerminal.Models;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="Subject{T}"/>.
/// </summary>
public class SubjectTests
{
    [Fact]
    internal void OnNext_WithSubscriber_DeliversValue()
    {
        var subject = new Subject<int>();
        int received = -1;
        subject.Subscribe(new TestObserver<int>(onNext: v => received = v));

        subject.OnNext(42);

        received.Should().Be(42);
    }

    [Fact]
    internal void OnNext_MultipleSubscribers_AllReceive()
    {
        var subject = new Subject<string>();
        var received1 = new List<string>();
        var received2 = new List<string>();

        subject.Subscribe(new TestObserver<string>(onNext: v => received1.Add(v)));
        subject.Subscribe(new TestObserver<string>(onNext: v => received2.Add(v)));

        subject.OnNext("hello");

        received1.Should().ContainSingle("hello");
        received2.Should().ContainSingle("hello");
    }

    [Fact]
    internal void Dispose_Subscription_StopsReceiving()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        IDisposable sub = subject.Subscribe(new TestObserver<int>(onNext: v => received.Add(v)));

        subject.OnNext(1);
        sub.Dispose();
        subject.OnNext(2);

        received.Should().Equal(1);
    }

    [Fact]
    internal void OnCompleted_NotifiesAllSubscribers()
    {
        var subject = new Subject<int>();
        bool completed = false;
        subject.Subscribe(new TestObserver<int>(onCompleted: () => completed = true));

        subject.OnCompleted();

        completed.Should().BeTrue();
    }

    [Fact]
    internal void OnError_NotifiesAllSubscribers()
    {
        var subject = new Subject<int>();
        Exception? receivedError = null;
        subject.Subscribe(new TestObserver<int>(onError: ex => receivedError = ex));

        var exception = new InvalidOperationException("test");
        subject.OnError(exception);

        receivedError.Should().BeSameAs(exception);
    }

    [Fact]
    internal void Dispose_Subject_StopsDelivery()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        subject.Subscribe(new TestObserver<int>(onNext: v => received.Add(v)));

        subject.OnNext(1);
        subject.Dispose();
        subject.OnNext(2);

        received.Should().Equal(1);
    }

    [Fact]
    internal void Subscribe_AfterDispose_Throws()
    {
        var subject = new Subject<int>();
        subject.Dispose();

        Action act = () => subject.Subscribe(new TestObserver<int>());
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    internal void LateSubscriber_DoesNotReceivePastItems()
    {
        var subject = new Subject<int>();
        subject.OnNext(1);

        var received = new List<int>();
        subject.Subscribe(new TestObserver<int>(onNext: v => received.Add(v)));

        subject.OnNext(2);

        received.Should().Equal(2);
    }

    /// <summary>
    /// Simple test observer for <see cref="Subject{T}"/> tests.
    /// </summary>
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
