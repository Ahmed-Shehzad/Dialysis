using System.Net;

using BuildingBlocks.ExceptionHandling;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Moq;

using Shouldly;

using Xunit;

namespace BuildingBlocks.ExceptionHandling.Tests;

public sealed class CentralExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ValidationException_Returns400ProblemDetailsAsync()
    {
        var ex = new Verifier.Exceptions.ValidationException(
            new List<Verifier.ValidationFailure> { new("Name", "Required") });
        var (context, mockSender) = CreateContext();

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Production");

        var handler = new CentralExceptionHandler(env.Object, mockSender.Object);
        bool handled = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(400);
        (context.Response.ContentType ?? "").ShouldContain("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_InProduction_SendsEmailReportAsync()
    {
        var ex = new InvalidOperationException("Test");
        var (context, mockSender) = CreateContext();

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Production");

        mockSender.Setup(s => s.SendAsync(It.IsAny<ExceptionReport>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CentralExceptionHandler(env.Object, mockSender.Object);
        await handler.TryHandleAsync(context, ex, CancellationToken.None);

        mockSender.Verify(
            s => s.SendAsync(It.Is<ExceptionReport>(r =>
                r.Exception.Type.Contains("InvalidOperationException") &&
                r.Exception.Message == "Test" &&
                r.Response.StatusCode == 500),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_InDevelopment_DoesNotSendEmailAsync()
    {
        var ex = new InvalidOperationException("Test");
        var (context, mockSender) = CreateContext();

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Development");

        var handler = new CentralExceptionHandler(env.Object, mockSender.Object);
        await handler.TryHandleAsync(context, ex, CancellationToken.None);

        mockSender.Verify(
            s => s.SendAsync(It.IsAny<ExceptionReport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static (DefaultHttpContext Context, Mock<IExceptionReportEmailSender> Sender) CreateContext()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Production");
        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IWebHostEnvironment))).Returns(env.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/patients";
        context.Request.Method = "GET";
        context.TraceIdentifier = "trace-123";
        context.Response.Body = new MemoryStream();
        context.RequestServices = services.Object;
        return (context, new Mock<IExceptionReportEmailSender>());
    }
}
