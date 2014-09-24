using System;
using System.Collections.Generic;
using System.Linq;
using Fiddler;

namespace Abstracta.FiddlerSessionComparer
{
    public class Page
    {
        private readonly List<Parameter> _parametersToUse, _parametersToExtract;

        public int Id { get; set; }

        public string Uri { get; set; }

        private string _bodyOfPost;

        private readonly string _headers;

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

                foreach (var parameter in _parametersToUse.Where(parameter => parameter.ParameterTarget == UseToReplaceIn.Body))
                {
                    if (!parameter.IsSourceOfValueDefined())
                    {
                        // todo: all parameters that haven't source of value, must have a constant
                        Utils.Logger.GetInstance().Log("SourceOfValue is not defined: " + parameter);
                        continue;
                    }

                    // for each parameter replace it as escaped and as plain string .. maybe have to improve this
                    var replaceValue = FiddlerSessionComparer.EscapeString(parameter.SourceOfValue.ReplaceValue);
                    var replaceWith = FiddlerSessionComparer.EscapeString(parameter.SourceOfValue.ReplaceWith);

                    // this is specific for JMeter, todo: decouple this 
                    var variable = "${__urlencode(${" + parameter.ExpressionPrefix + "})}";
                    replaceWith = replaceWith.Replace(Parameter.EscapedDefaultVariableName, variable);

					// only replace strings larger or equal than 5 chars
                    if (_bodyOfPost.Contains(replaceValue) && replaceValue.Length >= 5)
                    {
                        _bodyOfPost = _bodyOfPost.Replace(replaceValue, replaceWith);
                    }
                    else
                    {
                        replaceValue = parameter.SourceOfValue.ReplaceValue;
                        replaceWith = parameter.SourceOfValue.ReplaceWith.Replace(Parameter.DefaultVariableName, variable);
						

						// only replace strings larger or equal than 5 chars
                        if (_bodyOfPost.Contains(replaceValue) && replaceValue.Length >= 5)
                        {
                            _bodyOfPost = _bodyOfPost.Replace(replaceValue, replaceWith);
                        }
                        else
                        {
                            Utils.Logger.GetInstance().Log("Parameter not replaced in page (" + Id + "): " + parameter);
                        }
                    }
                }

                return _bodyOfPost;
            }

