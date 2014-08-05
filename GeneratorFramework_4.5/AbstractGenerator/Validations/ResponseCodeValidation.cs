namespace Abstracta.Generators.Framework.AbstractGenerator.Validations
{
    internal class ResponseCodeValidation : AbstractValidation
    {
        internal int ResponseCodeValidate { get; private set; }

        internal string ErrorDescription { get; private set; }

        internal bool NegateValidation { get; private set; }

        internal bool StopExecution { get; private set; }

        internal ResponseCodeValidation(int responseCodeValidation, string errDesc = "", bool negate = false, bool stop = true)
        {
            ResponseCodeValidate = responseCodeValidation;
            ErrorDescription = errDesc;
            NegateValidation = negate;
            StopExecution = stop;
        }

        public override string ToString()
        {
            var result = "if ( '" + ResponseCodeValidate + "' IS " + (!NegateValidation ? "NOT " : string.Empty) +
                          "the response code)\n";

            result += "\t\t{\n\t\t\tERROR: " + ErrorDescription + "\n" + (StopExecution ? "\t\t\tSTOP Execution;\n" : string.Empty) + "\t\t}";

            return result;
        }
    }
}