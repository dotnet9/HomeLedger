using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace HomeLedger.Avalonia.Controls;

/// <summary>
/// 玉账自定义窗体：无系统装饰（WindowDecorations.None），自绘标题栏 + 手动拖拽/八向缩放，
/// Win11 使用系统原生圆角，旧系统以透明 + 圆角 Border 模拟。移植自 Zitie 的 ZitieWindow。
/// </summary>
[PseudoClasses(":normal", ":maximized", ":fullscreen", ":native-window-corners")]
[TemplatePart("PART_TitleBar", typeof(InputElement))]
[TemplatePart("PART_MinimizeButton", typeof(Button))]
[TemplatePart("PART_MaximizeButton", typeof(Button))]
[TemplatePart("PART_CloseButton", typeof(Button))]
public class JadeWindow : Window
{
    public static readonly StyledProperty<bool> IsTitleBarVisibleProperty =
        AvaloniaProperty.Register<JadeWindow, bool>(nameof(IsTitleBarVisible), true);

    public static readonly StyledProperty<bool> IsManagedResizerVisibleProperty =
        AvaloniaProperty.Register<JadeWindow, bool>(nameof(IsManagedResizerVisible), true);

    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<JadeWindow, object?>(nameof(LeftContent));

    public static readonly StyledProperty<object?> TitleBarContentProperty =
        AvaloniaProperty.Register<JadeWindow, object?>(nameof(TitleBarContent));

    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<JadeWindow, double>(nameof(TitleBarHeight), 40);

    public static readonly StyledProperty<IBrush?> TitleBarBackgroundProperty =
        AvaloniaProperty.Register<JadeWindow, IBrush?>(nameof(TitleBarBackground));

    public static readonly StyledProperty<IBrush?> TitleBarForegroundProperty =
        AvaloniaProperty.Register<JadeWindow, IBrush?>(nameof(TitleBarForeground));

    public static readonly StyledProperty<CornerRadius> WindowCornerRadiusProperty =
        AvaloniaProperty.Register<JadeWindow, CornerRadius>(nameof(WindowCornerRadius), new CornerRadius(12));

    private readonly Dictionary<Control, WindowEdge> _resizeGrips = new();
    private readonly bool _usesNativeWindowCorners;
    private InputElement? _titleBar;
    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _closeButton;

    public JadeWindow()
    {
        WindowDecorations = WindowDecorations.None;
        ExtendClientAreaToDecorationsHint = false;
        _usesNativeWindowCorners = WindowsWindowCornerHelper.IsSupported;
        TransparencyLevelHint =
            [_usesNativeWindowCorners ? WindowTransparencyLevel.None : WindowTransparencyLevel.Transparent];
        TransparencyBackgroundFallback = Brushes.Transparent;
        PseudoClasses.Set(":native-window-corners", _usesNativeWindowCorners);
    }

    public bool IsTitleBarVisible
    {
        get => GetValue(IsTitleBarVisibleProperty);
        set => SetValue(IsTitleBarVisibleProperty, value);
    }

    public bool IsManagedResizerVisible
    {
        get => GetValue(IsManagedResizerVisibleProperty);
        set => SetValue(IsManagedResizerVisibleProperty, value);
    }

    public object? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    public IBrush? TitleBarBackground
    {
        get => GetValue(TitleBarBackgroundProperty);
        set => SetValue(TitleBarBackgroundProperty, value);
    }

    public IBrush? TitleBarForeground
    {
        get => GetValue(TitleBarForegroundProperty);
        set => SetValue(TitleBarForegroundProperty, value);
    }

