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
}
