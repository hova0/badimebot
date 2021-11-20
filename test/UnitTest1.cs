using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace badimebottests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            ServerNumericReply snr = new ServerNumericReply();
            snr.Nick = "badimebot";
            Assert.IsTrue(snr.TryParse(":test.example.org 266 badimebot :This is a test message"), "Could not parse regular server message");
            
        }
    }
}
