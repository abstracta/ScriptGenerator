using System;
using System.IO;
using Abstracta.FiddlerSessionComparer;
using Abstracta.FiddlerSessionComparer.Utils;

namespace Abstracta.FiddlerSessionComparerCLI
{
    public class Program
    {
        /// <summary>
        /// This is an example of using the Fiddler session comparer
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            const string fiddlerSessionsFile1 = @"";
            const string fiddlerSessionsFile2 = @"";
            const string fiddlerSessionsFile3 = @"";

            const string pagesResultFile = @"pruebaAuto-Param.txt";
            const bool isGenexusApp = true;
            const bool replaceInBodies = true;

            var fiddlerComparer = new FiddlerSessionComparer.FiddlerSessionComparer(replaceInBodies, isGenexusApp);
            fiddlerComparer.Load(fiddlerSessionsFile1, fiddlerSessionsFile2, null);

            var result = fiddlerComparer.CompareFull();

            // save page structure result to file
            (new StreamWriter(pagesResultFile)).WriteLine(result.ToString("", false));
        }

        /// <summary>
        /// This creates a referers chain from the sessions list 
        /// </summary>
        /// <param name="sessionFileName"></param>
        public static void Test(string sessionFileName)
        {
            var sessions = SazFormat.GetSessionsFromFile(sessionFileName);
            if (sessions == null)
            {
                throw new Exception("Sessions == null");
            }

            var referersChain = new Page(null, "", "", "", "", "", -1);

            foreach (var session in sessions)
            {
                var i = 0;
                var headersArray = new string[session.oResponse.headers.Count()];
                foreach (var httpResponseHeader in session.oResponse.headers)
                {
                    headersArray[i] = httpResponseHeader.ToString();
                    i++;
                }

                var referer = session.oRequest.headers["Referer"];
                var id = session.id;
                var uri = session.fullUrl;
                var body = session.HTTPMethodIs("POST") ? session.GetRequestBodyAsString() : "";
                var htmlResponse = session.GetResponseBodyAsString();
                var httpmethod = session.oRequest.headers.HTTPMethod;
                var responseHeaders = string.Join("\n", headersArray);
                var responseCode = session.responseCode;

                var refererPage = referersChain.FindRefererPage(referer, id);

                if (refererPage == null)
                {
                    referersChain.Followers.Add(new Page(new Page(null, referer, "", "", "", "", -1), uri, body, htmlResponse, httpmethod, responseHeaders, responseCode));
                }
                else
                {
                    refererPage.Followers.Add(new Page(refererPage, uri, body, htmlResponse, httpmethod, responseHeaders, responseCode));
                }
            }

            using (var fw = new StreamWriter(sessionFileName + "out.txt"))
            {
                fw.Write(referersChain.ToString("", false));
            }
        }
    }
}
