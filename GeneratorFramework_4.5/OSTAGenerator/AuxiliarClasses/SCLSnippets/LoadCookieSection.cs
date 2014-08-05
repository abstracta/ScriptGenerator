namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class LoadCookieSection : ISCLSections
    {
        public string CookieName { get; set; }
        public string CookieVar { get; set; }
        public int ConnID { get; set; }

        public LoadCookieSection(string cookieName, string cookieVarName, int connectionID)
        {
            CookieName = cookieName;
            CookieVar = cookieVarName;
            ConnID = connectionID;
        }

        public string WriteCode()
        {
            return string.Format("Load Response_Info Header ON {0} & \n" +
                                 "\t Into {1}&\n" +
                                 "\t ,WITH \"Set-Cookie,{2}\"",
                                 ConnID, CookieVar, CookieName);
        }
    }
}
