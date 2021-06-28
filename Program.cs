using System;
using System.Collections.Generic;

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
			Console.WriteLine("Badime Bot v1.03");
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
				if (y.Message.ToLower().StartsWith("!badime"))
					si.SendMessage(string.Format("Time Elapsed {0}", ct.GetElapsedTime()));
				if (y.Message.ToLower().StartsWith("@badime"))
					si.SendMessage(string.Format("Time Elapsed {0}", ct.GetElapsedTime()));

			};
			si.PrivateMessageReceived += (x, y) =>
			{
				if (y.Message.ToLower().StartsWith("!badime") || y.Message.ToLower().StartsWith("@badime"))
					si.SendMessage(y.From, string.Format("Time Elapsed {0}", ct.GetElapsedTime()));
				if (y.From == "hova")
				{
					if (IsValidBotCommand(y.Message, "shutdown"))
					{
						PrintToConsoleWithColor("Shutdown request received, exiting program", ConsoleColor.Red);
						si.Disconnect();
						Environment.Exit(0);
					}
					if (IsValidBotCommand(y.Message, "countdown"))
					{
						// Prototype start new countdown
						string[] countdownargs = SeperateIRCCommandArguments(y.Message);

						CountdownTimer newct = new CountdownTimer("00:05:00");
						int offset = 1;
						if (countdownargs[offset] == "to")
							offset++;
						newct.Title = countdownargs[offset];
						offset++;
						if (countdownargs[offset] == "in")
							offset++;
						if (newct.SetCountdown(countdownargs[offset]) == false)
							return; // Exit early, bad countdown arg

						//Stop the old and Start the new
						ct.Stop();
						ct.MessageEvent -= OnAlert;
						ct.Finished -= OnFinished;
						ct = newct;
						ct.MessageEvent += OnAlert;
						ct.Finished += OnFinished;
						ct.Start();
					}
				}
			};
			Console.WriteLine("Press Enter to quit...");
			Console.ReadLine();
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
			ct.Start();
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
