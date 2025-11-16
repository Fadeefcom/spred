# Authorization Microservice Documentation

## 1. Project Goals

The Authorization microservice provides authentication and authorization functionality for the platform. It manages user identities, issues tokens, and integrates with external identity providers like Spotify.

## 2. Requirements

### Functional Requirements

* Issue and validate JWT access tokens
* Integrate with external OAuth providers (Spotify, etc.)
* Secure APIs using role-based access control
* Persist authorization grants and user claims

### Non-Functional Requirements

* High availability and scalability
* Secure storage of secrets and keys
* Rate-limiting to prevent abuse
* Logging and monitoring of access patterns

## 3. Architecture

### Technical Stack

* .NET 9 Web API
* ASP.NET Identity
* Redis for data protection key storage
* StackExchange.Redis client
* Docker for containerization

### Logical Structure

* **Abstractions**: Contains interface contracts (`IAggregatorApi`, `IPlusStore`, etc.)
* **Helpers**: Utility classes for token manipulation and rate-limiting
* **DiExtensions**: Extension methods to inject dependencies
* **Routes**: Endpoint configuration and middleware pipeline
* **Entities**: EF Core or custom user models

## 4. User Guide

### Running Locally

```bash
docker compose up --build
```

### Environment Configuration

The microservice uses different configuration sources depending on the environment:

* **Production (Azure)**:
  Settings are provided via Azure App Configuration, Key Vault, and environment variables.

* **Development & Test**:
  Local settings are stored in `appsettings.Development.json` or `appsettings.Test.json`.

#### Common Configuration Sources

* `appsettings.json`: Base configuration for all environments
* `appsettings.Development.json`: Overrides for local development
* **Azure App Configuration**: Used in production for secure, centralized configuration
* **Azure Key Vault**: Stores secrets such as JWT signing keys or Spotify credentials

#### Overridden Sections in `appsettings.Development.json`

* `ConnectionStrings.Redis`: Local Redis connection string
* `Jwt.Secret`: Test JWT secret for token signing
* `Spotify.ClientId` / `Spotify.ClientSecret`: Developer credentials
* `Logging`: May include more verbose or debug-level logging settings
* `IdentityServer`: Settings for token lifetimes and claim mappings

#### Key Variables

* `ASPNETCORE_ENVIRONMENT`: dev/test/prod
* `KeyVaultConnectionString`: used if Azure Key Vault is enabled
* `FEED_ACCESSTOKEN`: Personal access token in azureDevops

> Configuration is loaded and merged during runtime based on the value of `ASPNETCORE_ENVIRONMENT`.

### Token Flow

1. User logs in with credentials or Spotify
2. Server generates JWT using `AuthorizationTokenHelper`
3. Token is stored in cookies or sent via header

## 5. Deployment Instructions (Docker Compose)

### docker-compose.yaml (simplified)

