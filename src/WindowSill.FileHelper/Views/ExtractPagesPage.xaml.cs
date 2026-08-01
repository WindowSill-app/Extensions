using Microsoft.UI.Xaml.Controls;

using WindowSill.API;
using WindowSill.FileHelper.ViewModels;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// Page that shows every page of the selected PDF as a thumbnail so the user can tick the ones to extract.
/// </summary>
/// <remarks>
/// Thumbnails are rendered as the repeater realizes each item rather than up front, so a large document opens
/// immediately and only the pages actually scrolled into view are rasterized.
/// </remarks>
internal sealed partial class ExtractPagesPage : Page
{
    internal ExtractPagesPage()
    {
        InitializeComponent();
    }

    internal ExtractPagesViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (ExtractPagesViewModel)e.Parameter;

        // Navigation supplies the view model after InitializeComponent() already ran the first x:Bind pass
        // against a null root, so the bindings need re-evaluating.
        Bindings.Update();

        PagesRepeater.ElementPrepared += PagesRepeater_ElementPrepared;
        Unloaded += ExtractPagesPage_Unloaded;

        LoadAsync().ForgetSafely();
    }

    private async Task LoadAsync()
    {
        await ViewModel.LoadAsync();

        LoadingRing.IsActive = false;
        LoadingRing.Visibility = Visibility.Collapsed;

        if (ViewModel.HasFailed)
        {
            PreviewUnavailableText.Visibility = Visibility.Visible;
        }
        else
        {
            PagesScroller.Visibility = Visibility.Visible;
        }
    }

    private void PagesRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (sender.ItemsSourceView?.GetAt(args.Index) is PdfPageItemViewModel page)
        {
            ViewModel.EnsureThumbnailAsync(page).ForgetSafely();
        }
    }

    private void ExtractPagesPage_Unloaded(object sender, RoutedEventArgs e)
    {
        PagesRepeater.ElementPrepared -= PagesRepeater_ElementPrepared;
        Unloaded -= ExtractPagesPage_Unloaded;
    }
}
