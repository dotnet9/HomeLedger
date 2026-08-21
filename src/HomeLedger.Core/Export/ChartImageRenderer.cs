using SkiaSharp;

namespace HomeLedger.Core.Export;

/// <summary>用 SkiaSharp 离屏渲染图表 PNG（月度柱状图 / 分类环形图），供导出复用。</summary>
public static class ChartImageRenderer
{
    private static readonly SKColor Jade = new(0x14, 0x7d, 0x64);
    private static readonly SKColor Red = new(0xc4, 0x55, 0x3d);
    private static readonly SKColor Gold = new(0xd9, 0xa4, 0x41);
    private static readonly SKColor Green = new(0x2f, 0x9e, 0x6e);
    private static readonly SKColor Gray = new(0xb9, 0xbd, 0xb4);
    private static readonly SKColor[] Palette = [Jade, Gold, Green, Red, Gray,
        new(0x6a, 0x9f, 0xc9), new(0xc9, 0x84, 0x6a), new(0x8a, 0x7f, 0xc9)];

    /// <summary>月度收支柱状图。series1=收入，series2=支出。</summary>
    public static byte[] MonthlyBars(List<(string Label, decimal In, decimal Out)> data, int width = 900, int height = 380)
    {
        using var bmp = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(new SKColor(0xff, 0xff, 0xff));
        var font = new SKFont(SKTypeface.Default, 12);
        var max = Math.Max(1m, data.Count == 0 ? 1 : data.Max(d => Math.Max(d.In, d.Out)));
        var plotTop = 20f; var plotBottom = height - 34f;
        var slot = width / (float)Math.Max(1, data.Count);
        var barW = Math.Min(26f, slot / 3f);
        for (var i = 0; i < data.Count; i++)
        {
            var cx = slot * i + slot / 2;
            var hIn = (float)(data[i].In / max) * (plotBottom - plotTop);
            var hOut = (float)(data[i].Out / max) * (plotBottom - plotTop);
            using var p1 = new SKPaint { Color = Green, IsAntialias = true };
            using var p2 = new SKPaint { Color = Red, IsAntialias = true };
            canvas.DrawRoundRect(cx - barW - 2, plotBottom - hIn, barW, Math.Max(hIn, 1), 4, 4, p1);
            canvas.DrawRoundRect(cx + 2, plotBottom - hOut, barW, Math.Max(hOut, 1), 4, 4, p2);
            using var tp = new SKPaint { Color = new SKColor(0x99, 0xa0, 0x96), IsAntialias = true };
            canvas.DrawText(data[i].Label, cx - font.MeasureText(data[i].Label) / 2, height - 12, font, tp);
        }
        return Encode(bmp);
    }

    /// <summary>分类占比环形图。</summary>
    public static byte[] Donut(List<(string Label, decimal Value)> data, int size = 360)
    {
        using var bmp = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(new SKColor(0xff, 0xff, 0xff));
        var total = data.Sum(d => d.Value);
        var rect = new SKRect(20, 20, size - 20, size - 20);
        if (total <= 0)
        {
            using var empty = new SKPaint { Color = Gray, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 34 };
            canvas.DrawOval(rect, empty);
            return Encode(bmp);
        }
        var start = -90f;
        for (var i = 0; i < data.Count; i++)
        {
            var sweep = (float)(data[i].Value / total) * 360;
            using var paint = new SKPaint { Color = Palette[i % Palette.Length], IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 40 };
            using var path = new SKPath();
            path.AddArc(rect, start, Math.Max(sweep - 1.5f, 0.5f));
            canvas.DrawPath(path, paint);
            start += sweep;
        }
        return Encode(bmp);
    }

    private static byte[] Encode(SKBitmap bmp)
    {
        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
