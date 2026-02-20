using System.Text.Json;

using Dialysis.Seeder;

using Refit;

var count = 300;
var gateway = "http://localhost:5001";

var idx = 0;
while (idx < args.Length)
{
    if (args[idx] == "--count" && idx + 1 < args.Length && int.TryParse(args[idx + 1], out int c))
    { count = Math.Clamp(c, 10, 5000); idx += 2; }
    else if (args[idx] == "--gateway" && idx + 1 < args.Length)
    { gateway = args[idx + 1].TrimEnd('/'); idx += 2; }
    else
    { idx++; }
}

Console.WriteLine($"Seeding {count} records to {gateway} (Prescription, Treatment, Alarm)...");

var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
using var httpClient = new HttpClient { BaseAddress = new Uri(gateway.TrimEnd('/') + "/") };
httpClient.DefaultRequestHeaders.Add("X-Tenant-Id", "default");
httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

IGatewaySeederApi api = RestService.For<IGatewaySeederApi>(httpClient, new RefitSettings { ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions) });

var faker = new Bogus.Faker();
faker.Random = new Bogus.Randomizer(42);

int prescriptions = 0, treatments = 0, alarms = 0;
bool loggedRsp = false, loggedOru = false, loggedAlarm = false;
var mrnPool = new HashSet<string>();
var sessionPool = new List<string>();

while (mrnPool.Count < count)
    _ = mrnPool.Add($"MRN{faker.Random.AlphaNumeric(6).ToUpperInvariant()}");

var mrns = mrnPool.Take(count).ToList();
foreach (string mrn in mrns)
    sessionPool.Add($"THERAPY{faker.Random.AlphaNumeric(6).ToUpperInvariant()}");

static string SanitizeHl7(string s) => (s ?? "").Replace("|", " ").Replace("^", " ").Replace("\\", " ").Replace("~", " ");

string RspK22(string mrn, string orderId, string msgId)
{
    string ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    string provider = SanitizeHl7(faker.Name.FullName());
    string phone = SanitizeHl7(faker.Phone.PhoneNumber("###-####"));
    return $"MSH|^~\\&|EMR|FAC|MACH|FAC|{ts}||RSP^K22^RSP_K21|{msgId}|P|2.6\r\n" +
           $"MSA|AA|{msgId}\r\n" +
           $"QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|{mrn}^^^^MR\r\n" +
           $"ORC|NW|{orderId}^FAC|||||{ts}|||PROVIDER^{provider}||{phone}\r\n" +
           $"PID|||{mrn}^^^^MR\r\n" +
           $"OBX|1|NM|12345^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC||{faker.Random.Int(250, 400)}|ml/min||||||||||RSET\r\n" +
           $"OBX|2|NM|12346^MDC_HDIALY_UF_RATE_SETTING^MDC||{faker.Random.Int(400, 600)}|mL/h||||||||||RSET\r\n" +
           $"OBX|3|NM|12347^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC||{faker.Random.Int(1500, 2500)}|mL||||||||||RSET\r\n";
}

string OruR01(string mrn, string sessionId, string msgId, DateTimeOffset ts)
{
    string device = $"{faker.Random.AlphaNumeric(6).ToUpperInvariant()}^EUI64^EUI-64";
    return $"""
        MSH|^~\&|{device}|FAC|PDMS|FAC|{ts:yyyyMMddHHmmss}||ORU^R01^ORU_R01|{msgId}|P|2.6
        PID|||{mrn}^^^^MR
        OBR|1||{sessionId}^MACH^EUI64|||{ts:yyyyMMddHHmmss}||||||start
        OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|{faker.Random.Int(280, 350)}|ml/min^ml/min^UCUM|||||F|||{ts:yyyyMMddHHmmss}|||AMEAS
        OBX|2|NM|158776^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC|1.1.3.2|{faker.Random.Int(100, 180)}|mmHg^mm[Hg]^UCUM|80-200||||F|||{ts.AddMinutes(1):yyyyMMddHHmmss}|||AMEAS
        """;
}

string OruR40(string mrn, string sessionId, string msgId, DateTimeOffset ts)
{
    string device = $"{faker.Random.AlphaNumeric(6).ToUpperInvariant()}_EUI64";
    string priority = faker.PickRandom("high", "medium", "low");
    return $"""
        MSH|^~\&|{device}|FAC|EMR|FAC|{ts:yyyyMMddHHmmss}||ORU^R40^ORU_R40|{msgId}|P|2.6
        PID|||{mrn}^^^^MR
        OBR|1||{sessionId}^MACH^EUI64
        OBX|1|ST|MDC_EVT_HI_VAL_ALARM^12345^MDC|1.1.3.1.1|MDC_PRESS_BLD_ART^150020^MDC|mmHg
        OBX|2|NM|MDC_PRESS_BLD_ART^12345^MDC|1.1.3.1.2|{faker.Random.Int(60, 200)}|mmHg|||{priority}|||{ts:yyyyMMddHHmmss}
        OBX|3|ST|MDC_ATTR_EVT_PHASE^68481^MDC|1.1.3.1.3|start
        OBX|4|ST|MDC_ATTR_ALARM_STATE^68482^MDC|1.1.3.1.4|active
        OBX|5|ST|MDC_ATTR_ALARM_INACTIVATION_STATE^68483^MDC|1.1.3.1.5|enabled
        """;
}

