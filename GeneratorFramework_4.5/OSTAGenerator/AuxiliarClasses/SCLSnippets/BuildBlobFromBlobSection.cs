namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class BuildBlobFromBlobSection : ISCLSections
    {
        public Variable Blob { get; set; }

        public BuildBlobFromBlobSection(Variable blob)
        {
            Blob = blob;
        }

        public string WriteCode()
        {
            return
                @"	BUILD AUTHENTICATION BLOB	&
		        FOR NTLM	&
		        FROM BLOB " + Blob.Name + @"	&
		        INTO " + Blob.Name;
        }
    }
}