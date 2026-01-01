using CommunityToolkit.Diagnostics;

using UnitsNet;
using UnitsNet.Units;

using WindowSill.API;

namespace WindowSill.UnitConverter.Dimension;

internal sealed class DimensionConverterViewModel : ViewModelBase
{
    internal SillListViewItem GetView(WindowTextSelection currentSelection)
    {
        Guard.IsTrue(UnitHelper.TryDetectDimension(currentSelection.SelectedText, CancellationToken.None, out IConvertible? dimension));
        Guard.IsNotNull(dimension);

        string displayText;
        string[] conversions;

        switch (dimension)
        {
            case Length length:
                PrepareLengthView(length, out displayText, out conversions);
                break;

            case Information information:
                PrepareInformationView(information, out displayText, out conversions);
                break;

            case Area area:
                PrepareAreaView(area, out displayText, out conversions);
                break;

            case Speed speed:
                PrepareSpeedView(speed, out displayText, out conversions);
                break;

            case Volume volume:
                PrepareVolumeView(volume, out displayText, out conversions);
                break;

            case Mass mass:
                PrepareMassView(mass, out displayText, out conversions);
                break;

            case Angle angle:
                PrepareAngleView(angle, out displayText, out conversions);
                break;

            default:
                throw new NotSupportedException($"Dimension type '{dimension.GetType().Name}' is not supported.");
        }

        var menuFlyout = new MenuFlyout();
        foreach (string conversion in conversions)
        {
            menuFlyout.Items.Add(
                new MenuFlyoutItem
                {
                    Icon = new SymbolIcon(Symbol.Copy),
                    Text = conversion,
                    Command = CopyCommand,
                    CommandParameter = conversion
                });
        }

        string tooltip = string.Join(Environment.NewLine, conversions);

        return new SillListViewMenuFlyoutItem(
            displayText,
            tooltip,
            menuFlyout);
    }

    private static void PrepareLengthView(Length length, out string displayText, out string[] conversions)
    {
        // Smart conversion logic based on context and magnitude
        displayText = length.Unit switch
        {
            // Metric small units - convert up when large enough
            LengthUnit.Millimeter when length.Millimeters >= 1000 => length.ToUnit(LengthUnit.Meter).ToString(),
            LengthUnit.Centimeter when length.Centimeters >= 100 => length.ToUnit(LengthUnit.Meter).ToString(),
            LengthUnit.Meter when length.Meters >= 1000 => length.ToUnit(LengthUnit.Kilometer).ToString(),

            // Metric large units - offer imperial alternative
            LengthUnit.Kilometer => length.ToUnit(LengthUnit.Mile).ToString(),

            // Imperial small units - convert up when large enough
            LengthUnit.Inch when length.Inches >= 12 => length.ToUnit(LengthUnit.Foot).ToString(),
            LengthUnit.Foot when length.Feet >= 3 => length.ToUnit(LengthUnit.Yard).ToString(),
            LengthUnit.Yard when length.Yards >= 1760 => length.ToUnit(LengthUnit.Mile).ToString(),

            // Imperial large units - offer metric alternative
            LengthUnit.Mile => length.ToUnit(LengthUnit.Kilometer).ToString(),

            // Nautical units - cross-convert
            LengthUnit.NauticalMile => length.ToUnit(LengthUnit.Kilometer).ToString(),

            // Default: try intelligent conversion based on magnitude
            _ => length.Meters switch
            {
                // Very small: show in centimeters or inches
                < 0.01 => length.ToUnit(LengthUnit.Millimeter).ToString(),
                < 1 => length.ToUnit(LengthUnit.Centimeter).ToString(),
                // Medium: show in feet (common alternative)
                < 1000 => length.ToUnit(LengthUnit.Foot).ToString(),
                // Large: show in kilometers
                _ => length.ToUnit(LengthUnit.Kilometer).ToString()
            }
        };

        conversions =
        [
            $"{"/WindowSill.UnitConverter/Dimension/Millimeters".GetLocalizedString()}: {length.ToUnit(LengthUnit.Millimeter)}",
            $"{"/WindowSill.UnitConverter/Dimension/Centimeters".GetLocalizedString()}: {length.ToUnit(LengthUnit.Centimeter)}",
            $"{"/WindowSill.UnitConverter/Dimension/Meters".GetLocalizedString()}: {length.ToUnit(LengthUnit.Meter)}",
            $"{"/WindowSill.UnitConverter/Dimension/Kilometers".GetLocalizedString()}: {length.ToUnit(LengthUnit.Kilometer)}",
            $"{"/WindowSill.UnitConverter/Dimension/Inches".GetLocalizedString()}: {length.ToUnit(LengthUnit.Inch)}",
            $"{"/WindowSill.UnitConverter/Dimension/Feet".GetLocalizedString()}: {length.ToUnit(LengthUnit.Foot)}",
            $"{"/WindowSill.UnitConverter/Dimension/Yards".GetLocalizedString()}: {length.ToUnit(LengthUnit.Yard)}",
            $"{"/WindowSill.UnitConverter/Dimension/Miles".GetLocalizedString()}: {length.ToUnit(LengthUnit.Mile)}"
        ];
    }

