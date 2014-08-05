namespace Abstracta.FiddlerSessionComparer.Utils
{
    internal class NameFactory
    {
        private static volatile NameFactory _instance;
        private static readonly object Lock = new object();
        private int _id = -1;

        internal static NameFactory GetInstance()
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = new NameFactory();
                    }
                }
            }
            return _instance;
        }

        internal string GetNewName()
        {
            return "URL_Params" + ++_id;
        }
    }
}