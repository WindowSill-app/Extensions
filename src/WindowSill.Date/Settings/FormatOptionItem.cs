namespace WindowSill.Date.Settings;

/// <summary>
/// Represents an item in a combo box with a value and localized display name.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
internal sealed class FormatOptionItem<T> where T : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatOptionItem{T}"/> class.
    /// </summary>
    /// <param name="value">The format enum value.</param>
    /// <param name="displayName">The localized display name or live preview.</param>
    public FormatOptionItem(T value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    /// <summary>
    /// Gets the format enum value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the display name shown in the combo box.
    /// </summary>
    public string DisplayName { get; }

    /// <inheritdoc/>
    public override string ToString() => DisplayName;
}
