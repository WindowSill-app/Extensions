using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.UniversalCommands.Core;
using WindowSill.UniversalCommands.ViewModels;

namespace WindowSill.UniversalCommands.Settings;

internal sealed partial class SettingsView : UserControl
{
    internal SettingsView(IPluginInfo pluginInfo, UniversalCommandsService universalCommandsService)
    {
        ViewModel = new SettingsViewModel(universalCommandsService);
        DataContext = ViewModel;
        InitializeComponent();

        EmptyStateImage.Source = new SvgImageSource(new Uri(System.IO.Path.Combine(pluginInfo.GetPluginContentDirectory(), "Assets", "ctrl.svg")));
        Loaded += (s, e) => ViewModel.XamlRoot = XamlRoot;
    }

    public SettingsViewModel ViewModel { get; }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var command = (UniversalCommand)button.DataContext;
        ViewModel.EditCommand.Execute(command);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var command = (UniversalCommand)button.DataContext;
        ViewModel.RemoveCommand.Execute(command);
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
