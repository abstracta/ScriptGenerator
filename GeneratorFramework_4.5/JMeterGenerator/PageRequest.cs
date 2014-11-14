using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Abstracta.FiddlerSessionComparer;
using Abstracta.Generators.Framework.AbstractGenerator;
using Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor;
using Abstracta.Generators.Framework.AbstractGenerator.Validations;
using Abstracta.Generators.Framework.Constants;
using Abstracta.Generators.Framework.JMeterGenerator.AuxiliarClasses;
using Abstracta.Generators.Framework.JMeterGenerator.ParameterExtractor;
using Fiddler;
using ExtractFrom = Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor.ExtractFrom;

namespace Abstracta.Generators.Framework.JMeterGenerator
{
    internal class PageRequest : AbstractPageRequest
    {
        private static readonly string[] ExcludedHttpHeaders = { "Cookie" };

        internal PageRequest(Session request, AbstractStep myStep, Page page)
            : base(request, myStep, page)
        {
        }

        public override string ToString()
        {
            string result;

            using (var stream = new MemoryStream())
            {
                // Create an XMLWriter object
                var xmlWriter = new XmlTextWriter(stream, System.Text.Encoding.UTF8)
                {
                    Formatting = Formatting.Indented
                };

                WriteHTTPSample(xmlWriter, MyStep, FiddlerSession, InfoPage, GetFullURL(InfoPage), Validations, ParametersToExtract);

                // if there aren't follow redirects, then add validations to the primary request
                if (FollowRedirects.Count > 0)
                {
                    // Adding follow Redirects
                    // the last request must have the validations
                    var lastRequestForValidation = GetLastRequestForValidation(FollowRedirects);

                    // Add JMeter GenericController to group the requests
                    JMeterWrapper.WriteStartElement(xmlWriter, "GenericController", "LogicControllerGui", "GenericController", "Follow Redirects", "true");
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("hashTree");

                    foreach (var followRedirect in FollowRedirects)
                    {
                        if (followRedirect.FiddlerSession == lastRequestForValidation)
                        {
                            WriteHTTPSample(xmlWriter,
                                            MyStep,
                                            followRedirect.FiddlerSession,
                                            followRedirect.InfoPage,
                                            followRedirect.GetFullURL(followRedirect.InfoPage),
                                            followRedirect.Validations,
                                            followRedirect.ParametersToExtract);
                        }
                        // Disable the follow redirects by response code
                        // Change :: JMeter encodes the parameters of the redirect URL, producing bad bahaviour
                        else
                        {
                            WriteHTTPSample(xmlWriter,
                                            MyStep,
                                            followRedirect.FiddlerSession,
                                            followRedirect.InfoPage,
                                            followRedirect.GetFullURL(followRedirect.InfoPage),
                                            followRedirect.Validations,
                                            followRedirect.ParametersToExtract);
                        }
                    }

                    // </hashTree>
                    xmlWriter.WriteEndElement();
                }

                // Adding secondary Requests
                if (SecondaryRequests.Count > 0)
                {
                    JMeterWrapper.WriteStartElement(xmlWriter, "GenericController", "LogicControllerGui", "GenericController", "Secondary Requests", "true");
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("hashTree");

                    // if controller
                    JMeterWrapper.WriteStartElement(xmlWriter, "IfController", "IfControllerPanel", "IfController", "If Controller", "true");
                    JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "IfController.condition", "${" + HTTPConstants.VariableNameDebug + "} == 0");
                    JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "IfController.evaluateAll", "false");
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("hashTree");

                    // write response assertion to skip HTTP response code validation
                    JMeterWrapper.WriteResponseAssertionSkipHTTPResponse(xmlWriter);

                    foreach (var secondaryRequest in SecondaryRequests)
                    {
                        WriteHTTPSample(xmlWriter, MyStep, secondaryRequest, null, secondaryRequest.fullUrl);
                    }

                    // </hashTree> 'if' controller
                    xmlWriter.WriteEndElement();

                    // </hashTree> 'generic' controller
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.Flush();

                stream.Position = 0;
                var streamReader = new StreamReader(stream);
                result = streamReader.ReadToEnd();
            }

            return result;
        }

