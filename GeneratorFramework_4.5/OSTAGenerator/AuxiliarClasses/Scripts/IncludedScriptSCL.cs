using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts
{
    internal class IncludedScriptSCL : ScriptSCL
    {
        int _lastIncludedScript;
        readonly MainScriptSCL _script;
                 
        public IncludedScriptSCL(string name, string author, Repository repo, MainScriptSCL script)
            : base(name, author, repo)
        {
            _script = script;
        }

        public override void AddVariable(Variable var)
        {
            _script.AddVariable(var);
        }

        public override Variable GetVariable(string varName)
        {
            return _script.GetVariable(varName);
        }

        public override void AddRequest(Fiddler.Session req, string stepName)
        {
            var isPrimary = SessionUtils.IsPrimaryReq(req);
            var reqCode = new RequestSection(req,this,stepName,ParametrizedValues, isPrimary);
            if (isPrimary)
            {
                Sections.Add(reqCode);
            }
            else
            { 
                //Agrego un "include" para poner el pedido secundario
                var lastIncludeSection = Sections[Sections.Count - 1] as IncludedScriptSection;
                if (lastIncludeSection == null)
                {
                    lastIncludeSection = new IncludedScriptSection(Name.Replace(".htp", "") + "_" + _lastIncludedScript, OpenSTARep, this);
                    _lastIncludedScript++;
                    Sections.Add(lastIncludeSection);
                }

                lastIncludeSection.AddRequest(req, stepName);
            }
        }

        public override void AddConstant(string name, string value)
        {
            _script.AddConstant(name, value);
        }

        public override string GetEndCode()
        {
            return string.Empty;
        }
    }
}
