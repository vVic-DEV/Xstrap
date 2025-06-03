using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Bloxstrap.UI.Converters
{
    class EnumNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Enum enumVal)
            {
                // If value is not an enum, return as-is or fallback
                return value?.ToString() ?? "Unknown";
            }

            var stringVal = enumVal.ToString();
            var type = enumVal.GetType();
            var typeName = type.FullName!;
            var memberInfo = type.GetMember(stringVal).FirstOrDefault();

            if (memberInfo != null)
            {
                var attribute = (EnumNameAttribute)memberInfo
                    .GetCustomAttributes(typeof(EnumNameAttribute), false)
                    .FirstOrDefault();

                if (attribute != null)
                {
                    if (!string.IsNullOrEmpty(attribute.StaticName))
                        return attribute.StaticName;

                    if (!string.IsNullOrEmpty(attribute.FromTranslation))
                        return Strings.ResourceManager.GetStringSafe(attribute.FromTranslation);
                }
            }

            return Strings.ResourceManager.GetStringSafe($"{typeName.Substring(typeName.IndexOf('.') + 1)}.{stringVal}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}