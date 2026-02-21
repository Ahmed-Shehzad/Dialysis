using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using BuildingBlocks.Testcontainers;

using Dialysis.Treatment.Api.Contracts;

using Shouldly;

using Xunit;

namespace Dialysis.Treatment.Tests;

/// <summary>
/// Controller-level API tests for Treatment endpoints.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class TreatmentControllerApiTests
{
    private readonly PostgreSqlFixture _fixture;

    public TreatmentControllerApiTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Health_ReturnsOkAsync()
    {
        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);

        using TreatmentApiWebApplicationFactory factory = new(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CompleteAndSign_Workflow_SucceedsAsync()
    {
        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);

        using TreatmentApiWebApplicationFactory factory = new(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();

        const string sessionId = "SESS-COMPLETE-SIGN-TEST";
        string oru = TreatmentTestData.OruR01("MRN001", sessionId);

        HttpResponseMessage ingestResponse = await client.PostAsJsonAsync("/api/hl7/oru", new IngestOruMessageRequest(oru));
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage completeResponse = await client.PostAsync($"/api/treatment-sessions/{sessionId}/complete", null);
        completeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage signResponse = await client.PostAsJsonAsync($"/api/treatment-sessions/{sessionId}/sign", new { });
        signResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/treatment-sessions/{sessionId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        SessionResponse? session = await getResponse.Content.ReadFromJsonAsync<SessionResponse>(options);
        session.ShouldNotBeNull();
        session.Status.ShouldBe("Completed");
        session.SignedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task RecordPreAssessment_Workflow_SucceedsAsync()
    {
        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);

        using TreatmentApiWebApplicationFactory factory = new(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();

        const string sessionId = "SESS-PREASSESS-TEST";
        string oru = TreatmentTestData.OruR01("MRN002", sessionId);

        HttpResponseMessage ingestResponse = await client.PostAsJsonAsync("/api/hl7/oru", new IngestOruMessageRequest(oru));
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var preAssessmentRequest = new
        {
            preWeightKg = 72.5m,
            bpSystolic = 120,
            bpDiastolic = 80,
            accessTypeValue = "AVF",
            prescriptionConfirmed = true,
            painSymptomNotes = (string?)null,
            recordedBy = (string?)null
        };

        HttpResponseMessage preAssessResponse = await client.PostAsJsonAsync($"/api/treatment-sessions/{sessionId}/pre-assessment", preAssessmentRequest);
        preAssessResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/treatment-sessions/{sessionId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        SessionWithPreAssessmentResponse? session = await getResponse.Content.ReadFromJsonAsync<SessionWithPreAssessmentResponse>(options);
        session.ShouldNotBeNull();
        session.PreAssessment.ShouldNotBeNull();
        session.PreAssessment.PreWeightKg.ShouldBe(72.5m);
        session.PreAssessment.BpSystolic.ShouldBe(120);
        session.PreAssessment.BpDiastolic.ShouldBe(80);
        session.PreAssessment.AccessTypeValue.ShouldBe("AVF");
        session.PreAssessment.PrescriptionConfirmed.ShouldBeTrue();
    }

    private sealed record SessionResponse(string SessionId, string Status, DateTimeOffset? SignedAt, string? SignedBy);

    private sealed record SessionWithPreAssessmentResponse(
        string SessionId,
        string Status,
        PreAssessmentResponse? PreAssessment);

    private sealed record PreAssessmentResponse(
        decimal? PreWeightKg,
        int? BpSystolic,
        int? BpDiastolic,
        string? AccessTypeValue,
        bool PrescriptionConfirmed,
        string? PainSymptomNotes,
        DateTimeOffset RecordedAt,
        string? RecordedBy);
}
