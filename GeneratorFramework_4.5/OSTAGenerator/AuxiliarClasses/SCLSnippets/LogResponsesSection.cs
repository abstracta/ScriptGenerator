namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class LogResponsesSection : ISCLSections
    {
        public int ConnectionID { get; set; }

        public LogResponsesSection(int connectionID)
        {
            ConnectionID = connectionID;
        }

        public string WriteCode()
        {
            /*
             * if (debug=1) then
                    set buffer = ""
             *      load Response_Info Body from Id into buffer
             *      log buffer
             *      set strHeader = ""
             *      log Response_Info header from Id into strHeader
             *      log strHeader
             * endif
            */

            return string.Format("\nIf (debug = \"1\") then \n" +
                                 "\t Load Response_Info Body ON {0} Into {1} \n" +
                                 "\t log {1} \n" +
                                 "\t Load Response_Info Header ON {0} Into {2} \n" +
                                 "\t log {2} \n" +
                                 "Endif \n\n", ConnectionID, "buffer", "strHeader");

        }
    }
}
