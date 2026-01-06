using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PortalCliente;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ⬇️ si esto está arriba, el warning desaparece
builder.Services.AddMudServices();

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Use the API base address during development so client calls go to the API server.
// Adjust the URL if your API runs on a different port.
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("http://localhost:5149") });

await builder.Build().RunAsync();
