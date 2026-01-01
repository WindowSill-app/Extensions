using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media.Animation;

using Windows.Media.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

using WindowSill.API;

using WinUIEx;

namespace WindowSill.ShortTermReminder.UI;

internal sealed partial class FullScreenNotificationWindow : ObservableObject
{
    private readonly Reminder _reminder;
    private readonly AcrylicWindowFrameworkElement _view;
    private readonly MediaPlayerElement _backgroundMediaPlayer = new();
    private readonly TaskCompletionSource<bool> _windowClosedTaskCompletionSource = new();
    private readonly Action<FullScreenNotificationWindow>? _onWindowClosed;
    private readonly RECT? _monitorRect;
    private readonly bool _playAudio;

    internal FullScreenNotificationWindow(Reminder reminder, RECT? monitorRect = null, Action<FullScreenNotificationWindow>? onWindowClosed = null, bool playAudio = true)
    {
        var uri = new Uri($@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\media\Windows Notify Calendar.wav");
        var audioNotificationMediaSource = MediaSource.CreateFromUri(uri);
        _backgroundMediaPlayer.Source = audioNotificationMediaSource;

        _reminder = reminder;
        _monitorRect = monitorRect;
        _onWindowClosed = onWindowClosed;
        _playAudio = playAudio;
        _view = new AcrylicWindowFrameworkElement()
            .DataContext(
                this,
                (view, viewModel) => view
                .Content(
                    new Grid()
                        .Children(
                            _backgroundMediaPlayer
                                .AreTransportControlsEnabled(false)
                                .IsFullWindow(false)
                                .HorizontalAlignment(HorizontalAlignment.Left)
                                .VerticalAlignment(VerticalAlignment.Top)
                                .Stretch(Stretch.None)
                                .Height(0)
                                .Width(0),

                            new Border()
                                .BorderThickness(1)
                                .BorderBrush(x => x.ThemeResource("CardStrokeColorDefaultBrush"))
                                .Background(x => x.ThemeResource("LayerOnAcrylicFillColorDefaultBrush"))
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .CornerRadius(x => x.ThemeResource("OverlayCornerRadius"))
                                .Margin(72)
                                .MaxWidth(1200)
                                .Child(
                                    new Grid()
                                        .RowDefinitions(
                                            new GridLength(1, GridUnitType.Star),
                                            GridLength.Auto
                                        )
                                        .Children(
                                            new StackPanel()
                                                .Grid(row: 0)
                                                .Padding(36, 40, 36, 40)
                                                .Spacing(16)
                                                .Background(x => x.ThemeResource("LayerOnAcrylicFillColorDefaultBrush"))
                                                .ChildrenTransitions(new EntranceThemeTransition() { IsStaggeringEnabled = true })
                                                .Children(
                                                    new TextBlock()
                                                        .Style(x => x.ThemeResource("BodyTextBlockStyle"))
                                                        .Foreground(x => x.ThemeResource("TextFillColorSecondaryBrush"))
                                                        .HorizontalTextAlignment(TextAlignment.Center)
                                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                        .CharacterSpacing(200)
                                                        .Text("/WindowSill.ShortTermReminder/FullScreenNotificationWindow/Title".GetLocalizedString()),

                                                    new TextBlock()
                                                        .Style(x => x.ThemeResource("TitleLargeTextBlockStyle"))
                                                        .MaxLines(5)
                                                        .TextWrapping(TextWrapping.WrapWholeWords)
                                                        .TextTrimming(TextTrimming.WordEllipsis)
                                                        .HorizontalTextAlignment(TextAlignment.Center)
                                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                        .Text(reminder.Title)
                                                ),

                                            new Border()
                                                .Grid(row: 1)
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                .VerticalAlignment(VerticalAlignment.Bottom)
                                                .Padding(36, 40, 36, 40)
                                                .BorderThickness(0, 1, 0, 0)
                                                .BorderBrush(x => x.ThemeResource("CardStrokeColorDefaultBrush"))
                                                .Child(
                                                    new StackPanel()
                                                        .Orientation(Orientation.Horizontal)
                                                        .Spacing(8)
                                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                                        .Children(
                                                            new Button()
                                                                .Command(() => viewModel.DismissCommand)
                                                                .Content(
                                                                    new StackPanel()
                                                                        .Orientation(Orientation.Horizontal)
                                                                        .Spacing(8)
                                                                        .Children(
                                                                            new FontIcon()
                                                                                .FontSize(16)
                                                                                .Glyph("\uE894"),

                                                                            new TextBlock()
                                                                                .Text("/WindowSill.ShortTermReminder/FullScreenNotificationWindow/Dismiss".GetLocalizedString())
                                                                        )
                                                                ),

                                                            new Button()
                                                                .Command(() => viewModel.SnoozeCommand)
                                                                .Content(
                                                                    new StackPanel()
                                                                        .Orientation(Orientation.Horizontal)
                                                                        .Spacing(8)
                                                                        .Children(
                                                                            new FontIcon()
                                                                                .FontSize(16)
                                                                                .Glyph("\uF4BD"),

                                                                            new TextBlock()
                                                                                .Text("/WindowSill.ShortTermReminder/FullScreenNotificationWindow/Snooze".GetLocalizedString())
                                                                        )
                                                                )
                                                        )
                                                )
                                        )
                                )
                        )
                )
            );

        _view.UnderlyingWindow.IsMinimizable = false;
        _view.UnderlyingWindow.IsMaximizable = false;
        _view.UnderlyingWindow.IsResizable = false;
        _view.UnderlyingWindow.IsTitleBarVisible = false;
        _view.UnderlyingWindow.IsShownInSwitchers = false;
        _view.UnderlyingWindow.IsAlwaysOnTop = true;

        if (_view.UnderlyingWindow.PresenterKind == AppWindowPresenterKind.Overlapped && _view.UnderlyingWindow.Presenter is OverlappedPresenter overlappedPresenter)
        {
            // Remove the window border and title bar.
            overlappedPresenter.SetBorderAndTitleBar(false, false);

            WindowStyle style = _view.UnderlyingWindow.GetWindowStyle();
            style &= ~WindowStyle.DlgFrame;
            _view.UnderlyingWindow.SetWindowStyle(style);
        }

        unsafe
        {
            if (IsWindows11OrGreater())
            {
                // Remove the rounded corners from the window.
                uint cornerPreference = (uint)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND;
                Guard.IsTrue(
                    PInvoke.DwmSetWindowAttribute(
                        (HWND)_view.UnderlyingWindow.GetWindowHandle(),
                        DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                        &cornerPreference,
                        sizeof(uint))
                    .Succeeded);
            }

            // Make the window still appearing when user uses Peek Desktop feature.
            int renderPolicy = (int)DWMNCRENDERINGPOLICY.DWMNCRP_ENABLED;
            Guard.IsTrue(
                PInvoke.DwmSetWindowAttribute(
                    (HWND)_view.UnderlyingWindow.GetWindowHandle(),
                    DWMWINDOWATTRIBUTE.DWMWA_EXCLUDED_FROM_PEEK,
                    &renderPolicy,
                    sizeof(int))
                .Succeeded);
        }

        _backgroundMediaPlayer.Loaded += BackgroundMediaPlayer_Loaded;
        _view.UnderlyingWindow.Closed += UnderlyingWindow_Closed;
    }

    internal async Task ShowAsync()
    {
        if (_monitorRect.HasValue)
        {
            // Position the window on the specific monitor
            RECT rect = _monitorRect.Value;
            _view.UnderlyingWindow.MoveAndResize(
                rect.left,
                rect.top,
                rect.right - rect.left,
                rect.bottom - rect.top);
        }
        else
        {
            _view.UnderlyingWindow.Maximize();
        }

        _view.UnderlyingWindow.Show();
        _view.UnderlyingWindow.BringToFront();
        _view.UnderlyingWindow.Activate();
        _view.UnderlyingWindow.SetForegroundWindow();

        await _windowClosedTaskCompletionSource.Task;
    }

    internal void Close()
    {
        _view.UnderlyingWindow.Close();
    }

    private void UnderlyingWindow_Closed(object sender, WindowEventArgs e)
    {
        _windowClosedTaskCompletionSource.TrySetResult(true);
        _onWindowClosed?.Invoke(this);
    }

    private void BackgroundMediaPlayer_Loaded(object sender, RoutedEventArgs e)
    {
        if (_playAudio)
        {
            _backgroundMediaPlayer.MediaPlayer.Play();
        }
    }

    [RelayCommand]
    private void Dismiss()
    {
        ShortTermReminderService.Instance.DeleteReminder(_reminder.Id);
        _view.UnderlyingWindow.Close();
    }

    [RelayCommand]
    private void Snooze()
    {
        ShortTermReminderService.Instance.SnoozeReminder(_reminder, TimeSpan.FromMinutes(5));
        _view.UnderlyingWindow.Close();
    }

    private static bool IsWindows11OrGreater()
    {
        return Environment.OSVersion.Version >= new Version(10, 0, 22000);
    }

    private partial class BlurredBackdrop : CompositionBrushBackdrop
    {
        protected override Windows.UI.Composition.CompositionBrush CreateBrush(Windows.UI.Composition.Compositor compositor)
            => compositor.CreateHostBackdropBrush();
    }
}
