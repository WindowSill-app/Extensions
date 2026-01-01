using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;

using System.Timers;

using WindowSill.API;

using Timer = System.Timers.Timer;

namespace WindowSill.ShortTermReminder.UI;

internal sealed partial class ReminderSillListViewPopupItem : ObservableObject, IDisposable
{
    private readonly Timer _timer;
    private readonly SillListViewPopupItem _view;
    private readonly TextBlock _previewFlyoutReminderTimeTextBlock = new();
    private readonly ProgressRing _progressRing = new();
    private readonly TextBlock _innerMinuteIndicatorTextBlock = new();
    private readonly TextBlock _outterMinuteIndicatorTextBlock = new();
    private readonly TextBlock _reminderTextBlock = new();

    private ReminderSillListViewPopupItem(Reminder reminder)
    {
        StackPanel previewFlyoutContent
            = new StackPanel()
                .Spacing(8)
                .Padding(8)
                .Children(
                    new TextBlock()
                        .Grid(column: 1)
                        .Style(x => x.ThemeResource("BodyTextBlockStyle"))
                        .TextTrimming(TextTrimming.CharacterEllipsis)
                        .TextWrapping(TextWrapping.NoWrap)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Text(reminder.Title),

                    _previewFlyoutReminderTimeTextBlock
                        .Style(x => x.ThemeResource("CaptionTextBlockStyle"))
                        .Foreground(x => x.ThemeResource("TextFillColorSecondaryBrush"))
                );

        _view = new SillListViewPopupItem()
            .PreviewFlyoutContent(previewFlyoutContent)
            .DataContext(
                this,
                (view, viewModel) => view
                .Content(
                    new StackPanel()
                        .Orientation(Orientation.Horizontal)
                        .Spacing(8)
                        .Margin(x => x.ThemeResource("SillCommandContentMargin"))
                        .Children(
                            new Grid()
                                .Children(
                                    _progressRing
                                        .MinHeight(0)
                                        .MinWidth(0)
                                        .IsIndeterminate(false),

                                    _innerMinuteIndicatorTextBlock
                                        .VerticalAlignment(VerticalAlignment.Center)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                ),

                            _outterMinuteIndicatorTextBlock
                                .VerticalAlignment(VerticalAlignment.Center)
                                .HorizontalAlignment(HorizontalAlignment.Left)
                                .Visibility(Visibility.Collapsed),

                            _reminderTextBlock
                                .VerticalAlignment(VerticalAlignment.Center)
                                .HorizontalAlignment(HorizontalAlignment.Left)
                                .Visibility(Visibility.Collapsed)
                        )
                )
            );

        Reminder = reminder;

        // Initialize the polling timer
        _timer = new Timer(1_000);
        _timer.Elapsed += TimerElapsed;

        _reminderTextBlock.Text = Reminder.Title;

        EnsureTimerRunning();

        _view.PopupContent = ReminderPopup.CreateView(reminder);

        OnIsSillOrientationOrSizeChanged(null, EventArgs.Empty);
        _view.IsSillOrientationOrSizeChanged += OnIsSillOrientationOrSizeChanged;
    }

    internal static SillListViewPopupItem CreateView(Reminder reminder)
    {
        var reminderSillListViewPopupItemViewModel = new ReminderSillListViewPopupItem(reminder);
        return reminderSillListViewPopupItemViewModel._view;
    }

    internal Reminder Reminder { get; }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }

    private void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateUI();
    }

    internal void EnsureTimerRunning()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _progressRing.Maximum = Reminder.OriginalReminderDuration.TotalSeconds;
        _progressRing.Minimum = 0;
        _progressRing.Value = Reminder.OriginalReminderDuration.TotalSeconds;

        if (!_timer.Enabled)
        {
            _timer.Start();
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        _view.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                TimeSpan remainingTime = Reminder.ReminderTime - DateTime.Now;
                if (remainingTime.TotalSeconds <= 0)
                {
                    // If the reminder time has passed, stop the timer and update the UI accordingly
                    _timer.Stop();
                    _previewFlyoutReminderTimeTextBlock.Text = "/WindowSill.ShortTermReminder/ReminderSillListViewPopupItem/ReminderPassed".GetLocalizedString();
                    _innerMinuteIndicatorTextBlock.Text = "0";
                    _outterMinuteIndicatorTextBlock.Text = "0";
                    _progressRing.Value = 0;

                    _view.StartFlashing();
                    ShortTermReminderService.Instance.NotifyUserAsync(Reminder).Forget();
                }
                else
                {
                    _previewFlyoutReminderTimeTextBlock.Text = string.Format("/WindowSill.ShortTermReminder/ReminderSillListViewPopupItem/ReminderRemainingTime".GetLocalizedString(), remainingTime.Minutes + 1, Reminder.ReminderTime.ToString("h:mm tt"));
                    _innerMinuteIndicatorTextBlock.Text = remainingTime.TotalMinutes.ToString("0");
                    _outterMinuteIndicatorTextBlock.Text = remainingTime.TotalMinutes.ToString("0");
                    _progressRing.Value = remainingTime.TotalSeconds;
                }
            }
            catch
            {
                // Handle exceptions (e.g., if the focused element cannot be retrieved)
            }
        });
    }

    private void OnIsSillOrientationOrSizeChanged(object? sender, EventArgs e)
    {
        _progressRing.Height(32).Width(32);
        _innerMinuteIndicatorTextBlock.Visibility(Visibility.Visible);
        _outterMinuteIndicatorTextBlock.Visibility(Visibility.Collapsed);
        _outterMinuteIndicatorTextBlock.Margin(0);
        _reminderTextBlock.Visibility(Visibility.Collapsed);

        switch (_view.SillOrientationAndSize)
        {
            case SillOrientationAndSize.HorizontalLarge:
                break;

            case SillOrientationAndSize.HorizontalMedium:
                _progressRing.Height(16).Width(16);
                _innerMinuteIndicatorTextBlock.Visibility(Visibility.Collapsed);
                _outterMinuteIndicatorTextBlock.Visibility(Visibility.Visible);
                break;

            case SillOrientationAndSize.HorizontalSmall:
                _progressRing.Height(12).Width(12);
                _innerMinuteIndicatorTextBlock.Visibility(Visibility.Collapsed);
                _outterMinuteIndicatorTextBlock.Visibility(Visibility.Visible);
                break;

            case SillOrientationAndSize.VerticalLarge:
                _reminderTextBlock.Visibility(Visibility.Visible);
                break;

            case SillOrientationAndSize.VerticalMedium:
                _reminderTextBlock.Visibility(Visibility.Visible);
                break;

            case SillOrientationAndSize.VerticalSmall:
                _innerMinuteIndicatorTextBlock.Visibility(Visibility.Collapsed);
                _outterMinuteIndicatorTextBlock.Visibility(Visibility.Visible);
                _outterMinuteIndicatorTextBlock.Margin(4, 0, 0, 0);
                break;

            default:
                throw new NotSupportedException($"Unsupported {nameof(SillOrientationAndSize)}: {_view.SillOrientationAndSize}");
        }
    }
}
