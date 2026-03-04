using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
using WindowSill.TextFinder.Core;
using WindowSill.TextFinder.Core.MeaningSimilarity;
using WindowSill.TextFinder.Views;
using Path = System.IO.Path;

namespace WindowSill.TextFinder.ViewModels;

internal sealed partial class FindWindowViewModel : ObservableObject
{
    private const int DebounceDelayMs = 300;

    private readonly System.Threading.Lock _lock = new();
    private readonly InputData _inputData;
    private readonly IPluginInfo _pluginInfo;
    private readonly Func<XamlRoot> _xamlRootProvider;
    private readonly Lazy<EmbeddingService> _embeddingService;

    private bool _ignoreSearchModeChanged;
    private SearchMode _previousSearchMode = SearchMode.LiteralMatch;
    private CancellationTokenSource? _debounceCts;
    private CancellationTokenSource? _findResultsCts;

    public FindWindowViewModel(InputData inputData, Lazy<EmbeddingService> embeddingService, IPluginInfo pluginInfo, Func<XamlRoot> xamlRootProvider)
    {
        _inputData = inputData;
        _embeddingService = embeddingService;
        _pluginInfo = pluginInfo;
        _xamlRootProvider = xamlRootProvider;

        LoadSourceTextAsync().Forget();
    }

