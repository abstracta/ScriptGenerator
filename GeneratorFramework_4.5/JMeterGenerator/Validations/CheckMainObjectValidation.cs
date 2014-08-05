namespace Abstracta.Generators.Framework.JMeterGenerator.Validations
{
    internal class CheckMainObjectValidation : AbstractGenerator.Validations.CheckMainObjectValidation
    {
        internal CheckMainObjectValidation(string objetctName) : base(objetctName)
        {
        }

        public override string ToString()
        {
            return ValidationHelper.CreateValidation("Check main object: " + ObjectName, ObjectName, "2");
        }
    }
}