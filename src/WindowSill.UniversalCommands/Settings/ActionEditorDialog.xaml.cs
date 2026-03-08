using Windows.System;
using WindowSill.API;
using WindowSill.UniversalCommands.Core;
using WindowSill.UniversalCommands.ViewModels;

namespace WindowSill.UniversalCommands.Settings;

/// <summary>
/// A user control that hosts the action editor form content.
/// Displayed inside a <see cref="ContentDialog"/> created by the static factory methods.
/// </summary>
public sealed partial class ActionEditorDialog : UserControl
{
    /// <summary>
    /// Shows a dialog for creating a new action.
    /// </summary>
    /// <param name="xamlRoot">The XAML root used to host the dialog.</param>
    /// <returns>The new command, or <see langword="null"/> if the dialog was cancelled.</returns>
    internal static async Task<UniversalCommand?> NewActionAsync(XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = "/WindowSill.UniversalCommands/SettingsView/NewCommand".GetLocalizedString(),
            PrimaryButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style,
            PrimaryButtonText = "/WindowSill.UniversalCommands/SettingsView/Save".GetLocalizedString(),
            CloseButtonText = "/WindowSill.UniversalCommands/SettingsView/Cancel".GetLocalizedString(),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };

        var viewModel = new ActionEditorDialogViewModel(dialog, new UniversalCommand());
        dialog.Content = new ActionEditorDialog(viewModel);

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return viewModel.ToUniversalCommand();
        }

        return null;
    }

    /// <summary>
    /// Shows a dialog for editing an existing action.
    /// </summary>
    /// <param name="xamlRoot">The XAML root used to host the dialog.</param>
    /// <param name="command">The command to edit.</param>
    /// <returns>The edited command, or <see langword="null"/> if the dialog was cancelled.</returns>
    internal static async Task<UniversalCommand?> EditActionAsync(XamlRoot xamlRoot, UniversalCommand command)
    {
        var dialog = new ContentDialog
        {
            Title = "/WindowSill.UniversalCommands/SettingsView/EditCommand".GetLocalizedString(),
            PrimaryButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style,
            PrimaryButtonText = "/WindowSill.UniversalCommands/SettingsView/Save".GetLocalizedString(),
            CloseButtonText = "/WindowSill.UniversalCommands/SettingsView/Cancel".GetLocalizedString(),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };

        var viewModel = new ActionEditorDialogViewModel(dialog, command);
        dialog.Content = new ActionEditorDialog(viewModel);

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return viewModel.ToUniversalCommand();
        }

        return null;
    }

    private ActionEditorDialog(ActionEditorDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        if (ViewModel.RecordedChordInts.Count > 0)
        {
            ShortcutRecorder.RecordedChord = ViewModel.RecordedChordInts
                .Select(combo => combo.Select(k => (VirtualKey)k).ToList()).ToList();
        }

        ShortcutRecorder.ShortcutChanged += OnShortcutChanged;
    }

    internal ActionEditorDialogViewModel ViewModel { get; }

    private void OnShortcutChanged(object? sender, EventArgs e)
    {
        var chord = ShortcutRecorder.RecordedChord.Select(combo => combo.Select(k => (int)k).ToList()).ToList();
        ViewModel.UpdateRecordedChord(chord);
    }

    private void OnGlyphFlyoutOpened(object sender, object e)
    {
        if (ViewModel.SelectedGlyphIndex >= 0 && ViewModel.Glyphs is not null)
        {
            GlyphGridView.ScrollIntoView(ViewModel.Glyphs[ViewModel.SelectedGlyphIndex]);
        }
    }

    private void OnGlyphSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0)
        {
            GlyphFlyout.Hide();
        }
    }
}
