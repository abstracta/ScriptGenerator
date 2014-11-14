using System.Collections.Generic;
using Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor;

namespace Abstracta.Generators.Framework.TestingGenerator.ParameterExtractor
{
    internal class TestRegExParameter : AbstractRegExParameter
    {
        internal TestRegExParameter(ExtractFrom extractParameterFrom, List<UseIn> useParameterIn, string varibleName, string regularExpression, string group, string valueToReplace, string description)
            : base(extractParameterFrom, useParameterIn, varibleName, regularExpression, group, valueToReplace, description)
        {
        }

        public override string ToString()
        {
            return "Extracted Parameter from Response: " + VariableName + ": " + RegularExpression + ": " + ExtractParameterFrom + ": " + ValueToReplace;
        }
    }
}
