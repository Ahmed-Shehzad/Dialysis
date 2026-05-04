namespace Dialysis.BuildingBlocks.Transponder.Transport.AwsSqsSns;

/// <summary>Standard SQS queue ingress (routing key in message attributes using the same names as <c>TransponderTransportHeaderNames</c>).</summary>
public sealed class TransponderAwsSqsOptions
{
    /// <summary>Full queue URL (e.g. https://sqs.us-east-1.amazonaws.com/123/queue-name).</summary>
    public string QueueUrl { get; set; } = string.Empty;

    /// <summary>Optional AWS region system name (e.g. us-east-1). When null, the SDK default region resolution is used.</summary>
    public string? RegionName { get; set; }

    /// <summary>Long polling wait time per receive (max 20).</summary>
    public int WaitTimeSeconds { get; set; } = 20;

    /// <summary>Max messages per receive (max 10).</summary>
    public int MaxNumberOfMessages { get; set; } = 10;
}
