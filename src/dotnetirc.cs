// using System;
// using SimpleIRCLib;

// public class dotnetirc : IIrc
// {
// 	public event EventHandler<MessageArgs> ChannelMessageReceived;
// 	public event EventHandler<MessageArgs> PrivateMessageReceived;
// 	public SimpleIRCLib.IrcClient _client;
// 	public SimpleIRCLib.SimpleIRC _irc;

// 	public string CurrentChannel {get;internal set;}

// 	public dotnetirc()
// 	{
// 		_irc = new SimpleIRC();
// 	}

// 	public bool Connected { get; internal set; }
// 	private bool _connectionFailure = false;

// 	public void Connect(string server, string nick)
// 	{
// 		// Set up irc client
// 		Console.WriteLine($"Connecting to server {server}");
// 		_irc.SetupIrc(server, nick, "#raspberryheaven");

// 		//event handlers
// 		_irc.IrcClient.OnMessageReceived += _OnMessageReceived;
		
// 		_irc.StartClient();
// 		// Event handlers
// 		_client = _irc.IrcClient;
		
		
// 		Console.WriteLine($"IRC Client Connected status {_irc.IsClientRunning()}");

// 	// end of Connect
// 	}

// 	public void Disconnect()
// 	{
// 		if (Connected)
// 			_irc.StopClient();
// 	}

// 	public void Dispose()
// 	{
// 		_irc.StopClient();
		
// 	}

// 	public void Join(string channel)
// 	{
// 		if(channel.StartsWith("#"))
// 			channel = channel.Substring(1);

// 		if(Connected) {
// 			if(!string.IsNullOrEmpty(CurrentChannel) )
// 				_irc.SendRawMessage($"PART #{CurrentChannel}");
// 			Console.WriteLine($"Joining channel {channel}");
// 			_irc.SendRawMessage($"JOIN #{channel}");
// 			CurrentChannel = channel;
// 		}
// 	}

// 	private void _OnMessageReceived(object sender, IrcReceivedEventArgs args) {

// 		string channel = args.Channel;
// 		string user =args.User;
// 		string msg = args.Message;
// 		if(string.IsNullOrEmpty(channel)) {
// 			this.PrivateMessageReceived?.Invoke(this, new MessageArgs(){ 
// 				From=user,
// 				Message=msg
// 			});
// 		} else {
// 			this.ChannelMessageReceived?.Invoke(this, new MessageArgs() {
// 				From=user,
// 				Message=msg,
// 				Channel=channel
// 			});
// 		}
// 	}
// 	public void PrivateMessage(string nick, string message)
// 	{
// 		_irc.SendRawMessage($"PRIVMSG {nick} :{message}");
// 	}

// 	public void SendMessage(string message)
// 	{
// 		_irc.SendRawMessage($"PRIVMSG #{CurrentChannel} :{message}");
// 	}
// }