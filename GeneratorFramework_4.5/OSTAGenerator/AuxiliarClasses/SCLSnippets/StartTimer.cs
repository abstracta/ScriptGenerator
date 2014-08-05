namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class StartTimer : ISCLSections
    {
        public string TimerName { get; set; }

        public StartTimer(string timerName)
        {
            TimerName = OpenSTAUtils.RenameVariableIfNeccesary(timerName);
        }

        public string WriteCode()
        {
            return string.Format("\nStart Timer {0}\n\n", TimerName);
        }
    }
}
