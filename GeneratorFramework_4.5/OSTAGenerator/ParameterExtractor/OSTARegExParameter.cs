namespace Abstracta.Generators.Framework.OSTAGenerator.ParameterExtractor
{
    internal class OSTARegExParameter : AbstractGenerator.ParameterExtractor.AbstractRegExParameter
    {
        internal OSTARegExParameter(string varibleName, string regularExpression, string valueToReplace, string description)
            : base(varibleName, regularExpression, valueToReplace, description)
        {
        }

        public override string ToString()
        {
            return "Extracted Parameter from Redirect Response: " + VariableName + ": " + RegularExpression;
        }
    }
}
