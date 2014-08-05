using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Abstracta.Generators.Framework.AbstractGenerator.Wrappers
{
    internal class GxTestScriptWrapper
    {
        private readonly List<DataPool> _dataPools;
        private readonly List<Command> _commands;

        internal GxTestScriptWrapper(XmlDocument performanceScript)
        {
            // read the name of the script file
            if (performanceScript.FirstChild.Attributes == null)
            {
                throw new FormatException("Malformed XML. Expected attribute list");
            }

            ScriptName = performanceScript.FirstChild.Attributes["Name"].Value;
            ScriptName = ScriptName.Replace(" ", "");
            ScriptName = ScriptName.Replace("\t", "");
            
            _dataPools = new List<DataPool>();
            _commands = new List<Command>();

            # region Getting Datapools

            var dataPools = performanceScript.GetElementsByTagName("DataPool");
            foreach (var dp in from object dataPool in dataPools select dataPool as XmlNode)
            {
                if (dp == null)
                {
                    throw new Exception("Exception reading 'performanceScript' XML file. DataPool element isn't an XmlNode as expected.");
                }

                if (dp.Attributes == null)
                {
                    throw new Exception("Exception reading 'performanceScript' XML file. DataPool element hasn't attributes as expected");
                }

                var dpName = dp.Attributes["Name"].Value;
                var dpFileName = dp.Attributes["File"].Value;

                if (!dp.HasChildNodes)
                {
                    throw new Exception("Exception reading 'performanceScript' XML file. DataPool element hasn't childNodes as expected");
                }

                var dpColumns = new List<string>();

                foreach (var dpc in from object dataPoolColumn in dp.ChildNodes select dataPoolColumn as XmlNode)
                {
                    if (dpc == null)
                    {
                        throw new Exception("Exception reading 'performanceScript' XML file. DataPoolColumn element isn't an XmlNode as expected.");
                    }

                    if (dpc.Attributes == null)
                    {
                        throw new Exception("Exception reading 'performanceScript' XML file. DataPoolColumn element hasn't attributes as expected");
                    }

                    var dpcName = dpc.Attributes["Name"].Value;

                    dpColumns.Add(dpcName);
                }

                _dataPools.Add(new DataPool(dpName, dpFileName, dpColumns));
            }

            # endregion

            # region Getting Commands

            var commands = performanceScript.GetElementsByTagName("Command");
            foreach (var c in from object command in commands select command as XmlNode)
            {
                if (c == null)
                {
                    throw new Exception("Exception reading 'performanceScript' XML file. Command element isn't an XmlNode as expected.");
                }

                if (c.Attributes == null)
                {
                    throw new Exception("Exception reading 'performanceScript' XML file. DataPool element hasn't attributes as expected"); 
                }

                var cName = c.Attributes["CommandName"].Value;
                var cType = c.Attributes["CommandType"].Value;
                var cDesc = c.Attributes["CommandDsc"].Value;

                if (!c.HasChildNodes)
                {
                    throw new Exception("Exception reading 'performanceScript' XML file. Command element hasn't childNodes as expected");
                }

                var crIds = new List<int>();
                var parameters = new Dictionary<ParametersType, string>();
                foreach (var ccn in from object childNode in c.ChildNodes select childNode as XmlNode)
                {
                    if (ccn == null)
                    {
                        throw new Exception("Exception reading 'performanceScript' XML file. Command Child Node isn't an XmlNode as expected.");
                    }

                    if (!ccn.HasChildNodes)
                    {
                        continue;
                    }

                    switch (ccn.Name)
                    {
                        case "Requests":
                            foreach (var r in from object request in ccn.ChildNodes select request as XmlNode)
                            {
                                if (r == null)
                                {
                                    throw new Exception(
                                        "Exception reading 'performanceScript' XML file. Request isn't an XmlNode as expected.");
                                }

                                if (r.Attributes == null)
                                {
                                    throw new Exception(
                                        "Exception reading 'performanceScript' XML file. Request element hasn't attributes as expected");
                                }

                                var rIdStr = r.Attributes["Id"].Value;
                                var rId = Convert.ToInt32(rIdStr);

                                crIds.Add(rId);
                            }

                            break;

                        case "Parameters":
                            foreach (var r in from object parameter in ccn.ChildNodes select parameter as XmlNode)
                            {
                                if (r == null)
                                {
                                    throw new Exception(
                                        "Exception reading 'performanceScript' XML file. Parameter isn't an XmlNode as expected.");
                                }

                                if (r.Attributes == null)
                                {
                                    throw new Exception(
                                        "Exception reading 'performanceScript' XML file. Parameter element hasn't attributes as expected");
                                }

                                var name = r.Attributes["Name"].Value;
                                var value = r.Attributes["Value"].Value;
                                //// var type = r.Attributes["Type"].Value; // 'Boolean', 'Value', 

                                var paramType = Command.GetParamFromName(name);
                                parameters.Add(paramType, value);
                            }

                            break;
                    }
                }

                _commands.Add(new Command(cName, cType, cDesc, crIds, parameters));
            }

            # endregion
        }

        internal List<DataPool> GetDataPools()
        {
            return _dataPools;
        }

        internal List<Command> GetCommands()
        {
            return _commands;
        }

        internal string ScriptName { get; private set; }
    }
}