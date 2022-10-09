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
            } 
        }

        private static void Si_PrivateMessageReceived(object x, MessageArgs y)
        {
            if (IsValidBotCommand(y.Message, "badime"))
                irc.PrivateMessage(y.From, string.Format("Time Elapsed {0}", animetimer.GetElapsedTime()));
            if (y.From == "hova")
            {
                string firstword = y.Message.Trim();
                if(firstword.IndexOf(' ') > 0)
                    firstword = firstword.Substring(0, firstword.IndexOf(' '));
                Console.WriteLine($"**DEBUG** '{firstword}'");

                if (firstword == "shutdown")
                {
                    PrintToConsoleWithColor("Shutdown request received, exiting program", ConsoleColor.Red);
                    irc.Disconnect($"{y.From} told me to quit");
                    animetimer.Stop();
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
            }
        }

        private static void Si_ChannelMessageReceived(object x, MessageArgs y)
        {
            //Only respond to the !badime trigger
            if (IsValidBotCommand(y.Message, "badime"))
                irc.SendMessage(string.Format("Time Elapsed {0}", animetimer.GetElapsedTime()));

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

        [Obsolete]
        private static string[] SeperateIRCCommandArguments(string fullmessage)
        {
            // No longer used, because we refactored some stuff into an irc library

            List<string> finalarguments = new List<string>();
            string[] fullsplit = fullmessage.Split(' ');
            string intermediatearg = "";
            for (int i = 0; i < fullsplit.Length; i++)
            {
                if (fullsplit[i].StartsWith("\""))
                {
                    // Start of quote
                    intermediatearg += fullsplit[i].Replace("\"", "");
                    if (fullsplit[i].EndsWith("\""))
                    {   // Single word fully quoted
                        finalarguments.Add(intermediatearg);
                        intermediatearg = "";
                    }
                    continue;
                }
                if (fullsplit[i].EndsWith("\""))
                {
                    // End of quote
                    intermediatearg += " " + fullsplit[i].Substring(0, fullsplit[i].Length - 1);
                    finalarguments.Add(intermediatearg);
                    intermediatearg = "";
                    continue;
                }
                //Middle of quote
                if (intermediatearg.Length > 0)
                    intermediatearg += " " + fullsplit[i];

                if (intermediatearg.Length == 0)
                {
                    finalarguments.Add(fullsplit[i]);
                }
            }
            if (intermediatearg.Length != 0)
                finalarguments.Add(intermediatearg);

            return finalarguments.ToArray();
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
