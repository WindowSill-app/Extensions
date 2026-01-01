using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Windows.ApplicationModel.DataTransfer;

namespace WindowSill.UnitConverter;

internal abstract partial class ViewModelBase : ObservableObject
{
    [RelayCommand]
    private void Copy(string value)
    {
        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Move };
        dataPackage.SetText(value);

        Clipboard.SetContentWithOptions(
            dataPackage,
            new ClipboardContentOptions()
            {
                IsAllowedInHistory = true,
                IsRoamable = false
            });
    }
}
