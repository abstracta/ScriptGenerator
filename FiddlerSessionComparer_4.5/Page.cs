using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Abstracta.FiddlerSessionComparer.Utils;
using Fiddler;
using Logger = Abstracta.FiddlerSessionComparer.Utils.Logger;

namespace Abstracta.FiddlerSessionComparer
{
    public class Page
    {
        private readonly List<Parameter> _parametersToUse, _parametersToExtract;
        private readonly string _headers;

        private string _url;
        private string _bodyOfPost;

        public int Id { get; set; }
        public int ResponseCode { get; private set; }
        public string HTTPMethod { get; set; }
        public string HTTPResponse { get; set; }
        public string RefererURL { get; set; }
        public Page Referer { get; set; }
        public List<Page> Followers { get; set; }

		/// <summary>
        /// parameterize the body of a post request.
        /// </summary>
        public string Body
        {
            get
            {
                if (!FiddlerSessionComparer.ReplaceInBodies)
                {
                    return _bodyOfPost;
                }

                foreach (var parameter in GetParametersToUseInBody())
                {
                    var parameterPage = parameter.GetFirstParameterPagesOfPageAndType(this, UseToReplaceIn.Body);

                    if (!parameterPage.IsSourceOfValueDefined())
                    {
                        Logger.GetInstance().Log("SourceOfValue is not defined: " + parameter);
                        continue;
                    }

                    // for each parameter replace it as escaped and as plain string .. todo have to improve this
                    var replaceValue = FiddlerSessionComparer.EscapeString(parameterPage.Replacement.ReplaceValue);
                    var replaceWith = FiddlerSessionComparer.EscapeString(parameterPage.Replacement.ReplaceWith);

                    // this is specific for JMeter, todo: decouple this 
                    var variable = "${__urlencode(${" + parameter.VariableName + "})}";
                    replaceWith = replaceWith.Replace(Parameter.EscapedDefaultVariableName, variable);

					// only replace strings larger or equal than 5 chars
                    if (_bodyOfPost.Contains(replaceValue) && replaceValue.Length >= 5)
                    {
                        _bodyOfPost = _bodyOfPost.Replace(replaceValue, replaceWith);
                    }
                    else
                    {
                        replaceValue = parameterPage.Replacement.ReplaceValue;
                        replaceWith = parameterPage.Replacement.ReplaceWith.Replace(Parameter.DefaultVariableName, variable);
						
						// only replace strings larger or equal than 5 chars
                        if (_bodyOfPost.Contains(replaceValue) && replaceValue.Length >= 5)
                        {
                            _bodyOfPost = _bodyOfPost.Replace(replaceValue, replaceWith);
                        }
                        else
                        {
                            Logger.GetInstance().Log("Parameter not replaced in page (" + Id + "): " + parameter);
                        }
                    }
                }

                return _bodyOfPost;
            }

            set { _bodyOfPost = value; }
        }

        /// <summary>
        /// parameterize the url of a request
        /// </summary>
        public string FullURL
        {
            get
            {
                // it does just replacements by values, todo: replace by key, value when it's possible
                foreach (var parameter in GetParametersToUseInURL())
                {
                    var parameterPage = parameter.GetFirstParameterPagesOfPageAndType(this, UseToReplaceIn.Url);

                    var replaceValue = parameterPage.Replacement.ReplaceValue;
                    var replaceWith = parameterPage.Replacement.ReplaceWith;

                    // this is specific for JMeter, todo: decouple this 
                    var variable = "${__urlencode(${" + parameter.VariableName + "})}";
                    replaceWith = replaceWith.Replace(Parameter.DefaultVariableName, variable);

                    if (_url.Contains(replaceValue))
                    {
                        _url = _url.Replace(replaceValue, replaceWith);
                    }
                    else
                    {
                        // try replace after escaping the values
                        replaceValue = FiddlerSessionComparer.EscapeString(parameterPage.Replacement.ReplaceValue);
                        replaceWith = FiddlerSessionComparer.EscapeString(parameterPage.Replacement.ReplaceWith);
                        replaceWith = replaceWith.Replace(Parameter.EscapedDefaultVariableName, variable);

                        if (_url.Contains(replaceValue))
                        {
                            _url = _url.Replace(replaceValue, replaceWith);
                        }
                        else
                        {
                            // CASE ANAPLAN -> '%22di%22:2714'
                            // just the '"' are scaped, but the ':' characters aren't scaped   :S
                            replaceValue = parameterPage.Replacement.ReplaceValue.Replace("\"", "%22");
                            replaceWith = parameterPage.Replacement.ReplaceWith.Replace("\"", "%22");
                            replaceWith = replaceWith.Replace(Parameter.DefaultVariableName, variable);

                            if (_url.Contains(replaceValue))
                            {
                                _url = _url.Replace(replaceValue, replaceWith);
                            }
                            else
                            {
                                Logger.GetInstance().Log("Parameter not replaced in page (" + Id + "): " + parameter);
                            }
                        }
                    }
                }

                return _url;
            }

            set { _url = value; }
        }

