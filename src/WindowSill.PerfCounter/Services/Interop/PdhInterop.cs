using System.Runtime.InteropServices;

namespace WindowSill.PerfCounter.Services.Interop;

/// <summary>
/// Shared PDH (Performance Data Helper) P/Invoke declarations and helper methods.
/// </summary>
internal static class PdhInterop
{
    internal const uint PDH_FMT_DOUBLE = 512U;
    internal const uint PDH_MORE_DATA = 0x800007D2;
    internal const uint PDH_CSTATUS_VALID_DATA = 0;
    internal const uint PERF_DETAIL_WIZARD = 400U;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PdhFmtCounterValue
    {
        public uint CStatus;
        public double DoubleValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PdhFmtCounterValueItemDouble
    {
        public nint SzName;
        public PdhFmtCounterValue FmtValue;
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    internal static extern uint PdhOpenQuery(nint szDataSource, nint dwUserData, out nint phQuery);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    internal static extern uint PdhAddCounter(nint hQuery, string szFullCounterPath, nint dwUserData, out nint phCounter);

    [DllImport("pdh.dll")]
    internal static extern uint PdhCollectQueryData(nint hQuery);

    [DllImport("pdh.dll")]
    internal static extern uint PdhCloseQuery(nint hQuery);

    [DllImport("pdh.dll")]
    internal static extern uint PdhGetFormattedCounterValue(nint hCounter, uint dwFormat, nint lpdwType, ref PdhFmtCounterValue pValue);

    [DllImport("pdh.dll")]
    internal static extern uint PdhGetFormattedCounterArray(nint hCounter, uint dwFormat, ref uint lpdwBufferSize, ref uint lpdwItemCount, nint itemBuffer);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    internal static extern uint PdhEnumObjectItems(
        string? szDataSource,
        string? szMachineName,
        string szObjectName,
        nint mszCounterList,
        ref uint pcchCounterListLength,
        nint mszInstanceList,
        ref uint pcchInstanceListLength,
        uint dwDetailLevel,
        uint dwFlags);

    /// <summary>
    /// Collects formatted counter array values, calling the two-pass PDH pattern.
    /// Returns the sum of all valid double values, or null if no valid data.
    /// </summary>
    internal static (double total, int validCount) CollectFormattedCounterArray(nint counter)
    {
        double total = 0.0;
        int validCount = 0;

        uint bufferSize = 0u;
        uint itemCount = 0u;

        uint status = PdhGetFormattedCounterArray(counter, PDH_FMT_DOUBLE,
            ref bufferSize, ref itemCount, nint.Zero);

        if (status == PDH_MORE_DATA && itemCount > 0)
        {
            nint buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                status = PdhGetFormattedCounterArray(counter, PDH_FMT_DOUBLE,
                    ref bufferSize, ref itemCount, buffer);

                if (status == 0)
                {
                    nint current = buffer;
                    for (int i = 0; i < itemCount; i++)
                    {
                        var item = Marshal.PtrToStructure<PdhFmtCounterValueItemDouble>(current);
                        if (item.FmtValue.CStatus == PDH_CSTATUS_VALID_DATA)
                        {
                            total += item.FmtValue.DoubleValue;
                            validCount++;
                        }

                        current = IntPtr.Add(current, Marshal.SizeOf<PdhFmtCounterValueItemDouble>());
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        else
        {
            // Try single counter value
            var counterValue = new PdhFmtCounterValue();
            uint singleStatus = PdhGetFormattedCounterValue(counter, PDH_FMT_DOUBLE, nint.Zero, ref counterValue);

            if (singleStatus == 0 && counterValue.CStatus == PDH_CSTATUS_VALID_DATA)
            {
                total += counterValue.DoubleValue;
                validCount++;
            }
        }

        return (total, validCount);
    }

    /// <summary>
    /// Enumerates PDH object instances by name, returning a list of instance names.
    /// </summary>
    internal static List<string> EnumerateObjectInstances(string objectName)
    {
        var instances = new List<string>();

        uint counterListSize = 0u;
        uint instanceListSize = 0u;

        uint status = PdhEnumObjectItems(
            null, null, objectName,
            nint.Zero, ref counterListSize,
            nint.Zero, ref instanceListSize,
            PERF_DETAIL_WIZARD, 0);

        if (status != PDH_MORE_DATA || instanceListSize == 0)
        {
            return instances;
        }

        nint buffer = Marshal.AllocHGlobal((int)instanceListSize * 2);
        try
        {
            status = PdhEnumObjectItems(
                null, null, objectName,
                nint.Zero, ref counterListSize,
                buffer, ref instanceListSize,
                PERF_DETAIL_WIZARD, 0);

            if (status == 0)
            {
                nint current = buffer;
                while (true)
                {
                    string? instanceName = Marshal.PtrToStringUni(current);
                    if (string.IsNullOrEmpty(instanceName))
                    {
                        break;
                    }

                    instances.Add(instanceName);
                    current = IntPtr.Add(current, (instanceName.Length + 1) * 2);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return instances;
    }
}
