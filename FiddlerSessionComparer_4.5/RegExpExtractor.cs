namespace Abstracta.FiddlerSessionComparer
{
    public class RegExpExtractor : Extractor
    {
        public int GroupNumber { get; set; }
        
        public string RegExp { get; set; }

        public RegExpExtractor(int groupNumber, string regExp)
        {
            GroupNumber = groupNumber;
            RegExp = regExp;
        }

        public override string ToString()
        {
            return "{ " +
                   "GroupNumber='" + GroupNumber + "' " +
                   "RegExp='" + RegExp + "' " +
                   ////"ReplaceValue='" + ReplaceValue + "' " +
                   ////"ReplaceWith='" + ReplaceWith + "'" +
                   "}";
        } 
    }

    public class Extractor
    {
    }
}