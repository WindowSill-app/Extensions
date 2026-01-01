using System.Runtime.CompilerServices;
using Microsoft.Recognizers.Text;
using UnitsNet;
using UnitsNet.Units;
using WindowSill.UnitConverter.Exchange;

namespace WindowSill.UnitConverter;

internal static class UnitHelper
{
    private const string Value = "value";
    private const string Unit = "unit";
    private const string Subtype = "subtype";
    private const string IsoCurrency = "isoCurrency";
    private const string CurrencyTypeName = "currency";

    private const string INFORMATION = "Information";
    private const string AREA = "Area";
    private const string LENGTH = "Length";
    private const string SPEED = "Speed";
    private const string VOLUME = "Volume";
    private const string WEIGHT = "Weight";
    private const string ANGLE = "Angle";

    private static readonly IReadOnlyDictionary<string, TemperatureUnit> temperatureMap = new Dictionary<string, TemperatureUnit>(StringComparer.OrdinalIgnoreCase)
        {
            { "C", TemperatureUnit.DegreeCelsius },
            { "F", TemperatureUnit.DegreeFahrenheit },
            { "K", TemperatureUnit.Kelvin },
            { "Kelvin", TemperatureUnit.Kelvin },
            { "R", TemperatureUnit.DegreeRankine },
            { "D", TemperatureUnit.DegreeDelisle },
            { "Degree", TemperatureUnit.DegreeFahrenheit },
            { "Degré", TemperatureUnit.DegreeCelsius }
        };

    private static readonly IReadOnlyDictionary<string, LengthUnit> lengthMap = new Dictionary<string, LengthUnit>(StringComparer.OrdinalIgnoreCase)
        {
            { "Centimeter", LengthUnit.Centimeter },
            { "Decameter", LengthUnit.Decameter },
            { "Decimeter", LengthUnit.Decimeter },
            { "Foot", LengthUnit.Foot },
            { "Hectometer", LengthUnit.Hectometer },
            { "Inch", LengthUnit.Inch },
            { "Kilometer", LengthUnit.Kilometer },
            { "Meter", LengthUnit.Meter },
            { "Micrometer", LengthUnit.Micrometer },
            { "Mile", LengthUnit.Mile },
            { "Millimeter", LengthUnit.Millimeter },
            { "Nanometer", LengthUnit.Nanometer },
            { "Yard", LengthUnit.Yard }
        };

    private static readonly IReadOnlyDictionary<string, InformationUnit> informationMap = new Dictionary<string, InformationUnit>(StringComparer.OrdinalIgnoreCase)
        {
            { "Bit", InformationUnit.Bit },
            { "Kilobit", InformationUnit.Kilobit },
            { "Megabit", InformationUnit.Megabit },
            { "Gigabit", InformationUnit.Gigabit },
            { "Terabit", InformationUnit.Terabit },
            { "Petabit", InformationUnit.Petabit },
            { "Byte", InformationUnit.Byte },
            { "Kilobyte", InformationUnit.Kilobyte },
            { "Megabyte", InformationUnit.Megabyte },
            { "Gigabyte", InformationUnit.Gigabyte },
            { "Terabyte", InformationUnit.Terabyte },
            { "Petabyte", InformationUnit.Petabyte }
        };

    private static readonly IReadOnlyDictionary<string, AreaUnit> areaMap = new Dictionary<string, AreaUnit>(StringComparer.OrdinalIgnoreCase)
        {
            { "Acre", AreaUnit.Acre },
            { "Square hectometer", AreaUnit.Hectare },
            { "Square centimeter", AreaUnit.SquareCentimeter },
            { "Square decimeter", AreaUnit.SquareDecimeter },
            { "Square foot", AreaUnit.SquareFoot },
            { "Square inch", AreaUnit.SquareInch },
            { "Square kilometer", AreaUnit.SquareKilometer },
            { "Square meter", AreaUnit.SquareMeter },
            { "Square mile", AreaUnit.SquareMile },
            { "Square millimeter", AreaUnit.SquareMillimeter },
            { "Square yard", AreaUnit.SquareYard }
        };

    private static readonly IReadOnlyDictionary<string, SpeedUnit> speedMap = new Dictionary<string, SpeedUnit>(StringComparer.OrdinalIgnoreCase)
        {
            { "Meter per second", SpeedUnit.MeterPerSecond },
            { "Kilometer per hour", SpeedUnit.KilometerPerHour },
            { "Kilometer per minute", SpeedUnit.KilometerPerMinute },
            { "Kilometer per second", SpeedUnit.KilometerPerSecond },
            { "Mile per hour", SpeedUnit.MilePerHour },
            { "Knot", SpeedUnit.Knot },
            { "Foot per second", SpeedUnit.FootPerSecond },
            { "Foot per minute", SpeedUnit.FootPerMinute },
            { "Yard per minute", SpeedUnit.YardPerMinute },
            { "Yard per second", SpeedUnit.YardPerSecond }
        };

