using WindowSill.PerfCounter.Services.Interop;

namespace WindowSill.PerfCounter.Services;

internal static class ThermalZoneSelector
{
    private static readonly string[] CpuZoneMarkers =
    [
        "CPU",
        "PROCESSOR",
        "PACKAGE",
        "PKG"
    ];

    internal static double? GetCpuTemperature(
        IReadOnlyList<PdhCounterValue> thermalZones)
    {
        double? hottestTemperature = null;

        foreach (PdhCounterValue thermalZone in thermalZones)
        {
            if (!IsCpuZone(thermalZone.Name))
            {
                continue;
            }

            double celsius = thermalZone.Value - 273.15;
            if (!double.IsFinite(celsius) || celsius is < -40 or > 150)
            {
                continue;
            }

            if (!hottestTemperature.HasValue || celsius > hottestTemperature.Value)
            {
                hottestTemperature = celsius;
            }
        }

        return hottestTemperature.HasValue
            ? Math.Round(hottestTemperature.Value, 1)
            : null;
    }

    private static bool IsCpuZone(string instanceName) =>
        CpuZoneMarkers.Any(marker =>
            instanceName.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
