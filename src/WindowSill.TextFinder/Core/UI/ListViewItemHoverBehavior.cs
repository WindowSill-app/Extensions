namespace WindowSill.TextFinder.Core.UI;

/// <summary>
/// Attached behavior that shows/hides elements marked with <see cref="RevealOnHoverOrSelectionProperty"/>
/// inside ListView items when the pointer hovers over the ListViewItem or when the item is selected.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// &lt;ListView ui:ListViewItemHoverBehavior.IsEnabled="True"&gt;
///     &lt;ListView.ItemTemplate&gt;
///         &lt;DataTemplate&gt;
///             &lt;Grid&gt;
///                 &lt;TextBlock Text="{x:Bind Name}" /&gt;
///                 &lt;StackPanel ui:ListViewItemHoverBehavior.RevealOnHoverOrSelection="True" Visibility="Collapsed"&gt;
///                     &lt;Button Content="Edit" /&gt;
///                 &lt;/StackPanel&gt;
///             &lt;/Grid&gt;
///         &lt;/DataTemplate&gt;
///     &lt;/ListView.ItemTemplate&gt;
/// &lt;/ListView&gt;
/// </code>
/// </remarks>
public static class ListViewItemHoverBehavior
{
    private static FrameworkElement? hoveredItemContent;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ListViewItemHoverBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    public static readonly DependencyProperty RevealOnHoverOrSelectionProperty =
        DependencyProperty.RegisterAttached(
            "RevealOnHoverOrSelection",
            typeof(bool),
            typeof(ListViewItemHoverBehavior),
            new PropertyMetadata(false));

    public static bool GetRevealOnHoverOrSelection(DependencyObject obj) =>
        (bool)obj.GetValue(RevealOnHoverOrSelectionProperty);

    public static void SetRevealOnHoverOrSelection(DependencyObject obj, bool value) =>
        obj.SetValue(RevealOnHoverOrSelectionProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListView listView)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            listView.ContainerContentChanging += OnContainerContentChanging;
            listView.SelectionChanged += OnSelectionChanged;
        }
        else
        {
            listView.ContainerContentChanging -= OnContainerContentChanging;
            listView.SelectionChanged -= OnSelectionChanged;
        }
    }

    private static void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer is not ListViewItem container)
        {
            return;
        }

        container.PointerEntered -= OnPointerEntered;
        container.PointerExited -= OnPointerExited;

        if (!args.InRecycleQueue)
        {
            container.PointerEntered += OnPointerEntered;
            container.PointerExited += OnPointerExited;
        }
    }

    private static void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not ListViewItem container)
        {
            return;
        }

        hoveredItemContent = FindDataTemplateRoot(container);
        SetRevealElementsVisibility(container, Visibility.Visible);
    }

    private static void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not ListViewItem container)
        {
            return;
        }

        hoveredItemContent = null;

        if (!container.IsSelected)
        {
            SetRevealElementsVisibility(container, Visibility.Collapsed);
        }
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListView listView)
        {
            return;
        }

        foreach (object? removed in e.RemovedItems)
        {
            if (listView.ContainerFromItem(removed) is ListViewItem oldContainer)
            {
                FrameworkElement? oldContent = FindDataTemplateRoot(oldContainer);
                if (oldContent != hoveredItemContent)
                {
                    SetRevealElementsVisibility(oldContainer, Visibility.Collapsed);
                }
            }
        }

        foreach (object? added in e.AddedItems)
        {
            if (listView.ContainerFromItem(added) is ListViewItem newContainer)
            {
                SetRevealElementsVisibility(newContainer, Visibility.Visible);
            }
        }
    }

    private static void SetRevealElementsVisibility(DependencyObject parent, Visibility visibility)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            if (child is UIElement element && GetRevealOnHoverOrSelection(child))
            {
                element.Visibility = visibility;
            }

            SetRevealElementsVisibility(child, visibility);
        }
    }

    private static FrameworkElement? FindDataTemplateRoot(ListViewItem container)
    {
        return FindChild<Grid>(container) ?? FindChild<StackPanel>(container) as FrameworkElement;
    }

    private static T? FindChild<T>(DependencyObject parent) where T : FrameworkElement
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            T? result = FindChild<T>(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}
