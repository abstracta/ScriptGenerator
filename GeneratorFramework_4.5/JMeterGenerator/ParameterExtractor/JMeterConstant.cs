namespace Abstracta.Generators.Framework.JMeterGenerator.ParameterExtractor
{
    internal class JMeterConstant
    {
        internal string Name { get; set; }

        internal string Value { get; set; }

        internal string Description { get; set; }

        public JMeterConstant(string name, string value, string description)
        {
            Name = name;
            Value = value;
            Description = description;
        }
    }
}
