using System;
using System.Collections.Generic;
using System.Linq;
using Abstracta.FiddlerSessionComparer.Utils;

namespace Abstracta.FiddlerSessionComparer
{
    public enum UseToReplaceIn { Url, Body }

    public enum ExtractFrom { Body, Headers, Url, None }

    public enum VariableType { HTML, JSONString, JSONInt, JSONBool, ToStringedJSON, Undefined }

    public class Parameter
    {
        public const string DefaultVariableName = "!!!!!!!";

        public static string EscapedDefaultVariableName = FiddlerSessionComparer.EscapeString(DefaultVariableName);

        public static List<Parameter> ParametersAlreadyCreated = new List<Parameter>();

        public Page ExtractFromPage { get; set; }

        public string ExpressionPrefix { get; set; }

        public Extractor ParamExtractor { get; set; }

        public string VariableName { get; set; }

        public List<string> Values { get; set; }

        public List<Page> UsedInPages
        {
            get
            {
                var res = new List<Page>();
                foreach (var parameterInPage in UsedInPPages.Where(parameterInPage => !res.Contains(parameterInPage.Page)))
                {
                    res.Add(parameterInPage.Page);
                }

                return res;
            }
        }

        public ExtractFrom ExtractFromSection { get; set; }

        public List<ParameterInPage> UsedInPPages { get; set; }

        public Parameter()
        {
            ExtractFromSection = ExtractFrom.Body;
            UsedInPPages = new List<ParameterInPage>();
        }

        public static void Reset()
        {
            ParametersAlreadyCreated = new List<Parameter>();
        }

        public static void AddParameterToMainList(Parameter parameter, ParameterContext pContext)
        {
            if (FindMatchingParameter(parameter, pContext) == null)
            {
                ParametersAlreadyCreated.Add(parameter);
            }
        }

