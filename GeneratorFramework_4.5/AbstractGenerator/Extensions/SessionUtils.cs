using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fiddler;

namespace Abstracta.Generators.Framework.AbstractGenerator.Extensions
{
    internal class SessionUtils
    {
        private static readonly string ExtensionsFile =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
            "\\Fiddler2\\Scripts\\Extensiones.txt";

        private static readonly string CookiesFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                                     "\\Fiddler2\\Scripts\\Extensiones.txt";

        // whitelist de extensiones para pedidos primarios
        internal static List<string> Extensiones = null;

        // blacklist de cookies para dejar hardcodeadas
        internal static List<string> Cookies = null;

        private static readonly string[] PrimaryExtensions = new[]
            {".htm", ".html", ".aspx", ".ashx", ".php", "faces", "svc", ".svc/"};

        // private static readonly string[] SecondaryExtensions = new[] {".js", ".css", ".jpg", ".jpeg", ".png", ".gif"};

        internal static bool IsPrimaryReq(Session req)
        {
            // NP 22/08/2013 - Se agrega soporte para Java Server Faces
            // NP 29/11/2013 - Se agrega soporte para PHP y ashx
            // NP 17/01/2014 - Se agrega soporte pra .svc
            // NP 23/01/2014 - Se agrega la lectura opcional de un archivo en el que se pueden especificar
            // nuevas extensiones a ser consideradas pedidos primarios.
            var uri = new Uri(req.fullUrl);

            return (PrimaryExtensions.Any(primaryExtension => uri.LocalPath.EndsWith(primaryExtension))
                    || BuscarEnArchivoExtensiones(uri.LocalPath)
                    || uri.LocalPath.Split('.').Length == 1)
                    || PrimaryExtensions.Any(primaryExtension => uri.LocalPath.Contains(primaryExtension))
                   && !uri.LocalPath.Contains("GXResourceProv");
        }

        // NP - 23/01/2014
        // Se implementan funciones para cargar una lista de extensiones de un archivo Extensiones.txt ubicado en el directorio de la dll,
        // y para realizar búsquedas de dichas extensiones en el path de la url recibido.
        private static bool BuscarEnArchivoExtensiones(string path)
        {
            if (Extensiones == null)
            {
                LeerArchivoExtensiones();
            }

            return Extensiones != null && Extensiones.Any(path.EndsWith);
        }

        private static void LeerArchivoExtensiones()
        {
            var filePath = ExtensionsFile;

            Extensiones = new List<string>();
            if (!File.Exists(filePath)) return;

            using (var file = new StreamReader(filePath))
            {
                string linea;
                while ((linea = file.ReadLine()) != null)
                {
                    Extensiones.Add(linea);
                }
            }
        }

        // NP - 24/01/2014
        // Se implementan funciones para cargar una lista de cookies de un archivo Cookies.txt ubicado en el directorio de la dll,
        // y para realizar búsquedas de dichas cookies en el string de la cookie recibida.
        internal static bool BuscarEnArchivoCookies(string cookie)
        {
            if (Cookies == null)
            {
                LeerArchivoCookies();
            }

            return Cookies != null && Cookies.Any(cookie.Contains);
        }

        private static void LeerArchivoCookies()
        {
            var filePath = CookiesFile;

            Cookies = new List<string>();
            if (!File.Exists(filePath)) return;

            using (var file = new StreamReader(filePath))
            {
                string linea;
                while ((linea = file.ReadLine()) != null)
                {
                    Cookies.Add(linea);
                }
            }
        }
    }
}