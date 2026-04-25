using System.Collections.ObjectModel;

using WindowSill.API;
using WindowSill.Date.Core.UI;

namespace WindowSill.Date.Views;

/// <summary>
/// Base class for per-DateSill-instance adapters that manage sill items in a
/// <see cref="ViewList"/>. Provides shared infrastructure:
/// <list type="bullet">
///   <item>Self-managed orientation updates via <c>SettingsProvider.SettingChanged</c>,
///         dispatched to the UI thread — works even when the visual container is
///         permanently Unloaded (a known WinUI lifecycle quirk).</item>
///   <item>Insert-index clamping for safe <see cref="ObservableCollection{T}.Insert"/>.</item>
///   <item>Disposed-guard pattern.</item>
/// </list>
/// </summary>
internal abstract class SillViewListAdapterBase : IDisposable
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly ObservableCollection<SillListViewItem> _viewList;
    private bool _disposed;

    protected SillViewListAdapterBase(
        ISettingsProvider settingsProvider,
        ObservableCollection<SillListViewItem> viewList)
    {
        _settingsProvider = settingsProvider;
        _viewList = viewList;

        _settingsProvider.SettingChanged += OnSettingChanged;
    }

    protected ISettingsProvider SettingsProvider => _settingsProvider;

    protected ObservableCollection<SillListViewItem> ViewList => _viewList;

    protected bool Disposed => _disposed;

    /// <summary>
    /// Returns all bar-content controls owned by this adapter so the base can
    /// apply orientation state updates to them.
    /// </summary>
    protected abstract IEnumerable<Control> GetBarContents();

    /// <summary>
    /// Called once during <see cref="Dispose"/> before the base unsubscribes from
    /// settings. Subclasses should unsubscribe from their own events and clean up
    /// entries here.
    /// </summary>
    protected abstract void OnDisposing();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        OnDisposing();
        _settingsProvider.SettingChanged -= OnSettingChanged;
    }

    /// <summary>
    /// Computes the current <see cref="SillOrientationAndSize"/> from settings.
    /// </summary>
    protected SillOrientationAndSize ComputeCurrentOrientation()
        => SillOrientationHelper.ComputeOrientationAndSize(_settingsProvider);

    /// <summary>
    /// Clamps a requested insert index into the valid <c>[0, ViewList.Count]</c> range
    /// so a stale or missing resolver can never throw.
    /// </summary>
    protected int ClampInsertIndex(int requested)
    {
        if (requested < 0 || requested > _viewList.Count)
        {
            return _viewList.Count;
        }

        return requested;
    }

    /// <summary>
    /// Handles <c>SettingsProvider.SettingChanged</c> for sill size/location changes.
    /// Dispatches orientation updates to the UI thread since this event may fire
    /// from any thread.
    /// </summary>
    private void OnSettingChanged(ISettingsProvider sender, SettingChangedEventArgs args)
    {
        if (_disposed)
        {
            return;
        }

        if (args.SettingName != PredefinedSettings.SillSize.Name
            && args.SettingName != PredefinedSettings.SillLocation.Name)
        {
            return;
        }

        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            if (_disposed)
            {
                return;
            }

            SillOrientationAndSize newState = ComputeCurrentOrientation();

            foreach (Control barContent in GetBarContents())
            {
                barContent.ApplyOrientationState(newState);
            }
        }).ForgetSafely();
    }
}
