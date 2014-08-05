using Abstracta.FiddlerSessionComparer;
using Abstracta.Generators.Framework.AbstractGenerator;
using Abstracta.Generators.Framework.AbstractGenerator.ParameterExtractor;
using Abstracta.Generators.Framework.AbstractGenerator.Validations;
using Abstracta.Generators.Framework.OSTAGenerator.ParameterExtractor;
using Fiddler;

namespace Abstracta.Generators.Framework.OSTAGenerator
{
    internal class Step : AbstractStep
    {
        protected override AbstractRegExParameter CreateRegExpExtractorToGetRedirectParameters(string varName, string expression, string valueToReplace, string description)
        {
            return new OSTARegExParameter(varName, expression, valueToReplace, description);
        }

        protected override AbstractPageRequest CreatePageRequest(Session primaryRequest, AbstractStep abstractStep, Page page)
        {
            return new PageRequest(primaryRequest, abstractStep, page);
        }

        internal override DefaultValidation CreateDefaultValidation()
        {
            return new Validations.DefaultValidation();
        }

        internal override CheckMainObjectValidation CreateCheckMainObjectValidation(string objectName)
        {
            return new Validations.CheckMainObjectValidation(objectName);
        }

        internal override AppearTextValidation CreateAppearTextValidation(string text, string desc, bool neg, bool stop)
        {
            return new Validations.AppearTextValidation(text, desc, neg, stop);
        }

        internal override ResponseCodeValidation CreateResponseCodeValidation(int responseCode, string desc = "", bool neg = false, bool stop = true)
        {
            return new Validations.ResponseCodeValidation(responseCode, desc, neg, stop);
        }
    }
}