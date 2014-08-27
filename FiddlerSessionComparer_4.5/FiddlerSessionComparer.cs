using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Abstracta.FiddlerSessionComparer.Utils;
using Fiddler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Abstracta.FiddlerSessionComparer
{
    public class FiddlerSessionComparer
    {
        private static readonly string[] ParamNamesWithJSONContent = new[] { "GXState" };

        private Session[] _sessions1, _sessions2;

        private Page _resultOfComparizon;

        # region public static methods

        /// <summary>
        /// Returns the session from saz file
        /// </summary>
        /// <param name="fiddlerSessionsFileName">fiddler session File Name</param>
        /// <returns>List of requests</returns>
        public static Session[] GetSessionsFromFile(string fiddlerSessionsFileName)
        {          
            if (!File.Exists(fiddlerSessionsFileName))
            {
                throw new Exception("File doesn't exists: " + fiddlerSessionsFileName);
            }

            var sessions = SazFormat.GetSessionsFromFile(fiddlerSessionsFileName);
            if (sessions == null)
            {
                throw new Exception("Unknown format of file: " + fiddlerSessionsFileName);
            }

            return sessions;
        }

        /// <summary>
        /// Returns a list of primary sessions without images css, png, gif, js, etc.
        /// Primary request are those that are not with static content.
        /// </summary>
        /// <param name="sessions">List of requests we want to filter.</param>
        /// <param name="extenssions">list of extenssions who are primary request (.html .jsp between others).</param>
        /// <returns>List of primary requests</returns>
        public static Session[] CleanSessions(IEnumerable<Session> sessions, string[] extenssions)
        {
            var sessionsTemp = new List<Session>();
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var s in sessions)
            {
                var uri = new Uri(s.fullUrl);
                if ((uri.LocalPath.Split('.').Length == 1) || IsPrimaryReq(uri.LocalPath, extenssions)
                    && !uri.LocalPath.Contains("GXResourceProv"))
                {
                    sessionsTemp.Add(s);
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return sessionsTemp.ToArray();
        }

        public static string Decode(string s)
        {
            return HttpUtility.HtmlDecode(s);
        }

        public static string Encode(string s)
        {
            return HttpUtility.HtmlEncode(s);
        }

        public static string EscapeString(string s)
        {
            return Uri.EscapeDataString(s);
        }

        # endregion

        /// <summary>
        /// Converts the fiddler sessions files received as parameters, in session type variables and load them.
        /// </summary>
        public void Load(string fiddlerSessionsFile1, string fiddlerSessionsFile2, string[] extenssions)
        {            
            var sessions1 = GetSessionsFromFile(fiddlerSessionsFile1);
            var sessions2 = GetSessionsFromFile(fiddlerSessionsFile2);

            Load(sessions1, sessions2, extenssions);
        }

        /// <summary>
        /// Load the sessions received as parameters applying the CleanSessions function previously.
        /// </summary>
        /// <param name="sessions1">First session to load</param>
        /// <param name="sessions2">Second session to load</param>
        /// <param name="extenssions">List of primary requests</param>
        public void Load(Session[] sessions1, Session[] sessions2, string[] extenssions = null)
        {
            _sessions1 = CleanSessions(sessions1, extenssions);
            _sessions2 = CleanSessions(sessions2, extenssions);
        }

        public List<EqualsResult> CompareSimple(int index1, int index2, ComparerResultType type)
        {
            VerifySessionsLoaded();

            if (_sessions1.Length <= index1)
            {
                throw new Exception("Out of index in session1: " + index1);
            }

            if (_sessions2.Length <= index2)
            {
                throw new Exception("Out of index in session2: " + index2);
            }

            var res = new List<EqualsResult>();
            var session1 = _sessions1[index1];
            var session2 = _sessions2[index2];

            // It searches the URL and it's parameters looking for differences
            var method1 = session1.oRequest.headers.HTTPMethod;
            var method2 = session2.oRequest.headers.HTTPMethod;
            res.Add(new EqualsResult("HTTP_Method", method1, method2));

            var params1 = GetParametersFromURL(session1.fullUrl);
            var params2 = GetParametersFromURL(session2.fullUrl);
            res.Add(new EqualsResult("URL_Parameters", params1, params2));

            // One of the URLs must have parameters in the URL
            if (params1 != null || params2 != null)
            {
                params1 = Decode(params1);
                params2 = Decode(params2);

                // It goes through all the parameters in the URL that has more of them, but it has to mantain the order to show the results
                var recorroURL1 = params1.Split(',').Length >= params2.Split(',').Length;
                var primario = recorroURL1 ? params1.Split(',') : params2.Split(',');
                var secundario = recorroURL1 ? params2.Split(',') : params1.Split(',');

                for (var i = 0; i < primario.Length; i++)
                {
                    var aux = "URL_Param_" + (i + 1);
                    var sec = (i < secundario.Length) ? secundario[i] : "NULL";

                    res.Add((recorroURL1)
                                ? new EqualsResult(aux, primario[i], sec)
                                : new EqualsResult(aux, sec, primario[i]));
                }
            }

            if (GetPathFromURL(session1.fullUrl) != GetPathFromURL(session2.fullUrl))
            {
                res.Add(new EqualsResult("URL_Path", GetPathFromURL(session1.fullUrl), GetPathFromURL(session2.fullUrl)));
            }
            else if (method1 == "POST" && method2 == "POST")
            {
                // If both requests are POST, you have to compare the parameters in the bodies
                params1 = session1.GetRequestBodyAsString();
                params2 = session2.GetRequestBodyAsString();

                var dicParams1 = CreateDictionaryFromBody(index1, params1, ParamNamesWithJSONContent);
                var dicParams2 = CreateDictionaryFromBody(index2, params2, ParamNamesWithJSONContent);

                res.AddRange(GetTheDifferences(dicParams1, dicParams2, type));
            }

            return res;
        }

        /// <summary>
        /// Compare all the request from the _session1 with all the request from _session2
        /// </summary>
        /// <returns>Depending on the value of the variable CompareResultType type, returns a page with the result of the comparison.</returns>
        public Page CompareFull()
        {
            VerifySessionsLoaded();

            var rootPage = new Page(null, "", "", "");

            var s1Count = _sessions1.Count();
            var s2Count = _sessions2.Count();

            for (int i1 = 0, i2 = 0; i1 < s1Count && i2 < s2Count; )
            {
                var s1 = _sessions1[i1];
                var s2 = _sessions2[i2];

                if (SameURL(s1, s2))
                {
                    // mark those that have a match as null
                    _sessions1[i1] = null;
                    _sessions2[i2] = null;

                    Utils.Logger.GetInstance().Log("Comparing sessions: " + s1.id + " with " + s2.id);
                    CompareParameters(s1, s2, rootPage);
                    
                    // i2 starts always from the beginning
                    i2 = 0;
                    i1++;
                }
                else
                {
                    // inc index of array i2
                    i2++;
                    
                    // if i2 reaches the end, inc i1
                    if (i2 == s2Count)
                    {
                        // Create an "empty" page, a page without differences
                        rootPage.CreateAndInsertPage(_sessions1[i1]);
                        
                        i2 = 0;
                        i1++;
                    }
                }
            }

            _resultOfComparizon = rootPage;
            return _resultOfComparizon;
        }

        /// <summary>
        /// Compare the existing sessions with others received as parameters.
        /// </summary>
        /// <param name="sessions">List of sessions to compare</param>
        /// <returns>Depending on the value of the variable CompareResultType type, returns a page with the result of the comparison.</returns>
        public Page CompareFull(Session[] sessions)
        {
            // todo verify sessions loaded and compared -> if _resultOfComparizon == null, compare sessions before
            VerifySessionsLoaded();

            // compares _resultOfComparizon, against sessions
            throw new NotImplementedException();
        }

        # region private methods

        private void VerifySessionsLoaded()
        {
            if (_sessions1 == null)
            {
                throw new Exception("Session1 wasn't loaded: execute 'Load' method");
            }

            if (_sessions2 == null)
            {
                throw new Exception("Session2 wasn't loaded: execute 'Load' method");
            }
        }

        private static void CompareParameters(Session s1, Session s2, Page rootPage)
        {
            const ComparerResultType type = ComparerResultType.HideNullOrEquals;

            switch (s1.oRequest.headers.HTTPMethod.ToLower())
            {
                case "get":
                    // resultLog.Append("SAME GET ? \n\t\t" + s1.fullUrl + "\n\t\t" + s2.fullUrl);
                    CompareParametersInGET(s1, s2, rootPage, type);
                    break;

                case "post":
                    // resultLog.Append("SAME POST ? \n\t\t" + s1.fullUrl + "\n\t\t" + s2.fullUrl);
                    CompareParametersInPOST(s1, s2, rootPage, type);
                    break;
            }
        }

        private static Page CompareParametersInGET(Session s1, Session s2, Page rootPage, ComparerResultType type)
        {
            if (s1 == null || s2 == null)
            {
                return null;
            }

            var page = rootPage.CreateAndInsertPage(s1);

            var temp1 = GetParametersFromURL(s1.fullUrl);
            var temp2 = GetParametersFromURL(s2.fullUrl);

            var expressionPrefix = GetProgramFromURL(GetPathFromURL(s1.fullUrl));

            string varName;
            Parameter parameter;
            switch (type)
            {
                // parametrize allways 
                case ComparerResultType.ShowAll:
                    varName = NameFactory.GetInstance().GetNewName();

                    parameter = new Parameter
                        {
                            ExtractedFromPage = page.Referer,
                            UsedInPages = new List<Page>(),
                            Values = new List<string> { temp1, temp2 },
                            VariableName = varName,
                            ExpressionPrefix = expressionPrefix,
                            ParameterTarget = UseToReplaceIn.Url,
                        };

                    parameter = page.Referer.AddParameterToExtract(parameter);
                    page.AddParameterToUse(parameter);
                    break;

                // parametrize when different
                case ComparerResultType.HideEquals:
                    if (temp1 != temp2)
                    {
                        varName = NameFactory.GetInstance().GetNewName();

                        parameter = new Parameter
                        {
                            ExtractedFromPage = page.Referer,
                            UsedInPages = new List<Page>(),
                            Values = new List<string> { temp1, temp2 },
                            VariableName = varName,
                            ExpressionPrefix = expressionPrefix,
                            ParameterTarget = UseToReplaceIn.Url,
                        };

                        parameter = page.Referer.AddParameterToExtract(parameter);
                        page.AddParameterToUse(parameter);
                    }
                    break;

                // parametrize when different and when parameter is in both strings
                case ComparerResultType.HideNullOrEquals:
                    if (temp1 != null && temp2 != null && temp1 != temp2)
                    {
                        varName = NameFactory.GetInstance().GetNewName();

                        parameter = new Parameter
                        {
                            ExtractedFromPage = page.Referer,
                            UsedInPages = new List<Page>(),
                            Values = new List<string> { temp1, temp2 },
                            VariableName = varName,
                            ExpressionPrefix = expressionPrefix,
                            ParameterTarget = UseToReplaceIn.Url,
                        };

                        parameter = page.Referer.AddParameterToExtract(parameter);
                        page.AddParameterToUse(parameter);
                    }
                    break;
            }

            return page;
        }

        private static void CompareParametersInPOST(Session s1, Session s2, Page rootPage, ComparerResultType type)
        {
            if (s1 == null || s2 == null)
            {
                return;
            }

            // Get the differences in the URLs, etc.
            var page = CompareParametersInGET(s1, s2, rootPage, type);

            // Get the differences in the bodies
            var s1Body = s1.GetRequestBodyAsString();
            var s2Body = s2.GetRequestBodyAsString();

            var params1 = CreateDictionaryFromBody(s1.id, s1Body, ParamNamesWithJSONContent);
            var params2 = CreateDictionaryFromBody(s1.id, s2Body, ParamNamesWithJSONContent);

            var differences = GetTheDifferences(params1, params2, type).ToList();
            foreach (var difference in differences)
            {
                var parameter = new Parameter
                {
                    ExtractedFromPage = page.Referer,
                    UsedInPages = new List<Page>(),
                    Values = new List<string> { difference.Value1, difference.Value2 },
                    VariableName = difference.Key,
                    ExpressionPrefix = difference.Key,
                    ParameterTarget = UseToReplaceIn.Body,
                };
                
                parameter = page.Referer.AddParameterToExtract(parameter);
                page.AddParameterToUse(parameter);
            }
        }

        private static IEnumerable<EqualsResult> GetTheDifferences(Dictionary<string, string> dic1, Dictionary<string, string> dic2, ComparerResultType type)
        {
            var result = new List<EqualsResult>();

            foreach (var key in dic1.Keys)
            {
                switch (type)
                {
                    case ComparerResultType.ShowAll:
                        if (!dic2.ContainsKey(key))
                        {
                            result.Add(new EqualsResult(key, dic1[key], "NULL"));
                        }
                        else
                        {
                            result.Add(new EqualsResult(key, dic1[key], dic2[key]));
                            dic2.Remove(key);
                        }
                        break;
                    
                    case ComparerResultType.HideEquals:
                        if (!dic2.ContainsKey(key))
                        {
                            result.Add(new EqualsResult(key, dic1[key], "NULL"));
                        }
                        else
                        {
                            if (dic1[key] != dic2[key])
                            {
                                result.Add(new EqualsResult(key, dic1[key], dic2[key]));
                            }

                            dic2.Remove(key);
                        }

                        break;
                    case ComparerResultType.HideNullOrEquals:
                        if (dic2.ContainsKey(key) && dic1[key] != dic2[key])
                        {
                            result.Add(new EqualsResult(key, dic1[key], dic2[key]));
                        }
                        break;
                }

            }

            switch (type)
            {
                case ComparerResultType.ShowAll:
                case ComparerResultType.HideEquals:
                    result.AddRange(dic2.Keys.Select(key => new EqualsResult(key, "NULL", dic2[key])));
                    break;
            }

            return result;
        }

        private static Dictionary<string, string> CreateDictionaryFromBody(int fiddlerSessionId, string body, string []paramsWithJSON)
        {
            var result = new Dictionary<string, string>();
            var parameters = body.Split('&');
            
            if (parameters.Length == 1)
            {
                GetParamsFromJSON("", body, result);
                return result;
            }

            foreach (var parameter in parameters)
            {
                try
                {
                    if (parameter == string.Empty)
                    {
                        continue;
                    }

                    var splitedParameter = parameter.Split('=');
                    var varName = splitedParameter[0];
                    var varValue = splitedParameter[1];

                    if (paramsWithJSON.Contains(varName))
                    {
                        GetParamsFromJSON("", varValue, result);
                    }
                    else
                    {
                        result.Add(varName, varValue);
                    }
                }
                catch (Exception e)
                {
                    Utils.Logger.GetInstance().Log("ERROR: Exception when comparing sessions in page (" + fiddlerSessionId + "): " + e.Message);
                }
            }

            return result;
        }

        private static void GetParamsFromJSON(string fatherVariableName, string value, IDictionary<string, string> result)
        {
            var json = HttpUtility.UrlDecode(value);

            // to support .NET 3.5 can't use dynamics
            // var values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // todo: tomar los parámetros de los json que vienen pasados a string como objetos dentro del json
            foreach (var varName in values.Keys)
            {
                // bug: falta considerar los values[k] que son arreglos de objetos (vienen entre [] y se separan por comas)
                if (values[varName] != null && (values[varName].GetType() == typeof(JObject) || values[varName].GetType().IsSubclassOf(typeof(JObject))))
                {
                    GetParamsFromJSON(varName, values[varName].ToString(), result);
                }
                else
                {
                    var val = (values[varName] ?? "NULL").ToString();

                    var hasFather = !string.IsNullOrEmpty(fatherVariableName);
                    if (!hasFather && !result.ContainsKey(varName))
                    {
                        result.Add(varName, val);
                    }
                    else if (hasFather && !result.ContainsKey(fatherVariableName))
                    {
                        result.Add(fatherVariableName, val);
                    }
                    else
                    {
                        Utils.Logger.GetInstance().Log("Variable couldn't be added to dictionary to compare JSON: " + fatherVariableName +
                                  "." + varName);
                    }
                }
            }
        }

        private static bool SameURL(Session s1, Session s2)
        {
            if (s1 == null && s2 == null)
            {
                return true;
            }
            if (s1 == null)
            {
                return false;
            }
            if (s2 == null)
            {
                return false;
            }

            return s1.oRequest.headers.HTTPMethod == s2.oRequest.headers.HTTPMethod
                   && s1.host == s2.host /* hostname:port */
                   && GetPathFromURL(s1.fullUrl) == GetPathFromURL(s2.fullUrl);
        }

        private static bool IsPrimaryReq(string url, IEnumerable<string> extenssions)
        {
            return extenssions != null && extenssions.Any(url.EndsWith);
        }

        private static string GetPathFromURL(string url)
        {
            var temp1 = url.Split('?');
            return temp1.Length == 1 ? url : temp1[0];
        }

        private static string GetParametersFromURL(string url)
        {
            var temp1 = url.Split('?');
            return temp1.Length == 1 ? null : temp1[1];
        }

        private static string GetProgramFromURL(string url)
        {
            var index = url.Length - 1;
            for (; index > 0; index--)
            {
                if (url[index] == '/') break;
            }

            url = url.Substring(index + 1);
            return url;
        }

        # endregion
    }
}