    private static void PrepareInformationView(Information information, out string displayText, out string[] conversions)
    {
        // Smart conversion logic based on context and magnitude
        displayText = information.Unit switch
        {
            // Small units - convert up when large enough
            InformationUnit.Bit when information.Bits >= 8 => information.ToUnit(InformationUnit.Byte).ToString(),
            InformationUnit.Byte when information.Bytes >= 1024 => information.ToUnit(InformationUnit.Kilobyte).ToString(),
            InformationUnit.Kilobyte when information.Kilobytes >= 1024 => information.ToUnit(InformationUnit.Megabyte).ToString(),
            InformationUnit.Megabyte when information.Megabytes >= 1024 => information.ToUnit(InformationUnit.Gigabyte).ToString(),
            InformationUnit.Gigabyte when information.Gigabytes >= 1024 => information.ToUnit(InformationUnit.Terabyte).ToString(),
            InformationUnit.Terabyte when information.Terabytes >= 1024 => information.ToUnit(InformationUnit.Petabyte).ToString(),

            // Large units - convert down when small enough
            InformationUnit.Petabyte when information.Petabytes < 1 => information.ToUnit(InformationUnit.Terabyte).ToString(),
            InformationUnit.Terabyte when information.Terabytes < 1 => information.ToUnit(InformationUnit.Gigabyte).ToString(),
            InformationUnit.Gigabyte when information.Gigabytes < 1 => information.ToUnit(InformationUnit.Megabyte).ToString(),
            InformationUnit.Megabyte when information.Megabytes < 1 => information.ToUnit(InformationUnit.Kilobyte).ToString(),
            InformationUnit.Kilobyte when information.Kilobytes < 1 => information.ToUnit(InformationUnit.Byte).ToString(),

            // Default: try intelligent conversion based on magnitude
            _ => information.Bytes switch
            {
                < 1 => information.ToUnit(InformationUnit.Bit).ToString(),
                < 1024 => information.ToUnit(InformationUnit.Byte).ToString(),
                < 1024 * 1024 => information.ToUnit(InformationUnit.Kilobyte).ToString(),
                < 1024L * 1024 * 1024 => information.ToUnit(InformationUnit.Megabyte).ToString(),
                < 1024L * 1024 * 1024 * 1024 => information.ToUnit(InformationUnit.Gigabyte).ToString(),
                < 1024L * 1024 * 1024 * 1024 * 1024 => information.ToUnit(InformationUnit.Terabyte).ToString(),
                _ => information.ToUnit(InformationUnit.Petabyte).ToString()
            }
        };

        conversions =
        [
            $"{"/WindowSill.UnitConverter/Dimension/Bits".GetLocalizedString()}: {information.ToUnit(InformationUnit.Bit)}",
            $"{"/WindowSill.UnitConverter/Dimension/Bytes".GetLocalizedString()}: {information.ToUnit(InformationUnit.Byte)}",
            $"{"/WindowSill.UnitConverter/Dimension/Kilobytes".GetLocalizedString()}: {information.ToUnit(InformationUnit.Kilobyte)}",
            $"{"/WindowSill.UnitConverter/Dimension/Megabytes".GetLocalizedString()}: {information.ToUnit(InformationUnit.Megabyte)}",
            $"{"/WindowSill.UnitConverter/Dimension/Gigabytes".GetLocalizedString()}: {information.ToUnit(InformationUnit.Gigabyte)}",
            $"{"/WindowSill.UnitConverter/Dimension/Terabytes".GetLocalizedString()}: {information.ToUnit(InformationUnit.Terabyte)}",
            $"{"/WindowSill.UnitConverter/Dimension/Petabytes".GetLocalizedString()}: {information.ToUnit(InformationUnit.Petabyte)}",
            $"{"/WindowSill.UnitConverter/Dimension/Exabytes".GetLocalizedString()}: {information.ToUnit(InformationUnit.Exabyte)}"
        ];
    }

