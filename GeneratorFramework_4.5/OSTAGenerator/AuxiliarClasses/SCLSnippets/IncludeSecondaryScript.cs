namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class IncludeSecondaryScript : ISCLSections
    {
        public string PathScript { get; set; }
        public string NameScript { get; set; }

        public IncludeSecondaryScript(string pathScript)
        {
            PathScript = pathScript;
            NameScript =
                pathScript.Substring(pathScript.LastIndexOf("\\"), pathScript.Length - pathScript.LastIndexOf("\\"))
                          .Replace("\\", "");
        }

        public string WriteCode()
        {
            return string.Format("Include \"{0}\" \n", NameScript);
        }
    }
}
