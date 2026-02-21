---
name: ASB Guidelines – Next Steps
overview: Phase 3 (Explain) of learn-by-doing: update architecture docs to reflect ASB setup, management libraries, and emulator alignment. Resolve outdated gaps in SOLUTION-REVIEW-REPORT.
todos:
  - id: asb-arch
    content: Update SYSTEM-ARCHITECTURE.md §9 with ASB management and emulator references
    status: completed
  - id: asb-review
    content: Update SOLUTION-REVIEW-REPORT – ASB receive is wired; remove outdated gap
    status: completed
isProject: false
---

# ASB Guidelines – Next Steps

## Context

ASB setup is aligned with Microsoft guidelines:
- Emulator: [test-locally-with-service-bus-emulator](https://learn.microsoft.com/en-us/azure/service-bus-messaging/test-locally-with-service-bus-emulator)
- Management: [service-bus-management-libraries](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-management-libraries)
- Packages: Azure.Messaging.ServiceBus, Azure.ResourceManager.ServiceBus, Azure.Identity

## Phase 3: Explain (Learn-by-Doing)

Update system architecture to reflect implemented reality.

## Tasks

1. **SYSTEM-ARCHITECTURE.md §9 Messaging Flow**
   - Add note on client vs ARM management (per AZURE-SERVICE-BUS.md)
   - Reference emulator and connection string scenarios

2. **SOLUTION-REVIEW-REPORT.md**
   - ASB receive endpoints are wired (Alarm consumes ThresholdBreachDetectedIntegrationEvent via Inbox)
   - Remove or update the "ASB receive endpoints not wired" gap
