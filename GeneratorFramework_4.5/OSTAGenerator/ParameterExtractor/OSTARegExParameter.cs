namespace Abstracta.Generators.Framework.OSTAGenerator.ParameterExtractor
{
    internal class OSTARegExParameter : AbstractGenerator.ParameterExtractor.AbstractRegExParameter
    {
        internal OSTARegExParameter(string varibleName, string regularExpression, string group, string valueToReplace, string description)
            : base(varibleName, regularExpression, group, valueToReplace, description)
        {
        }

        public override string ToString()
        {
            return "Extracted Parameter from Redirect Response: " + VariableName + ": " + RegularExpression;
        }
    }
}
