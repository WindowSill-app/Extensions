using WindowSill.Date.Core.Services;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Settings view for managing world clock timezone entries.
/// Supports drag-and-drop reordering, rename via flyout, and delete.
/// </summary>
internal sealed partial class WorldClockSettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClockSettingsView"/> class.
    /// </summary>
    /// <param name="worldClockService">The world clock service for CRUD operations.</param>
    public WorldClockSettingsView(WorldClockService worldClockService)
    {
        ViewModel = new WorldClockSettingsViewModel(worldClockService);
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this settings view.
    /// </summary>
    internal WorldClockSettingsViewModel ViewModel { get; }

    private void CitySearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.OnSearchTextChanged(sender.Text);
        }
    }

    private void CitySearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is CitySearchResult result)
        {
            ViewModel.AddFromSearchResult(result);
            sender.Text = string.Empty;
        }
    }

    private void ClockListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        ViewModel.PersistCurrentOrder();
    }
}
