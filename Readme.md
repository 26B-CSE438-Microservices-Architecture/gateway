# Trendyol Go Clone - API Gateway & Auth Service 
## Team Members
* Melike Esra Öz
* Sude Akıncı
* Mustafa Özger

## Project Overview
This repository contains the central API Gateway and Authentication/Authorization service for the Trendyol Go microservices architecture. Acting as the system's single entry point, this service intercepts all incoming requests from the client application (Frontend), handles security validations, and seamlessly routes traffic to the appropriate downstream microservices (e.g., User, Order, Payment, Restaurant) using Microsoft's high-performance reverse proxy, YARP.



## Technology Stack
* **Framework:** ASP.NET Core Web API (.NET 8/9)
* **Reverse Proxy / Gateway:** YARP (Yet Another Reverse Proxy)
* **Security:** JWT (JSON Web Token) Bearer Authentication & ASP.NET Core Identity
* **Database:** PostgreSQL (User & Role Persistence)
* **Cache:** Redis (Refresh Token & Rate Limiting Storage)
* **Communication:** gRPC (Internal Auth Check) & REST (External Routing)
* **Containerization:** Docker & Docker Compose

---

## Gateway & Auth Service Requirements & Deliverables

As the central entry point of the architecture, the Gateway/Auth service must fulfill specific infrastructure and security-oriented requirements:

### 1. Infrastructure & Security Persistence
* **Auth Persistence:** A dedicated **PostgreSQL** database must be managed to store user credentials, profile information, and role assignments.
* **Session Persistence:** A low-latency **Redis** cache layer must be utilized to store Refresh Tokens and track Rate Limiting counters.
* **Secret Management:** Sensitive data such as JWT Signing Keys and Database Connection Strings must be managed securely via `User Secrets` or `Environment Variables`.

### 2. Core Gateway Functionalities (YARP)
* **Reverse Proxy:** Using **YARP (Yet Another Reverse Proxy)**, incoming requests must be matched and routed to the correct downstream microservice based on path patterns.
* **Request Transformation:** The Gateway must strip the `/api` prefix from incoming URIs and inject verified user identity headers (`X-User-Id`, `X-User-Role`) before forwarding.
* **Global CORS & Rate Limiting:** A global CORS policy must restrict traffic to verified Frontend origins, and IP-based Rate Limiting must be enforced to prevent DoS attacks.



### 3. Identity & Authentication Logic
* **JWT Generation:** Upon successful login, the service must issue a short-lived `AccessToken` and a long-lived `RefreshToken`.
* **Role-Based Access Control (RBAC):** Requests must be filtered at the Gateway level based on the required authorization (e.g., Admin, Customer, Courier) defined in the route configuration.
* **Internal Communication:** A **gRPC** server must be available for high-speed internal authorization checks if downstream services need to verify permissions in real-time.

### 4. Observability & Health
* **Aggregated Swagger:** The Gateway should act as a central hub, aggregating the OpenAPI/Swagger documentation from all downstream services into a single UI.
* **Health Checks:** The service must expose a `/health` endpoint that monitors its own status as well as its connectivity to Redis and PostgreSQL.
* **Structured Logging:** All routing failures, 401 Unauthorized, and 403 Forbidden attempts must be logged in a structured format for security auditing.



### 5. Docker & Orchestration
* **Network Entrypoint:** In the `docker-compose.yml`, the Gateway must be defined as the primary entrypoint for the `backend-network`.
* **Dependency Management:** The Gateway container must use `depends_on` with `healthcheck` conditions to ensure it only starts after the PostgreSQL and Redis instances are fully operational.
---

## Database & Schema Design (Gateway/Auth)
The Auth service manages a dedicated PostgreSQL database to handle user identities:

* **Users:** Id, Email, PasswordHash, FullName, PhoneNumber.
* **UserRoles:** Customer, Courier, RestaurantAdmin, SysAdmin.
* **RefreshTokens:** Token, UserId, ExpiryDate, IsRevoked.

---

## API Interfaces & Endpoints

### 1. Authentication Service - `api/v1/auth`
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| POST | `/login` | Authenticates user via Email/Password, returns JWT and Refresh Token. | Public |
| POST | `/register` | Creates a new user account (Default role: `Customer`). | Public |
| POST | `/refresh-token` | Refreshes an expired Access Token using a valid Refresh Token. | Public |
| POST | `/logout` | Revokes the current Refresh Token and logs the user out. | Public |
| POST | `/forgot-password` | Sends a password reset code to the user's email. | Public |
| POST | `/reset-password` | Resets the password using the received code. | Public |
| GET | `/confirm-email` | Completes the email verification process. | Public |
| POST | `/change-password`| Changes the password for the currently authenticated user. | JWT Required |
| PUT | `/profile` | Updates user profile details. | JWT Required |
| DELETE| `/account` | Permanently deletes the user account and all associated tokens. | JWT Required |

### 2. Infrastructure & Monitoring Endpoints
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| GET | `/health` | Reports overall service and database health status (Internal check). |
| GET | `/swagger` | Central UI for exploring local API endpoints and proxied microservices. |

### 3. Gateway Proxied Interfaces (YARP)
The Gateway acts as a reverse proxy, stripping the `/api` prefix and injecting verified identity headers:
* `X-User-Id`: Extracted from the `uid` claim.
* `X-User-Role`: Extracted from the `roles` claim (e.g., Customer, Courier).

| Source Path | Target Service | Logic / Responsibility |
| :--- | :--- | :--- |
| `/api/order/**` | Order Service | Order placement and tracking |
| `/api/restaurant/**` | Restaurant Service | Menu browsing and management |
| `/api/payment/**` | Payment Service | Payment processing |
| `/api/user/**` | User Service | User profile management |

---

## Service Communication & Logic Flow
1. **Request:** Client sends request to Gateway.
2. **Auth:** Gateway validates JWT. If valid, extracts Claims.
3. **Transformation:** Gateway strips `/api` and adds `X-User-Id` header.
4. **Routing:** YARP forwards request to the Load Balanced destination of the target service.

---

## Setup & First Requirements
To get the gateway running in a local development environment:

### 1. Infrastructure Setup
Start the required infrastructure using Docker:
```bash
docker-compose up -d postgres-db redis-cache
```

### 2. Configuration
Define your JWT settings in appsettings.json:

```bash
JSON
{
  "JwtSettings": {
    "Secret": "Your_Super_Secret_Key_Here",
    "Issuer": "TrendyolGo.Auth",
    "Audience": "TrendyolGo.Clients"
  }
}
```
### 3. Database Migration
Apply the Identity schema to your PostgreSQL instance:

```bash
dotnet ef database update
```
### 4. Running the Service
```bash
dotnet build
dotnet run --project Gateway.Auth.Service
```