using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using TextBox = System.Windows.Controls.TextBox;
using AllyConsole.App.Hosting;
using AllyConsole.App.Windows;
using AllyConsole.Core.Navigation;

namespace AllyConsole.App;

public partial class MainWindow : Window
{
    private static readonly SolidColorBrush Ink = Freeze(MediaColor.FromRgb(0xF4, 0xF6, 0xFB));
    private static readonly SolidColorBrush Muted = Freeze(MediaColor.FromRgb(0xA8, 0xB0, 0xC0));
    private static readonly SolidColorBrush Card = Freeze(MediaColor.FromRgb(0x1C, 0x22, 0x30));
    private static readonly SolidColorBrush CardFocus = Freeze(MediaColor.FromRgb(0x32, 0x28, 0x18));
    private static readonly SolidColorBrush Line = Freeze(MediaColor.FromRgb(0x2C, 0x33, 0x44));
    private static readonly SolidColorBrush Amber = Freeze(MediaColor.FromRgb(0xE4, 0xA1, 0x5A));
    private static readonly SolidColorBrush Track = Freeze(MediaColor.FromRgb(0x2A, 0x31, 0x42));
    private static readonly SolidColorBrush Fill = Freeze(MediaColor.FromRgb(0xE4, 0xA1, 0x5A));

    private CompanionController? _controller;
    private TextBox? _uriBox;
    private bool _exiting;
    private bool _hooked;

    public MainWindow()
    {
        InitializeComponent();
    }

    internal void Bind(CompanionController controller)
    {
        _controller = controller;
        controller.StateChanged += OnState;
        controller.UiOpen += ShowFromController;
        controller.UiClose += HideFromController;
        ApplyChrome();
    }

    public IntPtr EnsureHotkeyWindow()
    {
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        if (_hooked)
            return hwnd;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        _hooked = true;
        return hwnd;
    }

    public void PrepareForExit() => _exiting = true;

