using AspireShop.AppHost.Resources;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var vault = builder.AddVaultServer("vault");
builder.AddExecutable("vaultcli", "bash", ".", "vault_setup.sh");


var catalogDb = builder.AddPostgres("catalog", password: builder.CreateStablePassword("catalog-password"))
    .WithDataVolume()
    .AddDatabase("catalogdb");

var basketCache = builder.AddRedis("basketcache")
    .WithRedisCommander()
    .WithDataVolume();

var catalogService = builder.AddProject<Projects.AspireShop_CatalogService>("catalogservice")
    .WithReference(catalogDb);

var basketService = builder.AddProject<Projects.AspireShop_BasketService>("basketservice")
    .WithReference(basketCache);

builder.AddProject<Projects.AspireShop_Frontend>("frontend")
    .WithReference(basketService)
    .WithReference(catalogService)
    .WithReference(vault)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.AspireShop_CatalogDbManager>("catalogdbmanager")
    .WithReference(catalogDb);

builder.Build().Run();
