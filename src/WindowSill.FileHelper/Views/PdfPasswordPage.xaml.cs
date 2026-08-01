using WindowSill.FileHelper.ViewModels;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// Page asking for the password to apply to, or remove from, a PDF.
/// </summary>
internal sealed partial class PdfPasswordPage : Page
{
    internal PdfPasswordPage()
    {
        InitializeComponent();
    }

    internal PdfPasswordViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (PdfPasswordViewModel)e.Parameter;

        // Navigation supplies the view model after InitializeComponent() already ran the first x:Bind pass
        // against a null root, so the bindings need re-evaluating.
        Bindings.Update();

        PasswordEntry.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Mirrors the box into the view model. PasswordBox deliberately does not support two-way binding updates on
    /// every keystroke, so the confirm button would otherwise stay disabled until focus moved away.
    /// </summary>
    private void PasswordEntry_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.Password = PasswordEntry.Password;
        }
    }
}