        private static void WriteHTTPSample(XmlWriter xmlWriter, AbstractStep myStep, Session request, Page page, string fullUrl, bool enable = true)
        {
            WriteHTTPSample(xmlWriter, myStep, request, page, fullUrl, new List<AbstractValidation>(), new List<AbstractParameterExtractor>(), false, enable);
        }

        private static void WriteHTTPSample(XmlWriter xmlWriter, AbstractStep myStep, Session request, Page page, string fullURL, IEnumerable<AbstractValidation> validations, IEnumerable<AbstractParameterExtractor> parametersToExtract, bool followRedirects = false, bool enable = true, bool isPrimary = true)
        {
            var httpMethod = request.oRequest.headers.HTTPMethod;

            string urlRequest;
            var serverName = "${" + HTTPConstants.VariableNameServer + "}";
            var serverPortName = "${" + HTTPConstants.VariableNamePort + "}";
            const string webAppName = "${" + HTTPConstants.VariableNameWebApp + "}";

            if (request.host == myStep.ServerNameAndPort)
            {
                urlRequest = httpMethod + " " + fullURL.Replace(myStep.ServerName, serverName);
                
                // when URL shows the port
                if (!myStep.IsDefaultPort())
                {
                    // if the port is the same as ServerName, parametrize it
                    if (request.port.ToString(CultureInfo.InvariantCulture) == myStep.ServerPort)
                    {
                        urlRequest = urlRequest.Replace(":" + myStep.ServerPort, ":" + serverPortName);
                    }
                    else
                    {
                        // don't parametrize the port
                        serverPortName = request.port.ToString(CultureInfo.InvariantCulture);    
                    }
                }

                if (myStep.WebApp.Length > 0)
                {
                    urlRequest = urlRequest.Replace(myStep.WebApp, webAppName);
                }
            }
            else
            {
                urlRequest = httpMethod + " " + fullURL;
                serverName = request.host;
                serverPortName = request.port.ToString(CultureInfo.InvariantCulture);    
            }

            if (urlRequest == string.Empty) return;

            // "GET http://${server}/${webApp}/home.aspx?${param1},etc"
            JMeterWrapper.WriteStartElement(xmlWriter, "HTTPSamplerProxy", "HttpTestSampleGui", "HTTPSamplerProxy", myStep.Index + " " +
                                          RemoveParameters(urlRequest), (enable ? "true" : "false"));

            switch (httpMethod.ToLower())
            {
                case "post":
                    var body = page == null ? request.GetRequestBodyAsString().Replace("'", "&apos;") : page.Body;
                    if (page != null && !FiddlerSessionComparer.FiddlerSessionComparer.ReplaceInBodies)
                    {
                        Logger.GetInstance().Log("Body is not parametrized. Differences detected in page: " + request.id);
                        foreach (var parameter in page.GetParametersToUse())
                        {
                            Logger.GetInstance().Log(parameter);    
                        }
                    }

                    // <boolProp name="HTTPSampler.postBodyRaw">true</boolProp>
                    JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "HTTPSampler.postBodyRaw", "true");

                    JMeterWrapper.WriteStartElement(xmlWriter, "elementProp", "HTTPsampler.Arguments", "Arguments");
                    JMeterWrapper.WriteStartElement(xmlWriter, "collectionProp", "Arguments.arguments");
                    JMeterWrapper.WriteStartElement(xmlWriter, "elementProp", string.Empty, "HTTPArgument");

                    JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "HTTPArgument.always_encode", "false");
                    JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.value", body);
                    JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.metadata", "=");

                    // elementProp
                    xmlWriter.WriteEndElement();

                    // collectionProp
                    xmlWriter.WriteEndElement();

                    // elementProp
                    xmlWriter.WriteEndElement();
                    break;

                case "get":
                    JMeterWrapper.WriteStartElement(xmlWriter, "elementProp", "HTTPsampler.Arguments", "Arguments", "HTTPArgumentsPanel", "Arguments", "User Defined Variables", "true");

                    JMeterWrapper.WriteStartElement(xmlWriter, "collectionProp", "Arguments.arguments");

                    // collectionProp
                    xmlWriter.WriteEndElement();