        public Page(Page referer, string uri, string body, string htmlResponse, string httpMethod, string headers, int responseCode)
        {
            Referer = referer;
            _url = uri;
            HTTPMethod = httpMethod;
            Body = body;
            HTTPResponse = htmlResponse;
            _headers = headers;
            ResponseCode = responseCode;

            Followers = new List<Page>();

            _parametersToUse = new List<Parameter>();
            _parametersToExtract = new List<Parameter>();
        }

        public void AddParameterToUse(Parameter parameter, UseToReplaceIn useToReplaceIn, ParameterContext pContext)
        {
            if (parameter == null)
            {
                return;
            }

            // Problem: When the Page has more than one follower, and two of them use the same variable, they may be repeated
            // Solution: Add only if the parameter is not in the list.
            if (!_parametersToUse.Any(p => p.VariableName == parameter.VariableName &&
                                           p.AreValuesTheSame(parameter)))
            {
                _parametersToUse.Add(parameter);
            }

            parameter.AddParameterPage(this, useToReplaceIn, pContext);
        }

        public Parameter AddParameterToExtract(Parameter parameter, ParameterContext pContext)
        {
            return AddParameterToExtract(parameter, this, pContext);
        }

        /// <summary>
        /// If the parameter is found in the static list of created parameters, add that parameter instead of the new and returns it.
        /// If the parameter isn't found, then the parameter prefix and the value is searched in the response. 
        ///  If it's found then, the parameter is added to this page in the _parametersToExtract list
        ///  If it isn't found, search in the follower pages to add the parameter to them
        ///  If it still isn't found, then create a default extracto for the parameter
        /// </summary>
        /// <param name="parameter">The parameter that is going to be added in the Page hierarchie</param>
        /// <param name="discardedPage">The page that uses the parameter</param>
        /// <param name="pContext">Used to select the matching criteria to find another parameter</param>
        /// <returns></returns>
        public Parameter AddParameterToExtract(Parameter parameter, Page discardedPage, ParameterContext pContext)
        {
            if (parameter == null)
            {
                return null;
            }
            
            // search the parameter in the Parameters list
            var mp = Parameter.FindMatchingParameter(parameter, pContext);
            if (mp != null)
            {
                // mp has same values and same prefix
                return mp;
            }

            var extractFrom = FindParameterValueInResponse(parameter);
            if (extractFrom != ExtractFrom.None)
            {
                parameter.ExtractFromPage = this;
                parameter.ExtractFromSection = extractFrom;

                // setting the regular expression
                parameter.SetExtractorOfParameter();
                _parametersToExtract.Add(parameter);
            }
            // Search the parameter value in the response of other Pages
            else
            {
                // Search the parameter in the HTTP response of the followers
                // Example: (1) -> GET HTML; 
                //          (2) -> GET OR POST AJAX REQUEST { UPDATE HTML }; 
                //          (3) -> GET OR POST { WITH UPDATED HTML }
                // GET / POST OF (2) AND (3) HAVE THE SAME REFERER, but the value searched is in the response of (2). 
                // The value searched isn't in the response of (1)
                var follower = Followers.FirstOrDefault(f => f != discardedPage && 
                                                        f.FindParameterValueInResponse(parameter) != ExtractFrom.None);
                if (follower != null)
                {
                    extractFrom = follower.FindParameterValueInResponse(parameter);

                    parameter.ExtractFromPage = follower;
                    parameter.ExtractFromSection = extractFrom;
                    parameter.SetExtractorOfParameter();
                    
                    follower._parametersToExtract.Add(parameter);
                }
                else
                {
                    parameter.SetAsConstant();
                    _parametersToExtract.Add(parameter);

                    Logger.GetInstance().Log("Didn't find a page (from id: " + Id + ") to assign a parameter to extract: " + parameter);
                }
            }

            // if there is another parameter with the same name, we have to change the names
            var p1 = Parameter.GetParameterWithVariableName(parameter.VariableName);
            if (p1 != null)
            {
                var pName = parameter.VariableName;

                p1.ChangeVariableNameTo(NameFactory.GetInstance().GetNewName(pName));
                parameter.ChangeVariableNameTo(NameFactory.GetInstance().GetNewName(pName));
            }

            Parameter.AddParameterToMainList(parameter, pContext);
            return parameter;
        }

