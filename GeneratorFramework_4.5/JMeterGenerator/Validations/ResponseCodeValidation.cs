namespace Abstracta.Generators.Framework.JMeterGenerator.Validations
{
    internal class ResponseCodeValidation : AbstractGenerator.Validations.ResponseCodeValidation
    {
        internal ResponseCodeValidation(int responseCodeValidation, string errDesc = "", bool negate = false, bool stop = true)
            : base(responseCodeValidation, errDesc, negate, stop)
        {
        }

        public override string ToString()
        {
            var titleName = (NegateValidation)
                               ? "Response Code Validation (negated): " + ResponseCodeValidate
                               : "Response Code Validation: " + ResponseCodeValidate;
            var assertionsTestType = (NegateValidation) ? "12" : "8";

            return ValidationHelper.CreateResponseCodeValidation(titleName, ResponseCodeValidate, assertionsTestType, ErrorDescription);
        }
    }
}