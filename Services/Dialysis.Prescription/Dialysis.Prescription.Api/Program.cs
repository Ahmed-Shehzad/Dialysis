using Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;

using Intercessor;
using Intercessor.Abstractions;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using Verifier.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(typeof(GetPrescriptionByMrnQuery).Assembly);
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var errors = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
            await context.Response.WriteAsJsonAsync(new { errors });
            return;
        }
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    });
});

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });

app.MapGet("/prescriptions/{mrn}", async (string mrn, ISender sender, CancellationToken ct) =>
{
    var query = new GetPrescriptionByMrnQuery(mrn);
    var response = await sender.SendAsync(query, ct);
    return response is null ? Results.NotFound() : Results.Ok(response);
})
.WithName("GetPrescriptionByMrn")
.WithTags("Prescription")
.Produces<GetPrescriptionByMrnResponse>()
.Produces(404);

await app.RunAsync();
