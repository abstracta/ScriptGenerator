namespace Abstracta.FiddlerSessionComparer
{
    public class EqualsResult
    {
        public string Key { get; private set; }

        public string Value1 { get; private set; }

        public string Value2 { get; private set; }

        public bool AreEqual { get; private set; }

        public EqualsResult(string key, string value1, string value2)
        {
            Key = key;
            Value1 = value1;
            Value2 = value2;
            AreEqual = value1 == value2;
        }

        public override string ToString()
        {
            return "{ " +
                  "VariableName='" + Key + "' " +
                  "AreEqual='" + AreEqual + "' " +
                  "Value1='" + Value1 + "' " +
                  "Value2='" + Value2 + "' " +
                  "}";
        }
    }
}
