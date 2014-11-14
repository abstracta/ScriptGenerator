namespace Abstracta.FiddlerSessionComparer.Utils
{
    public class StringUtils
    {
        public static bool IsNullOrWhiteSpace(string str)
        {
            return string.IsNullOrEmpty(str.Trim());
        }
    }
}
