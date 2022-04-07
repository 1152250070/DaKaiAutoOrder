using System;
using System.Threading;

namespace Deduce.DMIP.ResourceManage
{
    public delegate void RunAction();
    public class ETimer
    {
        public virtual event RunAction Run;
        TimerCallback timerDelegate;
        AutoResetEvent autoEvent = new AutoResetEvent(false);
        Timer _timer;
        TimeSpan _busy;
        TimeSpan _free;
        TimeSpan _curScheduling;
        int _startHour;
        int _endHour;

        public ETimer()
        {
            timerDelegate = new TimerCallback(Executing);
        }

        private int _timeOut = -1;
        /// <summary>
        /// 超时时间，默认不设置，单位秒，针对阻塞模式
        /// </summary>
        public int TimeOut
        {
            get
            {
                return _timeOut;
            }
            set
            {
                if (value > -1)
                {
                    _timeOut = 1000 * value;
                }
            }
        }

        /// <summary>
        /// 排队数，针对阻塞模式
        /// </summary>
        private int ThreadCount { get; set; } = 2;

        /// <summary>
        /// 默认阻塞模式true
        /// </summary>
        public bool IsBlock { get; set; } = true;

        public void Start(TimeSpan period)
        {
            _timer = new Timer(timerDelegate, autoEvent, new TimeSpan(0), period);
            autoEvent.Set();
        }

        public void Change(TimeSpan period)
        {
            _timer.Change(new TimeSpan(0), period);
        }

        public void Change(int startHour, int endHour, TimeSpan period)
        {
            if (period == _free)
                return;

            _startHour = startHour;
            _endHour = endHour;
            _busy = period;
        }

        int _count = 0;
        private void Executing(object nObject)
        {
            if (IsBlock)
            {
                AutoResetEvent autoEvent = (AutoResetEvent)nObject;
                if (_count++ < ThreadCount)
                {
                    autoEvent.WaitOne(TimeOut);
                    Run?.Invoke();
                    autoEvent.Set();
                }
                _count--;
            }
            else
            {
                Run?.Invoke();
            }
            if (_startHour != 0)
            {
                if (_startHour < DateTime.Now.Hour && _endHour > DateTime.Now.Hour && _curScheduling == _free)
                {
                    _timer.Change(_busy, _busy);
                    _curScheduling = _busy;
                }
                if ((_startHour > DateTime.Now.Hour || _endHour < DateTime.Now.Hour) && _curScheduling == _busy)
                {
                    _timer.Change(_free, _free);
                    _curScheduling = _free;
                }
            }
        }

        public void Stop()
        {
            if (_timer != null)
                _timer.Dispose();
        }
    }
}
