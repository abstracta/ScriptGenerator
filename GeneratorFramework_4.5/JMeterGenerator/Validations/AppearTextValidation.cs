namespace Abstracta.Generators.Framework.JMeterGenerator.Validations
{
    internal class AppearTextValidation : AbstractGenerator.Validations.AppearTextValidation
    {
        internal AppearTextValidation(string text, string errDesc, bool negate, bool stop) : base(text, errDesc, negate, stop)
        {
        }

        public override string ToString()
        {
            var titleName = (NegateValidation)
                                ? "Appear Text Validation (negated): " + TextToValidate
                                : "Appear Text Validation: " + TextToValidate;
            var assertionsTestType = (NegateValidation) ? "6" : "2";

            return ValidationHelper.CreateValidation(titleName, TextToValidate, assertionsTestType, ErrorDescription);
        }
    }
}