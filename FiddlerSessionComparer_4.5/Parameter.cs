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

        public Page ExtractedFromPage { get; set; }

        public ParameterSoure SourceOfValue { get; set; }

        public List<Page> UsedInPages { get; set; }

        public List<string> Values { get; set; }

        public string VariableName { get; set; }
        
        public string ExpressionPrefix { get; set; }

        public UseToReplaceIn ParameterTarget { get; set; }

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
            var replaceWith = "?${" + ExpressionPrefix + "}";

            if (IsHTMLResponse(body))
            {
                var htmlTag = GetTagThatContainsValue(body, Values[0]);
                if (htmlTag == null)
                {
                    // need to set a default value. When this executes, means ExpressionPrefix or body are incorrect
                    SourceOfValue = new RegExpExtractor(1, "", "1", "1");
                    Utils.Logger.GetInstance().Log("ERROR: Can't find a variable in the body: " + ExpressionPrefix);
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

        public void SetRegularExpressionOfParameterFromBody(string body)
        {
            var bodyCopy = body;

            // searches for the variable's name in the response body
            var pos = IndexOfParameterInHTML(body, ExpressionPrefix);

            if (pos < 0)
            {
                SourceOfValue = new RegExpExtractor(1, "", "1", "1");
                Utils.Logger.GetInstance().Log("ERROR: Can't find a variable in the body: " + ExpressionPrefix);
                return;
            }

            SourceOfValue = GetRegExp(bodyCopy, pos, ExpressionPrefix, Values[0]);
        }

        public bool IsContainedInHTML(string htmlResponse)
        {
            return IndexOfParameterInHTML(htmlResponse, ExpressionPrefix) >= 0;
        }

        private ParameterSoure GetRegExp(string body, int pos, string parameterName, string value)
        {
            switch (GetBodyType(body, pos, parameterName, value))
            {
                case VariableType.HTML:
                    // \b<variableName>=([^&$]*)
                    // holaquetal="1"&jhoholaquetal="2"&holaquetal="3"&hholaquetal="4"&holaquetal="5"
                    // matches "1", "3", "5"
                    // http://stackoverflow.com/questions/2552428/regex-use-start-of-line-end-of-line-signs-or-in-different-context
                    return new RegExpExtractor(1, "\\b" + parameterName + "=([^&$]*)",
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
            var htmlTag = GetTagThatContainsValue(body, value);
            if (htmlTag != null && htmlTag.Contains(key))
            {
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
            return html.Trim().StartsWith("<!DOCTYPE html>");
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
            var indexOf = html.IndexOf(value, StringComparison.Ordinal);
            if (indexOf < 0)
            {
                return null;
            }

            int indexIni = indexOf, indexEnd = indexOf;
            for (; indexIni > 0 && html[indexIni] != '<'; indexIni--)
            {
            }

            for (; indexEnd < html.Length && html[indexEnd] != '>'; indexEnd++)
            {
            }

            return html.Substring(indexIni, indexEnd - indexIni);
        }

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
    }
}
