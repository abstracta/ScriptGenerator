namespace Abstracta.Generators.Framework.AbstractGenerator.Validations
{
    internal class CheckMainObjectValidation : AbstractValidation
    {
        internal string ObjectName { get; private set; }

        internal CheckMainObjectValidation(string objetctName)
        {
            ObjectName = objetctName;
        }

        public override string ToString()
        {
            return "CheckMainObject: " + ObjectName;
        }
    }
}