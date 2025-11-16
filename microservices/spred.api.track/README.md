# Track Microservice Documentation

## 1. Project Goals

The Track microservice manages audio track metadata, audio streams, and ingestion for analysis and cataloging. It supports both public endpoints (for fetching track data) and internal endpoints (for service-to-service communication).

## 2. Requirements

### Functional Requirements

* CRUD operations for track metadata
* Audio stream retrieval and upload
* Track analysis ingestion via RabbitMQ
* JWT-based authentication (internal and external)
* Service-to-service internal endpoints

### Non-Functional Requirements

* Scalable background processing via RabbitMQ
* Redis integration for caching or rate-limiting
* Cosmos DB for track storage
* Structured configuration and logging
* Docker support for local and cloud deployments

## 3. Architecture

### Technical Stack

* .NET 9 Web API
* MediatR for CQRS
* AutoMapper for DTO mappings
* Redis for caching
* Azure Cosmos DB (NoSQL)
* MassTransit + RabbitMQ for message transport
* Docker for containerization

### Logical Structure

* **Abstractions/**: Interfaces like `IAnalyzeTrackService`
* **Configuration/**: App configuration models and bindings
* **Models/**:
  * Commands and Queries (CQRS)
  * DTOs: `TrackDto`, `PublicTrackDto`, etc.
  * Entities: `TrackMetadata`, `Artist`, `Album`
* **Routes/**: Route definitions for internal and public APIs

## 4. User Guide

### Running Locally

```bash
docker compose up --build
```

### Environment Configuration

#### Configuration Sources

* `appsettings.json`: Default settings
* `appsettings.Development.json`: Developer overrides
* `appsettings.Test.json`: Test-specific config
* Azure Key Vault (optional for secrets)

#### Key Environment Variables

* `ASPNETCORE_ENVIRONMENT`: dev/test/prod
* `KeyVaultConnectionString`: used if Azure Key Vault is enabled
* `FEED_ACCESSTOKEN`: Personal access token in azureDevops

> Configuration is loaded and merged during runtime based on the value of `ASPNETCORE_ENVIRONMENT`.

### Configuration Sections

* `JWT`: Token validation rules for internal/external access
* `Rabbit`: Exchange/queue setup
* `Redis`: Host details
* `CosmosDb`: Database and container configuration

## 5. Deployment Instructions (Docker Compose)

```yaml
trackapi:
    container_name: track-api
    image: ${DOCKER_REGISTRY}-trackapi
    links:
      - cosmos-db-emulator
      - redis
    depends_on:
      - cosmos-db-emulator
      - redis
    build:
      context: ../microservices/spred.api.track/source
      dockerfile: TrackService/Dockerfile
      target: development
      args:
      - FEED_ACCESSTOKEN=${FEED_ACCESSTOKEN}
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
    networks:
      - backend
    environment:
      - KeyVaultConnectionString=null
      - ManagedIdentity=null
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
```

## 6. API Specifications

### Public Endpoints

#### GET /track
Fetch track metadata with optional filters.

#### GET /track/{id}
Fetch metadata for a specific track.

#### GET /track/audio/{id}
Return audio stream for a track.

#### POST /track
Create a new track metadata entry.

#### PATCH /track/{id}
Update a track metadata entry.

#### DELETE /track/{id}
Delete a track metadata entry.

### Internal Endpoints

#### POST /track/internal
Internal ingestion of track data.

#### GET /track/internal/{id}
Retrieve internal track info by ID.

## 7. Token Handling

* Validates both internal and external JWTs
* Auth is set via `AddJwtBearer` in Program.cs
* Required claims include `sub`, `exp`, `role`

## 8. Testing & Coverage

### Install Tools

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool

# OR local tools
dotnet new tool-manifest
dotnet tool install dotnet-reportgenerator-globaltool
```

### Run Tests with Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=../coverage/coverage

dotnet tool run reportgenerator -reports:../source/tests/coverage/coverage.opencover.xml -targetdir:../source/tests/coverage/report -reporttypes:HtmlSummary
```

### Target Coverage

* Line/branch/method coverage ≥ 80%
* Coverage reports generated using Coverlet + ReportGenerator

## 9. Developer Workflow

### Setup

* .NET 9 SDK
* Docker (with Redis + CosmosDB Emulator)
* Visual Studio or Rider

### Add New Endpoint

1. Define Command/Query in `Models/`
2. Implement handler via MediatR
3. Map route in `Routes/`
4. Add integration/unit tests
5. Update docs

### Coding Guidelines

* Use async/await
* Support CancellationToken
* Validate DTOs using `[Required]` and other attributes

## 10. Troubleshooting

### Audio Not Streaming
* Check local file availability or blob config

### Cosmos DB Errors
* Ensure container name and partition key match

### RabbitMQ
* Verify routing keys and bindings in `Program.cs`

## 11. CI/CD Notes

* Azure DevOps pipelines (optional)
* Docker image built and pushed on `main` branch
* Environment secrets managed via variables or KeyVault

---

## Dependencies

* `MassTransit`
* `MediatR`
* `AutoMapper`
* `Microsoft.Azure.Cosmos`
* `StackExchange.Redis`
* `AspNetCore.Authentication.JwtBearer`
* `Swashbuckle.AspNetCore`
