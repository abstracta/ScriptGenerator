namespace Abstracta.Generators.Framework.TestingGenerator.ParameterExtractor
{
    internal class TestRegExParameter : AbstractGenerator.ParameterExtractor.AbstractRegExParameter
    {
        internal TestRegExParameter(string varibleName, string regularExpression, string valueToReplace, string description)
            : base(varibleName, regularExpression, valueToReplace, description)
        {
        }

        public override string ToString()
        {
            return "Extracted Parameter from Redirect Response: " + VariableName + ": " + RegularExpression;
        }
    }
}
