using CommunityToolkit.Diagnostics;

using WindowSill.API;

namespace WindowSill.ImageHelper.ResizeImage;

internal sealed class PercentagePage : XamlLessPage
{
    public PercentagePage()
    {
        this.Content(
            new Slider()
                .Minimum(1)
                .Maximum(200)
                .SmallChange(10)
                .VerticalAlignment(VerticalAlignment.Top)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Value(
                    x => x.Binding(() => ViewModel.Percentage)
                          .TwoWay()
                          .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged)));
    }

    internal ResizeImageViewModel ViewModel => (ResizeImageViewModel)DataContext;

    protected override void OnNavigatedTo(XamlLessNavigationEventArgs e)
    {
        Guard.IsNotNull(e.Parameter);
        Guard.IsOfType<ResizeImageViewModel>(e.Parameter);
        DataContext = (ResizeImageViewModel)e.Parameter;
    }
}