```yaml
authorizationapi:
  container_name: authorization-api
  image: ${DOCKER_REGISTRY}-authorizationapi
  links:
    - cosmos-db-emulator
    - redis
  depends_on:
    - cosmos-db-emulator
    - redis
  build:
    context: ../microservices/spred.api.authorazation/source
    dockerfile: Authorization/Dockerfile
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

### Authentication Endpoints

#### GET /login

Starts OAuth or local login flow.
**Response Model**: Redirect or JWT Token

#### POST /refresh-token

Refreshes an access token using a refresh token.
**Request Model**: `RefreshTokenRequest`
**Response Model**: `TokenResponse`

#### POST /logout

Logs the user out and removes their session.
**Response Model**: HTTP 204 / Success

#### GET /token

Issues a new token (internal or based on auth context).
**Request Model**: Auth context via headers/cookies
**Response Model**: `TokenResponse`

### User Profile Endpoints

#### GET /me

Returns current authenticated user profile.
**Request Model**: JWT in header/cookie
**Response Model**: `UserProfileDto`

#### GET /authentications

Returns a list of all linked OAuth authentications.
**Request Model**: JWT in header/cookie
**Response Model**: `List<OAuthAuthentication>`

### System & Utility Endpoints

#### GET /init

Returns basic configuration or initialization info.
**Response Model**: `InitConfigurationDto`

### Submission Endpoints

#### POST /notify

Submits a "notify me" request.
**Request Model**: `NotifyMe`
**Response Model**: HTTP 200 / Success

#### POST /feedback

Submits user feedback.
**Request Model**: `Feedback`
**Response Model**: HTTP 200 / Success

#### POST /track/batch

Submits a batch of track processing requests.
**Request Model**: `List<TrackSubmissionDto>`
**Response Model**: HTTP 200 / Success

## 7. Token Creation and Validation

### Token Creation

Tokens are created using the `AuthorizationTokenHelper` class. Upon successful authentication (e.g., OAuth or direct login), a JWT token is generated with the following structure:

* **Header**:

  * Algorithm: `HS256`
  * Type: `JWT`

* **Payload** (claims):

  * `sub`: user identifier (GUID)
  * `email`: user's email (if available)
  * `exp`: expiration timestamp
  * `role`: user role (if applicable)

* **Signature**:

  * HMAC-SHA256 using the `JWT_SECRET` environment variable

The token is then issued to the client via a cookie or Authorization header.

### Token Validation

Incoming requests validate tokens via middleware, which:

1. Parses the `Authorization` header or cookie
2. Validates signature using `JWT_SECRET`
3. Checks for expiration and required claims

### Key Management

Keys used to sign/validate tokens.

Additionally:

* Redis is used to store data protection keys (for ASP.NET Identity or cookies)
* Key storage is handled via `StackExchange.Redis` and ASP.NET Core `IDataProtectionProvider`

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
* Redis local (optional)
* Cosmos DB Emulator (for Windows)
* Visual Studio 2022+ or JetBrains Rider

### Steps to Add a New Endpoint

1. Define route in `Routes/AccountRoutes.cs` or `Routes/CustomGroup.cs`
2. Add DTOs to `Models/Dto`
3. Implement logic in service (e.g., `BaseManagerServices`)
4. Add unit & integration tests
5. Document the route in this file

### Contribution Rules

* Use `IAuthorizationService` and policies for access checks
* Prefer async/await and cancellation tokens
* Validate inputs using `[Required]`, `[EmailAddress]`, `[RegularExpression]`, etc.

## 10. Troubleshooting

### Login returns 404

Ensure the UI domain in `RedirectResponse` points to a valid frontend.

### Redis connection error

Check if `REDIS_CONNECTION` is correctly set in `.env` or system environment variables.

### Token validation failed

* Verify `JWT_SECRET` is consistent across services
* Ensure clocks are synced in container and host

### Store does not implement IUserAuthenticationTokenStore

Ensure that `BaseUserStore` inherits from `UserStore<...>` **and** implements token-related interfaces if overridden.

## 11. CI/CD Pipeline

### GitHub Actions (example)

* Build and test on PRs
* Docker image published on merge to `main`
* Secrets injected via GitHub repository settings

## 12. API Testing Examples

### Example: Get User Profile

```http
GET /user/me
Authorization: Bearer <access_token>
```

### Example: Notify Me

```http
POST /user/notify
Content-Type: application/json

{
  "name": "Jane",
  "email": "jane@example.com",
  "artistType": "Solo",
  "message": "Let me know when it's ready"
}
```

---

## Dependencies

* `AspNetCore.Identity.CosmosDb`
* `AutoMapper`
* `Extensions`
* `Hasher`
* `Microsoft.AspNetCore.Authentication.Google`
* `Microsoft.AspNetCore.Authentication.JwtBearer`
* `Microsoft.AspNetCore.Authentication.OpenIdConnect`
* `Microsoft.AspNetCore.DataProtection`
* `Microsoft.AspNetCore.OpenApi`
* `Microsoft.EntityFrameworkCore`
* `Microsoft.Extensions.Configuration.AzureAppConfiguration`
* `Microsoft.VisualStudio.Azure.Containers.Tools.Targets`
* `NRedisStack`
* `Refit`
* `Swashbuckle.AspNetCore`
* `Swashbuckle.AspNetCore.Swagger`
