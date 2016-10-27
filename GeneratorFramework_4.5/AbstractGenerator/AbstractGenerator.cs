using System.Collections.Generic;
using System.Linq;

namespace Abstracta.Generators.Framework.AbstractGenerator
{
    internal abstract class AbstractGenerator
    {
        protected List<AbstractStep> Steps;

        protected string ScriptName, HomeFolder, ServerName, WebAppName;
        protected bool IsBMScript, IsSecondary, IsBeanShell, IsGxApp;

        internal abstract void AddDataPools(List<DataPool> dataPools, string dataPoolFilesPath);

        internal abstract AbstractStep AddStep(string name, string type, string description, ScriptGenerator generator, int index);

        internal virtual void Initialize(string outPath, string mainScriptName, string serverName, string webAppName, bool isBMScript = false, bool isSecondary = true, bool isBeanShell = true, bool isGxApp = false)
        {
            HomeFolder = outPath;
            ScriptName = mainScriptName;
            ServerName = serverName;
            WebAppName = webAppName;
            IsBMScript = isBMScript;
            IsSecondary = isSecondary;
            IsBeanShell = isBeanShell;
            IsGxApp = isGxApp;

            Steps = new List<AbstractStep>();
        }

        protected virtual void AddStep(AbstractStep step)
        {
            Steps.Add(step);
        }

        internal virtual AbstractStep GetLastStep()
        {
            return Steps.Last();
        }

        internal abstract void Save();
    }
}
