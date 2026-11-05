using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ActionTimerExecutor.Tests;

public class ActionTimerTests : IDisposable
{
    private readonly ILogger<ActionTimer> _mockLogger;
    private readonly ActionTimer _actionTimer;

    public ActionTimerTests()
    {
        _mockLogger = Substitute.For<ILogger<ActionTimer>>();
        _actionTimer = new ActionTimer(_mockLogger);
    }

    [Fact]
    public void AddAction_WithNoWaitFalse_AddsToOrderedList()
    {
        // Arrange
        var actionName = "TestAction";
        Func<Task> action = () => Task.CompletedTask;
        var interval = 1000;

        // Act
        var result = _actionTimer.AddAction(actionName, action, interval, noWait: false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AddAction_WithNoWaitTrue_AddsToConcurrentList()
    {
        // Arrange
        var actionName = "TestAction";
        Func<Task> action = () => Task.CompletedTask;
        var interval = 1000;

        // Act
        var result = _actionTimer.AddAction(actionName, action, interval, noWait: true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AddAction_WithDuplicateKey_NoWaitFalse_ReturnsFalse()
    {
        // Arrange
        var actionName = "TestAction";
        Func<Task> action = () => Task.CompletedTask;
        var interval = 1000;

        // Act
        _actionTimer.AddAction(actionName, action, interval, noWait: false);
        var result = _actionTimer.AddAction(actionName, action, interval, noWait: false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AddAction_WithDuplicateKey_NoWaitTrue_ReturnsFalse()
    {
        // Arrange
        var actionName = "TestAction";
        Func<Task> action = () => Task.CompletedTask;
        var interval = 1000;

        // Act
        _actionTimer.AddAction(actionName, action, interval, noWait: true);
        var result = _actionTimer.AddAction(actionName, action, interval, noWait: true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveAction_WithNoWaitFalse_RemovesFromOrderedList()
    {
        // Arrange
        var actionName = "TestAction";
        Func<Task> action = () => Task.CompletedTask;
        var interval = 1000;
        _actionTimer.AddAction(actionName, action, interval, noWait: false);

        // Act
        var result = _actionTimer.RemoveAction(actionName, noWait: false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RemoveAction_WithNoWaitTrue_RemovesFromConcurrentList()
    {
        // Arrange
        var actionName = "TestAction";
        Func<Task> action = () => Task.CompletedTask;
        var interval = 1000;
        _actionTimer.AddAction(actionName, action, interval, noWait: true);

        // Act
        var result = _actionTimer.RemoveAction(actionName, noWait: true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RemoveAction_NonExistentAction_NoWaitFalse_ReturnsFalse()
    {
        // Arrange
        var actionName = "NonExistentAction";

        // Act
        var result = _actionTimer.RemoveAction(actionName, noWait: false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveAction_NonExistentAction_NoWaitTrue_ReturnsFalse()
    {
        // Arrange
        var actionName = "NonExistentAction";

        // Act
        var result = _actionTimer.RemoveAction(actionName, noWait: true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetErrors_WithNoWaitFalse_ReturnsEmptyList_WhenNoAction()
    {
        // Arrange
        var actionName = "NonExistentAction";

        // Act
        var errors = _actionTimer.GetErrors(actionName, noWait: false);

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void GetErrors_WithNoWaitTrue_ReturnsEmptyList_WhenNoAction()
    {
        // Arrange
        var actionName = "NonExistentAction";

        // Act
        var errors = _actionTimer.GetErrors(actionName, noWait: true);

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void GetErrors_WithNoWaitFalse_ReturnsEmptyList_WhenActionHasNoErrors()
    {
        // Arrange
        var actionName = "TestAction";
        Func<Task> action = () => Task.CompletedTask;
        var interval = 1000;
        _actionTimer.AddAction(actionName, action, interval, noWait: false);

        // Act
        var errors = _actionTimer.GetErrors(actionName, noWait: false);

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void GetErrors_WithNoWaitTrue_ReturnsEmptyList_WhenActionHasNoErrors()
    {
        // Arrange
        var actionName = "TestAction";
        Func<Task> action = () => Task.CompletedTask;
        var interval = 1000;
        _actionTimer.AddAction(actionName, action, interval, noWait: true);

        // Act
        var errors = _actionTimer.GetErrors(actionName, noWait: true);

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Start_ExecutesOrderedActions_WhenIntervalElapsed()
    {
        // Arrange
        var actionExecuted = false;
        var actionName = "TestAction";
        Func<Task> action = () =>
        {
            actionExecuted = true;
            return Task.CompletedTask;
        };
        var interval = 100; // 100ms interval
        _actionTimer.AddAction(actionName, action, interval, noWait: false);

        // Act
        var cts = new CancellationTokenSource();
        _actionTimer.Start(milliseconds: 50, ct: cts.Token); // Timer ticks every 50ms

        // Wait for at least one tick and action execution
        await Task.Delay(200);
        cts.Cancel();

        // Assert
        Assert.True(actionExecuted);
    }

    [Fact]
    public async Task Start_ExecutesConcurrentActions_WhenIntervalElapsed()
    {
        // Arrange
        var actionExecuted = false;
        var actionName = "TestAction";
        Func<Task> action = () =>
        {
            actionExecuted = true;
            return Task.CompletedTask;
        };
        var interval = 100; // 100ms interval
        _actionTimer.AddAction(actionName, action, interval, noWait: true);

        // Act
        var cts = new CancellationTokenSource();
        _actionTimer.Start(milliseconds: 50, ct: cts.Token); // Timer ticks every 50ms

        // Wait for at least one tick and action execution
        await Task.Delay(200);
        cts.Cancel();

        // Assert
        Assert.True(actionExecuted);
    }

    [Fact]
    public async Task Start_HandlesExceptionInAction_AndLogsError()
    {
        // Arrange
        var actionName = "FailingAction";
        Func<Task> action = () => throw new InvalidOperationException("Test exception");
        var interval = 100;
        _actionTimer.AddAction(actionName, action, interval, noWait: false);

        // Act
        var cts = new CancellationTokenSource();
        _actionTimer.Start(milliseconds: 50, ct: cts.Token);

        // Wait for execution
        await Task.Delay(200);
        cts.Cancel();

        // Assert
        var errors = _actionTimer.GetErrors(actionName, noWait: false);
        Assert.NotEmpty(errors);
        Assert.Contains("Test exception", errors[0]);
    }

    [Fact]
    public async Task Start_HandlesExceptionInConcurrentAction_AndLogsError()
    {
        // Arrange
        var actionName = "FailingAction";
        Func<Task> action = () => throw new InvalidOperationException("Test exception");
        var interval = 100;
        _actionTimer.AddAction(actionName, action, interval, noWait: true);

        // Act
        var cts = new CancellationTokenSource();
        _actionTimer.Start(milliseconds: 50, ct: cts.Token);

        // Wait for execution
        await Task.Delay(200);
        cts.Cancel();

        // Assert
        var errors = _actionTimer.GetErrors(actionName, noWait: true);
        Assert.NotEmpty(errors);
        Assert.Contains("Test exception", errors[0]);
    }

    [Fact]
    public async Task Start_DoesNotExecuteAction_WhenIntervalNotElapsed()
    {
        // Arrange
        var executionCount = 0;
        var actionName = "TestAction";
        Func<Task> action = () =>
        {
            executionCount++;
            return Task.CompletedTask;
        };
        var interval = 5000; // 5 seconds interval
        _actionTimer.AddAction(actionName, action, interval, noWait: false);

        // Act
        var cts = new CancellationTokenSource();
        _actionTimer.Start(milliseconds: 50, ct: cts.Token);

        // Wait for less than the interval
        await Task.Delay(200);
        cts.Cancel();

        // Assert
        Assert.Equal(0, executionCount);
    }

    [Fact]
    public async Task Start_ExecutesMultipleOrderedActions()
    {
        // Arrange
        var action1Executed = false;
        var action2Executed = false;
        
        Func<Task> action1 = () =>
        {
            action1Executed = true;
            return Task.CompletedTask;
        };
        
        Func<Task> action2 = () =>
        {
            action2Executed = true;
            return Task.CompletedTask;
        };

        var interval = 100;
        _actionTimer.AddAction("Action1", action1, interval, noWait: false);
        _actionTimer.AddAction("Action2", action2, interval, noWait: false);

        // Act
        var cts = new CancellationTokenSource();
        _actionTimer.Start(milliseconds: 50, ct: cts.Token);

        // Wait for execution
        await Task.Delay(200);
        cts.Cancel();

        // Assert
        Assert.True(action1Executed);
        Assert.True(action2Executed);
    }

    [Fact]
    public async Task Start_ExecutesMultipleConcurrentActions()
    {
        // Arrange
        var action1Executed = false;
        var action2Executed = false;
        
        Func<Task> action1 = () =>
        {
            action1Executed = true;
            return Task.CompletedTask;
        };
        
        Func<Task> action2 = () =>
        {
            action2Executed = true;
            return Task.CompletedTask;
        };

        var interval = 100;
        _actionTimer.AddAction("Action1", action1, interval, noWait: true);
        _actionTimer.AddAction("Action2", action2, interval, noWait: true);

        // Act
        var cts = new CancellationTokenSource();
        _actionTimer.Start(milliseconds: 50, ct: cts.Token);

        // Wait for execution
        await Task.Delay(200);
        cts.Cancel();

        // Assert
        Assert.True(action1Executed);
        Assert.True(action2Executed);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _actionTimer.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public async Task GetErrors_ClearsOldErrors_WhenExceeding100Errors()
    {
        // Arrange
        var actionName = "FailingAction";
        var executionCount = 0;
        Func<Task> action = () =>
        {
            executionCount++;
            throw new InvalidOperationException($"Error {executionCount}");
        };
        var interval = 10; // Very short interval
        _actionTimer.AddAction(actionName, action, interval, noWait: false);

        // Act
        var cts = new CancellationTokenSource();
        _actionTimer.Start(milliseconds: 10, ct: cts.Token);

        // Wait for many executions (enough to generate > 100 errors)
        await Task.Delay(2000);
        cts.Cancel();

        // Assert
        var errors = _actionTimer.GetErrors(actionName, noWait: false);
        // Should have cleared and started fresh, so less than or equal to 100
        Assert.True(errors.Count <= 100);
    }

    public void Dispose()
    {
        _actionTimer?.Dispose();
    }
}
