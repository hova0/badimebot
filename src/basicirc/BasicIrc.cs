using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using System.Threading;
using badimebot;
using Microsoft.VisualBasic;
using System.Runtime.CompilerServices;
/// <summary>
/// Simple class for one server, one channel IRC stuff.
/// Only supports one server.
/// Only supports one channel.
/// Can send messages to the channel and users.
/// </summary>
public class BasicIrc : IIrc, IDisposable
{
    // Events
    public event EventHandler<MessageArgs> ChannelMessageReceived;
    public event EventHandler<MessageArgs> PrivateMessageReceived;
    public event EventHandler Disconnected;
    public event EventHandler Connected;
    public event EventHandler<string> ChannelJoined;

    public bool IsSSLSecured { get; internal set; }
    System.Threading.Thread _MessagePumpThread;
    System.IO.StreamReader _incomingStream;
    System.IO.StreamWriter _outgoingStream;
    private TcpClient _ircTcpClient;
    public bool IsConnected { get; internal set; }
    private string _currentChannel;
    public string Nick { get; set; }
    private CancellationToken _ct = CancellationToken.None;
    private CancellationTokenSource _cts;
    public volatile bool _LookingforServerResponse = false;
    public volatile int _LookingforServerResponseCode = 0;
    public System.Threading.ManualResetEventSlim _LookingforServerResponseEvent = new ManualResetEventSlim();
    private string _server = "";
    private ManualResetEvent _server_Connected = new ManualResetEvent(false);
    private ConnectionState _state = ConnectionState.Disconnected;
    // Lock reference used for accessing _state variable.  For state changes always lock!
    private object _statelock = new object();
    // Log to file irc traffic (in raw form mostly)
    private System.IO.StreamWriter irclog;
    
    // These may be specific based on irc server?
    private const int SERVER_MOTD_FINISHED = 376;
    private const int SERVER_JOIN_FINISHED = 366;
    private const int SERVER_JOIN_NAMES_LIST = 353;
    private const int SERVER_JOIN_TOPIC = 332;

    private string _currentserver;
    private string _currentnick;
    /// <summary>
    /// Connects to an IRC server.  Will attempt SSL and fallback to unencrypted
    /// </summary>
    /// <param name="server"></param>
    /// <param name="nick"></param>
    public void Connect(string server, string nick)
    {
        _server_Connected.Reset();
        lock (_statelock)
        {
            if (_state != ConnectionState.Disconnected)
                return;
            _state = ConnectionState.Connecting;
        }
        _currentserver = server;
        _currentnick = nick;
        _cts = new CancellationTokenSource();
        string irclogfilename = GetIrcLogfilename();
        irclog = new System.IO.StreamWriter(irclogfilename);
        irclog.AutoFlush = true;
        Console.WriteLine($"Logging to {irclogfilename}");
        Console.WriteLine($"Connecting to {server} with nick {nick}");
        _server = server;
        _ct = _cts.Token;
        try
        {
            TcpClient t = new TcpClient(server, 6697);
            SslStream s = new SslStream(t.GetStream(), false, LogandIgnoreSSLCert);
            s.AuthenticateAsClient(server);
            _incomingStream = new System.IO.StreamReader(s);
            _outgoingStream = new System.IO.StreamWriter(s);
            _ircTcpClient = t;
            irclog.WriteLine($"Connected to {server}:6697");
        }
        catch (Exception e)
        {
            Program.ConsoleError($"Could not connect to {server}", e);
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
        _server_Connected.WaitOne(5000);
        SetNick(Nick);
        lock (_statelock)
        {
            _state = ConnectionState.Connected;
        }
        Connected?.Invoke(this, EventArgs.Empty);
    }
    private bool LogandIgnoreSSLCert(object o, System.Security.Cryptography.X509Certificates.X509Certificate? cert,
        System.Security.Cryptography.X509Certificates.X509Chain? chain,
        SslPolicyErrors sslpolicy)
    {
        System.IO.File.WriteAllBytes($"{_server}.cer", cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert));
        Console.WriteLine($"Wrote certificate to {_server}.cer");
        return true;
    }
    private void SetNick(string nick)
    {
        //NOTE:   When testing on an unrealircd server, we don't get the line 
        WriteToServerStreamDangerous($"USER {nick} {nick} {nick} :{nick}");
        //WriteToServerStreamDangerous($"AUTH {nick}:{nick.GetHashCode()}");
        WriteToServerStreamDangerous($"NICK {nick}");

    }

