namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses
{
    internal class ParametrizedValue
    {
        public string HTMLElement { get; set; }
        public string HTMLElementValue { get; set; }
        public Variable Var { get; set; }

        public ParametrizedValue(string htmlElement, string htmlValue, Variable vari)
        {
            HTMLElement = htmlElement;
            HTMLElementValue = htmlValue;
            Var = vari;
        }
    }
}