    private static readonly IReadOnlyDictionary<string, VolumeUnit> volumeMap = new Dictionary<string, VolumeUnit>(StringComparer.OrdinalIgnoreCase)
        {
            { "Cubic meter", VolumeUnit.CubicMeter },
            { "Cubic centimeter", VolumeUnit.CubicCentimeter },
            { "Cubic millimiter", VolumeUnit.CubicMillimeter },
            { "Hectoliter", VolumeUnit.Hectoliter },
            { "Decaliter", VolumeUnit.Decaliter },
            { "Liter", VolumeUnit.Liter },
            { "Deciliter", VolumeUnit.Deciliter },
            { "Centiliter", VolumeUnit.Centiliter },
            { "Milliliter", VolumeUnit.Milliliter },
            { "Cubic yard", VolumeUnit.CubicYard },
            { "Cubic inch", VolumeUnit.CubicInch },
            { "Cubic foot", VolumeUnit.CubicFoot },
            { "Cubic mile", VolumeUnit.CubicMile },
            { "Fluid ounce", VolumeUnit.UsOunce },
            { "Teaspoon", VolumeUnit.UsTeaspoon },
            { "Tablespoon", VolumeUnit.UsTablespoon },
            { "Pint", VolumeUnit.UsPint },
            { "Quart", VolumeUnit.UsQuart },
            { "Cup", VolumeUnit.UsCustomaryCup },
            { "Barrel", VolumeUnit.OilBarrel },
            { "Gallon", VolumeUnit.UsGallon }
        };

    private static readonly IReadOnlyDictionary<string, MassUnit> massMap = new Dictionary<string, MassUnit>(StringComparer.OrdinalIgnoreCase)
        {
            { "Kilogram", MassUnit.Kilogram },
            { "Gram", MassUnit.Gram },
            { "Milligram", MassUnit.Milligram },
            { "Microgram", MassUnit.Microgram },
            { "Ton", MassUnit.Tonne },
            { "Pound", MassUnit.Pound },
            { "Ounce", MassUnit.Ounce },
            { "Grain", MassUnit.Grain },
            { "Long ton (British)", MassUnit.LongTon },
            { "Short ton (US)", MassUnit.ShortTon },
            { "Short hundredweight (US)", MassUnit.ShortHundredweight },
            { "Stone", MassUnit.Stone }
        };

    private static readonly IReadOnlyDictionary<string, AngleUnit> angleMap = new Dictionary<string, AngleUnit>(StringComparer.OrdinalIgnoreCase)
        {
            { "Degree", AngleUnit.Degree },
            { "Radian", AngleUnit.Radian }
        };

    internal static bool TryDetectTemperature(string input, CancellationToken cancellationToken, out UnitsNet.Temperature temperature)
    {
        if (!string.IsNullOrWhiteSpace(input))
        {
            input = input.Replace("˚", null);
            foreach (Culture culture in Cultures.OrderedSupportedCultures)
            {
                List<ModelResult> models = Microsoft.Recognizers.Text.NumberWithUnit.NumberWithUnitRecognizer.RecognizeTemperature(input, culture.CultureName, fallbackToDefaultCulture: true);
                cancellationToken.ThrowIfCancellationRequested();

                if (models.Count == 1)
                {
                    ModelResult model = models[0];
                    if (model.Resolution is not null
                        && model.Resolution.TryGetValue(Value, out object? value)
                        && value is string valueString
                        && double.TryParse(valueString, out double valueDouble)
                        && model.Resolution.TryGetValue(Unit, out object? unit)
                        && unit is string unitString
                        && temperatureMap.TryGetValue(unitString, out TemperatureUnit temperatureUnit))
                    {
                        temperature = new UnitsNet.Temperature(valueDouble, temperatureUnit);
                        return true;
                    }
                }
            }
        }

        temperature = default;
        return false;
    }

