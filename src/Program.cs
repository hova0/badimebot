using System;
using System.Collections.Generic;

namespace badimebot
{
#pragma warning disable CS1591
    public class Program
    {

        static IIrc irc;
        static CountdownTimer animetimer = new CountdownTimer();
        static int Main(string[] args)
        {
            Console.WriteLine("Badime Bot v2.00");
            string server = "127.0.0.1";
            string channel = "#badimebottest";
            string AuthorizedNick = "hova";
            if (args.Length == 2)
            {
                server = args[0];
                channel = args[1];
            }
            if (args.Length >= 3)
                AuthorizedNick = args[2];

            irc = new BasicIrc();
            irc.Connect(server, "badimebot");
            Console.WriteLine("Connected");
            irc.Join(channel);
            Console.WriteLine($"Joined channel {channel}");
            animetimer.MessageEvent += (x, y) =>
            {
                irc.SendMessage(y.CountdownMessage);
            };
            irc.Connected += Irc_Connected;
            irc.ChannelMessageReceived += Si_ChannelMessageReceived;
            irc.PrivateMessageReceived += Si_PrivateMessageReceived;
            irc.Disconnected += Si_Disconnected;

            animetimer.Start();
            Console.WriteLine("Press Enter to quit...");
            Console.ReadLine();
            animetimer.Stop();
            irc.Disconnect("Someone pressed enter on the console");
            return 0;
        }

        private static void Irc_Connected(object sender, EventArgs e)
        {
            if (animetimer.State == CountdownTimer.CountdownState.Paused)
                animetimer.Resume();
        }

        private static void Si_Disconnected(object sender, EventArgs e)
        {
            if(animetimer.State == CountdownTimer.CountdownState.PreCountdown)
            {
                // Pause countdown if we get disconnected so we don't start the while disconnected
                // and thus unable to notify irc that it started.
                animetimer.Pause();
                Console.WriteLine("IRC Disconnected, countdown paused...");
                
            } 
        }

        /// <summary>
        /// Handle private messages
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private static void Si_PrivateMessageReceived(object x, MessageArgs y)
        {
            if (IsValidBotCommand(y.Message, "badime"))
                irc.PrivateMessage(y.From, GetBadimeMessageResponse(animetimer));
            if (y.From == "hova")
            {
                string firstword = y.Message.Trim();
                if(firstword.IndexOf(' ') > 0)
                    firstword = firstword.Substring(0, firstword.IndexOf(' '));

                if (firstword == "shutdown")
                {
                    PrintToConsoleWithColor("Shutdown request received, exiting program", ConsoleColor.Red);
                    irc.Disconnect($"{y.From} told me to quit");
                    animetimer.Stop();
                    irc.Dispose();
                    animetimer.Dispose();
                    Environment.Exit(0);
                }

                if (firstword == "add")
                {
                    CountdownItem ci = CountdownTimer.Parse(y.Message.Trim());

                    if (ci != CountdownItem.Empty)
                    {
                        animetimer.Enqueue(ci);
                        irc.PrivateMessage(y.From, $"Enqueued {ci.Title} for {ci.Length}");
                    }
                }
                if(firstword == "pause" && animetimer.State == CountdownTimer.CountdownState.PreCountdown)
                {
                    animetimer.Pause();
                    irc.PrivateMessage(y.From, $"Paused countdown at " + animetimer.GetElapsedTime());
                }
                if (firstword == "resume" && animetimer.State == CountdownTimer.CountdownState.Paused)
                {
                    animetimer.Resume();
                    irc.PrivateMessage(y.From, $"Resumed countdown from " + animetimer.GetElapsedTime());
                }
                if(firstword == "remove")
                {
                    string title = y.Message.Substring(y.Message.IndexOf(' '));
                    if (animetimer.Remove(title))
                        irc.PrivateMessage(y.From, $"'{title}' removed.");
                    else
                        irc.PrivateMessage(y.From, $"Could not find '{title}'");
                }
                if(firstword == "insert")
                {
                    string regex = @"insert (.*?) for (.*?) in (.*?) at (\d+)";
                    var m = System.Text.RegularExpressions.Regex.Match(y.Message, regex);
                    if (m.Success)
                    {
                        int position = int.Parse(m.Groups[4].Value);
                        CountdownItem ci = CountdownTimer.Parse(y.Message.Trim());
                        animetimer.Insert(ci, position);
                    }
                }
            }
        }

        /// <summary>
        /// Handle channel messages
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private static void Si_ChannelMessageReceived(object x, MessageArgs y)
        {
            //Only respond to the !badime trigger
            if (IsValidBotCommand(y.Message, "badime"))
                irc.SendMessage(GetBadimeMessageResponse(animetimer));

        }

        private static string GetBadimeMessageResponse(CountdownTimer countdowmTimer)
        {
            switch (countdowmTimer.State)
            {
                case CountdownTimer.CountdownState.Idle:
                    return "Nothing currently queued";
                case CountdownTimer.CountdownState.PreCountdown:
                    return string.Format("Counting down to {1} in  {0}", animetimer.GetElapsedTime() * -1, animetimer.CurrentItem.Title);
                case CountdownTimer.CountdownState.Paused:
                    return $"Currently paused at {animetimer.GetElapsedTime()}";
                case CountdownTimer.CountdownState.PostCountdown:
                    return string.Format("Time Elapsed {0}", animetimer.GetElapsedTime());
                default:
                    return string.Format("Time Elapsed {0}", animetimer.GetElapsedTime());
            }
        }

        public static bool IsValidBotCommand(string fullmessage, string expectedcommand)
        {
            if (string.IsNullOrEmpty(fullmessage) || fullmessage.Length <= 1)
                return false;
            if ((fullmessage[0] == '!' || fullmessage[0] == '@') == false)
                return false;
            if (fullmessage.Substring(1).ToLower().StartsWith(expectedcommand))
                return true;
            return false;
        }

        public static void ShowHelp()
        {
            Console.WriteLine("badimebot <irc server>  <irc channel>");
        }

        private static void PrintToConsoleWithColor(string message, System.ConsoleColor color)
        {
            System.ConsoleColor backup = Console.ForegroundColor;
            Console.ForegroundColor = color;
            System.Console.WriteLine(message);
            Console.ForegroundColor = backup;
        }

        public static void ConsoleError(string er, Exception e = null)
        {
            ConsoleColor backup = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(er);
            if (e != null)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Console.ForegroundColor = backup;

        }
#pragma warning restore CS1591

    }
}
