namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class Comment : ISCLSections
    {
        public string CommentStr { get; set; }

        public Comment(string comment)
        {
            CommentStr = comment;
        }

        public string WriteCode()
        {
            //fijarse que si el largo maximo es mayor al largo maximo de linea se tiene que poner en dos lineas
            //si es vacio no imprimir nada
            var aux = OpenSTAUtils.SplitCommentIfNecesary(CommentStr);
            if (!aux.StartsWith("!"))
            {
                aux = "!" + aux;
            }

            return aux;
        }
    }
}
