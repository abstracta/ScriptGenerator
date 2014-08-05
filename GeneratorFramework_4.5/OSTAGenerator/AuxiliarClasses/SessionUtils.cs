using System;
using System.Collections.Generic;
using System.IO;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses
{
    class SessionUtils
    {
        // whitelist de extensiones para pedidos primarios
        internal static List<string> Extensiones = null;
        // blacklist de cookies para dejar hardcodeadas
        internal static List<string> Cookies = null;

        internal static bool IsPrimaryReq(Fiddler.Session req)
        {
            // NP 22/08/2013 - Se agrega soporte para Java Server Faces
            // NP 29/11/2013 - Se agrega soporte para PHP y ashx
            // NP 17/01/2014 - Se agrega soporte pra .svc
            // NP 23/01/2014 - Se agrega la lectura opcional de un archivo en el que se pueden especificar
            // nuevas extensiones a ser consideradas pedidos primarios.
            var uri = new Uri(req.fullUrl);
            return (uri.LocalPath.EndsWith(".html")
                || uri.LocalPath.EndsWith("aspx")
                || uri.LocalPath.EndsWith(".ashx")
                || uri.LocalPath.EndsWith(".php")
                || uri.LocalPath.EndsWith(".faces")
                || uri.LocalPath.EndsWith(".svc")
                || uri.LocalPath.Contains(".svc/")
                || BuscarEnArchivoExtensiones(uri.LocalPath)
                || uri.LocalPath.Split('.').Length==1)
                && !uri.LocalPath.Contains("GXResourceProv");
        }

        /// <summary>
        /// devleuve las cookies de manera amigable para imprimir 
        /// </summary>
        /// <param name="hTTPRequestHeaders"></param>
        /// <returns></returns>
        internal static string GetCookiesFriendly(Fiddler.HTTPRequestHeaders hTTPRequestHeaders)
        {
            var result = "Send Cookies= ";
            foreach (Fiddler.HTTPHeaderItem item in hTTPRequestHeaders)
            {
                if (item.Name.ToLower().Contains("cookie"))
                {
                    result += item.Value;
                }
            }

            return OpenSTAUtils.SplitCommentIfNecesary( result);
        }

        /// <summary>
        /// devleuve las cookies de manera amigable para imprimir 
        /// </summary>
        /// <param name="hTTPResponseHeaders"></param>
        /// <returns></returns>
        internal static string GetCookiesFriendly(Fiddler.HTTPResponseHeaders hTTPResponseHeaders)
        {
            var result = "Received Cookies= ";
            foreach (Fiddler.HTTPHeaderItem item in hTTPResponseHeaders)
            {
                if (item.Name.ToLower().Contains("set-cookie"))
                {
                    result += item.Value;
                }
            }

            return OpenSTAUtils.SplitCommentIfNecesary(result);
        }

        
        /// <summary>
        /// Devuelve un string con las cookies parametrizadas y agrega variables si es necesario
        /// </summary>
        /// <param name="hTTPRequestHeaders"></param>
        /// <param name="scriptSCL"></param>
        /// <returns></returns>
        internal static string GetCookies(Fiddler.HTTPRequestHeaders hTTPRequestHeaders, ScriptSCL scriptSCL)
        {
            const string inicial = "\t\"Cookie: ";
            var result = inicial;
            //llamar a OpenSTAUtils para que corte el stirng si es necesario
            foreach (Fiddler.HTTPHeaderItem item in hTTPRequestHeaders)
            {
                if (!item.Name.ToLower().Contains("cookie")) continue;

                var cookies = item.Value.Split(';');
                foreach (var cookie in cookies)
                {
                    if (cookie.Contains("__utm") || BuscarEnArchivoCookies(cookie))
                    {//cookies de analitics o blacklisteadas
                        result += cookie + ";";
                    }
                    else
                    {
                        var cookieName = cookie.Split('=')[0];

                        //string cookieValue = cookie.Split('=')[1];
                        var var = new Variable(cookieName, "CHARACTER*1024", VariablesScopes.Local);
                        scriptSCL.AddVariable(var);
                        result += "\"+" + var.Name+ "+\";";
                    }
                }
            }
            if (result.EndsWith("+\";"))
            {
                result = result.Substring(0, result.Length - "+\";".Length);
            }
            else // NP 15/07/2013 si termina con una cookie de google analytics le saco el ; final y le cierro las comillas
            {
                result = result.Substring(0, result.Length - 1);
                result += "\"";
            }
            if (result==inicial)
            {
                result =result+ "\"";
            }
            result = OpenSTAUtils.SplitStringIfNecesary(result, "\t\t\t");
            return result;
        }

        #region búsqueda de extensiones desde archivo
        // NP - 23/01/2014
        // Se implementan funciones para cargar una lista de extensiones de un archivo Extensiones.txt ubicado en el directorio de la dll,
        // y para realizar búsquedas de dichas extensiones en el path de la url recibido.
        private static bool BuscarEnArchivoExtensiones(string path)
        {
            if (Extensiones == null)
            {
                LeerArchivoExtensiones();
            }

            foreach (var ext in Extensiones)
            {
                if (path.EndsWith(ext))
                    return true;
            }

            return false;
        }

        private static void LeerArchivoExtensiones()
        {
            Extensiones = new List<string>();
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Fiddler2\\Scripts\\Extensiones.txt"))
            {
                var file = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Fiddler2\\Scripts\\Extensiones.txt");
                string linea;
                while ((linea = file.ReadLine()) != null)
                {
                    Extensiones.Add(linea);
                }

                file.Close();
            }
        }
        #endregion

        #region búsqueda de cookies desde archivo
        // NP - 24/01/2014
        // Se implementan funciones para cargar una lista de cookies de un archivo Cookies.txt ubicado en el directorio de la dll,
        // y para realizar búsquedas de dichas cookies en el string de la cookie recibida.
        private static bool BuscarEnArchivoCookies(string cookie)
        {
            if (Cookies == null)
            {
                LeerArchivoCookies();
            }

            foreach (string cook in Cookies)
            {
                if (cookie.Contains(cook))
                    return true;
            }

            return false;
        }

        private static void LeerArchivoCookies()
        {
            Cookies = new List<string>();
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Fiddler2\\Scripts\\Cookies.txt"))
            {
                var file = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Fiddler2\\Scripts\\Cookies.txt");
                string linea;
                while ((linea = file.ReadLine()) != null)
                {
                    Cookies.Add(linea);
                }
                file.Close();
            }
        }
        #endregion
    }
}
