using AspireShop.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AspireShop.Frontend.Components;
using AspireShop.Frontend.Services;
using AspireShop.GrpcBasket;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

#pragma warning disable ASP0013
builder.Host.ConfigureAppConfiguration((ctx, config) =>
{
    config.AddVaultDevServerConfiguration(() => new VaultOptions
    {
        VaultAddress = ctx.Configuration["VAULT_ADDR"] ?? string.Empty,
        VaultToken = ctx.Configuration["VAULT_TOKEN"] ?? string.Empty,
        VaultMount = ctx.Configuration["VAULT_APP_MOUNT"] ?? string.Empty,
        AllowInsecure = true
    },  builder.Services);
});
#pragma warning restore ASP0013

builder.Services.AddHttpForwarderWithServiceDiscovery();

builder.Services.AddHttpServiceReference<CatalogServiceClient>("https+http://catalogservice", healthRelativePath: "health");

var isHttps = builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https";

builder.Services.AddSingleton<BasketServiceClient>()
    .AddGrpcServiceReference<Basket.BasketClient>($"{(isHttps ? "https" : "http")}://basketservice", failureStatus: HealthStatus.Degraded);

builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles();

app.MapRazorComponents<App>();

app.MapForwarder("/catalog/images/{id}", "https+http://catalogservice", "/api/v1/catalog/items/{id}/image");

app.MapDefaultEndpoints();

app.Run();
