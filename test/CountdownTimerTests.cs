using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using badimebot;

namespace badimebottests
{
    [TestClass]
    public class CountdownTimerTests
    {
        [TestMethod]
        public void Test_SmallCountdown()
        {
            CountdownTimer ct = new CountdownTimer();
            ct.Enqueue("Test Anime", TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(25));
            ct.MessageEvent += (x, y) =>
            {
                Logger.LogMessage(y.CountdownMessage);
            };
            ct.Start();
            System.Threading.Thread.Sleep(15000);
            ct.Stop();
            
        }

    }
}
