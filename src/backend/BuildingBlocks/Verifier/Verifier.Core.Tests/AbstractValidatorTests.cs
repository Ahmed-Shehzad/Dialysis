using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Verifier.Tests;

/// <summary>
/// Exercises the Verifier fluent engine end-to-end: string/length/regex/comparable rules,
/// conditional execution (When), short-circuiting (StopOnFirstFailure), custom messages, and
/// failure aggregation into <see cref="ValidationResult{T}"/> / <see cref="ValidationErrors"/>.
/// </summary>
public sealed class AbstractValidatorTests
{
    private sealed record Sample(string? Name, int Age, string? Code);

    private sealed class SampleValidator : AbstractValidator<Sample>
    {
        public SampleValidator()
        {
            RuleFor(static s => s.Name, nameof(Sample.Name))
                .NotEmpty()
                .WithMessage("Name is required.");

            RuleFor(static s => s.Name, nameof(Sample.Name))
                .Length(0, 10)
                .WithMessage("Name too long.");

            RuleFor(static s => s.Age, nameof(Sample.Age))
                .InclusiveBetween(0, 120);

            RuleFor(static s => s.Code, nameof(Sample.Code))
                .Matches("^[A-Z]{3}$")
                .When(static s => s.Code is not null);
        }
    }

    private readonly SampleValidator _sut = new();

    [Fact]
    public async Task Accepts_A_Fully_Valid_Instance_Async()
    {
        var result = await _sut.ValidateAsync(new Sample("Ada", 40, "ABC"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Ada");
    }

    [Fact]
    public async Task Aggregates_Multiple_Failures_Async()
    {
        var result = await _sut.ValidateAsync(new Sample("", 200, "ab"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        // empty name, age out of range, code regex miss.
        result.Error.Count.ShouldBeGreaterThanOrEqualTo(3);
        result.Error.ToDictionary().ShouldContainKey(nameof(Sample.Name));
        result.Error.ToDictionary().ShouldContainKey(nameof(Sample.Age));
        result.Error.ToDictionary().ShouldContainKey(nameof(Sample.Code));
    }

    [Fact]
    public async Task Conditional_Rule_Is_Skipped_When_Predicate_Is_False_Async()
    {
        // Code is null → the Matches rule is gated off by When(...).
        var result = await _sut.ValidateAsync(new Sample("Ada", 40, null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Surfaces_The_Custom_Message_Async()
    {
        var result = await _sut.ValidateAsync(new Sample(null, 40, "ABC"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Select(e => e.ErrorMessage).ShouldContain("Name is required.");
    }

    [Fact]
    public async Task Throws_On_Null_Instance_Async() =>
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _sut.ValidateAsync(null!, CancellationToken.None));
}
