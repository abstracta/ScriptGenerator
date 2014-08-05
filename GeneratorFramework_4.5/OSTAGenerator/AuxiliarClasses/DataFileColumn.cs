using System.Collections.Generic;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses
{
    internal class DataFileColumn
    {
        public string Name { get; set; }
        public List<string> Values { get; set; }
        public string VariableName { get; set; }

        public DataFileColumn(string name, string variable, string value)
        {
            Name = name;
            VariableName = variable;
            Values = new List<string> {value};
        }

        public DataFileColumn(string name, string variable, List<string> values)
        {
            Name = name;
            VariableName = variable;
            Values = values;
        }
    }
}