        /// <summary>
        /// Returns the referer page of the actual page
        /// </summary>
        /// <param name="referer">referer uri</param>
        /// <param name="childId">child fiddler id</param>
        /// <returns>Referer page</returns>
        public Page FindRefererPage(string referer, int childId)
        {
            // Compares backwards
            for (var i = Followers.Count -1 ; i >= 0; i--)
            {
                var pageResult = Followers[i].FindRefererPage(referer, childId);
                if (pageResult != null)
                {
                    return pageResult;
                }
            }

            // compares the current's page uri with the referers uri, 
            // makes sure the referal page is prior to the child by comparing fiddler ids
            if (_url == referer && childId > Id)
            {
                return this;
            } 
            
            return null;
        }

		/// <summary>
        /// Returns the Page of the request received as parameter.
        /// </summary>
        /// <param name="httpReq">Request</param>
        /// <returns>Page of request</returns>
        public Page FindSubPage(Session httpReq)
        {
            return httpReq.id == Id
                ? this
                : Followers.Select(page => page.FindSubPage(httpReq)).FirstOrDefault(tmp => tmp != null);
        }

		/// <summary>
        /// Create a Page with the session attributes and insert them in the referer page follower list. 
        /// </summary>
        /// <param name="session">Session</param>
        /// <returns>page created</returns>
        public Page CreateAndInsertPage(Session session)
		{
		    var i = 0;
            var headersArray = new string[session.oResponse.headers.Count()];
		    foreach (var httpResponseHeader in session.oResponse.headers)
		    {
		        headersArray[i] = httpResponseHeader.ToString();
		        i++;
		    }

            var id = session.id;
            var uri = session.fullUrl;
            var urlReferer = session.oRequest.headers["Referer"];
            var referer = FindRefererPage(urlReferer, id);
            var httpMethod = session.oRequest.headers.HTTPMethod;
            var headers = String.Join("\n", headersArray);
		    var responseCode = session.responseCode;

            if (referer == null)
            {
                referer = this;
                urlReferer = String.Empty;
            }

            var body = session.HTTPMethodIs("POST") ? session.GetRequestBodyAsString() : "";
            var htmlResponse = session.GetResponseBodyAsString();

            var result = new Page(referer, uri, body, htmlResponse, httpMethod, headers, responseCode)
            {
                Id = id,
                RefererURL = urlReferer,
            };

            referer.Followers.Add(result);

            return result;
        }

        public List<Parameter> GetParametersToExtract()
        {
            return _parametersToExtract.ToList();
        }

        public List<Parameter> GetParametersToUse()
        {
            return _parametersToUse.ToList();
        }

        public string ToString(string tab, bool printReferer)
        {
            var regExTab = tab + "\t\t";

            var res = tab + _url + ((printReferer && Referer != null) ? ": " + Referer._url : "") + "\n" +
                      regExTab + String.Join("\n", _parametersToExtract.Select(p => p.ToString()).ToArray()) + "\n";

            return Followers.Aggregate(res, (current, follower) => current + follower.ToString(tab + "\t", printReferer));
        }