    private void OnState()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(OnState);
            return;
        }

        ApplyChrome();
        if (IsVisible)
            RebuildRows();
    }

    private void ShowFromController()
    {
        Position();
        if (!IsVisible)
            Show();
        Activate();
        Focus();
        ApplyChrome();
        RebuildRows();
    }

    private void HideFromController() => Hide();

    private void Position()
    {
        var area = SystemParameters.WorkArea;
        var width = Math.Min(440, Math.Max(320, area.Width - 24));
        var height = Math.Min(700, Math.Max(420, area.Height - 24));
        Width = width;
        Height = height;
        Left = area.Right - width - 8;
        Top = area.Top + Math.Max(0, (area.Height - height) / 2);
    }

    private void ApplyChrome()
    {
        if (_controller is null)
            return;
        TitleText.Text = _controller.Title;
        SubtitleText.Text = _controller.Subtitle;
        ErrorText.Text = _controller.Error ?? "";
        DevBanner.Visibility = _controller.DevelopmentMode ? Visibility.Visible : Visibility.Collapsed;
        var hotkey = _controller.HotkeyMessage;
        HotkeyText.Text = hotkey ?? "";
        HotkeyText.Visibility = string.IsNullOrWhiteSpace(hotkey) ? Visibility.Collapsed : Visibility.Visible;
        HeaderButton.Content = _controller.Page == "quick" ? "Close" : "Back";
        HintText.Text = _controller.RecordingKeyboard
            ? "Press the new keyboard shortcut. Esc cancels. Windows will say so if RegisterHotKey rejects it."
            : "Controller, arrow keys, touch, or mouse. A or Enter selects. B or Esc goes back. Left and right change a slider.";
    }

    private void RebuildRows()
    {
        if (_controller is null)
            return;

        Rows.Children.Clear();
        _uriBox = null;
        if (_controller.EditingUri)
            Rows.Children.Add(BuildUriBox());

        FrameworkElement? focused = null;
        var index = 0;
        foreach (var item in _controller.Focus.Items)
        {
            var row = BuildRow(item, index == _controller.Focus.FocusIndex);
            Rows.Children.Add(row);
            if (index == _controller.Focus.FocusIndex)
                focused = row;
            index++;
        }

        focused?.BringIntoView();
    }

    private TextBox BuildUriBox()
    {
        var box = new TextBox
        {
            Text = _controller?.UriDraft ?? "",
            FontSize = 16,
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 8),
            Background = Card,
            Foreground = Ink,
            BorderBrush = Amber,
            BorderThickness = new Thickness(2),
            CaretBrush = Ink
        };
        box.TextChanged += (_, _) => _controller?.SetUriDraft(box.Text);
        _uriBox = box;
        Dispatcher.BeginInvoke(new Action(() => box.Focus()));
        return box;
    }

    private Border BuildRow(NavItem item, bool focused)
    {
        var body = new StackPanel();
        var header = new DockPanel();
        if (item.Kind == NavItemKind.Toggle)
        {
            var pill = new TextBlock
            {
                Text = item.Value == 1 ? "On" : "Off",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = item.Value == 1 ? Amber : Muted,
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(pill, Dock.Right);
            header.Children.Add(pill);
        }

        header.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = item.Enabled ? Ink : Muted,
            TextWrapping = TextWrapping.Wrap
        });

        body.Children.Add(header);
        var detail = DetailFor(item);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            body.Children.Add(new TextBlock
            {
                Text = detail,
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = 14,
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap
            });
        }

        if (item.Kind == NavItemKind.Slider)
            body.Children.Add(BuildSlider(item));

        var row = new Border
        {
            Child = body,
            Background = focused ? CardFocus : Card,
            BorderBrush = focused ? Amber : Line,
            BorderThickness = new Thickness(focused ? 3 : 1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 8),
            MinHeight = 64,
            Opacity = item.Enabled ? 1 : 0.72,
            Cursor = System.Windows.Input.Cursors.Hand,
            Focusable = false
        };
        System.Windows.Automation.AutomationProperties.SetName(row, item.Title);
        row.MouseLeftButtonUp += (_, args) =>
        {
            if (_controller is null || args.Handled)
                return;
            var activate = item.Kind is NavItemKind.Action or NavItemKind.Toggle;
            _controller.Select(item.Id, activate);
        };
        return row;
    }

    private UIElement BuildSlider(NavItem item)
    {
        var value = Math.Clamp(item.Value, item.Min, item.Max);
        var span = Math.Max(1, item.Max - item.Min);
        var filled = Math.Max(0, value - item.Min);
        var bar = new Grid { Height = 10, Margin = new Thickness(0, 8, 0, 0) };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(filled, 0.001), GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(span - filled, 0.001), GridUnitType.Star) });
        var done = new Border { Background = item.Enabled ? Fill : Track, CornerRadius = new CornerRadius(5, 0, 0, 5) };
        var rest = new Border { Background = Track, CornerRadius = new CornerRadius(0, 5, 5, 0) };
        if (filled == 0)
            done.CornerRadius = new CornerRadius(5);
        if (filled >= span)
            done.CornerRadius = new CornerRadius(5);
        Grid.SetColumn(done, 0);
        Grid.SetColumn(rest, 1);
        bar.Children.Add(done);
        bar.Children.Add(rest);
        bar.MouseLeftButtonUp += (_, args) =>
        {
            if (_controller is null || !item.Enabled)
                return;
            var width = Math.Max(1, bar.ActualWidth);
            var fraction = Math.Clamp(args.GetPosition(bar).X / width, 0, 1);
            var next = (int)Math.Round(item.Min + fraction * (item.Max - item.Min));
            _controller.SetSlider(item.Id, next);
            args.Handled = true;
        };
        return bar;
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_controller is null)
            return;

        if (_controller.RecordingKeyboard)
        {
            e.Handled = true;
            if (e.Key == Key.Escape)
            {
                _controller.CancelRecording();
                return;
            }

            if (IsModifier(e.Key))
                return;
            var recorded = MapKey(e.Key);
            if (recorded is null)
                return;
            var modifiers = Keyboard.Modifiers;
            _controller.TryCaptureKey(
                recorded,
                modifiers.HasFlag(ModifierKeys.Control),
                modifiers.HasFlag(ModifierKeys.Alt),
                modifiers.HasFlag(ModifierKeys.Shift),
                modifiers.HasFlag(ModifierKeys.Windows));
            return;
        }

        if (_uriBox is not null && _uriBox.IsKeyboardFocusWithin)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                _controller.Back();
            }

            return;
        }

        switch (e.Key)
        {
            case Key.Up:
                _controller.Move(NavDirection.Up);
                e.Handled = true;
                break;
            case Key.Down:
                _controller.Move(NavDirection.Down);
                e.Handled = true;
                break;
            case Key.Left:
                _controller.Move(NavDirection.Left);
                e.Handled = true;
                break;
            case Key.Right:
                _controller.Move(NavDirection.Right);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Space:
                _controller.Activate();
                e.Handled = true;
                break;
            case Key.Escape:
            case Key.Back:
                _controller.Back();
                e.Handled = true;
                break;
            case Key.Tab:
                _controller.Move(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? NavDirection.Up : NavDirection.Down);
                e.Handled = true;
                break;
        }
    }

    private void HeaderButton_Click(object sender, RoutedEventArgs e)
    {
        if (_controller is null)
            return;
        if (_controller.Page == "quick")
            _controller.HidePanel(restoreFocus: true);
        else
            _controller.Back();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_exiting)
            return;
        e.Cancel = true;
        _controller?.HidePanel(restoreFocus: true);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0312 && wParam.ToInt32() == HotkeyRegistrar.HotkeyId)
        {
            handled = true;
            _controller?.TogglePanel();
        }

        return IntPtr.Zero;
    }

    private static string DetailFor(NavItem item)
    {
        if (!item.Enabled && !string.IsNullOrWhiteSpace(item.DisabledReason))
            return item.DisabledReason ?? "";
        return item.Detail;
    }

    private static string? MapKey(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
            return key.ToString();
        if (key >= Key.F1 && key <= Key.F24)
            return key.ToString();
        if (key >= Key.D0 && key <= Key.D9)
            return ((int)key - (int)Key.D0).ToString();
        return key switch
        {
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Escape => "Escape",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            _ => null
        };
    }

    private static bool IsModifier(Key key) => key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System;

    private static SolidColorBrush Freeze(System.Windows.Media.Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
