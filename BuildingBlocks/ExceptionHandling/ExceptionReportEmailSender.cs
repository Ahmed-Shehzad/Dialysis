using System.Net;
using System.Text;
using System.Text.Json;

using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

namespace BuildingBlocks.ExceptionHandling;

#pragma warning disable IDE0058 // Expression value is never used - MailKit API calls

/// <summary>
/// Sends exception reports via SMTP to the development email.
/// When not configured (Enabled=false or DevelopmentEmail empty), returns immediately without sending.
/// </summary>
public sealed class ExceptionReportEmailSender : IExceptionReportEmailSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ExceptionReportEmailOptions _options;
    private readonly ILogger<ExceptionReportEmailSender> _logger;

    public ExceptionReportEmailSender(
        IOptions<ExceptionReportEmailOptions> options,
        ILogger<ExceptionReportEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(ExceptionReport report, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.DevelopmentEmail))
        {
            _logger.LogDebug("Exception report email skipped: not configured");
            return;
        }

        try
        {
            string json = JsonSerializer.Serialize(report, JsonOptions);
            string textBody = BuildStandardTextBody(report);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromDisplayName, _options.FromAddress));
            message.To.Add(MailboxAddress.Parse(_options.DevelopmentEmail));
            message.Subject = $"[Dialysis PDMS] Exception Report - {report.Exception.Type} at {report.OccurredAt:yyyy-MM-dd HH:mm:ss}Z";

            var builder = new BodyBuilder
            {
                TextBody = textBody,
                HtmlBody = null,
            };
            builder.Attachments.Add("exception-report.json", Encoding.UTF8.GetBytes(json));
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            var secureSocketOptions = _options.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.SmtpUser) && !string.IsNullOrWhiteSpace(_options.SmtpPassword))
            {
                var credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword);
                await client.AuthenticateAsync(credentials, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Exception report sent to {Email}", _options.DevelopmentEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send exception report email to {Email}", _options.DevelopmentEmail);
        }
    }

    private static string BuildStandardTextBody(ExceptionReport report)
    {
        string exceptionJson = JsonSerializer.Serialize(report.Exception, JsonOptions);

        var sb = new StringBuilder();
        sb.AppendLine("=== Dialysis PDMS Exception Report ===");
        sb.AppendLine();
        sb.AppendLine($"OccurredAt:    {report.OccurredAt:yyyy-MM-dd HH:mm:ss}Z");
        sb.AppendLine($"Environment:   {report.Environment}");
        sb.AppendLine($"Request:       {report.Request.Method} {report.Request.Path}{report.Request.QueryString}");
        sb.AppendLine($"Response:      {report.Response.StatusCode}");
        sb.AppendLine();
        sb.AppendLine("--- Exception (serialized) ---");
        sb.AppendLine(exceptionJson);
        sb.AppendLine();
        sb.AppendLine("Full report in exception-report.json attachment.");
        return sb.ToString();
    }
}
