using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using eBay.Service.Core.Soap;

namespace Inventory_Manager.Converters
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class CategoryFeatureToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProductIdentiferEnabledCodeType)
            {
                if ((ProductIdentiferEnabledCodeType)value != ProductIdentiferEnabledCodeType.Disabled)
                    return Visibility.Visible;
            }
            else if (value is ConditionEnabledCodeType)
            {
                if ((ConditionEnabledCodeType)value != ConditionEnabledCodeType.Disabled)
                    return Visibility.Visible;
            }
            else if (value is null == false)
            {
                return Visibility.Visible;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Visible ? true : false;
        }
    }
}
