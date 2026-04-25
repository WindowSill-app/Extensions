using WindowSill.API;

namespace WindowSill.Date.Core.UI;

/// <summary>
/// Extension methods for applying sill orientation visual states to controls.
/// </summary>
internal static class SillOrientationHelper
{
    /// <summary>
    /// Applies the visual state matching the given orientation and size
    /// using <see cref="VisualStateManager"/>.
    /// </summary>
    /// <param name="control">The control that owns the VisualStateGroups.</param>
    /// <param name="orientationAndSize">The current sill orientation and size.</param>
    /// <returns><see langword="true"/> if the state was applied successfully.</returns>
    public static bool ApplyOrientationState(this Control control, SillOrientationAndSize orientationAndSize)
    {
        string stateName = orientationAndSize switch
        {
            SillOrientationAndSize.HorizontalLarge => "HorizontalLarge",
            SillOrientationAndSize.HorizontalMedium => "HorizontalMedium",
            SillOrientationAndSize.HorizontalSmall => "HorizontalSmall",
            SillOrientationAndSize.VerticalLarge => "VerticalLarge",
            SillOrientationAndSize.VerticalMedium => "VerticalMedium",
            SillOrientationAndSize.VerticalSmall => "VerticalSmall",
            _ => throw new NotSupportedException($"Unsupported {nameof(SillOrientationAndSize)}: {orientationAndSize}")
        };

        return VisualStateManager.GoToState(control, stateName, useTransitions: true);
    }

    /// <summary>
    /// Computes the current <see cref="SillOrientationAndSize"/> from settings.
    /// Mirrors the logic in <c>SillViewBase.UpdateSillInfo()</c> so that callers
    /// that cannot rely on the <c>SillViewBase</c> Loaded/Unloaded subscription
    /// window can compute the value independently.
    /// </summary>
    public static SillOrientationAndSize ComputeOrientationAndSize(ISettingsProvider settingsProvider)
    {
        SillLocation location = settingsProvider.GetSetting(PredefinedSettings.SillLocation);
        bool isHorizontal = location is SillLocation.Top or SillLocation.Bottom;

        return settingsProvider.GetSetting(PredefinedSettings.SillSize) switch
        {
            SillSize.Small => isHorizontal ? SillOrientationAndSize.HorizontalSmall : SillOrientationAndSize.VerticalSmall,
            SillSize.Medium => isHorizontal ? SillOrientationAndSize.HorizontalMedium : SillOrientationAndSize.VerticalMedium,
            SillSize.Large => isHorizontal ? SillOrientationAndSize.HorizontalLarge : SillOrientationAndSize.VerticalLarge,
            _ => SillOrientationAndSize.HorizontalMedium,
        };
    }
}
