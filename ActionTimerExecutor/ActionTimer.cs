namespace ActionTimerExecutor
{
    public class ActionTimer : IDisposable, IActionTimer
    {
        private readonly ILogger<ActionTimer> _logger;
        private PeriodicTimer? _periodicTimer;
        private ConcurrentDictionary<string, ActionItem> _actionItems = new();
        private ConcurrentDictionary<string, ActionItem> _actionItemsConcurrent = new();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public ActionTimer(ILogger<ActionTimer> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Starts the Timer
        /// </summary>
        /// <param name="milliseconds">Milliseconds between ticks</param>
        /// <param name="ct">Cancellation token</param>
        public void Start(int milliseconds = 1000, CancellationToken? ct = null)
        {
            _periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(milliseconds));
            Task.Run(() => ExecuteAction(ct ?? _cts.Token));
        }
        /// <summary>
        /// Get's a list of errors from a specific function
        /// </summary>
        /// <param name="action">Unique Identifier for the Action</param>
        /// <param name="noWait">Flag to indicate the type of action.</param>
        /// <returns></returns>
        public List<string> GetErrors(string action, bool noWait)
        {
            if (!noWait && _actionItems.TryGetValue(action, out var actionItem))
                return actionItem.Exceptions;
            if (_actionItemsConcurrent.TryGetValue(action, out var actionItemConcurrent))
                return actionItemConcurrent.Exceptions;
            return new List<string>();
        }
        /// <summary>
        /// Adds an action to the correct list based on if we need to execute in order or not.
        /// </summary>
        /// <param name="action">A unique Identifier for the Action to Execute</param>
        /// <param name="actionToExecute">Func<Task> A Fuction that only returns Task.</param>
        /// <param name="intervalMilliseconds">Time in Milliseconds to wait before the action is Executed</param>
        /// <param name="noWait">Should the function be executed in order or parallel</param>
        /// <returns>True if the Action was added successfully, False if it was not</returns>
        public bool AddAction(string action, Func<Task> actionToExecute, int intervalMilliseconds, bool noWait)
        {
            if (!noWait)
                return _actionItems.TryAdd(action, new ActionItem(actionToExecute, intervalMilliseconds));
            return _actionItemsConcurrent.TryAdd(action, new ActionItem(actionToExecute, intervalMilliseconds));
        }
        /// <summary>
        /// Removes an action from the correct list based on if we need to execute in order or not.
        /// </summary>
        /// <param name="action">Unique Identifier for the Action</param>
        /// <param name="noWait">Flag to indicate the type of action.</param>
        /// <returns></returns>
        public bool RemoveAction(string action, bool noWait)
        {
            if (!noWait)
                return _actionItems.TryRemove(action, out var _);
            return _actionItemsConcurrent.TryRemove(action, out var _);
        }

        private async Task ExecuteAction(CancellationToken ct)
        {
            while (await _periodicTimer!.WaitForNextTickAsync(ct) && !ct.IsCancellationRequested)
            {
                var now = DateTime.Now;
                ExecuteConcurrentActions(now);
                await ExecuteOrderedActions(now);
            }
        }
        private async Task RunAction(KeyValuePair<string, ActionItem> actionItem, DateTime now)
        {
            try
            {
                if ((now - actionItem.Value.LastExecuted).TotalMilliseconds >= actionItem.Value.IntervalMilliseconds)
                {
                    actionItem.Value.LastExecuted = now;
                    await actionItem.Value.Action();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing Action {actionItem.Key}");
                if (actionItem.Value.Exceptions.Count > 100)
                    actionItem.Value.Exceptions.Clear();
                actionItem.Value.Exceptions.Add($"Error in {actionItem.Key} {ex.Message}");
            }
        }
        private void ExecuteConcurrentActions(DateTime now)
        {
            Parallel.ForEach(_actionItemsConcurrent, async actionItem =>
            {
                await RunAction(actionItem, now);
            });
        }
        private async Task ExecuteOrderedActions(DateTime now)
        {
            foreach (var actionItem in _actionItems.OrderByDescending(o => o.Value.IntervalMilliseconds))
            {
                await RunAction(actionItem, now);
            }
        }

        public void Dispose()
        {
            _periodicTimer?.Dispose();
            _cts.Dispose();
        }
    }
}