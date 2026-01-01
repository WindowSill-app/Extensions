using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.DevToys.Core;

namespace WindowSill.DevToys.ViewModels;

internal sealed partial class Base64DecodeViewModel : ObservableObject
{
    internal SillListViewItem GetView(WindowTextSelection currentSelection)
    {
        return new SillListViewButtonItem(
            "/WindowSill.DevToys/Misc/Decode".GetLocalizedString(),
            null,
            () => DecodeAsync(currentSelection));
    }

    private async Task DecodeAsync(WindowTextSelection currentSelection)
    {
        string result = Base64Helper.FromBase64ToText(currentSelection.SelectedText);

        if (!string.IsNullOrEmpty(result))
        {
            await TextInjector.InjectAsync(result, currentSelection);
        }
    }
}
