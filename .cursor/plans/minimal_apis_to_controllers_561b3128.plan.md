---
name: Minimal APIs to Controllers
overview: Refactor all three microservice APIs (Patient, Treatment, Alarm) from minimal API endpoints in Program.cs to thin API controllers, following the "thin controller" pattern where controllers only receive requests, delegate to Intercessor (ISender), and return responses.
todos:
  - id: patient-controller
    content: Create PatientsController in Patient API with GET by MRN, GET search, POST register endpoints
    status: completed
  - id: patient-contracts
    content: Move RegisterPatientRequest to Contracts/ folder in Patient API
    status: completed
  - id: patient-program
    content: "Update Patient API Program.cs: AddControllers, MapControllers, remove minimal API endpoints"
    status: completed
  - id: treatment-controllers
    content: Create TreatmentSessionsController and Hl7Controller in Treatment API
    status: completed
  - id: treatment-contracts
    content: Move IngestOruMessageRequest to Contracts/ folder in Treatment API
    status: completed
  - id: treatment-program
    content: "Update Treatment API Program.cs: AddControllers, MapControllers, remove minimal API endpoints"
    status: completed
  - id: alarm-controller
    content: Create Hl7Controller in Alarm API with POST alarm endpoint
    status: completed
  - id: alarm-contracts
    content: Move IngestOruR40MessageRequest to Contracts/ folder in Alarm API
    status: completed
  - id: alarm-program
    content: "Update Alarm API Program.cs: AddControllers, MapControllers, remove minimal API endpoints"
    status: completed
  - id: build-verify
    content: Build solution and verify no errors
    status: completed
isProject: false
---

# Refactor Minimal APIs to Thin Controllers

## Architecture

Each controller follows the **thin controller** pattern:

1. Receive HTTP request and bind parameters
2. Map to a command/query
3. Forward via `ISender` (Intercessor)
4. Return the appropriate HTTP response

```mermaid
flowchart LR
    HTTP["HTTP Request"] --> Controller["Thin Controller"]
    Controller --> ISender["ISender / Intercessor"]
    ISender --> Handler["Command/Query Handler"]
    Handler --> Domain["Domain / Repository"]
    Domain --> Response["HTTP Response"]
```



## Changes Per Service

### 1. Patient API (`Dialysis.Patient.Api`)

**New file:** `Controllers/PatientsController.cs`

- Route prefix: `api/patients`
- `[ApiController]` attribute
- Inject `ISender` via constructor
- 3 action methods:
  - `GetByMrn(string mrn)` -> `GET api/patients/mrn/{mrn}` -> sends `GetPatientByMrnQuery`
  - `Search(string firstName, string lastName)` -> `GET api/patients/search` -> sends `SearchPatientsQuery`
  - `Register(RegisterPatientRequest request)` -> `POST api/patients` -> sends `RegisterPatientCommand`
- Move `RegisterPatientRequest` record from [Program.cs](Services/Dialysis.Patient/Dialysis.Patient.Api/Program.cs) line 108-113 into a `Contracts/` folder or inline in the controller file

**Modify:** [Program.cs](Services/Dialysis.Patient/Dialysis.Patient.Api/Program.cs)

- Replace `AddEndpointsApiExplorer()` with `AddControllers()`
- Remove all `app.MapGet/MapPost` endpoint definitions (lines 70-104)
- Remove inline `RegisterPatientRequest` record (lines 108-113)
- Add `app.MapControllers()`

### 2. Treatment API (`Dialysis.Treatment.Api`)

**New file:** `Controllers/TreatmentSessionsController.cs`

- Route prefix: `api/treatment-sessions`
- 1 action method:
  - `GetBySessionId(string sessionId)` -> `GET api/treatment-sessions/{sessionId}` -> sends `GetTreatmentSessionQuery`

**New file:** `Controllers/Hl7Controller.cs`

- Route prefix: `api/hl7`
- 1 action method:
  - `IngestOru(IngestOruMessageRequest request)` -> `POST api/hl7/oru` -> sends `IngestOruMessageCommand`
- Move `IngestOruMessageRequest` record from [Program.cs](Services/Dialysis.Treatment/Dialysis.Treatment.Api/Program.cs) line 95 into the controller or a `Contracts/` folder

**Modify:** [Program.cs](Services/Dialysis.Treatment/Dialysis.Treatment.Api/Program.cs)

- Replace `AddEndpointsApiExplorer()` with `AddControllers()`
- Remove all `app.MapGet/MapPost` endpoint definitions (lines 72-91)
- Remove inline `IngestOruMessageRequest` record (line 95)
- Add `app.MapControllers()`

### 3. Alarm API (`Dialysis.Alarm.Api`)

**New file:** `Controllers/Hl7Controller.cs`

- Route prefix: `api/hl7`
- 1 action method:
  - `IngestAlarm(IngestOruR40MessageRequest request)` -> `POST api/hl7/alarm` -> sends `IngestOruR40MessageCommand`
- Move `IngestOruR40MessageRequest` record from [Program.cs](Services/Dialysis.Alarm/Dialysis.Alarm.Api/Program.cs) line 83 into the controller or a `Contracts/` folder

**Modify:** [Program.cs](Services/Dialysis.Alarm/Dialysis.Alarm.Api/Program.cs)

- Replace `AddEndpointsApiExplorer()` with `AddControllers()`
- Remove `app.MapPost` endpoint definition (lines 71-79)
- Remove inline `IngestOruR40MessageRequest` record (line 83)
- Add `app.MapControllers()`

## Shared Pattern

All `Program.cs` files will follow this structure after refactoring:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
// ... service registration (Intercessor, EF Core, repositories, etc.) unchanged ...
// ... health checks unchanged ...

var app = builder.Build();
// ... dev DB migration unchanged ...
// ... exception handler unchanged ...

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapControllers();

app.Run();
```

All controllers follow this pattern:

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class XxxController : ControllerBase
{
    private readonly ISender _sender;
    public XxxController(ISender sender) => _sender = sender;

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(XxxResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(new XxxQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
```

## Route Prefix Convention

All routes will be prefixed with `api/` for consistency (e.g., `/api/patients`, `/api/treatment-sessions`, `/api/hl7`). This is a common convention for API controllers and distinguishes API routes from health/OpenAPI routes.

## Request DTOs

Inline request records currently at the bottom of each `Program.cs` will be moved into `Contracts/` folders within each Api project for clean separation:

- `Dialysis.Patient.Api/Contracts/RegisterPatientRequest.cs`
- `Dialysis.Treatment.Api/Contracts/IngestOruMessageRequest.cs`
- `Dialysis.Alarm.Api/Contracts/IngestOruR40MessageRequest.cs`

## No .csproj Changes Required

All three API projects already use `Microsoft.NET.Sdk.Web`, which includes MVC/controller support out of the box. No additional NuGet packages are needed.
