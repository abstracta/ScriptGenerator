using System;
using System.Collections.Generic;
using System.Xml;
using Abstracta.Generators.Framework.AbstractGenerator;
using Abstracta.Generators.Framework.Constants;
using Abstracta.Generators.Framework.JMeterGenerator.AuxiliarClasses;
using GxTest.Utils.EnumTypes;

namespace Abstracta.Generators.Framework.JMeterGenerator
{
    internal class JMeterGenerator : AbstractGenerator.AbstractGenerator
    {
        private readonly List<DataPool> _dataPools = new List<DataPool>();

        internal override void AddDataPools(List<DataPool> dataPools, string dataPoolFilesPath)
        {
            _dataPools.AddRange(dataPools);
        }

        internal override AbstractStep AddStep(string name, string type, string description, ScriptGenerator generator, int index)
        {
            string servName, servPort;
            GetServerAndPortFromServerName(generator.ServerName, out servName, out servPort);

            var newStep = new Step
                {
                    Name = name,
                    Type = (CommandType)Enum.Parse(typeof(CommandType), type), 
                    Desc = description,
                    ServerName = servName,
                    ServerPort = servPort,
                    WebApp = generator.WebAppName,
                    Index = index,
                };

            AddStep(newStep);

            return newStep;
        }

        internal override void Save()
        {
            var xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
            };

            var threads = "threads" + ScriptName;
            var iterations = "iterations" + ScriptName;
            var rumpUp = "rumpUp" + ScriptName;

            using (var xmlWriter = XmlWriter.Create(HomeFolder + ScriptName + ".jmx", xmlWriterSettings))
            {
                xmlWriter.WriteStartDocument();

                // jmeterTestPlan
                xmlWriter.WriteStartElement("jmeterTestPlan");
                xmlWriter.WriteAttributeString("version", "1.2"); // TODO add parameter config for version attr
                xmlWriter.WriteAttributeString("properties", "2.5"); // TODO add parameter config for properties attr
                xmlWriter.WriteAttributeString("jmeter", "2.10 r1533061"); // TODO add parameter config for jmeter version

                // hashTree of jMeterTestPlan
                xmlWriter.WriteStartElement("hashTree");

                # region Test plan definition

                InitializeTestPlan(xmlWriter, threads, iterations, rumpUp);

                # endregion Test plan definition

                # region Test plan content

                // hashTree of TestPlan
                xmlWriter.WriteStartElement("hashTree");

                # region Common Elements

                // Adding Arguments 
                JMeterWrapper.WriteArgument(xmlWriter, CommonArgumentTypes.Paths);
                JMeterWrapper.WriteArgument(xmlWriter, CommonArgumentTypes.ThinkTimes);
                JMeterWrapper.WriteArgument(xmlWriter, CommonArgumentTypes.HTTPHeaders);

                // Adding ResultCollectors 
                JMeterWrapper.WriteResultCollector(xmlWriter, CommonCollectorTypes.ResultsXMLFile);
                JMeterWrapper.WriteResultCollector(xmlWriter, CommonCollectorTypes.ResultsLogFile);
                JMeterWrapper.WriteResultCollector(xmlWriter, CommonCollectorTypes.AggregateReport);
                JMeterWrapper.WriteResultCollector(xmlWriter, CommonCollectorTypes.ViewResultsInTable);
                JMeterWrapper.WriteResultCollector(xmlWriter, CommonCollectorTypes.ViewResultsTree);
                JMeterWrapper.WriteResultCollector(xmlWriter, CommonCollectorTypes.ResponseTimeGraph);

                # endregion Common Elements

                # region ThreadGroup

                // adding default Thread Group
                JMeterWrapper.WriteThreadGroup(xmlWriter, ScriptName, threads, iterations, rumpUp);

                // hashTree of threadGroup
                xmlWriter.WriteStartElement("hashTree");

                AddThreadGroupContent(xmlWriter);

                // hashtree
                xmlWriter.WriteEndElement();

                # endregion Threadgroup

                // hashTree
                xmlWriter.WriteEndElement();

                # endregion Test plan content

                // hashtree
                xmlWriter.WriteEndElement();
                // jmeterTestPlan
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
        }