                    // elementProp
                    xmlWriter.WriteEndElement();
                    break;
            }

            //var index = elementName.IndexOf("/${webApp}", StringComparison.Ordinal);
            //var path = elementName.Substring(index);

            var protocol = request.isHTTPS ? "HTTPS" : "HTTP";

            string path;
            int index;

            if (request.host == myStep.ServerName)
            {
                index = urlRequest.IndexOf("/" + serverName, StringComparison.Ordinal);
                index += ("/" + serverName).Length;
                
                path = urlRequest.Substring(index);
            }
            else
            {
                index = (request.isHTTPS)
                            ? urlRequest.IndexOf("https://", StringComparison.Ordinal)
                            : urlRequest.IndexOf("http://", StringComparison.Ordinal);

                if (index == -1)
                {
                    index = urlRequest.IndexOf("/", StringComparison.Ordinal);
                    path = urlRequest.Substring(index);
                }
                else
                {
                    index = urlRequest.IndexOf("//", StringComparison.Ordinal);
                    path = urlRequest.Substring(index + 2);
                    
                    index = path.IndexOf("/", StringComparison.Ordinal);
                    if (index > 0)
                    {
                        path = path.Substring(index);
                    }
                }
            }

            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.domain", serverName);
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.port", "" + serverPortName);
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.connect_timeout", "");
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.response_timeout", "");
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.protocol", protocol);

            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.contentEncoding", "");
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.path", path);
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.method", httpMethod);
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "HTTPSampler.follow_redirects", (followRedirects ? "true" : "false"));
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "HTTPSampler.auto_redirects", "false");

            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "HTTPSampler.use_keepalive", "true");
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "HTTPSampler.DO_MULTIPART_POST", "false");
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.implementation", "HttpClient4");
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "HTTPSampler.monitor", "false");
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "HTTPSampler.embedded_url_re", "");
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "TestPlan.comments", "FiddlerID: " + request.id);

            // </HTTPSamplerProxy>
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("hashTree");

            WriteHeaderManager(xmlWriter, request);

            // adding validations
            foreach (var validation in validations)
            {
                WriteElement(xmlWriter, validation);
            }

            // add "Logging HTML response in jmeter.log when test fail"
            if (isPrimary) 
            {
                JMeterWrapper.WriteBeanShellAssertionModule(xmlWriter);
            }

            // adding parameters extractor
            foreach (var extractor in parametersToExtract)
            {
                WriteElement(xmlWriter, extractor);
            }

            // adding the parameter extractors that were detected by comparing fiddler sessions
            if (page != null)
            {
                var parms = new List<JMeterRegExParameter>();
                var constants = new List<JMeterConstant>();

                foreach (var parameter in page.GetParametersToExtract())
                {
                    var description = "Used in pages: { "
                                          + string.Join(",", parameter.UsedInPages.Select(p => p.Id + "").ToArray())
                                          + " } Original value: " + parameter.Values[0];

                    if (parameter.ParamExtractor is RegExpExtractor)
                    {
                        var valueSource = parameter.ParamExtractor as RegExpExtractor;
                        var regExp = valueSource.RegExp;

                        var extractFrom = GetExtractFrom(parameter.ExtractFromSection);
                        var usedIn = GetUsedIn(parameter.TargetsOfPage(page));
                        var newValue = new JMeterRegExParameter(
                            extractFrom,
                            usedIn,
                            parameter.VariableName,
                            regExp,
                            "$" + valueSource.GroupNumber + "$",
                            parameter.Values[0],
                            description
                            );

                        parms.Add(newValue);
                    }
                    else
                    {
                        var newConstant = new JMeterConstant(parameter.VariableName, parameter.Values[0], description);

                        constants.Add(newConstant);
                    }
                }

                foreach (var paramExtractor in parms)
                {
                    WriteElement(xmlWriter, paramExtractor);
                }

                if (constants.Count > 0)
                {
                    // Write collection of constants
                    /*
                    <Arguments guiclass="ArgumentsPanel" testclass="Arguments" testname="Constants that couldn&apos;t be extracted from HTML" enabled="true">
                      <collectionProp name="Arguments.arguments">
                        <elementProp name="vUSUARIOMENUCODIGO" elementType="Argument">
                          <stringProp name="Argument.name">vUSUARIOMENUCODIGO</stringProp>
                          <stringProp name="Argument.value">A7</stringProp>
                          <stringProp name="Argument.metadata">=</stringProp>
                          <stringProp name="Argument.desc">Used in pages: { 14,17 } Original value: A7</stringProp>
                        </elementProp>
                      </collectionProp>
                    </Arguments>
                     * */

                    JMeterWrapper.WriteStartElement(xmlWriter, "Arguments", "ArgumentsPanel", "Arguments", "Constants that couldn't be extracted from HTML", "true");
                    JMeterWrapper.WriteStartElement(xmlWriter, "collectionProp", "Arguments.arguments");

                    foreach (var jMeterConstant in constants)
                    {
                        JMeterWrapper.WriteStartElement(xmlWriter, "elementProp", jMeterConstant.Name, "Argument");

                        JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.name", jMeterConstant.Name);
                        JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.value", jMeterConstant.Value);
                        JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.metadata", "=");
                        JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.desc", jMeterConstant.Description);

                        // </elementProp>
                        xmlWriter.WriteEndElement();
                    }

                    // </collectionProp>
                    xmlWriter.WriteEndElement();

                    // </Arguments>
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("hashTree");
                    xmlWriter.WriteEndElement();
                }
            }

            // </hashTree>
            xmlWriter.WriteEndElement();
        }

        private static List<UseIn> GetUsedIn(IEnumerable<UseToReplaceIn> parametersTarget)
        {
            var res = new List<UseIn>();

            foreach (var parameterTarget in parametersTarget)
            {
                switch (parameterTarget)
                {
                    case UseToReplaceIn.Body:
                        res.Add(UseIn.Body);
                        break;

                    case UseToReplaceIn.Url:
                        res.Add(UseIn.Url);
                        break;

                    default:
                        res.Add(UseIn.Body);
                        break;
                }
            }

            return res;
        }

        private static ExtractFrom GetExtractFrom(FiddlerSessionComparer.ExtractFrom extractParameterFrom)
        {
            switch (extractParameterFrom)
            {
                case FiddlerSessionComparer.ExtractFrom.Body:
                    return ExtractFrom.Body;
                case FiddlerSessionComparer.ExtractFrom.Headers:
                    return ExtractFrom.Headers;
                case FiddlerSessionComparer.ExtractFrom.Url:
                    return ExtractFrom.Url;

                default:
                    return ExtractFrom.Body;
            }
        }

        private static void WriteHeaderManager(XmlWriter xmlWriter, Session request)
        {
            JMeterWrapper.WriteStartElement(xmlWriter, "HeaderManager", "HeaderPanel", "HeaderManager", "HTTP Header Manager", "true");

            JMeterWrapper.WriteStartElement(xmlWriter, "collectionProp", "HeaderManager.headers");

            for (var i = 0; i < request.oRequest.headers.Count(); i++)
            {
                var header = request.oRequest.headers[i];

                if (!IsHeaderExcluded(header))
                {
                    AddHeaderToCollectionProp(xmlWriter, header.Name, header.Value);
                }
            }

            // </collectinProp>
            xmlWriter.WriteEndElement();

            // </HeaderManager>
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("hashTree");
            // </hashTree>
            xmlWriter.WriteEndElement();
        }

        private static void WriteElement(XmlWriter xmlWriter, object element)
        {
            var elementString = element.ToString();
            xmlWriter.WriteRaw(elementString);
        }

        private static bool IsHeaderExcluded(HTTPHeaderItem header)
        {
            return ExcludedHttpHeaders.Any(excludedHttpHeader => header.Name == excludedHttpHeader);
        }

        private static void AddHeaderToCollectionProp(XmlWriter xmlWriter, string name, string value, string desc = null)
        {
            xmlWriter.WriteStartElement("elementProp");
            xmlWriter.WriteAttributeString("name", name);
            xmlWriter.WriteAttributeString("elementType", "Header");

            // <stringProp name="Header.name">NAME</stringProp>
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Header.name", name);

            // <stringProp name="Header.value">VALUE</stringProp>
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Header.value", value);

            if (desc != null)
            {
                // <stringProp name="Header.desc">blah blah blah</stringProp>
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Header.desc", desc);
            }

            // elementProp
            xmlWriter.WriteEndElement();
        }

        private static Session GetLastRequestForValidation(IEnumerable<AbstractFollowRedirect> followRedirects)
        {
            return followRedirects.Last().FiddlerSession;
        }

        private static string RemoveParameters(string url)
        {
            var tmp = url.IndexOf('?');
            return tmp > 0 ? url.Substring(0, tmp) : url;
        }
    }
}
