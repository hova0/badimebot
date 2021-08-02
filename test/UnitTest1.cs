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
            Assert.IsTrue(snr.TryParse(":test.example.org 266 badimebot :This is a test message", "badimebot"));
            Assert.IsFalse(snr.TryParse("NICK  Test", "badimebot"));
            System.Console.WriteLine("Test");
        }
    }
}
