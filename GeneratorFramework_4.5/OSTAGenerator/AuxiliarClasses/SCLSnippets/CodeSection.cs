namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class CodeSection : ISCLSections
    {
        //string code = string.Format("Code \n" +
        //                            "\t !Read in the default browser user agent field \n" +
        //                            "\t Entry[USER_AGENT,USE_PAGE_TIMERS] \n");
        #region ISCLSections Members


        public string WriteCode()
        {
            return string.Format("Code \n" +
                                    "\t !Read in the default browser user agent field \n" +
                                    "\t Entry[USER_AGENT,USE_PAGE_TIMERS] \n\n{0}",appendedText);
        }

        #endregion

        private string appendedText = "";
        internal void AppendText(string p)
        {
            appendedText += p;
        }
    }
}
