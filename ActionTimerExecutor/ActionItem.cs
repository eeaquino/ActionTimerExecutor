using System.Diagnostics;

namespace ActionTimerExecutor
{
    internal sealed class ActionItem
    {
        public ActionItem(Func<Task> actionToExecute, int intervalMilliseconds)
        {
            Action = actionToExecute;
            IntervalMilliseconds = intervalMilliseconds;
            LastExecutedTimestamp = Stopwatch.GetTimestamp();
        }

        public int IntervalMilliseconds { get; }

        public Func<Task> Action { get; }

        /// <summary>
        /// Monotonic timestamp (<see cref="Stopwatch"/> ticks) of the last execution.
        /// A monotonic clock cannot jump backwards, so a system clock adjustment can no
        /// longer make the elapsed delta negative and stall the action.
        /// </summary>
        public long LastExecutedTimestamp { get; set; }

        public List<string> Exceptions { get; } = [];

        public bool IsDue(long nowTimestamp) =>
            Stopwatch.GetElapsedTime(LastExecutedTimestamp, nowTimestamp).TotalMilliseconds
                >= IntervalMilliseconds;
    }
}
