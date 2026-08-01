using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WindowSill.API;

namespace WindowSill.FileHelper.ViewModels;

/// <summary>
/// ViewModel for the PDF password page, used both to add a password and to remove an existing one.
/// </summary>
internal sealed partial class PdfPasswordViewModel : ObservableObject
{
    private readonly ConvertDocumentPopupViewModel _owner;
    private readonly string _sourcePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfPasswordViewModel"/> class.
    /// </summary>
    /// <param name="owner">The popup view model that starts the work once confirmed.</param>
    /// <param name="sourcePath">The PDF being protected or unlocked.</param>
    /// <param name="protect"><see langword="true"/> to add a password; <see langword="false"/> to remove one.</param>
    internal PdfPasswordViewModel(ConvertDocumentPopupViewModel owner, string sourcePath, bool protect)
    {
        _owner = owner;
        _sourcePath = sourcePath;
        IsProtecting = protect;
    }

    /// <summary>
    /// Gets a value indicating whether this page is adding a password rather than removing one.
    /// </summary>
    internal bool IsProtecting { get; }

    /// <summary>
    /// Gets or sets the password the user typed.
    /// </summary>
    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets the instruction shown above the password box.
    /// </summary>
    public string Hint
        => (IsProtecting
            ? "/WindowSill.FileHelper/PdfActions/ProtectHint"
            : "/WindowSill.FileHelper/PdfActions/UnlockHint").GetLocalizedString();

    /// <summary>
    /// Gets the confirm button caption.
    /// </summary>
    public string ConfirmText
        => (IsProtecting
            ? "/WindowSill.FileHelper/PdfActions/ProtectConfirm"
            : "/WindowSill.FileHelper/PdfActions/UnlockConfirm").GetLocalizedString();

    /// <summary>
    /// Gets a value indicating whether a password has been entered.
    /// </summary>
    public bool CanConfirm => !string.IsNullOrEmpty(Password);

    /// <summary>
    /// Applies the password change.
    /// </summary>
    [RelayCommand]
    private void Confirm()
    {
        if (!CanConfirm)
        {
            return;
        }

        _owner.StartPdfPassword(_sourcePath, Password, IsProtecting);
    }

    partial void OnPasswordChanged(string value)
    {
        OnPropertyChanged(nameof(CanConfirm));
        ConfirmCommand.NotifyCanExecuteChanged();
    }
}
