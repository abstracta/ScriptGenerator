using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Abstracta.FiddlerSessionComparer;
using Abstracta.Generators.Framework.AbstractGenerator.Extensions;
using Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor;
using Abstracta.Generators.Framework.AbstractGenerator.Validations;
using Fiddler;
using GxTest.Utils.EnumTypes;
using Newtonsoft.Json.Linq;

namespace Abstracta.Generators.Framework.AbstractGenerator
{
    internal abstract class AbstractStep
    {
        internal AbstractStep()
        {
            Requests = new List<AbstractPageRequest>();
        }

        internal string Name { get; set; }

        internal string Desc { get; set; }

        internal string ServerName { get; set; }

        internal string ServerPort { get; set; }

        internal string Host
        {
            get
            {
                return (IsDefaultPort()) ? ServerName : ServerName + ":" + ServerPort;
            }
        }

        internal string WebApp { get; set; }

        internal CommandType Type { get; set; }

        internal void AddRequest(Session httpReq, Page page)
        {
            if (IsInBlackList(httpReq))
            {
                return;
            }

            // find the Page of the request in the Page tree
            if (page != null) 
            {
                page = page.FindSubPage(httpReq);
            }

            var referer = httpReq.oRequest.headers["Referer"];

            // if isn't static resource. Can be a follow redirect of last request or a new request
            if (httpReq.IsPrimaryRequest())
            {
                // this means the current primaryRequest is first request of the step
                if (Requests.Count == 0)
                {
                    var req = CreatePageRequest(httpReq, this, page);
                    Requests.Add(req);
                }
                else
                {
                    var lastPageRequest = Requests.Last();

                    // todo: should use the referer request instead of LastPrimaryRequest?
                    var fiddlerSessionOfLastPageRequest = lastPageRequest.GetLastPrimaryRequest();
                   
                    // the response code of the previous request was a redirect
                    if (fiddlerSessionOfLastPageRequest.IsRedirectByResponseCode())
                    {
                        lastPageRequest.AddFollowRedirect(httpReq, RedirectType.ByResponseCode, page);
                    }
                    // the response code of the previous request was a redirect
                    else if (fiddlerSessionOfLastPageRequest.IsRedirectByJavaScript())
                    {
                        lastPageRequest.AddFollowRedirect(httpReq, RedirectType.ByJavaScript, page);

                        // Add validation to the followRedirect: HTTP "200" + Content "Redirect"
                        if (fiddlerSessionOfLastPageRequest.IsGenexusRedirect())
                        {
                            lastPageRequest.Validations.Clear();
                            lastPageRequest.Validations.Add(CreateAppearTextValidation("redirect", "This HTML should be a javascript redirect", false, true));
                            lastPageRequest.Validations.Add(CreateResponseCodeValidation(200));

                            // This is done just when comparing two fiddler sessions
                            ////var parametersExtractor = CreateRegExpExtractorToGetRedirectParameters(lastPageRequest.FiddlerSession.GetResponseBodyAsString());
                            ////if (parametersExtractor != null)
                            ////{
                            ////    lastPageRequest.ParametersToExtract.Add(parametersExtractor);
                            ////}
                        }
                    }
                    // the last request wasn't a redirect, it's a new request in the step
                    else
                    {
                        // Constructor creates PageRequest with default validations
                        var req = CreatePageRequest(httpReq, this, page);
                        Requests.Add(req);
                    }
                }
            }
            else
            {
                // If secondary request has referer header
                if (!String.IsNullOrEmpty(referer))
                {
                    var refererRequest = GetRequestByUrl(referer);

                    if (refererRequest != null)
                    {
                        refererRequest.AddSecondaryRequest(httpReq);
                    }
                    else
                    {
                        if (Requests.Count > 0)
                        {
                            Requests.Last().AddSecondaryRequest(httpReq);    
                        }
                        else
                        {
                            // Add it as a PrimaryRequest
                            // may be an error on GetRequestByUrl() ?
                            var req = CreatePageRequest(httpReq, this, page);
                            Requests.Add(req);
                        }
                    }
                }
                else
                {
                    Requests.Last().AddSecondaryRequest(httpReq);
                }
            }
        }

        internal void AddValidation(Command command)
        {
            // can't add a validation to a request that doesn't exist
            if (Requests.Count < 1)
            {
                return;
            }

            AbstractValidation val;
            switch (command.Name)
            {
                case "CheckMainObject":
                    var objectName = command.Desc.Split(' ')[1];

                    val = CreateCheckMainObjectValidation(objectName);
                    break;

                case "AppearText":
                    var text = command.Parameters[ParametersType.TextToValidate];
                    var desc = command.Parameters[ParametersType.ErrorDescription];
                    var neg = String.Compare((command.Parameters[ParametersType.NegateValidation]), "true", StringComparison.OrdinalIgnoreCase) == 0;
                    var stop = String.Compare((command.Parameters[ParametersType.StopExecution]), "true", StringComparison.OrdinalIgnoreCase) == 0;

                    val = CreateAppearTextValidation(text, desc, neg, stop);
                    break;

                default:
                    val = CreateDefaultValidation();
                    break;
            }

            Requests[Requests.Count - 1].AddValidation(val);
        }