        public SortedList<int, Page> GetSubPagesList()
        {
            //Make a sorted list of all the Pages of the tree
            var slist = new SortedList<int, Page>();

            AddFollowersToList(this, slist);
                        
            return slist;
        }

        public void AddPreparedParameterToExtract(Parameter parameter, ParameterContext pContext)
        {
            Parameter.AddParameterToMainList(parameter, pContext);

            _parametersToExtract.Add(parameter);
        }

        public void AddPreparedParameterToUse(Parameter parameter)
        {
            _parametersToUse.Add(parameter);
        }

        public List<Parameter> GetParametersToUseInURL()
        {
            return
                _parametersToUse.Where(
                    parameter =>
                    parameter.UsedInPPages.Any(p => p.ParameterTarget == UseToReplaceIn.Url && p.Page == this))
                                .ToList();
        }

        public List<Parameter> GetParametersToUseInBody()
        {
            return
                _parametersToUse.Where(
                    parameter =>
                    parameter.UsedInPPages.Any(p => p.ParameterTarget == UseToReplaceIn.Body && p.Page == this))
                                .ToList();
        }

        public string GetSource(ExtractFrom extractParameterFrom)
        {
            switch (extractParameterFrom)
            {
                case ExtractFrom.Body:
                    return HTTPResponse;

                case ExtractFrom.Headers:
                    return _headers;

                case ExtractFrom.Url:
                    return _url;

                default:
                    return String.Empty;
            }
        }

        public override string ToString()
        {
            return Id + " - " + HTTPMethod + " " + _url + " <" + ResponseCode + ">";
        }

        public static bool IsHTMLResponse(string html)
        {
            if (html.Length < 25)
            {
                return false;
            }

            var h2 = html.TrimStart().Substring(0, 20).ToLower();
            return h2.StartsWith("<!doctype html") || h2.StartsWith("<html");
        }

        public static bool IsJSONResponse(string html)
        {
            return html.Trim().StartsWith("{");
        }

        public static bool IsHTMLAnchorTag(string htmlTag)
        {
            return htmlTag.ToLower().StartsWith("<a ");
        }

        public static bool IsHTMLIframeTag(string htmlTag)
        {
            return htmlTag.ToLower().StartsWith("<iframe ");
        }

        public static bool IsHTMLFormTag(string htmlTag)
        {
            return htmlTag.ToLower().StartsWith("<form ");
        }

        public static string GetTagThatContainsValue(string html, string value)
        {
            return GetTagThatContainsValues(html, value, String.Empty);
        }

        public static string GetTagThatContainsValues(string html, string val1, string val2)
        {
            if (!IsHTMLResponse(html))
            {
                return null;
            }

            // must be unescaped: i.e. '%3Ck2b%20xmlns%3D%22SPU%22' to '<k2b xmlns="SPU"'
            var tmp11 = HttpUtility.UrlDecode(val1);
            var tmp21 = HttpUtility.UrlDecode(val2);

            if (tmp11 == null || tmp21 == null)
            {
                return null;
            }

            // enconde to HTML: i.e. '<k2b xmlns="SPU"' to '&lt;k2b xmlns=&quot;SPU&quot;'
            var tmp12 = HttpUtility.HtmlEncode(tmp11);
            var tmp22 = HttpUtility.HtmlEncode(tmp21);

            tmp12 = ProcessAccents(tmp12);
            tmp22 = ProcessAccents(tmp22);

            // Can't change the html string, because the regular expression must match the HTML unchanged
            var html2 = html.Replace("\n", "").Replace("\r", "");

            while (html2.Length > 0)
            {
                var indexOf = html2.IndexOf(tmp12, StringComparison.Ordinal);
                if (indexOf < 0)
                {
                    return null;
                }

                // Verifies searched value is just it and not another value
                if (
                     (indexOf == 0 && indexOf + tmp12.Length < html2.Length && char.IsLetterOrDigit(html2[indexOf + tmp12.Length]))
                     ||
                     (indexOf > 0 && indexOf + tmp12.Length < html2.Length && (char.IsLetterOrDigit(html2[indexOf - 1]) || char.IsLetterOrDigit(html2[indexOf + tmp12.Length])))
                     ||
                     (indexOf > 0 && tmp12.Length == html2.Length && char.IsLetterOrDigit(html2[indexOf - 1]))
                     ||
                     (indexOf == 0 && tmp12.Length == html2.Length)
                   )
                {
                    html2 = html2.Substring(indexOf + tmp12.Length);
                    continue;
                }

                int indexIni = indexOf, indexEnd = indexOf;
                for (; indexIni > 0 && html2[indexIni] != '<'; indexIni--)
                {
                }

                for (; indexEnd < html2.Length && html2[indexEnd] != '>'; indexEnd++)
                {
                }

                var tag = html2.Substring(indexIni, (indexEnd - indexIni));

                // if tag contains value2
                var indexOf2 = html2.IndexOf(tmp22, StringComparison.Ordinal);
                if (indexOf2 > 0 && !(char.IsLetterOrDigit(html2[indexOf2 - 1]) || char.IsLetterOrDigit(html2[indexOf2 + tmp22.Length])))
                {
                    return tag;
                }

                html2 = html2.Substring(indexOf + tmp12.Length);
            }

            return null;
        }

