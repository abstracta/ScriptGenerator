namespace Abstracta.Generators.Framework.TestingGenerator.ParameterExtractor
{
    internal class TestRegExParameter : AbstractGenerator.ParameterExtractor.AbstractRegExParameter
    {
        internal TestRegExParameter(string varibleName, string regularExpression, string group, string valueToReplace, string description)
            : base(varibleName, regularExpression, group, valueToReplace, description)
        {
        }

        public override string ToString()
        {
            return "Extracted Parameter from Redirect Response: " + VariableName + ": " + RegularExpression;
        }
    }
}
