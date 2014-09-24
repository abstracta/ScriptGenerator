namespace Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor
{
    internal class NameGenerator
    {
        private static volatile NameGenerator _instance;

        private static readonly object Lock = new object();

        private static int _id = -1;

        internal static NameGenerator GetInstance()
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = new NameGenerator();
                    }
                }
            }

            return _instance;
        }

        internal string GetNewName()
        {
            return "URL_Params_Generator_" + ++_id;
        }
    }
}
