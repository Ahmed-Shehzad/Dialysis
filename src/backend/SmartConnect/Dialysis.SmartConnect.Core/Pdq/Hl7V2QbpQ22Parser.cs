using Dialysis.SmartConnect.DataTypes;

namespace Dialysis.SmartConnect.Pdq;

/// <summary>
/// Parses an inbound <c>QBP^Q22^QBP_Q21</c> patient-demographics query into a
/// <see cref="PdqCriteria"/>. Reads the QPD-3 "user parameters" field which carries
/// repeating tokens of the form <c>@PID.x[.y]^value[^^^^id-type]</c> per IHE ITI-21.
/// </summary>
/// <remarks>
/// Supports the criteria exercised by IG Section 4.3:
/// <list type="bullet">
///   <item><c>@PID.3^value^^^^MR</c> → MedicalRecordNumber.</item>
///   <item><c>@PID.3^value^^^^PN</c> → PersonNumber.</item>
///   <item><c>@PID.5.1^value</c> → FamilyName.</item>
///   <item><c>@PID.5.2^value</c> → GivenName.</item>
/// </list>
/// Tokens we don't yet support are silently ignored — the resolver downstream uses what
/// it has. <see cref="PdqCriteria.MessageControlId"/> is sourced from MSH-10 so the
/// downstream <c>RSP^K22</c> builder can echo it into MSA-2.
/// </remarks>
public static class Hl7V2QbpQ22Parser
{
    public static PdqCriteria Parse(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var queryTag = message.GetValue("QPD.2") ?? string.Empty;
        var messageControlId = message.GetValue("MSH.10") ?? string.Empty;

        string? mrn = null;
        string? personNumber = null;
        string? family = null;
        string? given = null;

        // QPD-3 carries the user parameters as repeating @PID.x[.y]^value tokens separated
        // by the message's repeat separator. GetValue with explicit repeat indexes pulls
        // them one at a time until the field is exhausted.
        for (var repeat = 1; repeat <= 32; repeat++)
        {
            var path = message.GetValue($"QPD.3[{repeat}]");
            if (string.IsNullOrEmpty(path))
                break;

            // Each token has up to 6 components: @PID-path ^ value ^ ^ ^ ^ id-type.
            // GetValue with a component index strips the component separator for us.
            var token = message.GetValue($"QPD.3[{repeat}].1");
            var value = message.GetValue($"QPD.3[{repeat}].2");
            var idType = message.GetValue($"QPD.3[{repeat}].6");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(value))
                continue;

            switch (token.ToUpperInvariant())
            {
                case "@PID.3":
                    if (string.Equals(idType, "PN", StringComparison.OrdinalIgnoreCase))
                        personNumber = value;
                    else
                        mrn = value;
                    break;
                case "@PID.5.1":
                    family = value;
                    break;
                case "@PID.5.2":
                    given = value;
                    break;
            }
        }

        return new PdqCriteria(queryTag, messageControlId, mrn, personNumber, family, given);
    }
}
