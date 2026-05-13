# Trendyol Go Clone — API Gateway & Auth Service

**Team Members:** Melike Esra Öz · Sude Akıncı · Mustafa Özger

---

## Overview

This repository is the **single entry point** of the Trendyol Go microservices architecture. It has two core responsibilities:

1. **Auth Service** — Identity management built on ASP.NET Core Identity + PostgreSQL. Handles registration, login, JWT issuance, token refresh, and password management. Exposes a gRPC endpoint for real-time token verification by downstream services.

2. **API Gateway (BFF)** — Authenticates every inbound request, injects verified identity headers, and either handles the request locally (profile aggregation, cart, saga orchestration) or forwards it to a downstream microservice via YARP.

---

## Technology Stack

| Concern | Technology |
| :--- | :--- |
| Framework | ASP.NET Core Web API (.NET 9) |
| Reverse Proxy | YARP 2.2 |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Database | PostgreSQL (Identity persistence) |
| Cache | Redis (health-monitored; reserved for future use) |
| Messaging | RabbitMQ (Saga command bus) |
| Internal RPC | gRPC |
| Containerization | Docker & Docker Compose |

---

## Architecture

```
                        ┌──────────────────────────────────────────┐
                        │       API Gateway & Auth Service         │
                        │                                          │
  Client ──────────────▶│  JWT Middleware                          │
                        │    └─ validates token                    │
                        │    └─ injects X-User-Id, X-User-Role     │
                        │                                          │
                        │  ┌──────────────┐   ┌──────────────────┐ │
                        │  │ Auth Service │   │   BFF Layer      │ │
                        │  │              │   │                  │ │
                        │  │  Identity    │   │  aggregates and  │ │
                        │  │  JWT/Refresh │   │  orchestrates    │ │
                        │  │  gRPC server │   │  downstream APIs │ │
                        │  └──────────────┘   └────────┬─────────┘ │
                        │                              │           │
                        │              ┌───────────────┘           │
                        │              │  YARP Reverse Proxy       │
                        │              │  (transparent forwarding) │
                        └──────────────┼───────────────────────────┘
                                       │
               ┌───────────────────────┼──────────────────────┐
               │               RabbitMQ│(Saga)                │
               ▼                       ▼                      ▼
       ┌──────────────┐      ┌──────────────────┐   ┌──────────────────┐
       │ order-service│      │restaurant-service│   │ payment-service  │
       │    :8082     │      │ :5000 (YARP)     │   │     :3000        │
       └──────────────┘      │ :5001 (BFF)      │   └──────────────────┘
                             └──────────────────┘   ┌──────────────────┐
                                                    │  user-service    │
                                                    │     :8000        │
                                                    └──────────────────┘
```

---

## Service Communication

### 1. YARP — Transparent Reverse Proxy

Certain routes are forwarded directly to downstream services without any BFF logic. Before forwarding, YARP injects the verified identity headers extracted from the JWT:

- `X-User-Id` — from the `uid` claim
- `X-User-Role` — from the `roles` claim

| Inbound Path | Target Service | Forwarded As |
| :--- | :--- | :--- |
| `/api/order/{**}` | `order-service:8082` | `/{**}` |
| `/api/restaurant/{**}` | `restaurant-service:5000` | `/api/v1/restaurants/{**}` |
| `/api/payment/{**}` | `payment-service:3000` | `/payments/{**}` |
| `/api/user/{**}` | `user-service:8000` | `/api/v1/users/{**}` |

---

### 2. Named HTTP Clients — BFF Aggregation

For endpoints that require aggregating data from multiple services (e.g. user profile + restaurant details for favorites), the gateway calls downstream services directly using named `HttpClient` instances. The current `Authorization` header from the inbound request is forwarded to maintain the authenticated context.

| Client Name | Target | Used By |
| :--- | :--- | :--- |
| `user` | `user-service:8000` | Profile, addresses, favorites |
| `order` | `order-service:8082` | Cart, orders, checkout |
| `restaurant` | `restaurant-service:5001` | Vendors, campaigns, search, reviews |
| `payment` | `payment-service:3000` | Payments |

