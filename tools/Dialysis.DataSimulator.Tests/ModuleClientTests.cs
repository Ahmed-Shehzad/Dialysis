using System.Net;
using System.Text;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace Dialysis.DataSimulator.Tests;

public sealed class ModuleClientTests
{
    [Fact]
    public async Task Register_Patient_Posts_To_The_Clinical_Endpoint_And_Reads_The_Id_Async()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler($"{{\"id\":\"{id}\"}}");
        var client = NewClient(handler);
        var ehr = new EhrClient(client);

        var patient = new GeneratedPatient("SIM-1", "Doe", "Jane", new DateOnly(1980, 1, 1), "F", Guid.NewGuid(), false, "MED");
        var result = await ehr.RegisterPatientAsync(patient, CancellationToken.None);

        result.ShouldBe(id);
        handler.LastRequestUri!.AbsolutePath.ShouldEndWith("/api/v1.0/clinical/patients");
        handler.LastBody.ShouldContain("medicalRecordNumber");
        handler.LastBody.ShouldContain("SIM-1");
    }

    [Fact]
    public async Task Hie_Upload_Reads_The_Document_Id_From_The_Envelope_Async()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler($"{{\"data\":{{\"documentId\":\"{id}\"}},\"links\":[]}}");
        var client = NewClient(handler);
        var hie = new HieClient(client);

        var result = await hie.UploadDocumentAsync(Guid.NewGuid(), "VisitSummary", "Visit Summary", "text/plain", Encoding.UTF8.GetBytes("x"), CancellationToken.None);

        result.ShouldBe(id);
        handler.LastBody.ShouldContain("base64Content");
    }

    [Fact]
    public async Task Book_Appointment_Reads_The_Id_From_The_Data_Envelope_Async()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler($"{{\"data\":{{\"id\":\"{id}\"}},\"links\":[]}}");
        var his = new HisClient(NewClient(handler));

        var result = await his.BookAppointmentAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), CancellationToken.None);

        result.ShouldBe(id);
        handler.LastRequestUri!.AbsolutePath.ShouldEndWith("/api/v1.0/scheduling/appointments");
    }

    private static HttpClient NewClient(StubHandler handler) =>
        new(handler) { BaseAddress = new Uri("http://localhost") };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public StubHandler(string responseJson) => _responseJson = responseJson;

        public Uri? LastRequestUri { get; private set; }

        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            _ = JsonDocument.Parse(_responseJson);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
            };
        }
    }
}
