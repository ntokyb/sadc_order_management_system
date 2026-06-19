# SADC Order Management System

Take-home assessment for **Senior Full Stack Developer** (Amrod).

**Repository:** https://github.com/ntokyb/sadc_order_management_system

## Repository layout

```
SADC_Order_Management_System/
├── README.md                 ← start here
├── docker-compose.yml        ← run everything with Docker
├── backend/                  ← .NET 8 API, worker, domain, tests
│   ├── SadcOrders.sln
│   ├── src/
│   └── tests/
├── frontend/                 ← React + TypeScript (Vite)
├── docker/                   ← Dockerfiles for API and worker
└── .github/workflows/        ← CI pipeline
```

## Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 8, EF Core, SQL Server, MediatR, RabbitMQ.Client |
| Frontend | React, TypeScript, Vite, Axios |
| Messaging | Transactional outbox → RabbitMQ |
| Observability | Serilog, correlation IDs, Prometheus |

## Quick start (Docker)

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Web UI | http://localhost:5173 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| RabbitMQ | http://localhost:15672 (guest/guest) |
| Metrics | http://localhost:8080/metrics |

## Quick start (local)

**1. Infrastructure**

```bash
docker compose up sqlserver rabbitmq
```

**2. Backend**

```bash
cd backend
dotnet restore
dotnet ef database update --project src/SadcOrders.Infrastructure --startup-project src/SadcOrders.API
dotnet run --project src/SadcOrders.API
```

In a second terminal:

```bash
cd backend
dotnet run --project src/SadcOrders.Worker
```

**3. Frontend**

```bash
cd frontend
cp .env.example .env.local   # set VITE_API_URL=http://localhost:8080
npm install
npm run dev
```

## Auth (mock Entra)

Protected endpoints require a JWT with role claims. Two development accounts are configured in `appsettings.json`:

| Username | Password | Role | Access |
|----------|----------|------|--------|
| `admin` | `Admin123!` | `OrderAdmin` | Full CRUD — create/update/delete customers and orders, change order status |
| `viewer` | `Viewer123!` | `OrderReader` | Read-only — list and view customers and orders only |

**UI:** open `/login`, sign in with one of the accounts above.

**API:**

```bash
curl -s -X POST http://localhost:8080/api/dev/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}'
# Use: Authorization: Bearer <token>
```

**CRUD summary**

| Resource | Create | Read | Update | Delete |
|----------|--------|------|--------|--------|
| Customers | POST `/api/customers` | GET list / by id | PUT `/api/customers/{id}` | DELETE `/api/customers/{id}` (no orders) |
| Orders | POST `/api/orders` | GET list / by id | PUT `/api/orders/{id}/status` | DELETE `/api/orders/{id}` (Pending only) |

Production: replace dev login with Microsoft Entra ID (OIDC / JWKS validation) and map Entra app roles to `OrderAdmin` / `OrderReader`.

## Tests

```bash
cd backend
dotnet test
```

- **Unit tests** — domain rules, validators (no I/O)
- **Integration tests** — API + SQL Server + RabbitMQ via Testcontainers (requires Docker)

## Author

Ntokozo Banda