---

### 3. RabbitMQ — Saga Command Bus

The Order Saga Orchestrator uses RabbitMQ to drive distributed transactions asynchronously. The gateway publishes commands to a topic exchange and `SagaBackgroundService` consumes them in the background, executing each saga step (order creation → payment → restaurant confirmation) and running compensation logic on failure.

| Routing Key | Trigger |
| :--- | :--- |
| `saga.command.start` | New order initiated |
| `saga.command.payment-callback` | iyzico payment result received |
| `saga.command.confirm` | Restaurant approves the order |
| `saga.command.reject` | Restaurant rejects the order |
| `saga.command.cancel` | Customer cancels (triggers compensation) |

---

### 4. gRPC — Internal Token Verification

`AuthGrpcService` exposes a high-speed gRPC endpoint that downstream services can call to verify a JWT and retrieve the caller's identity without making an HTTP round-trip through the gateway.

---

### 5. Internal REST — Service-to-Service

Other microservices can call the gateway's internal endpoints to query Identity data (user lookup, role assignment). Most of these endpoints are protected by a shared `X-Internal-Secret` header and are not exposed to clients.

| Path | Protected | Description |
| :--- | :--- | :--- |
| `POST internal/v1/users/{userId}/roles` | X-Internal-Secret | Assign a role to a user |
| `GET  internal/v1/users/{userId}` | X-Internal-Secret | Fetch user by ID |
| `POST internal/v1/users/lookup` | X-Internal-Secret | Bulk fetch users by ID list |
| `GET  internal/v1/users/by-email` | X-Internal-Secret | Fetch user by email |
| `POST internal/v1/vendors/lookup` | No auth | Bulk fetch vendor details |
| `POST api/v1/internal/orders/{id}/payment-callback` | X-Internal-Secret | Forward payment callback to Order Service |

---

## Auth Endpoints — `api/v1/auth`

The only endpoints implemented and owned entirely by this service.

| Method | Path | Description | Auth |
| :--- | :--- | :--- | :--- |
| POST | `/register` | Create a new account (default role: `Customer`) | Public |
| POST | `/login` | Authenticate with email/password, returns JWT + Refresh Token | Public |
| POST | `/refresh-token` | Exchange a valid Refresh Token for a new Access Token | Public |
| POST | `/logout` | Revoke the current Refresh Token | Public |
| POST | `/forgot-password` | Send a password reset code to the user's email | Public |
| POST | `/reset-password` | Reset password using the received code | Public |
| GET | `/confirm-email` | Complete email verification | Public |
| POST | `/verify-token` | Validate a JWT and return its claims | Public |
| GET | `/me` | Get the authenticated user's identity | JWT Required |
| POST | `/change-password` | Change password for the authenticated user | JWT Required |
| PUT | `/profile` | Update auth-level profile | JWT Required |
| DELETE | `/account` | Permanently delete the account and all tokens | JWT Required |

---

## Infrastructure

| Endpoint | Description |
| :--- | :--- |
| `GET /health` | Service health — checks PostgreSQL and Redis connectivity |
| `GET /swagger` | Swagger UI |
| `GET /info` | Service version and last updated timestamp |

---

## Database Schema

Managed by this service via Entity Framework Core + PostgreSQL.

- **User** — Id, Email, PasswordHash, FirstName, LastName, PhoneNumber, EmailConfirmed
- **Role** — `Customer`, `RestaurantOwner`
- **UserRoles** — User ↔ Role mapping
- **RefreshTokens** — Id, Token, Expires, Created, CreatedByIp, Revoked, RevokedByIp, ReplacedByToken

---

## Setup

### 1. Start infrastructure

```bash
docker-compose up -d postgres redis
```

### 2. Configure secrets

```json
{
  "JWTSettings": {
    "Key": "Your_Super_Secret_Key_Here",
    "Issuer": "Gateway.Auth.Service",
    "Audience": "Gateway.Clients"
  }
}
```

### 3. Apply migrations

```bash
dotnet ef database update
```

### 4. Run

```bash
dotnet run --project CleanArchitecture.WebApi
```
