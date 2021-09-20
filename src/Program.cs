using System;
using System.Collections.Generic;

namespace badimebot
{
#pragma warning disable CS1591
	public class Program
	{

		static IIrc si;

		static int Main(string[] args)
		{
			Console.WriteLine("Badime Bot v2.00");
		
			//ct = new CountdownTimer();    //default 15 min countdown
			//if (args.Length > 0)
			//	if (!ct.SetCountdown(args[0]))
			//	{
			//		//womp womp parse error
			//		ShowHelp();
			//		return 1;
			//	}

			//if (args.Length > 1)
			//	Title = args[1];

			//ct.Title = Title;
			//ct.MessageEvent += OnAlert;
			//ct.Finished += OnFinished;

			si = new BasicIrc();
			si.Connect("127.0.0.1", "badimebot");
			Console.WriteLine("Connected");
			si.Join("#raspberryheaven");
			Console.WriteLine("Joined channel");
			CountdownTimer animetimer = new CountdownTimer();
			animetimer.MessageEvent += (x, y) =>
			{
				si.SendMessage(y.CountdownMessage);
			};

			si.ChannelMessageReceived += (x, y) =>
			{
				//Only respond to the !badime trigger
				if (IsValidBotCommand(y.Message, "badime"))
					si.SendMessage(string.Format("Time Elapsed {0}", animetimer.GetElapsedTime()));
				//if (y.Message.ToLower().StartsWith("@badime"))
				//	si.SendMessage(string.Format("Time Elapsed {0}", animetimer.GetElapsedTime()));

			};
			si.PrivateMessageReceived += (x, y) =>
			{
				
				if(IsValidBotCommand(y.Message, "badime"))
					si.PrivateMessage(y.From, string.Format("Time Elapsed {0}", animetimer.GetElapsedTime()));
				if (y.From == "hova")
				{
					if (y.Message == "shutdown")
					{
						PrintToConsoleWithColor("Shutdown request received, exiting program", ConsoleColor.Red);
						si.Disconnect();
						animetimer.Stop();
						Environment.Exit(0);
					}
				
					if(y.Message.StartsWith("add"))
                    {
						CountdownItem ci = CountdownTimer.Parse(y.Message);

                        if (ci != CountdownItem.Empty)
                        {
							animetimer.Enqueue(ci);
							si.PrivateMessage(y.From, $"Enqueued {ci.Title} for {ci.Length}");
                        }
                    }
				}
			};
			animetimer.Start();
			Console.WriteLine("Press Enter to quit...");
			Console.ReadLine();
			animetimer.Stop();
			si.Disconnect();
			return 0;
		}

		public static bool IsValidBotCommand(string fullmessage, string expectedcommand)
		{
			if (string.IsNullOrEmpty(fullmessage))
				return false;
			if ((fullmessage[0] == '!' || fullmessage[0] == '@') == false)
				return false;
			if (fullmessage.Substring(1).ToLower().StartsWith(expectedcommand))
				return true;
			return false;
		}

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
			Console.WriteLine("badimebot <00:00:00> <Title of thing to countdown to>");
		}

		public static void OnAlert(object sender, CountDownMessageEventArgs ce)
		{
			Console.WriteLine("[IRC] " + ce.CountdownMessage);
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
			// System.Threading.Thread.Sleep((int)new TimeSpan(0, 20, 0).TotalMilliseconds);
			// si.Quit();
			// Environment.Exit(0);
		}
		/// <summary>
		/// Event fired when IRC has connected successfully
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public static void Connected(object sender, EventArgs e)
		{
			Console.WriteLine("[IRC] Connected Event Fired");
		}

		private static void PrintToConsoleWithColor(string message, System.ConsoleColor color)
		{
			System.ConsoleColor backup = Console.ForegroundColor;
			Console.ForegroundColor = color;
			System.Console.WriteLine(message);
			Console.ForegroundColor = backup;
		}

	}
#pragma warning restore CS1591

}
