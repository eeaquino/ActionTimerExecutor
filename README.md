# ActionTimerExecutor

This package uses a PeriodicTimer to execute functions of the type Func<Task> at specified intervals. 
The functions can be executed in descending order based on the Interval Requested (noWait = false) or can be executed concurrently (noWait = true).

# Requirements

Targets .NET 10 (`net10.0`). The .NET 10 SDK is pinned in `global.json`.
  
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
