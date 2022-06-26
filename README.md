# ActionTimerExecutor

Ths package uses a PeriodicTimer to execure functions of the type Func<Task> at specified intervals. 
The functions can be executed in descending order based on the Interval Requested(nowait = false) or can be concurrently executed(noWait = true).
  
  # Usage
  Inject your timer, register your actions and start the timer. 
  
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
