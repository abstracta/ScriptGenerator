namespace Abstracta.FiddlerSessionComparer
{
    public class RegExpExtractor
    {
        public int GroupNumber { get; set; }
        
        public string RegExp { get; set; }

        public string ReplaceValue { get; set; }

        public string ReplaceWith { get; set; }

        public RegExpExtractor(int groupNumber, string regExp, string replaceValue, string replaceWith)
        {
            GroupNumber = groupNumber;
            RegExp = regExp;
            ReplaceValue = replaceValue;
            ReplaceWith = replaceWith;
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