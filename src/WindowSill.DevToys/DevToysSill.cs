using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.DevToys.Activators;
using WindowSill.DevToys.ViewModels;
using Path = System.IO.Path;

namespace WindowSill.DevToys;

[Export(typeof(ISill))]
[Name("DevToys")]
internal sealed class DevToysSill : ISillActivatedByTextSelection, ISillListView
{
    private readonly JsonFormatterViewModel _jsonFormatterViewModel = new();
    private readonly JsonSortViewModel _jsonSortViewModel = new();
    private readonly JsonConvertViewModel _jsonConvertViewModel = new();
    private readonly UnescapeViewModel _unescapeViewModel = new();
    private readonly Base64DecodeViewModel _base64DecodeViewModel = new();
    private readonly XmlFormatterViewModel _xmlFormatterViewModel = new();

    [Import]
    private IPluginInfo _pluginInfo = null!;

    public string[] TextSelectionActivatorTypeNames { get; }
        = [
            Base64ImageSelection.ActivationTypeName,
            Base64TextSelection.ActivationTypeName,
            EscapedCharactersSelection.ActivationTypeName,
            PredefinedActivationTypeNames.JsonSelection,
            JsonArraySelection.ActivationTypeName,
            XmlSelection.ActivationTypeName,
            XsdSelection.ActivationTypeName
        ];

    public string DisplayName => "/WindowSill.DevToys/Misc/DisplayName".GetLocalizedString();

    public SillSettingsView[]? SettingsViews => throw new NotImplementedException();

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => throw new NotImplementedException();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "devtoys.svg")))
        };

    public async ValueTask OnActivatedAsync(string textSelectionActivatorTypeName, WindowTextSelection currentSelection)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ViewList.Clear();

            switch (textSelectionActivatorTypeName)
            {
                case PredefinedActivationTypeNames.JsonSelection:
                case JsonArraySelection.ActivationTypeName:
                    if (!currentSelection.IsReadOnly)
                    {
                        ViewList.Add(_jsonFormatterViewModel.GetView(currentSelection));
                        ViewList.Add(_jsonSortViewModel.GetView(currentSelection));
                        ViewList.Add(_jsonConvertViewModel.GetView(currentSelection));
                    }
                    break;

                case EscapedCharactersSelection.ActivationTypeName:
                    if (!currentSelection.IsReadOnly)
                    {
                        ViewList.Add(_unescapeViewModel.GetView(currentSelection));
                    }
                    break;

                case Base64TextSelection.ActivationTypeName:
                    if (!currentSelection.IsReadOnly)
                    {
                        ViewList.Add(_base64DecodeViewModel.GetView(currentSelection));
                    }
                    break;

                case XmlSelection.ActivationTypeName:
                    if (!currentSelection.IsReadOnly)
                    {
                        ViewList.Add(_xmlFormatterViewModel.GetView(currentSelection));
                    }
                    break;
            }
        });
    }

    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(ViewList.Clear);
    }
}
