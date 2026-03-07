namespace WindowSill.TextFinder.Core.Converters;

/// <summary>
/// Converts between an enum value and a boolean by comparing against a string parameter.
/// Intended for binding radio buttons to an enum property.
/// </summary>
internal sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Enum enumValue && parameter is string parameterString)
        {
            return enumValue.ToString() == parameterString;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string parameterString)
        {
            return Enum.Parse(targetType, parameterString);
        }

        throw new InvalidOperationException("Parameter must be a string representing the enum value.");
    }
}
