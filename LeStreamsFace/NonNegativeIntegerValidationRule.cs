using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace LeStreamsFace
{
    internal class NonNegativeIntegerValidationRule : ValidationRule
    {
        private int intValue = 0;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                if (((string)value).Length > 0)
                    intValue = Int32.Parse((String)value);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if (intValue < 0)
            {
                return new ValidationResult(false, "Input an integer >= 0");
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }
}