using System.Collections.Generic;
using System.IO;
using System.Linq;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class IncludedScriptSection : ISCLSections 
    {
        public string Name { get; set; }

        Repository _rep;

        public ScriptSCL ParentScript { get; set; }

        readonly List<ISCLSections> _sections = new List<ISCLSections>();

        public IncludedScriptSection(string name, Repository repo, ScriptSCL mainScript)
        {
            Name = name;
            _rep = repo;
            ParentScript = mainScript;
            _sections.Add(new PlainCode("!Browser:IE5 \n"));
            _sections.Add(new PlainCode("if (debug=\"0\") then\n"));
        }

        public string WriteCode()
        {
            _sections.Add(new PlainCode("endif"));
            var includedFile = _sections.Aggregate("", (current, sec) => current + sec.WriteCode());

            var largo = ParentScript.Name.Length;
            var folderName = ParentScript.Name.Replace(".htp", "");
            var fileFolderPath = ParentScript.OpenSTARep.IncludePath + "\\" + folderName + "\\";
            if (!Directory.Exists(fileFolderPath))
            {
                Directory.CreateDirectory(fileFolderPath);
            }

            var filePath = fileFolderPath + Name + ".htp";
            var file = new StreamWriter(filePath);
            file.Write(includedFile);
            file.Close();
            return "include \"" + ParentScript.Name.Replace(".htp", "") + "/" + Name + ".htp\"\n";
        }

        public void AddRequest(Fiddler.Session req, string stepName)
        {
            bool isPrimary = SessionUtils.IsPrimaryReq(req);
            var reqCode = new RequestSection(req, ParentScript, stepName, ParentScript.ParametrizedValues, isPrimary);
            _sections.Add(reqCode);
        }
    }
}
