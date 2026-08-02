using System.Reflection;
using FluentAssertions;

using UnitTests.Fakes;

using WindowSill.PerfCounter.Services;

namespace UnitTests.PerfCounter.Services;

public class PerformanceMonitorServiceTests
{
    [Fact]
    internal void MonitoringLeases_StartAndStopSourcesExactlyOnce()
    {
        LoggingSetup.EnsureInitialized();

        var cpu = new FakeCpuMonitor();
        var gpu = new FakeGpuMonitor();
        var temperature = new FakeTemperatureMonitor();
        using var monitor = new PerformanceMonitorService(cpu, gpu, temperature);

        monitor.StartMonitoring();
        monitor.StartMonitoring();

        cpu.StartCount.Should().Be(1);
        gpu.StartCount.Should().Be(1);
        temperature.StartCount.Should().Be(1);

        monitor.StopMonitoring();

        cpu.StopCount.Should().Be(0);
        gpu.StopCount.Should().Be(0);
        temperature.StopCount.Should().Be(0);

        monitor.StopMonitoring();
        monitor.StopMonitoring();

        cpu.StopCount.Should().Be(1);
        gpu.StopCount.Should().Be(1);
        temperature.StopCount.Should().Be(1);
    }

    [Fact]
    internal void StartMonitoring_ThrowsAfterDisposal()
    {
        LoggingSetup.EnsureInitialized();

        var monitor = new PerformanceMonitorService(
            new FakeCpuMonitor(),
            new FakeGpuMonitor(),
            new FakeTemperatureMonitor());

        monitor.Dispose();

        Action action = monitor.StartMonitoring;
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    internal void GetCurrentPerformanceData_UsesTemporaryMonitoringLease()
    {
        LoggingSetup.EnsureInitialized();

        var cpu = new FakeCpuMonitor
        {
            Data = new CpuPerformanceData(42, 30, 12, [])
        };
        var gpu = new FakeGpuMonitor();
        var temperature = new FakeTemperatureMonitor();
        using var monitor = new PerformanceMonitorService(
            cpu,
            gpu,
            temperature,
            TimeSpan.FromMilliseconds(1));

        PerformanceData result = monitor.GetCurrentPerformanceData();

        result.CpuUsage.Should().Be(42);
        cpu.StartCount.Should().Be(1);
        cpu.StopCount.Should().Be(1);
        gpu.StartCount.Should().Be(1);
        gpu.StopCount.Should().Be(1);
        temperature.StartCount.Should().Be(1);
        temperature.StopCount.Should().Be(1);
    }

    [Fact]
    internal async Task GetCurrentPerformanceData_RetainsLeaseWhenExistingLeaseStops()
    {
        LoggingSetup.EnsureInitialized();

        var cpu = new FakeCpuMonitor
        {
            Data = new CpuPerformanceData(42, 30, 12, [])
        };
        var gpu = new FakeGpuMonitor();
        var temperature = new FakeTemperatureMonitor();
        var samplingDelay = new ControlledSamplingDelay();
        using var monitor = new PerformanceMonitorService(
            cpu,
            gpu,
            temperature,
            TimeSpan.FromSeconds(1),
            samplingDelay.WaitAsync);

        monitor.StartMonitoring();
        await samplingDelay.WaitStarted.WaitAsync(TimeSpan.FromSeconds(5));

        Task<PerformanceData> pendingSample = Task.Run(
            monitor.GetCurrentPerformanceData);

        SpinWait.SpinUntil(
            () => GetMonitoringLeaseCount(monitor) == 2,
            TimeSpan.FromSeconds(5)).Should().BeTrue();

        monitor.StopMonitoring();

        cpu.StopCount.Should().Be(0);
        samplingDelay.Release();

        PerformanceData result =
            await pendingSample.WaitAsync(TimeSpan.FromSeconds(5));
        result.CpuUsage.Should().Be(42);
        cpu.StopCount.Should().Be(1);
        gpu.StopCount.Should().Be(1);
        temperature.StopCount.Should().Be(1);
    }

    private static int GetMonitoringLeaseCount(
        PerformanceMonitorService monitor)
    {
        FieldInfo countField = typeof(PerformanceMonitorService).GetField(
            "_monitoringCount",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "Unable to inspect the monitoring lease count.");
        FieldInfo lockField = typeof(PerformanceMonitorService).GetField(
            "_lockObject",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "Unable to inspect the monitoring lock.");
        object lockObject = lockField.GetValue(monitor)
            ?? throw new InvalidOperationException(
                "The monitoring lock is unavailable.");

        lock (lockObject)
        {
            return (int)(countField.GetValue(monitor)
                ?? throw new InvalidOperationException(
                    "The monitoring lease count is unavailable."));
        }
    }

    private sealed class ControlledSamplingDelay
    {
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _waitStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _waitCount;

        internal Task WaitStarted => _waitStarted.Task;

        internal void Release() => _release.TrySetResult(true);

        internal async Task WaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _waitCount) == 1)
            {
                _waitStarted.TrySetResult(true);
                await _release.Task.WaitAsync(cancellationToken);
                return;
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class FakeCpuMonitor : ICpuMonitorService
    {
        internal int StartCount { get; private set; }
        internal int StopCount { get; private set; }
        internal CpuPerformanceData? Data { get; init; }

        public void StartMonitoring() => StartCount++;
        public void StopMonitoring() => StopCount++;
        public CpuPerformanceData? GetCpuUsage() => Data;
        public void Dispose() => StopMonitoring();
    }

    private sealed class FakeGpuMonitor : IGpuMonitorService
    {
        internal int StartCount { get; private set; }
        internal int StopCount { get; private set; }

        public void StartMonitoring() => StartCount++;
        public void StopMonitoring() => StopCount++;
        public GpuPerformanceData? GetGpuUsage() => null;
        public void Dispose() => StopMonitoring();
    }

    private sealed class FakeTemperatureMonitor : ITemperatureMonitorService
    {
        internal int StartCount { get; private set; }
        internal int StopCount { get; private set; }

        public void StartMonitoring() => StartCount++;
        public void StopMonitoring() => StopCount++;
        public double? GetCpuTemperature() => null;
        public double? GetGpuTemperature(GpuAdapterInfo? adapter) => null;
        public void Dispose() => StopMonitoring();
    }
}
