namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class BuildBlobFromUserSection : ISCLSections 
    {
        public string User { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
        public int ConnectionID { get; set; }

        public static string AutenticationMode { get; set; }

        public BuildBlobFromUserSection(string user, string pwd, string domain, int connID)
        {
            User = user;
            Password = pwd;
            Domain = domain;
            ConnectionID = connID;
        }
    

        public string  WriteCode()
        {
// NP - 17/01/2014
// si AuthenticationMode es null le pongo Basic y salimos de esta ;)
            if (AutenticationMode == null)
                AutenticationMode = "Basic";
            return
   @"BUILD AUTHENTICATION BLOB&
	FOR "+AutenticationMode+@" &
	FROM USER " + User + " PASSWORD " + Password + " DOMAIN " + Domain + " &\nINTO blob1\n\n";
        }

}
}
