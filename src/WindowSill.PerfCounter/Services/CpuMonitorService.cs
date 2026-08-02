using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;

using WindowSill.API;
using WindowSill.PerfCounter.Services.Interop;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Samples group-aware CPU utilization through language-neutral PDH counters.
/// </summary>
[Export(typeof(ICpuMonitorService))]
internal sealed class CpuMonitorService : ICpuMonitorService
{
    private static readonly string[] CounterPaths =
    [
        @"\Processor Information(*)\% Processor Time",
        @"\Processor Information(*)\% User Time",
        @"\Processor Information(*)\% Privileged Time"
    ];

    private static readonly ILogger Logger = typeof(CpuMonitorService).Log();

    private readonly Lock _lock = new();

    private PdhCounterQuery? _query;
    private DateTimeOffset _retryAfter;
    private bool _initializationFailureLogged;
    private bool _readFailureLogged;
    private bool _isMonitoring;

    public void StartMonitoring()
    {
        lock (_lock)
        {
            _isMonitoring = true;
            _retryAfter = DateTimeOffset.MinValue;
            EnsureQuery();
        }
    }

    public void StopMonitoring()
    {
        lock (_lock)
        {
            _isMonitoring = false;
            DisposeQuery();
        }
    }

    public CpuPerformanceData? GetCpuUsage()
    {
        lock (_lock)
        {
            if (!_isMonitoring)
            {
                return null;
            }

            if (!EnsureQuery() || _query is null)
            {
                return null;
            }

            if (!_query.Collect())
            {
                HandleReadFailure("PDH failed to collect CPU counter data.");
                return null;
            }

            CpuPerformanceData? data = CpuUsageAggregator.Create(
                _query.GetFormattedCounterArray(0),
                _query.GetFormattedCounterArray(1),
                _query.GetFormattedCounterArray(2));

            if (data is null)
            {
                HandleReadFailure("PDH returned no valid CPU counter data.");
                return null;
            }

            _readFailureLogged = false;
            return data;
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }

    private bool EnsureQuery()
    {
        if (!_isMonitoring)
        {
            return false;
        }

        if (_query is not null)
        {
            return true;
        }

        if (DateTimeOffset.UtcNow < _retryAfter)
        {
            return false;
        }

        if (!PdhCounterQuery.TryCreate(CounterPaths, out PdhCounterQuery? query, out uint status) ||
            query is null)
        {
            if (!_initializationFailureLogged)
            {
                Logger.LogWarning(
                    "Unable to initialize CPU performance counters. PDH status: 0x{Status:X8}.",
                    status);
                _initializationFailureLogged = true;
            }

            _retryAfter = DateTimeOffset.UtcNow.AddSeconds(30);
            return false;
        }

        if (!query.Collect())
        {
            query.Dispose();
            HandleInitializationFailure("Unable to prime CPU performance counters.");
            return false;
        }

        _query = query;
        _initializationFailureLogged = false;
        _readFailureLogged = false;
        return true;
    }

    private void HandleInitializationFailure(string message)
    {
        if (!_initializationFailureLogged)
        {
            Logger.LogWarning("{Message}", message);
            _initializationFailureLogged = true;
        }

        _retryAfter = DateTimeOffset.UtcNow.AddSeconds(30);
    }

    private void HandleReadFailure(string message)
    {
        if (!_readFailureLogged)
        {
            Logger.LogWarning("{Message}", message);
            _readFailureLogged = true;
        }

        DisposeQuery();
        _retryAfter = DateTimeOffset.UtcNow.AddSeconds(5);
    }

    private void DisposeQuery()
    {
        _query?.Dispose();
        _query = null;
    }
}
