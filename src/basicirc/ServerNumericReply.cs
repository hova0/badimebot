using System.Collections.Generic;
using badimebot;

public interface IServerMessage
{

	bool TryParse(string rawmsg);
	string Nick { get; set; }
}

public class ServerNumericReply : IServerMessage
{
	private static string _server_identifier;
	public string Server { get; internal set; }
	public int ReplyCode { get; internal set; }
	public string To { get; internal set; }
	public string Msg { get; internal set; }
	public string Nick { get; set; }

	public ServerNumericReply()
	{
	}
	public bool TryParse(string rawmsg)
	{
		if(string.IsNullOrEmpty(Nick))
			throw new System.Exception("Nick must be set before calling TryParse");

		var wordenum = rawmsg.GetWords().GetEnumerator();
		List<string> sections = new List<string>();

		if (wordenum.MoveNext() == false) return false;

		string tmp = wordenum.Current;
		sections.Add(tmp);      // Server
		if (!string.IsNullOrEmpty(_server_identifier) && _server_identifier != tmp)
			return false;

		if (wordenum.MoveNext() == false) return false;
		sections.Add(wordenum.Current); // Reply code

		if (wordenum.MoveNext() == false) return false;
		sections.Add(wordenum.Current); // our nick

		int x;
		if (int.TryParse(sections[1], out x) == false)  // Parse server message number
			return false;
		ReplyCode = x;

		if (sections[2] != Nick)    // Validate it is our nick
			return false;

		if (string.IsNullOrEmpty(_server_identifier))   // Stash server identifier
			_server_identifier = sections[0];

		//passed all checks
		return true;
	}

	


}

public class ChatMessage : IServerMessage {
	public string Nick {get;set;}
	public string From {get;set;}
	public string Channel {get;set;}
	public string Message {get;set;}
	/*

   RAW :hova!hova@Clk-E1EA8378 PRIVMSG #raspberryheaven :hello
   RAW :hova!hova@Clk-E1EA8378 PRIVMSG badimebot :hello

	*/
	public ChatMessage() { }
	public ChatMessage(string rawmsg)
    {
		if (TryParse(rawmsg) == false)
			throw new System.FormatException();
    }

	/// <summary>
	/// Attempts to parse a PRIVMSG into components
	/// </summary>
	/// <param name="rawmsg">The raw IRC Message</param>
	/// <returns>True is it was parsed.  This object will then be populated</returns>
	public bool TryParse(string rawmsg)
	{
		if(string.IsNullOrEmpty(Nick))
			throw new System.Exception("Nick must be set before calling TryParse");
		var words = rawmsg.GetWordsWithIndex().GetEnumerator();
		if(words.MoveNext() == false) return false;
		string rawfrom = words.Current.word;
		if(rawfrom.StartsWith(":") && rawfrom.Contains('!'))
			From = rawfrom.Substring(1, rawfrom.IndexOf('!') - 1);
		if(words.MoveNext() == false) return false;
		if(words.Current.word != "PRIVMSG") return false;
		if(words.MoveNext() == false) return false;
		string rawto = words.Current.word;
		if(rawto.StartsWith('#')) 
			this.Channel = rawto;
		 else if(rawto != this.Nick) 
			return false;
		else 
			this.Channel = null;
		// get message content
		if(words.MoveNext() == false) return false;
		var msg = words.Current;
		if(msg.word.StartsWith(':') == false) return false;
		Message = rawmsg.Substring(msg.index+1);

		return true;
	}
		
	
}