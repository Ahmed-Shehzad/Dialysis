using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Integration.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Integration;

public static class IntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddHisIntegrationStubs(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LaboratoryGatewayOptions>(configuration.GetSection("His:Laboratory"));
        services.Configure<PharmacyGatewayOptions>(configuration.GetSection("His:Pharmacy"));
        services.AddHttpClient(
            "his-laboratory",
            (sp, client) =>
            {
                var o = sp.GetRequiredService<IOptionsMonitor<LaboratoryGatewayOptions>>().CurrentValue;
                if (Uri.TryCreate(o.BaseUri, UriKind.Absolute, out var uri))
                    client.BaseAddress = uri;
            });
        services.AddHttpClient(
            "his-pharmacy",
            (sp, client) =>
            {
                var o = sp.GetRequiredService<IOptionsMonitor<PharmacyGatewayOptions>>().CurrentValue;
                if (Uri.TryCreate(o.BaseUri, UriKind.Absolute, out var uri))
                    client.BaseAddress = uri;
            });

        services.AddSingleton(new SlidingWindowRateLimiter(120, TimeSpan.FromMinutes(1)));
        services.AddSingleton<ILaboratoryGateway>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<LaboratoryGatewayOptions>>().Value;
            if (string.IsNullOrWhiteSpace(o.BaseUri))
                return new LaboratoryGatewayStub();
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new HttpLaboratoryGateway(factory.CreateClient("his-laboratory"));
        });
        services.AddSingleton<IPharmacyGateway>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<PharmacyGatewayOptions>>().Value;
            if (string.IsNullOrWhiteSpace(o.BaseUri))
                return new PharmacyGatewayStub();
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new HttpPharmacyGateway(factory.CreateClient("his-pharmacy"));
        });
        services.AddSingleton<IOtherHisGateway, OtherHisGatewayStub>();
        return services;
    }
}
