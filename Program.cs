using System;

namespace hovabot
{
    public class Program
    {
        
        static SimpleIrc si ;
        static CountdownTimer ct;
        static int Main(string[] args)
        {
            Console.WriteLine("Badime Bot v1.01");
            ct = new CountdownTimer("00:15:00");    //default 15 min countdown
            if(args.Length > 0)
                ct.SetCountdown(args[0]);
            Console.WriteLine("Startup.  Timer set for {0}", ct.countdowntimer);
            
            ct.MessageEvent += OnAlert;
            ct.Finished += OnFinished;
            
            si = new SimpleIrc();
            si.Connected += Connected;
            si.Connect();
            si.MessageReceived += (x,y) => {
                //Only respond to the !badime trigger
                if(y.Message.StartsWith("!badime"))
                    si.SendMessage(string.Format("Time Elapsed {0}", ct.GetElapsedTime()));
            };
            Console.WriteLine("Press Enter to quit...");
            Console.ReadLine();
            return 0;
        }

        public static void OnAlert(object sender, CountDownMessageEventArgs ce) {
            Console.WriteLine("[IRC] " + ce.CountdownMessage);
            si.SendMessage(ce.CountdownMessage);
        }

        public static void OnFinished(object sender, EventArgs e) {
            // Sleep after the timer is done for 20 minutes (average length of an anime)
            System.Threading.Thread.Sleep((int)new TimeSpan(0, 20, 0).TotalMilliseconds);
            si.Quit();
            Environment.Exit(0);
        }

        public static void Connected(object sender, EventArgs e) {
            Console.WriteLine("[IRC] Connected Event Fired");
            //si.SendMessage("Counting down to Badime in " + ct.countdowntimer.ToString());
            ct.Start();
        }

    }


}
