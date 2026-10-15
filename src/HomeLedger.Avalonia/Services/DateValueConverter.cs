using System.Globalization;
using Avalonia.Data.Converters;

namespace HomeLedger.Avalonia.Services;

/// <summary>紧凑日期选择器与现有日期偏移视图模型互转。</summary>
public sealed class DateValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is DateTimeOffset date ? date.DateTime : null;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is DateTime date ? new DateTimeOffset(date) : null;
}
