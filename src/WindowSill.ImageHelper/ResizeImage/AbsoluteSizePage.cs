using CommunityToolkit.Diagnostics;

using WindowSill.API;

namespace WindowSill.ImageHelper.ResizeImage;

public sealed class AbsoluteSizePage : XamlLessPage
{
    public AbsoluteSizePage()
    {
        this.Content(
            new Grid()
                .RowSpacing(8)
                .ColumnSpacing(8)
                .ColumnDefinitions(
                    new GridLength(1, GridUnitType.Star),
                    new GridLength(1, GridUnitType.Star)
                )
                .RowDefinitions(
                    GridLength.Auto,
                    GridLength.Auto
                )
                .Children(
                    new NumberBox()
                        .Grid(row: 0, column: 0)
                        .Minimum(1)
                        .Value(
                            x => x.Binding(() => ViewModel.Width)
                                  .TwoWay()
                                  .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged)
                        )
                        .Header("/WindowSill.ImageHelper/ResizeImage/Width".GetLocalizedString()),

                    new NumberBox()
                        .Grid(row: 0, column: 1)
                        .Minimum(1)
                        .Value(
                            x => x.Binding(() => ViewModel.Height)
                                  .TwoWay()
                                  .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged)
                        )
                        .Header("/WindowSill.ImageHelper/ResizeImage/Height".GetLocalizedString()),

                    new CheckBox()
                        .Grid(row: 1, columnSpan: 2)
                        .IsChecked(
                            x => x.Binding(() => ViewModel.MaintainAspectRatio)
                                  .TwoWay()
                                  .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged)
                        )
                        .Content("/WindowSill.ImageHelper/ResizeImage/AspectRatio".GetLocalizedString())
                ));
    }

    internal ResizeImageViewModel ViewModel => (ResizeImageViewModel)DataContext;

    protected override void OnNavigatedTo(XamlLessNavigationEventArgs e)
    {
        Guard.IsNotNull(e.Parameter);
        Guard.IsOfType<ResizeImageViewModel>(e.Parameter);
        DataContext = (ResizeImageViewModel)e.Parameter;
    }
}
