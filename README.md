# QuizMaster

A full-stack quiz platform. Users take official quizzes, write their own, share them with the community, and generate quizzes from their own documents with AI.

## Stack

| Layer | Tools |
|---|---|
| Frontend | React 19, TypeScript, Vite, React Router, TanStack Query |
| Backend | ASP.NET Core 10 Web API, ASP.NET Core Identity |
| Database | PostgreSQL 18 + pgvector, EF Core 10 (Npgsql) |
| AI | Gemini through Microsoft.Extensions.AI |

## Prerequisites

- .NET 10 SDK
- Node.js 24 LTS
- Docker Desktop

## Getting started

```powershell
# 1. Start the database (the first run downloads the image)
docker compose up -d

# 2. Restore the local .NET tools (dotnet-ef)
dotnet tool restore

# 3. Create the tables and seed the official quizzes
dotnet ef database update --project backend/QuizMaster.Api

# 4. Choose the admin account's password (stored outside the repo)
dotnet user-secrets set "Seed:AdminPassword" "<your password>" --project backend/QuizMaster.Api

# 5. Run the API
dotnet run --project backend/QuizMaster.Api
```

The API runs at <http://localhost:5080>. Interactive API docs are at <http://localhost:5080/scalar> (development only).

On startup the API creates the `Admin` and `User` roles and, if it doesn't exist yet, the admin account `admin@quizmaster.local` (configured in `appsettings.Development.json`). Passwords need at least 8 characters with an uppercase letter, a lowercase letter, a digit and a symbol. Changing the secret later does not change an existing admin's password.

## Authentication

Accounts use ASP.NET Core Identity with cookie authentication. Signing in sets an `HttpOnly`, `SameSite=Strict` cookie that the browser sends with every request; there are no tokens to store in JavaScript. Every endpoint requires a signed-in user unless it is explicitly marked anonymous.

## API

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/api/health` | Anyone | Health check, including the database connection |
| POST | `/api/auth/register` | Anyone | Create an account and sign in |
| POST | `/api/auth/login` | Anyone | Sign in (sets the auth cookie) |
| POST | `/api/auth/logout` | Anyone | Sign out (clears the auth cookie) |
| GET | `/api/auth/me` | Signed in | The current user and their roles |
| GET | `/api/quizzes` | Anyone | Official and public quizzes, plus your own private ones |
| GET | `/api/quizzes/{id}` | Anyone | Quiz with its questions and options (no answer key) |
| GET | `/api/admin/users` | Admin | All accounts with their roles |

## Database changes

The schema is managed with EF Core migrations in `backend/QuizMaster.Api/Data/Migrations`.

```powershell
# After changing an entity or AppDbContext
dotnet ef migrations add <Name> --project backend/QuizMaster.Api --output-dir Data/Migrations
dotnet ef database update --project backend/QuizMaster.Api
```

## Useful commands

| Task | Command |
|---|---|
| Start database | `docker compose up -d` |
| Stop database (keeps data) | `docker compose down` |
| Delete database data | `docker compose down -v` |
| Open a SQL shell | `docker compose exec db psql -U quizmaster -d quizmaster` |

## Project layout

```text
compose.yaml                  Database container (PostgreSQL + pgvector)
QuizMaster.slnx               .NET solution
dotnet-tools.json             Pinned .NET CLI tools (dotnet-ef)
backend/QuizMaster.Api/
  Program.cs                  Startup: services and HTTP pipeline
  Auth/                       Roles, policies, user and access helpers
  Models/                     Entities (database tables)
  Data/                       AppDbContext, seed data, migrations
  Contracts/                  Request/response shapes sent over HTTP
  Controllers/                API endpoints
```
