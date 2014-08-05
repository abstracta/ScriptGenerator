namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class VariableDeclaration : ISCLSections 
    {
        public Variable Variable { get; set; }
        public VariableDeclaration(Variable var)
        {
            Variable = var;
        }
    
        public string  WriteCode()
        {
            switch (Variable.Scope)
            {
                case VariablesScopes.Local:
                    return string.Format("\t {0} \t {1} \n", Variable.Type, Variable.Name);

                case VariablesScopes.File:
                    return string.Format("\t {0} \t {1} , FILE = \"{1}\", script\n", Variable.Type, Variable.Name);

                default:
                    return string.Format("\t {0} \t {1},{2} \n\n", Variable.Type, Variable.Name, Variable.Scope);
            }
        }
    }
}
