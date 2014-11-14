using System.Collections.Generic;

namespace Abstracta.FiddlerSessionComparer.Utils
{
    public class NameFactory
    {
        private static volatile NameFactory _instance;
        private static readonly object Lock = new object();
        private Dictionary<string, int> _nameRegister;

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

        public NameFactory()
        {
            Reset();
        }

        public string GetNewName(string name)
        {
            if (!_nameRegister.ContainsKey(name))
            {
                _nameRegister.Add(name, -1);
            }

            return name + "_" + ++_nameRegister[name];
        }

        public string GetNewName()
        {
            return "URL_Params_Comparer" + "_" + ++_nameRegister["URL_Params_Comparer"];
        }

        public void Reset()
        {
            _nameRegister = new Dictionary<string, int>
                {
                    { "URL_Params_Comparer", -1 }
                };
        }
    }
}