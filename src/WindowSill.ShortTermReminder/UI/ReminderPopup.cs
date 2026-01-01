using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Text;

using WindowSill.API;

namespace WindowSill.ShortTermReminder.UI;

internal sealed partial class ReminderPopup : ObservableObject
{
    private readonly Reminder _reminder;
    private readonly SillPopupContent _view;
    private readonly TextBlock _exactTimeTextBlock = new();

    private ReminderPopup(Reminder reminder)
    {
        _reminder = reminder;
        _view = new SillPopupContent(OnOpening)
            .Width(350)
            .DataContext(
                this,
                (view, viewModel) => view
                .Content(
                    new Grid()
                        .RowDefinitions(
                            new GridLength(1, GridUnitType.Star),
                            GridLength.Auto
                        )
                        .Children(
                            new StackPanel()
                                .Grid(row: 0)
                                .Padding(24)
                                .Spacing(12)
                                .Children(
                                    new TextBlock()
                                        .FontSize(20)
                                        .FontWeight(FontWeights.SemiBold)
                                        .HorizontalAlignment(HorizontalAlignment.Left)
                                        .VerticalAlignment(VerticalAlignment.Top)
                                        .MaxLines(5)
                                        .TextWrapping(TextWrapping.Wrap)
                                        .TextTrimming(TextTrimming.WordEllipsis)
                                        .Text(reminder.Title),

                                    _exactTimeTextBlock
                                        .Foreground(x => x.ThemeResource("TextFillColorTertiaryBrush"))
                                ),

                            new Border()
                                .Grid(row: 1)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .VerticalAlignment(VerticalAlignment.Bottom)
                                .XYFocusKeyboardNavigation(XYFocusKeyboardNavigationMode.Enabled)
                                .Padding(24)
                                .BorderThickness(0, 1, 0, 0)
                                .BorderBrush(x => x.ThemeResource("CardStrokeColorDefaultBrush"))
                                .Background(x => x.ThemeResource("LayerOnAcrylicFillColorDefaultBrush"))
                                .Child(
                                    new Button()
                                        .ElementSoundMode(ElementSoundMode.FocusOnly)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                        .MinWidth(80)
                                        .Command(() => viewModel.DeleteCommand)
                                        .Content(
                                            new StackPanel()
                                                .Orientation(Orientation.Horizontal)
                                                .Spacing(8)
                                                .Children(
                                                    new FontIcon()
                                                        .FontSize(16)
                                                        .Glyph("\uE74D"),

                                                    new TextBlock()
                                                        .Text("/WindowSill.ShortTermReminder/ReminderSillListViewPopupItem/Delete".GetLocalizedString())
                                                )
                                        )
                                )
                        )
                ));
    }

    internal static SillPopupContent CreateView(Reminder reminder)
    {
        var reminderSillListViewPopupItemViewModel = new ReminderPopup(reminder);
        return reminderSillListViewPopupItemViewModel._view;
    }

    public void OnOpening()
    {
        TimeSpan remainingTime = _reminder.ReminderTime - DateTime.Now;
        _exactTimeTextBlock.Text = string.Format("/WindowSill.ShortTermReminder/ReminderSillListViewPopupItem/ReminderRemainingTime".GetLocalizedString(), remainingTime.Minutes + 1, _reminder.ReminderTime.ToString("h:mm tt"));
    }

    [RelayCommand]
    private void Delete()
    {
        ShortTermReminderService.Instance.DeleteReminder(_reminder.Id);
        _view.Close();
    }
}
