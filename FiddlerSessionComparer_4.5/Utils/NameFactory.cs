namespace Abstracta.FiddlerSessionComparer.Utils
{
    public class NameFactory
    {
        private static volatile NameFactory _instance;
        private static readonly object Lock = new object();
        private int _id = -1;

        public static NameFactory GetInstance()
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

        public string GetNewName()
        {
            return "URL_Params_Comparer_" + ++_id;
        }

        public void Reset()
        {
            _id = -1;
        }
    }
}