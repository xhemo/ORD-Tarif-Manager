using System;
using System.Globalization;
using System.Windows.Data;
using OrdTarifManager.Core;

namespace OrdTarifManager.UI
{
    public class TariffCellDisplayConverter : IValueConverter
    {
        private readonly string _colName;

        public TariffCellDisplayConverter(string colName)
        {
            _colName = colName;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return NumberParser.FormatValueForDisplay(value, _colName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value != null ? value.ToString() : "";
            return NumberParser.ParseValueFromInput(str, _colName);
        }
    }
}