        internal bool IsDefaultPort()
        {
            return ServerPort == Constants.HTTPConstants.DefaultPortStr;
        }

        internal abstract DefaultValidation CreateDefaultValidation();

        internal abstract CheckMainObjectValidation CreateCheckMainObjectValidation(string objectName);

        internal abstract AppearTextValidation CreateAppearTextValidation(string text, string desc, bool neg, bool stop);

        internal abstract ResponseCodeValidation CreateResponseCodeValidation(int responseCodeValidation, string desc = "", bool neg = false, bool stop = true);

        internal AbstractValidation CreateDefaultValidationFromRequest(Session request)
        {
            var htmlResponse = request.GetResponseBodyAsString();
            if (IsHTMLResponse(htmlResponse))
            {
                var match = Regex.Match(htmlResponse, "<\\s*title\\s*>(.*)</\\s*title\\s*>");
                if (match.Success)
                {
                    var htmlTitle = match.Value;

                    return CreateAppearTextValidation(htmlTitle, "Default validation: validates HTML page title", false, true);
                }

                return CreateDefaultValidation();
            }

            if (IsJSONResponse(htmlResponse))
            {
                // TODO create something specific here
                // return new AppearTextValidation(text, "Default validation: validates JSON string", false, true);
            }

            return CreateDefaultValidation();
        }

        protected List<AbstractPageRequest> Requests { get; set; }

        protected AbstractRegExParameter CreateRegExpExtractorToGetRedirectParameters(string bodyAsString)
        {
            // {"gxCommands":[{"redirect":{"url":"historiaclinicaprincipalv2?INS,21,61,21","forceDisableFrm":1}}]}
            // "historiaclinicaprincipalv2\?([^"]+)"
            var json = System.Web.HttpUtility.UrlDecode(bodyAsString);

            // when the response isn't a JSON -> return null
            JObject jObject;
            try
            {
                jObject = JObject.Parse(json);
            }
            catch (Exception)
            {
                return null;
            }

            var tmp1 = (JObject)jObject["gxCommands"].First;
            var tmp2 = tmp1["redirect"];

            // when the redirect isn't to an URL -> return null
            // e.g. "{\"gxCommands\":[{\"redirect\":\"subastasactivas\"}]}"
            if (tmp2 is JValue)
            {
                return null;
            }

            if (tmp2 is JObject)
            {
                var url = tmp2["url"].ToString();
                var index = url.IndexOf('?');

                // when the redirect is to an URL without parameters -> return null
                // e.g. "{\"gxCommands\":[{\"redirect\":{\"url\":\"wwclient.aspx\",\"forceDisableFrm\":1}}]}"
                if (index < 0)
                {
                    return null;
                }

                var numberOfChars = url.Length - index;
                var valueToReplace = url.Substring(index, numberOfChars);

                var desc = "Used in: Original value: " + valueToReplace;

                // e.g. "{"gxCommands":[{"redirect":{"url":"historiaclinicaprincipalv2?INS,0,1,3","forceDisableFrm":1}}]}"
                var expression = url.Substring(0, index) + "(\\?[^\"]*)\"";
                return CreateRegExpExtractorToGetRedirectParameters(NameGenerator.GetInstance().GetNewName(), expression, valueToReplace, desc);
            }

            return null;
        }

        protected abstract AbstractRegExParameter CreateRegExpExtractorToGetRedirectParameters(string bodyAsString, string expression, string valueToReplace, string description);

        protected abstract AbstractPageRequest CreatePageRequest(Session primaryRequest, AbstractStep abstractStep, Page page);

        protected AbstractPageRequest GetRequestByUrl(string url)
        {
            if (StringUtils.IsNullOrWhiteSpace(url) || Requests.Count == 0)
            {
                return null;
            }

            // Use RefererURL (full URL without parameters) to compare against referer
            return Requests.FirstOrDefault(pageRequest => pageRequest.IsUrlInFollowRedirectChain(url));
        }

        protected static bool IsInBlackList(Session request)
        {
            // TODO implement IsInBlackList(request) method
            var method = request.oRequest.headers.HTTPMethod;

            if (method != "GET" && method != "POST")
            {
                return true;
            }

            return false;
        }

        private static bool IsHTMLResponse(string html)
        {
            return html.Trim().StartsWith("<!DOCTYPE html>");
        }

        private static bool IsJSONResponse(string html)
        {
            return html.Trim().StartsWith("{");
        }
    }
}