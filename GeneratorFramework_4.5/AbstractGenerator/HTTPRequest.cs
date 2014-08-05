using System.Collections.Generic;
using Abstracta.FiddlerSessionComparer;
using Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor;
using Abstracta.Generators.Framework.AbstractGenerator.Validations;
using Fiddler;

namespace Abstracta.Generators.Framework.AbstractGenerator
{
    internal class HTTPRequest
    {
        private Session _request;

        internal Page InfoPage;

        internal Session FiddlerSession
        {
            get { return _request; }
            set
            {
                _request = value;

                RefererURL = value.fullUrl;
                HTTPRCode = value.responseCode;
            }
        }

        internal int HTTPRCode { get; private set; }

        /// <summary>
        /// URL used to compare with the 'referer' HTTP header of other HTTP requests
        /// </summary>
        internal string RefererURL { get; private set; }

        internal List<AbstractValidation> Validations { get; private set; }

        internal List<AbstractParameterExtractor> ParametersToExtract { get; private set; }

        internal HTTPRequest()
        {
            Validations = new List<AbstractValidation>();
            ParametersToExtract = new List<AbstractParameterExtractor>();
        }

        internal HTTPRequest(Session request)
        {
            FiddlerSession = request;
            Validations = new List<AbstractValidation>();
            ParametersToExtract = new List<AbstractParameterExtractor>();
        }

        internal HTTPRequest(Session request, Page infoPage)
        {
            InfoPage = infoPage;
            FiddlerSession = request;
            Validations = new List<AbstractValidation>();
            ParametersToExtract = new List<AbstractParameterExtractor>();
        }

        internal HTTPRequest(List<AbstractValidation> validations)
        {
            Validations = validations;
            ParametersToExtract = new List<AbstractParameterExtractor>();
        }

        internal HTTPRequest(Session request, List<AbstractValidation> validations)
        {
            FiddlerSession = request;
            Validations = validations;
            ParametersToExtract = new List<AbstractParameterExtractor>();
        }

        internal static string ClearParametersFromURL(string url)
        {
            if (url == null)
            {
                return null;
            }

            var indexOf = url.IndexOf('?');
            return indexOf < 0 ? url : url.Remove(indexOf);
        }
    }
}
