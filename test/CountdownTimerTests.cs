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

        [TestMethod]
        public void Test_Insertion_Removal()
        {
            CountdownTimer ct = new CountdownTimer();
            ct.Enqueue("Test1", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            ct.Enqueue("Test3", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            ct.Enqueue("Test5", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            ct.Insert(new CountdownItem() { Title = "Test2", Length = TimeSpan.FromSeconds(30), PreCountdown = TimeSpan.FromSeconds(30) }, 1);
            ct.Insert(new CountdownItem() { Title = "Test4", Length = TimeSpan.FromSeconds(30), PreCountdown = TimeSpan.FromSeconds(30) }, 3);

            for(int i = 1; i <= 5; i++)
            {
                string title = "Test" + i.ToString();
                Assert.IsTrue(ct.CountdownList[i-1].Title == title);
            }
            Assert.IsTrue(ct.Remove("Test3"));
            foreach(var item in ct.CountdownList)
            {
                Assert.IsFalse(item.Title == "Test3");
            }
            Assert.IsFalse(ct.Remove("Test3"));
        }

        [TestMethod]
        public void Test_Parsing()
        {
            var item = CountdownTimer.Parse("add Pui Pui Molcar for 15:00 in 05:00");
            Assert.AreNotEqual(item, CountdownItem.Empty);
            Assert.IsTrue(item.Title == "Pui Pui Molcar");
            Assert.IsTrue(item.Length == TimeSpan.FromMinutes(15));
            Assert.IsTrue(item.PreCountdown == TimeSpan.FromMinutes(5));

            item = CountdownTimer.Parse("insert Dragonball Z for 15:00 in 20:00 at 3");
            Assert.IsTrue(item.Title == "Dragonball Z");
            Assert.IsTrue(item.Length == TimeSpan.FromMinutes(15));
            Assert.IsTrue(item.PreCountdown == TimeSpan.FromMinutes(20));

        }
    }
}
