using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets;
using System.IO;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts
{
    internal class MainScriptSCL : ScriptSCL
    {
        /*
         * Tiene toda la sintaxis del OpenSTA.
         * se puede consultar en www.opensta.org/docs/sclref/
         */
         
        private int _lastIncludedScript;

        public CodeSection Code { get; set; }

        public HeaderSection Header { get; set; }

        // blacklist de hosts para dejar afuera
        internal static List<string> Hosts;

        // estructura para guardar los hosts que se descartaron y los números de pedido en los que aparecen
        internal static Dictionary<string, List<int>> Descartados = null;
                      
        static MainScriptSCL ()
        {
            LeerArchivoHosts();
        }

        public MainScriptSCL(string name, string dsc, string authorName, Repository repo): base(name, authorName,repo)
        {
            InitialComments.CommentStr = string.Format("Browser:IE5 \n" + "!Date:{0}\n", CreationDate);
             
            Sections.Add( new EnviromentSection(dsc));
            Header = new HeaderSection();
            Sections.Add(Header);
            Code = new CodeSection();
            Sections.Add(Code);

            AddVariable(new Variable(Name.Replace(".htp", ""), "Timer", VariablesScopes.Local));
            Sections.Add(new StartTimer(Name.Replace(".htp","")));
        }
        
        public override sealed void AddVariable(Variable var)
        {
            Header.AddVariable(var);
        }

        public override Variable GetVariable(string varName)
        {
            return Header.GetVariable(varName);
        }
        
        public override void AddRequest(Fiddler.Session req, string stepName)
        {
            // NP 12/07/2013
            // Dejo afuera los pedidos de Google Analytics (movido para la función filtrarPedidos para poder notificar los descartes)
            // NP 27/01/2014
            // Dejo afuera los pedidos de la blacklist

            if (FiltrarPedidos(req))
            {
                return;
            }

            /*if (req.oRequest.headers.Exists("Cookie"))
            {
                string[] separador = { "Cookie: " };
                string[] cookies = req.oRequest.headers.ToString().Split(separador, StringSplitOptions.None);
                separador[0] = "; ";
                foreach (string cookie in cookies[1].Split(separador, StringSplitOptions.None))
                {
                    if (cookie.StartsWith("__utm")) return;
                }
            }*/

            var isPrimaryReq = SessionUtils.IsPrimaryReq(req);
            var reqCode = new RequestSection(req, this,stepName, ParametrizedValues, isPrimaryReq);
            if (isPrimaryReq)
            {
                Sections.Add(reqCode);
            }
            else
            {
                //agrego un include para poner el pedido secundario
                var lastIncludedSection = Sections[Sections.Count - 1] as IncludedScriptSection;
                if (lastIncludedSection == null)
                {
                    lastIncludedSection = new IncludedScriptSection(Name.Replace(".htp","") + "_" + _lastIncludedScript, OpenSTARep,this);
                    _lastIncludedScript++;
                    Sections.Add(lastIncludedSection);
                }

                lastIncludedSection.AddRequest(req,stepName);
            }
        }

        public override string GetEndCode()
        {
            var agregarHacerLogin = false;

            //creo los archivos de datos
            foreach (var df in DataFiles.Values)
            {
                //creo la variable para el archivo

                var fileVar = new Variable(df.Name, "CHARACTER*128", VariablesScopes.File);
                var fileTempVar = new Variable("v_" + df.Name, "CHARACTER*2024", VariablesScopes.Local);
                AddVariable(fileVar);
                AddVariable(fileTempVar);

                //creo las variables para las columnas
                var columnVariables = new List<Variable>();
                foreach (var dfc in df.Columns.Values)
                {
                    var varDFC = new Variable(dfc.Name, "CHARACTER*128", VariablesScopes.Local);
                    columnVariables.Add(varDFC);
                    AddVariable(varDFC);
                }

                string accesData;
                if (df.Columns.Count == 1)
                {
                    accesData = string.Format("\nACQUIRE LOCAL MUTEX \"{0}\"\n" +
                                              "\tNEXT {1}\n" +
                                              "\tSET {2} = {1}\n" +
                                              "RELEASE MUTEX \"{0}\"\n", Name, fileVar.Name, fileTempVar.Name);

                }
                else
                {
                    accesData = string.Format("\nACQUIRE LOCAL MUTEX \"{0}\"\n" +
                                              "\tNEXT {1}\n" +
                                              "\tSET {2} = {1}\n" +
                                              "RELEASE MUTEX \"{0}\"\n\n", Name, fileVar.Name, fileTempVar.Name);
                    accesData += "Set F_Separador	= \"" + DataFile.Separator + "\"\n";
                    accesData += "Set F_Entrada	= " + fileTempVar.Name + "\n";
                    accesData += "call ParseString \n";

                    var index = 0;
                    foreach (var varCol in columnVariables)
                    {
                        accesData += "Set " + varCol.Name + " = Resultado[" + index + "]\n";
                        index++;
                    }

                    accesData += "\n";
                    //!set F_Separador	= ";"
                    //!set F_Entrada	= idCentro
                    //!call ParseString 
                    //!set idCentro = Resultado[0]
                    //!set idSEjec = Resultado[1]
                    //!set idAlmacen = Resultado[2]
                    //!set idMaterial = Resultado[3]
                }

                if (fileVar.Name.Contains("Login"))
                {
                    var loginDataIf = string.Format("next hacerlogin\n" +
                                                    "if (hacerlogin = 1) then\n");
                    var loginDataEndIf = string.Format("\nendif\n");
                    Code.AppendText(loginDataIf);
                    Code.AppendText(accesData);
                    Code.AppendText(loginDataEndIf);
                    agregarHacerLogin = true;
                }
                else 
                {
                    Code.AppendText(accesData);
                }

                df.Write(OpenSTARep.DataPath);
            }

            Sections.Add(new EndTimer(Name.Replace(".htp", "")));
            if (agregarHacerLogin)
                return 
                     string.Format("Exit \n\n" + 
                                "ERR_LABEL: \n" +
                                "\t reset hacerlogin \n" +
                                "\t If (MESSAGE <> \"\") Then \n" +
                                "\t\t Report MESSAGE \n" +
                                "\t Endif \n\n" +
                                "Exit \n" +
                                "Include \"{0}\"", Repository.FUNCTIONS);
            return
                string.Format("Exit \n\n" +
                              "ERR_LABEL: \n" +
                              "\t If (MESSAGE <> \"\") Then \n" +
                              "\t\t Report MESSAGE \n" +
                              "\t Endif \n\n" +
                              "Exit \n" +
                              "Include \"{0}\"", Repository.FUNCTIONS);
        }

        public override void AddConstant(string name, string value)
        {
            if (Constants.ContainsKey(name))
            {
                return;
            }

            Constants.Add(name, value);
            Header.AddConstant(new ConstantDeclarationSection(name, value));
        }

        #region búsqueda de hosts desde archivo
        // NP - 27/01/2014
        // Se implementan funciones para cargar una lista de hosts de un archivo Hosts.txt ubicado en el directorio de la dll,
        // para realizar búsquedas de dichos hosts en el session request recibido, y para escribir un mensaje con los pedidos descartados.
        internal static string NotificarDescartados()
        {
            if (Descartados == null || Descartados.Count == 0)
            {
                return string.Empty;
            }

            var res = "Se descartaron los pedidos a estos hosts, correspondientes a los siguientes ids de fiddler:\n";
            foreach (var pair in Descartados)
            {
                res += pair.Key + ": ";
                res += string.Join(", ", pair.Value.Select(i => i.ToString(CultureInfo.CurrentCulture)).ToArray());
                res += "\n";
            }

            Descartados = null;

            return res;
        }

        private static bool FiltrarPedidos(Fiddler.Session req)
        {
            if (req.HostnameIs("www.google-analytics.com"))
            {
                if (Descartados == null)
                {
                    Descartados = new Dictionary<string, List<int>>();
                }

                if (Descartados.ContainsKey("www.google-analytics.com"))
                {
                    Descartados["www.google-analytics.com"].Add(req.id);
                }
                else
                {
                    Descartados.Add("www.google-analytics.com", new List<int> { req.id });
                }

                return true;
            }

            foreach (var host in Hosts.Where(host => req.hostname.EndsWith(host)))
            {
                if (Descartados == null)
                {
                    Descartados = new Dictionary<string, List<int>>();
                }

                if (Descartados.ContainsKey(host))
                {
                    Descartados[host].Add(req.id);
                }
                else
                {
                    Descartados.Add(host, new List<int> { req.id });
                }

                return true;
            }

            return false;
        }

        private static void LeerArchivoHosts()
        {
            Hosts = new List<string>();

            var pathToFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Fiddler2\\Scripts\\Hosts.txt";

            if (!File.Exists(pathToFile))
            {
                return;
            }

            using (var file = new StreamReader(pathToFile))
            {
                string linea;
                while ((linea = file.ReadLine()) != null)
                {
                    Hosts.Add(linea);
                }
            }
        }

        #endregion
    }
}