        public static string ProcessAccents(string value)
        {
            value = value.Replace("&#225;", "á");
            value = value.Replace("&#233;", "é");
            value = value.Replace("&#237;", "í");
            value = value.Replace("&#243;", "ó");
            value = value.Replace("&#250;", "ú");

            return value;
        }

        private ExtractFrom FindParameterValueInResponse(Parameter p)
        {
            // search in body
            if (IsHTMLResponse(HTTPResponse))
            {
                var htmlTag = GetTagThatContainsValues(HTTPResponse, p.ExpressionPrefix, p.Values[0]);

                if (htmlTag != null)
                {
                    return ExtractFrom.Body;
                }
            }
            else // is JSON
            {
                // todo improve this search
                var tmp1 = HttpUtility.UrlDecode(p.Values[0]);
                var tmp2 = HttpUtility.UrlDecode(p.ExpressionPrefix);
                if (tmp1 != null && tmp2 != null)
                {
                    var tmp12 = HttpUtility.HtmlEncode(tmp1);
                    var tmp22 = HttpUtility.HtmlEncode(tmp2);
                    tmp12 = ProcessAccents(tmp12);
                    tmp22 = ProcessAccents(tmp22);

                    if (IsValueInString(tmp12, HTTPResponse) && IsValueInString(tmp22, HTTPResponse))
                    {
                        return ExtractFrom.Body;
                    }
                }
            }

            // search in headers
            if (p.Values[0].Length > 5 && IsValueInString(p.Values[0], _headers))
            {
                return ExtractFrom.Headers;
            }

            // search in url
            if (p.Values[0].Length > 5 && IsValueInString(p.Values[0], _url))
            {
                return ExtractFrom.Url;
            }

            // parameter & value isn't in this page
            return ExtractFrom.None;
        }

        /// <summary>
        /// Searches the value between delimiter characters
        /// </summary>
        /// <param name="value"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        private static bool IsValueInString(string value, string container)
        {
            if (container.Length < value.Length)
            {
                return false;
            }

            if (container.Length == 0)
            {
                return true;
            }

            var index = 0;
            do
            {
                index = container.IndexOf(value, index + 1, StringComparison.Ordinal);
                if (index < 0)
                {
                    return false;
                }

                var cBefore = '"';
                if (index - 1 >= 0)
                {
                    cBefore = container[index - 1];
                }

                var cAfter = '"';
                if (index + value.Length < container.Length)
                {
                    cAfter = container[index + value.Length];
                }

                if (IsDelimiter(cBefore) && IsDelimiter(cAfter))
                {
                    return true;
                }

                if (index + value.Length == container.Length)
                {
                    return false;
                }

            } while (true);
        }

        private static bool IsDelimiter(char character)
        {
            return !Char.IsLetterOrDigit(character);
        }

        private static void AddFollowersToList(Page pag, IDictionary<int, Page> list)
        {
            foreach (var p in pag.Followers)
            {
                list.Add(p.Id, p);
                if (p.Followers.Count > 0)
                {
                    AddFollowersToList(p, list);
                }
            }
        }
    }
}