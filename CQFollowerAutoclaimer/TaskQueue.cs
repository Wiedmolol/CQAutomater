using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQFollowerAutoclaimer
{
    class TaskQueue
    {
        System.Timers.Timer queueTimer = new System.Timers.Timer();
        List<Tuple<Func<Task<bool>>, string>> _queue = new List<Tuple<Func<Task<bool>>, string>>();
        public TaskQueue()
        {
            queueTimer.Interval = 4000;
            queueTimer.Elapsed += queueTimer_Elapsed;
            queueTimer.Start();
        }

        async void queueTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_queue.Count > 0)
            {
                var t = _queue.Pop();
                if (!await t.Item1.Invoke())
                {
                    Enqueue(t);
                }
            }
        }

        public bool Contains(string s)
        {
            foreach (var t in _queue)
            {
                if (t.Item2 == s)
                    return true;
            }
            return false;
        }


        public void Enqueue(Tuple<Func<Task<bool>>, string> t)
        {
            if (t.Item2 == "bid")
            {
                _queue.Insert(0, t);
            }
            _queue.Add(t);
        }

        public void Enqueue(Func<Task<bool>> t, string s)
        {
            Tuple<Func<Task<bool>>, string> temp = new Tuple<Func<Task<bool>>, string>(t, s);
            Enqueue(temp);
        }

    }
}
