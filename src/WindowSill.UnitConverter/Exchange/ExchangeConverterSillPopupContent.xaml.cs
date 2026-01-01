using CommunityToolkit.Diagnostics;

using WindowSill.API;

namespace WindowSill.UnitConverter.Exchange;

public sealed partial class ExchangeConverterSillPopupContent : SillPopupContent
{
    internal ExchangeConverterSillPopupContent(ExchangeConverterViewModel viewModel)
    {
        DefaultStyleKey = typeof(ExchangeConverterSillPopupContent);

        ViewModel = viewModel;
        InitializeComponent();
    }

    internal ExchangeConverterViewModel ViewModel { get; }

    private void SillPopupContent_Opening(object sender, EventArgs e)
    {
        SearchBox.Focus(FocusState.Keyboard);
    }

    private void SearchBoxFocusShortcut_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        SearchBox.Focus(FocusState.Keyboard);
    }

    private void ItemTapped(object sender, TappedRoutedEventArgs e)
    {
        Guard.IsOfType<Grid>(sender);
        if (sender is Grid grid)
        {
            if (grid.DataContext is CurrencyValue currencyValue)
            {
                ViewModel.CopyCommand.Execute(currencyValue.FormattedValue);
            }
        }
    }
}
