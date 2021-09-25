using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using System.Threading;
using badimebot;

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
    public string Nick { get; set; }
    private CancellationToken _ct = CancellationToken.None;
    private CancellationTokenSource _cts;
    public volatile bool _LookingforServerResponse = false;
    public volatile int _LookingforServerResponseCode = 0;
    public System.Threading.ManualResetEventSlim _LookingforServerResponseEvent = new ManualResetEventSlim();
    private string _server = "";
    private volatile bool _server_message_connected = false;

    private System.IO.StreamWriter irclog;

    public void Connect(string server, string nick)
    {
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
#if DEBUG
            //Console.WriteLine("Bypassing SSL");
            //System.Net.ServicePointManager.ServerCertificateValidationCallback = LogandIgnoreSSLCert;
            //    = (a, b, c, d) =>
            //{
            //    System.IO.File.WriteAllBytes($"{server}.cer", b.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert));
            //    Console.WriteLine($"Wrote certificate to {server}.cer");
            //    return true;
            //};
#endif

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
        WriteToServerStream($"USER {nick} {nick} {nick} :{nick}");
        WriteToServerStream($"NICK {nick}");

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
            if (_ct.IsCancellationRequested)
                break;
            string _incoming = null;
            try
            {
                _incoming = _incomingStream.ReadLine();
            }catch(Exception e)
            {
                badimebot.Program.ConsoleError("Error Reading from server", e);
                System.Threading.Thread.Sleep(50);
                break;
            }
            if (_incoming == null)
            {
                break;  // end?
            }
            //Console.WriteLine("RAW " + _incoming);
            irclog.WriteLine(_incoming);
            if (_incoming.StartsWith("PING"))
                WriteToServerStream($"PONG {_incoming.Substring(5)}");

            if (snr.TryParse(_incoming))
            {
                if (snr.ReplyCode == 266)
                {
                    _server_message_connected = true;   // Set volatile flag that indicates connection complete and join established
                }
                if (_LookingforServerResponse && snr.ReplyCode == _LookingforServerResponseCode)
                {
                    _LookingforServerResponse = false;
                    _LookingforServerResponseEvent.Set();
                }
            } else 
            if (_incoming.Contains("PRIVMSG") && msg.TryParse(_incoming))
            {
                if (msg.Channel == null)
                    this.PrivateMessageReceived?.Invoke(this, new MessageArgs() { From = msg.From, Message = msg.Message });
                else
                    this.ChannelMessageReceived?.Invoke(this, new MessageArgs() { From = msg.From, Message = msg.Message, Channel = msg.Channel });
            } else
            {
                // ??
            }

             //System.Threading.Thread.Sleep(20);
            System.Threading.Thread.Yield();
        }

        Console.WriteLine("Message pump finished");
    }

    private void WriteToServerStream(string msg)
    {
        if(_outgoingStream != null && _ircTcpClient.Connected)
        {
            irclog?.WriteLine(msg);
            _outgoingStream.WriteLine(msg);
        }
    }

    private void WaitforServer()
    {
        while (true)
        {
            if (_LookingforServerResponse == false)
                break;
            System.Threading.Thread.Sleep(50);
        }
    }
    /*

   RAW :hova!hova@Clk-E1EA8378 PRIVMSG #raspberryheaven :hello
   RAW :hova!hova@Clk-E1EA8378 PRIVMSG badimebot :hello

    */

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
        } catch (Exception e) {
            Program.ConsoleError("Could not QUIT server", e);
        }
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
            _outgoingStream?.Dispose();
            _ircTcpClient?.Close();
            _ircTcpClient?.Dispose();
            _disposed = true;
        }
    }

    public void Join(string channel)
    {
        _LookingforServerResponseEvent.Reset();
        if (!channel.StartsWith("#"))
            channel = "#" + channel;
        WriteToServerStream($"JOIN {channel}");
        _LookingforServerResponse = true;
        _LookingforServerResponseCode = 366;
        _LookingforServerResponseEvent.Wait();
        //WaitforServer();
        _currentChannel = channel;
    }




    public void PrivateMessage(string nick, string message)
    {
        WriteToServerStream($"PRIVMSG {nick} :{message}");
    }

    public void SendMessage(string message)
    {
        WriteToServerStream($"PRIVMSG {this._currentChannel} :{message}");
    }

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

}