string BuildBatch(IEnumerable<string> messages)
{
    var list = messages.ToList();
    return $"FHS|^~\\&||||||\r\nBHS|^~\\&||||||\r\n{string.Join("\r\n", list)}\r\nBTS|{list.Count}\r\nFTS|1\r\n";
}

const int BatchSize = 25;
int batchCount = (count + BatchSize - 1) / BatchSize;
const string TenantId = "default";

for (int b = 0; b < batchCount; b++)
{
    int start = b * BatchSize;
    int take = Math.Min(BatchSize, count - start);
    var batchMrns = mrns.Skip(start).Take(take).ToList();
    var batchSessions = sessionPool.Skip(start).Take(take).ToList();

    foreach ((string mrn, _) in batchMrns.Zip(batchSessions))
    {
        try
        {
            string msgId = $"MSG{faker.Random.Int(1000, 99999)}";
            var rspReq = new IngestRspK22Request(RspK22(mrn, $"ORD{faker.Random.AlphaNumeric(6).ToUpperInvariant()}", msgId), null);
            HttpResponseMessage r = await api.PostRspK22Async(rspReq, TenantId);
            if (r.IsSuccessStatusCode) prescriptions++;
            else if (!loggedRsp) { loggedRsp = true; string body = await r.Content.ReadAsStringAsync(); Console.Error.WriteLine($"  RSP^K22: {(int)r.StatusCode} {(body.Length > 200 ? body[..200] + "..." : body)}"); }
        }
        catch (Exception ex) when (!loggedRsp) { loggedRsp = true; Console.Error.WriteLine($"  RSP^K22: {ex.Message}"); }
        catch { /* ignore */ }
    }

    var oruMessages = new List<string>();
    var baseTime = DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 30));
    for (int i = 0; i < take; i++)
    {
        string mrn = batchMrns[i];
        string sid = batchSessions[i];
        var ts = baseTime.AddHours(i * 2);
        oruMessages.Add(OruR01(mrn, sid, $"M{start + i:D4}", ts));
    }

    try
    {
        var batchReq = new IngestOruBatchRequest(BuildBatch(oruMessages));
        HttpResponseMessage r = await api.PostOruBatchAsync(batchReq, TenantId);
        if (r.IsSuccessStatusCode) treatments += take;
        else if (!loggedOru) { loggedOru = true; string body = await r.Content.ReadAsStringAsync(); Console.Error.WriteLine($"  ORU batch: {(int)r.StatusCode} {(body.Length > 200 ? body[..200] + "..." : body)}"); }
    }
    catch (Exception ex) when (!loggedOru) { loggedOru = true; Console.Error.WriteLine($"  ORU batch: {ex.Message}"); }
    catch { /* ignore */ }

    for (int i = 0; i < take; i++)
    {
        try
        {
            string mrn = batchMrns[i];
            string sid = batchSessions[i];
            var ts = baseTime.AddHours(i * 2).AddMinutes(faker.Random.Int(5, 90));
            var alarmReq = new IngestAlarmRequest(OruR40(mrn, sid, $"A{start + i:D4}", ts));
            HttpResponseMessage r = await api.PostAlarmAsync(alarmReq, TenantId);
            if (r.IsSuccessStatusCode) alarms++;
            else if (!loggedAlarm) { loggedAlarm = true; string body = await r.Content.ReadAsStringAsync(); Console.Error.WriteLine($"  Alarm: {(int)r.StatusCode} {(body.Length > 200 ? body[..200] + "..." : body)}"); }
        }
        catch (Exception ex) when (!loggedAlarm) { loggedAlarm = true; Console.Error.WriteLine($"  Alarm: {ex.Message}"); }
        catch { /* ignore */ }
    }

    Console.Write($"\r  Prescriptions: {prescriptions}, Treatments: {treatments}, Alarms: {alarms}   ");
}

Console.WriteLine($"\nDone. Seeded {prescriptions} prescriptions, {treatments} treatments, {alarms} alarms.");
if (prescriptions == 0 && treatments == 0 && alarms == 0)
    Console.Error.WriteLine("Tip: Run 'docker compose up -d --build prescription-api treatment-api alarm-api gateway' to rebuild. For 500 errors: docker compose logs prescription-api treatment-api alarm-api");
