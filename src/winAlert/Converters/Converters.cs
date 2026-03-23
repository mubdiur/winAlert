using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using winAlert.Domain.Models;

namespace winAlert.Converters;

/// <summary>
/// Converts AlertSeverity to a SolidColorBrush.
/// </summary>
public sealed class SeverityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AlertSeverity severity)
        {
            var hexColor = severity.ToColorHex();
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to Visibility (true = Visible, false = Collapsed).
/// Supports ConverterParameter="Invert" for reversing the logic.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        
        // Check if parameter is "Invert" (XAML passes this as string)
        var shouldInvert = Invert;
        if (parameter is string paramStr && paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            shouldInvert = !shouldInvert;
        }

        if (shouldInvert)
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            var result = visibility == Visibility.Visible;
            var shouldInvert = Invert;
            if (parameter is string paramStr && paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                shouldInvert = !shouldInvert;
            }
            return shouldInvert ? !result : result;
        }

        return false;
    }
}

/// <summary>
/// Converts DateTime to a human-readable "time ago" string.
/// </summary>
public sealed class TimeAgoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var elapsed = DateTime.UtcNow - dateTime;

            if (elapsed.TotalSeconds < 60)
                return $"{(int)elapsed.TotalSeconds}s ago";
            if (elapsed.TotalMinutes < 60)
                return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24)
                return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 30)
                return $"{(int)elapsed.TotalDays}d ago";

            return dateTime.ToString("MMM d, yyyy");
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts AlertSeverity to display name string.
/// </summary>
public sealed class SeverityToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AlertSeverity severity)
        {
            return severity.ToDisplayName();
        }

        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

/// <summary>
/// Converts a count to a visibility (count > 0 = Visible).
/// </summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts connection status to a color brush.
/// </summary>
public sealed class ConnectionStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#66BB6A")) // Green
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9999B8")); // Gray
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
