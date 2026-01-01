using CommunityToolkit.Diagnostics;

using UnitsNet.Units;

using WindowSill.API;

namespace WindowSill.UnitConverter.Temperature;

internal sealed class TemperatureConverterViewModel : ViewModelBase
{
    internal SillListViewItem GetView(WindowTextSelection currentSelection)
    {
        Guard.IsTrue(UnitHelper.TryDetectTemperature(currentSelection.SelectedText, CancellationToken.None, out UnitsNet.Temperature temperature));

        string displayText = temperature.Unit switch
        {
            TemperatureUnit.DegreeCelsius => temperature.ToUnit(TemperatureUnit.DegreeFahrenheit).ToString(),
            TemperatureUnit.DegreeFahrenheit => temperature.ToUnit(TemperatureUnit.DegreeCelsius).ToString(),
            TemperatureUnit.Kelvin => temperature.ToUnit(TemperatureUnit.DegreeCelsius).ToString(),
            TemperatureUnit.DegreeRankine => temperature.ToUnit(TemperatureUnit.DegreeCelsius).ToString(),
            TemperatureUnit.DegreeDelisle => temperature.ToUnit(TemperatureUnit.DegreeCelsius).ToString(),
            _ => temperature.ToString()
        };

        string tooltipText = $"""
            Celsius: {temperature.ToUnit(TemperatureUnit.DegreeCelsius)}
            Fahrenheit: {temperature.ToUnit(TemperatureUnit.DegreeFahrenheit)}
            Kelvin: {temperature.ToUnit(TemperatureUnit.Kelvin)}
            Rankine: {temperature.ToUnit(TemperatureUnit.DegreeRankine)}
            Delisle: {temperature.ToUnit(TemperatureUnit.DegreeDelisle)}
            """;

        string[] temperatures = new[]
        {
            temperature.ToUnit(TemperatureUnit.DegreeCelsius).ToString(),
            temperature.ToUnit(TemperatureUnit.DegreeFahrenheit).ToString(),
            temperature.ToUnit(TemperatureUnit.Kelvin).ToString(),
            temperature.ToUnit(TemperatureUnit.DegreeRankine).ToString(),
            temperature.ToUnit(TemperatureUnit.DegreeDelisle).ToString()
        };

        var menuFlyout = new MenuFlyout();
        for (int i = 0; i < temperatures.Length; i++)
        {
            menuFlyout.Items.Add(
                new MenuFlyoutItem
                {
                    Icon = new SymbolIcon(Symbol.Copy),
                    Text = temperatures[i],
                    Command = CopyCommand,
                    CommandParameter = temperatures[i]
                });
        }

        return new SillListViewMenuFlyoutItem(
            displayText,
            tooltipText,
            menuFlyout);
    }
}
