using System.Collections.Generic;
using System.IO;
using System.Xml;
using Abstracta.FiddlerSessionComparer;
using Abstracta.Generators.Framework.AbstractGenerator;
using Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor;
using Abstracta.Generators.Framework.AbstractGenerator.Validations;
using Abstracta.Generators.Framework.JMeterGenerator.AuxiliarClasses;
using Abstracta.Generators.Framework.JMeterGenerator.ParameterExtractor;
using Fiddler;
using ExtractFrom = Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor.ExtractFrom;
using Formatting = System.Xml.Formatting;

namespace Abstracta.Generators.Framework.JMeterGenerator
{
    internal class Step : AbstractStep
    {
        protected override AbstractRegExParameter CreateRegExpExtractorToGetRedirectParameters(ExtractFrom extractParameterFrom, List<UseIn> useParameterIn, string varName, string expression, string group, string valueToReplace, string description)
        {
            return new JMeterRegExParameter(extractParameterFrom, useParameterIn, varName, expression, group, valueToReplace, description);
        }

        protected override AbstractPageRequest CreatePageRequest(Session primaryRequest, AbstractStep abstractStep, Page page)
        {
            return new PageRequest(primaryRequest, abstractStep, page);
        }

        internal override DefaultValidation CreateDefaultValidation()
        {
            return new Validations.DefaultValidation();
        }

        internal override CheckMainObjectValidation CreateCheckMainObjectValidation(string objectName)
        {
            return new Validations.CheckMainObjectValidation(objectName);
        }

        internal override AppearTextValidation CreateAppearTextValidation(string text, string desc, bool neg, bool stop)
        {
            return new Validations.AppearTextValidation(text, desc, neg, stop);
        }

        internal override ResponseCodeValidation CreateResponseCodeValidation(int responseCode, string desc = "", bool neg = false, bool stop = true)
        {
            return new Validations.ResponseCodeValidation(responseCode, desc, neg, stop);
        }

        public override string ToString()
        {
            //<TransactionController guiclass="TransactionControllerGui" testclass="TransactionController" testname="Step 00 - Event Go" enabled="true">
            //  <boolProp name="TransactionController.parent">false</boolProp>
            //  <stringProp name="TestPlan.comments">Go( "http://localhost/CursoGXtest.NetEnvironment/home.aspx" )</stringProp>
            //</TransactionController>

            string result;

            using (var stream = new MemoryStream())
            {
                // Create an XMLWriter object
                var xmlWriter = new XmlTextWriter(stream, System.Text.Encoding.UTF8)
                    {
                        Formatting = Formatting.Indented
                    };

                JMeterWrapper.WriteStartElement(xmlWriter, "TransactionController", "TransactionControllerGui",
                                              "TransactionController", Name, "true");
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "TransactionController.parent", "false");
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "TestPlan.comments", Desc);
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "TransactionController.includeTimers", "false");
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("hashTree");

                foreach (var pageRequest in Requests)
                {
                    xmlWriter.WriteRaw(pageRequest.ToString());
                }

                // </hashTree>
                xmlWriter.WriteEndElement();
                xmlWriter.Flush();

                stream.Position = 0;
                var streamReader = new StreamReader(stream);
                result = streamReader.ReadToEnd();
            }

            return result;
        }
    }
}