using Dialysis.CQRS;
using Dialysis.EHR.Registration.Features.GetPatientsByIds;
using Dialysis.EHR.Registration.Features.RegisterPatient;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Registration;

/// <summary>
/// The batch identity lookup that backs the SPA's N+1-free patient-label resolver: many ids resolve in
/// one query, unknown ids are simply absent, and an empty request is a no-op.
/// </summary>
[Collection(nameof(EhrFixtureCollection))]
public sealed class GetPatientsByIdsQueryTests
{
    private readonly EhrApiWebApplicationFactory _factory;
    public GetPatientsByIdsQueryTests(EhrApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Resolves_Many_Ids_In_One_Query_And_Skips_Unknown_Async()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var mrnA = $"MRN-{Guid.NewGuid():N}";
        var idA = await gateway.SendCommandAsync<RegisterPatientCommand, Guid>(
            new RegisterPatientCommand(mrnA, "Bell", "Marcus", null, new DateOnly(1968, 3, 14), "M", "en"));
        var idB = await gateway.SendCommandAsync<RegisterPatientCommand, Guid>(
            new RegisterPatientCommand($"MRN-{Guid.NewGuid():N}", "Lee", "Ada", null, new DateOnly(1980, 1, 1), "F", "en"));

        var labels = await gateway.SendQueryAsync<GetPatientsByIdsQuery, IReadOnlyList<PatientLabelDto>>(
            new GetPatientsByIdsQuery([idA, idB, Guid.NewGuid()]), CancellationToken.None);

        labels.Count.ShouldBe(2);
        labels.ShouldContain(l => l.Id == idA && l.GivenName == "Marcus" && l.FamilyName == "Bell" && l.MedicalRecordNumber == mrnA);
        labels.ShouldContain(l => l.Id == idB && l.GivenName == "Ada");
    }

    [Fact]
    public async Task Empty_Ids_Returns_Empty_Async()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var labels = await gateway.SendQueryAsync<GetPatientsByIdsQuery, IReadOnlyList<PatientLabelDto>>(
            new GetPatientsByIdsQuery([]), CancellationToken.None);

        labels.ShouldBeEmpty();
    }
}
