# ActionTimerExecutor

This package uses a PeriodicTimer to execute functions of the type Func<Task> at specified intervals. 
The functions can be executed in descending order based on the Interval Requested (noWait = false) or can be executed concurrently (noWait = true).

# Requirements

Targets .NET 10 (`net10.0`) and .NET 11 (`net11.0`). The .NET 11 SDK is pinned in `global.json`; the `net11.0` build enables the runtime async engine (`runtime-async=on`).
  
  # Usage
  Inject your timer, register your actions and start the timer. 
  Make sure you select the correct lifetime (Scoped, Transient, Singleton) for your use case. 
  
  ```
  builder.Services.AddTransient<IActionTimer, ActionTimer>();
  ```
  
  Sample Blazor Component
  
  ```
  @inject IActionTimer actionTimer;
  @implements IAsyncDisposable
  
  protected override async Task OnInitializedAsync()
  {
      actionTimer.AddAction(nameof(ExecuteUpdateOrders), ExecuteUpdateOrders, 1000*2,false);
  }
  
  public async Task ExecuteUpdateOrders()
  {
      foreach (var order in _orders)
      {
           order.LastUpdated = DateTime.Now;
           visibleOrders = _orders.ToList();
           await InvokeAsync(StateHasChanged);
      }
  }
  
  public async ValueTask DisposeAsync()
  {
      actionTimer.RemoveAction(nameof(ExecuteUpdateOrders),false);
      actionTimer.Dispose();
  }
  ```
