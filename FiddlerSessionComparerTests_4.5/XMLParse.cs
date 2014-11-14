using Abstracta.FiddlerSessionComparer.Content;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abstracta.FiddlerSessionComparerTests
{
    [TestClass]
    public class XMLParse
    {
        [TestMethod]
        [Owner("SDU")]
        public void XmlContentType_01()
        {
            const string xmlString = "<K2BStack xmlns=\"SPU\">" +
                                     "	<K2BStack.K2BStackItem xmlns=\"K2B\">" +
                                     "		<Caption>Home</Caption>" +
                                     "		<Url>impspuhome</Url>" +
                                     "	</K2BStack.K2BStackItem>" +
                                     "	<K2BStack.K2BStackItem xmlns=\"K2B\">" +
                                     "		<Caption>Plan Comercial</Caption>" +
                                     "		<Url>wwspuplancomercial?82sPNmMP8O3TlpdEZk1r8A==</Url>" +
                                     "	</K2BStack.K2BStackItem>" +
                                     "	<K2BStack.K2BStackItem xmlns=\"K2B\">" +
                                     "		<Caption>Datos de Plan</Caption>" +
                                     "		<Url>entitymanagerspuplancomercial?ZkXtkIVyfkuci1dQyGquy+ga2TsISqX52M2Wex3ND4EzzuYK+wUlDHuBPv5blE5O</Url>" +
                                     "	</K2BStack.K2BStackItem>" +
                                     "</K2BStack>";

            var res = XmlContentType.Deserialize(xmlString);

            Assert.AreEqual("K2BStack", res.TagName);
            Assert.AreEqual(1, res.Attributes.Count);
            Assert.AreEqual(3, res.Children.Count);

            Assert.AreEqual("impspuhome", res.Children[0].Children[1].Value);
            Assert.AreEqual("wwspuplancomercial?82sPNmMP8O3TlpdEZk1r8A==", res.Children[1].Children[1].Value);
            Assert.AreEqual("entitymanagerspuplancomercial?ZkXtkIVyfkuci1dQyGquy+ga2TsISqX52M2Wex3ND4EzzuYK+wUlDHuBPv5blE5O", res.Children[2].Children[1].Value);
        }
    }
}
