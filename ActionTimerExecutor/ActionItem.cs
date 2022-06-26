using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionTimerExecutor
{
    internal class ActionItem
    {
        public int IntervalMilliseconds { get; set; }
        public Func<Task> Action { get; set; }
        public DateTime LastExecuted { get; set; } = DateTime.Now;
        public List<string> Exceptions = new();

        public ActionItem(Func<Task> actionToExecute, int intervalMilliseconds)
        {
            Action = actionToExecute;
            IntervalMilliseconds = intervalMilliseconds;
        }
    }
}
