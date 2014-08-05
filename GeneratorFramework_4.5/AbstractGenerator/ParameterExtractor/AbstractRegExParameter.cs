namespace Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor
{
    internal abstract class AbstractRegExParameter : AbstractParameterExtractor
    {
        internal string RegularExpression { get; set; }

        protected AbstractRegExParameter(string variableName, string regularExpression, string valueToReplace, string description)
            : base(variableName, valueToReplace, description)
        {
            RegularExpression = regularExpression;
        }
    }
}
