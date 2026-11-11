using ActionTimerExecutor;
using ActionTimerExecutor.BlazorSample.Components;

var builder = WebApplication.CreateBuilder(args);

// Serve the framework static assets (_framework/blazor.web.js) when running from build
// output rather than a published folder.
builder.WebHost.UseStaticWebAssets();

// Blazor interactive server: timers run server-side and push updates to the page.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// One ActionTimer per circuit (browser connection), as recommended by the README:
// choose the lifetime that matches your use case.
builder.Services.AddScoped<IActionTimer, ActionTimer>();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