        private void InitializeTestPlan(XmlWriter xmlWriter, string threads, string iterations, string rumpUp)
        {
            string servName, servPort;
            GetServerAndPortFromServerName(ServerName, out servName, out servPort);

            JMeterWrapper.WriteStartElement(xmlWriter, "TestPlan", "TestPlanGui", "TestPlan", "Test Plan", "true");

            // <stringProp name="TestPlan.comments"></stringProp>
            // <boolProp name="TestPlan.functional_mode">false</boolProp>
            // <boolProp name="TestPlan.serialize_threadgroups">false</boolProp>
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "TestPlan.comments", string.Empty);
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "TestPlan.functional_mode", "false");
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "boolProp", "TestPlan.serialize_threadgroups", "false");

            // <elementProp name="TestPlan.user_defined_variables" elementType="Arguments" guiclass="ArgumentsPanel" testclass="Arguments" testname="User Defined Variables" enabled="true">
            JMeterWrapper.WriteStartElement(xmlWriter, "elementProp", "TestPlan.user_defined_variables", "Arguments", "ArgumentsPanel",
                            "Arguments", "User Defined Variables", "true");

            // <collectionProp name="Arguments.arguments">
            JMeterWrapper.WriteStartElement(xmlWriter, "collectionProp", "Arguments.arguments");

            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, "HomeFolder", HomeFolder);
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, "-----------------------------------",
                                        "-----------------------------------");
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, HTTPConstants.VariableNameServer, servName);
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, HTTPConstants.VariableNamePort, servPort);
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, HTTPConstants.VariableNameWebApp, WebAppName);
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, "-----------------------------------",
                                        "-----------------------------------");
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, threads, "${__P(tut1,1)}");
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, iterations, "${__P(iut1,1)}");
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, rumpUp, "${__P(rut1,3600)}");
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, "-----------------------------------",
                                        "-----------------------------------");
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, HTTPConstants.VariableNameDebug, "0");
            JMeterWrapper.WriteArgumentToCollectionProp(xmlWriter, HTTPConstants.VariableLogFails, "1");

            // collectionProp
            xmlWriter.WriteEndElement();

            // elementProp
            xmlWriter.WriteEndElement();

            // <stringProp name="TestPlan.user_define_classpath"></stringProp>
            JMeterWrapper.WriteElementWithTextChildren(xmlWriter, "stringProp", "TestPlan.user_define_classpath", string.Empty);

            // TestPlan
            xmlWriter.WriteEndElement();
        }

        private void AddThreadGroupContent(XmlWriter xmlWriter)
        {
            JMeterWrapper.WriteCookieManager(xmlWriter);

            foreach (var dataPool in _dataPools)
            {
                JMeterWrapper.WriteCSVDataSet(xmlWriter, dataPool.FileName, dataPool.ColumnsJoined(","));
            }

            JMeterWrapper.WriteExampleOfRegExpExtractor(xmlWriter);

            foreach (var step in Steps)
            {
                step.Name = GetStepName(Steps.Count, step.Index, step.Type, step.Name);
                
                xmlWriter.WriteRaw(step.ToString());
                
                JMeterWrapper.WriteThinkTime(xmlWriter, ThinktimeType.Medium);
            }
        }

        private static string GetStepName(int totalSteps, int stepIndex, CommandType stepType, string stepName)
        {
            string stepStr;

            var digitsNumber = (totalSteps <= 9) ? 1 : ((totalSteps <= 99) ? 2 : 3);
            switch (digitsNumber)
            {
                case 1:
                    stepStr = "Step " + stepIndex + " - " + stepType + " " + stepName;
                    break;

                case 2:
                    stepStr = "Step " + ((stepIndex <= 9) ? "0" + stepIndex : "" + stepIndex)
                              + " - " + stepType + " " + stepName;
                    break;

                default:
                    stepStr = "Step " + ((stepIndex <= 9)
                                             ? "00" + stepIndex
                                             : ((stepIndex <= 99) ? "0" + stepIndex : "" + stepIndex))
                                                + " - " + stepType + " " + stepName;
                    break;
            }

            return stepStr;
        }

        private static void GetServerAndPortFromServerName(string serverName, out string servName, out string servPort)
        {
            var tmp = serverName.Split(':');
            if (tmp.Length == 1)
            {
                servName = serverName;
                servPort = HTTPConstants.DefaultPortStr;
            }
            else
            {
                servName = tmp[0];
                servPort = tmp[1];
            }
        }
    }
}
