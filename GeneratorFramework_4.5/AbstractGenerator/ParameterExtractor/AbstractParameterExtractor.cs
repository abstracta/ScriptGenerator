namespace Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor
{
    public enum ExtractFrom { Body, Headers, Url }
    public enum UseIn { Body, Url }

    internal abstract class AbstractParameterExtractor
    {
        internal string VariableName { get; private set; }

        internal string ValueToReplace { get; private set; }

        internal string Description { get; private set; }

        internal ExtractFrom ExtractParameterFrom { get; private set; }

        internal UseIn UseParameterIn { get; private set; }

        protected AbstractParameterExtractor(ExtractFrom extractParameterFrom, UseIn useParameterIn, string variableName, string valueToReplace, string description)
        {
            ExtractParameterFrom = extractParameterFrom;
            UseParameterIn = useParameterIn;
            VariableName = variableName;
            ValueToReplace = valueToReplace;
            Description = description;
        }
    }
}
