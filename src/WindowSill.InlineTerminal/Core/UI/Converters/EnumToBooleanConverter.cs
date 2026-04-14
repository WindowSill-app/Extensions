namespace WindowSill.InlineTerminal.Core.UI.Converters;

/// <summary>
/// Converts between an enum value and a boolean by comparing against a string parameter.
/// </summary>
internal sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Enum enumValue && parameter is string parameterString)
        {
            if (parameterString.Contains('|'))
            {
                string[] parameterValues = parameterString.Split('|');
                return parameterValues.Any(p => p == enumValue.ToString());
            }

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
