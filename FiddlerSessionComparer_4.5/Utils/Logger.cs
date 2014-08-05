using System.IO;

namespace Abstracta.FiddlerSessionComparer.Utils
{
    internal class Logger
    {
        private static volatile Logger _instance;
        private static readonly object Lock = new object();

        private const string FileName = "FiddlerSessionComparer_Logger.txt";

        internal static Logger GetInstance()
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = new Logger();
                    }
                }
            }
            return _instance;
        }

        internal void Log(object log)
        {
            using (var file = new StreamWriter(FileName, true))
            {
                file.WriteLine(System.DateTime.Now + " - " + log);
            }
        }
    }
}