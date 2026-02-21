using Bogus;

using Dialysis.DataProducerSimulator;

string gateway = "http://localhost:5001";
string tenantId = "default";
int intervalOruSec = 2;
int intervalAlarmSec = 30;
int intervalEmrSec = 60;
int intervalCompleteSec = 120;
bool enableDialysis = true;
bool enableEmr = true;
bool enableEhr = true;
int seedCount = 0;

int idx = 0;
while (idx < args.Length)
{
    if (args[idx] == "--gateway" && idx + 1 < args.Length)
    { gateway = args[idx + 1].TrimEnd('/'); idx += 2; }
    else if (args[idx] == "--tenant" && idx + 1 < args.Length)
    { tenantId = args[idx + 1]; idx += 2; }
    else if (args[idx] == "--interval-oru" && idx + 1 < args.Length && int.TryParse(args[idx + 1], out int ioru))
    { intervalOruSec = Math.Clamp(ioru, 1, 3600); idx += 2; }
    else if (args[idx] == "--interval-alarm" && idx + 1 < args.Length && int.TryParse(args[idx + 1], out int ialm))
    { intervalAlarmSec = Math.Clamp(ialm, 5, 3600); idx += 2; }
    else if (args[idx] == "--interval-emr" && idx + 1 < args.Length && int.TryParse(args[idx + 1], out int iemr))
    { intervalEmrSec = Math.Clamp(iemr, 10, 3600); idx += 2; }
    else if (args[idx] == "--interval-complete" && idx + 1 < args.Length && int.TryParse(args[idx + 1], out int icmp))
    { intervalCompleteSec = Math.Clamp(icmp, 30, 3600); idx += 2; }
    else if (args[idx] == "--enable-dialysis")
    { (enableDialysis, idx) = ParseBoolFlag(args, idx); }
    else if (args[idx] == "--enable-emr")
    { (enableEmr, idx) = ParseBoolFlag(args, idx); }
    else if (args[idx] == "--enable-ehr")
    { (enableEhr, idx) = ParseBoolFlag(args, idx); }
    else if (args[idx] == "--seed" && idx + 1 < args.Length && int.TryParse(args[idx + 1], out int s))
    { seedCount = Math.Max(0, s); idx += 2; }
    else
    { idx++; }
}

static int RoundRobinIndex(int value, int count) => ((value % count) + count) % count;

static (bool value, int newIdx) ParseBoolFlag(string[] a, int idx)
{
    int next = idx + 1;
    if (next >= a.Length || a[next].StartsWith('-')) return (true, idx + 1);
    return (!bool.TryParse(a[next], out bool v) || v, idx + 2);
}

Console.WriteLine("Data Producer Simulator - Continuous HL7 to Gateway");
Console.WriteLine($"  Gateway: {gateway} | Tenant: {tenantId}");
Console.WriteLine($"  Dialysis (ORU^R01): every {intervalOruSec}s | Alarms (ORU^R40): every {intervalAlarmSec}s | Complete (OBR-12 end): every {intervalCompleteSec}s");
Console.WriteLine($"  EMR (QBP^Q22/D01): every {intervalEmrSec}s");
if (enableEhr) Console.WriteLine($"  EHR (RSP^K22 Patient/Prescription): at seed + every {intervalEmrSec}s");
if (seedCount > 0) Console.WriteLine($"  Pre-seed: {seedCount} session(s)");
Console.WriteLine("  Press Ctrl+C to stop.\n");

using var client = new GatewayApiClient(gateway, tenantId);
var faker = new Faker();
faker.Random = new Randomizer(Environment.TickCount);

var mrns = new List<string>();
var sessions = new List<string>();

const int SessionLimit = 50;  // Must match React client; both fetch same API

