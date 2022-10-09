using System;

public interface IIrc : IDisposable
{
	void Connect(string server, string nick);
	void Disconnect(string quitmessage);
	void Join(string channel);

	event EventHandler<MessageArgs> ChannelMessageReceived;
	event EventHandler<MessageArgs> PrivateMessageReceived;
	event EventHandler Disconnected;
	event EventHandler Connected;
	event EventHandler<string> ChannelJoined;

	void SendMessage(string message);
	void PrivateMessage(string nick, string message);


}

public class MessageArgs : EventArgs 
{
	public string From {get;set;}
	public string Message {get;set;}
	public string Channel {get;set;}
}

