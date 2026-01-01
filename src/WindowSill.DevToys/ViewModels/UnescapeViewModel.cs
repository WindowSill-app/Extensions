using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.DevToys.Core;

namespace WindowSill.DevToys.ViewModels;

internal sealed partial class UnescapeViewModel : ObservableObject
{
    internal SillListViewItem GetView(WindowTextSelection currentSelection)
    {
        return new SillListViewButtonItem(
            "/WindowSill.DevToys/Misc/Unescape".GetLocalizedString(),
            null,
            () => UnescapeAsync(currentSelection));
    }

    private async Task UnescapeAsync(WindowTextSelection currentSelection)
    {
        string result = StringHelper.UnescapeString(currentSelection.SelectedText);

        if (!string.IsNullOrEmpty(result))
        {
            await TextInjector.InjectAsync(result, currentSelection);
        }
    }
}
