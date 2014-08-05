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

        internal AbstractFollowRedirect AddFollowRedirect(Session request, RedirectType rType, Page page)
        {
            var redirect = new AbstractFollowRedirect(request, rType, page);
            FollowRedirects.Add(redirect);

            return redirect;
        }

        internal void AddSecondaryRequest(Session httpReq)
        {
            SecondaryRequests.Add(httpReq);
        }

        internal Session GetLastPrimaryRequest()
        {
            return FollowRedirects == null || FollowRedirects.Count == 0
                       ? FiddlerSession
                       : FollowRedirects[FollowRedirects.Count - 1].FiddlerSession;
        }

        public bool IsUrlInFollowRedirectChain(string url)
        {
            return url == RefererURL || FollowRedirects.Any(followRedirect => followRedirect.FiddlerSession.fullUrl == url);
        }

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