    private static void PrepareAreaView(Area area, out string displayText, out string[] conversions)
    {
        // Smart conversion logic based on context and magnitude
        displayText = area.Unit switch
        {
            // Metric small units - convert up when large enough
            AreaUnit.SquareMillimeter when area.SquareMillimeters >= 1_000_000 => area.ToUnit(AreaUnit.SquareMeter).ToString(),
            AreaUnit.SquareCentimeter when area.SquareCentimeters >= 10_000 => area.ToUnit(AreaUnit.SquareMeter).ToString(),
            AreaUnit.SquareMeter when area.SquareMeters >= 10_000 => area.ToUnit(AreaUnit.Hectare).ToString(),
            AreaUnit.Hectare when area.Hectares >= 100 => area.ToUnit(AreaUnit.SquareKilometer).ToString(),

            // Metric large units - offer imperial alternative
            AreaUnit.SquareKilometer => area.ToUnit(AreaUnit.SquareMile).ToString(),

            // Imperial small units - convert up when large enough
            AreaUnit.SquareInch when area.SquareInches >= 144 => area.ToUnit(AreaUnit.SquareFoot).ToString(),
            AreaUnit.SquareFoot when area.SquareFeet >= 9 => area.ToUnit(AreaUnit.SquareYard).ToString(),
            AreaUnit.SquareYard when area.SquareYards >= 4840 => area.ToUnit(AreaUnit.Acre).ToString(),
            AreaUnit.Acre when area.Acres >= 640 => area.ToUnit(AreaUnit.SquareMile).ToString(),

            // Imperial large units - offer metric alternative
            AreaUnit.SquareMile => area.ToUnit(AreaUnit.SquareKilometer).ToString(),

            // Default: try intelligent conversion based on magnitude
            _ => area.SquareMeters switch
            {
                < 0.0001 => area.ToUnit(AreaUnit.SquareMillimeter).ToString(),
                < 1 => area.ToUnit(AreaUnit.SquareCentimeter).ToString(),
                < 10_000 => area.ToUnit(AreaUnit.SquareFoot).ToString(),
                < 1_000_000 => area.ToUnit(AreaUnit.Hectare).ToString(),
                _ => area.ToUnit(AreaUnit.SquareKilometer).ToString()
            }
        };

        conversions =
        [
            $"{"/WindowSill.UnitConverter/Dimension/SquareMillimeters".GetLocalizedString()}: {area.ToUnit(AreaUnit.SquareMillimeter)}",
            $"{"/WindowSill.UnitConverter/Dimension/SquareCentimeters".GetLocalizedString()}: {area.ToUnit(AreaUnit.SquareCentimeter)}",
            $"{"/WindowSill.UnitConverter/Dimension/SquareMeters".GetLocalizedString()}: {area.ToUnit(AreaUnit.SquareMeter)}",
            $"{"/WindowSill.UnitConverter/Dimension/Hectares".GetLocalizedString()}: {area.ToUnit(AreaUnit.Hectare)}",
            $"{"/WindowSill.UnitConverter/Dimension/SquareKilometers".GetLocalizedString()}: {area.ToUnit(AreaUnit.SquareKilometer)}",
            $"{"/WindowSill.UnitConverter/Dimension/SquareInches".GetLocalizedString()}: {area.ToUnit(AreaUnit.SquareInch)}",
            $"{"/WindowSill.UnitConverter/Dimension/SquareFeet".GetLocalizedString()}: {area.ToUnit(AreaUnit.SquareFoot)}",
            $"{"/WindowSill.UnitConverter/Dimension/SquareYards".GetLocalizedString()}: {area.ToUnit(AreaUnit.SquareYard)}",
            $"{"/WindowSill.UnitConverter/Dimension/Acres".GetLocalizedString()}: {area.ToUnit(AreaUnit.Acre)}",
            $"{"/WindowSill.UnitConverter/Dimension/SquareMiles".GetLocalizedString()}: {area.ToUnit(AreaUnit.SquareMile)}"
        ];
    }

