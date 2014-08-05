using System;
using Fiddler;

namespace Abstracta.Generators.Framework.AbstractGenerator.Wrappers
{
    internal class FiddlerSessionsWrapper
    {
        private readonly Session[] _sessions;

        internal FiddlerSessionsWrapper(Session[] fiddlerSessions)
        {
            _sessions = fiddlerSessions;
        }

        internal Session GetRequest(int requestId)
        {
            if (requestId < 0)
            {
                throw new Exception("Out of index when accessing fiddler sessions: " + requestId);
            }

            if (requestId >= _sessions.Length)
            {
                throw new Exception("Out of index when accessing fiddler sessions: " + requestId);
            }

            return _sessions[requestId];
        }

        internal Session[] GetSessions()
        {
            var res = new Session[_sessions.Length];

            for (var i = 0; i < _sessions.Length; i++)
            {
                res[i] = _sessions[i];
            }

            return res;
        }
    }
}