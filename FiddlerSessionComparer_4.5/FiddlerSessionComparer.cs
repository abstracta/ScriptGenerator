using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Web;
using Abstracta.FiddlerSessionComparer.Content;
using Abstracta.FiddlerSessionComparer.Utils;
using Fiddler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Abstracta.FiddlerSessionComparer
{
    internal enum CompareURLType
    {
        WholeURL,
        SkipParameters,
    }

    public class FiddlerSessionComparer
    {
        // todo: create the parameter as detected difference, but don't replace the value by the parameter
        // Parameters with values larger than this constant will not be marked as a difference
        private const int MaxLengthOfValue = 1024;

        private Session[] _sessions1, _sessions2;

        private Page _resultOfComparizon;

        public static bool ReplaceInBodies { get; private set; }

        public static bool IsGx { get; private set; }

        public FiddlerSessionComparer(bool replaceInBodies, bool isGenexusApplication)
        {
            ReplaceInBodies = replaceInBodies;
            IsGx = isGenexusApplication;
        }

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

            SazFormat.ResetId();
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
		    return (from session in sessions
		            let method = session.oRequest.headers.HTTPMethod.ToLower()
		            where session.IsPrimaryRequest() && method == "get" || method == "post"
		            select session).ToArray();
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

        public static void ResetComparer()
        {
            Parameter.Reset();
            NameFactory.GetInstance().Reset();
        }

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

                var dicParams1 = CreateListOfValuesFromString(index1, params1);
                var dicParams2 = CreateListOfValuesFromString(index2, params2);

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

            var rootPage = new Page(null, "", "", "", "", "", -1);

            var s1Count = _sessions1.Count();

            for (var i1 = 0; i1 < s1Count; i1++)
            {
                var s1 = _sessions1[i1];
                
                // first try to find a complete match in URLs, then try to find a URL that matches without parameters
                var i2 = FindMatchingURL(_sessions2, s1, CompareURLType.WholeURL);
                if (i2 < 0)
                {
                    i2 = FindMatchingURL(_sessions2, s1, CompareURLType.SkipParameters);
                }

                if (i2 < 0)
                {
                    var method = s1.oRequest.headers.HTTPMethod.ToLower();
                    if (method == "get" || method == "post")
                    {
                        // Create an "empty" page, a page without differences
                        rootPage.CreateAndInsertPage(s1);
                    }
                }
                else
                {
                    var s2 = _sessions2[i2];

                    // mark those that have a match as null
                    _sessions1[i1] = null;
                    _sessions2[i2] = null;

                    Utils.Logger.GetInstance().Log("Comparing sessions: " + s1.id + " with " + s2.id);
                    CompareParameters(s1, s2, rootPage);
                }
            }

            _resultOfComparizon = rootPage;
            return _resultOfComparizon;
        }

        private static int FindMatchingURL(IList<Session> sessions, Session s1, CompareURLType compareType)
        {
            var s2Count = sessions.Count();
            for (var i2 = 0; i2 < s2Count; i2++)
            {
                var s2 = sessions[i2];

                if (s2 == null) continue;

                switch (compareType)
                {
                    case CompareURLType.WholeURL:
                        if (s1.fullUrl == s2.fullUrl)
                        {
                            return i2;
                        }
                        break;

                    case CompareURLType.SkipParameters:
                        if (SameURL(s1, s2)) 
                        {
                            return i2;
                        }
                        break;
                }
            }

            return -1;
        }

        /// <summary>
        /// Compare the existing sessions with others received as parameters.
        /// </summary>
        /// <param name="sessions">List of sessions to compare</param>
        /// <returns>Depending on the value of the variable CompareResultType type, returns a page with the result of the comparison.</returns>
        public Page CompareFull(Session[] sessions)
        {
            // Verify 2 sessions were loaded 
            VerifySessionsLoaded();

            // Verify 2 sessions loaded were compared
            if (_resultOfComparizon == null)
            {
                _resultOfComparizon = CompareFull();
            }

            var sCount = sessions.Count();
            var sessions3 = sessions.ToArray();

            // Make a sorted list of all Pages
		    var pageList = _resultOfComparizon.GetSubPagesList();

            // compares _resultOfComparizon, against sessions
            
            var pTCount = pageList.Count();
            
            for (int i1 = 0, i2 = 0; i1 < sCount && i2 < pTCount; )
            {
                var s1 = sessions3[i1];
                var s2 = pageList.Values[i2];

                if (SameURL(s1, s2)) 
                {
                    // mark those that have a match as null
                    sessions3[i1] = null;
                   
                    Utils.Logger.GetInstance().Log("Comparing session " + s1.id + " with page " + s2.Id);
                    CompareSessionVsPage(s2, s1);

                    // i2 starts always from the beginning
                    i2 = 0;
                    i1++;
                }
                else
                {
                    // inc index of array i2
                    i2++;

                    // if i2 reaches the end, inc i1
                    if (i2 == pTCount)
                    {
                        Utils.Logger.GetInstance().Log("No matching URL found in Pages for session: " + s1.id);
                        i2 = 0;
                        i1++;
                    }
                }
            }

            return _resultOfComparizon;
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

            // var redirectByCode = 299 < page.Referer.ResponseCode && page.Referer.ResponseCode < 400;
            // var sourceOfParameter = redirectByCode ? ExtractFrom.Headers : ExtractFrom.Body;

            var temp1 = GetParametersFromURL(s1.fullUrl);
            var temp2 = GetParametersFromURL(s2.fullUrl);

            if (IsGx)
            {
                temp1 = RemoveUnusedParameters(temp1);
                temp2 = RemoveUnusedParameters(temp2);
            }

            if (temp1 == null && temp2 == null)
            {
                return page;
            }

            if (IsGx || AreComaSeparatedValues(temp1))
            {
                var expressionPrefix = GetProgramFromURL(GetPathFromURL(s1.fullUrl));
                CompareParametersInsideList(page, s1.id, s2.id, temp1, temp2, type, UseToReplaceIn.Url, expressionPrefix); 
            }
            else if (AreKeyValueValues(temp1))
            {
                CompareParametersInsideDictionary(page, s1.id, s2.id, temp1, temp2, type, UseToReplaceIn.Url);
            }
            else
            {
                Utils.Logger.GetInstance().Log("Unknown format for parameters (try coma separated values): " + temp1);

                // by default will try coma separated values (it may be just one value)
                var expressionPrefix = GetProgramFromURL(GetPathFromURL(s1.fullUrl));
                CompareParametersInsideList(page, s1.id, s2.id, temp1, temp2, type, UseToReplaceIn.Url, expressionPrefix);
            }

            return page;
        }

        private static void CompareParametersInsideList(Page page, int id1, int id2, string params1, string params2, ComparerResultType type, 
                                              UseToReplaceIn useToReplaceIn, string expressionPrefix)
        {
            var paramsInGet1 = params1 != null ? params1.Split(',') : new string[0];
            var paramsInGet2 = params2 != null ? params2.Split(',') : new string[0];

            // todo the condition of the for doesn't consider ShowAll, and HideEquals, just works for 'HideNull&Equals'
            for (var i = 0; i < paramsInGet1.Length && i < paramsInGet2.Length; i++)
            {
                var temp1 = paramsInGet1[i];
                var temp2 = paramsInGet2[i];

                var contextType = ParameterContext.ComaSeparatedValue;
                if (i == 0 && paramsInGet1.Length == 1)
                {
                    contextType = ParameterContext.AloneValue;
                }
                else if (i == 0)
                {
                    contextType = ParameterContext.FirstComaSeparatedValue;
                }
                else if (i == paramsInGet1.Length - 1)
                {
                    contextType = ParameterContext.LastComaSeparatedValue;
                }

                string varName;
                Parameter parameter;
                switch (type)
                {
                        // parametrize allways 
                    case ComparerResultType.ShowAll:
                        varName = NameFactory.GetInstance().GetNewName();

                        parameter = new Parameter
                            {
                                ExtractFromPage = page.Referer,
                                UsedInPPages = new List<ParameterInPage> (),
                                Values = new List<string> {temp1, temp2},
                                VariableName = varName,
                                ExpressionPrefix = expressionPrefix,
                                // ExtractFromSection = sourceOfParameter,
                            };

                        parameter = page.Referer.AddParameterToExtract(parameter, page, contextType);
                        page.AddParameterToUse(parameter, useToReplaceIn, contextType);
                        break;

                        // parametrize when different
                    case ComparerResultType.HideEquals:
                        if (temp1 != temp2)
                        {
                            varName = NameFactory.GetInstance().GetNewName();

                            parameter = new Parameter
                                {
                                    ExtractFromPage = page.Referer,
                                    Values = new List<string> {temp1, temp2},
                                    VariableName = varName,
                                    ExpressionPrefix = expressionPrefix,
                                    // ExtractFromSection = sourceOfParameter,
                                };

                            parameter = page.Referer.AddParameterToExtract(parameter, page, contextType);
                            page.AddParameterToUse(parameter, useToReplaceIn, contextType);
                        }
                        break;

                        // parametrize when different and when parameter is in both strings
                    case ComparerResultType.HideNullOrEquals:
                        if (temp1 != null && temp2 != null && temp1 != temp2)
                        {
                            varName = NameFactory.GetInstance().GetNewName();

                            parameter = new Parameter
                                {
                                    ExtractFromPage = page.Referer,
                                    Values = new List<string> {temp1, temp2},
                                    VariableName = varName,
                                    ExpressionPrefix = expressionPrefix,
                                    // ExtractFromSection = sourceOfParameter,
                                };

                            parameter = page.Referer.AddParameterToExtract(parameter, page, contextType);
                            page.AddParameterToUse(parameter, useToReplaceIn, contextType);
                        }
                        break;
                }
            }
        }

        private static bool AreKeyValueValues(string str)
        {
            return str != null && str.Split('&').All(p => p.Contains('='));
        }

        private static bool AreComaSeparatedValues(string str)
        {
            return str != null && str.Contains(',');
        }

        private static string RemoveUnusedParameters(string parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            // find the parameter 
            var indexOf = parameters.IndexOf("gx-no-cache", StringComparison.Ordinal);

            // when there are two or more parameters
            if (indexOf > 0 && parameters[indexOf - 1] == ',')
            {
                indexOf--;
            }

            var res = indexOf == -1 ? parameters : parameters.Substring(0, indexOf);

		    return res;
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

            CompareParametersInsideDictionary(page, s1.id, s2.id, s1Body, s2Body, type, UseToReplaceIn.Body);
        }

        private static void CompareParametersInsideDictionary(Page page, int id1, int id2, string s1Body, string s2Body, ComparerResultType type, UseToReplaceIn useToReplaceIn)
        {
            var params1 = CreateListOfValuesFromString(id1, s1Body);
            var params2 = CreateListOfValuesFromString(id2, s2Body);

            var differences = GetTheDifferences(params1, params2, type);
            foreach (var difference in differences)
            {
                // The parameter needs the context (delimiters of the value) to know how to replace it in the string. 
                // In other case we can replace incorrect values
                var pContext = FindParamDefinitionFromKeyValue(params1, difference.Key, difference.Value1);

                var parameter = new Parameter
                {
                    ExtractFromPage = page.Referer,
                    Values = new List<string> { difference.Value1, difference.Value2 },
                    VariableName = difference.Key,
                    ExpressionPrefix = difference.Key,
                };

                parameter = page.Referer.AddParameterToExtract(parameter, page, pContext);
                page.AddParameterToUse(parameter, useToReplaceIn, pContext);
            }
        }

        private static ParameterContext FindParamDefinitionFromKeyValue(IEnumerable<ParameterDefinition> parameters, string key, string value)
        {
            var param = parameters.FirstOrDefault(p => p.Key == key && p.Value == value);
            return (param != null)?  param.Context : ParameterContext.Default;
        }

        // todo refactor this function
        private static void CompareSessionVsPage(Page p2, Session s1)
        {
            const ComparerResultType type = ComparerResultType.HideNullOrEquals;

            switch (s1.oRequest.headers.HTTPMethod.ToLower())
            {
                case "get":
                    // resultLog.Append("SAME GET ? \n\t\t" + s1.fullUrl + "\n\t\t" + s2.fullUrl);
                    CompareSessionVsPageInGET(p2, s1, type);
                    break;

                case "post":
                    // resultLog.Append("SAME POST ? \n\t\t" + s1.fullUrl + "\n\t\t" + s2.fullUrl);
                    CompareSessionVsPageInPOST(p2, s1, type);
                    break;
            }
        }

        // todo refactor this function
        private static Page CompareSessionVsPageInGET(Page p2, Session s1, ComparerResultType type)
        {
            if (s1 == null || p2 == null)
            {
                throw new NullReferenceException();
            }

            var temp1 = GetParametersFromURL(s1.fullUrl);
            var temp2 = GetParametersFromURL(p2.FullURL);

            #region
            // todo create a function for this
            var expressionPrefix = GetPathFromURL(s1.fullUrl);
            var index = expressionPrefix.Length - 1;
            for (; index > 0; index--)
            {
                if (expressionPrefix[index] == '/') break;
            }

            expressionPrefix = expressionPrefix.Substring(index + 1);
            # endregion

            string varName;
            Parameter parameter;      
            switch (type)
            {
                // parametrize allways 
                case ComparerResultType.ShowAll:
                    if (IsParametrized(temp2))
                    {
                        Utils.Logger.GetInstance().Log("Result of compare session " + s1.id + " and page " + p2.Id + ": same url and parametrized");
                    }
                    else
                    {
                        varName = NameFactory.GetInstance().GetNewName();

                        parameter = new Parameter
                            {
                                ExtractFromPage = p2.Referer,
                                Values = new List<string> {temp1, temp2},
                                VariableName = varName,
                                ExpressionPrefix = expressionPrefix,
                            };

                        parameter = p2.Referer.AddParameterToExtract(parameter, p2, ParameterContext.ComaSeparatedValue);
                        p2.AddParameterToUse(parameter, UseToReplaceIn.Url, ParameterContext.ComaSeparatedValue);
                    }

                    break;

                // parametrize when different
                case ComparerResultType.HideEquals:
                    if (temp1 != temp2)
                    {                        
                            if (IsParametrized(temp2))
                            {
                                Utils.Logger.GetInstance().Log("Result of compare session " + s1.id + " and page " + p2.Id + ": same url and parametrized");
                            }
                            else
                            {
                                varName = NameFactory.GetInstance().GetNewName();

                                parameter = new Parameter
                                {
                                    ExtractFromPage = p2.Referer,
                                    Values = new List<string> { temp1, temp2 },
                                    VariableName = varName,
                                    ExpressionPrefix = expressionPrefix,
                                };

                                parameter = p2.Referer.AddParameterToExtract(parameter, p2, ParameterContext.ComaSeparatedValue);
                                p2.AddParameterToUse(parameter, UseToReplaceIn.Url, ParameterContext.ComaSeparatedValue);
                            }
                    }
                    else if ((temp1 == null) && (temp2 == null))
                    {
                            Utils.Logger.GetInstance().Log("Result of compare session " + s1.id + " and page " + p2.Id + ": same url without parameters");
                    }
                    break;

                // parametrize when different and when parameter is in both strings
                case ComparerResultType.HideNullOrEquals:
                    if (temp1 != null && temp2 != null && temp1 != temp2)
                    {
                        if (IsParametrized(temp2))
                        {
                            Utils.Logger.GetInstance().Log("Result of compare session " + s1.id + " and page " + p2.Id + ": same url and parametrized");
                        }
                        else
                        {
                            varName = NameFactory.GetInstance().GetNewName();

                            parameter = new Parameter
                            {
                                ExtractFromPage = p2.Referer,
                                Values = new List<string> { temp1, temp2 },
                                VariableName = varName,
                                ExpressionPrefix = expressionPrefix,
                            };

                            parameter = p2.Referer.AddParameterToExtract(parameter, p2, ParameterContext.ComaSeparatedValue);
                            p2.AddParameterToUse(parameter, UseToReplaceIn.Url, ParameterContext.ComaSeparatedValue);
                        }
                    }
                    break;
            }

            return p2;
        }

        // todo refactor this function
        private static void CompareSessionVsPageInPOST(Page p2, Session s1, ComparerResultType type)
        {
            if (s1 == null || p2 == null)
            {
                throw new NullReferenceException();
            }

            // Get the differences in the URLs, etc.
            var page = CompareSessionVsPageInGET(p2, s1, type);

            // Get the differences in the bodies
            var s1Body = s1.GetRequestBodyAsString();
            var s2Body = p2.Body;

            var params1 = CreateListOfValuesFromString(s1.id, s1Body);
            var params2 = CreateListOfValuesFromString(s1.id, s2Body);

            var differences = GetTheDifferences(params1, params2, type).ToList();
            //todo remove differences parametrized

            if (differences.Count == 0)
            {
                Utils.Logger.GetInstance().Log("Result of compare session " + s1.id + " and page " + p2.Id + ": bodies are equal");
            }
            else
            {
                foreach (var difference in differences)
                {
                    if (IsParametrized(difference.Value2))
                    {
                        Utils.Logger.GetInstance().Log("Result of compare session " + s1.id + " and page " + p2.Id + ": atributte " + difference.Key + " is parametrized in page");
                    }
                    else
                    {
                        var parameter = new Parameter
                        {
                            ExtractFromPage = page.Referer,
                            Values = new List<string> { difference.Value1, difference.Value2 },
                            VariableName = difference.Key,
                            ExpressionPrefix = difference.Key,
                        };

                        parameter = page.Referer.AddParameterToExtract(parameter, page, ParameterContext.ComaSeparatedValue);
                        page.AddParameterToUse(parameter, UseToReplaceIn.Body, ParameterContext.ComaSeparatedValue);
                    }
                }
            }
        }

        private static IEnumerable<EqualsResult> GetTheDifferences(IEnumerable<ParameterDefinition> paramValues1, ICollection<ParameterDefinition> paramValues2, ComparerResultType type)
        {
            var result = new List<EqualsResult>();
            const string aux = "NULL";

            foreach (var pv in paramValues1)
            {
                // todo: improve performance here, use find element instead of search if its contained and after that get it
                var key = pv.Key;
                var value1 = pv.Value;
                var paramValues2ContainsKey = ContainsKey(paramValues2, key);
                var value2 = (paramValues2ContainsKey) ? GetValueByKey(paramValues2, key) : aux;

                switch (type)
                {
                    case ComparerResultType.ShowAll:
                        if (!paramValues2ContainsKey)
                        {
                            result.Add(new EqualsResult(key, value1, value2));
                        }
                        else
                        {
                            result.Add(new EqualsResult(key, value1, value2));
                            RemoveByKey(paramValues2, key);
                        }
                        break;
                    
                    case ComparerResultType.HideEquals:
                        if (!paramValues2ContainsKey)
                        {
                            result.Add(new EqualsResult(key, value1, value2));
                        }
                        else
                        {
                            if (value1 != value2)
                            {
                                result.Add(new EqualsResult(key, value1, value2));
                            }

                            RemoveByKey(paramValues2, key);
                        }
                        break;
                    
                    case ComparerResultType.HideNullOrEquals:
                        if (paramValues2ContainsKey)
                        {
                            if (value1 != value2)
                            {
                                result.Add(new EqualsResult(key, value1, value2));
                            }

                            RemoveByKey(paramValues2, key);
                        }
                        break;
                }
            }

            switch (type)
            {
                case ComparerResultType.ShowAll:
                case ComparerResultType.HideEquals:
                    result.AddRange(paramValues2.Select(t => new EqualsResult(t.Key, aux, t.Value)));
                    break;
            }

            return result;
        }

        private static void RemoveByKey(ICollection<ParameterDefinition> list, string key)
        {
            var item = list.First(i => i.Key == key);
            list.Remove(item);
        }

        private static string GetValueByKey(IEnumerable<ParameterDefinition> list, string key)
        {
            return list.First(i => i.Key == key).Value;
        }

        private static bool ContainsKey(IEnumerable<ParameterDefinition> list, string key)
        {
            return list.Any(i => i.Key == key);
        }

        public static List<ParameterDefinition> CreateListOfValuesFromString(int fiddlerSessionId, string str)
        {
            var result = new List<ParameterDefinition>();
            
            if (string.IsNullOrEmpty(str.Trim()))
            {
                return result;
            }

            var parameters = str.Split('&');

            // body is JSON? XML?
            if (parameters.Length == 1)
            {
                try
                {
                    if (ContentFactory.IsComplexType(str))
                    {
                        var res = GetLeavesFromComplexType(str);
                        result.AddRange(res);
                    }
                    else if (parameters[0].Contains('='))
                    {
                        var splitedParameter = parameters[0].Split('=');
                        if (ContentFactory.IsComplexType(splitedParameter[1]))
                        {
                            var res = GetLeavesFromComplexType(splitedParameter[1]);
                            result.AddRange(res);
                        }
                        else
                        {
                            result.Add(new ParameterDefinition(splitedParameter[0], splitedParameter[1], ParameterContext.KeyEqualValue));
                        }
                    }
                    else
                    {
                        throw new Exception("Unsupported case: ERRORCODE 1");
                    }
                }
                catch (Exception e)
                {
                    Utils.Logger.GetInstance().Log("ERROR: Exception when comparing sessions in page (" + fiddlerSessionId + "): " + e.Message);
                }
            }
            else
            {
                foreach (var parameter in parameters)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(parameter))
                        {
                            continue;
                        }

                        var splitedParameter = parameter.Split('=');

                        if (splitedParameter.Length == 1)
                        {
                            var res = GetLeavesFromComplexType(parameter);
                            result.AddRange(res);
                        }
                        else if (splitedParameter.Length == 2)
                        {
                            if (ContentFactory.IsComplexType(splitedParameter[1]))
                            {
                                var res = GetLeavesFromComplexType(splitedParameter[1]);
                                result.AddRange(res);
                            }
                            else
                            {
                                result.Add(new ParameterDefinition(splitedParameter[0], splitedParameter[1], ParameterContext.KeyEqualValue));
                            }
                        }
                        else
                        {
                            throw new Exception("Unsupported case: ERRORCODE 2");
                        }
                    }
                    catch (Exception e)
                    {
                        Utils.Logger.GetInstance().Log("ERROR: Exception when comparing sessions in page (" + fiddlerSessionId + "): " + e.Message);
                    }
                }
            }

            return result;
        }

        private static IEnumerable<ParameterDefinition> GetLeavesFromComplexType(string value)
        {
            var result = new List<ParameterDefinition>();
            var decodedType = HttpUtility.UrlDecode(value);

            if (value.Trim() == string.Empty || value == "\"\"") 
            {
                return result;
            }

            if (ContentFactory.IsJSON(decodedType))
            {
                var jsonSettings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All,
                        TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                    };

                var jsonValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(decodedType, jsonSettings);
                result.AddRange(GetLeavesFromJSON(jsonValues));
            }
            else if (ContentFactory.IsXML(decodedType))
            {
                var xmlValues = XmlContentType.Deserialize(decodedType);
                result.AddRange(xmlValues.GetLeaves());
            }

            return result;
        }

        private static IEnumerable<ParameterDefinition> GetLeavesFromJSON(Dictionary<string, object> jsonValues)
        {
            var result = new List<ParameterDefinition>();

            foreach (var key in jsonValues.Keys)
            {
                var value = jsonValues[key] as JToken;
                if (value == null)
                {
                    AddValueToResult(key, jsonValues[key], result);
                }
                else if (value is JArray)
                {
                    var jarray = value as JArray;
                    foreach (var item in jarray)
                    {
                        var tmp = new Dictionary<string, object> { { key, item } };
                        result.AddRange(GetLeavesFromJSON(tmp));
                    }
                }
                else if (value is JProperty)
                {
                    var jproperty = value as JProperty;
                    var strValue = jproperty.Value.ToString();
                    var pContext = ParameterDefinition.GetContextFromJPropertyType(jproperty.Type);

                    if (ContentFactory.IsXML(strValue))
                    {
                        // this is necesary to process the xml when it has several root elements
                        strValue = "<root>" + strValue + "</root>";
                        var xmlValues = XmlContentType.Deserialize(strValue);
                        result.AddRange(xmlValues.GetLeaves());
                    }
                    else
                    {
                        if (strValue.Length < MaxLengthOfValue)
                        {
                            result.Add(new ParameterDefinition(jproperty.Name, strValue, pContext));
                        }
                    }
                }
                else if (value is JObject)
                {
                    var jobject = value as JObject;
                    foreach (var property in jobject.Properties())
                    {
                        result.AddRange(GetLeavesFromJSON(new Dictionary<string, object> { { property.Name, property.Value } } ));
                    }
                }
                else
                {
                    AddValueToResult(key, jsonValues[key], result);
                }
            }

            return result;
        }

        private static void AddValueToResult(string key, object value, List<ParameterDefinition> result)
        {
            if (value == null)
            {
                result.Add(new ParameterDefinition(key, null, ParameterContext.JSonNumberValue));
                return;
            }

            var strValue = value.ToString(); 
            
            var jValue = value as JValue;
            var pContext = (jValue == null)
                               ? ParameterDefinition.GetContextFromStringValue(value)
                               : ParameterDefinition.GetContextFromJPropertyType(jValue.Type);

            if (ContentFactory.IsXML(strValue))
            {
                try
                {
                    // this is necesary to process the xml when it has several root elements
                    var tmpValue = "<root>" + strValue + "</root>";
                    var xmlValues = XmlContentType.Deserialize(tmpValue);
                    result.AddRange(xmlValues.GetLeaves());
                }
                catch (Exception)
                {
                    if (strValue.Length < MaxLengthOfValue)
                    {
                        result.Add(new ParameterDefinition(key, strValue, pContext));
                    }
                }
            }
            else
            {
                if (strValue.Length < MaxLengthOfValue)
                {
                    result.Add(new ParameterDefinition(key, strValue, pContext));
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

        private static bool SameURL(Session s1, Page p2)
        {
            if (s1 == null && p2 == null)
            {
                return true;
            }
            if (s1 == null)
            {
                return false;
            }
            if (p2 == null)
            {
                return false;
            }
            
            return s1.oRequest.headers.HTTPMethod == p2.HTTPMethod 
                   && GetPathFromURL(s1.fullUrl) == GetPathFromURL(p2.FullURL);
        }

        private static bool IsParametrized(String param2)
        {
            if (param2.Length >= 2)
            {
                return param2.Substring(0, 2) == "${" && param2.EndsWith("}");
            }

            return false;
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
