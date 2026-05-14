using Dialysis.BuildingBlocks.Fhir.Mapping;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir;

/// <summary>
/// Configuration root for <see cref="FhirServiceCollectionExtensions.AddFhir"/>. Modules call
/// <c>fhir.AddReader&lt;TResource, TReader&gt;()</c>, <c>fhir.AddSearcher&lt;...&gt;()</c>,
/// <c>fhir.AddMapper&lt;...&gt;()</c> to opt in.
/// </summary>
public sealed class FhirBuilder
{
    private readonly FhirResourceRegistry _registry;

    internal FhirBuilder(IServiceCollection services, FhirResourceRegistry registry)
    {
        Services = services;
        this._registry = registry;
    }

    public IServiceCollection Services { get; }

    public string BaseUrl { get; private set; } = "/fhir";

    public FhirBuilder UseBaseUrl(string baseUrl)
    {
        BaseUrl = baseUrl;
        return this;
    }

    public FhirBuilder AddMapper<TMapper>() where TMapper : class
    {
        Services.AddScoped<TMapper>();
        foreach (var mapperInterface in typeof(TMapper).GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFhirResourceMapper<,>)))
        {
            Services.AddScoped(mapperInterface, sp => sp.GetRequiredService<TMapper>());
        }
        return this;
    }

    public FhirBuilder AddReader<TResource, TReader>()
        where TResource : Resource, new()
        where TReader : class, IFhirReader<TResource>
    {
        Services.AddScoped<IFhirReader<TResource>, TReader>();
        _registry.RegisterReader<TResource>();
        return this;
    }

    public FhirBuilder AddSearcher<TResource, TSearcher>()
        where TResource : Resource, new()
        where TSearcher : class, IFhirSearcher<TResource>
    {
        Services.AddScoped<IFhirSearcher<TResource>, TSearcher>();
        _registry.RegisterSearcher<TResource>();
        return this;
    }

    public FhirBuilder UseConsentGate<TGate>() where TGate : class, IFhirConsentGate
    {
        Services.RemoveAll<IFhirConsentGate>();
        Services.AddScoped<IFhirConsentGate, TGate>();
        return this;
    }

    public FhirBuilder RequireProfile<TResource>(string profileUrl) where TResource : Resource, new()
    {
        _registry.RegisterProfile<TResource>(profileUrl);
        return this;
    }
}
