namespace Abstracta.Generators.Framework.OSTAGenerator.Validations
{
    internal class ResponseCodeValidation : AbstractGenerator.Validations.ResponseCodeValidation
    {
        internal ResponseCodeValidation(int responseCodeValidation, string errDesc = "", bool negate = false, bool stop = true)
            : base(responseCodeValidation, errDesc, negate, stop)
        {
        }
    }
}