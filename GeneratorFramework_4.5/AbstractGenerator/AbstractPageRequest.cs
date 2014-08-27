using System.Collections.Generic;
using System.Linq;
using Abstracta.FiddlerSessionComparer;
using Abstracta.Generators.Framework.AbstractGenerator.Validations;
using Fiddler;

namespace Abstracta.Generators.Framework.AbstractGenerator
{
    internal enum RedirectType
    {
        ByResponseCode,
        ByJavaScript,
    }

    internal class AbstractPageRequest : HTTPRequest
    {
        protected AbstractStep MyStep { get; private set; }

        internal List<AbstractFollowRedirect> FollowRedirects { get; private set; }

        internal List<Session> SecondaryRequests { get; set; }

        internal long ThinkTime { get; set; }

        internal AbstractPageRequest(Session request, AbstractStep myStep, Page page)
            : base(request, page)
        {
            Validations.Add(myStep.CreateDefaultValidationFromRequest(request));
            FollowRedirects = new List<AbstractFollowRedirect>();
            SecondaryRequests = new List<Session>();

            MyStep = myStep;
        }

        /// <summary>
        /// Creates a variable of type AbstractFollowRedirect, add in the FollowRedirect list and return de variable.
        /// </summary>
        /// <param name="request">Session of request</param>
        /// <param name="rType">Redirect Type</param>
        /// <param name="page">Page of request</param>
        /// <returns>Returns the variable created</returns>
        internal AbstractFollowRedirect AddFollowRedirect(Session request, RedirectType rType, Page page)
        {
            var redirect = new AbstractFollowRedirect(request, rType, page);
            FollowRedirects.Add(redirect);

            return redirect;
        }

        /// <summary>
        /// Adds the session received as parameter in SecondaryRequest list.
        /// </summary>
        /// <param name="httpReq">Session to add</param>
        internal void AddSecondaryRequest(Session httpReq)
        {
            SecondaryRequests.Add(httpReq);
        }

        /// <summary>
        /// Returns the last Primary request of the FollowRedirects list
        /// </summary>
        /// <returns>Last session of the list</returns>
        internal Session GetLastPrimaryRequest()
        {
            return FollowRedirects == null || FollowRedirects.Count == 0
                       ? FiddlerSession
                       : FollowRedirects[FollowRedirects.Count - 1].FiddlerSession;
        }

        /// <summary>
        /// Returns if a request is a follow redirect.
        /// </summary>
        /// <param name="url">URL of request</param>
        /// <returns>Returns true if the request belongs to FollowRedirects list or false otherwise</returns>
        public bool IsUrlInFollowRedirectChain(string url)
        {
            return url == RefererURL || FollowRedirects.Any(followRedirect => followRedirect.FiddlerSession.fullUrl == url);
        }

        /// <summary>
        /// Adds the validation received as parameter in the validations list.
        /// </summary>
        /// <param name="val">Validation to add</param>
        public void AddValidation(AbstractValidation val)
        {
            if (Validations.Count == 1 && Validations[0] is DefaultValidation)
            {
                Validations.RemoveAt(0);
            }

            Validations.Add(val);
        }
    }
}