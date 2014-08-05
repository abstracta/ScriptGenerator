namespace Abstracta.Generators.Framework.JMeterGenerator.Validations
{
    internal class DefaultValidation : AbstractGenerator.Validations.DefaultValidation
    {
        public override string ToString()
        {
            return ValidationHelper.CreateValidation("Response Assert - ", "TEXT TO VALIDATE", "2");
        }
    }
}