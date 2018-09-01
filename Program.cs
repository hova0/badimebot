using System;

namespace hovabot
{
    public class Program
    {
        
        static SimpleIrc si ;
        static CountdownTimer ct;
        static int Main(string[] args)
        {
            Console.WriteLine("Startup");
            ct = new CountdownTimer("00:01:10");
            if(args.Length > 0)
                ct.SetCountdown(args[0]);
            
            ct.MessageEvent += OnAlert;
            ct.Finished += OnFinished;
            
            si = new SimpleIrc();
            si.Connected += Connected;
            si.Connect();
            
            Console.WriteLine("Press Enter to quit...");
            Console.ReadLine();
            return 0;
        }

        public static void OnAlert(object sender, CountDownMessageEventArgs ce) {
            Console.WriteLine("[IRC] " + ce.CountdownMessage);
            si.SendMessage(ce.CountdownMessage);
        }

        public static void OnFinished(object sender, EventArgs e) {
            si.Quit();
            System.Threading.Thread.Sleep(2000);
            Environment.Exit(0);
        }

        public static void Connected(object sender, EventArgs e) {
            Console.WriteLine("[IRC] Connected Event Fired");
            //si.SendMessage("Counting down to Badime in " + ct.countdowntimer.ToString());
            ct.Start();
        }

    }


}
