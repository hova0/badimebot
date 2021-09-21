using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace badimebot
{
#pragma warning disable  CS1591    // 
    public class CountdownTimer
    {
        private object _lockobject = new object();

      
        public enum CountdownState
        {
            Idle,
            PreCountdown,
            PostCountdown
        }
        public Queue<CountdownItem> CountdownList { get; set; } = new Queue<CountdownItem>();
        public CountdownItem CurrentItem { get; set; }
        CountdownState _state = CountdownState.Idle;


        System.Threading.CancellationToken ct;
        System.Threading.CancellationTokenSource cts;
        System.Threading.Thread timerthread;
        public event EventHandler<CountDownMessageEventArgs> MessageEvent;
        public bool SuppressMessages { get; set; }


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

        public CountdownTimer() {
            cts = new System.Threading.CancellationTokenSource();
            ct = cts.Token;
        }

        public void Enqueue(string Anime, TimeSpan Countdown, TimeSpan Length)
        {
            CountdownItem item = new CountdownItem();
            item.Title = Anime;
            item.Length = Length;
            item.PreCountdown = Countdown;

            CountdownList.Enqueue(item);
        }
        public void Enqueue(CountdownItem item)
        {
            CountdownList.Enqueue(item);
        }


        public void OnMessageEvent(string countdownmessage)
        {
            if (SuppressMessages)
                return;
            if (MessageEvent != null)
                MessageEvent.Invoke(this, new CountDownMessageEventArgs()
                {
                    CountdownMessage = countdownmessage,
                    ElapsedTime = GetElapsedTime()
                });
        }
        /// <summary>
        /// Returns elapsed time since the "epoch"  This can be positive AND negative, depending on if the countdown has passed 0 mark.
        /// </summary>
        /// <returns>See summary</returns>
        public TimeSpan GetElapsedTime()
        {
            return DateTime.Now.Subtract(CurrentItem.Epoch);
        }

        public void Start()
        {
            if (timerthread != null)
                return;
            System.Threading.ThreadStart ts = new System.Threading.ThreadStart(CountdownThread);
            ct = cts.Token;
            this.timerthread = new System.Threading.Thread(ts);
            this.timerthread.Start();
            //OnMessageEvent(string.Format("Counting down to {0} in {1}",Title,  countdowntimer));

        }

        /// <summary>
        /// Main countdown thread.
        /// </summary>
        private void CountdownThread()
        {

            while (ct.IsCancellationRequested == false)
            {
                switch (_state)
                {
                    case CountdownState.Idle:
                        while (ct.IsCancellationRequested == false)
                        {
                            lock (_lockobject)
                            {
                                if (CountdownList.Count != 0)
                                {
                                    _state = CountdownState.PreCountdown;
                                    break;
                                }
                            }
                            System.Threading.Thread.Sleep(1000);
                        }
                        break;
                    case CountdownState.PreCountdown:
                        CountdownItem _nextItem;
                        lock (_lockobject)
                        {
                            _nextItem = CountdownList.Dequeue();
                        }
                        _nextItem.Epoch = DateTime.Now.Add(_nextItem.PreCountdown);
                        CurrentItem = _nextItem;
                        //if ((CurrentItem.Epoch - DateTime.Now).TotalSeconds < 5)
                        //    throw new Exception($"Epoch calculation wrong. Epoch = {CurrentItem.Epoch} Now = {DateTime.Now}  PreCountdown = {CurrentItem.PreCountdown} ");

                        int _alertIndex = 0;
                        for (int i = 0; i < Alerts.Length; i++)
                            if (Alerts[i] <= CurrentItem.PreCountdown)
                            {
                                _alertIndex = i;
                                break;
                            }

                        while (ct.IsCancellationRequested == false)
                        {
                            if ((CurrentItem.Epoch - DateTime.Now) < Alerts[_alertIndex])
                            {
                                // alart
                                Console.WriteLine($"{(CurrentItem.Epoch - DateTime.Now).TotalSeconds} < {Alerts[_alertIndex]}");
                                PrintAlert(CurrentItem, Alerts[_alertIndex]);
                                if (_alertIndex == Alerts.Length - 1)
                                {   // Ran out of alerts, less than one second left
                                    _state = CountdownState.PostCountdown;
                                    break;
                                }
                                _alertIndex++;
                            }
                            System.Threading.Thread.Sleep(100);
                        }
                        break;
                    case CountdownState.PostCountdown:
                        if (DateTime.Now > (CurrentItem.Epoch + CurrentItem.Length))
                        {
                            lock (_lockobject)
                            {
                                if (CountdownList.Count == 0)
                                {
                                    // no more things to countdown
                                    _state = CountdownState.Idle;
                                    OnMessageEvent("THE END N SHIT");
                                }
                                else
                                {
                                    // Move to next anime  (PreCountdown will dequeue)
                                    _state = CountdownState.PreCountdown;
                                }
                            }
                        }
                        // Long sleep
                        System.Threading.Thread.Sleep(1000);
                        break;
                }
            }
            // end of loop
        }

        private void PrintAlert(CountdownItem item, TimeSpan alerttime, CountdownState state = CountdownState.PreCountdown)
        {
            if (state == CountdownState.PostCountdown)
                OnMessageEvent($"{item.Title} elapsed time is {DateTime.Now - item.Epoch}");
            else if (state == CountdownState.PreCountdown)
                OnMessageEvent($"{item.Title} in {alerttime}");
            else
                OnMessageEvent($"{item.Title} starting!");
            //OnMessageEvent($"{item.Title} in {alerttime}");
        }

        public void PrintAlert()
        {
            if (_state == CountdownState.Idle)
                return;
            PrintAlert(CurrentItem, DateTime.Now - CurrentItem.Epoch, _state);
            //OnMessageEvent($"{CurrentItem.Title} elapsed time is {DateTime.Now - CurrentItem.Epoch}");
        }

        /// <summary>
        /// Immediately halt the countdown
        /// </summary>
        public void Stop()
        {
            cts.Cancel();
        }


        public static CountdownItem Parse(string message)
        {
            // add Anime Title for 25:00 in 15:00
            // add <title> for <length> in <countdown>
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"add\s(.*?)\sfor\s(.*?)\sin\s(.*)");
            if (r.IsMatch(message))
            {
                var m = r.Match(message);

                CountdownItem ci = new CountdownItem()
                {
                    Title = m.Groups[1].Value,
                    Length = ParseTime(m.Groups[2].Value),
                    PreCountdown = ParseTime(m.Groups[3].Value)
                };
                return ci;
            }
            return CountdownItem.Empty;
        }

        // Creates a Timespan out of a string.  Supports hh:mm:ss and mm:ss formats
        private static TimeSpan ParseTime(string timerstring)
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
