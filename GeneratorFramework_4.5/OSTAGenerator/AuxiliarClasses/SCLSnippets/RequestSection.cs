using System;
using System.Collections.Generic;
using System.Linq;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts;
using Fiddler;
using System.Text.RegularExpressions;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class RequestSection : ISCLSections
    {
        public static int ConnectionID = 1;
        public static bool FirstRequest { get; set; }
        public static Dictionary<string, int> PreviousRequestResponse { get; set; }
        public static Variable User { get; set; }
        public static Variable Pwd { get; set; }
        public static string PreviousRedirectURL { get; set; }
        public static int StepCounter = 1;

        public static int GetConnectionID(Session req)
        {
            if (!req.oRequest.bClientSocketReused)
            {
                ConnectionID++;
            }
            return ConnectionID;
        }

        public static DateTime LastResponseTime { get; set; }
        public Session Request { get; set; }
        public ScriptSCL Script { get; set; }
        private string _result = "";
        public string StepName { get; set; }
        public AppearTextSection Validation { get; set; }
        private readonly Dictionary<string, ParametrizedValue> _parametrizedValues;

        public RequestSection(Session req, ScriptSCL script, string stepName,
                              Dictionary<string, ParametrizedValue> parametrizedValues, bool isPrimary)
        {
            Request = req;
            Script = script;
            StepName = stepName;
            _parametrizedValues = parametrizedValues;
            Validation = null;
            _result = WriteCodeInt(isPrimary);
        }

        public string WriteCode()
        {
            if (Validation != null)
            {
                _result += Validation.WriteCode();
            }
            return _result;
        }

        private string WriteCodeInt(bool isPrimary)
        {
            var result = "";
            if (Request.oRequest.headers.HTTPMethod == "CONNECT")
            {
                return result;
            }

            if (FirstRequest)
            {
                FirstRequest = false;
            }
            else
            {
                //agrego un wait desde la respuesta anterior al pedido nuevo
                var delta = Request.Timers.ClientBeginRequest - LastResponseTime;
                if (delta.Milliseconds > 0)
                {
                    var pc = new PlainCode("\tWait " + delta.Milliseconds + "\n");
                    result += pc.WriteCode();

                }
            }

            LastResponseTime = Request.Timers.ServerDoneResponse;

            var reqComment = new Comment(string.Format("\nRequest Fiddler Index: {3}\n!Response Code: {0}\n!{1}\n!{2}",
                                                       Request.responseCode,
                                                       SessionUtils.GetCookiesFriendly(Request.oRequest.headers),
                                                       SessionUtils.GetCookiesFriendly(Request.oResponse.headers),
                                                       Request.id)
                );

            result += reqComment.WriteCode();

            //hay que ver cómo llevar los números de conexion (alguna variable interna en esta clase)            
            result += AddRequest(Request, isPrimary);

            return result;
        }

        public string AddRequest(Session req, bool primary)
        {
            var url = req.fullUrl;
            url = PreviousRedirectURL == req.fullUrl ? "redirectUrl+\"" : SplitAndParametrizeUrl(url, Script);

            //string cookies = SessionUtils.GetCookies(req.oRequest.headers, this.Script);
            var connectionID = GetConnectionID(Request);

            var primaryString = "";
            if (primary)
            {
                primaryString = "PRIMARY";
            }

            //tengo que saber cuando es get y cuando es post
            var isGet = req.oRequest.headers.HTTPMethod == "GET";

            var openSTAHeaders = GetOpenSTAHeaders();

            var result = "";
            if (isGet)
            {
                result += string.Format("\n\t{0} GET URI {1} HTTP/1.1\" ON {2} &\n\t{3}\n\n"
                                        , primaryString, url, connectionID, openSTAHeaders);
            }
            else
            {
                result += "\n\t" + primaryString + " POST URI " + url + " HTTP/1.1\" ON " + connectionID + " &\n\t" +
                          openSTAHeaders + "		 &\n" +
                          "\t,BODY \"" + SplitBody(Request.GetRequestBodyAsString()) + "\n\n";
            }

            //si hay cookies para cargar tengo que cargarlas
            result += LoadCookies(connectionID);
            if (primary)
            {
                var lrs = new LogResponsesSection(connectionID);
                result += lrs.WriteCode();

                switch (Request.responseCode)
                {
                    case 200:
                        //pongo el codigo para la validacion de texto
                        Validation = new AppearTextSection("<Text to validate>", connectionID,
                                                           OpenSTAUtils.SplitStringIfNecesary("Step " + StepCounter +
                                                                                              " - " +
                                                                                              Script.GetUsedDataFriendly
                                                                                                  ()));
                        StepCounter++;
                        //me fijo si es un pdf 
                        //Content-Type: application/pdf
                        if ((req.oResponse.headers.Exists("Content-Type")) &&
                            (req.oResponse.headers["Content-Type"] == "application/pdf"))
                        {
                            Validation.Body = false;
                            Validation.Text = "application/pdf";
                        }
                        break;

                    case 301:
                    case 302:
                    case 303:
                        //aca falta que se maneje el redirect para el proximo pedido
                        //cargo el campo location en una variable url y luego lo uso en el proximo pedido que coincida con la url
                        if (req.oResponse.headers.Exists("Location"))
                        {
                            PreviousRedirectURL = req.oResponse.headers["Location"];
                            var redVar = new Variable("redirectUrl", "CHARACTER*1024", VariablesScopes.Local);
                            Script.AddVariable(redVar);
                            result += "\tLoad Response_Info Header on " + connectionID + " into buffer\n";
                            result += "\tSet strInicial = 'Location: '\n";
                            result += "\tSet strFinal = '~<CR>'\n";
                            result += "\tcall between\n";
                            result += "\tSet redirectUrl = Straux\n\n";
                        }
                        break;

                    case 401:
                        //me fijo si es el primer 401 para este pedido entonces agrego un from user, sino si es el segundo agrego un from blob  
                        var blob = new Variable("blob1", "CHARACTER*2048", VariablesScopes.Local);
                        Script.AddVariable(blob);
                        if (
                            (PreviousRequestResponse.ContainsKey(req.fullUrl)) &&
                            (PreviousRequestResponse[req.fullUrl] == 401)
                            )
                        {
                            //me fijo si esta el campo WWW-Authenticate
                            if (req.oResponse.headers.Exists("WWW-Authenticate"))
                            {
                                result += string.Format(
                                    "\n\tLoad Response_Info Header on {0}		&\n" +
                                    "\tInto blob1	&\n" +
                                    "\t,WITH \"WWW-Authenticate\"\n\n", connectionID);
                            }
                            result += new BuildBlobFromBlobSection(blob).WriteCode();
                        }
                        else
                        {
                            var usrStr = "\"<user>\"";
                            var pwdStr = "\"<pwd>\"";
                            var domain = "\"\"";
                            if (User != null)
                            {
                                usrStr = User.Name;
                                pwdStr = Pwd.Name;
                            }
                            result += new BuildBlobFromUserSection(usrStr, pwdStr, domain, connectionID).WriteCode();
                        }
                        break;
                }
            }

            if (!PreviousRequestResponse.ContainsKey(req.fullUrl))
            {
                PreviousRequestResponse.Add(req.fullUrl, req.responseCode);
            }

            return result;
        }

        private string LoadCookies(int connID)
        {
            var cookies = new List<LoadCookieSection>();
            foreach (HTTPHeaderItem header in Request.oResponse.headers)
            {
                switch (header.Name.ToLower())
                {
                    case "set-cookie":
                        {
                            var cookie = header.Value.Split(';')[0].Split('=');
                            var cookieName = cookie[0];
                            //string cookieValue = "";
                            //if (cookie.Length>1)
                            //{

                            //    cookieValue = cookie[1];
                            //}
                            var var = new Variable(cookieName, "CHARACTER*1024", VariablesScopes.Local);
                            Script.AddVariable(var);
                            cookies.Add(new LoadCookieSection(cookieName, var.Name, connID));
                        }
                        break;

                    case "www-authenticate":
                        BuildBlobFromUserSection.AutenticationMode = header.Value.Contains("Basic") ? "Basic" : "NTLM";
                        break;
                }
            }

            return cookies.Aggregate("", (current, lcs) => current + (lcs.WriteCode() + "\n"));
        }

        private string SplitBody(string body)
        {
//para el gxstate tengo que hacer algo especial
            // NP - 16/01/2014
            // agrego esta movida para que no se rompa el body cuando trae comillas adentro
            body = body.Replace("\"", "\"+'\"'+\"");
            var result = "";
            var lastLine = "";
            var separatorStr = "&";
            var bodyParameters = body.Split('&'); //separo los parámetrosr
            foreach (var str in bodyParameters) //recorro los parámetros
            {
                if ((lastLine.Length + str.Length) < ScriptSCL.MaxLineLengh)
                    //me fijo si el param. y la linea anterior caben en una línea
                {
                    // NP - 16/01/2014
                    // agrego el if para no poner el & al principio del body, que rompe los formatos de los json.
                    if (result == lastLine)
                    {
                        lastLine += str;
                    }
                    else
                    {
                        lastLine += "&" + str; //vuelvo a poner el & que separa los parámetros que perdí en el split
                    }
                }
                else
                {
                    if (result == "")
                        // si result es vacío le agrego el parámetro y el & para el próximo parámetro, y pongo el cierre de linea
                    {
                        // NP - 16/01/2014
                        // agrego el if para no meter una línea con únicamente un ampersand al principio de los bodys cuando el 1er parámetro no cabe en una línea
                        if (lastLine != "")
                        {
                            result += lastLine + "&\"&\n";
                        }
                    }
                    else // si no es, armo la linea igual que en el otro caso pero empezando con tab y comillas
                    {
                        result += "\t\"" + lastLine + "&\"&\n";
                    }
                    lastLine = "";
                    if (str.Length > ScriptSCL.MaxLineLengh) //me fijo si el param. no cabe en la linea
                    {
                        if (str.StartsWith("GXState")) // si es el param. GXState lo separo por ;
                        {
                            //separo por %22%2C%22 es el ; codificado
                            string[] separator = {"%22%2C%22"};
                            separatorStr = separator[0]; //no encuentro donde se usa esta variable
                            var gxstateParameters = str.Split(separator, StringSplitOptions.None);
                            var firstGXparameter = true;
                            foreach (var strGx in gxstateParameters) // recorro los params de GXState
                            {
                                if ((lastLine.Length + strGx.Length) < ScriptSCL.MaxLineLengh)
                                {
// chequeo si cabe la linea anterior con el param nuevo en una linea
                                    if (firstGXparameter) // si es el 1er param. lo meto en last line de una
                                    {
                                        firstGXparameter = false;
                                        lastLine += strGx;
                                    }
                                    else // si no es el 1ero le agrego el separador antes de meterlo en last line
                                    {
                                        lastLine += separator[0] + strGx;
                                    }
                                }
                                else // si no cabe el param
                                {
// armo la línea como la voy a mostrar
                                    /*if (lastLine.StartsWith("GXState"))///////////////////////////////////////////////////
                                    {
                                        result += "\t\"" + lastLine + "\"&\n";
                                    }
                                    else
                                    {
                                        result += "\t\"" +   lastLine + "\"&\n";
                                    }
                                    else//                                      qué onda con esto?
                                    {*/
                                    result += "\t\"" + lastLine + "\"&\n";
                                    //}/////////////////////////////////////////////////////////////////////////////////////
                                    lastLine = ""; // empiezo una linea nueva
                                    if (strGx.Length > ScriptSCL.MaxLineLengh) // 
                                    {
                                        // NP 29/11/2013 - Agrego control para que no ponga el separador si es la primer variable del gxstate
                                        if (strGx.StartsWith("GXState="))
                                        {
                                            result += "\t\"" + OpenSTAUtils.SplitStringIfNecesary(strGx, "\t") + "\"&\n";
                                        }
                                        else
                                        {
                                            result += "\t\"" + separator[0] +
                                                      OpenSTAUtils.SplitStringIfNecesary(strGx, "\t") + "\"&\n";
                                        }
                                    }
                                    else
                                    {
                                        // idem arriba
                                        if (strGx.StartsWith("GXState="))
                                        {
                                            lastLine += strGx;
                                        }
                                        else
                                        {
                                            lastLine += separator[0] + strGx;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // NP - 16/01/2014
                            // Se agrega el control de si result es vacío, porque la función que invoca a esta ya pone las primeras comillas
                            if (result == "")
                            {
                                result += OpenSTAUtils.SplitStringIfNecesary(str) + "\"&\n";
                            }
                            else
                            {
                                /*se cambia lo de la izq para que arranque con " en lugar de & y se agrega
                                 * lo de la der para arreglar el bug del tab en el medio del body*/
                                // (Anónimo)
                                result += "\"" + OpenSTAUtils.SplitStringIfNecesary(str) + "\"&\n";
                            }
                        }
                    }
                    else
                    {
                        lastLine += str;
                    }
                }
            }
            // NP - 16/01/2014
            // saco el ampersand que se agregaba después de lastLine para que no se termine el body con un ampersand (por los json)
            // NP - 20/01/2014
            // agrego el if para que no quede una comilla de más cuando el body cabe todo en la 1er línea

            if (result == "")
            {
                result += lastLine + "\"\n";
            }
            else
            {
                result += "\t\"" + lastLine + "\"\n";
            }
            return ParametrizeBody(result);
        }


        private string ParametrizeBody(string body)
        {
            foreach (var par in _parametrizedValues)
            {
                var parameterRegex = new Regex(par.Value.HTMLElement.Replace("$", "").Replace("^", ""),
                                               RegexOptions.IgnoreCase);
                //Regex parameterRegex = new Regex("W0020W0008W0012vMOTIVO");
                //campos comunes
                var element = parameterRegex.Match(body);
                if (!element.Success) continue;

                Script.AddVariable(par.Value.Var);
                var encoded = System.Web.HttpUtility.HtmlEncode(par.Value.HTMLElementValue);
                //TODO: esto hay que pasarlo a regex

                body = body.Replace(element.Value + "=" + encoded,
                                    element.Value + "=\"+" + par.Value.Var.Name + "+\"");
                //campos en el gxstate con comilla
                body = body.Replace(element.Value + "%22%3A%22" + encoded,
                                    element.Value + "%22%3A%22\"+" + par.Value.Var.Name + "+\"");
                //campos en el gxstate sin comilla
                body = body.Replace(element.Value + "%22%3A" + encoded,
                                    element.Value + "%22%3A\"+" + par.Value.Var.Name + "+\"");

                encoded = encoded.ToUpper();
                body = body.Replace(element.Value + "=" + encoded,
                                    element.Value + "=\"+" + par.Value.Var.Name + "+\"");
                //campos en el gxstate con comilla
                body = body.Replace(element.Value + "%22%3A%22" + encoded,
                                    element.Value + "%22%3A%22\"+" + par.Value.Var.Name + "+\"");
                //campos en el gxstate sin comilla
                body = body.Replace(element.Value + "%22%3A" + encoded,
                                    element.Value + "%22%3A\"+" + par.Value.Var.Name + "+\"");
            }

            return body;
        }

        private string SplitAndParametrizeUrl(string fullURL, ScriptSCL Script)
        {
            var server = Request.host;
            Script.OpenSTARep.Global.AddConstant("Server", "\"" + server);
            var result = fullURL.Replace(fullURL.Contains(server) ? server : Request.host, "\"+Server+\"");

            //saco la comilla inicial y el ultimo caracter &
            result = OpenSTAUtils.SplitStringIfNecesary(result, 50);

            //result = result.Substring(0, result.Length - 2);
            if (!result.StartsWith("\""))
            {
                result = "\"" + result;
            }

            return result;
        }

        private string GetOpenSTAHeaders()
        {
            //primero armo el global
            var defaultHeaders = "\""; //DEFAULT_HEADERS
            var reqHeadrs = "";
            foreach (HTTPHeaderItem headr in Request.oRequest.headers)
            {
                var name = headr.Name.ToLower();
                if (
                    (!name.StartsWith("cookie")) &&
                    (name != "user-agent") &&
                    (name != "host") &&
                    (name != "accept-encoding")
                    )
                {
                    if (name == "authorization")
                    {
                        reqHeadrs += string.Format("\t\"{0}: \"+{1},&\n", headr.Name, "blob1");
                    }
                    else
                    {
                        var stringDelmiter = "\"";
                        if (headr.Value.Contains("\""))
                        {
                            stringDelmiter = "'";
                        }
                        var aux = OpenSTAUtils.SplitStringIfNecesary(headr.Value);

                        reqHeadrs += string.Format("\t{2}{0}: {1}{2},&\n", headr.Name, aux, stringDelmiter);
                    }
                }
            }

            defaultHeaders += "\"Host: " + Request.oRequest.headers["Host"] + "^J\"&\n" +
                              "\"User-Agent: " + Request.oRequest.headers["User-Agent"];

            Script.OpenSTARep.Global.AddConstant("DEFAULT_HEADERS", defaultHeaders);
            return "\t HEADER DEFAULT_HEADERS & \n" +
                   "\t , WITH {&\n" +
                   reqHeadrs +
                   SessionUtils.GetCookies(
                       Request.oRequest.headers,
                       this.Script)
                   + "}";
        }
    }
}
