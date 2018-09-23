
using System;
using System.Collections;
using System.Threading;

namespace hovabot
{
    /// <summary>
    /// I cannot find a decent IRC library for .net core
    /// So I used Meeby which has been stable for YEARS in old .net framework
    /// </summary>
    public class SimpleIrc
    {
        
        static readonly string channel = "#raspberryheaven";
        public static Meebey.SmartIrc4net.IrcClient _ircclient;

        public event EventHandler Connected;

        public event EventHandler<MessageEvent> MessageReceived;


        public System.Threading.Thread t {get;set;}
        public void Connect() {
            ThreadStart ts = new ThreadStart(ThreadConnect);
            t = new Thread(ts);
            t.Start();
        }

        public void ThreadConnect()
        {
            Meebey.SmartIrc4net.IrcClient c = new Meebey.SmartIrc4net.IrcClient();
            if (_ircclient == null)
            {
                _ircclient = c;
            }
            else
            {
                c = _ircclient;
            }
            c.SendDelay = 250;
            c.OnError += (x, y) =>
            {
                PrintToConsoleWithColor(y.ErrorMessage, ConsoleColor.Red);
            };
            c.OnRawMessage += (x, y) =>
            {
                PrintToConsoleWithColor("   [RAW]   " + y.Data.RawMessage, System.ConsoleColor.DarkGray);
            };
            c.OnConnecting += (x, y) =>
            {
                PrintToConsoleWithColor("[Connecting] ...", ConsoleColor.DarkYellow);
            };
            c.OnConnectionError += (x, y) => { PrintToConsoleWithColor("Connection Error" + x.ToString(), ConsoleColor.Red); };
            c.OnJoin += (x, y) =>
            {
                PrintToConsoleWithColor("[JOIN] " + y.Channel, ConsoleColor.DarkGreen);
                if (Connected != null)
                    Connected.Invoke(this, EventArgs.Empty);
            };
            c.OnConnected += (x, y) => { PrintToConsoleWithColor("[Connected]", ConsoleColor.DarkGreen); /* _connected = true; */ };
            //Event relay.  Simplifies the interface from irc library to other user code.  
            c.OnChannelMessage += (x,y) => {
                if(y.Data.Channel == "#raspberryheaven" && MessageReceived != null) {
                    MessageEvent me = new MessageEvent() { From = y.Data.From, Message = y.Data.Message};
                    MessageReceived.Invoke(this, me);
                }
            };
            try
            {
                c.Connect("irc.synirc.net", 6667);
            }
            catch (Exception e)
            {
                PrintToConsoleWithColor("Connection Error: " + e.Message, ConsoleColor.Red);
                return;
            }
            try
            {
                c.Login("Badimebot", "Bad Anime Bot");
                c.RfcJoin(channel);
                c.ListenOnce(true);
                System.Threading.Thread.Sleep(500);
                // There was some additional code here to take some actions after joining, but it was removed

                c.Listen(true);//BLOCKING

            }
            catch (Exception e)
            {
                PrintToConsoleWithColor("Login Error: " + e.Message, ConsoleColor.Red);
                return;
            }

            // Note:  We should not really ever get here 

            if (c.IsConnected)
                c.Disconnect();
            Console.WriteLine("Done");
            return;
        }

        private void PrintToConsoleWithColor(string message, System.ConsoleColor color) {
            System.ConsoleColor backup = Console.ForegroundColor;
            Console.ForegroundColor = color;
            System.Console.WriteLine(message);
            Console.ForegroundColor = backup;
        }

        public void SendMessage(string msg)
        {
            _ircclient.SendMessage(Meebey.SmartIrc4net.SendType.Message, channel, msg);
        }

        public void Quit()
        {
            _ircclient.RfcQuit();
            _ircclient.Disconnect();
        }

        public class MessageEvent : EventArgs {
            public string From;
            public string Message;


        }
  
    }
}