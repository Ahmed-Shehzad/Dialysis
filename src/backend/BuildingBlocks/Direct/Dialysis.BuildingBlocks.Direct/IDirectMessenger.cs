namespace Dialysis.BuildingBlocks.Direct;

public interface IDirectMessenger
{
    ValueTask SendAsync(DirectMessage message, CancellationToken cancellationToken);
}

public interface IDirectInboundReceiver
{
    ValueTask<DirectMessage> ReceiveAsync(byte[] envelope, CancellationToken cancellationToken);
}

/// <summary>
/// Module-supplied notification adapters. The building block ships only the interfaces; hosts wire
/// concrete SendGrid / Twilio / etc. implementations.
/// </summary>
public interface IEmailNotifier
{
    ValueTask SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken);
}

public interface ISmsNotifier
{
    ValueTask SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken);
}
