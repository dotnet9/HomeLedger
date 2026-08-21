using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HomeLedger.Avalonia.Controls;

public record BarItem(string Label, double In, double Out);
public record DonutItem(string Label, double Value, string Color);

internal static class ChartHelpers
{
    public static FormattedText Fmt(string text, double size, IBrush? foreground = null) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, size, foreground ?? SolidColorBrush.Parse("#5a6058"));
}

/// <summary>月度收支柱状图（纯 Avalonia 自绘，无第三方图表依赖）。</summary>
public class BarChartControl : Control
{
    public static readonly StyledProperty<IReadOnlyList<BarItem>?> ItemsProperty =
        AvaloniaProperty.Register<BarChartControl, IReadOnlyList<BarItem>?>(nameof(Items));

    public static readonly StyledProperty<string> InLabelProperty =
        AvaloniaProperty.Register<BarChartControl, string>(nameof(InLabel), "收入");
    public static readonly StyledProperty<string> OutLabelProperty =
        AvaloniaProperty.Register<BarChartControl, string>(nameof(OutLabel), "支出");

    public IReadOnlyList<BarItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }
    public string InLabel { get => GetValue(InLabelProperty); set => SetValue(InLabelProperty, value); }
    public string OutLabel { get => GetValue(OutLabelProperty); set => SetValue(OutLabelProperty, value); }

    static BarChartControl() => AffectsMeasure<BarChartControl>(ItemsProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var h = Math.Min(availableSize.Height is > 0 and < 4000 ? availableSize.Height : 180, 260);
        return new Size(availableSize.Width is > 0 ? availableSize.Width : 300, h);
    }

    public override void Render(DrawingContext context)
    {
        var items = Items;
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w < 40 || h < 40) return;

        var fg = SolidColorBrush.Parse("#99a096");
        var plotBottom = h - 20;

        if (items is not { Count: > 0 })
        {
            var empty = ChartHelpers.Fmt("暂无数据", 13);
            context.DrawText(empty, new Point(w / 2 - empty.Width / 2, h / 2 - empty.Height / 2));
            return;
        }

        var max = Math.Max(1.0, items.Max(i => Math.Max(i.In, i.Out)));
        var slot = w / items.Count;
        var barW = Math.Min(24.0, slot / 3.2);
        var inBrush = new LinearGradientBrush
        {
            GradientStops = new GradientStops { new GradientStop(Color.Parse("#35ab7d"), 0), new GradientStop(Color.Parse("#2f9e6e"), 1) },
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
        };
        var outBrush = new LinearGradientBrush
        {
            GradientStops = new GradientStops { new GradientStop(Color.Parse("#d97a63"), 0), new GradientStop(Color.Parse("#c4553d"), 1) },
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
        };

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var cx = slot * i + slot / 2;
            var hIn = item.In / max * (plotBottom - 6);
            var hOut = item.Out / max * (plotBottom - 6);
            var rIn = new Rect(cx - barW - 2, plotBottom - Math.Max(hIn, 2), barW, Math.Max(hIn, 2));
            var rOut = new Rect(cx + 2, plotBottom - Math.Max(hOut, 2), barW, Math.Max(hOut, 2));
            context.FillRectangle(inBrush, rIn, 4);
            context.FillRectangle(outBrush, rOut, 4);

            var label = ChartHelpers.Fmt(item.Label, 11, fg);
            context.DrawText(label, new Point(cx - label.Width / 2, plotBottom + 4));
        }

        var legend = ChartHelpers.Fmt($"● {InLabel}   ● {OutLabel}", 11, SolidColorBrush.Parse("#5a6058"));
        context.DrawText(legend, new Point(w / 2 - legend.Width / 2, 0));
    }
}

/// <summary>分类占比环形图（纯 Avalonia 自绘）。</summary>
public class DonutControl : Control
{
    public static readonly StyledProperty<IReadOnlyList<DonutItem>?> ItemsProperty =
        AvaloniaProperty.Register<DonutControl, IReadOnlyList<DonutItem>?>(nameof(Items));

    public IReadOnlyList<DonutItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    static DonutControl() => AffectsMeasure<DonutControl>(ItemsProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Math.Min(availableSize.Width is > 0 and < 4000 ? availableSize.Width : 150, 170);
        return new Size(size, size);
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size < 30) return;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var outer = size / 2 - 4;
        var inner = outer - Math.Max(14, outer * 0.32);
        var items = Items;
        var total = items?.Sum(i => i.Value) ?? 0;

        if (items is not { Count: > 0 } || total <= 0)
        {
            context.DrawEllipse(null, new Pen(SolidColorBrush.Parse("#b9bdb4"), outer - inner), center, (outer + inner) / 2, (outer + inner) / 2);
            return;
        }

        var angle = -90.0;
        foreach (var item in items)
        {
            var sweep = item.Value / total * 360;
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                var a0 = angle * Math.PI / 180;
                var a1 = (angle + sweep - 1.5) * Math.PI / 180;
                var p0o = new Point(center.X + outer * Math.Cos(a0), center.Y + outer * Math.Sin(a0));
                var p1o = new Point(center.X + outer * Math.Cos(a1), center.Y + outer * Math.Sin(a1));
                var p1i = new Point(center.X + inner * Math.Cos(a1), center.Y + inner * Math.Sin(a1));
                var p0i = new Point(center.X + inner * Math.Cos(a0), center.Y + inner * Math.Sin(a0));
                ctx.BeginFigure(p0o, true);
                ctx.ArcTo(p1o, new Size(outer, outer), 0, false, SweepDirection.Clockwise, true);
                ctx.LineTo(p1i);
                ctx.ArcTo(p0i, new Size(inner, inner), 0, false, SweepDirection.CounterClockwise, true);
                ctx.EndFigure(true);
            }
            context.DrawGeometry(SolidColorBrush.Parse(item.Color), null!, geometry);
            angle += sweep;
        }

        var text = ChartHelpers.Fmt(total.ToString("0.##"), 12);
        context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
    }
}
