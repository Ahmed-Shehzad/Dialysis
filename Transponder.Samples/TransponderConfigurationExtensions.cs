using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Transponder.Samples;

public static class TransponderConfigurationExtensions
{
    public static TransponderSettings AddTransponderSettings(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "TransponderSettings")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection(sectionName);

        _ = services.AddOptions<TransponderSettings>()
            .Bind(section);
        _ = services.AddSingleton(sp => sp.GetRequiredService<IOptions<TransponderSettings>>().Value);

        return section.Get<TransponderSettings>() ?? new TransponderSettings();
    }
}
