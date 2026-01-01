using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.DevToys.Core;

namespace WindowSill.DevToys.ViewModels;

internal sealed partial class JsonSortViewModel : ObservableObject
{
    internal SillListViewItem GetView(WindowTextSelection currentSelection)
    {
        return new SillListViewButtonItem(
            "/WindowSill.DevToys/Misc/Sort".GetLocalizedString(),
            null,
            () => SortJsonAsync(currentSelection));
    }

    private async Task SortJsonAsync(WindowTextSelection currentSelection)
    {
        string sortedJson = JsonHelper.Sort(currentSelection.SelectedText);

        if (!string.IsNullOrEmpty(sortedJson))
        {
            await TextInjector.InjectAsync(sortedJson, currentSelection);
        }
    }
}
