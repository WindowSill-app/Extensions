using FluentAssertions;
using WindowSill.InlineTerminal.Core.Commands;

namespace UnitTests.InlineTerminal.Core;

public class ReplaySubjectTests
{
    [Fact]
    public void OnNext_ActiveSubscriber_ReceivesValues()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();
        var observer = new TestObserver<int>();
        subject.Subscribe(observer);

        // Act
        subject.OnNext(1);
        subject.OnNext(2);

        // Assert
        observer.Values.Should().Equal(1, 2);
        observer.IsCompleted.Should().BeFalse();
        observer.Error.Should().BeNull();
    }

    [Fact]
    public void Subscribe_LateSubscriber_ReceivesReplayedAndLiveValues()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();
        subject.OnNext(1);
        subject.OnNext(2);

        var lateObserver = new TestObserver<int>();

        // Act
        subject.Subscribe(lateObserver);
        subject.OnNext(3);

        // Assert
        lateObserver.Values.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Subscribe_AfterCompleted_ReceivesReplayThenCompleted()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();
        subject.OnNext(1);
        subject.OnCompleted();

        var lateObserver = new TestObserver<int>();

        // Act
        subject.Subscribe(lateObserver);

        // Assert
        lateObserver.Values.Should().Equal(1);
        lateObserver.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Subscribe_AfterError_ReceivesReplayThenError()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();
        subject.OnNext(1);
        var exception = new InvalidOperationException("test");
        subject.OnError(exception);

        var lateObserver = new TestObserver<int>();

        // Act
        subject.Subscribe(lateObserver);

        // Assert
        lateObserver.Values.Should().Equal(1);
        lateObserver.Error.Should().BeSameAs(exception);
        lateObserver.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void OnCompleted_IsTerminal_NoFurtherValuesDelivered()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();
        var observer = new TestObserver<int>();
        subject.Subscribe(observer);

        // Act
        subject.OnNext(1);
        subject.OnCompleted();
        subject.OnNext(2); // Should be ignored

        // Assert
        observer.Values.Should().Equal(1);
        observer.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void OnError_IsTerminal_NoFurtherValuesDelivered()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();
        var observer = new TestObserver<int>();
        subject.Subscribe(observer);

        // Act
        subject.OnNext(1);
        subject.OnError(new InvalidOperationException("test"));
        subject.OnNext(2); // Should be ignored

        // Assert
        observer.Values.Should().Equal(1);
        observer.Error.Should().NotBeNull();
    }

    [Fact]
    public void OnError_NoOnCompletedAfter()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();
        var observer = new TestObserver<int>();
        subject.Subscribe(observer);

        // Act
        subject.OnError(new InvalidOperationException("test"));
        subject.OnCompleted(); // Should be ignored

        // Assert
        observer.Error.Should().NotBeNull();
        observer.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_StopsDelivery()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();
        var observer = new TestObserver<int>();
        IDisposable subscription = subject.Subscribe(observer);

        subject.OnNext(1);

        // Act
        subscription.Dispose();
        subject.OnNext(2);

        // Assert
        observer.Values.Should().Equal(1);
    }

    [Fact]
    public void Subscribe_AfterDispose_Throws()
    {
        // Arrange
        var subject = new ReplaySubject<int>();
        subject.Dispose();

        // Act
        Action act = () => subject.Subscribe(new TestObserver<int>());

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveValues()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();
        var observer1 = new TestObserver<int>();
        var observer2 = new TestObserver<int>();
        subject.Subscribe(observer1);

        subject.OnNext(1);

        subject.Subscribe(observer2); // Late subscriber
        subject.OnNext(2);

        // Assert
        observer1.Values.Should().Equal(1, 2);
        observer2.Values.Should().Equal(1, 2); // Gets replay of 1, then live 2
    }

    [Fact]
    public void Subscribe_NullObserver_Throws()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();

        // Act
        Action act = () => subject.Subscribe(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OnError_NullException_Throws()
    {
        // Arrange
        using var subject = new ReplaySubject<int>();

        // Act
        Action act = () => subject.OnError(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class TestObserver<T> : IObserver<T>
    {
        internal List<T> Values { get; } = [];

        internal bool IsCompleted { get; private set; }

        internal Exception? Error { get; private set; }

        public void OnCompleted() => IsCompleted = true;

        public void OnError(Exception error) => Error = error;

        public void OnNext(T value) => Values.Add(value);
    }
}
