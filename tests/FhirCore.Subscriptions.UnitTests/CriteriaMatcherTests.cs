using FhirCore.Subscriptions;
using Shouldly;
using Xunit;

namespace FhirCore.Subscriptions.UnitTests;

public sealed class CriteriaMatcherTests
{
    private readonly ICriteriaMatcher _matcher = new CriteriaMatcher();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Empty_or_whitespace_criteria_returns_false(string criteria)
    {
        _matcher.Matches(criteria, "Observation", "x", null).ShouldBeFalse();
    }

    [Fact]
    public void Null_criteria_returns_false()
    {
        _matcher.Matches(null!, "Observation", "x", null).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Observation", "Observation", "abc", true)]
    [InlineData("Patient", "Patient", "123", true)]
    [InlineData("Encounter", "Encounter", "enc-1", true)]
    [InlineData("observation", "Observation", "abc", true)]
    [InlineData("OBSERVATION", "Observation", "abc", true)]
    [InlineData("Observation", "Patient", "123", false)]
    [InlineData("Encounter", "Observation", "x", false)]
    public void Matches_resource_type_case_insensitive(string criteria, string resourceType, string resourceId, bool expected)
    {
        _matcher.Matches(criteria, resourceType, resourceId, null).ShouldBe(expected);
    }

    [Fact]
    public void Criteria_with_params_and_null_search_context_returns_false()
    {
        _matcher.Matches("Observation?patient=123", "Observation", "obs1", null).ShouldBeFalse();
    }

    [Fact]
    public void Criteria_with_empty_query_after_question_mark_returns_true()
    {
        _matcher.Matches("Observation?", "Observation", "obs1", null).ShouldBeTrue();
        _matcher.Matches("Observation? ", "Observation", "obs1", null).ShouldBeTrue();
    }

    [Theory]
    [InlineData("123", "123", true)]
    [InlineData("Patient/123", "123", true)]
    [InlineData("Patient/456", "456", true)]
    [InlineData("123", "456", false)]
    public void Matches_patient_param_various_formats(string ctxValue, string criteriaValue, bool expected)
    {
        var ctx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["patient"] = ctxValue };
        _matcher.Matches($"Observation?patient={criteriaValue}", "Observation", "obs1", ctx).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Patient/456", "456", true)]
    [InlineData("Patient/456", "Patient/456", true)]
    [InlineData("456", "456", true)]
    public void Matches_subject_param(string ctxValue, string criteriaValue, bool expected)
    {
        var ctx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["subject"] = ctxValue };
        _matcher.Matches($"Observation?subject={criteriaValue}", "Observation", "obs1", ctx).ShouldBe(expected);
    }

    [Fact]
    public void Patient_and_subject_are_aliases()
    {
        var ctxPatient = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["patient"] = "123" };
        var ctxSubject = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["subject"] = "Patient/123" };
        _matcher.Matches("Observation?patient=123", "Observation", "obs1", ctxPatient).ShouldBeTrue();
        _matcher.Matches("Observation?subject=123", "Observation", "obs1", ctxSubject).ShouldBeTrue();
        _matcher.Matches("Observation?patient=123", "Observation", "obs1", ctxSubject).ShouldBeTrue();
        _matcher.Matches("Observation?subject=123", "Observation", "obs1", ctxPatient).ShouldBeTrue();
    }

    [Fact]
    public void Does_not_match_wrong_patient()
    {
        var ctx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["patient"] = "123" };
        _matcher.Matches("Observation?patient=999", "Observation", "obs1", ctx).ShouldBeFalse();
    }

    [Fact]
    public void Missing_param_in_context_returns_false()
    {
        var ctx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["other"] = "x" };
        _matcher.Matches("Observation?patient=123", "Observation", "obs1", ctx).ShouldBeFalse();
    }

    [Fact]
    public void ParseQueryString_url_decoded_values()
    {
        var ctx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["patient"] = "p-123" };
        _matcher.Matches("Observation?patient=p-123", "Observation", "obs1", ctx).ShouldBeTrue();
    }

    [Fact]
    public void Multiple_params_both_must_match()
    {
        var ctx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["patient"] = "123",
            ["encounter"] = "e1"
        };
        _matcher.Matches("Observation?patient=123&encounter=e1", "Observation", "obs1", ctx).ShouldBeTrue();
        _matcher.Matches("Observation?patient=123&encounter=e2", "Observation", "obs1", ctx).ShouldBeFalse();
    }

    [Fact]
    public void Empty_resource_id_allowed()
    {
        _matcher.Matches("Observation", "Observation", "", null).ShouldBeTrue();
    }
}
