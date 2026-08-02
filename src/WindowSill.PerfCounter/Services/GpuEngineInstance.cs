using System.Globalization;

namespace WindowSill.PerfCounter.Services;

internal readonly record struct GpuEngineInstance(long Luid, string EngineId)
{
    internal static bool TryParse(string instanceName, out GpuEngineInstance instance)
    {
        const string luidMarker = "_luid_";
        const string physicalMarker = "_phys_";
        const string engineMarker = "_eng_";
        const string engineTypeMarker = "_engtype_";

        int luidStart = instanceName.IndexOf(luidMarker, StringComparison.OrdinalIgnoreCase);
        int physicalStart = instanceName.IndexOf(
            physicalMarker,
            luidStart >= 0 ? luidStart + luidMarker.Length : 0,
            StringComparison.OrdinalIgnoreCase);
        int engineStart = instanceName.IndexOf(
            engineMarker,
            physicalStart >= 0 ? physicalStart + physicalMarker.Length : 0,
            StringComparison.OrdinalIgnoreCase);
        int engineTypeStart = instanceName.IndexOf(
            engineTypeMarker,
            engineStart >= 0 ? engineStart + engineMarker.Length : 0,
            StringComparison.OrdinalIgnoreCase);

        if (luidStart < 0 || physicalStart < 0 || engineStart < 0 || engineTypeStart < 0)
        {
            instance = default;
            return false;
        }

        ReadOnlySpan<char> luidToken = instanceName.AsSpan(
            luidStart + luidMarker.Length,
            physicalStart - luidStart - luidMarker.Length);
        int luidSeparator = luidToken.IndexOf('_');

        ReadOnlySpan<char> physicalToken = instanceName.AsSpan(
            physicalStart + physicalMarker.Length,
            engineStart - physicalStart - physicalMarker.Length);
        ReadOnlySpan<char> engineToken = instanceName.AsSpan(
            engineStart + engineMarker.Length,
            engineTypeStart - engineStart - engineMarker.Length);

        if (luidSeparator <= 0 ||
            !TryParseHex(luidToken[..luidSeparator], out uint highPart) ||
            !TryParseHex(luidToken[(luidSeparator + 1)..], out uint lowPart) ||
            physicalToken.IsEmpty ||
            engineToken.IsEmpty)
        {
            instance = default;
            return false;
        }

        long luid = ((long)highPart << 32) | lowPart;
        instance = new GpuEngineInstance(
            luid,
            string.Concat(physicalToken, ":", engineToken));
        return true;
    }

    private static bool TryParseHex(ReadOnlySpan<char> value, out uint result)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        return uint.TryParse(
            value,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out result);
    }
}
