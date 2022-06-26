namespace ActionTimerExecutor
{
    public interface IActionTimer
    {
        bool AddAction(string action, Func<Task> actionToExecute, int intervalMilliseconds, bool noWait);
        void Dispose();
        List<string> GetErrors(string action, bool noWait);
        bool RemoveAction(string action, bool noWait);
        void Start(int milliseconds = 1000, CancellationToken? ct = null);
    }
}