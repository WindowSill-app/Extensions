using System.Runtime.InteropServices;

namespace WindowSill.PerfCounter.Services.Interop;

internal readonly record struct PdhCounterValue(string Name, double Value);

/// <summary>
/// Shared language-neutral PDH declarations and counter-path helpers.
/// </summary>
internal static class PdhInterop
{
    internal const uint ErrorSuccess = 0;
    internal const uint PdhMoreData = 0x800007D2;
    internal const uint PdhNoInstance = 0x800007D1;

    private const uint PdhFormatDouble = 0x00000200;
    private const uint PdhStatusValidData = 0;
    private const uint PdhStatusNewData = 1;
    private const uint PdhRefreshCounters = 0x00000001;
    private const int MaximumBufferAttempts = 3;

    [StructLayout(LayoutKind.Sequential)]
    private struct PdhFormattedCounterValue
    {
        public uint Status;
        public double Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PdhCounterInfo
    {
        public uint Length;
        public uint Type;
        public uint Version;
        public uint Status;
        public int Scale;
        public int DefaultScale;
        public nuint UserData;
        public nuint QueryUserData;
        public nint FullPath;
        public nint MachineName;
        public nint ObjectName;
        public nint InstanceName;
        public nint ParentInstance;
        public uint InstanceIndex;
        public nint CounterName;
        public nint ExplainText;
    }

    [DllImport("pdh.dll", EntryPoint = "PdhOpenQueryW", CharSet = CharSet.Unicode)]
    internal static extern uint PdhOpenQuery(
        string? dataSource,
        nint userData,
        out nint query);

    [DllImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", CharSet = CharSet.Unicode)]
    internal static extern uint PdhAddEnglishCounter(
        nint query,
        string fullCounterPath,
        nint userData,
        out nint counter);

    [DllImport("pdh.dll", EntryPoint = "PdhAddCounterW", CharSet = CharSet.Unicode)]
    internal static extern uint PdhAddCounter(
        nint query,
        string fullCounterPath,
        nint userData,
        out nint counter);

    [DllImport("pdh.dll")]
    internal static extern uint PdhCollectQueryData(nint query);

    [DllImport("pdh.dll")]
    internal static extern uint PdhCloseQuery(nint query);

    [DllImport("pdh.dll")]
    internal static extern uint PdhRemoveCounter(nint counter);

    [DllImport("pdh.dll", EntryPoint = "PdhGetFormattedCounterValue")]
    private static extern uint PdhGetFormattedCounterValue(
        nint counter,
        uint format,
        nint counterType,
        ref PdhFormattedCounterValue value);

    [DllImport("pdh.dll", EntryPoint = "PdhGetCounterInfoW", CharSet = CharSet.Unicode)]
    private static extern uint PdhGetCounterInfo(
        nint counter,
        [MarshalAs(UnmanagedType.Bool)] bool retrieveExplainText,
        ref uint bufferSize,
        nint buffer);

    [DllImport("pdh.dll", EntryPoint = "PdhExpandWildCardPathW", CharSet = CharSet.Unicode)]
    private static extern uint PdhExpandWildCardPath(
        string? dataSource,
        string wildcardPath,
        nint expandedPathList,
        ref uint pathListLength,
        uint flags);

    internal static bool TryGetFormattedCounterValue(nint counter, out double value)
    {
        var formattedValue = new PdhFormattedCounterValue();
        uint status = PdhGetFormattedCounterValue(
            counter,
            PdhFormatDouble,
            nint.Zero,
            ref formattedValue);

        if (status == ErrorSuccess &&
            IsValidDataStatus(formattedValue.Status) &&
            double.IsFinite(formattedValue.Value))
        {
            value = formattedValue.Value;
            return true;
        }

        value = 0;
        return false;
    }

    internal static bool TryGetLocalizedCounterPath(
        nint query,
        string englishCounterPath,
        out string localizedCounterPath,
        out uint status)
    {
        localizedCounterPath = string.Empty;
        status = PdhAddEnglishCounter(
            query,
            englishCounterPath,
            nint.Zero,
            out nint translationCounter);
        if (status != ErrorSuccess)
        {
            return false;
        }

        try
        {
            uint bufferSize = 0;
            status = PdhGetCounterInfo(
                translationCounter,
                false,
                ref bufferSize,
                nint.Zero);
            if (status != PdhMoreData || bufferSize == 0)
            {
                return false;
            }

            for (int attempt = 0; attempt < MaximumBufferAttempts; attempt++)
            {
                nint buffer = Marshal.AllocHGlobal(checked((int)bufferSize));
                try
                {
                    uint requestedSize = bufferSize;
                    status = PdhGetCounterInfo(
                        translationCounter,
                        false,
                        ref requestedSize,
                        buffer);

                    if (status == PdhMoreData)
                    {
                        bufferSize = requestedSize > bufferSize
                            ? requestedSize
                            : checked(bufferSize * 2);
                        continue;
                    }

                    if (status != ErrorSuccess)
                    {
                        return false;
                    }

                    PdhCounterInfo counterInfo =
                        Marshal.PtrToStructure<PdhCounterInfo>(buffer);
                    localizedCounterPath =
                        Marshal.PtrToStringUni(counterInfo.FullPath) ?? string.Empty;
                    return localizedCounterPath.Length > 0;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            return false;
        }
        finally
        {
            PdhRemoveCounter(translationCounter);
        }
    }

    internal static bool TryExpandWildcardPath(
        string localizedWildcardPath,
        out IReadOnlyList<string> expandedPaths,
        out uint status)
    {
        expandedPaths = [];
        uint pathListLength = 0;
        status = PdhExpandWildCardPath(
            null,
            localizedWildcardPath,
            nint.Zero,
            ref pathListLength,
            PdhRefreshCounters);

        if (status == PdhNoInstance)
        {
            return true;
        }

        if (status != PdhMoreData || pathListLength == 0)
        {
            return false;
        }

        for (int attempt = 0; attempt < MaximumBufferAttempts; attempt++)
        {
            nint buffer = Marshal.AllocHGlobal(checked((int)pathListLength * sizeof(char)));
            try
            {
                uint requestedLength = pathListLength;
                status = PdhExpandWildCardPath(
                    null,
                    localizedWildcardPath,
                    buffer,
                    ref requestedLength,
                    PdhRefreshCounters);

                if (status == PdhMoreData)
                {
                    pathListLength = requestedLength > pathListLength
                        ? requestedLength
                        : checked(pathListLength * 2);
                    continue;
                }

                if (status == PdhNoInstance)
                {
                    expandedPaths = [];
                    return true;
                }

                if (status != ErrorSuccess)
                {
                    return false;
                }

                expandedPaths = ReadMultiString(buffer);
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return false;
    }

    private static IReadOnlyList<string> ReadMultiString(nint buffer)
    {
        var values = new List<string>();
        nint current = buffer;

        while (true)
        {
            string? value = Marshal.PtrToStringUni(current);
            if (string.IsNullOrEmpty(value))
            {
                return values;
            }

            values.Add(value);
            current = IntPtr.Add(current, checked((value.Length + 1) * sizeof(char)));
        }
    }

    private static bool IsValidDataStatus(uint status) =>
        status is PdhStatusValidData or PdhStatusNewData;
}
