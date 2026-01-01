using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.AppLauncher.Core;

namespace WindowSill.AppLauncher.Settings;

public sealed partial class SettingsView : UserControl
{
    private readonly AppGroupService _appGroupService;

    internal SettingsView(Uri iconPath, AppGroupService appGroupService)
    {
        _appGroupService = appGroupService;
        IsEmpty = _appGroupService.AppGroups.Count == 0;

        InitializeComponent();

        EmptyStateImage.Source = new SvgImageSource(iconPath);
    }

    public static readonly DependencyProperty IsEmptyProperty
        = DependencyProperty.Register(
            nameof(IsEmpty),
            typeof(bool),
            typeof(SettingsView),
            new PropertyMetadata(true));

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }

    internal ObservableCollection<AppGroup> AppGroups => _appGroupService.AppGroups;

    private void NewGroupButton_Click(object sender, RoutedEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            AppGroup? newAppGroup
                = await EditGroupContentDialog.NewGroupAsync(
                    this.FindParent<WindowFrameworkElement>()!);

            if (newAppGroup is not null)
            {
                IsEmpty = false;
                _appGroupService.AppGroups.Insert(0, newAppGroup);
                await _appGroupService.SaveAsync();
            }
        }).ForgetSafely();
    }

    private void EditGroupButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var appGroup = (AppGroup)button.DataContext;

        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            AppGroup? newAppGroup
                = await EditGroupContentDialog.EditGroupAsync(
                    this.FindParent<WindowFrameworkElement>()!,
                    appGroup);

            if (newAppGroup is not null)
            {
                int index = _appGroupService.AppGroups.IndexOf(appGroup);
                _appGroupService.AppGroups[index] = newAppGroup;
                await _appGroupService.SaveAsync();
            }
        }).ForgetSafely();
    }

    private void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var appGroup = (AppGroup)button.DataContext;
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            _appGroupService.AppGroups.Remove(appGroup);
            IsEmpty = _appGroupService.AppGroups.Count == 0;
            await _appGroupService.SaveAsync();
        }).ForgetSafely();
    }

    private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var element = (Border)sender;
        element.Background = Application.Current.Resources["ControlFillColorSecondaryBrush"] as Brush;
    }

    private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var element = (Border)sender;
        element.Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush;
    }

    private void Border_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var element = (Border)sender;
        element.Background = Application.Current.Resources["ControlFillColorTertiaryBrush"] as Brush;
    }
}