    public CornerRadius WindowCornerRadius
    {
        get => GetValue(WindowCornerRadiusProperty);
        set => SetValue(WindowCornerRadiusProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(JadeWindow);

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ApplyNativeWindowCornerPreference();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        DetachTemplateEvents();

        _titleBar = e.NameScope.Find<InputElement>("PART_TitleBar");
        _minimizeButton = e.NameScope.Find<Button>("PART_MinimizeButton");
        _maximizeButton = e.NameScope.Find<Button>("PART_MaximizeButton");
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");

        if (_titleBar is not null) _titleBar.PointerPressed += TitleBar_OnPointerPressed;
        if (_minimizeButton is not null) _minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        if (_maximizeButton is not null) _maximizeButton.Click += (_, _) => ToggleMaximizeRestore();
        if (_closeButton is not null) _closeButton.Click += (_, _) => Close();

        AttachResizeGrip(e, "PART_ResizeTopLeft", WindowEdge.NorthWest);
        AttachResizeGrip(e, "PART_ResizeTop", WindowEdge.North);
        AttachResizeGrip(e, "PART_ResizeTopRight", WindowEdge.NorthEast);
        AttachResizeGrip(e, "PART_ResizeLeft", WindowEdge.West);
        AttachResizeGrip(e, "PART_ResizeRight", WindowEdge.East);
        AttachResizeGrip(e, "PART_ResizeBottomLeft", WindowEdge.SouthWest);
        AttachResizeGrip(e, "PART_ResizeBottom", WindowEdge.South);
        AttachResizeGrip(e, "PART_ResizeBottomRight", WindowEdge.SouthEast);

        UpdateWindowState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty || change.Property == CanResizeProperty || change.Property == IsManagedResizerVisibleProperty)
            UpdateWindowState();
        if (change.Property == WindowStateProperty)
            ApplyNativeWindowCornerPreference();
    }

    protected override void OnClosed(EventArgs e)
    {
        DetachTemplateEvents();
        base.OnClosed(e);
    }

    private void AttachResizeGrip(TemplateAppliedEventArgs e, string name, WindowEdge edge)
    {
        var grip = e.NameScope.Find<Control>(name);
        if (grip is null) return;
        _resizeGrips.Add(grip, edge);
        grip.PointerPressed += ResizeGrip_OnPointerPressed;
    }

    private void DetachTemplateEvents()
    {
        if (_titleBar is not null) _titleBar.PointerPressed -= TitleBar_OnPointerPressed;
        foreach (var grip in _resizeGrips.Keys)
            grip.PointerPressed -= ResizeGrip_OnPointerPressed;
        _resizeGrips.Clear();
        _titleBar = null;
        _minimizeButton = null;
        _maximizeButton = null;
        _closeButton = null;
    }

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || IsFromFocusableControl(e.Source))
            return;

        if (e.ClickCount == 2 && CanResize && WindowState != WindowState.FullScreen)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }
        BeginMoveDrag(e);
        e.Handled = true;
    }

    private void ResizeGrip_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsManagedResizerVisible || !CanResize
            || WindowState is WindowState.Maximized or WindowState.FullScreen
            || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || sender is not Control grip
            || !_resizeGrips.TryGetValue(grip, out var edge))
            return;

        BeginResizeDrag(edge, e);
        e.Handled = true;
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void UpdateWindowState()
    {
        PseudoClasses.Set(":normal", WindowState == WindowState.Normal);
        PseudoClasses.Set(":maximized", WindowState == WindowState.Maximized);
        PseudoClasses.Set(":fullscreen", WindowState == WindowState.FullScreen);

        if (_maximizeButton is not null)
            _maximizeButton.IsEnabled = CanResize && WindowState != WindowState.FullScreen;

        var showResizeGrips = IsManagedResizerVisible && CanResize
                              && WindowState is not (WindowState.Maximized or WindowState.FullScreen);
        foreach (var grip in _resizeGrips.Keys)
            grip.IsVisible = showResizeGrips;
    }

    private void ApplyNativeWindowCornerPreference()
    {
        if (_usesNativeWindowCorners)
            WindowsWindowCornerHelper.TryApply(this, WindowState == WindowState.Normal);
    }

    private bool IsFromFocusableControl(object? source)
    {
        if (source is not Visual sourceVisual) return false;
        var visual = sourceVisual;
        while (visual is not null && visual != _titleBar)
        {
            if (visual is Button or TextBox or ComboBox or CheckBox or ToggleButton)
                return true;
            visual = visual.GetVisualParent();
        }
        return false;
    }
}

internal static class WindowsWindowCornerHelper
{
    private const int DwmWindowCornerPreferenceAttribute = 33;
    private const int DoNotRound = 1;
    private const int Round = 2;

    public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    public static bool TryApply(Window window, bool rounded)
    {
        if (!IsSupported) return false;
        var platformHandle = window.TryGetPlatformHandle();
        if (platformHandle is null || platformHandle.Handle == IntPtr.Zero) return false;

        var preference = rounded ? Round : DoNotRound;
        return DwmSetWindowAttribute(platformHandle.Handle, DwmWindowCornerPreferenceAttribute, ref preference, sizeof(int)) >= 0;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int attributeValue, int attributeSize);
}
