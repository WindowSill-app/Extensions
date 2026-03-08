using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.System;
using WindowSill.API;

namespace WindowSill.UniversalCommands.Core.UI;

public sealed partial class ShortcutRecorderControl : UserControl
{
    private readonly List<List<VirtualKey>> _chord = [];
    private readonly List<VirtualKey> _currentCombo = [];
    private readonly ObservableCollection<string> _displayTokens = [];
    private bool _isRecording;
    private bool _waitingForNextCombo;

    public ShortcutRecorderControl()
    {
        InitializeComponent();
        KeysRepeater.ItemsSource = _displayTokens;
        Placeholder = "/WindowSill.UniversalCommands/ShortcutRecorderControl/PlaceholderDefault".GetLocalizedString();
    }

    public static readonly DependencyProperty PlaceholderProperty
        = DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(ShortcutRecorderControl),
            new PropertyMetadata(string.Empty));

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty) as string;
        set => SetValue(PlaceholderProperty, value);
    }

    internal List<List<VirtualKey>> RecordedChord
    {
        get => _chord.Select(combo => combo.ToList()).ToList();
        set
        {
            _chord.Clear();
            _chord.AddRange(value.Select(combo => combo.ToList()));
            UpdateDisplay();
        }
    }

    internal event EventHandler? ShortcutChanged;

    private void OnGridPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;
        Focus(FocusState.Programmatic);
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        StartRecording();
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        FinalizeChord();
    }

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!_isRecording)
        {
            return;
        }

        e.Handled = true;

        // Escape cancels recording and clears.
        if (e.Key == VirtualKey.Escape)
        {
            _chord.Clear();
            _currentCombo.Clear();
            FinalizeChord();
            return;
        }

        // If we were waiting for the next combo, a new keypress starts it.
        if (_waitingForNextCombo)
        {
            _waitingForNextCombo = false;
            _currentCombo.Clear();
        }

        VirtualKey key = KeyboardHelper.NormalizeModifier(e.Key);

        if (!_currentCombo.Contains(key))
        {
            if (KeyboardHelper.IsModifierKey(key))
            {
                int lastModifierIndex = _currentCombo.FindLastIndex(KeyboardHelper.IsModifierKey);
                _currentCombo.Insert(lastModifierIndex + 1, key);
            }
            else
            {
                _currentCombo.Add(key);
            }

            UpdateLiveDisplay();
        }
    }

    private void OnPreviewKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (!_isRecording)
        {
            return;
        }

        e.Handled = true;
        VirtualKey key = KeyboardHelper.NormalizeModifier(e.Key);

        // When a non-modifier key is released, commit the current combo and wait for next.
        if (!KeyboardHelper.IsModifierKey(key) && _currentCombo.Count > 0)
        {
            _chord.Add([.. _currentCombo]);
            _currentCombo.Clear();
            _waitingForNextCombo = true;
            UpdateDisplay();

            ShortcutChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnClearClicked(object sender, RoutedEventArgs e)
    {
        _chord.Clear();
        _currentCombo.Clear();
        UpdateDisplay();
        ShortcutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void StartRecording()
    {
        _isRecording = true;
        _waitingForNextCombo = false;
        _chord.Clear();
        _currentCombo.Clear();
        _displayTokens.Clear();

        Placeholder = "/WindowSill.UniversalCommands/ShortcutRecorderControl/PlaceholderRecording".GetLocalizedString();
        PlaceholderText.Visibility = Visibility.Visible;
        ClearButton.Visibility = Visibility.Collapsed;

        RecorderGrid.BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        RecorderGrid.Background = (Brush)Application.Current.Resources["ControlFillColorInputActiveBrush"];
        RecorderGrid.BorderThickness = new Thickness(0, 0, 0, 2);
    }

    private void FinalizeChord()
    {
        _isRecording = false;
        _waitingForNextCombo = false;

        // If there's an uncommitted combo, commit it.
        if (_currentCombo.Count > 0)
        {
            _chord.Add([.. _currentCombo]);
            _currentCombo.Clear();
        }

        RecorderGrid.BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"];
        RecorderGrid.Background = (Brush)Application.Current.Resources["ControlFillColorDefaultBrush"];
        RecorderGrid.BorderThickness = new Thickness(1);

        UpdateDisplay();
        ShortcutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateLiveDisplay()
    {
        _displayTokens.Clear();

        for (int i = 0; i < _chord.Count; i++)
        {
            if (i > 0)
            {
                _displayTokens.Add(string.Empty);
            }

            foreach (VirtualKey key in _chord[i])
            {
                _displayTokens.Add(KeyboardHelper.GetDisplayName(key));
            }
        }

        if (_currentCombo.Count > 0)
        {
            if (_chord.Count > 0)
            {
                _displayTokens.Add(string.Empty);
            }

            foreach (VirtualKey key in _currentCombo)
            {
                _displayTokens.Add(KeyboardHelper.GetDisplayName(key));
            }
        }

        PlaceholderText.Visibility
            = _displayTokens.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        ClearButton.Visibility = Visibility.Collapsed;
    }

    private void UpdateDisplay()
    {
        _displayTokens.Clear();

        for (int i = 0; i < _chord.Count; i++)
        {
            if (i > 0)
            {
                _displayTokens.Add(string.Empty);
            }

            foreach (VirtualKey key in _chord[i])
            {
                _displayTokens.Add(KeyboardHelper.GetDisplayName(key));
            }
        }

        PlaceholderText.Visibility = _chord.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        ClearButton.Visibility = _chord.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
