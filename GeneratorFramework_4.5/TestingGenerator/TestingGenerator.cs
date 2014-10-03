using System;
using System.Collections.Generic;
using System.IO;
using Abstracta.Generators.Framework.AbstractGenerator;
using GxTest.Utils.EnumTypes;

namespace Abstracta.Generators.Framework.TestingGenerator
{
    internal class TestingGenerator : AbstractGenerator.AbstractGenerator
    {
        internal override void AddDataPools(List<DataPool> dataPools, string dataPoolFilesPath)
        {
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
            using (var fw = new StreamWriter(HomeFolder + "ProcessingResult.txt"))
            {
                foreach (var step in Steps)
                {
                    fw.WriteLine(step.ToString());
                }
            }
        }
    }
}