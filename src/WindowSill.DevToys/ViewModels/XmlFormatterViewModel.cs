using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowSill.API;
using WindowSill.DevToys.Core;

namespace WindowSill.DevToys.ViewModels;

internal sealed partial class XmlFormatterViewModel : ObservableObject
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
                        Command = FormatXmlCommand,
                        CommandParameter = (currentSelection, Indentation.Minified)
                    },
                    new MenuFlyoutItem
                    {
                        Text = "/WindowSill.DevToys/Misc/TwoSpaces".GetLocalizedString(),
                        Command = FormatXmlCommand,
                        CommandParameter = (currentSelection, Indentation.TwoSpaces)
                    },
                    new MenuFlyoutItem
                    {
                        Text = "/WindowSill.DevToys/Misc/FourSpaces".GetLocalizedString(),
                        Command = FormatXmlCommand,
                        CommandParameter = (currentSelection, Indentation.FourSpaces)
                    },
                    new MenuFlyoutItem
                    {
                        Text = "/WindowSill.DevToys/Misc/OneTab".GetLocalizedString(),
                        Command = FormatXmlCommand,
                        CommandParameter = (currentSelection, Indentation.OneTab)
                    }
                }
            });
    }

    [RelayCommand]
    private async Task FormatXmlAsync((WindowTextSelection currentSelection, Indentation indentation) value)
    {
        string formattedXml = XmlHelper.Format(value.currentSelection.SelectedText, value.indentation, newLineOnAttributes: true);

        if (!string.IsNullOrEmpty(formattedXml))
        {
            await TextInjector.InjectAsync(formattedXml, value.currentSelection);
        }
    }
}