    // Thread Pump for IRC messages
    private void _MessagePump()
    {
        ServerNumericReply snr = new ServerNumericReply();
        ChatMessage msg = new ChatMessage();
        msg.Nick = Nick;
        snr.Nick = Nick;
        try
        {
            while (_ircTcpClient.Connected && _incomingStream.EndOfStream == false)
            {
                if (_ct.IsCancellationRequested)
                    break;
                string _incoming = null;
                try
                {
                    _incoming = _incomingStream.ReadLine();
                }
                catch (Exception e)
                {
                    HandleNetworkFault(e);
                }
                if (_incoming == null)
                {
                    badimebot.Program.ConsoleError("Read blank line from irc server");
                    break;  // end?
                }
                irclog.WriteLine(_incoming);

                // Parse and fire events
                if (_incoming.StartsWith("PING"))
                    WriteToServerStream($"PONG {_incoming.Substring(5)}");
                else
                if (snr.TryParse(_incoming))
                {
                    if (snr.ReplyCode == SERVER_MOTD_FINISHED)
                    {
                        _server_Connected.Set();    // Set flag to allow connection to continue
                    }
                    if (_LookingforServerResponse && snr.ReplyCode == _LookingforServerResponseCode)
                    {
                        _LookingforServerResponse = false;
                        _LookingforServerResponseEvent.Set();
                    }
                }
                else
                if (_incoming.Contains("PRIVMSG") && msg.TryParse(_incoming))
                {
                    if (msg.Channel == null)
                        this.PrivateMessageReceived?.Invoke(this, new MessageArgs() { From = msg.From, Message = msg.Message });
                    else
                        this.ChannelMessageReceived?.Invoke(this, new MessageArgs() { From = msg.From, Message = msg.Message, Channel = msg.Channel });
                }
                else
                {
                    // ??
                    badimebot.Program.ConsoleError($"Unknown server message: {_incoming}");
                }

                System.Threading.Thread.Sleep(100);
                System.Threading.Thread.Yield();
            }
        }catch(Exception ioe)
        {
            Console.WriteLine("MessagePump: " + ioe.Message);
            HandleNetworkFault(ioe);
        }

        Console.WriteLine("Message pump finished");
    }

    private void WriteToServerStream(string msg)
    {
        if (_state == ConnectionState.Disconnected)
            return;
        if (_outgoingStream != null && _ircTcpClient.Connected)
        {
            irclog?.WriteLine(msg);
            try
            {
                lock (_outgoingStream)
                {
                    _outgoingStream.WriteLine(msg);
                }
            }catch(Exception e)
            {
                HandleNetworkFault(e);
            }
        }
    }
    private void WriteToServerStreamDangerous(string msg)
    {
        irclog?.WriteLine(msg);
        lock (_outgoingStream)
        {
            _outgoingStream.WriteLine(msg);
        }
    }

