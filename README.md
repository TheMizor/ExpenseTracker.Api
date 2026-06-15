# ExpenseTracker.Api

A small but production-minded REST API for tracking personal expenses, built with **.NET 10** and **ASP.NET Core Minimal APIs**. It features JWT authentication, per-user data isolation, and a clean separation between domain entities and the data exposed over HTTP.

This is a portfolio project: the goal is readable, well-structured code rather than feature breadth.

## Tech stack

- **.NET 10** / ASP.NET Core Minimal APIs
- **Entity Framework Core** with **SQLite** (zero-config, file-based)
- **JWT Bearer** authentication
- **BCrypt** password hashing
- **Scalar** for the interactive OpenAPI UI

## Features

- User registration and login, returning a signed JWT
- Passwords stored hashed (BCrypt), never in plain text
- Full CRUD for **categories** and **transactions**
- Transaction listing with optional filters (date range, category)
- Strict per-user data isolation: every request only ever touches the authenticated user's data
- DTOs on every endpoint — EF entities are never exposed directly (no over-posting, no leaking of internal structure)

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run it

```bash
git clone https://github.com/<your-username>/expense-tracker-api.git
cd expense-tracker-api
```

The JWT signing key is not committed. Set it once via user-secrets:

```bash
dotnet user-secrets set "Jwt:Key" "<a-long-random-secret>"
```

Then run the API:

```bash
dotnet run
```

The SQLite database is created automatically on first launch. Once running, open the interactive docs at:

```
https://localhost:<port>/scalar
```

### Try it

1. `POST /api/auth/register` with an email and password → you receive a JWT.
2. Authorize with that token (the **Authorize** button in Scalar, or the `Authorization: Bearer <token>` header).
3. Create a category, then a transaction on it, list and filter your transactions.

Without a valid token, the category and transaction endpoints return `401 Unauthorized`.

## API overview

### Auth (public)

| Method | Route                | Description                  |
|--------|----------------------|------------------------------|
| POST   | `/api/auth/register` | Create an account, get a JWT |
| POST   | `/api/auth/login`    | Log in, get a JWT            |

### Categories (auth required)

| Method | Route                   | Description           |
|--------|-------------------------|-----------------------|
| GET    | `/api/categories`       | List your categories  |
| POST   | `/api/categories`       | Create a category     |
| GET    | `/api/categories/{id}`  | Get one category      |
| PUT    | `/api/categories/{id}`  | Update a category     |
| DELETE | `/api/categories/{id}`  | Delete a category     |

### Transactions (auth required)

| Method | Route                      | Description                                  |
|--------|----------------------------|----------------------------------------------|
| GET    | `/api/transactions`        | List transactions (filters: `from`, `to`, `categoryId`) |
| POST   | `/api/transactions`        | Create a transaction                         |
| GET    | `/api/transactions/{id}`   | Get one transaction                          |
| PUT    | `/api/transactions/{id}`   | Update a transaction                         |
| DELETE | `/api/transactions/{id}`   | Delete a transaction                         |

Example filtered query:

```
GET /api/transactions?from=2026-01-01&to=2026-01-31&categoryId=2
```

## Project structure

```
ExpenseTracker.Api/
├── Models/         # EF Core entities (User, Category, Transaction)
├── Data/           # AppDbContext
├── DTOs/           # Request/response contracts
├── Services/       # TokenService (JWT generation)
├── Extensions/     # ClaimsPrincipal helper (current user id)
├── Migrations/     # EF Core migrations
└── Program.cs      # Endpoints + app configuration
```

## Roadmap

- Aggregation endpoints (totals by category, monthly breakdown)
- Unit tests (xUnit) covering validation and per-user isolation
- Dockerfile + docker-compose
- CI with GitHub Actions (build + test on each push)
- Refresh tokens and password reset
