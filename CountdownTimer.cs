using System;

namespace hovabot
{
    public class CountdownTimer
    {

        public System.TimeSpan countdowntimer;
        System.TimeSpan lengthtimer;
        //System.DateTime epoch;
        System.Threading.CancellationToken ct;
        System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
        System.Threading.Thread timerthread;
        public event EventHandler<CountDownMessageEventArgs> MessageEvent;
        public event EventHandler Finished;

        //public bool Finished { get; set; }

        private TimeSpan[] Alerts = new TimeSpan[] {
            new TimeSpan(0,15,0),
            new TimeSpan(0,10,0),
            new TimeSpan(0,5,0),
            new TimeSpan(0,3,0),
            new TimeSpan(0,1,0),
            new TimeSpan(0,0,30),
            new TimeSpan(0,0,15),
            new TimeSpan(0,0,10),
            new TimeSpan(0,0,5),
            new TimeSpan(0,0,4),
            new TimeSpan(0,0,3),
            new TimeSpan(0,0,2),
            new TimeSpan(0,0,1)
        };

        public void SetCountdown(string countdown) {
            try {
            countdowntimer = ParseTime(countdown);
            }catch(Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public void OnMessageEvent(string countdownmessage)
        {
            if (MessageEvent != null)
                MessageEvent.Invoke(this, new CountDownMessageEventArgs() { CountdownMessage = countdownmessage });
        }

        public CountdownTimer(string timerstring)
        {

            countdowntimer = ParseTime(timerstring);
            lengthtimer = new TimeSpan(0, 30, 0);   //defautl length of an anime
            //epoch = DateTime.Now.Add(countdowntimer);

        }

        public CountdownTimer(string timerstring, string lengthstring)
        {
            countdowntimer = ParseTime(timerstring);
            lengthtimer = ParseTime(lengthstring);
        }

        public void Start()
        {
            if (timerthread != null)
                return;
            if (ct == null)
                ct = cts.Token;
            System.Threading.ThreadStart ts = new System.Threading.ThreadStart(ThreadStarter);

            this.timerthread = new System.Threading.Thread(ts);
            this.timerthread.Start();
            OnMessageEvent("Counting down to BADIME in " + countdowntimer.ToString());

        }

        public void ThreadStarter()
        {
            DateTime endtime =  DateTime.Now.Add(countdowntimer);;
            TimeSpan lastalert = new TimeSpan(999, 999, 999);
            for (int i = 0; i < Alerts.Length; i++)
            {
                if (countdowntimer < Alerts[i])
                {
                    lastalert = Alerts[i];
                }
            }
            Console.WriteLine("Time is now " + DateTime.Now.ToString("hh:mm:ss"));
            Console.WriteLine("Timer Started for " + countdowntimer.ToString() + " counting down to " + endtime.ToString("hh:mm:ss"));
            Console.WriteLine("Last Alert set to " + lastalert.ToString());
            while (DateTime.Now < endtime && !ct.IsCancellationRequested)
            {
                System.Threading.Thread.Sleep(100);
                DateTime _now = DateTime.Now;
                for (int i = 0; i < Alerts.Length; i++)
                {
                    if (_now > endtime.Subtract(Alerts[i]) && Alerts[i] < lastalert)
                    {
                        Console.WriteLine($"Alert triggered: {_now.ToString("hh:mm:ss")} > {endtime.Subtract(Alerts[i]).ToString("hh:mm:ss")}");
                        //Console.WriteLine($"Last Alert = {lastalert}");
                        if(Alerts[i].TotalSeconds >= 60)
                            OnMessageEvent(" /!\\ BADIME IN  " + Alerts[i].ToString() + $" /!\\");
                        if(Alerts[i].TotalSeconds < 60)
                            OnMessageEvent("" + Alerts[i].Seconds.ToString() + $"");
                        lastalert = Alerts[i];
                        break;
                    }
                }
            }
            OnMessageEvent(" /!\\ BADIME /!\\");
            Console.WriteLine("Timer Finished");
            if(Finished != null)
                Finished.Invoke(this, EventArgs.Empty);
            //Finished = true;
        }

        public void Stop()
        {
            cts.Cancel();
            timerthread.Abort();
        }

        public static System.TimeSpan ParseTime(string timerstring)
        {

            var timeslices = timerstring.Split(':');
            int h, m, s = 0;
            if (timeslices.Length == 3
            && Int32.TryParse(timeslices[0], out h)
            && Int32.TryParse(timeslices[1], out m)
            && Int32.TryParse(timeslices[2], out s))
                return new TimeSpan(h, m, s);
            if (timeslices.Length == 2
            && Int32.TryParse(timeslices[0], out m)
            && Int32.TryParse(timeslices[1], out s)
             )
                return new TimeSpan(0, m, s);

            throw new Exception("Unable to parse time string of \"" + timerstring + "\"");
        }

    }
    public class CountDownMessageEventArgs : EventArgs
    {
        public string CountdownMessage;

        //public CountDownMessageEventArgs()
    }

}
