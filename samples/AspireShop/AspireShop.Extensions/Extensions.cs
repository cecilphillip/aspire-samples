using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;

namespace AspireShop.Extensions;

public static class Extensions
{
    public static IServiceCollection AddStripe(this IServiceCollection services,
        IConfiguration config) {
        StripeConfiguration.ApiKey = config.GetValue<string>("STRIPE_SECRET_KEY");

        var appInfo = new AppInfo { Name = "AspireShop", Version = "0.1.0" };
        StripeConfiguration.AppInfo = appInfo;

        services.AddHttpClient("Stripe");
        services.AddTransient<IStripeClient, StripeClient>(s => {
            var clientFactory = s.GetRequiredService<IHttpClientFactory>();
            var httpClient = new SystemNetHttpClient(
                httpClient: clientFactory.CreateClient("Stripe"),
                maxNetworkRetries: StripeConfiguration.MaxNetworkRetries,
                appInfo: appInfo,
                enableTelemetry: StripeConfiguration.EnableTelemetry);

            return new StripeClient(apiKey: StripeConfiguration.ApiKey, httpClient: httpClient);
        });

        return services;
    }
}
