using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Selects the appropriate <see cref="DataTemplate"/> for a clipboard history item
/// based on its ViewModel type. Used by the compact mode popup ListView.
/// </summary>
internal sealed class ClipboardPopupItemTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template for text-based items.
    /// </summary>
    public DataTemplate? TextTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for HTML items.
    /// </summary>
    public DataTemplate? HtmlTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for RTF items.
    /// </summary>
    public DataTemplate? RtfTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for image items.
    /// </summary>
    public DataTemplate? ImageTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for URI/web link items.
    /// </summary>
    public DataTemplate? UriTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for application link items.
    /// </summary>
    public DataTemplate? AppLinkTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for color items.
    /// </summary>
    public DataTemplate? ColorTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for file/folder items.
    /// </summary>
    public DataTemplate? FileTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for user activity items.
    /// </summary>
    public DataTemplate? UserActivityTemplate { get; set; }

    /// <summary>
    /// Gets or sets the default template for unknown or unsupported item types.
    /// </summary>
    public DataTemplate? DefaultTemplate { get; set; }

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch
        {
            TextItemViewModel => TextTemplate,
            HtmlItemViewModel => HtmlTemplate,
            RtfItemViewModel => RtfTemplate,
            ImageItemViewModel => ImageTemplate,
            UriItemViewModel => UriTemplate,
            ApplicationLinkItemViewModel => AppLinkTemplate,
            ColorItemViewModel => ColorTemplate,
            FileItemViewModel => FileTemplate,
            UserActivityItemViewModel => UserActivityTemplate,
            _ => DefaultTemplate,
        };
    }

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
