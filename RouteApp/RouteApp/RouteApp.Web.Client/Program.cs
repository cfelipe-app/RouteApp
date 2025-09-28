using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using RouteApp.Shared.Services;
using RouteApp.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the RouteApp.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

builder.Services.AddMudServices();  // <-- MudBlazor

await builder.Build().RunAsync();