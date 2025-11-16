# Spred Architecture Overview

## 1. Concept
Spred is a cloud-native, distributed microservice platform for AI‑powered music promotion, analytics, and recommendation. It processes audio, runs large‑scale ML inference, manages playlists, and provides premium subscription functionality.

The platform is designed with modern architectural principles focusing on scalability, resilience, observability, and secure service‑to‑service communication inside a private AKS environment.

## 2. Architectural Principles

### Domain-Driven Design (DDD)
Each microservice is designed around a clear bounded context:
- Playlist Context  
- Inference Context  
- Aggregator Context  
- Subscription Context  
- Activity/Analytics Context  

The internal structure follows: Application → Domain → Infrastructure.

### Clean Architecture / Ports & Adapters
Services follow strict separation of concerns:
- ports (interfaces) for all external dependencies  
- adapters for storage, messaging, and external APIs  
- business logic is isolated and fully testable  

### Event-Driven Architecture
The system is built around asynchronous events:
- track upload  
- metadata aggregation  
- ML inference  
- vector similarity search  
- playlist updates  
- premium subscription state updates  

Messaging:
- MassTransit  
- RabbitMQ (Exchange, Queue, Retry, DLQ)

## 3. Microservices

### PlaylistService
- Full CRUD for playlist metadata  
- Public and internal APIs (`/playlist/internal`)  
- Typed metadata resolution  
- JWT-based service-to-service authorization  
- Cosmos DB storage  

### TrackService / TrackDownloadService
- Audio upload  
- FFmpeg-based preprocessing  
- Preloading tracks from external sources  
- Background worker using ConcurrentStack  

### InferenceService
- ML pipeline: FFmpeg → embedding → FAISS search  
- Redis-based inference state tracking  
- Message-driven processing  
- Result publishing  

### AggregatorService
- Spotify + Chartmetric integration  
- Rate limiting  
- Redis caching  
- Background aggregation tasks  

### SubscriptionService
- Stripe Checkout + Webhooks  
- Premium TTL state stored in Redis  
- Claims injection (“Premium”, “Premium_exp”)  
- Comprehensive integration tests  

### ActivityService
- GA4 event tracking  
- Custom analytics ingestion  
- Cosmos DB storage  
- Application Insights-based observability  

## 4. Storage Layer

### Cosmos DB
- Primary metadata store  
- PartitionKey standardized as string  
- Designed for high-throughput reads  
- Denormalized models for performance  

### Redis
- Premium state with TTL  
- Deduplication for inference tasks  
- Dynamic rate limiter  
- Distributed cache  

### Azure Blob Storage
- Raw & processed audio  
- Embeddings and vector data  
- Automatic hash validation  

### FAISS + IVF Indexing
- Local VectorStore manager  
- Persistent on-disk inverted lists  
- High-performance similarity search  

## 5. Integrations

### Stripe
- Checkout Sessions  
- Subscription lifecycle and renewals  
- Webhook-based state updates  

### Chartmetric
- Token lifecycle  
- Rate limiting  
- DTO mapping via AutoMapper  
- Bulk track/genre aggregation  

### Omnisend
- Azure Functions event producer  
- Multiple event formats (feedback, notifyMe, subscribed, etc.)

## 6. Communication & Security

### Private AKS Cluster
- No public IPs for workloads  
- Egress via NAT Gateway  
- Ingress only through Application Gateway  
- cert-manager + Let’s Encrypt  
- Azure Firewall + DDoS Protection  
- All service endpoints internal-only  

### NGINX Ingress Controller (external-nginx)
- Strict security headers (CSP, COOP, CORP, HSTS, etc.)  
- Optimized timeouts  
- Custom header injection  
- Rolling updates and node affinity  

## 7. Observability
- Application Insights traces, logs, and metrics  
- Structured logging (JSON)  
- Distributed tracing with correlation IDs  
- Health & readiness probes  
- Per-service dashboards  

## 8. DevOps & Deployment

### Infrastructure as Code
- Full AKS, VNET, NAT, AGIC, Redis, Cosmos, Storage — all managed via Terraform  
- Multi-stage CI/CD pipelines in Azure DevOps  
- Docker multi-stage builds  
- Automated configuration delivery through Azure App Configuration  

### Tests
- xUnit-based unit and integration tests  
- MassTransit test harness  
- Full coverage for consumers, handlers, services  
- Refit-based API tests with custom WebApplicationFactory  

## 9. Key Technical Highlights
- Fully distributed event-driven architecture  
- High-performance ML inference with FAISS  
- Secure, private Kubernetes environment  
- Optimized Cosmos DB data models  
- Premium subscription with TTL-based claims  
- Strong DevOps automation and IaC  
