using System.Text.Json;
using Nito.AsyncEx;

namespace AspireShop.Frontend;
using Microsoft.Extensions.DependencyInjection;

public class VaultOptions
{
    public string VaultAddress { get; init; }
    public string VaultToken { get; init; }
    public string VaultMount { get; init; }
    public bool AllowInsecure { get; init; }
    public bool PeriodicRefresh { get; init; } = false;
    public int RefreshInterval { get; init; } = 30;

    public IServiceCollection Services { get; init; }
}
public class VaultDevServerConfigurationSource(VaultOptions options) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new VaultDevServerConfigurationProvider(options);
    }
}

public class VaultDevServerConfigurationProvider: ConfigurationProvider
{
    private readonly VaultOptions _options;
    private readonly IServiceProvider _provider;
    private int _currentVersion = -1;

    public VaultDevServerConfigurationProvider(VaultOptions options)
    {
        _options = options;
      _provider = _options.Services.BuildServiceProvider();
    }
    
    public override void Load()
    {
        AsyncContext.Run(LoadAsync);
    }
    
    private async Task LoadAsync()
    {
        if(_options.PeriodicRefresh)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.RefreshInterval));
            while (await timer.WaitForNextTickAsync(CancellationToken.None))
            {
                await LoadFromVaultAsync();
            }
        }
        else
        {
            await LoadFromVaultAsync();
        }
    }

    private async Task LoadFromVaultAsync()
    {
        using var httpClient = _provider.GetService<IHttpClientFactory>().CreateClient("vault");
        var response = await httpClient.GetAsync($"v1/{_options.VaultMount}/data/stripe");
        
        if(response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var payload = JsonDocument.Parse(content);
            
            var version = payload.RootElement.GetProperty("data").GetProperty("metadata").GetProperty("version").GetInt32();
            
            if(version == _currentVersion)
                return;
            
            var dataValues = payload.RootElement.GetProperty("data").GetProperty("data").EnumerateObject();
            
            foreach(var property in dataValues)
            {
                var key = property.Name.ToUpper();
                var value = property.Value.GetString();
                Data[key] = value;
            }
            _currentVersion = version;
            OnReload();
        }
    }
}




public static class ConfigurationManagerExtensions
{
    public static IConfigurationBuilder AddVaultDevServerConfiguration(this IConfigurationBuilder configBuilder, Func<VaultOptions> configureOptions)
    {
        var options = configureOptions();
        
        if(options.VaultAddress == null)
            throw new ArgumentNullException(nameof(options.VaultAddress));
        
        options.Services.AddHttpClient("vault", c =>
        {
            c.BaseAddress = new Uri(options.VaultAddress);
            c.DefaultRequestHeaders.Add("X-Vault-Token", options.VaultToken);
        });
         configBuilder.Add(new VaultDevServerConfigurationSource(options));

        return configBuilder;
    }
}
