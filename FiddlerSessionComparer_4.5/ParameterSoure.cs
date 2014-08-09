namespace Abstracta.FiddlerSessionComparer
{
    public class ParameterSoure
    {
        /// <summary>
        /// This is the parameterName
        /// </summary>
        public string ReplaceWith { get; set; }

        /// <summary>
        /// This is the parameterValue
        /// </summary>
        public string ReplaceValue { get; set; }

        public ParameterSoure(string replaceWith, string replaceValue)
        {
            ReplaceWith = replaceWith;
            ReplaceValue = replaceValue;
        }

        public override string ToString()
        {
            return "{ " +
                   "ConstantName='" + ReplaceWith + "' " +
                   "ConstantValue='" + ReplaceValue + "' " +
                   "}";
        } 
    }
}