// Try to use existing sessions from the API (aligns with dashboard dropdown)
var existingSessions = await client.GetTreatmentSessionIdsAsync(SessionLimit).ConfigureAwait(false);
if (existingSessions.Count > 0)
{
    sessions.AddRange(existingSessions.Take(SessionLimit));
    for (int i = mrns.Count; i < sessions.Count; i++)
        mrns.Add($"MRN{faker.Random.AlphaNumeric(6).ToUpperInvariant()}");
    Console.WriteLine($"  Using {sessions.Count} existing session(s) from API (synced with dashboard)");
}
// Fallback: generate our own session pool when no sessions in DB
int targetCount = seedCount > 0 ? seedCount : 20;
while (sessions.Count < targetCount)
{
    mrns.Add($"MRN{faker.Random.AlphaNumeric(6).ToUpperInvariant()}");
    sessions.Add($"THERAPY{faker.Random.AlphaNumeric(6).ToUpperInvariant()}");
}

// Pre-seed: when we created synthetic sessions (none from API), POST ORU^R01 each + EHR ingest
bool hasSyntheticSessions = existingSessions.Count == 0 && sessions.Count > 0;
if (hasSyntheticSessions)
{
    Console.WriteLine($"  Pre-seeding {sessions.Count} session(s)...");

    if (enableEhr)
    {
        int patOk = 0, rxOk = 0;
        for (int i = 0; i < sessions.Count; i++)
        {
            string mrn = mrns[i];
            string msgId = $"SEED{i + 1:D4}";
            if (await client.PostRspK22PatientAsync(Hl7Builders.RspK22Patient(mrn, msgId + "P", faker)).ConfigureAwait(false)) patOk++;
            string orderId = $"ORD{faker.Random.AlphaNumeric(6).ToUpperInvariant()}";
            if (await client.PostRspK22PrescriptionAsync(Hl7Builders.RspK22Prescription(mrn, orderId, msgId + "R", faker)).ConfigureAwait(false)) rxOk++;
        }
        Console.WriteLine($"  EHR ingest: Patient {patOk}/{sessions.Count}, Prescription {rxOk}/{sessions.Count}");
    }

    int seedOk = 0;
    for (int i = 0; i < sessions.Count; i++)
    {
        string mrn = mrns[i];
        string sid = sessions[i];
        string msgId = $"SEED{i + 1:D4}";
        string oru = Hl7Builders.OruR01(mrn, sid, msgId, DateTimeOffset.UtcNow, faker);
        if (await client.PostOruAsync(oru).ConfigureAwait(false))
            seedOk++;
    }
    Console.WriteLine($"  Pre-seeded ORU: {seedOk}/{sessions.Count} ok\n");
}

int oruOk = 0, oruFail = 0;
int alarmOk = 0, alarmFail = 0;
int emrOk = 0, emrFail = 0;
int ehrOk = 0, ehrFail = 0;
int msgSeq = 0;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// Shared session list for sync between dialysis loop and refresh task
var sessionsLock = new object();
var alarmRoundRobin = 0;
var completedSessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

async Task RunSessionRefreshAsync()
{
    var interval = TimeSpan.FromSeconds(60);
    var next = DateTime.UtcNow.Add(interval);
    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            if (DateTime.UtcNow >= next)
            {
                var fresh = await client.GetTreatmentSessionIdsAsync(SessionLimit).ConfigureAwait(false);
                if (fresh.Count > 0)
                {
                    int added;
                    int total;
                    lock (sessionsLock)
                    {
                        var existing = new HashSet<string>(sessions);
                        added = 0;
                        foreach (var s in fresh.Take(SessionLimit))
                        {
                            if (existing.Add(s))
                            {
                                sessions.Add(s);
                                mrns.Add($"MRN{faker.Random.AlphaNumeric(6).ToUpperInvariant()}");
                                added++;
                            }
                        }
                        total = sessions.Count;
                    }
                    if (added > 0) Console.WriteLine($"  [Sync] +{added} new session(s), {total} total");
                }
                next = DateTime.UtcNow.Add(interval);
            }
            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex) { Console.Error.WriteLine($"[Sync] {ex.Message}"); }
    }
}

