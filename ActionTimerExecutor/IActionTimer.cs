namespace ActionTimerExecutor
{
    /// <summary>
    /// Depends on Microsoft.Extensons.Logging
    /// </summary>
    public interface IActionTimer
    {
        /// <summary>
        /// Adds an action to the correct list based on if we need to execute in order or not.
        /// </summary>
        /// <param name="action">A unique Identifier for the Action to Execute</param>
        /// <param name="actionToExecute">A Fuction that only returns Task.</param>
        /// <param name="intervalMilliseconds">Time in Milliseconds to wait before the action is Executed</param>
        /// <param name="noWait">Should the function be executed in order or parallel</param>
        /// <returns>True if the Action was added successfully, False if it was not</returns>
        bool AddAction(string action, Func<Task> actionToExecute, int intervalMilliseconds, bool noWait);
        /// <summary>
        /// Dispose all used elements
        /// </summary>
        void Dispose();
        /// <summary>
        /// Get's a list of errors from a specific function
        /// </summary>
        /// <param name="action">Unique Identifier for the Action</param>
        /// <param name="noWait">Flag to indicate the type of action.</param>
        /// <returns></returns>
        List<string> GetErrors(string action, bool noWait);
        /// <summary>
        /// Removes an action from the correct list based on if we need to execute in order or not.
        /// </summary>
        /// <param name="action">Unique Identifier for the Action</param>
        /// <param name="noWait">Flag to indicate the type of action.</param>
        /// <returns></returns>
        bool RemoveAction(string action, bool noWait);
        /// <summary>
        /// Starts the Timer
        /// </summary>
        /// <param name="milliseconds">Milliseconds between ticks</param>
        /// <param name="ct">Cancellation token</param>
        void Start(int milliseconds = 1000, CancellationToken? ct = null);
    }
}