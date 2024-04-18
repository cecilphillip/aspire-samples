using Microsoft.EntityFrameworkCore;
using AspireShop.CatalogDb;
using AspireShop.CatalogDbManager;
using AspireShop.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.ConfigureAppConfiguration((ctx, config) =>
{
    config.AddVaultDevServerConfiguration(() => new VaultOptions
    {
        VaultAddress = ctx.Configuration["VAULT_ADDR"],
        VaultToken = ctx.Configuration["VAULT_TOKEN"],
        VaultMount = ctx.Configuration["VAULT_APP_MOUNT"],
        AllowInsecure = true
    }, builder.Services);
});

builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb", null,
    optionsBuilder => optionsBuilder.UseNpgsql(npgsqlBuilder =>
        npgsqlBuilder.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(CatalogDbInitializer.ActivitySourceName));

builder.Services.AddStripe(builder.Configuration);
builder.Services.AddSingleton<CatalogDbInitializer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CatalogDbInitializer>());
builder.Services.AddHealthChecks()
    .AddCheck<CatalogDbInitializerHealthCheck>("DbInitializer", null);

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();
