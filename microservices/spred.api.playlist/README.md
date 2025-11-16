
# Catalog Microservice Documentation

## 1. Project Goals

The Catalog microservice is responsible for managing user-generated Catalog metadata, enabling retrieval, creation, update, and internal ingestion of playlist data. It is a core part of the platform's catalog and submission pipeline.

## 2. Requirements

### Functional Requirements

* CRUD operations on playlist metadata
* Support for internal service-to-service ingestion
* JWT-based authentication (internal and external)
* Integration with Redis, Cosmos DB, and RabbitMQ

### Non-Functional Requirements

* Support for scalable processing via RabbitMQ
* Secure, multi-source configuration handling
* Environment-specific overrides for dev/test/prod
* Monitoring and health endpoints

## 3. Architecture

### Technical Stack

* .NET 9 Web API
* MediatR for CQRS
* AutoMapper for DTO mappings
* Redis (e.g., caching, rate-limiting)
* Azure Cosmos DB (NoSQL)
* MassTransit with RabbitMQ for messaging
* Docker for containerization

### Logical Structure

* **Abstractions/**: Shared interfaces such as `IManager`
* **Configuration/**: Extensions for config binding
* **DiExtensions/**: Service wiring and setup
* **Routes/**: Route groupings for public and internal APIs
* **Models/**: DTOs and domain models

## 4. User Guide

### Running Locally

```bash
docker compose up --build
```

### Environment Configuration

#### Configuration Sources

* `appsettings.json`: Base settings
* `appsettings.Development.json`: Local overrides
* `appsettings.Test.json`: Test overrides
* Azure App Configuration (optional for production)
* Azure Key Vault (optional for secrets)

#### Key Environment Variables

* `ASPNETCORE_ENVIRONMENT`: dev/test/prod
* `KeyVaultConnectionString`: used if Azure Key Vault is enabled
* `FEED_ACCESSTOKEN`: Personal access token in azureDevops

> Configuration is loaded and merged during runtime based on the value of `ASPNETCORE_ENVIRONMENT`.

### Configuration Sections

* `Jwt`: Signing and validation parameters
* `Rabbit`: Exchange, queue, and consumer config
* `Redis`: Host and instance ID settings
* `CosmosDb`: Container and partition setup

## 5. Deployment Instructions (Docker Compose)

```yaml
playlistapi:
    container_name: playlist-api
    image: ${DOCKER_REGISTRY}-playlistapi
    links:
      - cosmos-db-emulator
      - redis
    depends_on:
      - cosmos-db-emulator
      - redis
    build:
      context: ../microservices/spred.api.playlist/source/
      dockerfile: PlaylistService/Dockerfile
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

#### GET /playlist
Fetch playlists with optional filters.
**Query**: `type`, `search`, `sort`
**Response**: `PlaylistResponse`

#### GET /playlist/{id}
Fetch a playlist by ID.
**Path**: `id`
**Response**: `MetadataDto`

#### POST /playlist
Add a playlist.
**Body**: `MetadataDto`
**Response**: `{ id: GUID }`

#### PATCH /playlist/{id}
Update a playlist by ID.
**Path**: `id`
**Body**: `MetadataDto`
**Response**: HTTP 200

#### DELETE /playlist/{id}
Delete a playlist by ID.
**Path**: `id`
**Response**: HTTP 204

#### GET /{spreUserId}/{id}
Get a playlist by ID.
**Path**: `spreUserId`
**Path**: `id`
**Response**: `PublicMetadataDto`

### Internal Endpoints (service-to-service)

#### POST /playlist/internal
Add playlist metadata (bulk).
**Body**: `MetadataDto`
**Response**: `{ id: GUID }`

#### GET /playlist/internal/{id}
Return all playlists for service.
**Path**: `id`
**Response**: `MetadataDto`

## 7. Token Creation and Validation

* Internal and external JWT bearers are supported
* Tokens validated via `AddJwtBearer()` for both audiences
* Required claims: `sub`, `role`, `exp`
* Middleware extracts and validates claims

## 8. Testing and Code Coverage

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

### Recommended Setup

* .NET 9 SDK
* Docker Desktop
* Redis (local)
* Azure Cosmos DB Emulator (Windows only)
* Visual Studio or Rider

### Steps to Add a New Endpoint

1. Create handler with MediatR
2. Define DTOs in `Models/`
3. Map endpoint in `Routes/`
4. Write tests in `Playlist.Test`
5. Update documentation

### Coding Conventions

* Use async/await
* Propagate `CancellationToken`
* Validate input using DataAnnotations

## 10. Troubleshooting

### Cosmos DB query fails
* Check partition key and indexing policy

### RabbitMQ failure
* Ensure queues and bindings are configured properly

## 11. CI/CD Notes

* Azure DevOps
* Build/test on PR
* Docker image publish on `main` merge
* Secrets via pipeline environment settings

## 12. API Testing Examples

### Example: Fetch Playlists

```http
GET /playlist?type=playlist
Authorization: Bearer <token>
```

### Example: Add Playlist Metadata

```http
POST /playlist/internal
Content-Type: application/json
Authorization: Bearer <internal-service-token>

{
  "name": "Chill Vibes",
  "ownerId": "...",
  "tracks": ["track-guid-1", "track-guid-2"]
}
```

---

## Dependencies

* `MassTransit`
* `MediatR`
* `AutoMapper`
* `Microsoft.Azure.Cosmos`
* `Microsoft.Extensions.*`
* `StackExchange.Redis`
* `AspNetCore.Authentication.JwtBearer`
* `Swashbuckle.AspNetCore`
