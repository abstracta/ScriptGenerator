using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts
{
    internal abstract class ScriptSCL : IScript
    {
        public static string ExtensionSCL = ".HTP";
        public static int MaxLineLengh = 100;

        public Comment InitialComments { get; set; }
        public Repository OpenSTARep { get; set; }
        public IList<ISCLSections> Sections { get; set; }

        public string Path { get; set; }
        public string Name { get; set; }
        public string AuthorName { get; private set; }
        public string CreationDate { get; private set; }
        public Dictionary<string, DataFile> DataFiles { get; set; }
        public Dictionary<string, string> Constants = new Dictionary<string, string>();
        public Dictionary<string, ParametrizedValue> ParametrizedValues { get; set; }

        protected ScriptSCL(string name, string authorName, Repository repo)
        {
            //recibe en el parámetro folder la ruta al directorio Scripts
            //entonces crea el script scl de nombre dado en el directorio folder
            OpenSTARep = repo;
            Name = name;
            AuthorName = authorName;
            Sections = new List<ISCLSections>();
            DataFiles = new Dictionary<string, DataFile>();
            ParametrizedValues = new Dictionary<string, ParametrizedValue>();

            CreationDate = DateTime.Today.ToShortDateString();

            InitialComments = new Comment("");
            Sections.Add(InitialComments);
        }

        public abstract void AddVariable(Variable var);

        /// <summary>
        /// Devuelve la variable parametrizada cuyo nombre coincide con varName
        /// </summary>
        /// <param name="varName"></param>
        /// <returns></returns>
        public abstract Variable GetVariable(string varName);

        public abstract void AddConstant(string name, string value);

        public abstract void AddRequest(Fiddler.Session req, string stepName);

        public abstract string GetEndCode();

        public void AddStartTimer(string timerName)
        {
            //agrega un timer en un nuevo paso (nodo de GXtest)
            Sections.Add(new StartTimer(timerName));
        }

        public void AddEndTimer(string timerName)
        {
            //agrega un timer en un nuevo paso (nodo de GXtest)
            //antes pone el disconnect all y luego pone el thinktime sólo si no se está haciendo debug
            Sections.Add(new EndTimer(timerName));
        }

        public void AddComment(string comment)
        {
            // agrega un comentario que tenga el nodo en el TC de GXtest
            Sections.Add(new Comment(comment));
        }

        public void AddDataFile(string fileName, string fileColmumn, Variable variable, string value)
        {
            //me fijo si ya exite el arcihvo 
            if (!DataFiles.ContainsKey(fileName))
            {
                DataFiles.Add(fileName, new DataFile(fileName));
            }

            DataFiles[fileName].AddColumn(fileColmumn, variable, value);
        }

        public void Save(string folder)
        {
            if (!folder.EndsWith("\\"))
            {
                folder = folder + "\\";
            }

            //ahora construyo el string
            var code = Sections.Aggregate("", (current, sec) => current + sec.WriteCode());

            code += GetEndCode();
            var file = new StreamWriter(folder + Name);

            file.Write(code);
            file.Close();
        }

        public void AddString(string s)
        {
            Sections.Add(new PlainCode(s));
        }

        internal void AddDataFile(DataFile df)
        {
            if (!DataFiles.ContainsKey(df.Name))
            {
                DataFiles.Add(df.Name, df);
            }
        }

        //public void AddValidation(string toValidate, string stepName)
        //{
        //    //agregar la rutina a los includes validateText.inc
        //    // como se hace? se invoca luego del llamado al pedido sobre el cual se quiere validar
        //    //o sea, antes de los pedidos secundarios y después del follow redirects

        //    addLoadInfo("Buffer", BODY);
        //    string line = string.Format("Set StepName = {0} \n" +
        //                                "Set expectedResponse = {1} \n\n" +
        //                                "Include \"validateText.inc\"\n", stepName, toValidate);

        //}

        internal void AddLogResponse(int conectionID)
        {
            throw new NotImplementedException();
        }

        internal void AddThinkTime(int p)
        {
            Sections.Add(new ThinkTimeSection(p));
        }
        
        internal void ParametrizeValueInSubsecuentsRequest(string elementName, string elementValue, Variable elementVariable)
        {
            if (!ParametrizedValues.ContainsKey(elementName))
            {
                ParametrizedValues.Add(elementName, new ParametrizedValue(elementName, elementValue, elementVariable));
            }
        }

        internal void AddScriptReference(ScriptSCL script)
        {
            Sections.Add(new IncludeSecondaryScript(script.Path));
        }

        internal string GetUsedDataFriendly()
        {
            var result = "";
            foreach (var df in DataFiles.Values)
            {
                foreach (var dfc in df.Columns.Values)
                {
                    result += OpenSTAUtils.RemoveSpecialCharacters(dfc.Name) + ":\"+" + dfc.VariableName + "+\", ";
                }
            }

            result = result.Length > 0
                         ? result.Substring(0, result.Length - "+\", ".Length)
                         : "\"";

            return result;
        }
    }
}