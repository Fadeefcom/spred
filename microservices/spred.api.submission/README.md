# SubmissionService – README

## Overview

**SubmissionService** is a microservice responsible for managing music submissions within the **Spred** platform.  
It implements a **CQRS** (Command/Query Responsibility Segregation) pattern using **MediatR**, supports event-driven integration via the **Outbox pattern**, and guarantees traceability with an **activity/audit pipeline**.  

Artists can submit tracks to curators’ playlists, and curators can manage and review these submissions.  
The service persists all data in **Azure Cosmos DB** with transactional batches to ensure strong consistency.

---

## Goals of the Project

- Allow **artists** to submit tracks to curators for review.  
- Enable **curators** to manage incoming submissions and update statuses.  
- Provide **audit logging** with before/after snapshots and dual ownership (artist + curator).  
- Guarantee **idempotency** and **consistency** with Cosmos DB transactional batches and unique key constraints.  
- Ensure **reliability** through the **Outbox pattern** and background publishing.  
- Expose RESTful endpoints protected by **JWT-based authorization policies**.  

---

## Project Structure

```
SubmissionService
│
├── Abstractions/              # Interfaces (IAuditableCommand, ITrackService, ICatalogService)
├── Components/
│   ├── Handlers/
│   │   ├── CommandHandlers/   # MediatR command handlers (CreateSubmission, UpdateSubmissionStatus)
│   │   ├── QueryHandlers/     # MediatR query handlers (GetSubmissionsByCatalog, GetMySubmissions, GetSubmissionById)
│   │   └── ActivityBehavior/  # MediatR pipeline behavior for audit logging
│   └── Workers/               # Background workers (OutboxWorker)
├── Configurations/            # AutoMapper profiles
├── DependencyExtensions/      # DI registration
├── Models/
│   ├── Commands/              # CQRS commands & results
│   ├── Entities/              # Domain entities (Submission, OutboxEvent, ArtistInbox)
│   ├── Queries/               # CQRS queries
│   ├── Requests/              # API request DTOs
│   └── Models/                # Response DTOs (SubmissionDto)
└── Routes/                    # Minimal API route mappings
```

---

## Endpoints

All endpoints are grouped under `/submissions` and require **JWT user authorization**.

### 1. Create Submission
```
POST /submissions/
```
- **Request:** `CreateSubmissionRequest` (CuratorUserId, CatalogItemId, TrackId)  
- **Response:** `201 Created` with submission ID and creation timestamp.  

### 2. Update Submission Status
```
PATCH /submissions/{catalogId}/{id}/status
```
- **Request:** `UpdateSubmissionStatusRequest` (ArtistId, NewStatus)  
- **Response:** `204 No Content` on success, `400 Bad Request` if invalid status.  

### 3. Get Submissions by Catalog
```
GET /submissions/{catalogId}?status={status}
```
- Retrieves submissions for a specific catalog.  
- Supports **status filter**, **pagination via query params** (`offset`, `limit`).  

### 4. Get Submission by Id
```
GET /submissions/{catalogId}/{id}
```
- Retrieves a single submission by ID.  

### 5. Get My Submissions
```
GET /submissions?status={status}
```
- Retrieves submissions for the **authenticated user** (artist or curator).  
- Supports **status filter** and pagination.  

---

## Business Logic

1. **Submission Creation**
   - Validates track via **Track Service** and catalog item via **Catalog Service**.  
   - Creates `Submission` and `ArtistInbox` entities.  
   - Creates an `OutboxEvent` of type `SubmissionCreated`.  
   - Persists data in Cosmos DB via **transactional batch**.  
   - Generates **audit activities** for both artist and curator.  

2. **Status Update**
   - Retrieves `Submission` and `ArtistInbox` entities.  
   - Updates status if different from the current one.  
   - Creates an `OutboxEvent` of type `SubmissionStatusChanged`.  
   - Updates entities in Cosmos DB via **transactional batch**.  
   - Generates **audit activities** for both artist and curator with before/after snapshots.  

3. **Event Publishing (OutboxWorker)**
   - Queries pending outbox events from Cosmos DB.  
   - Claims event (idempotent lock).  
   - Publishes to **MassTransit** message bus.  
   - Marks event as **Published** or **Failed**.  

---

## Technical Details & Constraints

- **Architecture:** CQRS with MediatR pipeline behaviors (`ActivityBehavior`).  
- **Database:** Azure Cosmos DB  
  - Entities: `Submission`, `ArtistInbox`, `OutboxEvent`.  
  - Partition Keys:
    - `Submission`: `CuratorUserId + CatalogItemId`
    - `ArtistInbox`: `ArtistId`
    - `OutboxEvent`: `CuratorUserId + CatalogItemId`  
  - Unique key constraints on `(CatalogItemId, TrackId)`.  
  - Composite indexes for efficient querying.  

- **Idempotency:**  
  - Achieved via **unique keys** and using `Submission.Id` for `ArtistInbox.Id`.  

- **Resiliency:**  
  - Retry policies for external services (TrackService, CatalogService).  
  - Outbox pattern ensures reliable event delivery.  

- **Security:**  
  - JWT-based authorization (`JwtSpredPolicy.JwtUserPolicy`).  
  - All endpoints require authenticated user context.  

---

## Limitations

- No direct deletion of submissions; instead, a `Deleted` status is used.  
- Background worker (`OutboxWorker`) currently uses polling instead of **Change Feed Processor** (future improvement).  
- External service availability is critical (track/playlist lookups required).  
- Cosmos DB conflict handling is limited to **Conflict** and **PreconditionFailed** scenarios.  
