using WindowSill.API;

namespace WindowSill.CalendarPlus.FirstTimeSetup;

/// <summary>
/// View for the Calendar Plus first-time setup step.
/// </summary>
internal sealed class CalendarFirstTimeSetupView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarFirstTimeSetupView"/> class.
    /// </summary>
    public CalendarFirstTimeSetupView()
    {
        this.Content(
            new StackPanel()
                .Spacing(16)
                .Children(
                    new TextBlock()
                        .TextWrapping(TextWrapping.WrapWholeWords)
                        .Text("/WindowSill.CalendarPlus/Setup/SetupDescription".GetLocalizedString()),
                    new TextBlock()
                        .TextWrapping(TextWrapping.WrapWholeWords)
                        .Foreground(x => x.ThemeResource("TextFillColorSecondaryBrush"))
                        .Text("/WindowSill.CalendarPlus/Setup/NoAccountsYet".GetLocalizedString())
                )
        );
    }
}
