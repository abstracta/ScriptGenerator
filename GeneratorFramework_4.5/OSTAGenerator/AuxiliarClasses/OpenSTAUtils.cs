using System.Text.RegularExpressions;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses
{
    internal class OpenSTAUtils
    {
        public static string SplitCommentIfNecesary(string str)
        {
            return SplitIfNecesary(str, "!", "", ScriptSCL.MaxLineLengh);
        }

        public static string SplitIfNecesary(string str, string startLine, string endLine, int largo)
        {
            var result = string.Empty;

            // tengo que partir el script mientras el largo sea mayor a la linea
            var firstTime = true;

            // si el largo del string original es uno más que el largo máximo admitido y termina en "\n" , no es necesario entrar al while
            if ((str.Length == largo + 1) && (str.EndsWith("\n")))
            {
                return str;
            }

            while (str.Length > largo)
                // NP - 13/5/2013
                // ojo, si el largo del string es uno más que el largo máximo admitido, el string arranca con "\n<startLine>", y no contiene más "\n" que el del principio, explota al hacer el substring en la variable "medio"
            {
                if (str.StartsWith("\n"))
                {
                    str = str.Substring(1);
                }

                var corte = largo;

                // NP - 14/5/2013
                // en teoría este if lo arregla (lo de adentro antes se ejecutaba siempre)
                if (str.Length != largo)
                {
                    if (str.StartsWith(startLine))
                    {
                        corte = largo + startLine.Length;
                    }

                    if (str.Contains("\n"))
                    {
                        corte = System.Math.Min(str.IndexOf("\n", System.StringComparison.Ordinal), corte);
                    }
                }

                string inicial, medio, final;

                inicial = string.Empty;
                medio = str.Substring(0, corte);

                if ((!firstTime) && (!medio.StartsWith(startLine)))
                {
                    inicial = startLine;
                }

                if (medio.EndsWith(endLine))
                {
                    final = "\n";
                }
                else
                {
                    final = endLine + "\n";
                }

                firstTime = false;

                //me fijo si el medio termina con una variable
                for (var i = medio.Length - 1; i > 0; i--)
                {
                    if (medio[i] == '"')
                    {
                        if (i == medio.Length - 1)
                            final = "&\n";
                        break;
                    }

                    if (medio[i] == '+')
                    {
                        if (i > 0)
                        {
                            if (medio[i - 1] == '"')
                            {
                                corte = i;
                                medio = medio.Substring(0, i - 1);
                                firstTime = true;
                                break;
                            }
                        }
                    }
                }

                result += inicial + medio + final;
                str = str.Substring(corte);
            }

            if (str.StartsWith("\n"))
            {
                str = str.Substring(1);
            }

            if ((!str.StartsWith(startLine)) && (!string.IsNullOrEmpty(result)))
            {
                if (!firstTime)
                {
                    // si empieza con una variable hay que sacarle las comillas
                    if ((str.StartsWith("+")) && (TieneCantidadParDeComillas(str)))
                    {
                        startLine = startLine.TrimEnd('\"');
                        str = startLine + str;
                    }
                    else
                        str = startLine + str;
                }
            }

            result += str;
            if (result.Contains("\n"))
            {
                if (result.StartsWith(startLine))
                {
                    result = result.Substring(startLine.Length);
                }
            }

            return result;
        }

        public static string SplitStringIfNecesary(string str, int largo)
        {
            return SplitIfNecesary(str, "\"", "\"&", largo);
        }

        public static string SplitStringIfNecesary(string str)
        {
            return SplitIfNecesary(str, "\"", "\"&", ScriptSCL.MaxLineLengh);
        }

        public static string SplitStringIfNecesary(string str, string identation)
        {
            return SplitIfNecesary(str, identation + "\"", "\"&", ScriptSCL.MaxLineLengh);
        }

        internal static string RenameVariableIfNeccesary(string name)
        {
            /* An OpenSTA Dataname has between 1 and 16 characters. 
             * These characters may only be alphanumerics, underscores, or hyphens. 
             * The first character must be alphabetic, no spaces, no double underscores or hyphens, 
             * and no trailing underscore or hyphen.
             */

            var result = name.Trim();
            if (result == "ASP.NET_SessionId")
            {
                result = "NET_SessionId";
            }
            else
            {
                // NP - 13/01/2014
                // se come todos los caracteres no alfabéticos del principio, y si queda vacío le pone el nombre genércio "variable" y la hora actual de minutos a milisegundos
                while (result.Length > 0 && !char.IsLetter(result[0]))
                {
                    result = result.Substring(1);
                }

                if (result.Length == 0)
                {
                    result = "variable" + System.DateTime.Now.ToString("mmssFFF");
                }
                else if (result.Length > 16)
                {
                    result = result.Substring(0, 16);
                }
            }

            result = RemoveSpecialCharacters(result);

            return result.Replace(" ", "");
        }

        // Quita los caracteres que no son letras o números
        public static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }

        // Devuelve true si 'str' contiene una cantidad par de comillas
        public static bool TieneCantidadParDeComillas(string str)
        {
            var cant = 0;
            for (var i = 0; i < str.Length - 1; i++)
            {
                if (str[i] == '\"')
                {
                    cant++;
                }
            }

            return cant%2 == 0;
        }
    }
}
