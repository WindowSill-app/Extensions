using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowSill.API;
using WindowSill.DevToys.Core;

namespace WindowSill.DevToys.ViewModels;
internal sealed partial class JsonFormatterViewModel : ObservableObject
{
    internal SillListViewItem GetView(WindowTextSelection currentSelection)
    {
        return new SillListViewMenuFlyoutItem(
            "/WindowSill.DevToys/Misc/Format".GetLocalizedString(),
            null,
            new MenuFlyout
            {
                Items =
                {
                    new MenuFlyoutItem
                    {
                        Text = "/WindowSill.DevToys/Misc/Minified".GetLocalizedString(),
                        Command = FormatJsonCommand,
                        CommandParameter = (currentSelection, Indentation.Minified)
                    },
                    new MenuFlyoutItem
                    {
                        Text = "/WindowSill.DevToys/Misc/TwoSpaces".GetLocalizedString(),
                        Command = FormatJsonCommand,
                        CommandParameter = (currentSelection, Indentation.TwoSpaces)
                    },
                    new MenuFlyoutItem
                    {
                        Text = "/WindowSill.DevToys/Misc/FourSpaces".GetLocalizedString(),
                        Command = FormatJsonCommand,
                        CommandParameter = (currentSelection, Indentation.FourSpaces)
                    },
                    new MenuFlyoutItem
                    {
                        Text = "/WindowSill.DevToys/Misc/OneTab".GetLocalizedString(),
                        Command = FormatJsonCommand,
                        CommandParameter = (currentSelection, Indentation.OneTab)
                    }
                }
            });
    }

    [RelayCommand]
    private async Task FormatJsonAsync((WindowTextSelection currentSelection, Indentation indentation) value)
    {
        string formattedJson = JsonHelper.Format(value.currentSelection.SelectedText, value.indentation);

        if (!string.IsNullOrEmpty(formattedJson))
        {
            await TextInjector.InjectAsync(formattedJson, value.currentSelection);
        }
    }
}
