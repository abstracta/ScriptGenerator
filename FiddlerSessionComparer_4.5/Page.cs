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

        public string Body
        {
            get
            {
                foreach (var parameter in _parametersToUse.Where(parameter => parameter.ParameterTarget == UseToReplaceIn.Body))
                {
                    if (parameter.RegularExpressionExtractor == null)
                    {
                        Utils.Logger.GetInstance().Log("Something bad happened. RegularExpressionExtractor is null: " + parameter);
                        continue;
                    }
                    // for each parameter replace it as escaped and as plain string .. maybe have to improve this
                    var replaceValue = FiddlerSessionComparer.EscapeString(parameter.RegularExpressionExtractor.ReplaceValue);
                    var replaceWith = FiddlerSessionComparer.EscapeString(parameter.RegularExpressionExtractor.ReplaceWith);

                    var variable = "${__urlencode(${" + parameter.ExpressionPrefix + "})}";
                    replaceWith = replaceWith.Replace(Parameter.EscapedDefaultVariableName, variable);

                    if (_bodyOfPost.Contains(replaceValue))
                    {
                        _bodyOfPost = _bodyOfPost.Replace(replaceValue, replaceWith);
                    }
                    else
                    {
                        replaceValue = parameter.RegularExpressionExtractor.ReplaceValue;
                        replaceWith = parameter.RegularExpressionExtractor.ReplaceWith.Replace(Parameter.DefaultVariableName,
                                                                                     variable);
                        if (_bodyOfPost.Contains(replaceValue))
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

        public string FullURL
        {
            get
            {
                foreach (var parameter in _parametersToUse.Where(parameter => parameter.ParameterTarget == UseToReplaceIn.Url))
                {
                    var replaceValue = parameter.RegularExpressionExtractor.ReplaceValue;
                    var replaceWith = parameter.RegularExpressionExtractor.ReplaceWith;

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

        public string HTMLResponse { get; set; }

        public string RefererURL { get; set; }

        public Page Referer { get; set; }
        
        public List<Page> Followers { get; set; }

        public Page(Page referer, string uri, string body, string htmlResponse)
        {
            Referer = referer;
            Uri = uri;
            _url = uri;
            Body = body;
            HTMLResponse = htmlResponse;

            Followers = new List<Page>();

            _parametersToUse = new List<Parameter>();
            _parametersToExtract = new List<Parameter>();
        }

        public void AddParameterToUse(Parameter parameter)
        {
            if (parameter == null)
            {
                return;
            }

            if (parameter.RegularExpressionExtractor == null)
            {
                Utils.Logger.GetInstance().Log("ERROR: parameter.RegularExpressionExtractor == null in AddParameterToUse: " + parameter);
            }

            // When the Page has more than one follower, and two of them use the same variable, they may be repeated
            if (_parametersToUse.All(p => p.ExpressionPrefix != parameter.ExpressionPrefix))
            {
                _parametersToUse.Add(parameter);    
            }
        }

        public Parameter AddParameterToExtract(Parameter parameter)
        {
            if (parameter == null)
            {
                return null;
            }

            if (parameter.ParameterTarget == UseToReplaceIn.Body)
            {
                # region Parameter used in a Body of a POST
                // When the Page has more than one follower, and two of them use the same variable, they may be repeated
                if (_parametersToExtract.Any(p => p.ExpressionPrefix == parameter.ExpressionPrefix))
                {
                    return _parametersToExtract.First(p => p.ExpressionPrefix == parameter.ExpressionPrefix);
                }

                // If the parameter can be found in the HTML, extract from this page
                if (parameter.IsContainedInHTML(HTMLResponse))
                {
                    // setting the regular expression
                    parameter.SetRegularExpressionOfParameterFromBody(HTMLResponse);
                    _parametersToExtract.Add(parameter);
                }
                    // Search the parameter in the HTML response of the followers
                    // Case (1) -> GET HTML; (2) -> GET OR POST { UPDATE HTML }; (3) -> GET OR POST { WITH UPDATED HTML }
                    // GET / POST OF (2) AND (3) HAVE THE SAME REFERER
                else
                {
                    var follower = Followers.FirstOrDefault(f => parameter.IsContainedInHTML(f.HTMLResponse));
                    if (follower != null)
                    {
                        parameter = follower.AddParameterToExtract(parameter);
                        parameter.ExtractedFromPage = follower;
                    }
                    else
                    {
                        Utils.Logger.GetInstance().Log("Didn't find a page (from id: " + Id + ") to assign a parameter to extract: " + parameter);
                        return null;
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

                // Search the parameter value in the HTML
                if (HTMLResponse.Contains(parameter.Values[0]))
                {
                    // setting the regular expression
                    parameter.SetRegularExpressionOfParameterFromURL(HTMLResponse);
                    _parametersToExtract.Add(parameter);
                }
                else
                {
                    // it can be a redirect from another page
                    var follower = Followers.FirstOrDefault(f => f.HTMLResponse.Contains(parameter.Values[0]));
                    if (follower != null)
                    {
                        parameter = follower.AddParameterToExtract(parameter);
                        parameter.ExtractedFromPage = follower;
                    }
                    else
                    {
                        Utils.Logger.GetInstance().Log("Didn't find a page (from id: " + Id + ") to assign a parameter to extract: " + parameter);
                        return null;
                    }
                }

                # endregion
            }

            return parameter;
        }

        public Page FindRefererPage(string referer, int childId)
        {
            // compares the current's page uri with the referers uri, and makes sure the referal page is prior to the child by comparing fiddler ids
            return (Uri == referer && childId > Id)
                       ? this
                       : Followers.Select(follower => follower.FindRefererPage(referer, childId))
                                  .FirstOrDefault(result => result != null);
        }

        public Page FindSubPage(Session httpReq)
        {
            return httpReq.id == Id
                ? this
                : Followers.Select(page => page.FindSubPage(httpReq)).FirstOrDefault(tmp => tmp != null);
        }

        public Page CreateAndInsertPage(Session session)
        {
            var id = session.id;
            var uri = session.fullUrl;
            var urlReferer = session.oRequest.headers["Referer"];
            var referer = FindRefererPage(urlReferer, id);

            if (referer == null)
            {
                referer = this;
                urlReferer = string.Empty;
            }

            var body = session.HTTPMethodIs("POST") ? session.GetRequestBodyAsString() : "";
            var htmlResponse = session.GetResponseBodyAsString();

            var result = new Page(referer, uri, body, htmlResponse)
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
    }
}