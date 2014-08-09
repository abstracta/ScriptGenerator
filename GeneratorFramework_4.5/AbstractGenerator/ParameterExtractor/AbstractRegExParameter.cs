namespace Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor
{
    internal abstract class AbstractRegExParameter : AbstractParameterExtractor
    {
        internal string RegularExpression { get; set; }

        internal string Group { get; set; }

        protected AbstractRegExParameter(string variableName, string regularExpression, string group, string valueToReplace, string description)
            : base(variableName, valueToReplace, description)
        {
            RegularExpression = regularExpression;
            Group = group;
        }
    }
}
