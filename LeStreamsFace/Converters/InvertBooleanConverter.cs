using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace LeStreamsFace.Converters
{
    internal class InvertBooleanConverter : MarkupExtension, IValueConverter
    {
        private static InvertBooleanConverter _converter = null;

        public InvertBooleanConverter()
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_converter == null)
            {
                _converter = new InvertBooleanConverter();
            }
            return _converter;
        }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            // commented out for clowny use in a multiple converter chain
//            if (targetType != typeof(bool))
//                throw new InvalidOperationException("The target must be a boolean");

//            if (value.GetType() != typeof(bool))
//                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion IValueConverter Members
    }
}