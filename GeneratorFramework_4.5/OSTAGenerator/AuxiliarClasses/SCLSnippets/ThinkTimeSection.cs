namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class ThinkTimeSection : ISCLSections
    {
        public int Seconds { get; set; }

        public ThinkTimeSection(int seconds)
        {
            Seconds = seconds;
        }

        public string WriteCode()
        {
            //thnik time condicionado a debug
            return string.Format("if (debug=\"0\") then \n\tWait {0}\nendif\n", Seconds);
        }
    }
}
