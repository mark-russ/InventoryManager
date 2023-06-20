using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using eBay.Service.Core.Soap;

namespace Inventory_Manager.Converters
{
    [ValueConversion(typeof(object), typeof(String))]
    public class eBayListingDurationToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Objects.eBayListingDuration)value).Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Visible ? true : false;
        }
    }
}