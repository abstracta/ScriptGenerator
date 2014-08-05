using System;
using System.Xml;
using Abstracta.Generators.Framework.Constants;

namespace Abstracta.Generators.Framework.JMeterGenerator.AuxiliarClasses
{
    internal class JMeterWrapper
    {
        internal static void WriteThinkTime(XmlWriter xmlWriter, ThinktimeType thinktimeType)
        {
            //<IfController guiclass="IfControllerPanel" testclass="IfController" testname="If Controller" enabled="true">
            //  <stringProp name="IfController.condition">${debug} == 0</stringProp>
            //  <boolProp name="IfController.evaluateAll">false</boolProp>
            //</IfController>
            //<hashTree>
            //  <UniformRandomTimer guiclass="UniformRandomTimerGui" testclass="UniformRandomTimer" testname="Uniform Random Timer" enabled="true">
            //    <stringProp name="ConstantTimer.delay">${Medium}</stringProp>
            //    <stringProp name="RandomTimer.range">${Medium_Random}</stringProp>
            //  </UniformRandomTimer>
            //  <hashTree/>
            //  <TestAction guiclass="TestActionGui" testclass="TestAction" testname="Test Action - nothing" enabled="true">
            //    <intProp name="ActionProcessor.action">1</intProp>
            //    <intProp name="ActionProcessor.target">0</intProp>
            //    <stringProp name="ActionProcessor.duration">0</stringProp>
            //  </TestAction>
            //  <hashTree/>
            //</hashTree>

            // 'IfController'
            WriteStartElement(xmlWriter, "IfController", "IfControllerPanel", "IfController", "if (debug == 0) then Thinktime " + thinktimeType, "true");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "IfController.condition", "${debug} == 0");
            WriteElementWithTextChildren(xmlWriter, "boolProp", "IfController.evaluateAll", "false");

            // 'IfController'
            xmlWriter.WriteEndElement();

            // 'hashTree'
            xmlWriter.WriteStartElement("hashTree");

