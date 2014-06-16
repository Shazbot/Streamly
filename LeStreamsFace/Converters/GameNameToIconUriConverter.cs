using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace LeStreamsFace
{
    public class GameNameToIconUriConverter : MarkupExtension, IValueConverter
    {
        private static GameNameToIconUriConverter _converter = null;
        public GameNameToIconUriConverter()
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_converter == null)
            {
                _converter = new GameNameToIconUriConverter();
            }
            return _converter;
        }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
                              CultureInfo culture)
        {
            string iconUri = null;

            switch (value as string)
            {
                case "League of Legends":
                    iconUri = "leagueIcon.png";
                    break;

                case "Dota 2":
                    iconUri = "DotaIcon.png";
                    break;

                case "StarCraft II: Heart of the Swarm":
                case "StarCraft II":
                    iconUri = "StarcraftIcon.png";
                    break;

                case "Diablo III":
                    iconUri = "diabloIcon.png";
                    break;

                case "Tribes Ascend":
                    iconUri = "tribesIcon.png";
                    break;

                case "Minecraft":
                    iconUri = "minecraftIcon.ico";
                    break;

                case "Heroes of Newerth":
                    iconUri = "honIcon.png";
                    break;

                case "The Binding of Isaac":
                    iconUri = "bindingOfIsaacIcon.png";
                    break;

                default:
                    return null;
            }

            return new Uri("pack://application:,,,/Resources/" + iconUri);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                                  CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion IValueConverter Members
    }
}