async Task RunDialysisMachineAsync()
{
    var oruInterval = TimeSpan.FromSeconds(intervalOruSec);
    var alarmInterval = TimeSpan.FromSeconds(intervalAlarmSec);
    var nextOru = DateTime.UtcNow;
    var nextAlarm = DateTime.UtcNow.AddSeconds(intervalAlarmSec / 2.0);

    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            var now = DateTime.UtcNow;

            if (now >= nextOru)
            {
                string[] mrnsCopy;
                string[] sessionsCopy;
                lock (sessionsLock)
                {
                    var active = sessions
                        .Select((s, i) => (Session: s, Mrn: mrns[i]))
                        .Where(x => !completedSessions.Contains(x.Session))
                        .ToList();
                    mrnsCopy = active.Select(x => x.Mrn).ToArray();
                    sessionsCopy = active.Select(x => x.Session).ToArray();
                }
                if (sessionsCopy.Length == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false);
                    continue;
                }
                var oruTasks = new List<Task<bool>>(sessionsCopy.Length);
                for (int j = 0; j < sessionsCopy.Length; j++)
                {
                    string mrn = mrnsCopy[j];
                    string sid = sessionsCopy[j];
                    string msgId = $"ORU{Interlocked.Increment(ref msgSeq):D5}";
                    string eventPhase = "update";
                    string oru = Hl7Builders.OruR01(mrn, sid, msgId, now, faker, eventPhase);
                    oruTasks.Add(client.PostOruAsync(oru, cts.Token));
                }
                var results = await Task.WhenAll(oruTasks).ConfigureAwait(false);
                oruOk += results.Count(r => r);
                oruFail += results.Count(r => !r);
                nextOru = now.Add(oruInterval);
            }

            if (now >= nextAlarm)
            {
                int count = 0;
                int i = 0;
                string[]? alarmSessionsCopy = null;
                string[]? alarmMrnsCopy = null;
                lock (sessionsLock)
                {
                    var active = sessions
                        .Select((s, idx) => (Session: s, Mrn: mrns[idx], Idx: idx))
                        .Where(x => !completedSessions.Contains(x.Session))
                        .ToList();
                    count = active.Count;
                    if (count > 0)
                    {
                        i = RoundRobinIndex(Interlocked.Increment(ref alarmRoundRobin) - 1, count);
                        alarmMrnsCopy = active.Select(x => x.Mrn).ToArray();
                        alarmSessionsCopy = active.Select(x => x.Session).ToArray();
                    }
                }
                if (count == 0 || alarmSessionsCopy == null || alarmMrnsCopy == null) { await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false); continue; }
                string mrn = alarmMrnsCopy[i];
                string sid = alarmSessionsCopy[i];
                string msgId = $"ALM{++msgSeq:D5}";
                string alarm = Hl7Builders.OruR40(mrn, sid, msgId, now, faker);
                if (await client.PostAlarmAsync(alarm, cts.Token).ConfigureAwait(false))
                    alarmOk++;
                else
                    alarmFail++;
                nextAlarm = now.Add(alarmInterval);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex) { Console.Error.WriteLine($"[Dialysis] {ex.Message}"); }
    }
}

async Task RunEmrSimulatorAsync()
{
    int msgSeq = 0;
    var interval = TimeSpan.FromSeconds(intervalEmrSec);
    var next = DateTime.UtcNow;

    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            if (DateTime.UtcNow >= next)
            {
                int mrnCount;
                string mrn;
                lock (sessionsLock)
                {
                    mrnCount = mrns.Count;
                    mrn = mrnCount > 0 ? faker.PickRandom(mrns) : "";
                }
                if (mrnCount == 0) { await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false); continue; }
                string msgId = $"EMR{++msgSeq:D5}";

                bool q22Ok = await client.PostQbpQ22Async(Hl7Builders.QbpQ22(mrn, msgId), cts.Token).ConfigureAwait(false);
                bool d01Ok = await client.PostQbpD01Async(Hl7Builders.QbpD01(mrn, msgId + "b"), cts.Token).ConfigureAwait(false);

                if (q22Ok) emrOk++; else emrFail++;
                if (d01Ok) emrOk++; else emrFail++;

                next = DateTime.UtcNow.Add(interval);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex) { Console.Error.WriteLine($"[EMR] {ex.Message}"); }
    }
}

