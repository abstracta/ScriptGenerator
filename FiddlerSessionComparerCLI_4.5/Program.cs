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

            var fiddlerComparer = new FiddlerSessionComparer.FiddlerSessionComparer(true);
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

            var referersChain = new Page(null, "", "", "", "");

            foreach (var session in sessions)
            {
                var referer = session.oRequest.headers["Referer"];
                var id = session.id;
                var uri = session.fullUrl;
                var body = session.HTTPMethodIs("POST") ? session.GetRequestBodyAsString() : "";
                var htmlResponse = session.GetResponseBodyAsString();
                var httpmethod = session.oRequest.headers.HTTPMethod;

                var refererPage = referersChain.FindRefererPage(referer, id);

                if (refererPage == null)
                {
                    referersChain.Followers.Add(new Page(new Page(null, referer, "", "", ""), uri, body, htmlResponse, httpmethod));
                }
                else
                {
                    refererPage.Followers.Add(new Page(refererPage, uri, body, htmlResponse, httpmethod));
                }
            }

            using (var fw = new StreamWriter(sessionFileName + "out.txt"))
            {
                fw.Write(referersChain.ToString("", false));
            }
        }
    }
}
