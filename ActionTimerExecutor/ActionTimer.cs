using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ActionTimerExecutor
{
    /// <summary>
    /// Depends on Microsoft.Extensons.Logging
    /// </summary>
    public partial class ActionTimer : IDisposable, IActionTimer
    {
        private const int MaxErrorsPerAction = 100;

        private readonly ILogger<ActionTimer> _logger;
        private readonly ConcurrentDictionary<string, ActionItem> _actionItems = new();
        private readonly ConcurrentDictionary<string, ActionItem> _actionItemsConcurrent = new();
        private readonly CancellationTokenSource _cts = new();

        private PeriodicTimer? _periodicTimer;

        // Execution order and membership only change on AddAction/RemoveAction, so the
        // per-tick enumeration snapshots are cached and invalidated on mutation instead
        // of being rebuilt (and re-sorted) on every tick.
        private volatile KeyValuePair<string, ActionItem>[]? _orderedSnapshot;
        private volatile KeyValuePair<string, ActionItem>[]? _concurrentSnapshot;

        /// <summary>
        /// Constructor Requires a Microsoft Ilogger
        /// </summary>
        /// <param name="logger">Ilogger element</param>
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
            Task.Run(() => ExecuteAction(_periodicTimer, ct ?? _cts.Token));
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
        /// <param name="actionToExecute">A Fuction that only returns Task.</param>
        /// <param name="intervalMilliseconds">Time in Milliseconds to wait before the action is Executed</param>
        /// <param name="noWait">Should the function be executed in order or parallel</param>
        /// <returns>True if the Action was added successfully, False if it was not</returns>
        public bool AddAction(string action, Func<Task> actionToExecute, int intervalMilliseconds, bool noWait)
        {
            if (!noWait)
            {
                if (!_actionItems.TryAdd(action, new ActionItem(actionToExecute, intervalMilliseconds)))
                    return false;

                _orderedSnapshot = null;
                return true;
            }

            if (!_actionItemsConcurrent.TryAdd(action, new ActionItem(actionToExecute, intervalMilliseconds)))
                return false;

            _concurrentSnapshot = null;
            return true;
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
            {
                if (!_actionItems.TryRemove(action, out _))
                    return false;

                _orderedSnapshot = null;
                return true;
            }

            if (!_actionItemsConcurrent.TryRemove(action, out _))
                return false;

            _concurrentSnapshot = null;
            return true;
        }

        private async Task ExecuteAction(PeriodicTimer? periodicTimer, CancellationToken ct)
        {
            if (periodicTimer is null)
                return;

            while (await periodicTimer.WaitForNextTickAsync(ct))
            {
                // One monotonic read per tick, shared by every action.
                var now = Stopwatch.GetTimestamp();
                DispatchConcurrentActions(now);
                await ExecuteOrderedActions(now);
            }
        }

        private async Task RunAction(KeyValuePair<string, ActionItem> actionItem, long now)
        {
            var item = actionItem.Value;

            try
            {
                if (!item.IsDue(now))
                    return;

                item.LastExecutedTimestamp = now;
                await item.Action();
            }
            catch (Exception ex)
            {
                LogActionExecutionError(actionItem.Key, ex);

                if (item.Exceptions.Count > MaxErrorsPerAction)
                    item.Exceptions.Clear();

                item.Exceptions.Add($"Error in {actionItem.Key} {ex.Message}");
            }
        }

        private void DispatchConcurrentActions(long now)
        {
            // Fire-and-forget, as before: the tick never waits for concurrent actions.
            // RunAction records its own failures, so nothing is left unobserved.
            foreach (var actionItem in _concurrentSnapshot ??= BuildConcurrentSnapshot())
                _ = RunAction(actionItem, now);
        }

        private async Task ExecuteOrderedActions(long now)
        {
            foreach (var actionItem in _orderedSnapshot ??= BuildOrderedSnapshot())
                await RunAction(actionItem, now);
        }

        private KeyValuePair<string, ActionItem>[] BuildOrderedSnapshot() =>
            _actionItems.OrderByDescending(static o => o.Value.IntervalMilliseconds).ToArray();

        private KeyValuePair<string, ActionItem>[] BuildConcurrentSnapshot() =>
            _actionItemsConcurrent.ToArray();

        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Error executing Action {ActionName}")]
        private partial void LogActionExecutionError(string actionName, Exception exception);

        /// <summary>
        /// Dispose all used elements
        /// </summary>
        public void Dispose()
        {
            _periodicTimer?.Dispose();
            _cts.Dispose();
        }
    }
}
