using System;

namespace hovabot
{
    public class Program
    {
        
        static SimpleIrc si ;
        static CountdownTimer ct;
        static int Main(string[] args)
        {
            ct = new CountdownTimer("00:01:10");
            if(args.Length > 0)
                ct.SetCountdown(args[0]);
            Console.WriteLine("Startup.  Timer set for {0}", ct.countdowntimer);
            
            ct.MessageEvent += OnAlert;
            ct.Finished += OnFinished;
            
            si = new SimpleIrc();
            si.Connected += Connected;
            si.Connect();
            si.MessageReceived += (x,y) => {
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
            System.Threading.Thread.Sleep((int)new TimeSpan(0, 15, 0).TotalMilliseconds);
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
