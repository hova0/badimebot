﻿using System;

namespace hovabot
{
#pragma warning disable CS1591
    public class Program
    {

        static SimpleIrc si;
        static CountdownTimer ct;
        static string Title = "BADIME";
        static int Main(string[] args)
        {
            Console.WriteLine("Badime Bot v1.02");
            if (args.Length == 0)
            {
                ShowHelp();
                return 1;
            }

            ct = new CountdownTimer("00:15:00");    //default 15 min countdown
            if (args.Length > 0)
                if (!ct.SetCountdown(args[0]))
                {
                    //womp womp parse error
                    ShowHelp();
                    return 1;
                }

            Console.WriteLine("Startup.  Timer set for {0}", ct.countdowntimer);
            if (args.Length > 1)
                Title = args[1];

            ct.Title = Title;
            ct.MessageEvent += OnAlert;
            ct.Finished += OnFinished;

            si = new SimpleIrc();
            si.Connected += Connected;
            si.Connect();
            si.MessageReceived += (x, y) =>
            {
                //Only respond to the !badime trigger
                if (y.Message.StartsWith("!badime"))
                    si.SendMessage(string.Format("Time Elapsed {0}", ct.GetElapsedTime()));
                if (y.Message.StartsWith("@badime"))
                    si.SendMessage(string.Format("Time Elapsed {0}", ct.GetElapsedTime()));

            };
            si.PrivateMessageReceived += (x,y) => 
            {
                if(y.Message.ToLower().StartsWith("!badime"))
                    si.SendMessage(y.From, string.Format("Time Elapsed {0}", ct.GetElapsedTime()));
                if(y.From == "hova" && y.Message.StartsWith("!shutdown")) 
                {
                    PrintToConsoleWithColor("Shutdown request received, exiting program", ConsoleColor.Red);
                    si.Disconnect();
                    Environment.Exit(0);
                }
            };
            Console.WriteLine("Press Enter to quit...");
            Console.ReadLine();
            return 0;
        }

        public static void ShowHelp()
        {
            Console.WriteLine("badimebot <00:00:00> <Title of thing to countdown to>");
        }

        public static void OnAlert(object sender, CountDownMessageEventArgs ce)
        {
            //Console.WriteLine("[IRC] " + ce.CountdownMessage);
            si.SendMessage(ce.CountdownMessage);
        }

        /// <summary>
        /// Event Fired when countdown has reached 0
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnFinished(object sender, EventArgs e)
        {
            // Sleep after the timer is done for 20 minutes (average length of an anime)
            System.Threading.Thread.Sleep((int)new TimeSpan(0, 20, 0).TotalMilliseconds);
            si.Quit();
            Environment.Exit(0);
        }
        /// <summary>
        /// Event fired when IRC has connected successfully
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Connected(object sender, EventArgs e)
        {
            Console.WriteLine("[IRC] Connected Event Fired");
            ct.Start();
        }

        private static void PrintToConsoleWithColor(string message, System.ConsoleColor color) {
            System.ConsoleColor backup = Console.ForegroundColor;
            Console.ForegroundColor = color;
            System.Console.WriteLine(message);
            Console.ForegroundColor = backup;
        }

    }
#pragma warning restore CS1591 

}