            set { _bodyOfPost = value; }
        }

        private string _url;

        public string HTTPMethod { get; set; }

        public int ResponseCode { get; private set; }

        /// <summary>
        /// parameterize the url of a request
        /// </summary>
        public string FullURL
        {
            get
            {
                foreach (var parameter in _parametersToUse.Where(parameter => parameter.ParameterTarget == UseToReplaceIn.Url))
                {
                    var replaceValue = parameter.SourceOfValue.ReplaceValue;
                    var replaceWith = parameter.SourceOfValue.ReplaceWith;

                    if (_url.Contains(replaceValue))
                    {
                        _url = _url.Replace(replaceValue, replaceWith);
                    }
                    else
                    {
                        Utils.Logger.GetInstance().Log("Parameter not replaced in page (" + Id + "): " + parameter);
                    }
                }

                return _url;
            }

            set { _url = value; }
        }

        public string HTTPResponse { get; set; }

        public string RefererURL { get; set; }

        public Page Referer { get; set; }
        
        public List<Page> Followers { get; set; }

        public Page(Page referer, string uri, string body, string htmlResponse, string httpMethod, string headers, int responseCode)
        {
            Referer = referer;
            Uri = uri;
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

		/// <summary>
        /// Add a parameter in the list of parameters to use
        /// </summary>
        /// <param name="parameter">Parameter to add</param>
        public void AddParameterToUse(Parameter parameter)
        {
            if (parameter == null)
            {
                return;
            }

            if (parameter.SourceOfValue == null)
            {
                Utils.Logger.GetInstance().Log("ERROR: parameter.SourceOfValue == null in AddParameterToUse: " + parameter);
            }

            // Problem: When the Page has more than one follower, and two of them use the same variable, they may be repeated
            // Solution: Add only if the parameter is not in the list.
            if (_parametersToUse.All(p => p.ExpressionPrefix != parameter.ExpressionPrefix))
            {
                _parametersToUse.Add(parameter);
                parameter.UsedInPages.Add(this);
            }
        }

        public Parameter AddParameterToExtract(Parameter parameter)
        {
            if (parameter == null)
            {
                return null;
            }
            
            // search the parameter in the Parameters list
            var mp = Parameter.GetMatchingParameter(parameter);
            if (mp != null)
            {
                return mp;
            }
            
            if (parameter.ParameterTarget == UseToReplaceIn.Body)
            {
                # region Parameter used in a Body of a POST
                // When this Page has more than one follower, and two of them use the same parameter, I don't need to extract it twice. Just return it.
                if (_parametersToExtract.Any(p => p.ExpressionPrefix == parameter.ExpressionPrefix))
                {
                    return _parametersToExtract.First(p => p.ExpressionPrefix == parameter.ExpressionPrefix);
                }

                // If the parameter can be found in the HTML, extract from this page
                if (parameter.IsContainedInResponse(HTTPResponse))
                {
                    // setting the regular expression
                    parameter.SetRegularExpressionOfParameterFromBody(HTTPResponse);
                    _parametersToExtract.Add(parameter);
                }
                    // Search the parameter in the HTML response of the followers
                    // Example: (1) -> GET HTML; 
                    //          (2) -> GET OR POST AJAX REQUEST { UPDATE HTML }; 
                    //          (3) -> GET OR POST { WITH UPDATED HTML }
                    // GET / POST OF (2) AND (3) HAVE THE SAME REFERER, but the value searched is in the response of (2). 
                    // The value searched isn't in the response of (1)
                else
                {
                    // Skip the pages that use the parameter
                    var follower = Followers.FirstOrDefault(f => 
                                                    !parameter.UsedInPages.Contains(f) && 
                                                    parameter.IsContainedInResponse(f.HTTPResponse));

                    if (follower != null)
                    {
                        parameter = follower.AddParameterToExtract(parameter);
                        parameter.ExtractedFromPage = follower;
                    }
                    else
                    {
                        parameter.SetDefaultSourceOfValue();
                        _parametersToExtract.Add(parameter);

                        Utils.Logger.GetInstance().Log("Didn't find a page (from id: " + Id + ") to assign a parameter to extract: " + parameter); 
                    }
                }

                # endregion
            }
            else
            {
                # region Parameter used in an URL of a GET / POST

                // When the Page has more than one follower, and two of them use the same variable, they may be repeated
                if (_parametersToExtract.Any(p => p.Values[0] == parameter.Values[0]))
                {
                    return _parametersToExtract.First(p => p.Values[0] == parameter.Values[0]);
                }

                // Search the parameter value in this response
                var extractFrom = FindParameterInResponse(parameter.Values[0]);
                if (extractFrom != ExtractFrom.None)
                {
                    parameter.ExtractParameterFrom = extractFrom;

                    // setting the regular expression
                    parameter.SetRegularExpressionOfParameterFromURL(GetSource(extractFrom));
                    _parametersToExtract.Add(parameter);
                }
                // Search the parameter value in the response of other Pages
                else
                {
                    // it can be a redirect from another page
                    var follower = Followers.FirstOrDefault(f => f.FindParameterInResponse(parameter.Values[0]) != ExtractFrom.None);
                    if (follower != null)
                    {
                        parameter = follower.AddParameterToExtract(parameter);
                        parameter.ExtractedFromPage = follower;
                    }
                    else
                    {
                        parameter.SetDefaultSourceOfValue();
                        _parametersToExtract.Add(parameter);

                        Utils.Logger.GetInstance().Log("Didn't find a page (from id: " + Id + ") to assign a parameter to extract: " + parameter);
                    }
                }

                # endregion
            }

            Parameter.AddParameterToMainList(parameter);
            return parameter;
        }

        private ExtractFrom FindParameterInResponse(string parameterValue)
        {
            if (HTTPResponse.Contains(parameterValue))
            {
                return ExtractFrom.Body;
            }
            
            if (_headers.Contains(parameterValue))
            {
                return ExtractFrom.Headers;
            }

            if (_url.Contains(parameterValue))
            {
                return ExtractFrom.Url;
            }

            return ExtractFrom.None;
        }

        private string GetSource(ExtractFrom extractParameterFrom)
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
                    return string.Empty;
            }
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
            if (Uri == referer && childId > Id)
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
            var id = session.id;
            var uri = session.fullUrl;
            var urlReferer = session.oRequest.headers["Referer"];
            var referer = FindRefererPage(urlReferer, id);
            var httpMethod = session.oRequest.headers.HTTPMethod;
            var headers = string.Join("\n", session.oResponse.headers);
		    var responseCode = session.responseCode;

            if (referer == null)
            {
                referer = this;
                urlReferer = string.Empty;
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

            var res = tab + Uri + ((printReferer && Referer != null) ? ": " + Referer.Uri : "") + "\n" +
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

        public void AddPreparedParameterToExtract(Parameter parameter)
        {
            Parameter.AddParameterToMainList(parameter);

            _parametersToExtract.Add(parameter);
        }

        public void AddPreparedParameterToUse(Parameter parameter)
        {
            _parametersToUse.Add(parameter);
        }

        public void DeleteAllParametersToUseInURL()
        {
            var paramsToUseInURL = _parametersToUse.Where(p => p.ParameterTarget == UseToReplaceIn.Url).ToList();
            foreach (var paramToUseInURL in paramsToUseInURL)
            {
                _parametersToUse.Remove(paramToUseInURL);
            }
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

        public List<Parameter> GetParametersToUseInURL()
        {
            return _parametersToUse.Where(p => p.ParameterTarget == UseToReplaceIn.Url).ToList();
        }
    }
}