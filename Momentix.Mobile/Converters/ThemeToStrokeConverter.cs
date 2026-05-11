using System.Globalization;

namespace Momentix.Mobile.Converters
{
    public class ThemeToStrokeConverter : IValueConverter
    {
        public string TargetTheme { get; set; } = string.Empty;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString() ? 2.5 : 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}