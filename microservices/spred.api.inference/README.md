
# Inference Microservice Documentation

## 1. Project Goals

The Inference microservice processes audio tracks through AI models to produce predictions and metadata. It operates asynchronously using background workers and queues, storing results for further enrichment and consumption.

## 2. Requirements

### Functional Requirements
- Process track audio or metadata via AI model(s)
- Generate inferences with scores and similar track suggestions
- Store and expose inference results via API
- Work asynchronously using a background queue

### Non-Functional Requirements
- High throughput and horizontal scalability
- Configurable model execution
- Robust error handling and retries
- Logging and metrics collection for observability

## 3. Architecture

### Technical Stack
- .NET 9 Web API
- BackgroundService for queued processing
- Redis or similar for distributed coordination
- AutoMapper for DTO mapping
- Docker for containerization

### Logical Structure
- **Routes**: Endpoint declarations
- **Components**: Core processing logic (e.g., `InferenceProcessor`)
- **BackgroundTasks**: Hosted services like `InferenceQueueHandler`
- **Configuration**: Options for inference, queue limits, and model setup

## 4. API Guide

### POST /inference
Submit a track (or reference) to the inference queue.  
- **Request Model**: `SubmitInferenceRequest`  
- **Response Model**: `InferenceQueuedResponse`

### GET /inference/{id}
Get the status or result of an inference.  
- **Response Model**: `InferenceResultDto`

## 5. Background Processing

The microservice uses a background queue system to offload AI inference. Jobs are queued via API or event triggers and consumed by `InferenceQueueHandler`.

### Flow:
1. Incoming request is validated and transformed
2. Job is added to the queue (Redis, in-memory, etc.)
3. Background worker picks up the job
4. AI model is executed (via `ModelProcessor`)
5. Result is stored and available for querying

## 6. Configuration

### Environments:
- `Development`: Uses local queue and mock model processor
- `Production`: Uses full model engine and persistent queue

### Options:
- `InferenceOptions`: Max concurrency, batch size
- `QueueOptions`: Retry delay, visibility timeout
- `ModelOptions`: Model version, scoring threshold

Configured via `appsettings.{Environment}.json` and environment variables.

## 7. Dependencies

- `AutoMapper`
- `Microsoft.Extensions.Hosting`
- `Microsoft.AspNetCore.HttpLogging`
- `Refit`
- `Hasher`
- `Extensions`

## 8. Test Plan (Overview)
- Unit tests for `InferenceProcessor`, queue logic
- Integration tests for API and background execution
- Load tests with high-volume submission
- Negative tests for invalid or malformed input

---
