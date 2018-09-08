
using System;
using System.Collections;
using System.Threading;

namespace hovabot
{

    public class SimpleIrc
    {
        //static volatile bool _connected = false;
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
                Console.WriteLine(y.ErrorMessage);
            };
            c.OnRawMessage += (x, y) =>
            {
                System.ConsoleColor backup = Console.ForegroundColor;
                Console.ForegroundColor = System.ConsoleColor.DarkGray;
                Console.WriteLine("   [RAW]   " + y.Data.RawMessage);
                Console.ForegroundColor = backup;
            };
            c.OnConnecting += (x, y) =>
            {
                Console.WriteLine("[Connecting] ...");
            };
            c.OnConnectionError += (x, y) => { Console.WriteLine("Connection Error" + x.ToString()); };
            c.OnJoin += (x, y) =>
            {
                Console.WriteLine("[JOIN] " + y.Channel);
                if (Connected != null)
                    Connected.Invoke(this, EventArgs.Empty);
                else 
                    Console.WriteLine("[WARN] No connected handler!");
            };
            c.OnConnected += (x, y) => { Console.WriteLine("[Connected]"); /* _connected = true; */ };
            //Event relay
            c.OnChannelMessage += (x,y) => {
                if(y.Data.Channel == "#raspberryheaven" && MessageReceived != null) {
                    MessageEvent me = new MessageEvent() { From = y.Data.From, Message = y.Data.Message};
                    MessageReceived.Invoke(this, me);
                }
            };
            try
            {
                c.Connect("irc.synirc.net", 6667);


                //Console.WriteLine("Connection information : " + Newtonsoft.Json.JsonConvert.SerializeObject(c.ServerProperties));
                //Console.WriteLine(" Send Delay" + c.SendDelay.ToString());
                Console.WriteLine(" Server Address" + c.Address.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection Error: " + e.Message);
                return;
            }
            try
            {
                c.Login("Badimebot", "Bad Anime Bot");

                // while (!_connected)
                //     System.Threading.Thread.Sleep(50);
                //System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(TestThread));
                //t.Start();

                c.RfcJoin(channel);
                c.ListenOnce(true);
                System.Threading.Thread.Sleep(500);
                //Console.WriteLine("Did join channel? " + c.IsJoined(channel).ToString());
                //c.SendMessage(Meebey.SmartIrc4net.SendType.Message, channel, "Hello World");

                c.Listen(true);//BLOCKING

                //Console.WriteLine("Connected State: " + c.IsConnected.ToString());
                //c.ListenOnce(true);
            }
            catch (Exception e)
            {
                Console.WriteLine("Login Error: " + e.Message);
                return;
            }
            if (c.IsConnected)
                c.Disconnect();
            Console.WriteLine("Done");
            return;
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