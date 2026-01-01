using System.Net.Http.Headers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;

namespace WindowSill.URLHelper;

internal sealed partial class ShortenURLSillPopup : ObservableObject
{
    // This is a public API key for the Short.io service, using Free Tier. Anyone can use it to shorten URLs, but
    // you won't be able to generate other links than using the domain `windowsill.short.gy`.
    // TODO: If this feature gets popular, consider using a private API key and include this feature
    // in WindowSill+ since we'd have to pay for the Short.io service (or whatever other service we choose).
    private const string ShortIoApiKey = "pk_uA5OzJEpoXz0NCGW";

    private readonly WindowTextSelection _currentSelection;

    private bool _alreadyGenerated;

    private ShortenURLSillPopup(WindowTextSelection currentSelection)
    {
        _currentSelection = currentSelection;
    }

    internal static SillPopupContent CreateView(WindowTextSelection currentSelection)
    {
        var shortenURLSillPopupViewModel = new ShortenURLSillPopup(currentSelection);
        return new SillPopupContent(shortenURLSillPopupViewModel.OnOpening)
            .Width(350)
            .DataContext(
                shortenURLSillPopupViewModel,
                (view, viewModel) => view
                .Content(
                    new Grid()
                        .Padding(8)
                        .ColumnSpacing(8)
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .ColumnDefinitions(
                            new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) },
                            new ColumnDefinition() { Width = GridLength.Auto }
                        )
                        .Children(
                            new TextBox()
                                .Grid(column: 0)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .IsReadOnly(true)
                                .IsEnabled(x => x.Binding(() => viewModel.GeneratedUrl).Convert(url => !string.IsNullOrEmpty(url)))
                                .PlaceholderText("/WindowSill.URLHelper/ShortenURL/UrlTextBoxPlaceholderText".GetLocalizedString())
                                .Text(() => viewModel.GeneratedUrl),
                            new Button()
                                .Grid(column: 1)
                                .Style(x => x.ThemeResource("AccentButtonStyle"))
                                .VerticalAlignment(VerticalAlignment.Stretch)
                                .IsEnabled(x => x.Binding(() => viewModel.GeneratedUrl).Convert(url => !string.IsNullOrEmpty(url)))
                                .MinWidth(64)
                                .ToolTipService(toolTip: "/WindowSill.URLHelper/ShortenURL/CopyButtonToolTip".GetLocalizedString())
                                .Command(() => viewModel.CopyCommand)
                                .Content(
                                    new Grid()
                                        .Children(
                                            new ProgressRing()
                                                .IsIndeterminate(x => x.Binding(() => viewModel.GeneratedUrl).Convert(url => string.IsNullOrEmpty(url)))
                                                .Height(16)
                                                .Width(16),
                                            new TextBlock()
                                                .Visibility(x => x.Binding(() => viewModel.GeneratedUrl).Convert(url => string.IsNullOrEmpty(url) ? Visibility.Collapsed : Visibility.Visible))
                                                .Text("/WindowSill.URLHelper/ShortenURL/Copied".GetLocalizedString())
                                        )
                                )
                        )
                )
            );
    }

    [ObservableProperty]
    public partial string GeneratedUrl { get; set; } = string.Empty;

    [RelayCommand]
    private void Copy()
    {
        CopyUrl(GeneratedUrl);
    }

    private void OnOpening()
    {
        if (!_alreadyGenerated)
        {
            _alreadyGenerated = true;
            ShortenUrlAsync(_currentSelection.SelectedText).Forget();
        }
    }

    private async Task ShortenUrlAsync(string url)
    {
        string shortenedUrl = string.Empty;
        try
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.short.io/links/public"),
                Headers =
                {
                    { "accept", "application/json" },
                    { "Authorization", ShortIoApiKey },
                },
                Content = new StringContent("{\"skipQS\":false,\"allowDuplicates\":false,\"archived\":false,\"originalURL\":\"" + url + "\",\"domain\":\"windowsill.short.gy\"}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };

            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync();
            var json = System.Text.Json.JsonDocument.Parse(body);
            shortenedUrl = json.RootElement.GetProperty("secureShortURL").GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            this.Log().LogError(ex, "Error while trying to generate a shorter URL.");
        }
        finally
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                CopyUrl(shortenedUrl);
                GeneratedUrl = shortenedUrl;
            });
        }
    }

    private static void CopyUrl(string? url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(url);
            dataPackage.SetUri(new Uri(url));
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
        }
    }
}
