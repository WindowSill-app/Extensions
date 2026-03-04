using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Markup;

namespace WindowSill.TextFinder.Core.UI;

[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_DeleteButton, Type = typeof(AdvancedTextBox))]
public sealed class AdvancedTextBox : TextBox
{
    private const string PART_DeleteButton = "DeleteButton";

    private Button? _deleteButton;

    public AdvancedTextBox()
    {
        DefaultStyleKey = typeof(AdvancedTextBox);
        TextChanged += AdvancedTextBox_TextChanged;
    }

    public object Content
    {
        get { return (object)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(AdvancedTextBox), new PropertyMetadata(null, OnContentChanged));

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (AdvancedTextBox)d;
        if (ctl != null)
        {
            ctl.UpdateCursor();
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _deleteButton = (Button)GetTemplateChild(PART_DeleteButton);
        _deleteButton.Visibility = string.IsNullOrEmpty(Text) ? Visibility.Collapsed : Visibility.Visible;
        _deleteButton.Click += DeleteButton_Click;
    }

    private void UpdateCursor()
    {
        if (Content != null)
        {
            if (Content is Button button)
            {
                GeneralHelper.ChangeCursor(button, InputSystemCursor.Create(InputSystemCursorShape.Arrow));
            }
            else if (Content is Panel panel)
            {
                foreach (UIElement? item in panel.Children)
                {
                    GeneralHelper.ChangeCursor(item, InputSystemCursor.Create(InputSystemCursorShape.Arrow));
                }
            }
        }
    }

    private void AdvancedTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_deleteButton is not null)
        {
            _deleteButton.Visibility = string.IsNullOrEmpty(Text) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        Text = string.Empty;
    }
}
