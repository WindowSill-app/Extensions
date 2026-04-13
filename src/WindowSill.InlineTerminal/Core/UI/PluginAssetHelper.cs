using Microsoft.UI.Xaml.Media.Imaging;

namespace WindowSill.InlineTerminal.Core.UI;

/// <summary>
/// Provides an attached property for resolving plugin-relative asset paths
/// to <see cref="SvgImageSource"/> on <see cref="ImageIcon"/> elements in XAML.
/// </summary>
public static class PluginAssetHelper
{
    /// <summary>
    /// Gets or sets the base directory for resolving relative asset paths.
    /// Must be set once during plugin initialization via <see cref="WindowSill.API.IPluginInfo.GetPluginContentDirectory"/>.
    /// </summary>
    internal static string? BaseDirectory { get; set; }

    /// <summary>
    /// Identifies the <c>Path</c> attached dependency property.
    /// </summary>
    public static readonly DependencyProperty PathProperty =
        DependencyProperty.RegisterAttached(
            "Path",
            typeof(string),
            typeof(PluginAssetHelper),
            new PropertyMetadata(null, OnPathChanged));

    /// <summary>
    /// Gets the relative asset path for the specified <see cref="ImageIcon"/>.
    /// </summary>
    /// <param name="obj">The target <see cref="ImageIcon"/>.</param>
    /// <returns>The relative asset path.</returns>
    public static string? GetPath(ImageIcon obj) => (string?)obj.GetValue(PathProperty);

    /// <summary>
    /// Sets the relative asset path for the specified <see cref="ImageIcon"/>,
    /// which will be resolved to an <see cref="SvgImageSource"/> using the plugin's base directory.
    /// </summary>
    /// <param name="obj">The target <see cref="ImageIcon"/>.</param>
    /// <param name="value">The relative asset path (e.g., "Assets/ok.svg").</param>
    public static void SetPath(ImageIcon obj, string? value) => obj.SetValue(PathProperty, value);

    private static void OnPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageIcon imageIcon && e.NewValue is string relativePath && BaseDirectory is not null)
        {
            string fullPath = System.IO.Path.Combine(BaseDirectory, relativePath);
            imageIcon.Source = new SvgImageSource(new Uri(fullPath));
        }
    }
}