    private static void PrepareSpeedView(Speed speed, out string displayText, out string[] conversions)
    {
        // Smart conversion logic based on context and magnitude
        displayText = speed.Unit switch
        {
            // Metric units - offer imperial alternative
            SpeedUnit.MeterPerSecond => speed.ToUnit(SpeedUnit.FootPerSecond).ToString(),
            SpeedUnit.KilometerPerHour => speed.ToUnit(SpeedUnit.MilePerHour).ToString(),

            // Imperial units - offer metric alternative
            SpeedUnit.FootPerSecond => speed.ToUnit(SpeedUnit.MeterPerSecond).ToString(),
            SpeedUnit.MilePerHour => speed.ToUnit(SpeedUnit.KilometerPerHour).ToString(),

            // Nautical units - cross-convert
            SpeedUnit.Knot => speed.ToUnit(SpeedUnit.KilometerPerHour).ToString(),

            // Default: try intelligent conversion based on magnitude
            _ => speed.MetersPerSecond switch
            {
                < 1 => speed.ToUnit(SpeedUnit.FootPerSecond).ToString(),
                < 100 => speed.ToUnit(SpeedUnit.KilometerPerHour).ToString(),
                _ => speed.ToUnit(SpeedUnit.MilePerHour).ToString()
            }
        };

        conversions =
        [
            $"{"/WindowSill.UnitConverter/Dimension/MetersPerSecond".GetLocalizedString()}: {speed.ToUnit(SpeedUnit.KilometerPerSecond)}",
            $"{"/WindowSill.UnitConverter/Dimension/KilometersPerHour".GetLocalizedString()}: {speed.ToUnit(SpeedUnit.KilometerPerHour)}",
            $"{"/WindowSill.UnitConverter/Dimension/FeetPerSecond".GetLocalizedString()}: {speed.ToUnit(SpeedUnit.FootPerSecond)}",
            $"{"/WindowSill.UnitConverter/Dimension/MilesPerHour".GetLocalizedString()}: {speed.ToUnit(SpeedUnit.MilePerHour)}",
            $"{"/WindowSill.UnitConverter/Dimension/Knots".GetLocalizedString()}: {speed.ToUnit(SpeedUnit.Knot)}",
            $"{"/WindowSill.UnitConverter/Dimension/Mach".GetLocalizedString()}: {speed.ToUnit(SpeedUnit.Mach)}"
        ];
    }

