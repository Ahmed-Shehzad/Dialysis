using Bogus;
using Dialysis.Alerting.Features.ProcessAlerts;
using Dialysis.AuditConsent.Features.Audit;
using Dialysis.DeviceIngestion.Features.IngestVitals;
using Dialysis.HisIntegration.Features.AdtSync;
using Dialysis.IdentityAdmission.Features.PatientAdmission;
using Dialysis.IdentityAdmission.Features.SessionScheduling;
using Dialysis.Prediction.Handlers;
using Shouldly;
using Verifier;
using Xunit;

namespace Dialysis.Tests;

public sealed class AdtIngestValidatorTests
{
    [Fact]
    public async Task Valid_command_passes()
    {
        var validator = new AdtIngestValidator();
        var cmd = new AdtIngestCommand { MessageType = "ADT-A01", RawMessage = "MSH|^~\\&|HIS|HOSP|||20240115120000||ADT^A01|MSG001|P|2.5" };
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_messageType_fails(string messageType)
    {
        var validator = new AdtIngestValidator();
        var cmd = new AdtIngestCommand { MessageType = messageType, RawMessage = "MSH|^~\\&|HIS" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "MessageType");
    }

    [Fact]
    public void Empty_rawMessage_fails()
    {
        var validator = new AdtIngestValidator();
        var cmd = new AdtIngestCommand { MessageType = "ADT-A01", RawMessage = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "RawMessage");
    }
}

public sealed class IngestVitalsValidatorTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void Valid_command_passes()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Empty_patientId_fails(string? patientId)
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { PatientId = patientId ?? "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "PatientId");
    }

    [Fact]
    public void Empty_encounterId_fails()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { EncounterId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_deviceId_fails()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { DeviceId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Null_readings_fails()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { Readings = null! };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_readings_fails()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { Readings = [] };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}

public sealed class CreateAlertValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_patientId_fails()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate() with { PatientId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_encounterId_fails()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate() with { EncounterId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_code_fails()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate() with { Code = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_severity_fails()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate() with { Severity = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}

public sealed class AcknowledgeAlertValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new AcknowledgeAlertValidator();
        var cmd = BogusFakers.AcknowledgeAlertCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_alertId_fails()
    {
        var validator = new AcknowledgeAlertValidator();
        var cmd = new AcknowledgeAlertCommand { AlertId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}

public sealed class RecordAuditValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new RecordAuditValidator();
        var cmd = BogusFakers.RecordAuditCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_resourceType_fails()
    {
        var validator = new RecordAuditValidator();
        var cmd = BogusFakers.RecordAuditCommandFaker().Generate() with { ResourceType = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_resourceId_fails()
    {
        var validator = new RecordAuditValidator();
        var cmd = BogusFakers.RecordAuditCommandFaker().Generate() with { ResourceId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_action_fails()
    {
        var validator = new RecordAuditValidator();
        var cmd = BogusFakers.RecordAuditCommandFaker().Generate() with { Action = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}

public sealed class AdmitPatientValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new AdmitPatientValidator();
        var cmd = BogusFakers.AdmitPatientCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_mrn_fails()
    {
        var validator = new AdmitPatientValidator();
        var cmd = BogusFakers.AdmitPatientCommandFaker().Generate() with { Mrn = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_familyName_fails()
    {
        var validator = new AdmitPatientValidator();
        var cmd = BogusFakers.AdmitPatientCommandFaker().Generate() with { FamilyName = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Null_givenName_passes()
    {
        var validator = new AdmitPatientValidator();
        var cmd = BogusFakers.AdmitPatientCommandFaker().Generate() with { GivenName = (string?)null };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }
}

public sealed class CreateSessionValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new CreateSessionValidator();
        var cmd = BogusFakers.CreateSessionCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_patientId_fails()
    {
        var validator = new CreateSessionValidator();
        var cmd = BogusFakers.CreateSessionCommandFaker().Generate() with { PatientId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_deviceId_fails()
    {
        var validator = new CreateSessionValidator();
        var cmd = BogusFakers.CreateSessionCommandFaker().Generate() with { DeviceId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}

public sealed class ObservationCreatedValidatorTests
{
    [Fact]
    public void Valid_event_passes()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate();
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_observationId_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { ObservationId = default };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_patientId_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { PatientId = default };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_encounterId_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { EncounterId = default };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_code_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { Code = "" };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_value_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { Value = "" };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }
}
