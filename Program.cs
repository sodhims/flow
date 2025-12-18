using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using dfd2wasm;
using dfd2wasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<LayoutOptimizationService>();
builder.Services.AddScoped<LayoutOptimizerService>();

builder.Services.AddScoped<GeometryService>();
builder.Services.AddScoped<PathService>();
builder.Services.AddScoped<UndoService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<ShapeLibraryService>();

await builder.Build().RunAsync();
