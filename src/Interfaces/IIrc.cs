using System;

public interface IIrc : IDisposable
{
	void Connect(string server, string nick);
	void Disconnect();
	void Join(string channel);

	event EventHandler<MessageArgs> ChannelMessageReceived;
	event EventHandler<MessageArgs> PrivateMessageReceived;
	void SendMessage(string message);
	void PrivateMessage(string nick, string message);


}

public class MessageArgs : EventArgs 
{
	public string From {get;set;}
	public string Message {get;set;}
	public string Channel {get;set;}
}

