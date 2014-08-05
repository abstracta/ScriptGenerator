namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class AppearTextSection : ISCLSections
    {
        #region ISCLSections Members

        public string Text { get; set; }
        public int ConnectionID { get; set; }
        private string _code = "";
        public string StepName { get; set; }
        public bool Negation { get; set; }
        public bool Body { get; set; }

        public AppearTextSection(string text, int connectionID, string stepName)
        {
            Text = text;
            ConnectionID = connectionID;
            StepName = stepName;
            Negation = false;
            Body = true;
        }

        public string WriteCode()
        {
            var negationStr = Negation ? "Not" : "";
            var source = Body ? "Body" : "Header";
            var sourceFriendly = Body ? "Page" : "Header";

            _code = string.Format("!-----------------------------------------------------------\n" +
                                  "!============ Check if text: " + Text + " appears in {5}--------\n" +
                                  "!-----------------------------------------------------------\n" +
                                  "Set buffer=''\n" +
                                  "Set StepName = \"{0} \n" +
                                  "Set expectedResponse = \"{1}\" \n" +
                                  "Load Response_Info {4} ON {2} Into buffer\n" +
                                  "Include \"{3}AppearText.inc\"\n" +
                                  "!-----------------------------------------------------------\n",
                                  StepName, Text, ConnectionID, negationStr, source, sourceFriendly);

            return _code;
        }

        #endregion
    }
}
