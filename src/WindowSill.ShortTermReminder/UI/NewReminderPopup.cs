using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Text;

using Windows.System;

using WindowSill.API;

namespace WindowSill.ShortTermReminder.UI;

internal sealed partial class NewReminderPopup : ObservableObject
{
    private const int DefaultReminderDurationMinutes = 30;

    private readonly SillPopupContent _view;
    private readonly TextBox _reminderTextBox = new();
    private readonly NumberBox _minutesNumberBox = new();
    private readonly TextBlock _exactTimeTextBlock = new();
    private readonly Button _okButton = new();

    private NewReminderPopup()
    {
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
                            new Grid()
                                .Grid(row: 0)
                                .Padding(24)
                                .RowDefinitions(
                                    GridLength.Auto,
                                    new GridLength(1, GridUnitType.Star)
                                )
                                .Children(
                                    new TextBlock()
                                        .Grid(row: 0)
                                        .Margin(0, 0, 0, 12)
                                        .FontSize(20)
                                        .FontWeight(FontWeights.SemiBold)
                                        .HorizontalAlignment(HorizontalAlignment.Left)
                                        .VerticalAlignment(VerticalAlignment.Top)
                                        .MaxLines(2)
                                        .TextWrapping(TextWrapping.Wrap)
                                        .Text("/WindowSill.ShortTermReminder/NewReminderPopup/Title".GetLocalizedString()),

                                    new StackPanel()
                                        .Grid(row: 1)
                                        .Spacing(12)
                                        .Children(
                                            _reminderTextBox
                                                .TabIndex(0)
                                                .Header("/WindowSill.ShortTermReminder/NewReminderPopup/ReminderTextBox".GetLocalizedString()),

                                            new StackPanel()
                                                .Orientation(Orientation.Horizontal)
                                                .Spacing(8)
                                                .Children(
                                                    new TextBlock()
                                                        .VerticalAlignment(VerticalAlignment.Center)
                                                        .Text("/WindowSill.ShortTermReminder/NewReminderPopup/InHowLong".GetLocalizedString()),

                                                    _minutesNumberBox
                                                        .TabIndex(1)
                                                        .Minimum(1)
                                                        .Maximum(90)
                                                        .SpinButtonPlacementMode(NumberBoxSpinButtonPlacementMode.Compact)
                                                        .SmallChange(5)
                                                        .LargeChange(10),

                                                    new TextBlock()
                                                        .VerticalAlignment(VerticalAlignment.Center)
                                                        .Text("/WindowSill.ShortTermReminder/NewReminderPopup/Minutes".GetLocalizedString())
                                                ),

                                            _exactTimeTextBlock
                                                .Foreground(x => x.ThemeResource("TextFillColorTertiaryBrush"))
                                        )
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
                                    new Grid()
                                        .ColumnSpacing(8)
                                        .ColumnDefinitions(
                                            new GridLength(1, GridUnitType.Star),
                                            new GridLength(1, GridUnitType.Star)
                                        )
                                        .Children(
                                            _okButton
                                                .Grid(column: 1)
                                                .Style(x => x.ThemeResource("LargeAccentButtonStyle"))
                                                .ElementSoundMode(ElementSoundMode.FocusOnly)
                                                .Content("/WindowSill.ShortTermReminder/NewReminderPopup/OkButton".GetLocalizedString())
                                        )
                                )
                        )
                )
            );

        _reminderTextBox.KeyDown += ReminderTextBox_KeyDown;
        _reminderTextBox.TextChanged += ReminderTextBox_TextChanged;
        _minutesNumberBox.KeyDown += ReminderTextBox_KeyDown;
        _minutesNumberBox.ValueChanged += MinutesNumberBox_ValueChanged;
        _okButton.Click += OKButton_Click;
    }

    internal static SillPopupContent CreateView()
    {
        var newReminderPopup2ViewModel = new NewReminderPopup();
        return newReminderPopup2ViewModel._view;
    }

    private void OnOpening()
    {
        _okButton.IsEnabled = false;
        _reminderTextBox.Text = string.Empty;
        _minutesNumberBox.Value = DefaultReminderDurationMinutes;
        UpdateTime();
    }

    private void ReminderTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_reminderTextBox.Text) && _minutesNumberBox.Value > 0;
    }

    private void MinutesNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        UpdateTime();
    }

    private void ReminderTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            Task.Run(async () =>
            {
                await Task.Delay(100);
                _view.DispatcherQueue.TryEnqueue(() =>
                {
                    OKButton_Click(sender, null!);
                });
            });
        }
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_reminderTextBox.Text))
        {
            if (_minutesNumberBox.Value == double.NaN)
            {
                _minutesNumberBox.Value = DefaultReminderDurationMinutes;
            }

            var originalReminderDuration = TimeSpan.FromMinutes(_minutesNumberBox.Value);
            ShortTermReminderService.Instance.AddNewReminder(
                _reminderTextBox.Text,
                originalReminderDuration,
                DateTime.Now + originalReminderDuration);

            _view.Close();
        }
    }

    private void UpdateTime()
    {
        if (_minutesNumberBox.Value == double.NaN)
        {
            _minutesNumberBox.Value = DefaultReminderDurationMinutes;
        }
        int minutes = (int)_minutesNumberBox.Value;
        DateTime reminderTime = DateTime.Now.AddMinutes(minutes);
        _exactTimeTextBlock.Text = string.Format("/WindowSill.ShortTermReminder/NewReminderPopup/WillRemindAt".GetLocalizedString(), reminderTime.ToString("h:mm tt"));
    }
}
