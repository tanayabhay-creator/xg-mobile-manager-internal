namespace AllyConsole.App.Windows;

internal sealed class TrayHost : IDisposable
{
    private readonly System.Windows.Forms.NotifyIcon _icon;

    public TrayHost(Action open, Action exit)
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open quick panel", null, (_, _) => open());
        menu.Items.Add("Exit Ally Console", null, (_, _) => exit());
        _icon = new System.Windows.Forms.NotifyIcon
        {
            Icon = (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone(),
            Visible = true,
            Text = "Ally Console",
            ContextMenuStrip = menu
        };
        _icon.DoubleClick += (_, _) => open();
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
