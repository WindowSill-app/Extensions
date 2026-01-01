using WindowSill.API;

namespace WindowSill.Teams.UI;

internal static class LeaveCallSillListViewButtonItem
{
    internal static SillListViewButtonItem Create(IPluginInfo pluginInfo, Func<Task> onClickTask)
    {
        StackPanel content = new StackPanel()
            .Spacing(8)
            .Orientation(Orientation.Horizontal)
            .Children(
                new Image()
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Width(x => x.ThemeResource("SillIconSize"))
                    .Height(x => x.ThemeResource("SillIconSize"))
                    .Stretch(Stretch.Uniform)
                    .Source(new Uri(System.IO.Path.Combine(pluginInfo.GetPluginContentDirectory(), "Assets", "leave_call.svg"))));

        return new SillListViewButtonItem(content, "/WindowSill.Teams/Misc/Leave".GetLocalizedString(), onClickTask)
            .Background("#D13438")
            .Padding(x => x.ThemeResource("SillCommandContentMargin"));
    }
}
