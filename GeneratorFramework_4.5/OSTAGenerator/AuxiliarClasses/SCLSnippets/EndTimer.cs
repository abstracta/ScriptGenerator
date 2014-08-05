namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class EndTimer : ISCLSections
    {
        public string TimerName { get; set; }

        public EndTimer(string timerName)
        {
            TimerName = OpenSTAUtils.RenameVariableIfNeccesary(timerName);
        }

        public string WriteCode()
        {
            return string.Format("Disconnect All\nEnd Timer {0}\n", TimerName);
        }
    }
}