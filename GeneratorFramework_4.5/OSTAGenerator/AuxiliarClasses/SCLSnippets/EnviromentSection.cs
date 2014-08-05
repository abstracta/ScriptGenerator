namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class EnviromentSection : ISCLSections
    {
        private readonly string _code;

        public EnviromentSection(string dsc)
        {
            _code = string.Format("\nEnvironment\n" +
                                 "\t Description \"{0}\"\n" +
                                 "\t Mode \t HTTP \n" +
                                 "\t Wait \t UNIT MILLISECONDS \n\n",
                                 dsc);
        }

        public string WriteCode()
        {
            return _code;
        }
    }
}
