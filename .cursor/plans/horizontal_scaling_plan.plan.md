---
name: Horizontal Scaling
overview: Enable horizontal scaling of PDMS APIs by adding SignalR Redis backplane, documenting scaling approach, and providing docker-compose scale example.
todos:
  - id: hs-signalr
    content: Add SignalR Redis backplane to Treatment and Alarm when Redis configured
    status: completed
  - id: hs-docs
    content: Document horizontal scaling in SYSTEM-ARCHITECTURE.md
    status: completed
  - id: hs-compose
    content: Add docker-compose.scale.yml example
    status: completed
isProject: false
---

# Horizontal Scaling Plan

## Context

PDMS APIs are stateless except SignalR (Treatment, Alarm). To scale horizontally:
1. SignalR requires a backplane (Redis or Azure SignalR) so multiple instances share connection state
2. FHIR subscriptions must use FhirDb (PostgreSQL) not InMemorySubscriptionStore
3. Load balancer can distribute traffic; no sticky sessions needed with Redis backplane

## Implementation

### 1. SignalR Redis Backplane

When `ConnectionStrings:Redis` is set, chain `.AddStackExchangeRedis()` after `AddSignalR()` in Treatment and Alarm APIs. Package: `Microsoft.AspNetCore.SignalR.StackExchangeRedis`.

### 2. Documentation

Add §15 Horizontal Scaling to SYSTEM-ARCHITECTURE.md with:
- Prerequisites (Redis backplane, FhirDb)
- Scaling diagram
- Load balancer notes

### 3. Docker Compose Scale

Example `docker-compose.scale.yml` that uses `deploy.replicas` for stateless APIs.
