using System.Collections.Generic;
using System.Linq;
using Abstracta.FiddlerSessionComparer;
using Abstracta.Generators.Framework.AbstractGenerator;
using Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor;
using Abstracta.Generators.Framework.AbstractGenerator.Validations;
using Abstracta.Generators.Framework.TestingGenerator.ParameterExtractor;
using Fiddler;
using ExtractFrom = Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor.ExtractFrom;

namespace Abstracta.Generators.Framework.TestingGenerator
{
    internal class Step : AbstractStep
    {
        protected override AbstractRegExParameter CreateRegExpExtractorToGetRedirectParameters(ExtractFrom extractParameterFrom, List<UseIn> useParameterIn, string varName, string expression, string group, string valueToReplace, string description)
        {
            return new TestRegExParameter(extractParameterFrom, useParameterIn, varName, expression, group, valueToReplace, description);
        }

        protected override AbstractPageRequest CreatePageRequest(Session primaryRequest, AbstractStep abstractStep, Page page)
        {
            return new PageRequest(primaryRequest, abstractStep, page);
        }

        internal override DefaultValidation CreateDefaultValidation()
        {
            return new DefaultValidation();
        }

        internal override CheckMainObjectValidation CreateCheckMainObjectValidation(string objectName)
        {
            return new CheckMainObjectValidation(objectName);
        }

        internal override AppearTextValidation CreateAppearTextValidation(string text, string desc, bool neg, bool stop)
        {
            return new AppearTextValidation(text, desc, neg, stop);
        }

        internal override ResponseCodeValidation CreateResponseCodeValidation(int responseCode, string desc = "", bool neg = false, bool stop = true)
        {
            return new ResponseCodeValidation(responseCode, desc, neg, stop);
        }

        public override string ToString()
        {
            var result = "Name: " + Name + "\n";
            result += "Desc: " + Desc + "\n";
            result += "Type: " + Type + "\n";

            return Requests.Aggregate(result, (current, pageRequest) => current + (pageRequest + "\n"));
        }
    }
}