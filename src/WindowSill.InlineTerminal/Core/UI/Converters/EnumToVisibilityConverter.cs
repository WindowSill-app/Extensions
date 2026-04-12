namespace WindowSill.InlineTerminal.Core.UI.Converters;

/// <summary>
/// Converts between an enum value and a <see cref="Visibility"/> by comparing against a string parameter.
/// Intended for binding radio buttons to an enum property.
/// </summary>
internal sealed class EnumToVisibilityConverter : IValueConverter
{
    public Visibility WhenMatch { get; set; } = Visibility.Visible;

    public Visibility WhenNotMatch { get; set; } = Visibility.Collapsed;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Enum enumValue && parameter is string parameterString)
        {
            if (parameterString.Contains("|"))
            {
                string[] parameterValues = parameterString.Split('|');
                if (parameterValues.Any(p => p == enumValue.ToString()))
                {
                    return WhenMatch;
                }

                return WhenNotMatch;
            }

            return enumValue.ToString() == parameterString ? WhenMatch : WhenNotMatch;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
