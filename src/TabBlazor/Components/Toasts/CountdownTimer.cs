using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TabBlazor.Components.Toasts
{
    internal class CountdownTimer : IDisposable
    {
        private Timer _timer;
        private int _percentComplete;

        internal Action<int> OnTick;
        internal Action OnElapsed;

       

        internal CountdownTimer(int timeout)
        {
            _timer = new Timer(timeout)
            {
                Interval = (timeout  / 100),
                AutoReset = true
            };

            _timer.Elapsed += HandleTick;

            _percentComplete = 0;
        }

        internal void Start()
        {
            _timer.Start();
        }

        private void HandleTick(object sender, ElapsedEventArgs args)
        {
            _percentComplete += 1;
            OnTick?.Invoke(_percentComplete);

            if (_percentComplete >= 100)
            {
                OnElapsed?.Invoke();
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
            _timer = null;
        }
    }
}
