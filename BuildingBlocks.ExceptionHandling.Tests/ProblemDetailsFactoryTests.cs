using BuildingBlocks.ExceptionHandling;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Verifier;
using Verifier.Exceptions;

using Shouldly;

using Xunit;

namespace BuildingBlocks.ExceptionHandling.Tests;

public sealed class ProblemDetailsFactoryTests
{
    [Fact]
    public void Create_ValidationException_Returns400WithErrors()
    {
        var failures = new List<ValidationFailure> { new("Name", "Required") };
        var ex = new ValidationException(failures);
        DefaultHttpContext context = CreateHttpContext("/api/patients");

        (ProblemDetails problem, int statusCode) = ProblemDetailsFactory.Create(ex, context, includeStackTrace: false);

        statusCode.ShouldBe(400);
        problem.Status.ShouldBe(400);
        problem.Detail.ShouldContain("Required");
        problem.Extensions.ShouldNotContainKey("stackTrace");
    }

    [Fact]
    public void Create_ValidationException_WithStackTrace_IncludesStackTrace()
    {
        var failures = new List<ValidationFailure> { new("Name", "Required") };
        var ex = new ValidationException(failures);
        DefaultHttpContext context = CreateHttpContext("/api/patients");

        (ProblemDetails problem, _) = ProblemDetailsFactory.Create(ex, context, includeStackTrace: true);

        problem.Extensions.ShouldContainKey("stackTrace");
    }

    [Fact]
    public void Create_ArgumentException_Returns400()
    {
        var ex = new ArgumentException("Invalid id");
        DefaultHttpContext context = CreateHttpContext("/api/patients/123");

        (ProblemDetails problem, int statusCode) = ProblemDetailsFactory.Create(ex, context, includeStackTrace: false);

        statusCode.ShouldBe(400);
        problem.Status.ShouldBe(400);
        problem.Detail.ShouldBe("Invalid id");
    }

    [Fact]
    public void Create_UnhandledException_Returns500()
    {
        var ex = new InvalidOperationException("Connection refused");
        DefaultHttpContext context = CreateHttpContext("/api/patients");

        (ProblemDetails problem, int statusCode) = ProblemDetailsFactory.Create(ex, context, includeStackTrace: false);

        statusCode.ShouldBe(500);
        problem.Status.ShouldBe(500);
        problem.Detail.ShouldBe("Connection refused");
        problem.Extensions.ShouldNotContainKey("stackTrace");
    }

    [Fact]
    public void Create_UnhandledException_WithStackTrace_IncludesStackTrace()
    {
        var ex = new InvalidOperationException("Connection refused");
        DefaultHttpContext context = CreateHttpContext("/api/patients");

        (ProblemDetails problem, _) = ProblemDetailsFactory.Create(ex, context, includeStackTrace: true);

        problem.Extensions.ShouldContainKey("stackTrace");
    }

    [Fact]
    public void ToJson_SerializesToValidJson()
    {
        var problem = new ProblemDetails
        {
            Type = "https://example.com/error",
            Title = "Test",
            Status = 500,
            Detail = "Test detail",
        };

        string json = ProblemDetailsFactory.ToJson(problem);

        json.ShouldContain("test");
        json.ShouldContain("500");
        json.ShouldContain("Test detail");
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.TraceIdentifier = "trace-123";
        return context;
    }
}
