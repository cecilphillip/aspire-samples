namespace AspireShop.AppHost.Resources;

public static class DotEnvReader
{
    public static IDictionary<string,string> Read(string filePath)
    {
        var envDictionary = new Dictionary<string, string>();
        
        if (!File.Exists(filePath))
            return envDictionary;

        foreach (var line in  File.ReadAllLines(filePath))
        {
            var setting = line.Split('=',StringSplitOptions.RemoveEmptyEntries);

            if (setting.Length != 2)
                continue;

            envDictionary[setting[0]] = setting[1];
        }

        return envDictionary;
    }
}

public class EnvironmentVariablesResource : Resource
{
    private string EnvFilePath { get; }
    public IDictionary<string, string> EnvironmentVariables { get; }

    public EnvironmentVariablesResource(string name, string path = ".env"): base(name)
    {
        EnvFilePath =NormalizePath(path);
        EnvironmentVariables = DotEnvReader.Read(EnvFilePath);
    }
    
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        path = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(path);
    }
}

public static class EnvironmentVariablesBuilderExtensions
{
    public static IResourceBuilder<EnvironmentVariablesResource> AddEnvironmentVariables(this IDistributedApplicationBuilder builder, string name, string filePath)
    {
        return builder.AddResource(new EnvironmentVariablesResource(name, filePath))
            .ExcludeFromManifest();
    }

    public static IResourceBuilder<TDestination> WithReference<TDestination>(
        this IResourceBuilder<TDestination> builder, IResourceBuilder<EnvironmentVariablesResource> source)
        where TDestination : IResourceWithEnvironment
    {
        return builder.WithEnvironment(ctx =>
        {
            foreach (var (key, value) in source.Resource.EnvironmentVariables)
            {
                ctx.EnvironmentVariables[key] = value;
            }
        });
    }
}


