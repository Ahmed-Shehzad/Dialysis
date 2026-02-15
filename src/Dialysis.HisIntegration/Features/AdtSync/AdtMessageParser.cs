namespace Dialysis.HisIntegration.Features.AdtSync;

public sealed class AdtMessageParser
{
    public AdtParsedData? Parse(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return null;
        }

        var segments = rawMessage.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToList();

        var msh = segments.FirstOrDefault(s => s.StartsWith("MSH", StringComparison.Ordinal));
        if (msh is null)
        {
            return null;
        }

        var mshFields = msh.Split('|');
        var messageType = mshFields.Length > 8 ? mshFields[8] : "";

        string? mrn = null, familyName = null, givenName = null, birthDate = null, gender = null;
        string? admitDateTime = null, dischargeDateTime = null, encounterId = null, ward = null, attending = null;

        foreach (var seg in segments)
        {
            if (seg.StartsWith("PID|"))
            {
                var pid = seg.Split('|');
                if (pid.Length > 3)
                {
                    var cx = pid[3].Split('^');
                    mrn = cx.Length > 0 ? cx[0] : null;
                }
                if (pid.Length > 5)
                {
                    var name = pid[5].Split('^');
                    familyName = name.Length > 0 ? name[0] : null;
                    givenName = name.Length > 1 ? name[1] : null;
                }
                birthDate = pid.Length > 7 ? pid[7] : null;
                gender = pid.Length > 8 ? pid[8] : null;
            }
            else if (seg.StartsWith("PV1|"))
            {
                var pv1 = seg.Split('|');
                encounterId = pv1.Length > 19 ? pv1[19] : null;
                admitDateTime = pv1.Length > 44 ? pv1[44] : null;
                dischargeDateTime = pv1.Length > 45 ? pv1[45] : null;
                ward = pv1.Length > 3 ? pv1[3] : null;
                attending = pv1.Length > 7 ? pv1[7] : null;
            }
        }

        return new AdtParsedData
        {
            MessageType = messageType,
            Mrn = mrn,
            FamilyName = familyName,
            GivenName = givenName,
            BirthDate = birthDate,
            Gender = gender,
            AdmitDateTime = admitDateTime,
            DischargeDateTime = dischargeDateTime,
            EncounterId = encounterId,
            Ward = ward,
            AttendingPhysician = attending
        };
    }
}
