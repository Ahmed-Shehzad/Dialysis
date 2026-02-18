namespace Transponder.Serilog;

/// <summary>
/// Configures Serilog message scope enrichment.
/// </summary>
public sealed class TransponderSerilogOptions
{
    public string OperationPropertyName { get; set; } = "transponder.operation";

    public string MessageIdPropertyName { get; set; } = "transponder.message_id";

    public string CorrelationIdPropertyName { get; set; } = "transponder.correlation_id";

    public string ConversationIdPropertyName { get; set; } = "transponder.conversation_id";

    public string MessageTypePropertyName { get; set; } = "transponder.message_type";

    public string SourceAddressPropertyName { get; set; } = "transponder.source_address";

    public string DestinationAddressPropertyName { get; set; } = "transponder.destination_address";

    public string SentTimePropertyName { get; set; } = "transponder.sent_time";
}
