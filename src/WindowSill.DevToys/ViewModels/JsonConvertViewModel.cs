using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowSill.API;
using WindowSill.DevToys.Core;

namespace WindowSill.DevToys.ViewModels;

internal sealed partial class JsonConvertViewModel : ObservableObject
{
    internal SillListViewItem GetView(WindowTextSelection currentSelection)
    {
        return new SillListViewMenuFlyoutItem(
            "/WindowSill.DevToys/Misc/Convert".GetLocalizedString(),
            null,
            new MenuFlyout
            {
                Items =
                {
                    new MenuFlyoutItem
                    {
                        Text = "/WindowSill.DevToys/Misc/Yaml".GetLocalizedString(),
                        Command = ConvertToYamlCommand,
                        CommandParameter = currentSelection
                    }
                }
            });
    }

    [RelayCommand]
    private async Task ConvertToYamlAsync(WindowTextSelection currentSelection)
    {
        string yaml = YamlHelper.ConvertFromJson(currentSelection.SelectedText, Indentation.TwoSpaces);

        if (!string.IsNullOrEmpty(yaml))
        {
            await TextInjector.InjectAsync(yaml, currentSelection);
        }
    }
}