    internal static bool TryDetectCurrency(string input, CancellationToken cancellationToken, out CurrencyValue? currency)
    {
        if (!string.IsNullOrWhiteSpace(input))
        {
            foreach (Culture culture in Cultures.OrderedSupportedCultures)
            {
                List<ModelResult> models = Microsoft.Recognizers.Text.NumberWithUnit.NumberWithUnitRecognizer.RecognizeCurrency(input, culture.CultureName, fallbackToDefaultCulture: true);
                cancellationToken.ThrowIfCancellationRequested();

                if (models.Count == 1)
                {
                    ModelResult model = models[0];
                    if (model.TypeName == CurrencyTypeName
                        && model.Resolution is not null
                        && model.Resolution.TryGetValue(Value, out object? value)
                        && value is string valueString
                        && double.TryParse(valueString, out double valueDouble)
                        && model.Resolution.TryGetValue(Unit, out object? unit)
                        && unit is string unitString
                        && !string.IsNullOrWhiteSpace(unitString))
                    {
                        string isoCurrency = string.Empty;
                        if (model.Resolution.TryGetValue(IsoCurrency, out object? isoCurrencyObject))
                        {
                            isoCurrency = isoCurrencyObject as string ?? string.Empty;
                        }
                        else if (unitString == "Dollar")
                        {
                            isoCurrency = "USD";
                        }
                        else if (unitString == "Pound")
                        {
                            isoCurrency = "GBP";
                        }

                        if (!string.IsNullOrWhiteSpace(isoCurrency))
                        {
                            currency = new CurrencyValue(double.Parse(valueString), unitString, isoCurrency);
                            return true;
                        }
                    }
                }
            }
        }

        currency = null;
        return false;
    }

    internal static bool TryDetectDimension(string input, CancellationToken cancellationToken, out IConvertible? dimension)
    {
        if (!string.IsNullOrWhiteSpace(input))
        {
            foreach (Culture culture in Cultures.OrderedSupportedCultures)
            {
                List<ModelResult> models = Microsoft.Recognizers.Text.NumberWithUnit.NumberWithUnitRecognizer.RecognizeDimension(input, culture.CultureName, fallbackToDefaultCulture: true);
                cancellationToken.ThrowIfCancellationRequested();

                if (models.Count == 1)
                {
                    ModelResult model = models[0];

                    if (model.Resolution is not null
                        && model.Resolution.TryGetValue(Value, out object? value)
                        && value is string valueString
                        && model.Resolution.TryGetValue(Unit, out object? unit)
                        && unit is string unitString
                        && model.Resolution.TryGetValue(Subtype, out object? subtype)
                        && subtype is string subtypeString)
                    {
                        switch (subtypeString)
                        {
                            case LENGTH:
                                if (TryParseLength(unitString, valueString, out dimension))
                                {
                                    return true;
                                }
                                break;

                            case INFORMATION:
                                if (TryParseInformation(unitString, valueString, out dimension))
                                {
                                    return true;
                                }
                                break;

                            case AREA:
                                if (TryParseArea(unitString, valueString, out dimension))
                                {
                                    return true;
                                }
                                break;

                            case SPEED:
                                if (TryParseSpeed(unitString, valueString, out dimension))
                                {
                                    return true;
                                }
                                break;

                            case VOLUME:
                                if (TryParseVolume(unitString, valueString, out dimension))
                                {
                                    return true;
                                }
                                break;

                            case WEIGHT:
                                if (TryParseMass(unitString, valueString, out dimension))
                                {
                                    return true;
                                }
                                break;

                            case ANGLE:
                                if (TryParseAngle(unitString, valueString, out dimension))
                                {
                                    return true;
                                }
                                break;
                        }
                    }
                }
            }
        }

        dimension = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseLength(string unit, string valueString, out IConvertible? result)
    {
        if (lengthMap.TryGetValue(unit, out LengthUnit lengthUnit))
        {
            result = new Length(double.Parse(valueString), lengthUnit);
            return true;
        }

        result = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseInformation(string unit, string valueString, out IConvertible? result)
    {
        if (informationMap.TryGetValue(unit, out InformationUnit informationUnit))
        {
            result = new Information(decimal.Parse(valueString), informationUnit);
            return true;
        }

        result = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseArea(string unit, string valueString, out IConvertible? result)
    {
        if (areaMap.TryGetValue(unit, out AreaUnit areaUnit))
        {
            result = new Area(double.Parse(valueString), areaUnit);
            return true;
        }

        result = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseSpeed(string unit, string valueString, out IConvertible? result)
    {
        if (speedMap.TryGetValue(unit, out SpeedUnit speedUnit))
        {
            result = new Speed(double.Parse(valueString), speedUnit);
            return true;
        }

        result = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseVolume(string unit, string valueString, out IConvertible? result)
    {
        if (volumeMap.TryGetValue(unit, out VolumeUnit volumeUnit))
        {
            result = new Volume(double.Parse(valueString), volumeUnit);
            return true;
        }

        result = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseMass(string unit, string valueString, out IConvertible? result)
    {
        if (massMap.TryGetValue(unit, out MassUnit massUnit))
        {
            result = new Mass(double.Parse(valueString), massUnit);
            return true;
        }

        result = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseAngle(string unit, string valueString, out IConvertible? result)
    {
        if (angleMap.TryGetValue(unit, out AngleUnit angleUnit))
        {
            result = new Angle(double.Parse(valueString), angleUnit);
            return true;
        }

        result = null;
        return false;
    }
}