    private void Reconnect()
    {
        if (_state != ConnectionState.Disconnected)
            return;
        for (int i = 0; i < 30; i++)
        {
            try
            {
                Connect(_server, Nick);
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{i:00}] Failure re-connecting: " + e.Message);
                if (_state == ConnectionState.Connecting)
                    _state = ConnectionState.Disconnected;
                
                Thread.Sleep(1000);
            }
            if(i == 29)
            {
                Console.WriteLine("Could not reconnect after 30 tries.  Aborting program");
                throw new Exception("Fatal reconnect error");
            }
        }
        if (!string.IsNullOrEmpty(_currentChannel)) 
            Join(_currentChannel);
    }

    /// <summary>
    /// Disconnects from the IRC server
    /// </summary>
    /// <param name="quitmessage"></param>
    public void Disconnect(string quitmessage = "")
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BasicIrc));
        _cts.Cancel();
        Console.WriteLine("DISCONNECT");
        if (quitmessage == null)
            quitmessage = "";
        quitmessage = "QUIT :" + quitmessage;
        try
        {
            WriteToServerStream(quitmessage);
        }
        catch (Exception e)
        {
            Program.ConsoleError("Could not QUIT server", e);
        }
        _ircTcpClient.Close();
        _state = ConnectionState.Disconnected;
        _ircTcpClient = null;
    }
    private bool _disposed = false;
    /// <summary>
    /// Cleans up resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (IsConnected)
                Disconnect();

            irclog.Close();
            irclog.Dispose();
            _outgoingStream?.Dispose();
            _ircTcpClient?.Close();
            _ircTcpClient?.Dispose();
            _disposed = true;
        }
    }
    /// <summary>
    /// Joins an IRC channel.   Only one channel is supported at a time. 
    /// </summary>
    /// <param name="channel"></param>
    public void Join(string channel)
    {
        // Leave current channel
        lock (_statelock)
        {
            if (!String.IsNullOrEmpty(_currentChannel) && _state == ConnectionState.ChannelJoined)
            {
                WriteToServerStream($"PART {_currentChannel}");
                _state = ConnectionState.Connected;
            }
        }

        _LookingforServerResponseEvent.Reset();
        if (!channel.StartsWith("#"))
            channel = "#" + channel;
        _LookingforServerResponse = true;
        _LookingforServerResponseCode = SERVER_JOIN_FINISHED;
        WriteToServerStream($"JOIN {channel}");
        _LookingforServerResponseEvent.Wait();

        // Successfully joined
        _currentChannel = channel;
        lock (_statelock)
        {
            _state = ConnectionState.ChannelJoined;
        }
        ChannelJoined?.Invoke(this, channel);
    }



    /// <summary>
    /// Sends a private message to an IRC user
    /// </summary>
    /// <param name="nick"></param>
    /// <param name="message"></param>
    public void PrivateMessage(string nick, string message)
    {
        WriteToServerStream($"PRIVMSG {nick} :{message}");
    }
    /// <summary>
    /// Sends a message to the current channel
    /// </summary>
    /// <param name="message"></param>
    public void SendMessage(string message)
    {
        lock (_statelock)
        {
            if (_state != ConnectionState.ChannelJoined)
                return;
        WriteToServerStream($"PRIVMSG {this._currentChannel} :{message}");
        }
    }

    /// <summary>
    /// Gets a filename to log irc raw to
    /// </summary>
    /// <returns></returns>
    private string GetIrcLogfilename()
    {
        DateTime n = DateTime.Now;
        string filename = System.IO.Path.Combine(Environment.CurrentDirectory, $"{n.Year}{n.Month:00}{n.Day:00}_{n.Hour:00}{n.Minute:00}{n.Second:00}_IrcLog.txt");
        if (System.IO.File.Exists(filename) == false)
            return filename;
        for (int i = 0; i < 100; i++)
        {
            System.Threading.Thread.Sleep(1000);
            n = DateTime.Now;
            filename = System.IO.Path.Combine(Environment.CurrentDirectory, $"{n.Year}{n.Month:00}{n.Day:00}_{n.Hour:00}{n.Minute:00}{n.Second:00}_IrcLog{i}.txt");
            if (System.IO.File.Exists(filename) == false)
                return filename;
        }
        throw new Exception("Exhausted potential files.  Reached max limit of 99 files");
    }


    private void HandleNetworkFault(Exception e)
    {
        badimebot.Program.ConsoleError("Error Reading from server", e);
        System.Threading.Thread.Sleep(50);
        if (_ircTcpClient.Connected == false)
        {
            lock (_statelock)
            {
                _state = ConnectionState.Disconnected;
            }
            Disconnected?.Invoke(this, EventArgs.Empty);
            Reconnect();
        }
        else
        {
            badimebot.Program.ConsoleError("Still connected on tcp stream", null);
              // Connected but got a weird error, bail out
        }
    }

}

public enum ConnectionState
{
    /// <summary>
    /// Not connected
    /// </summary>
    Disconnected,
    /// <summary>
    /// In the process of connecting.  Cannot send messages or join channels yet.
    /// </summary>
    Connecting,
    /// <summary>
    /// Connected to irc server but not joined to a channel yet.  Cannot send messages
    /// </summary>
    Connected,
    /// <summary>
    /// Fully connected.   Can send messages.
    /// </summary>
    ChannelJoined
}