            WriteStartElement(xmlWriter, "UniformRandomTimer", "UniformRandomTimerGui", "UniformRandomTimer",
                            "Uniform Random Timer", "true");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "ConstantTimer.delay", "${" + thinktimeType + "}");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "RandomTimer.range", "${" + thinktimeType + "_Random}");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("hashTree");
            xmlWriter.WriteEndElement();

            WriteStartElement(xmlWriter, "TestAction", "TestActionGui", "TestAction", "Test Action - nothing", "true");
            WriteElementWithTextChildren(xmlWriter, "intProp", "ActionProcessor.action", "1");
            WriteElementWithTextChildren(xmlWriter, "intProp", "ActionProcessor.target", "0");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "ActionProcessor.duration", "0");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("hashTree");
            xmlWriter.WriteEndElement();

            // 'hashTree'
            xmlWriter.WriteEndElement();
        }

        internal static void WriteExampleOfRegExpExtractor(XmlWriter xmlWriter)
        {
            /*
            <RegexExtractor guiclass="RegexExtractorGui" testclass="RegexExtractor" testname="RegEx Extractor - AjaxKey" enabled="true">
              <stringProp name="RegexExtractor.useHeaders">false</stringProp>
              <stringProp name="RegexExtractor.refname">AjaxKey</stringProp>
              <stringProp name="RegexExtractor.regex">,&quot;GX_AJAX_KEY&quot;:&quot;([^&quot;]+)&quot;,</stringProp>
              <stringProp name="RegexExtractor.template">$1$</stringProp>
              <stringProp name="RegexExtractor.default">NOT FOUND</stringProp>
              <stringProp name="RegexExtractor.match_number">1</stringProp>
            </RegexExtractor>
            <hashTree/>
             */

            WriteStartElement(xmlWriter, "RegexExtractor", "RegexExtractorGui", "RegexExtractor",
                            "RegExp Extractor - Example", "false");

            WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.useHeaders", "false");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.refname", "VARIABLE_NAME");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.regex",
                                       ",\"VARIABLE_NAME\":\"([^\"]+*)\",");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.template", "$1$");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.default", "NOT FOUND");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "RegexExtractor.match_number", "1");

            xmlWriter.WriteEndElement();

            // Add and close 'hashTree'
            xmlWriter.WriteStartElement("hashTree");
            xmlWriter.WriteEndElement();
            //*/
        }

        internal static void WriteCSVDataSet(XmlWriter xmlWriter, string fileName, string variableNames)
        {
            //<CSVDataSet guiclass="TestBeanGUI" testclass="CSVDataSet" testname="CSV Data Set - Alta Cliente.csv" enabled="true">
            //  <stringProp name="delimiter">,</stringProp>
            //  <stringProp name="fileEncoding"></stringProp>
            //  <stringProp name="filename">${DataFolder}/Alta Cliente.csv</stringProp>
            //  <boolProp name="quotedData">false</boolProp>
            //  <boolProp name="recycle">true</boolProp>
            //  <stringProp name="shareMode">Current thread group</stringProp>
            //  <boolProp name="stopThread">false</boolProp>
            //  <stringProp name="variableNames">ClientFirstName,ClientLastName,ClientBalance</stringProp>
            //</CSVDataSet>
            //<hashTree/>

            WriteStartElement(xmlWriter, "CSVDataSet", "TestBeanGUI", "CSVDataSet", "CSV Data Set - " + fileName, "true");

            WriteElementWithTextChildren(xmlWriter, "stringProp", "delimiter", ",");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "fileEncoding", string.Empty);
            WriteElementWithTextChildren(xmlWriter, "stringProp", "filename", "${DataFolder}/" + fileName);
            WriteElementWithTextChildren(xmlWriter, "boolProp", "quotedData", "false");
            WriteElementWithTextChildren(xmlWriter, "boolProp", "recycle", "true");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "shareMode", "Current thread group");
            WriteElementWithTextChildren(xmlWriter, "boolProp", "stopThread", "false");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "variableNames", variableNames);

            // CookieManager
            xmlWriter.WriteEndElement();

            // Add and close 'hashTree'
            xmlWriter.WriteStartElement("hashTree");
            xmlWriter.WriteEndElement();
        }

        internal static void WriteCookieManager(XmlWriter xmlWriter)
        {
            //<CookieManager guiclass="CookiePanel" testclass="CookieManager" testname="HTTP Cookie Manager" enabled="true">
            //  <collectionProp name="CookieManager.cookies"/>
            //  <boolProp name="CookieManager.clearEachIteration">true</boolProp>
            //</CookieManager>
            //<hashTree/>

            WriteStartElement(xmlWriter, "CookieManager", "CookiePanel", "CookieManager", "HTTP Cookie Manager", "true");

            WriteElementWithTextChildren(xmlWriter, "collectionProp", "CookieManager.cookies", string.Empty);
            WriteElementWithTextChildren(xmlWriter, "boolProp", "CookieManager.clearEachIteration", "true");

            // CookieManager
            xmlWriter.WriteEndElement();

            // Add and close 'hashTree'
            xmlWriter.WriteStartElement("hashTree");
            xmlWriter.WriteEndElement();
        }

        internal static void WriteResultCollector(XmlWriter xmlWriter, CommonCollectorTypes cCollectorTypes)
        {
            string testName;
            string guiclass;
            switch (cCollectorTypes)
            {
                case CommonCollectorTypes.AggregateReport:
                    guiclass = "StatVisualizer";
                    testName = "Aggregate Report";
                    break;

                case CommonCollectorTypes.ResponseTimeGraph:
                    guiclass = "RespTimeGraphVisualizer";
                    testName = "Response Time Graph";
                    break;

                case CommonCollectorTypes.ResultsLogFile:
                    testName = "ResultsLogFile";
                    guiclass = "SimpleDataWriter";
                    break;

                case CommonCollectorTypes.ResultsXMLFile:
                    guiclass = "SimpleDataWriter";
                    testName = "ResultsXMLFile";
                    break;

                case CommonCollectorTypes.ViewResultsInTable:
                    guiclass = "TableVisualizer";
                    testName = "View Results in Table";
                    break;

                case CommonCollectorTypes.ViewResultsTree:
                    guiclass = "ViewResultsFullVisualizer";
                    testName = "View Results Tree";
                    break;

                default:
                    throw new Exception("CommonArgument enumerator not implemented: " + cCollectorTypes);
            }

            WriteStartElement(xmlWriter, "ResultCollector", guiclass, "ResultCollector", testName, "false");

            // <boolProp name="ResultCollector.error_logging">false</boolProp>
            xmlWriter.WriteStartElement("boolProp");
            xmlWriter.WriteAttributeString("name", "ResultCollector.error_logging");
            xmlWriter.WriteString("false");
            xmlWriter.WriteEndElement();

            // <stringProp name="filename">.......</stringProp>
            switch (cCollectorTypes)
            {
                case CommonCollectorTypes.ResultsLogFile:
                case CommonCollectorTypes.ResultsXMLFile:
                    WriteElementWithTextChildren(xmlWriter, "stringProp", "filename",
                                               (cCollectorTypes == CommonCollectorTypes.ResultsLogFile
                                                    ? "${ResultLogFileName}"
                                                    : "${ResultXMLFileName}"));
                    break;
            }

            // <objProp>
            xmlWriter.WriteStartElement("objProp");

            // <name>saveConfig</name>
            WriteSimpleElement(xmlWriter, "name", "saveConfig");

            // <value class="SampleSaveConfiguration">
            xmlWriter.WriteStartElement("value");
            xmlWriter.WriteAttributeString("class", "SampleSaveConfiguration");

            // Adding common attributes
            WriteSimpleElement(xmlWriter, "time", "true");
            WriteSimpleElement(xmlWriter, "latency", "true");
            WriteSimpleElement(xmlWriter, "timestamp", "true");
            WriteSimpleElement(xmlWriter, "success", "true");
            WriteSimpleElement(xmlWriter, "label", "true");
            WriteSimpleElement(xmlWriter, "code", "true");
            WriteSimpleElement(xmlWriter, "message", "true");
            WriteSimpleElement(xmlWriter, "threadName", "true");
            WriteSimpleElement(xmlWriter, "bytes", "true");
            WriteSimpleElement(xmlWriter, "threadCounts", "true");

            WriteSimpleElement(xmlWriter, "encoding", "false");
            WriteSimpleElement(xmlWriter, "responseData", "false");
            WriteSimpleElement(xmlWriter, "samplerData", "false");
            WriteSimpleElement(xmlWriter, "responseHeaders", "false");
            WriteSimpleElement(xmlWriter, "requestHeaders", "false");
            WriteSimpleElement(xmlWriter, "responseDataOnError", "false");
            WriteSimpleElement(xmlWriter, "assertionsResultsToSave", "0");

            switch (cCollectorTypes)
            {
                case CommonCollectorTypes.AggregateReport:
                case CommonCollectorTypes.ViewResultsInTable:
                case CommonCollectorTypes.ViewResultsTree:
                case CommonCollectorTypes.ResponseTimeGraph:
                    WriteSimpleElement(xmlWriter, "dataType", "true");
                    WriteSimpleElement(xmlWriter, "assertions", "true");
                    WriteSimpleElement(xmlWriter, "subresults", "true");
                    WriteSimpleElement(xmlWriter, "saveAssertionResultsFailureMessage", "false");
                    break;

                case CommonCollectorTypes.ResultsLogFile:
                case CommonCollectorTypes.ResultsXMLFile:
                    WriteSimpleElement(xmlWriter, "dataType", "false");
                    WriteSimpleElement(xmlWriter, "assertions", "false");
                    WriteSimpleElement(xmlWriter, "subresults", "false");
                    WriteSimpleElement(xmlWriter, "saveAssertionResultsFailureMessage", "true");
                    break;
            }

            switch (cCollectorTypes)
            {
                case CommonCollectorTypes.AggregateReport:
                case CommonCollectorTypes.ViewResultsTree:
                case CommonCollectorTypes.ResponseTimeGraph:
                    WriteSimpleElement(xmlWriter, "fieldNames", "false");
                    break;

                case CommonCollectorTypes.ViewResultsInTable:
                case CommonCollectorTypes.ResultsLogFile:
                case CommonCollectorTypes.ResultsXMLFile:
                    WriteSimpleElement(xmlWriter, "fieldNames", "true");
                    break;
            }

            switch (cCollectorTypes)
            {
                case CommonCollectorTypes.AggregateReport:
                case CommonCollectorTypes.ViewResultsTree:
                case CommonCollectorTypes.ResponseTimeGraph:
                case CommonCollectorTypes.ViewResultsInTable:
                case CommonCollectorTypes.ResultsLogFile:
                    WriteSimpleElement(xmlWriter, "xml", "false");
                    break;

                case CommonCollectorTypes.ResultsXMLFile:
                    WriteSimpleElement(xmlWriter, "xml", "true");
                    break;
            }

            // </value>
            xmlWriter.WriteEndElement();

            // </objProp>
            xmlWriter.WriteEndElement();

            // ResultCollector
            xmlWriter.WriteEndElement();

            // Add and close 'hashTree'
            xmlWriter.WriteStartElement("hashTree");
            xmlWriter.WriteEndElement();
        }

        internal static void WriteThreadGroup(XmlWriter xmlWriter, string name, string threads, string iterations, string rumpUp)
        {
            // <ThreadGroup guiclass="ThreadGroupGui" testclass="ThreadGroup" testname="Caso de Prueba Alta Cliente" enabled="true">
            WriteStartElement(xmlWriter, "ThreadGroup", "ThreadGroupGui", "ThreadGroup", "Caso de Prueba " + name, "true");

            // <stringProp name="ThreadGroup.on_sample_error">startnextloop</stringProp>
            WriteElementWithTextChildren(xmlWriter, "stringProp", "ThreadGroup.on_sample_error", "startnextloop");

            //  <elementProp name="ThreadGroup.main_controller" elementType="LoopController" guiclass="LoopControlPanel" testclass="LoopController" testname="Loop Controller" enabled="true">
            //      <boolProp name="LoopController.continue_forever">false</boolProp>
            //      <stringProp name="LoopController.loops">${iterationsAltaCliente}</stringProp>
            //  </elementProp>
            WriteStartElement(xmlWriter, "elementProp", "ThreadGroup.main_controller", "LoopController",
                            "LoopControlPanel", "LoopController", "Loop Controller", "true");
            WriteElementWithTextChildren(xmlWriter, "boolProp", "LoopController.continue_forever", "false");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "LoopController.loops", "${" + iterations + "}");
            xmlWriter.WriteEndElement();

            // <stringProp name="ThreadGroup.num_threads">${threadsAltaCliente}</stringProp>
            WriteElementWithTextChildren(xmlWriter, "stringProp", "ThreadGroup.num_threads", "${" + threads + "}");

            // <stringProp name="ThreadGroup.ramp_time">${rumpUpAltaCliente}</stringProp>
            WriteElementWithTextChildren(xmlWriter, "stringProp", "ThreadGroup.ramp_time", "${" + rumpUp + "}");

            // <longProp name="ThreadGroup.start_time">1379894843000</longProp>
            WriteElementWithTextChildren(xmlWriter, "longProp", "ThreadGroup.start_time", "1379894843000");

            // <longProp name="ThreadGroup.end_time">1379894843000</longProp>
            WriteElementWithTextChildren(xmlWriter, "longProp", "ThreadGroup.end_time", "1379894843000");

            // <boolProp name="ThreadGroup.scheduler">false</boolProp>
            WriteElementWithTextChildren(xmlWriter, "boolProp", "ThreadGroup.scheduler", "false");

            // <stringProp name="ThreadGroup.duration"></stringProp>
            WriteElementWithTextChildren(xmlWriter, "stringProp", "ThreadGroup.duration", string.Empty);

            // <stringProp name="ThreadGroup.delay"></stringProp>
            WriteElementWithTextChildren(xmlWriter, "stringProp", "ThreadGroup.delay", string.Empty);

            // ThreadGroup
            xmlWriter.WriteEndElement();
        }

        internal static void WriteArgument(XmlWriter xmlWriter, CommonArgumentTypes cArgmnt)
        {
            string testName;
            switch (cArgmnt)
            {
                case CommonArgumentTypes.Paths:
                    testName = "Vars - Paths";
                    break;

                case CommonArgumentTypes.ThinkTimes:
                    testName = "Vars - Think Times";
                    break;

                case CommonArgumentTypes.HTTPHeaders:
                    testName = "Vars - Cache HTTP Headers";
                    break;

                default:
                    throw new Exception("CommonArgument enumerator not implemented: " + cArgmnt);
            }

            WriteStartElement(xmlWriter, "Arguments", "ArgumentsPanel", "Arguments", testName, "true");

            xmlWriter.WriteStartElement("collectionProp");
            xmlWriter.WriteAttributeString("name", "Arguments.arguments");

            switch (cArgmnt)
            {
                case CommonArgumentTypes.Paths:
                    WriteArgumentToCollectionProp(xmlWriter, "ResultXMLFileName",
                                                "${HomeFolder}/Resultados/${__time(YMDHMS)}_Result.xml");
                    WriteArgumentToCollectionProp(xmlWriter, "ResultLogFileName",
                                                "${HomeFolder}/Resultados/${__time(YMDHMS)}_Result.log");
                    WriteArgumentToCollectionProp(xmlWriter, "DataFolder", "${HomeFolder}/Datos");
                    break;

                case CommonArgumentTypes.ThinkTimes:
                    WriteArgumentToCollectionProp(xmlWriter, ThinktimeType.Low + string.Empty, "5000");
                    WriteArgumentToCollectionProp(xmlWriter, ThinktimeType.Medium + string.Empty, "30000");
                    WriteArgumentToCollectionProp(xmlWriter, ThinktimeType.Large + string.Empty, "60000");
                    WriteArgumentToCollectionProp(xmlWriter, ThinktimeType.Low + "_Random", "2000");
                    WriteArgumentToCollectionProp(xmlWriter, ThinktimeType.Medium + "_Random", "5000");
                    WriteArgumentToCollectionProp(xmlWriter, ThinktimeType.Large + "_Random", "10000");
                    break;

                case CommonArgumentTypes.HTTPHeaders:
                    WriteArgumentToCollectionProp(xmlWriter, "IMS_style", "Tue, 08 Oct 2013 17:47:16 GMT",
                                                "If-Modified-Since header used in cache for css file");
                    WriteArgumentToCollectionProp(xmlWriter, "INM_style", "\"6bb2-59b8-4e8292fcb5300\"",
                                                "If-None-Match header used in cache for css file");
                    WriteArgumentToCollectionProp(xmlWriter, "------------------", "---------------------",
                                                "----------------------");
                    break;
            }

            // collectionProp
            xmlWriter.WriteEndElement();

            // Arguments
            xmlWriter.WriteEndElement();

            // Add and close 'hashTree'
            xmlWriter.WriteStartElement("hashTree");
            xmlWriter.WriteEndElement();
        }

        internal static void WriteArgumentToCollectionProp(XmlWriter xmlWriter, string name, string value,
                                                string desc = null)
        {
            xmlWriter.WriteStartElement("elementProp");
            xmlWriter.WriteAttributeString("name", name);
            xmlWriter.WriteAttributeString("elementType", "Argument");

            // <stringProp name="Argument.name">NAME</stringProp>
            WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.name", name);

            // <stringProp name="Argument.value">VALUE</stringProp>
            WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.value", value);

            // <stringProp name="Argument.metadata">=</stringProp>
            WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.metadata", "=");

            if (desc != null)
            {
                // <stringProp name="Argument.desc">blah blah blah</stringProp>
                WriteElementWithTextChildren(xmlWriter, "stringProp", "Argument.desc", desc);
            }

            // elementProp
            xmlWriter.WriteEndElement();
        }

        internal static void WriteResponseAssertionSkipHTTPResponse(XmlWriter xmlWriter)
        {
            WriteStartElement(xmlWriter, "ResponseAssertion", "AssertionGui", "ResponseAssertion", "Disable response code validation for secondary requests", "true");
            WriteStartAndEndElement(xmlWriter, "collectionProp", "Asserion.test_strings");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "Assertion.test_field", "Assertion.response_code");
            WriteElementWithTextChildren(xmlWriter, "boolProp", "Assertion.assume_success", "true");
            WriteElementWithTextChildren(xmlWriter, "intProp", "Assertion.test_type", "2");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("hashTree");
            xmlWriter.WriteEndElement();
        }

        /// <summary>
        /// Writes &lt;'elementName' guiclass='guiclass' testclass='testclass' testname='testname' enabled='enabled'&gt;
        /// </summary>
        internal static void WriteStartElement(XmlWriter xmlWriter, string elementName, string guiclass, string testclass,
                                    string testname, string enabled)
        {
            xmlWriter.WriteStartElement(elementName);
            xmlWriter.WriteAttributeString("guiclass", guiclass);
            xmlWriter.WriteAttributeString("testclass", testclass);
            xmlWriter.WriteAttributeString("testname", testname);
            xmlWriter.WriteAttributeString("enabled", enabled);
        }

        /// <summary>
        /// Writes &lt;'elementName' name='name' elementType='elementType' guiclass='guiclass' testclass='testclass' testname='testname' enabled='enabled'&gt;
        /// </summary>
        internal static void WriteStartElement(XmlWriter xmlWriter, string elementName, string name, string elementType,
                                            string guiclass, string testclass, string testname, string enabled)
        {
            xmlWriter.WriteStartElement(elementName);
            xmlWriter.WriteAttributeString("name", name);
            xmlWriter.WriteAttributeString("elementType", elementType);
            xmlWriter.WriteAttributeString("guiclass", guiclass);
            xmlWriter.WriteAttributeString("testclass", testclass);
            xmlWriter.WriteAttributeString("testname", testname);
            xmlWriter.WriteAttributeString("enabled", enabled);
        }

        /// <summary>
        /// Writes &lt;'elementName' name='name' elementType='elementType'&gt;
        /// </summary>
        internal static void WriteStartElement(XmlWriter xmlWriter, string elementName, string name, string elementType)
        {
            xmlWriter.WriteStartElement(elementName);
            xmlWriter.WriteAttributeString("name", name);
            xmlWriter.WriteAttributeString("elementType", elementType);
        }

        /// <summary>
        /// Writes &lt;'elementName' name='name'&gt;
        /// </summary>
        internal static void WriteStartElement(XmlWriter xmlWriter, string elementName, string name)
        {
            xmlWriter.WriteStartElement(elementName);
            xmlWriter.WriteAttributeString("name", name);
        }

        /// <summary>
        /// Writes &lt;'elementName' name='name'&gt;&lt;/'elementName'&gt;
        /// </summary>
        internal static void WriteStartAndEndElement(XmlWriter xmlWriter, string elementName, string name)
        {
            xmlWriter.WriteStartElement(elementName);
            xmlWriter.WriteAttributeString("name", name);
            xmlWriter.WriteEndElement();
        }

        /// <summary>
        /// Writes &lt;'elementName'&gt;'value'&lt;/'elementName'&gt;
        /// </summary>
        internal static void WriteSimpleElement(XmlWriter xmlWriter, string elementName, string elementValue)
        {
            xmlWriter.WriteStartElement(elementName);
            xmlWriter.WriteString(elementValue);
            xmlWriter.WriteEndElement();
        }

        /// <summary>
        /// Writes &lt;'elementName' name='valueOfPropName'&gt;'elementValue'&lt;/'elementName'&gt;
        /// </summary>
        internal static void WriteElementWithTextChildren(XmlWriter xmlWriter, string elementName, string valueOfPropName,
                                                       string elementValue)
        {
            xmlWriter.WriteStartElement(elementName);
            xmlWriter.WriteAttributeString("name", valueOfPropName);
            xmlWriter.WriteString(elementValue);
            xmlWriter.WriteEndElement();
        }

        internal static void WriteBeanShellAssertionModule(XmlWriter xmlWriter)
        {
            /*
            <BeanShellAssertion guiclass="BeanShellAssertionGui" testclass="BeanShellAssertion" testname="BeanShell Assertion" enabled="true">
              <stringProp name="TestPlan.comments">Verify </stringProp>
              <stringProp name="BeanShellAssertion.query">
	if (vars.get(&quot;logFails_enable&quot;) == &quot;1&quot;)
	{
		for (a: SampleResult.getAssertionResults()) 
		{
			if (a.isError() || a.isFailure()) 
			{
				log.error(
					Thread.currentThread().getName() + 
					&quot;: &quot; + 
					SampleLabel + 
					&quot;: Assertion failed for response: &quot; + 
					new String((byte[]) ResponseData)
				);
			}
		}
	}
	else 
	{
		log.error(vars.get(&quot;logFails_enable&quot;));
	}
              </stringProp>
              <stringProp name="BeanShellAssertion.filename"></stringProp>
              <stringProp name="BeanShellAssertion.parameters"></stringProp>
              <boolProp name="BeanShellAssertion.resetInterpreter">false</boolProp>
            </BeanShellAssertion>
             // */

            const string name = "If (fail) then Log HTTP Response in JMeter.log";
            const string bsQuery = "\n\tif (vars.get(\"" + HTTPConstants.VariableLogFails + "\") == \"1\")\n" +
                                   "\t{\n" +
                                   "\t\tfor (a: SampleResult.getAssertionResults())\n" +
                                   "\t\t{\n" +
                                   "\t\t\tif (a.isError() || a.isFailure())\n" +
                                   "\t\t\t{\n" +
                                   "\t\t\t\tlog.error(\n" +
                                   "\t\t\t\t\tThread.currentThread().getName() + \n" +
                                   "\t\t\t\t\t\": \" + \n" +
                                   "\t\t\t\t\tSampleLabel + \n" +
                                   "\t\t\t\t\t\": Assertion failed for response: \" + \n" +
                                   "\t\t\t\t\tnew String((byte[]) ResponseData)\n" +
                                   "\t\t\t\t);\n" +
                                   "\t\t\t}\n" +
                                   "\t\t}\n" +
                                   "\t}\n";

            WriteStartElement(xmlWriter, "BeanShellAssertion", "BeanShellAssertionGui", "BeanShellAssertion", name, "true");
            WriteElementWithTextChildren(xmlWriter, "stringProp", "TestPlan.comments", name);
            WriteElementWithTextChildren(xmlWriter, "stringProp", "BeanShellAssertion.query", bsQuery);
            WriteElementWithTextChildren(xmlWriter, "stringProp", "BeanShellAssertion.filename", string.Empty);
            WriteElementWithTextChildren(xmlWriter, "stringProp", "BeanShellAssertion.parameters", string.Empty);
            WriteElementWithTextChildren(xmlWriter, "stringProp", "BeanShellAssertion.resetInterpreter", "false");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("hashTree");
            xmlWriter.WriteEndElement();
        }
    }
}
