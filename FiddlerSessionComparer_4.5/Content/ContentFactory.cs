using System.Globalization;

namespace Abstracta.FiddlerSessionComparer.Content
{
    public abstract class ContentFactory
    {
        public static bool IsComplexType(string varValue)
        {
            return IsJSON(varValue) || IsXML(varValue);
        }

        public static bool IsJSON(string value)
        {
            // '%7B' = '{' -> escaped JSON
            return value.StartsWith("{") || value.StartsWith("%7B", true, CultureInfo.InvariantCulture);
        }

        public static bool IsXML(string value)
        {
            // '%7C' = '<' -> escaped XML
            return value.StartsWith("<") || value.StartsWith("%7C", true, CultureInfo.InvariantCulture);
        }
    }
}