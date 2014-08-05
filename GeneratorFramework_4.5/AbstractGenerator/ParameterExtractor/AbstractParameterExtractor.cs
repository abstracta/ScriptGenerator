namespace Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor
{
    internal abstract class AbstractParameterExtractor
    {
        internal string VariableName { get; private set; }

        internal string ValueToReplace { get; private set; }

        internal string Description { get; private set; }

        protected AbstractParameterExtractor(string variableName, string valueToReplace, string description)
        {
            VariableName = variableName;
            ValueToReplace = valueToReplace;
            Description = description;
        }
    }
}
