using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using badimebot;
using System.Threading;

namespace badimebottests
{
    [TestClass]
    public class CountdownTimerTests
    {
        [TestMethod]
        public void Test_SmallCountdown()
        {
            CountdownTimer ct = new CountdownTimer();
            ct.Enqueue(new CountdownItem()
            {
                Title = "Test1",
                Length = new TimeSpan(0, 0, 0, 10),
                PreCountdown = new TimeSpan(0, 0, 0, 5 )
            });
            DateTime start = DateTime.Now;

            ct.Start();
            Thread.Sleep(3000);
            Assert.IsTrue(ct.State == CountdownTimer.CountdownState.PreCountdown);
            Thread.Sleep(3000);
            Assert.IsTrue(ct.State == CountdownTimer.CountdownState.PostCountdown);
            
            ct.Dispose();
        }

        [TestMethod]
        public void Test_Pause()
        {
            CountdownTimer ct = new CountdownTimer();
            ct.Enqueue(new CountdownItem()
            {
                Title = "Test1",
                Length = new TimeSpan(0, 0, 0, 3),
                PreCountdown = new TimeSpan(0, 0, 0, 5)
            });
            DateTime start = DateTime.Now;

            ct.Start();
            Thread.Sleep(3000);
            ct.Pause();
            Assert.IsTrue(ct.State == CountdownTimer.CountdownState.Paused);
            Thread.Sleep(3000);
            Assert.IsTrue(ct.State == CountdownTimer.CountdownState.Paused);
            ct.Resume();
            Assert.IsTrue(ct.State == CountdownTimer.CountdownState.PreCountdown);
            Thread.Sleep(3000);
            Assert.IsTrue(ct.State == CountdownTimer.CountdownState.PostCountdown);
            ct.Dispose();
        }


    }
}
