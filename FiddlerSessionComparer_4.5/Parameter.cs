using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Abstracta.FiddlerSessionComparer
{
    public enum UseToReplaceIn { Url, Body }

    public enum VariableType { HTML, JSONString, JSONInt, JSONBool, ToStringedJSON, Undefined }

    public class Parameter
    {
        public const string DefaultVariableName = "!!!!!!!";

        public static string EscapedDefaultVariableName = FiddlerSessionComparer.EscapeString(DefaultVariableName);

        public static List<Parameter> ParametersAlreadyCreated = new List<Parameter>();

        public Page ExtractedFromPage { get; set; }

        public ParameterSoure SourceOfValue { get; set; }

        public List<Page> UsedInPages { get; set; }

        public List<string> Values { get; set; }

        public string VariableName { get; set; }
        
        public string ExpressionPrefix { get; set; }

        public UseToReplaceIn ParameterTarget { get; set; }

        public static void Reset()
        {
            ParametersAlreadyCreated = new List<Parameter>();
        }

        public static void AddParameterToMainList(Parameter parameter)
        {
            if (GetMatchingParameter(parameter) == null)
            {
                ParametersAlreadyCreated.Add(parameter);
            }
        }

        /// <summary>
        /// Returns a parameter that matches some conditions:
        /// 1- Same values
        /// 2- If check names: 
        ///       ExpressionPrefix is contained in the other ExpressionPrefix or viceversa
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="checkNames"></param>
        /// <returns></returns>
        public static Parameter GetMatchingParameter(Parameter parameter, bool checkNames = true)
        {
            foreach (var p in ParametersAlreadyCreated)
            {
                var sameValues = true;
                for (var i = 0; i < p.Values.Count; i++)
                {
                    sameValues = sameValues && p.Values[i] == parameter.Values[i];
                }

                if (p.ParameterTarget == parameter.ParameterTarget && sameValues)
                {
                    if (checkNames && (p.ExpressionPrefix.Contains(parameter.ExpressionPrefix) ||
                                       parameter.ExpressionPrefix.Contains(p.ExpressionPrefix)))
                    {
                        return p;
                    }
                    
                    if (!checkNames)
                    {
                        return p;
                    }
                }
            }

            return null;
        }

        public override string ToString()
        {
            var values = string.Join("' | '", Values.ToArray());
            var pages = string.Join("','", UsedInPages.Select(p => p.Id.ToString(CultureInfo.CurrentCulture)).ToArray());

            return "{ " +
                   "UseToReplaceIn='" + ParameterTarget + "' " +
                   "ExtractedFromPage='" + ExtractedFromPage.Id + "' " +
                   "UsedInPages='" + pages + "' " +
                   "VariableName='" + VariableName + "' " +
                   "ExpressionPrefix='" + ExpressionPrefix + "' " +
                   "SourceOfValue=" + SourceOfValue + " " +
                   "Values=['" + values + "']" +
                   "}";
        }

        public bool IsSourceOfValueDefined()
        {
            return SourceOfValue != null;
        }

        /// <summary>
        /// Create a regular expression for URL parameters.
        /// </summary>
        /// <param name="body">Character string to filter the regular expression</param>
        public void SetRegularExpressionOfParameterFromURL(string body)
        {
            /* Options:
             *   body is a HTML <a ... href="contenedorpestanas?INS,2,60,893,0,0,,ConsultaActual" ...>...</a>
             *   body is a HTML <IFRAME ... src="contenedorpestanas?INS,2,60,893,0,0,," ...>...</IFRAME>
             *   body is a HTML <form ... action="historiaclinicaprincipalv2?INS,2,60,893" ...> 
             *   body is a JSON {"gxCommands":[{"redirect":{"url":"historiaclinicaprincipalv2?INS,2,60,893","forceDisableFrm":1}}]}
             * 
             * Need a different regular expression for each case
             * The problem: body can contain several of each option
            //*/

            string regExp;
            var replaceValue = "?" + Values[0];
            var replaceWith = "?${" + VariableName + "}";

            if (IsHTMLResponse(body))
            {
                var htmlTag = GetTagThatContainsValue(body, Values[0]);
                if (htmlTag == null)
                {
                    // need to set a default value. When this executes, means ExpressionPrefix or body are incorrect
                    Utils.Logger.GetInstance().Log("ERROR: Can't find a variable in the body: " + ExpressionPrefix);
                    SetDefaultSourceOfValue();
                    return;
                }

                if (IsHTMLIframeTag(htmlTag))
                {
                    regExp = "<IFRAME .* src=\"" + ExpressionPrefix + "\\?([^\"]+)\".*>";
                }
                else if (IsHTMLAnchorTag(htmlTag))
                {
                    regExp = "<a .* href=\"" + ExpressionPrefix + "\\?([^\"]+)\".*>";
                }
                else if (IsHTMLFormTag(htmlTag))
                {
                    regExp = "<form .* action=\"" + ExpressionPrefix + "\\?([^\"]+)\".*>";
                }
                else
                {
                    Utils.Logger.GetInstance().Log("ERROR: Don't know what an HTML tag is: " + htmlTag);
                    
                    regExp = "\"" + ExpressionPrefix + "\\?([^\"]*)\"";
                }
            }
            else if (IsJSONResponse(body))
            {
                regExp = "\"" + ExpressionPrefix + "\\?([^\"]+)\"";
            }
            else
            {
                Utils.Logger.GetInstance().Log("ERROR: Can't calculate RegExpExtractor. Using default for parameter: " + this);
                Utils.Logger.GetInstance().Log("ERROR: Body isn't HTML and also isn't JSON: " + body);

                regExp = "\"" + ExpressionPrefix + "\\?([^\"]*)\"";
            }

            SourceOfValue = new RegExpExtractor(1, regExp, replaceValue, replaceWith);
        }

        /// <summary>
        /// Create a regular expression for body parameters.
        /// </summary>
        /// <param name="httpResponse">Character string to filter the regular expression</param>
        public void SetRegularExpressionOfParameterFromBody(string httpResponse)
        {
            var bodyCopy = httpResponse;

            if (IsHTMLResponse(httpResponse))
            {
                // searches for the variable's name in the response body
                var pos = IndexOfParameterInHTML(httpResponse, ExpressionPrefix);

                if (pos < 0)
                {
                    Utils.Logger.GetInstance().Log("ERROR: Can't find a variable in the body: " + ExpressionPrefix);
                    SetDefaultSourceOfValue();
                    return;
                }

                SourceOfValue = GetRegExp(bodyCopy, pos, ExpressionPrefix, Values[0]);
            }
            else
            {
                // searches for the variable's name in the response 
                var pos = IndexOfParameterInHTML(httpResponse, ExpressionPrefix);

                if (pos < 0)
                {
                    Utils.Logger.GetInstance().Log("MSG: Can't find a variable name in a JSON Response, need to extract just the value: " + ExpressionPrefix);
                    SetDefaultSourceOfValue();
                    return;
                }

                SourceOfValue = GetRegExp(bodyCopy, pos, ExpressionPrefix, Values[0]);
            }
        }

        /// <summary>
        /// Returns the value of truth of ExpressionPrefix is contained in htmlResponse
        /// </summary>
        /// <param name="response">Character string where i want to search ExpressionPrefix</param>
        /// <returns>Returns true if ExpressionPrefix is contained in htmlResponse or false otherwise</returns>
        public bool IsContainedInResponse(string response)
        {
            if (IsHTMLResponse(response))
            {
                var htmlTag = GetTagThatContainsValues(response, ExpressionPrefix, Values[0]);
                return htmlTag != null;
            }

            var tmp11 = System.Web.HttpUtility.UrlDecode(Values[0]);
            if (tmp11 == null)
            {
                return false;
            }

            var tmp12 = System.Web.HttpUtility.HtmlEncode(tmp11);
            tmp12 = ProcessAccents(tmp12);

            var indexOf = response.IndexOf(tmp12, StringComparison.Ordinal);
            return indexOf > 0;
        }

        public void SetDefaultSourceOfValue()
        {
            SourceOfValue = new ParameterSoure(DefaultVariableName, Values[0]);
        }

        # region private methods

        private static ParameterSoure GetRegExp(string body, int pos, string parameterName, string value)
        {
            switch (GetBodyType(body, pos, parameterName, value))
            {
                case VariableType.HTML:
                    // \b<variableName>=([^&$]*)
                    // holaquetal="1"&jhoholaquetal="2"&holaquetal="3"&hholaquetal="4"&holaquetal="5"
                    // matches "1", "3", "5"
                    // http://stackoverflow.com/questions/2552428/regex-use-start-of-line-end-of-line-signs-or-in-different-context
                    return new RegExpExtractor(1, /*"\\b" + */parameterName + "=([^&$]*)",
                                               parameterName + "=" + value,
                                               parameterName + "=" + DefaultVariableName + "");
                case VariableType.JSONString:
                    return new RegExpExtractor(1, "\"" + parameterName + "\":\"([^\"]*)\"",
                                                "\"" + parameterName + "\":\"" + value + "\"",
                                                "\"" + parameterName + "\":\"" + DefaultVariableName + "\"");
                case VariableType.JSONInt:
                    return new RegExpExtractor(1, "\"" + parameterName + "\":([\\d]+(\\.[\\d]+)?)",
                                                "\"" + parameterName + "\":" + value + "",
                                                "\"" + parameterName + "\":" + DefaultVariableName + "");

                case VariableType.JSONBool:
                    return new RegExpExtractor(1, "\"" + parameterName + "\":(true|false|null)[,|\\}|\\]]",
                                                "\"" + parameterName + "\":" + value + "",
                                                "\"" + parameterName + "\":" + DefaultVariableName + "");

                case VariableType.ToStringedJSON:
                    return new RegExpExtractor(1, "\"" + parameterName + "\\\\\",\\\\\"([^\\\\\"]*)\\\\\"",
                                                "\"" + parameterName + "\\\\\",\\\\\"" + value + "\\\\\"",
                                                "\"" + parameterName + "\\\\\",\\\\\"" + DefaultVariableName + "\\\\\"");
                default:
                    // VariableName += "_UndefinedTypeParameter";
                    // Utils.Logger.GetInstance().Log("Parameter context format (HTML, JSON) undefined: " + this);
                    return new ParameterSoure(parameterName, value);
            }
        }

        private static VariableType GetBodyType(string body, int pos, string key, string value)
        {
            // checks if there is a tag in the HTML response, that contains the value and the key
            var htmlTag = GetTagThatContainsValues(body, key, value);
            if (htmlTag != null && htmlTag.Contains(key))
            {
                if ((body[pos - 1] == '\"' && body[pos + key.Length] == '\"') && (body[pos + key.Length + 2] == '\"'))
                {
                    return VariableType.JSONString;
                }

                return VariableType.HTML;
            }

            if (body[pos - 1] == '\"' && body[pos - 2] == '\\')
            {
                return VariableType.ToStringedJSON;
            }

            // checks if parameters are in JSON format
            if (body[pos - 1] == '\"' && body[pos + key.Length] == '\"')
            {
                if (body[pos + key.Length + 2] == '\"')
                {
                    return VariableType.JSONString;
                }

                if (char.IsNumber(body[pos + key.Length + 2]))
                {
                    return VariableType.JSONInt;
                }

                if (body.Substring(pos + key.Length + 2, 4).ToLower().Equals("true") ||
                    body.Substring(pos + key.Length + 2, 4).ToLower().Equals("null") ||
                    body.Substring(pos + key.Length + 2, 5).ToLower().Equals("false"))
                {
                    return VariableType.JSONBool;
                }
            }

            return VariableType.Undefined;
        }

        private static bool IsHTMLResponse(string html)
        {
            return html.Trim().StartsWith("<!DOCTYPE html");
        }

        private static bool IsJSONResponse(string html)
        {
            return html.Trim().StartsWith("{");
        }

        private static bool IsHTMLAnchorTag(string htmlTag)
        {
            return htmlTag.ToLower().StartsWith("<a ");
        }

        private static bool IsHTMLIframeTag(string htmlTag)
        {
            return htmlTag.ToLower().StartsWith("<iframe ");
        }

        private static bool IsHTMLFormTag(string htmlTag)
        {
            return htmlTag.ToLower().StartsWith("<form ");
        }

        private static string GetTagThatContainsValue(string html, string value)
        {
            return GetTagThatContainsValues(html, value, string.Empty);
        }

        private static string GetTagThatContainsValues(string html, string val1, string val2)
        {
            // must be unescaped: i.e. '%3Ck2b%20xmlns%3D%22SPU%22' to '<k2b xmlns="SPU"'
            var tmp11 = System.Web.HttpUtility.UrlDecode(val1);
            var tmp21 = System.Web.HttpUtility.UrlDecode(val2);

            if (tmp11 == null || tmp21 == null)
            {
                return null;
            }

            // enconde to HTML: i.e. '<k2b xmlns="SPU"' to '&lt;k2b xmlns=&quot;SPU&quot;'
            var tmp12 = System.Web.HttpUtility.HtmlEncode(tmp11);
            var tmp22 = System.Web.HttpUtility.HtmlEncode(tmp21);

            tmp12 = ProcessAccents(tmp12);
            tmp22 = ProcessAccents(tmp22);
          
            // Can't change the html string, because it's what it comes. The regular expression must match the HTML unchanged.
            var html2 = html.Replace("\n", "").Replace("\r", "");

            while (html2.Length > 0)
            {
                var indexOf = html2.IndexOf(tmp12, StringComparison.Ordinal);
                if (indexOf < 0)
                {
                    return null;
                }

                int indexIni = indexOf, indexEnd = indexOf;
                for (; indexIni > 0 && html2[indexIni] != '<'; indexIni--)
                {
                }

                for (; indexEnd < html2.Length && html2[indexEnd] != '>'; indexEnd++)
                {
                }

                var tag = html2.Substring(indexIni, (indexEnd - indexIni));

                if (tag.Contains(tmp22))
                {
                    return tag;
                }

                html2 = html2.Substring(indexOf + tmp12.Length);
            }

            return null;
        }

        private static string ProcessAccents(string value)
        {
            value = value.Replace("&#225;", "á");
            value = value.Replace("&#233;", "é");
            value = value.Replace("&#237;", "í");
            value = value.Replace("&#243;", "ó");
            value = value.Replace("&#250;", "ú");

            return value;
        }
        
        /// <summary>
        /// Index of parameterName and parameterValue
        /// </summary>
        /// <param name="htmlResponse"></param>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        private static int IndexOfParameterInHTML(string htmlResponse, string parameterName)
        {
            var offset = 0;

            var pos = htmlResponse.IndexOf(parameterName, StringComparison.Ordinal);
            if (pos == -1)
            {
                return -1;
            }

            while (pos != 0 && Char.IsLetter(htmlResponse[pos - 1]))
            {
                offset += (pos + parameterName.Length);
                htmlResponse = htmlResponse.Substring(pos + parameterName.Length);
                pos = htmlResponse.IndexOf(parameterName, StringComparison.Ordinal);

                if (pos == -1)
                {
                    return -1;
                }
            }

            return offset + pos;
        }

        #endregion
    }
}
