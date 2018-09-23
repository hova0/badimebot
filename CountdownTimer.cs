using System;

namespace hovabot
{
    public class CountdownTimer
    {

        public System.TimeSpan countdowntimer;
        //System.TimeSpan lengthtimer;
        System.DateTime epoch;
        System.Threading.CancellationToken ct;
        System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
        System.Threading.Thread timerthread;
        public event EventHandler<CountDownMessageEventArgs> MessageEvent;
        public event EventHandler Finished;
        public string Title {get;set;}
        //public bool Finished { get; set; }

        private TimeSpan[] Alerts = new TimeSpan[] {
            new TimeSpan(0,15,0),
            new TimeSpan(0,10,0),
            new TimeSpan(0,5,0),
            new TimeSpan(0,3,0),
            new TimeSpan(0,2,0),
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
        /// <summary>
        /// Sets the countdown timer based on a string.  Does not throw exceptions
        /// </summary>
        /// <param name="countdown">hh:mm:ss or mm:ss </param>
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
                MessageEvent.Invoke(this, new CountDownMessageEventArgs() { 
                    CountdownMessage = countdownmessage, 
                    ElapsedTime = GetElapsedTime() 
                    });
        }
        /// <summary>
        /// Returns elapsed time since the "epoch"  This can be positive AND negative, depending on if the countdown has passed 0 mark.
        /// </summary>
        /// <returns>See summary</returns>
        public TimeSpan GetElapsedTime() {
            return DateTime.Now.Subtract(epoch);
        }

        public CountdownTimer(string timerstring)
        {
            Title = "BADIME";
            countdowntimer = ParseTime(timerstring);
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
            OnMessageEvent(string.Format("Counting down to {0} in {1}",Title,  countdowntimer));

        }

        private void ThreadStarter()
        {
            DateTime endtime =  DateTime.Now.Add(countdowntimer);
            epoch = endtime;
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
                        if(Alerts[i].TotalSeconds >= 60)
                            OnMessageEvent($" /!\\ {Title} IN  {Alerts[i]}  /!\\");
                        if(Alerts[i].TotalSeconds < 60)
                            OnMessageEvent(Alerts[i].Seconds.ToString());
                        lastalert = Alerts[i];
                        break;
                    }
                }
            }
            if(ct.IsCancellationRequested)
                return; //Timer did not finish, abort immediately
            OnMessageEvent($" /!\\ {Title} /!\\");
            Console.WriteLine("Timer Finished");
            if(Finished != null)
                Finished.Invoke(this, EventArgs.Empty);
            //Finished = true;
        }
        /// <summary>
        /// Immediately halt the countdown
        /// </summary>
        public void Stop()
        {
            cts.Cancel();
            timerthread.Abort();
        }

        // Creates a Timespan out of a string.  Supports hh:mm:ss and mm:ss formats
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
        public TimeSpan ElapsedTime;
    }

}
