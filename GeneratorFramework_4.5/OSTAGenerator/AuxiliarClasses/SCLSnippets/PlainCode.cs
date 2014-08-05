namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class PlainCode : ISCLSections
    {
        private readonly string _plainCode;

        public PlainCode(string code)
        {
            _plainCode = code;
        }

        public string WriteCode()
        {
            //fijarse que si el largo maximo es mayor al largo maximo de linea se tiene que poner en dos lineas
            //return OpenSTAUtils.SplitStringIFNecesary( plainCode);
            return _plainCode;
        }
    }
}