async Task RunSessionCompletionAsync()
{
    var interval = TimeSpan.FromSeconds(intervalCompleteSec);
    var next = DateTime.UtcNow.Add(interval);

    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            if (DateTime.UtcNow >= next)
            {
                string? mrn = null;
                string? sid = null;
                lock (sessionsLock)
                {
                    var active = sessions
                        .Select((s, i) => (Session: s, Mrn: mrns[i]))
                        .Where(x => !completedSessions.Contains(x.Session))
                        .ToList();
                    if (active.Count > 0)
                    {
                        var pick = faker.PickRandom(active);
                        mrn = pick.Mrn;
                        sid = pick.Session;
                        _ = completedSessions.Add(pick.Session);
                    }
                }
                if (mrn != null && sid != null)
                {
                    string msgId = $"END{Interlocked.Increment(ref msgSeq):D5}";
                    string oru = Hl7Builders.OruR01(mrn, sid, msgId, DateTime.UtcNow, faker, "end");
                    if (await client.PostOruAsync(oru, cts.Token).ConfigureAwait(false))
                        Console.WriteLine($"  [Complete] Session {sid} ended (OBR-12 end)");
                }
                next = DateTime.UtcNow.Add(interval);
            }
            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex) { Console.Error.WriteLine($"[Complete] {ex.Message}"); }
    }
}

async Task RunEhrIngestAsync()
{
    var interval = TimeSpan.FromSeconds(intervalEmrSec);
    var next = DateTime.UtcNow.AddSeconds(intervalEmrSec / 2.0); // Offset from EMR

    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            if (DateTime.UtcNow >= next)
            {
                List<string> mrnsCopy;
                lock (sessionsLock)
                {
                    mrnsCopy = [.. mrns];
                }
                if (mrnsCopy.Count == 0) { await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false); continue; }

                string mrn = faker.PickRandom(mrnsCopy);
                string msgId = $"EHR{faker.Random.AlphaNumeric(8).ToUpperInvariant()}";
                string orderId = $"ORD{faker.Random.AlphaNumeric(6).ToUpperInvariant()}";

                bool patOk = await client.PostRspK22PatientAsync(Hl7Builders.RspK22Patient(mrn, msgId + "P", faker), cts.Token).ConfigureAwait(false);
                bool rxOk = await client.PostRspK22PrescriptionAsync(Hl7Builders.RspK22Prescription(mrn, orderId, msgId + "R", faker), cts.Token).ConfigureAwait(false);

                if (patOk) ehrOk++; else ehrFail++;
                if (rxOk) ehrOk++; else ehrFail++;

                next = DateTime.UtcNow.Add(interval);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex) { Console.Error.WriteLine($"[EHR] {ex.Message}"); }
    }
}

var tasks = new List<Task> { RunSessionRefreshAsync() };
if (enableDialysis)
{
    tasks.Add(RunDialysisMachineAsync());
    tasks.Add(RunSessionCompletionAsync());
}
if (enableEmr) tasks.Add(RunEmrSimulatorAsync());
if (enableEhr) tasks.Add(RunEhrIngestAsync());

if (tasks.Count == 1)
{
    Console.WriteLine("No producers enabled. Use --enable-dialysis, --enable-emr, and/or --enable-ehr (all default true).");
    return 1;
}

var statsTask = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), cts.Token).ConfigureAwait(false);
        if (cts.Token.IsCancellationRequested) break;
        Console.WriteLine($"  [Stats] ORU: {oruOk}/{oruFail} | Alarm: {alarmOk}/{alarmFail} | EMR: {emrOk}/{emrFail} | EHR: {ehrOk}/{ehrFail}");
    }
}, cts.Token);

tasks.Add(statsTask);
await Task.WhenAll(tasks).ConfigureAwait(false);

Console.WriteLine($"\nStopped. ORU: {oruOk}/{oruFail} | Alarm: {alarmOk}/{alarmFail} | EMR: {emrOk}/{emrFail} | EHR: {ehrOk}/{ehrFail}");
return 0;
