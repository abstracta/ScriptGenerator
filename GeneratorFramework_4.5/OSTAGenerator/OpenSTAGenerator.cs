using System;
using System.Collections.Generic;
using System.IO;
using Abstracta.Generators.Framework.AbstractGenerator;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts;
using GxTest.Utils.EnumTypes;

namespace Abstracta.Generators.Framework.OSTAGenerator
{
    internal class OpenSTAGenerator : AbstractGenerator.AbstractGenerator
    {
        private Repository Rep { get; set; }

        private MainScriptSCL MainScript { get; set; }

        public new void Initialize(string outPath, string mainScriptName, string serverName, string webAppName)
        {
            base.Initialize(outPath, mainScriptName, serverName, webAppName);

            // inicializo el repositorio del opensta
            Rep = new Repository(outPath, mainScriptName);
            MainScript = Rep.MainScl;
        }

        internal override void AddDataPools(List<DataPool> dataPools, string dataPoolFilesPath)
        {
            foreach (var dataPool in dataPools)
            {
                #region leo los archivos referenciados

                var data = new List<string>();
                using (var reader = new StreamReader(dataPoolFilesPath + dataPool.FileName))
                {
                    string record;
                    while ((record = reader.ReadLine()) != null)
                    {
                        data.Add(record);
                    }
                }

                #endregion

                #region leo nombre de columnas

                IList<KeyValuePair<string, Variable>> columnsNames = new List<KeyValuePair<string, Variable>>();
                foreach (var columnName in dataPool.Columns())
                {
                    var columnVariable = new Variable(columnName, "CHARACTER*128", VariablesScopes.Local);
                    columnsNames.Add(new KeyValuePair<string, Variable>(columnName, columnVariable));

                    MainScript.AddVariable(columnVariable);
                }

                #endregion

                var df = new DataFile(dataPool.Name, columnsNames, data);
                MainScript.AddDataFile(df);
            }
        }

        internal override AbstractStep AddStep(string name, string type, string description, ScriptGenerator generator, int index)
        {
            var tmp = generator.ServerName.Split(':');

            string servName, servPort;
            if (tmp.Length == 1)
            {
                servName = generator.ServerName;
                servPort = Constants.HTTPConstants.DefaultPortStr;
            }
            else
            {
                servName = tmp[0];
                servPort = tmp[1];
            }

            var newStep = new Step
            {
                Name = name,
                Type = (CommandType)Enum.Parse(typeof(CommandType), type),
                Desc = description,
                ServerName = servName,
                ServerPort = servPort,
                WebApp = generator.WebAppName,
                Index = index,
            };

            AddStep(newStep);

            return newStep;
        }

        internal override void Save()
        {
            throw new NotImplementedException();
        }
    }
}
