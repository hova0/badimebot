using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using System.Threading;

public class BasicIrc : IIrc, IDisposable
{
	public event EventHandler<MessageArgs> ChannelMessageReceived;
	public event EventHandler<MessageArgs> PrivateMessageReceived;
	public bool IsSSLSecured { get; internal set; }
	System.Threading.Thread _MessagePumpThread;
	//System.Net.Sockets.NetworkStream ircNetworkStream;
	System.IO.StreamReader _incomingStream;
	System.IO.StreamWriter _outgoingStream;
	private TcpClient _ircTcpClient;
	public bool Connected { get; internal set; }
	private string _currentChannel;
	public string Nick {get;set;}
	private CancellationToken _ct = CancellationToken.None;
	private CancellationTokenSource _cts = new CancellationTokenSource();
	public volatile bool _LookingforServerResponse = false;
	public volatile int _LookingforServerResponseCode=0;
	public System.Threading.ManualResetEventSlim _LookingforServerResponseEvent = new ManualResetEventSlim();

	private volatile bool _server_message_connected = false;

	private System.IO.StreamWriter irclog;

	public void Connect(string server, string nick)
	{
		string irclogfilename = GetIrcLogfilename();
		irclog = new System.IO.StreamWriter(irclogfilename);
		irclog.AutoFlush = true;
		Console.WriteLine($"Logging to {irclogfilename}");

		_ct = _cts.Token;
		try
		{
#if DEBUG
			Console.WriteLine("Bypassing SSL");
			System.Net.ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) =>
			{
				return true;
			};
#endif

			TcpClient t = new TcpClient(server, 6697);
			SslStream s = new SslStream(t.GetStream());
			s.AuthenticateAsClient(server);
			_incomingStream = new System.IO.StreamReader(s);
			_outgoingStream = new System.IO.StreamWriter(s);
			_ircTcpClient = t;
			irclog.WriteLine($"Connected to {server}:6697");
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
			Console.WriteLine("Failed to connect via SSL, falling back to plaintext");
			TcpClient t = new TcpClient(server, 6667);

			_incomingStream = new System.IO.StreamReader(t.GetStream());
			_outgoingStream = new System.IO.StreamWriter(t.GetStream());
			_ircTcpClient = t;
			irclog.WriteLine($"Failed to connec via SSL: {e.Message}.");
			irclog.WriteLine($"Connected to {server}:6667");
		}
		_outgoingStream.AutoFlush = true;
		_MessagePumpThread = new System.Threading.Thread(_MessagePump);
		_MessagePumpThread.Start();
		Nick = nick;
		SetNick(Nick);
		while (_server_message_connected == false)
			System.Threading.Thread.Sleep(50);
	}

	private void SetNick(string nick)
	{
		_outgoingStream.WriteLine($"USER {nick} {nick} {nick} :{nick}");
		_outgoingStream.WriteLine($"NICK {nick}");

	}

	// Thread Pump for IRC messages
	private void _MessagePump()
	{
		ServerNumericReply snr = new ServerNumericReply();
		ChatMessage msg = new ChatMessage();
		msg.Nick = Nick;
		snr.Nick = Nick;
		while (_ircTcpClient.Connected && _incomingStream.EndOfStream == false)
		{
			if(_ct.IsCancellationRequested)
				break;
			string _incoming = _incomingStream.ReadLine();
			if(_incoming == null) {
				break;	// end?
			}
			//Console.WriteLine("RAW " + _incoming);
			irclog.WriteLine(_incoming);
			if (_incoming.StartsWith("PING"))
				_outgoingStream.WriteLine($"PONG {_incoming.Substring(5)}");

			if(snr.TryParse(_incoming ) )
			{
				if(snr.ReplyCode == 266) 
				{
					_server_message_connected = true;	// Set volatile flag that indicates connection complete and join established
				}
				if(_LookingforServerResponse && snr.ReplyCode == _LookingforServerResponseCode) {
					_LookingforServerResponse = false;
					_LookingforServerResponseEvent.Set();
				}
			}
			if(_incoming.Contains("PRIVMSG") && msg.TryParse(_incoming) ) {
				if(msg.Channel == null) 
					this.PrivateMessageReceived?.Invoke(this, new MessageArgs() { From=msg.From, Message=msg.Message });
				else 
					this.ChannelMessageReceived?.Invoke(this, new MessageArgs() { From=msg.From, Message=msg.Message, Channel=msg.Channel });
			}

			//System.Threading.Thread.Sleep(20);
			System.Threading.Thread.Yield();
		}
		Console.WriteLine("Message pump finished");
	}

	private void WaitforServer() {
		while(true) {
			if(_LookingforServerResponse == false)
				break;
			System.Threading.Thread.Sleep(50);
		}
	}
 /*

RAW :hova!hova@Clk-E1EA8378 PRIVMSG #raspberryheaven :hello
RAW :hova!hova@Clk-E1EA8378 PRIVMSG badimebot :hello

 */
	
	public void Disconnect()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(BasicIrc));
		_cts.Cancel();
		Console.WriteLine("DISCONNECT");
		_outgoingStream.WriteLine("QUIT");
		_ircTcpClient.Close();
		_ircTcpClient = null;

	}
	private bool _disposed = false;
	public void Dispose()
	{
		if (!_disposed)
		{
			if (Connected)
				Disconnect();

			irclog.Close();
			irclog.Dispose();
			_ircTcpClient?.Close();
		}
	}

	public void Join(string channel)
	{
		_LookingforServerResponseEvent.Reset();
		if (!channel.StartsWith("#"))
			channel = "#" + channel;
		_outgoingStream.WriteLine($"JOIN {channel}");
		_LookingforServerResponse = true;
		_LookingforServerResponseCode = 366;
		_LookingforServerResponseEvent.Wait();
		//WaitforServer();
		_currentChannel = channel;
	}

	


	public void PrivateMessage(string nick, string message)
	{
		_outgoingStream.WriteLine($"PRIVMSG {nick} :{message}");
	}

	public void SendMessage(string message)
	{	
		_outgoingStream.WriteLine($"PRIVMSG {this._currentChannel} :{message}");
	}

	private string GetIrcLogfilename()
    {
		DateTime n = DateTime.Now;
		string filename = System.IO.Path.Combine(Environment.CurrentDirectory, $"{n.Year}{n.Month:00}{n.Day:00}_{n.Hour:00}{n.Minute:00}{n.Second:00}_IrcLog.txt");
		if (System.IO.File.Exists(filename) == false)
			return filename;
		for(int i = 0; i < 100; i++)
        {
			n = DateTime.Now;
			filename = System.IO.Path.Combine(Environment.CurrentDirectory, $"{n.Year}{n.Month:00}{n.Day:00}_{n.Hour:00}{n.Minute:00}{n.Second:00}_IrcLog{i}.txt");
			if (System.IO.File.Exists(filename) == false)
				return filename;
		}
		throw new Exception("Exhausted potential files.  Reached max limit of 99 files in one second");
	}

}