    private static void PrepareVolumeView(Volume volume, out string displayText, out string[] conversions)
    {
        // Smart conversion logic based on context and magnitude
        displayText = volume.Unit switch
        {
            // Metric small units - convert up when large enough
            VolumeUnit.Milliliter when volume.Milliliters >= 1000 => volume.ToUnit(VolumeUnit.Liter).ToString(),
            VolumeUnit.Liter when volume.Liters >= 1000 => volume.ToUnit(VolumeUnit.CubicMeter).ToString(),

            // Metric large units - offer imperial alternative
            VolumeUnit.CubicMeter => volume.ToUnit(VolumeUnit.CubicFoot).ToString(),

            // Imperial liquid units - convert up when large enough
            VolumeUnit.UsTeaspoon when volume.UsTeaspoons >= 3 => volume.ToUnit(VolumeUnit.UsTablespoon).ToString() + " tsp",
            VolumeUnit.UsTablespoon when volume.UsTablespoons >= 2 => volume.ToUnit(VolumeUnit.UsOunce).ToString() + " tbsp",
            VolumeUnit.UsOunce when volume.UsOunces >= 8 => volume.ToUnit(VolumeUnit.UsCustomaryCup).ToString() + " cup",
            VolumeUnit.UsCustomaryCup when volume.UsCustomaryCups >= 2 => volume.ToUnit(VolumeUnit.UsPint).ToString(),
            VolumeUnit.UsPint when volume.UsPints >= 2 => volume.ToUnit(VolumeUnit.UsQuart).ToString(),
            VolumeUnit.UsQuart when volume.UsQuarts >= 4 => volume.ToUnit(VolumeUnit.UsGallon).ToString(),

            // Imperial liquid units - offer metric alternative
            VolumeUnit.UsGallon => volume.ToUnit(VolumeUnit.Liter).ToString(),

            // Imperial solid units
            VolumeUnit.CubicInch when volume.CubicInches >= 1728 => volume.ToUnit(VolumeUnit.CubicFoot).ToString(),
            VolumeUnit.CubicFoot when volume.CubicFeet >= 27 => volume.ToUnit(VolumeUnit.CubicYard).ToString(),

            // Default: try intelligent conversion based on magnitude
            _ => volume.Liters switch
            {
                < 0.001 => volume.ToUnit(VolumeUnit.Milliliter).ToString(),
                < 1 => volume.ToUnit(VolumeUnit.UsOunce).ToString(),
                < 4 => volume.ToUnit(VolumeUnit.UsCustomaryCup).ToString() + " cup",
                < 1000 => volume.ToUnit(VolumeUnit.UsGallon).ToString(),
                _ => volume.ToUnit(VolumeUnit.CubicMeter).ToString()
            }
        };

        conversions =
        [
            $"{"/WindowSill.UnitConverter/Dimension/Milliliters".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.Milliliter)}",
            $"{"/WindowSill.UnitConverter/Dimension/Centiliters".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.Centiliter)}",
            $"{"/WindowSill.UnitConverter/Dimension/Deciliters".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.Deciliter)}",
            $"{"/WindowSill.UnitConverter/Dimension/Liters".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.Liter)}",
            $"{"/WindowSill.UnitConverter/Dimension/Hectoliters".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.Hectoliter)}",
            $"{"/WindowSill.UnitConverter/Dimension/MetricCups".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.MetricCup)}",
            $"{"/WindowSill.UnitConverter/Dimension/UsTeaspoons".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.UsTeaspoon)}",
            $"{"/WindowSill.UnitConverter/Dimension/UsTablespoons".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.UsTablespoon)}",
            $"{"/WindowSill.UnitConverter/Dimension/UsOunces".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.UsOunce)}",
            $"{"/WindowSill.UnitConverter/Dimension/UsCups".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.UsCustomaryCup)}",
            $"{"/WindowSill.UnitConverter/Dimension/UsLegalCups".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.UsLegalCup)}",
            $"{"/WindowSill.UnitConverter/Dimension/UsPints".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.UsPint)}",
            $"{"/WindowSill.UnitConverter/Dimension/UsQuarts".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.UsQuart)}",
            $"{"/WindowSill.UnitConverter/Dimension/UsGallons".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.UsGallon)}",
            $"{"/WindowSill.UnitConverter/Dimension/CubicInches".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.CubicInch)}",
            $"{"/WindowSill.UnitConverter/Dimension/CubicInches".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.CubicInch)}",
            $"{"/WindowSill.UnitConverter/Dimension/CubicFeet".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.CubicFoot)}",
            $"{"/WindowSill.UnitConverter/Dimension/CubicMeters".GetLocalizedString()}: {volume.ToUnit(VolumeUnit.CubicMeter)}",
        ];
    }

