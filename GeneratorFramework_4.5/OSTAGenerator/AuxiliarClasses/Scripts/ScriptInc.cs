using System;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts
{
    internal class ScriptInc : ScriptSCL
    {
        public ScriptInc(string name,string autor, Repository repo):base(name,autor,repo)
        {
        }

        public new void Save(string folder)
        {
            throw new NotImplementedException();
        }

        public override void AddVariable(Variable var)
        {
            throw new NotImplementedException();
        }

        public override Variable GetVariable(string varName)
        {
            return null;
        }

        public override void AddRequest(Fiddler.Session req, string stepName)
        {
            throw new NotImplementedException();
        }

        public override void AddConstant(string name, string value)
        {
            if (Constants.ContainsKey(name)) return;

            Constants.Add(name, value);
            Sections.Add(new ConstantDeclarationSection(name, value));
        }

        public override string GetEndCode()
        {
            return string.Empty;
        }
    }
}
