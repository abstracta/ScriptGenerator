using Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor;

namespace Abstracta.Generators.Framework.OSTAGenerator.ParameterExtractor
{
    internal class OSTARegExParameter : AbstractRegExParameter
    {
        internal OSTARegExParameter(ExtractFrom extractParameterFrom, UseIn useParameterIn, string varibleName, string regularExpression, string group, string valueToReplace, string description)
            : base(extractParameterFrom, useParameterIn, varibleName, regularExpression, group, valueToReplace, description)
        {
        }

        public override string ToString()
        {
            return "Extracted Parameter from Redirect Response: " + VariableName + ": " + RegularExpression;
        }
    }
}