    [ObservableProperty]
    internal partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FindResultCountText))]
    [NotifyPropertyChangedFor(nameof(NoResultFound))]
    internal partial bool IsSearching { get; set; }

    [ObservableProperty]
    internal partial string SourceText { get; set; } = string.Empty;

    [ObservableProperty]
    internal partial string SourceTitle { get; set; } = string.Empty;

    [ObservableProperty]
    internal partial string FindText { get; set; } = string.Empty;

    [ObservableProperty]
    internal partial string ReplaceText { get; set; } = string.Empty;

    [ObservableProperty]
    internal partial SearchMode SearchMode { get; set; } = SearchMode.LiteralMatch;

    [ObservableProperty]
    internal partial bool MatchCase { get; set; }

    [ObservableProperty]
    internal partial bool MatchWholeWord { get; set; }

    [ObservableProperty]
    internal partial double MeaningSimilarityThreshold { get; set; } = 40;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FindResultCountText))]
    [NotifyPropertyChangedFor(nameof(NoResultFound))]
    internal partial ObservableCollection<FindResult> FindResults { get; set; } = [];

    [ObservableProperty]
    internal partial FindResult? SelectedFindResult { get; set; }

    internal bool NoResultFound => FindResults.Count == 0 && !IsSearching;

    internal string FindResultCountText => FindResults.Count == 0
        ? "No results"
        : string.Format("/WindowSill.TextFinder/FindWindow/ResultCount".GetLocalizedString(), FindResults.Count);

    partial void OnFindTextChanged(string value)
    {
        UpdateFindResults();
    }

    partial void OnSearchModeChanged(SearchMode value)
    {
        if (value == SearchMode.MeaningSimilarity && !_ignoreSearchModeChanged)
        {
            _ = EnsureMeaningSimilarityModelExistsAsync();
        }
        else
        {
            _previousSearchMode = value;
            UpdateFindResults();
        }
    }

    private async Task EnsureMeaningSimilarityModelExistsAsync()
    {
        XamlRoot xamlRoot = _xamlRootProvider();
        bool downloaded = await ModelDownloadDialogContent.ShowAndDownloadAsync(xamlRoot, _pluginInfo);

        if (downloaded)
        {
            _previousSearchMode = SearchMode.MeaningSimilarity;
            UpdateFindResults();
        }
        else
        {
            // User cancelled - revert to previous search mode
            _ignoreSearchModeChanged = true;
            SearchMode = _previousSearchMode;
            _ignoreSearchModeChanged = false;
        }
    }

    partial void OnMatchCaseChanged(bool value)
    {
        UpdateFindResults();
    }

    partial void OnMatchWholeWordChanged(bool value)
    {
        UpdateFindResults();
    }

    partial void OnMeaningSimilarityThresholdChanged(double value)
    {
        UpdateFindResults();
    }

    [RelayCommand]
    private void SelectPreviousResult()
    {
        if (FindResults.Count == 0)
        {
            return;
        }

        int currentIndex = SelectedFindResult is null ? 0 : FindResults.IndexOf(SelectedFindResult);
        int previousIndex = currentIndex <= 0 ? FindResults.Count - 1 : currentIndex - 1;
        SelectedFindResult = FindResults[previousIndex];
    }

    [RelayCommand]
    private void SelectNextResult()
    {
        if (FindResults.Count == 0)
        {
            return;
        }

        int currentIndex = SelectedFindResult is null ? -1 : FindResults.IndexOf(SelectedFindResult);
        int nextIndex = currentIndex >= FindResults.Count - 1 ? 0 : currentIndex + 1;
        SelectedFindResult = FindResults[nextIndex];
    }

    [RelayCommand]
    private void DismissResult(FindResult result)
    {
        FindResults.Remove(result);
    }

    [RelayCommand]
    private void ReplaceResult(FindResult result)
    {
        if (result.Match.Length == 0)
        {
            return;
        }

        int resultIndex = FindResults.IndexOf(result);
        if (resultIndex < 0)
        {
            return;
        }

        int originalMatchIndex = result.Match.Index;
        int originalMatchLength = result.Match.Length;
        int lengthDelta = ReplaceText.Length - originalMatchLength;

        FindResults.RemoveAt(resultIndex);

        // Adjust subsequent results' positions by the length difference
        for (int i = resultIndex; i < FindResults.Count; i++)
        {
            FindResult subsequentResult = FindResults[i];
            subsequentResult.Match = new TextSpan(
                subsequentResult.Match.Index + lengthDelta,
                subsequentResult.Match.Length);
        }

        SourceText = string.Concat(
            SourceText.AsSpan(0, originalMatchIndex),
            ReplaceText,
            SourceText.AsSpan(originalMatchIndex + originalMatchLength));
    }

    [RelayCommand]
    private void ReplaceAll()
    {
        if (FindResults.Count == 0)
        {
            return;
        }

        // Replace from end to beginning to avoid position recalculation
        StringBuilder stringBuilder = PooledStringBuilder.Instance.Get();
        try
        {
            stringBuilder.Append(SourceText);
            for (int i = FindResults.Count - 1; i >= 0; i--)
            {
                FindResult findResult = FindResults[i];
                stringBuilder.Remove(findResult.Match.Index, findResult.Match.Length);
                stringBuilder.Insert(findResult.Match.Index, ReplaceText);
            }

            SourceText = stringBuilder.ToString();
        }
        finally
        {
            PooledStringBuilder.Instance.Return(stringBuilder);
        }

        FindResults.Clear();
    }

    [RelayCommand]
    private void Copy()
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(SourceText);
        Clipboard.SetContent(dataPackage);
        Clipboard.Flush();
    }

    private void UpdateFindResults()
    {
        // Cancel any pending debounce and previous search operation
        lock (_lock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();

            _findResultsCts?.Cancel();
            _findResultsCts?.Dispose();
            _findResultsCts = null;
        }

        CancellationToken debounceToken = _debounceCts.Token;

        // Capture values for background execution
        string sourceText = SourceText;
        string findText = FindText;
        SearchMode searchMode = SearchMode;
        bool matchCase = MatchCase;
        bool matchWholeWord = MatchWholeWord;
        float similarityThreshold = (float)MeaningSimilarityThreshold / 100;

        Task.Run(async () =>
        {
            try
            {
                // Debounce: wait before executing the search
                await Task.Delay(DebounceDelayMs, debounceToken);

                await ThreadHelper.RunOnUIThreadAsync(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                    () =>
                    {
                        FindResults.Clear();
                        IsSearching = true;
                    });

                CancellationToken searchToken;
                lock (_lock)
                {
                    _findResultsCts = new CancellationTokenSource();
                    searchToken = _findResultsCts.Token;
                }

                ObservableCollection<FindResult> results
                    = TextSearchHelper.FindMatches(
                        sourceText,
                        findText,
                        searchMode,
                        matchCase,
                        matchWholeWord,
                        similarityThreshold,
                        _embeddingService,
                        searchToken);

                if (searchToken.IsCancellationRequested)
                {
                    return;
                }

                await ThreadHelper.RunOnUIThreadAsync(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                    () =>
                    {
                        if (searchToken.IsCancellationRequested)
                        {
                            return;
                        }

                        FindResults = results;
                        IsSearching = false;
                    });
            }
            catch (OperationCanceledException)
            {
                // Debounce or search was cancelled - expected behavior
            }
            catch
            {
                // Handle other exceptions silently
            }
        }, debounceToken).Forget();
    }

    private async Task LoadSourceTextAsync()
    {
        await Task.Run(async () =>
        {
            string sourceTitle = string.Empty;
            string sourceText = string.Empty;
            if (!string.IsNullOrEmpty(_inputData.FilePath) && !string.IsNullOrEmpty(_inputData.DragAndDropActivatorTypeName))
            {
                sourceTitle = Path.GetFileName(_inputData.FilePath);
                sourceText = await LoadSourceTextFromFileAsync(_inputData.DragAndDropActivatorTypeName, _inputData.FilePath);
            }
            else if (_inputData.TextSelection is not null)
            {
                sourceTitle = _inputData.TextSelection.WindowTitle;
                sourceText = _inputData.TextSelection.SelectedText;
            }

            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                SourceTitle = sourceTitle;
                SourceText = sourceText;
                IsLoading = false;
            });
        });
    }

    private static async Task<string> LoadSourceTextFromFileAsync(string dragAndDropActivatorTypeName, string filePath)
    {
        string fileTextContent = string.Empty;
        switch (dragAndDropActivatorTypeName)
        {
            // Read plain text file.
            case PredefinedActivationTypeNames.PlainTextFileDrop:
                fileTextContent = await File.ReadAllTextAsync(filePath);
                break;

            // Read PDF file.
            case PredefinedActivationTypeNames.PdfFileDrop:
                {
                    StringBuilder stringBuilder = PooledStringBuilder.Instance.Get();
                    try
                    {
                        using var document = PdfDocument.Open(filePath);
                        foreach (UglyToad.PdfPig.Content.Page page in document.GetPages())
                        {
                            stringBuilder.Append(ContentOrderTextExtractor.GetText(page));
                        }
                        fileTextContent = stringBuilder.ToString();
                    }
                    finally
                    {
                        PooledStringBuilder.Instance.Return(stringBuilder);
                    }
                    break;
                }

            // Read DOCX file.
            case PredefinedActivationTypeNames.DocxFileDrop:
                {
                    StringBuilder stringBuilder = PooledStringBuilder.Instance.Get();
                    try
                    {
                        using var doc = WordprocessingDocument.Open(filePath, false);
                        Body? body = doc.MainDocumentPart?.Document.Body;
                        if (body is not null)
                        {
                            foreach (Text text in body.Descendants<Text>())
                            {
                                stringBuilder.Append(text.Text);
                            }
                        }
                        fileTextContent = stringBuilder.ToString();
                    }
                    finally
                    {
                        PooledStringBuilder.Instance.Return(stringBuilder);
                    }
                    break;
                }
        }

        return fileTextContent;
    }
}
