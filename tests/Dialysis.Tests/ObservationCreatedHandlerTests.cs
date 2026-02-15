using Dialysis.Contracts.Events;
using Dialysis.Contracts.Ids;
using Dialysis.Prediction.Handlers;
using Dialysis.Prediction.Services;
using Intercessor.Abstractions;
using NSubstitute;
using Shouldly;
using Transponder.Abstractions;
using Verifier;
using Verifier.Abstractions;
using Verifier.Exceptions;
using Xunit;

namespace Dialysis.Tests;

public sealed class ObservationCreatedHandlerTests
{
    [Fact]
    public async Task HandleAsync_validation_failure_throws()
    {
        var riskScorer = Substitute.For<IRiskScorer>();
        var vitalCache = Substitute.For<IVitalHistoryCache>();
        var validator = Substitute.For<IValidator<ObservationCreated>>();
        var failResult = new ValidationResult();
        failResult.AddError(new ValidationFailure("Value", "Invalid"));
        validator.ValidateAsync(Arg.Any<ObservationCreated>(), Arg.Any<CancellationToken>())
            .Returns(failResult);

        var publishEndpoint = Substitute.For<IPublishEndpoint>();

        var handler = new ObservationCreatedHandler(riskScorer, vitalCache, validator, publishEndpoint);
        var evt = new ObservationCreated(
            Ulid.NewUlid(),
            "default",
            ObservationId.Create("obs-1"),
            PatientId.Create("p1"),
            EncounterId.Create("e1"),
            "8480-6",
            "120",
            DateTimeOffset.UtcNow,
            null);

        await Should.ThrowAsync<ValidationException>(async () => await handler.HandleAsync(evt));
        await publishEndpoint.DidNotReceive().PublishAsync(Arg.Any<HypotensionRiskRaised>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_high_risk_publishes_HypotensionRiskRaised()
    {
        var riskScorer = Substitute.For<IRiskScorer>();
        riskScorer.CalculateRisk(Arg.Any<string>(), Arg.Any<IReadOnlyList<VitalSnapshot>>()).Returns(0.85);

        var vitalCache = new VitalHistoryCache();
        vitalCache.Append("p1", new VitalSnapshot("8480-6", 85, DateTimeOffset.UtcNow));

        var validator = Substitute.For<IValidator<ObservationCreated>>();
        validator.ValidateAsync(Arg.Any<ObservationCreated>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var publishEndpoint = Substitute.For<IPublishEndpoint>();

        var handler = new ObservationCreatedHandler(riskScorer, vitalCache, validator, publishEndpoint);
        var evt = new ObservationCreated(
            Ulid.NewUlid(),
            "default",
            ObservationId.Create("obs-1"),
            PatientId.Create("p1"),
            EncounterId.Create("e1"),
            "8480-6",
            "85",
            DateTimeOffset.UtcNow,
            null);

        await handler.HandleAsync(evt);

        await publishEndpoint.Received(1).PublishAsync(
            Arg.Is<HypotensionRiskRaised>(r => r.PatientId.Value == "p1" && r.RiskScore >= 0.6),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_low_risk_does_not_publish()
    {
        var riskScorer = Substitute.For<IRiskScorer>();
        riskScorer.CalculateRisk(Arg.Any<string>(), Arg.Any<IReadOnlyList<VitalSnapshot>>()).Returns(0.3);

        var vitalCache = new VitalHistoryCache();
        var validator = Substitute.For<IValidator<ObservationCreated>>();
        validator.ValidateAsync(Arg.Any<ObservationCreated>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var publishEndpoint = Substitute.For<IPublishEndpoint>();

        var handler = new ObservationCreatedHandler(riskScorer, vitalCache, validator, publishEndpoint);
        var evt = new ObservationCreated(
            Ulid.NewUlid(),
            "default",
            ObservationId.Create("obs-1"),
            PatientId.Create("p1"),
            EncounterId.Create("e1"),
            "8480-6",
            "120",
            DateTimeOffset.UtcNow,
            null);

        await handler.HandleAsync(evt);

        await publishEndpoint.DidNotReceive().PublishAsync(Arg.Any<HypotensionRiskRaised>(), Arg.Any<CancellationToken>());
    }
}
