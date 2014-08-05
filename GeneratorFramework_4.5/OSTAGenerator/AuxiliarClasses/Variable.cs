namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses
{
    internal class Variable
    {
        public VariablesScopes Scope { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public Variable(string name, string type, VariablesScopes scope)
        {
            Name = OpenSTAUtils.RenameVariableIfNeccesary(name);
            Type = type;
            Scope = scope;
        }
    }
}