    private static void PrepareMassView(Mass mass, out string displayText, out string[] conversions)
    {
        // Smart conversion logic based on context and magnitude
        displayText = mass.Unit switch
        {
            // Metric small units - convert up when large enough
            MassUnit.Milligram when mass.Milligrams >= 1000 => mass.ToUnit(MassUnit.Gram).ToString(),
            MassUnit.Gram when mass.Grams >= 1000 => mass.ToUnit(MassUnit.Kilogram).ToString(),
            MassUnit.Kilogram when mass.Kilograms >= 1000 => mass.ToUnit(MassUnit.Tonne).ToString(),

            // Metric large units - offer imperial alternative
            MassUnit.Tonne => mass.ToUnit(MassUnit.ShortTon).ToString(),

            // Imperial small units - convert up when large enough
            MassUnit.Ounce when mass.Ounces >= 16 => mass.ToUnit(MassUnit.Pound).ToString(),
            MassUnit.Pound when mass.Pounds >= 2000 => mass.ToUnit(MassUnit.ShortTon).ToString(),

            // Imperial large units - offer metric alternative
            MassUnit.ShortTon => mass.ToUnit(MassUnit.Tonne).ToString(),

            // Default: try intelligent conversion based on magnitude
            _ => mass.Kilograms switch
            {
                < 0.001 => mass.ToUnit(MassUnit.Milligram).ToString(),
                < 1 => mass.ToUnit(MassUnit.Gram).ToString(),
                < 1000 => mass.ToUnit(MassUnit.Pound).ToString(),
                _ => mass.ToUnit(MassUnit.Tonne).ToString()
            }
        };

        conversions =
        [
            $"{"/WindowSill.UnitConverter/Dimension/Milligrams".GetLocalizedString()}: {mass.ToUnit(MassUnit.Milligram)}",
            $"{"/WindowSill.UnitConverter/Dimension/Grams".GetLocalizedString()}: {mass.ToUnit(MassUnit.Gram)}",
            $"{"/WindowSill.UnitConverter/Dimension/Kilograms".GetLocalizedString()}: {mass.ToUnit(MassUnit.Kilogram)}",
            $"{"/WindowSill.UnitConverter/Dimension/Tonnes".GetLocalizedString()}: {mass.ToUnit(MassUnit.Tonne)}",
            $"{"/WindowSill.UnitConverter/Dimension/Ounces".GetLocalizedString()}: {mass.ToUnit(MassUnit.Ounce)}",
            $"{"/WindowSill.UnitConverter/Dimension/Pounds".GetLocalizedString()}: {mass.ToUnit(MassUnit.Pound)}",
            $"{"/WindowSill.UnitConverter/Dimension/ShortTons".GetLocalizedString()}: {mass.ToUnit(MassUnit.ShortTon)}"
        ];
    }

    private static void PrepareAngleView(Angle angle, out string displayText, out string[] conversions)
    {
        // Smart conversion logic based on context and magnitude
        displayText = angle.Unit switch
        {
            // Convert degrees to radians and vice versa
            AngleUnit.Degree => angle.ToUnit(AngleUnit.Radian).ToString(),
            AngleUnit.Radian => angle.ToUnit(AngleUnit.Degree).ToString(),

            // Convert gradian to degrees
            AngleUnit.Gradian => angle.ToUnit(AngleUnit.Degree).ToString(),

            // Convert arcminutes/arcseconds to degrees
            AngleUnit.Arcminute => angle.ToUnit(AngleUnit.Degree).ToString(),
            AngleUnit.Arcsecond => angle.ToUnit(AngleUnit.Degree).ToString(),

            // Default: show in degrees
            _ => angle.ToUnit(AngleUnit.Degree).ToString()
        };

        conversions =
        [
            $"{"/WindowSill.UnitConverter/Dimension/Degrees".GetLocalizedString()}: {angle.ToUnit(AngleUnit.Degree)}",
            $"{"/WindowSill.UnitConverter/Dimension/Radians".GetLocalizedString()}: {angle.ToUnit(AngleUnit.Radian)}",
            $"{"/WindowSill.UnitConverter/Dimension/Gradians".GetLocalizedString()}: {angle.ToUnit(AngleUnit.Gradian)}",
            $"{"/WindowSill.UnitConverter/Dimension/Arcminutes".GetLocalizedString()}: {angle.ToUnit(AngleUnit.Arcminute)}",
            $"{"/WindowSill.UnitConverter/Dimension/Arcseconds".GetLocalizedString()}: {angle.ToUnit(AngleUnit.Arcsecond)}"
        ];
    }
}
