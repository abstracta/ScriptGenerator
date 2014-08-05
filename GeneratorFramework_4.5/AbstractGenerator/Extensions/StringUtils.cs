namespace Abstracta.Generators.Framework.AbstractGenerator.Extensions
{
    internal class StringUtils
    {
        internal static bool IsNullOrWhiteSpace(string str)
        {
            return string.IsNullOrEmpty(str.Trim());
        }
    }
}
