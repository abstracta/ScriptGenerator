namespace Abstracta.Generators.Framework.AbstractGenerator.Validations
{
    internal class AppearTextValidation : AbstractValidation
    {
        internal string TextToValidate { get; private set; }

        internal string ErrorDescription { get; private set; }

        internal bool NegateValidation { get; private set; }

        internal bool StopExecution { get; private set; }

        internal AppearTextValidation(string text, string errDesc, bool negate, bool stop)
        {
            TextToValidate = text;
            ErrorDescription = errDesc;
            NegateValidation = negate;
            StopExecution = stop;
        }

        public override string ToString()
        {
            var result = "if ( '" + TextToValidate + "' IS " + (!NegateValidation ? "NOT " : string.Empty) +
                         "in last response)\n";

            result += "\t\t{\n\t\t\tERROR: " + ErrorDescription + "\n" + (StopExecution ? "\t\t\tSTOP Execution;\n" : string.Empty) + "\t\t}";

            return result;
        }
    }
}