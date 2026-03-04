using WindowSill.API;
using WindowSill.TextFinder.Core;
using WindowSill.TextFinder.Core.MeaningSimilarity;
using WindowSill.TextFinder.ViewModels;
using WinUIEx;

namespace WindowSill.TextFinder.Views;

public sealed partial class FindWindow : WindowFrameworkElement
{
    private readonly Lazy<EmbeddingService> _embeddingService;

    internal FindWindow(IPluginInfo pluginInfo, InputData inputData)
    {
        _embeddingService = new(() => new EmbeddingService(pluginInfo));
        ViewModel = new FindWindowViewModel(inputData, _embeddingService, pluginInfo, () => XamlRoot);

        InitializeComponent();

        UnderlyingWindow.MinWidth = 600;
        UnderlyingWindow.MinHeight = 250;
        UnderlyingWindow.SystemBackdrop = new MicaBackdrop();

        UnderlyingWindow.ExtendsContentIntoTitleBar = true;
        UnderlyingWindow.SetTitleBar(TitleBar);
        UnderlyingWindow.Title = TitleBar.Title;

        UnderlyingWindow.Closed += UnderlyingWindow_Closed;

        TitleBar.IconSource = new BitmapIconSource
        {
            UriSource = new Uri(System.IO.Path.Combine(pluginInfo.GetPluginContentDirectory(), "Assets", "search.png")),
            ShowAsMonochrome = false,
        };

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void UnderlyingWindow_Closed(object sender, WindowEventArgs args)
    {
        UnderlyingWindow.Closed -= UnderlyingWindow_Closed;
        if (_embeddingService.IsValueCreated)
        {
            _embeddingService.Value.Dispose();
        }
    }

    internal FindWindowViewModel ViewModel { get; }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FindWindowViewModel.SelectedFindResult) && ViewModel.SelectedFindResult is not null)
        {
            ResultsListView.ScrollIntoView(ViewModel.SelectedFindResult);
        }
    }
}
