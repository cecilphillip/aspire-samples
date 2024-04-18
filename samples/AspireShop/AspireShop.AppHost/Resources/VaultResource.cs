namespace AspireShop.AppHost.Resources;

public class VaultServerResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
    internal const string PrimaryEndpointName = "http";
    internal const int DefaultContainerPort = 8200;
    internal const string DefaultTokenId = "dev-root";
}

public static class VaultServerBuilderExtensions
{
    public static IResourceBuilder<VaultServerResource> AddVaultServer(this IDistributedApplicationBuilder builder,
        string name, int? port = null, string? rootTokenId = null)
    {
        var vaultContainer = new VaultServerResource(name);
        var address = $"0.0.0.0:{VaultServerResource.DefaultContainerPort}";
        var apiAddress = $"http://{address}";
        var args = new List<string> { "server", "-dev", "-dev-no-store-token" };

        return builder.AddResource(vaultContainer)
            .WithImage("hashicorp/vault")
            .WithImageRegistry("docker.io")
            .WithHttpEndpoint(
                port: port ?? VaultServerResource.DefaultContainerPort,
                targetPort: VaultServerResource.DefaultContainerPort,
                name: VaultServerResource.PrimaryEndpointName
            )
            .WithArgs(args.ToArray())
            .WithEnvironment("VAULT_LOG_LEVEL", "info")
            .WithEnvironment("VAULT_DEV_ROOT_TOKEN_ID", rootTokenId ?? VaultServerResource.DefaultTokenId)
            .WithEnvironment("VAULT_API_ADDR", apiAddress)
            .WithEnvironment("VAULT_DEV_LISTEN_ADDRESS", address);
    }

    public static IResourceBuilder<TDestination> WithReference<TDestination>(
        this IResourceBuilder<TDestination> builder, IResourceBuilder<VaultServerResource> source,
        string? rootTokenId = null)
        where TDestination : IResourceWithEnvironment
    {
        builder.WithReference(source as IResourceBuilder<IResourceWithServiceDiscovery>);
        
        return builder.WithEnvironment(ctx =>
        {
            ctx.EnvironmentVariables["VAULT_INSECURE"] = ReferenceExpression.Create($"true");

            var address = $"http://{source.Resource.Name}:{VaultServerResource.DefaultContainerPort}";
            ctx.EnvironmentVariables["VAULT_ADDR"] = ReferenceExpression.Create($"{address}");
            ctx.EnvironmentVariables["VAULT_APP_MOUNT"] = ReferenceExpression.Create($"aspireshop");
            ctx.EnvironmentVariables["VAULT_TOKEN"] =
                ReferenceExpression.Create($"{rootTokenId ?? VaultServerResource.DefaultTokenId}");
        });
    }
}
