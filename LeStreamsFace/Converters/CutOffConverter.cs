using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace LeStreamsFace
{
    internal class CutoffConverter : IValueConverter
    {
        public int Cutoff { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int intValue = System.Convert.ToInt32(value);
            return intValue > Cutoff;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}