        /// <summary>
        /// Returns a parameter that matches some conditions:
        /// 1- Same values
        /// 2- If parameter isn't a listValue value, then verify matching in names
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="pContext"></param>
        /// <returns></returns>
        public static Parameter FindMatchingParameter(Parameter parameter, ParameterContext pContext)
        {
            var checkNames = !IsListValueParameter(pContext);

            foreach (var p in ParametersAlreadyCreated)
            {
                var sameValues = p.AreValuesTheSame(parameter);

                if (sameValues)
                {
                    if (checkNames && p.ExpressionPrefix != null && 
                        (p.ExpressionPrefix.Contains(parameter.ExpressionPrefix) 
                        || parameter.ExpressionPrefix.Contains(p.ExpressionPrefix)))
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

        public static bool IsListValueParameter(ParameterContext pContext)
        {
            return ParameterInPage.IsListValueParameter(pContext);
        }

        public bool IsListValueParameter(int indexParameter = 0)
        {
            if (indexParameter < 0 || UsedInPPages.Count <= indexParameter)
            {
                // throw new Exception("IsListValueParameter: " + indexParameter);
                return false;
            }

            return UsedInPPages[indexParameter].IsListValueParameter();
        }

        public override string ToString()
        {
            var values = String.Join("' | '", Values.ToArray());
            var replaceIn = String.Join("','", UsedInPPages.Select(p => p.Page.Id + "-" + p.ParameterTarget).ToArray());

            return "{ " +
                   "ExtractedFromPage='" + ExtractFromPage.Id + "' " +
                   "VariableName='" + VariableName + "' " +
                   "ExpressionPrefix='" + ExpressionPrefix + "' " +
                   "Values=['" + values + "'] " +
                   "UseInPages=['" + replaceIn + "'] " +
                   "}";
        }

        public bool ExtractFromHeaders()
        {
            return ExtractFromSection == ExtractFrom.Headers;
        }

        public void SetExtractorOfParameter()
        {
            // todo, add parameter "ExtractorType" -> { regex, between, etc. }
            var responseCopy = ExtractFromPage.GetSource(ExtractFromSection);
            
            var pos = IndexOfParameterInResponse(responseCopy, ExpressionPrefix);
            if (pos < 0)
            {
                Logger.GetInstance()
                      .Log("Error when setting extractor: Can't find variable in the body of Page " + ExtractFromPage.Id + ": " +
                           ExpressionPrefix + ". => Using default extractor");
                ParamExtractor = GetDefaultExtractor(ExpressionPrefix);
                return;
            }

            ParamExtractor = GetRegExp(responseCopy, pos, ExpressionPrefix, Values[0]);
        }

        public void SetDefaultExtractor()
        {
            ParamExtractor = GetDefaultExtractor(ExpressionPrefix);
        }

        public void ChangeVariableNameTo(string newName)
        {
            VariableName = newName;
        }

        public void AddParameterPage(Page page, UseToReplaceIn useToReplaceIn, ParameterContext pContext)
        {
            var pInPage = new ParameterInPage(this, page, pContext, useToReplaceIn);

            if (FindMatchingParameterInPage(pInPage) == null)
            {
                UsedInPPages.Add(pInPage);    
            }
        }

        public void AddParameterPage(Page page, UseToReplaceIn useToReplaceIn, string replaceValue, string replaceWith)
        {
            var pInPage = new ParameterInPage(this, page, ParameterContext.Default, useToReplaceIn, replaceValue, replaceWith);

            if (FindMatchingParameterInPage(pInPage) == null)
            {
                UsedInPPages.Add(pInPage);
            }
        }

        public List<ParameterInPage> GetParameterPagesOfPage(Page page)
        {
            return UsedInPPages.Where(p => p.Page == page).ToList();
        }

        public ParameterInPage GetFirstParameterPagesOfPageAndType(Page page, UseToReplaceIn useToReplaceIn)
        {
            return UsedInPPages.FirstOrDefault(p => p.Page == page && p.ParameterTarget == useToReplaceIn);
        }

        public bool AreValuesTheSame(Parameter parameter)
        {
            var sameValues = true;
            for (var i = 0; i < Values.Count; i++)
            {
                sameValues = sameValues && Values[i] == parameter.Values[i];
            }

            return sameValues;
        }

        public IEnumerable<UseToReplaceIn> TargetsOfPage(Page page)
        {
            return
                (from parameterInPage in UsedInPPages
                 where parameterInPage.Page == page
                 select parameterInPage.ParameterTarget).ToList();
        }

        public void SetAsConstant()
        {
            ParamExtractor = new Extractor();
        }
        
        public static bool ContainsParameterWithVariableName(string variableName)
        {
            return GetParameterWithVariableName(variableName) != null;
        }

        public static Extractor GetDefaultExtractor(string expressionPrefix)
        {
            return new RegExpExtractor(1, expressionPrefix + "=([^&$]*)");
        }

        public static Parameter GetParameterWithVariableName(string variableName)
        {
            return ParametersAlreadyCreated.FirstOrDefault(parameter => parameter.VariableName == variableName);
        }

        private ParameterInPage FindMatchingParameterInPage(ParameterInPage pInPage)
        {
            return UsedInPPages.FirstOrDefault(pip => pip.Page == pInPage.Page && pip.ParameterTarget == pInPage.ParameterTarget);
        }

        /*
        /// <summary>
        /// todo: unify this method with 'SetRegularExpressionOfParameter' method
        /// </summary>
        /// <param name="sourceOfValue">Character string to filter the regular expression</param>
        public void SetRegularExpressionOfParameterUsedInURL(string sourceOfValue)
        {
            // Options:
             //   sourceOfValue is a HTML <a ... href="contenedorpestanas?INS,2,60,893,0,0,,ConsultaActual" ...>...</a>
             //   sourceOfValue is a HTML <IFRAME ... src="contenedorpestanas?INS,2,60,893,0,0,," ...>...</IFRAME>
             //   sourceOfValue is a HTML <form ... action="historiaclinicaprincipalv2?INS,2,60,893" ...> 
             //   sourceOfValue is a JSON {"gxCommands":[{"redirect":{"url":"historiaclinicaprincipalv2?INS,2,60,893","forceDisableFrm":1}}]}
             // 
             // Need a different regular expression for each case
             // The problem: sourceOfValue can contain several of each option
            //

            string regExp, replaceValue, replaceWith;
            CalculateReplacementFromContext(out replaceValue, out replaceWith);

            if (Page.IsHTMLResponse(sourceOfValue))
            {
                var htmlTag = Page.GetTagThatContainsValue(sourceOfValue, _p.Values[0]);
                if (htmlTag == null)
                {
                    // need to set a default value. When this executes, means ExpressionPrefix or body are incorrect
                    Logger.GetInstance().Log("ERROR: Can't find a variable in the source: " + _p.ExpressionPrefix);
                    SetDefaultSourceOfValue();
                    return;
                }

                if (Page.IsHTMLIframeTag(htmlTag))
                {
                    // <IFRAME .*src=".*hinicionucleo\?([^"]+)".*>
                    regExp = "<IFRAME .*src=\".*" + _p.ExpressionPrefix + "\\?([^\"]+)\".*>";
                }
                else if (Page.IsHTMLAnchorTag(htmlTag))
                {
                    regExp = "<a .*href=\".*" + _p.ExpressionPrefix + "\\?([^\"]+)\".*>";
                }
                else if (Page.IsHTMLFormTag(htmlTag))
                {
                    regExp = "<form .*action=\".*" + _p.ExpressionPrefix + "\\?([^\"]+)\".*>";
                }
                else
                {
                    Logger.GetInstance().Log("ERROR: Don't know what an HTML tag is: " + htmlTag);

                    regExp = "\"" + _p.ExpressionPrefix + "\\?([^\"]*)\"";
                }
            }
            else if (Page.IsJSONResponse(sourceOfValue))
            {
                regExp = "\"" + _p.ExpressionPrefix + "\\?([^\"]+)\"";
            }
            else if (_p.ExtractFromHeaders())
            {
                // matches http && https
                regExp = @"Location: http.?://.*\?(.*)";
            }
            else
            {
                Logger.GetInstance().Log("ERROR: Can't calculate RegExpExtractor. Using default for parameter: " + this);
                Logger.GetInstance().Log("ERROR: Body isn't HTML and also isn't JSON: " + sourceOfValue);

                regExp = "\"" + _p.ExpressionPrefix + "\\?([^\"]*)\"";
            }

            Replacement = new RegExpExtractor(1, regExp, replaceValue, replaceWith);
        }

        // */

        private static Extractor GetRegExp(string body, int pos, string expressionPrefix, string value)
        {
            switch (GetBodyType(body, pos, expressionPrefix, value))
            {
                case VariableType.HTML:
                    // \b<variableName>=([^&$]*)
                    // holaquetal="1"&jhoholaquetal="2"&holaquetal="3"&hholaquetal="4"&holaquetal="5"
                    // matches "1", "3", "5"
                    // http://stackoverflow.com/questions/2552428/regex-use-start-of-line-end-of-line-signs-or-in-different-context
                    return new RegExpExtractor(1, /*"\\b" + */expressionPrefix + "=([^&$]*)");
                    ////parameterName + "=" + value,
                    ////parameterName + "=" + Parameter.DefaultVariableName + "");
                case VariableType.JSONString:
                    return new RegExpExtractor(1, "\"" + expressionPrefix + "\":\"([^\"]*)\"");
                    ////"\"" + parameterName + "\":\"" + value + "\"",
                    ////"\"" + parameterName + "\":\"" + Parameter.DefaultVariableName + "\"");
                case VariableType.JSONInt:
                    return new RegExpExtractor(1, "\"" + expressionPrefix + "\":([\\d]+(\\.[\\d]+)?)");
                    ////"\"" + parameterName + "\":" + value + "",
                    ////"\"" + parameterName + "\":" + Parameter.DefaultVariableName + "");

                case VariableType.JSONBool:
                    return new RegExpExtractor(1, "\"" + expressionPrefix + "\":(true|false|null)[,|\\}|\\]]");
                    ////"\"" + parameterName + "\":" + value + "",
                    ////"\"" + parameterName + "\":" + Parameter.DefaultVariableName + "");

                case VariableType.ToStringedJSON:
                    return new RegExpExtractor(1, "\"" + expressionPrefix + "\\\\\",\\\\\"([^\\\\\"]*)\\\\\"");
                    ////"\"" + parameterName + "\\\\\",\\\\\"" + value + "\\\\\"",
                    ////"\"" + parameterName + "\\\\\",\\\\\"" + Parameter.DefaultVariableName + "\\\\\"");
                default:
                    // VariableName += "_UndefinedTypeParameter";
                    // Utils.Logger.GetInstance().Log("Parameter context format (HTML, JSON) undefined: " + this);
                    return GetDefaultExtractor(expressionPrefix);
            }
        }

        private static VariableType GetBodyType(string body, int pos, string key, string value)
        {
            // checks if there is a tag in the HTML response, that contains the value and the key
            var htmlTag = Page.GetTagThatContainsValues(body, key, value);
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

                if (Char.IsNumber(body[pos + key.Length + 2]))
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
        
        private static int IndexOfParameterInResponse(string htmlResponse, string parameterName)
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

    public class ParameterInPage
    {
        private readonly Parameter _p;

        public Page Page { get; set; }

        public Replacement Replacement { get; set; }

        public UseToReplaceIn ParameterTarget { get; set; }

        public ParameterContext ContextWhereParameterIsUsed { get; set; }

        public ParameterInPage(Parameter parameter, Page page, ParameterContext parameterContext, UseToReplaceIn parameterTarget)
        {
            _p = parameter;

            Page = page;
            ParameterTarget = parameterTarget;
            ContextWhereParameterIsUsed = parameterContext;

            string replaceValue, replaceWith;
            CalculateReplacementFromContext(out replaceValue, out replaceWith);

            Replacement = new Replacement(replaceWith, replaceValue);
        }

        public ParameterInPage(Parameter parameter, Page page, ParameterContext parameterContext,
                               UseToReplaceIn parameterTarget, string replaceValue, string replaceWith)
        {
            _p = parameter;

            Page = page;
            ParameterTarget = parameterTarget;
            ContextWhereParameterIsUsed = parameterContext;
            Replacement = new Replacement(replaceWith, replaceValue);
        }

        public bool IsListValueParameter()
        {
            return IsListValueParameter(ContextWhereParameterIsUsed);
        }

        public static bool IsListValueParameter(ParameterContext pContext)
        {
            return pContext == ParameterContext.AloneValue
                   || pContext == ParameterContext.FirstComaSeparatedValue
                   || pContext == ParameterContext.ComaSeparatedValue
                   || pContext == ParameterContext.LastComaSeparatedValue;
        }

        public bool IsSourceOfValueDefined()
        {
            return Replacement != null;
        }

        private void CalculateReplacementFromContext(out string replaceValue, out string replaceWith)
        {
            switch (ContextWhereParameterIsUsed)
            {
                case ParameterContext.XMLAttribute:
                case ParameterContext.KeyEqualValue:
                    replaceValue = _p.ExpressionPrefix + "=" + _p.Values[0];
                    replaceWith = _p.ExpressionPrefix + "=" + Parameter.DefaultVariableName;
                    break;

                case ParameterContext.JSonNumberValue:
                    replaceValue = "\"" + _p.ExpressionPrefix + "\":" + _p.Values[0];
                    replaceWith = "\"" + _p.ExpressionPrefix + "\":" + Parameter.DefaultVariableName;
                    break;

                case ParameterContext.JSonStringValue:
                    replaceValue = "\"" + _p.ExpressionPrefix + "\":\"" + _p.Values[0] + "\"";
                    replaceWith = "\"" + _p.ExpressionPrefix + "\":\"" + Parameter.DefaultVariableName + "\"";
                    break;

                case ParameterContext.FirstComaSeparatedValue:
                    replaceValue = _p.Values[0] + ",";
                    replaceWith = Parameter.DefaultVariableName + ",";
                    break;

                case ParameterContext.ComaSeparatedValue:
                    replaceValue = "," + _p.Values[0] + ",";
                    replaceWith = "," + Parameter.DefaultVariableName + ",";
                    break;

                case ParameterContext.LastComaSeparatedValue:
                    replaceValue = "," + _p.Values[0];
                    replaceWith = "," + Parameter.DefaultVariableName;
                    break;

                default:
                    replaceValue = _p.Values[0];
                    replaceWith = Parameter.DefaultVariableName;
                    break;
            }
        }
    }
}
