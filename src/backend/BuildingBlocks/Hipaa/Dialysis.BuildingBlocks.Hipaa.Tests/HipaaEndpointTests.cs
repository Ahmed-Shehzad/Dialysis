using System.Net;
using System.Text.Json;
using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Hipaa.AspNetCore;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Hipaa.Tests;

public sealed class HipaaEndpointTests
{
    [Fact]
    public async Task Get_Admin_Hipaa_Safeguards_Returns_Snapshot_Json_Async()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s =>
                {
                    s.AddRouting();
                    s.AddHipaaCompliance("test");
                    s.AddHipaaAspNetCoreSafeguards();
                    s.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
                    s.AddSingleton<IAuditEventEmitter, NoOpEmitter>();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapHipaaSafeguardsEndpoint());
                });
            })
            .StartAsync();

        var client = host.GetTestClient();
        var response = await client.GetAsync(new Uri(HipaaEndpointExtensions.SafeguardsRoute, UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var safeguards = doc.RootElement.GetProperty("safeguards").EnumerateArray().ToList();
        Assert.NotEmpty(safeguards);
        Assert.Contains(safeguards, s => s.GetProperty("id").GetString() == "encryption-at-rest");
        Assert.Contains(safeguards, s => s.GetProperty("id").GetString() == "audit-log-emitter");
        Assert.Contains(safeguards, s => s.GetProperty("id").GetString() == "transport-security-hsts");
    }

    private sealed class NoOpEmitter : IAuditEventEmitter
    {
        public ValueTask EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
