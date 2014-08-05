using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class ConstantDeclarationSection : ISCLSections
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public ConstantDeclarationSection(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string WriteCode()
        {
            int valor;
            string result;
            if (int.TryParse(Value, out valor))
            {
                result = string.Format("CONSTANT {0} = {1}\n", Name, Value);
            }
            else
            {
                var aux = OpenSTAUtils.SplitIfNecesary(Value, "\"", "\"&", ScriptSCL.MaxLineLengh);
                result = string.Format("CONSTANT {0} = {1}\"\n", Name, aux);
            }
            return result;
        }
    }
}
