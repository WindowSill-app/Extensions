using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using WindowSill.API;
using WindowSill.FileHelper.Core;
using WindowSill.FileHelper.Services;

namespace WindowSill.FileHelper.ViewModels;

/// <summary>
/// ViewModel for the file actions popup, managing which actions are offered for the current selection, starting the
/// chosen one, and tracking its queue through progress and results.
/// </summary>
/// <remarks>
/// The same view model backs both experiences — converting documents and acting on PDFs — because the popup is the
/// same shape in each case: a list of actions for the current selection. Which actions those are is decided by
/// <see cref="ConversionCatalog"/> or <see cref="PdfActionCatalog"/> and baked into the buttons by the factory
/// methods below, so the view never needs to know which kind of selection it is showing. Actions that need the user
/// to choose something first (which pages, which order) raise <see cref="ConfigurationRequested"/> instead of
/// starting straight away.
/// </remarks>
internal sealed partial class ConvertDocumentPopupViewModel : ObservableObject
{
    private readonly IFileOperationService _service;

    private ConvertDocumentPopupViewModel(IReadOnlyList<IStorageFile> files, IFileOperationService service)
    {
        Files = files;
        _service = service;
        Actions = [];
    }

    /// <summary>
    /// Creates a view model offering the formats <paramref name="sourceFormat"/> can be converted to.
    /// </summary>
    /// <param name="files">The selected files, all of <paramref name="sourceFormat"/> format.</param>
    /// <param name="sourceFormat">The format the selected files are in.</param>
    /// <param name="service">The file operation service.</param>
    internal static ConvertDocumentPopupViewModel ForConversion(
        IReadOnlyList<IStorageFile> files,
        DocumentFileFormat sourceFormat,
        IFileOperationService service)
    {
        var viewModel = new ConvertDocumentPopupViewModel(files, service);

        viewModel.Actions =
        [
            .. ConversionCatalog.GetTargets(sourceFormat).Select(target =>
                new FileActionViewModel(
                    target.DisplayName,
                    $"ConvertTo{target.Format}",
                    new RelayCommand(() => viewModel.Start(() => service.StartConversion(files, sourceFormat, target.Format)))))
        ];

        return viewModel;
    }

    /// <summary>
    /// Creates a view model offering the PDF actions that apply to the selection.
    /// </summary>
    /// <param name="files">The selected PDFs, in selection order.</param>
    /// <param name="service">The file operation service.</param>
    internal static ConvertDocumentPopupViewModel ForPdfActions(
        IReadOnlyList<IStorageFile> files,
        IFileOperationService service)
    {
        var viewModel = new ConvertDocumentPopupViewModel(files, service);

        viewModel.Actions =
        [
            .. PdfActionCatalog.GetActions(files.Count).Select(action =>
                new FileActionViewModel(
                    action.DisplayName,
                    $"PdfAction{action.Action}",
                    new RelayCommand(() => viewModel.InvokePdfAction(action))))
        ];

        return viewModel;
    }

    /// <summary>
    /// Creates a view model offering the encoding and line-ending adjustments for a text selection.
    /// </summary>
    /// <param name="files">The selected text files.</param>
    /// <param name="service">The file operation service.</param>
    internal static ConvertDocumentPopupViewModel ForTextActions(
        IReadOnlyList<IStorageFile> files,
        IFileOperationService service)
    {
        var viewModel = new ConvertDocumentPopupViewModel(files, service);

        viewModel.Actions =
        [
            .. TextActionCatalog.GetActions(files.Count).Select(action =>
                new FileActionViewModel(
                    action.DisplayName,
                    $"TextAction{action.Action}",
                    new RelayCommand(() => viewModel.Start(() => service.StartTextAction(files, action.Action)))))
        ];

        return viewModel;
    }

    /// <summary>
    /// Creates a view model that only displays an already-running or finished queue, with no actions to start.
    /// </summary>
    /// <param name="queue">The queue to display.</param>
    /// <param name="service">The file operation service.</param>
    internal static ConvertDocumentPopupViewModel ForQueue(FileOperationQueue queue, IFileOperationService service)
        => new([], service) { Queue = queue };

    /// <summary>
    /// Gets the files the offered actions apply to.
    /// </summary>
    internal IReadOnlyList<IStorageFile> Files { get; }

    /// <summary>
    /// Gets the actions offered for the current selection, in presentation order.
    /// </summary>
    public IReadOnlyList<FileActionViewModel> Actions { get; private set; }

