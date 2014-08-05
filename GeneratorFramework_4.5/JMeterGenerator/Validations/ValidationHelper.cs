using System.Globalization;
using System.IO;
using System.Xml;
using Abstracta.Generators.Framework.JMeterGenerator.AuxiliarClasses;

namespace Abstracta.Generators.Framework.JMeterGenerator.Validations
{
    internal class ValidationHelper
    {
        internal static string CreateValidation(string title, string assertText, string assertType)
        {
            return CreateValidation(title, assertText, assertType, string.Empty);
        }

        internal static string CreateValidation(string title, string assertText, string assertType, string errDesc)
        {
            string result;

            using (var stream = new MemoryStream())
            {
                // Create an XMLWriter object
                var xmlWriter = new XmlTextWriter(stream, System.Text.Encoding.UTF8)
                {
                    Formatting = Formatting.Indented
                };

                // <ResponseAssertion guiclass="AssertionGui" testclass="ResponseAssertion" testname="Response Assert - " enabled="true">
                JMeterWrapper.WriteStartElement(xmlWriter, "ResponseAssertion", "AssertionGui", "ResponseAssertion", title, "true");

                // <collectionProp name="Asserion.test_strings">
                JMeterWrapper.WriteStartElement(xmlWriter, "collectionProp", "Asserion.test_strings");

                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "", assertText);

                // </collectionProp>
                xmlWriter.WriteEndElement();

                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Assertion.test_field", "Assertion.response_data");
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "Assertion.assume_success", "false");
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "intProp", "Assertion.test_type", assertType);
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Assertion.comments", " Replace the validation text ");
                if (errDesc != string.Empty)
                {
                    JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "TestPlan.comments", errDesc);
                }

                // </ResponseAssertion>
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("hashTree");
                xmlWriter.WriteEndElement();

                xmlWriter.Flush();

                stream.Position = 0;
                var streamReader = new StreamReader(stream);
                result = streamReader.ReadToEnd();
            }

            return result;
        }

        public static string CreateResponseCodeValidation(string title, int responseCodeValidate, string assertionsTestType, string errorDescription)
        {
            string result;

            using (var stream = new MemoryStream())
            {
                // Create an XMLWriter object
                var xmlWriter = new XmlTextWriter(stream, System.Text.Encoding.UTF8)
                {
                    Formatting = Formatting.Indented
                };

                // <ResponseAssertion guiclass="AssertionGui" testclass="ResponseAssertion" testname="Response Assert - " enabled="true">
                JMeterWrapper.WriteStartElement(xmlWriter, "ResponseAssertion", "AssertionGui", "ResponseAssertion", title, "true");

                // <collectionProp name="Asserion.test_strings">
                JMeterWrapper.WriteStartElement(xmlWriter, "collectionProp", "Asserion.test_strings");

                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "49586", responseCodeValidate.ToString(CultureInfo.InvariantCulture));

                // </collectionProp>
                xmlWriter.WriteEndElement();

                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Assertion.test_field", "Assertion.response_code");
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "Assertion.assume_success", "false");
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "intProp", "Assertion.test_type", assertionsTestType);
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "Assertion.comments", "");
                if (errorDescription != string.Empty)
                {
                    JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "TestPlan.comments", errorDescription);
                }

                // </ResponseAssertion>
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("hashTree");
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