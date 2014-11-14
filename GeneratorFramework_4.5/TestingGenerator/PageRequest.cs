using System.Globalization;
using System.Linq;
using Abstracta.FiddlerSessionComparer;
using Abstracta.Generators.Framework.AbstractGenerator;
using Fiddler;

namespace Abstracta.Generators.Framework.TestingGenerator
{
    internal class PageRequest : AbstractPageRequest
    {
        internal PageRequest(Session request, AbstractStep myStep, Page page)
            : base(request, myStep, page)
        {
        }

        public override string ToString()
        {
            var result = "\tPrimReq: (" + (InfoPage == null? "?" : InfoPage.Id.ToString(CultureInfo.CurrentCulture)) + ") " + GetRequestString(FiddlerSession) + "\n";

            // ---------------------------------

            result = ParametersToExtract.Aggregate(result,
                                                 (current, paramExtractor) =>
                                                 current + ("\t\tGetParameter: " + paramExtractor + "\n"));

            if (ParametersToExtract.Count > 0)
            {
                result += "\n";
            }

            // ---------------------------------

            if (InfoPage != null)
            {
                var tmp = InfoPage.GetParametersToExtract();
                if (tmp.Count > 0)
                {
                    result += string.Join("\n", tmp.Select(p => "\t\tGetParameter: " + p.ToString()).ToArray()) + "\n";
                }

                // ---------------------------------

                tmp = InfoPage.GetParametersToUse();
                if (tmp.Count > 0)
                {
                    result += string.Join("\n", tmp.Select(p => "\t\tUseParameter: " + p.ToString()).ToArray()) + "\n";
                }
            }
            
            // ---------------------------------

            result = FollowRedirects.Aggregate(result,
                                               (current, request) =>
                                               current +
                                               ("\t\tRedir: " + request.RedirectType + ": " +
                                               "(" + request.InfoPage.Id + ") " +
                                                GetRequestString(request.FiddlerSession) + 
                                                "\n\t\t\t" +
                                                string.Join("\n\t\t\t", request.InfoPage.GetParametersToExtract().Select(p => "GetParameter: " + p.ToString()).ToArray()) +
                                                "\n\t\t\t" +
                                                string.Join("\n\t\t\t", request.InfoPage.GetParametersToUse().Select(p => "UseParameter: " + p.ToString()).ToArray()) + 
                                                "\n"));

            if (FollowRedirects.Count > 0)
            {
                result += "\n";
            }

            // ---------------------------------

            result = SecondaryRequests.Aggregate(result,
                                                 (current, request) =>
                                                 current + ("\t\tSecRec: " + GetRequestString(request) + "\n"));

            if (SecondaryRequests.Count > 0)
            {
                result += "\n";
            }

            // ---------------------------------

            result = Validations.Aggregate(result,
                                                 (current, validation) =>
                                                 current + ("\t\tValidation: " + validation + "\n"));

            if (Validations.Count > 0)
            {
                result += "\n";
            }

            // ---------------------------------

            result += "Think time: " + ThinkTime + " millisecs.\n";

            return result;
        }

        private static string GetRequestString(Session req)
        {
            return "\"" + req.oRequest.headers.HTTPMethod + " " + req.fullUrl + " " + req.oRequest.headers.HTTPVersion + "\"";
        }
    }
}
