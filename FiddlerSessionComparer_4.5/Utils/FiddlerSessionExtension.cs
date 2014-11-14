using System;
using System.Collections.Generic;
using System.Linq;
using Fiddler;

namespace Abstracta.FiddlerSessionComparer.Utils
{
    public static class FiddlerSessionExtension
    {
        public static bool IsPrimaryRequest(this Session session)
        {
            return SessionUtils.IsPrimaryReq(session);
        }

        public static bool IsSecondaryRequest(this Session session)
        {
            return !session.IsPrimaryRequest();
        }

        public static bool IsRedirectByResponseCode(this Session request)
        {
            return 400 > request.responseCode && request.responseCode >= 300;
        }

        public static bool IsRedirectByJavaScript(this Session request)
        {
            return IsGenexusRedirect(request) /* || IsOtherKindOfRedirect() */;
        }

        public static bool IsGenexusRedirect(this Session request)
        {
            var gxCommands = GetGxCommandsFromBody(request);

            return gxCommands.Any(command => command == "redirect");
        }

        public static List<string> GetGxCommandsFromBody(this Session request)
        {
            var body = request.GetResponseBodyAsString();
            var index = body.IndexOf("\"gxCommands\"", StringComparison.Ordinal);

            if (index < 0)
            {
                return new List<string>();
            }

            var reading = false;
            var commands = new System.Text.StringBuilder();
            var buffer = new char[100];
            var bufferIndex = 0;
            while (body[index] != ']')
            {
                if (reading)
                {
                    buffer[bufferIndex] = body[index];
                    bufferIndex++;

                    if (bufferIndex == 100)
                    {
                        commands.Append(buffer);
                        buffer = new char[100];
                        bufferIndex = 0;
                    }
                }

                if (body[index] == '[')
                {
                    reading = true;
                }

                index++;
            }

            /*
            "gxCommands":
            [
            {"popup":["wc_confirmaoferta?bIA6MwXk3GlbytteOKWIoNQ7n67/HxEAc8/wrIi0OYTLHeDUO6BhVtSuZuVjtP6T",1,0,0,0,0,0,[],[]]},
            {"refresh":"POST"},
            {"redirect":"subastasactivas"},
            ]
             */

            var commandsStr = commands.Append(buffer, 0, bufferIndex).ToString();
            var commandsArray = commandsStr.Split('{');

            return (from commandStr in commandsArray
                    where !StringUtils.IsNullOrWhiteSpace(commandStr)
                    select commandStr.Split(':')[0].Split('"')[1]).ToList();
        }
    }
}
