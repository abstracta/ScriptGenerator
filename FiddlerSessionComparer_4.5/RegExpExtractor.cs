namespace Abstracta.FiddlerSessionComparer
{
    public class RegExpExtractor : ParameterSoure
    {
        public int GroupNumber { get; set; }
        
        public string RegExp { get; set; }

        public RegExpExtractor(int groupNumber, string regExp, string replaceValue, string replaceWith)
            : base(replaceWith, replaceValue)
        {
            GroupNumber = groupNumber;
            RegExp = regExp;
        }

        public override string ToString()
        {
            return "{ " +
                   "GroupNumber='" + GroupNumber + "' " +
                   "RegExp='" + RegExp + "' " +
                   "ReplaceValue='" + ReplaceValue + "' " +
                   "ReplaceWith='" + ReplaceWith + "'" +
                   "}";
        } 
    }
}