    /// <summary>
    /// Gets a short description of what the actions will be applied to, e.g. "meeting.txt" or "3 files selected",
    /// so the popup says what is about to be acted on rather than presenting unlabelled choices.
    /// </summary>
    public string SelectionSummary
        => Files.Count switch
        {
            0 => string.Empty,
            1 => System.IO.Path.GetFileName(Files[0].Path),
            _ => string.Format("/WindowSill.FileHelper/ConvertDocument/SelectionSummaryMany".GetLocalizedString(), Files.Count),
        };

    /// <summary>
    /// Gets or sets the running queue. Null when no action has been started yet.
    /// </summary>
    [ObservableProperty]
    public partial FileOperationQueue? Queue { get; set; }

    /// <summary>
    /// Raised when the chosen action needs the user to configure it first, so the popup can navigate to the
    /// matching configuration page.
    /// </summary>
    internal event EventHandler<PdfAction>? ConfigurationRequested;

    /// <summary>
    /// Raised when an action has started and the UI should navigate to the progress page.
    /// </summary>
    internal event EventHandler? ConversionStarted;

    /// <summary>
    /// Raised when the queue has completed and the UI should navigate to the result page.
    /// </summary>
    internal event EventHandler? ConversionCompleted;

    /// <summary>
    /// Starts merging the selected PDFs in the order the user arranged them.
    /// </summary>
    /// <param name="orderedPaths">The PDFs to merge, in output order.</param>
    internal void StartMerge(IReadOnlyList<string> orderedPaths)
        => Start(() => _service.StartPdfMerge(orderedPaths));

    /// <summary>
    /// Starts copying the pages the user picked out of a PDF.
    /// </summary>
    /// <param name="sourcePath">The PDF to take pages from.</param>
    /// <param name="pageIndices">Zero-based indices of the pages to copy, in output order.</param>
    internal void StartExtract(string sourcePath, IReadOnlyList<int> pageIndices)
        => Start(() => _service.StartPdfExtract(sourcePath, pageIndices));

    /// <summary>
    /// Starts adding or removing a PDF's open password.
    /// </summary>
    /// <param name="sourcePath">The PDF to protect or unlock.</param>
    /// <param name="password">The password to apply, or the current password when unlocking.</param>
    /// <param name="protect"><see langword="true"/> to add a password; <see langword="false"/> to remove it.</param>
    internal void StartPdfPassword(string sourcePath, string password, bool protect)
        => Start(() => _service.StartPdfPassword(sourcePath, password, protect));

    /// <summary>
    /// Opens the output folder in File Explorer.
    /// </summary>
    [RelayCommand]
    private void OpenOutputFolder()
    {
        if (Queue is null)
        {
            return;
        }

        IReadOnlyList<string> outputPaths = Queue.OutputPaths;
        if (outputPaths.Count == 0)
        {
            return;
        }

        string? directory = System.IO.Path.GetDirectoryName(outputPaths[0]);
        if (directory is not null && Directory.Exists(directory))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true,
            });
        }
    }

    /// <summary>
    /// Cancels the current queue.
    /// </summary>
    [RelayCommand]
    private void CancelConversion()
    {
        Queue?.Cancel();
    }

    /// <summary>
    /// Subscribes to queue state changes to detect completion.
    /// Called by the popup code-behind after navigation.
    /// </summary>
    internal void ObserveQueueCompletion()
    {
        if (Queue is not null)
        {
            Queue.PropertyChanged += Queue_PropertyChanged;
        }
    }

    /// <summary>
    /// Unsubscribes from queue events. Called when the popup closes.
    /// </summary>
    internal void StopObservingQueue()
    {
        if (Queue is not null)
        {
            Queue.PropertyChanged -= Queue_PropertyChanged;
        }
    }

    /// <summary>
    /// Resets the ViewModel so the user can start a fresh action.
    /// Called when the original popup is reopened after a previous queue ran.
    /// </summary>
    internal void Reset()
    {
        StopObservingQueue();
        Queue = null;
    }

    private void InvokePdfAction(PdfActionInfo action)
    {
        if (action.RequiresConfiguration)
        {
            ConfigurationRequested?.Invoke(this, action.Action);
            return;
        }

        Start(() => _service.StartPdfAction(Files, action.Action));
    }

    private void Start(Func<FileOperationQueue> startQueue)
    {
        Queue = startQueue();
        ObserveQueueCompletion();
        ConversionStarted?.Invoke(this, EventArgs.Empty);
    }

    private void Queue_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileOperationQueue.State)
            && Queue?.State is FileOperationQueueState.Completed or FileOperationQueueState.Failed or FileOperationQueueState.Canceled)
        {
            ConversionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
