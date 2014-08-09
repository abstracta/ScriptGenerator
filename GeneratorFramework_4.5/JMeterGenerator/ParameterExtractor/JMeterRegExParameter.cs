using System.IO;
using System.Xml;
using Abstracta.Generators.Framework.JMeterGenerator.AuxiliarClasses;

namespace Abstracta.Generators.Framework.JMeterGenerator.ParameterExtractor
{
    internal class JMeterRegExParameter : AbstractGenerator.ParameterExtractor.AbstractRegExParameter
    {
        internal JMeterRegExParameter(string varibleName, string regularExpression, string group, string valueToReplace, string description)
            : base(varibleName, regularExpression, group, valueToReplace, description)
        {
        }

        public override string ToString()
        {
            /*
            <RegexExtractor guiclass="RegexExtractorGui" testclass="RegexExtractor" testname="RegExp Extractor - URL_Params1" enabled="true">
              <stringProp name="TestPlan.comments">commmmmmmmeent</stringProp>
              <stringProp name="RegexExtractor.useHeaders">false</stringProp>
              <stringProp name="RegexExtractor.refname">URL_Params1</stringProp>
              <stringProp name="RegexExtractor.regex">&quot;historiaclinicaprincipalv2\?([^&quot;]+)&quot;</stringProp>
              <stringProp name="RegexExtractor.template">$1$</stringProp>
              <stringProp name="RegexExtractor.default">NOT FOUND</stringProp>
              <stringProp name="RegexExtractor.match_number">1</stringProp>
            </RegexExtractor>
             * */

            string result;

            using (var stream = new MemoryStream())
            {
                // Create an XMLWriter object
                var xmlWriter = new XmlTextWriter(stream, System.Text.Encoding.UTF8)
                {
                    Formatting = Formatting.Indented
                };

                JMeterWrapper.WriteStartElement(xmlWriter, "RegexExtractor", "RegexExtractorGui", "RegexExtractor", "RegExp Extractor - " + VariableName, "true");
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "TestPlan.comments", Description);
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.useHeaders", "false");
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.refname", VariableName);
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.regex", RegularExpression);
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.template", Group);
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.default", "NOT FOUND");
                JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.match_number", "1");

                // </RegexExtractor>
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
