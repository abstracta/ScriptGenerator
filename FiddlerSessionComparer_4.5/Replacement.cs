namespace Abstracta.FiddlerSessionComparer
{
    public class Replacement
    {
        /// <summary>
        /// This is the parameterName
        /// </summary>
        public string ReplaceWith { get; set; }

        /// <summary>
        /// This is the parameterValue
        /// </summary>
        public string ReplaceValue { get; set; }

        public Replacement(string replaceWith, string replaceValue)
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
