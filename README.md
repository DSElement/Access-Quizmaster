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

# 4. Run the API
dotnet run --project backend/QuizMaster.Api
```

The API runs at <http://localhost:5080>. Interactive API docs are at <http://localhost:5080/scalar> (development only).

## API

| Method | Route | Description |
|---|---|---|
| GET | `/api/health` | Health check, including the database connection |
| GET | `/api/quizzes` | List quizzes |
| GET | `/api/quizzes/{id}` | Quiz with its questions and options (no answer key) |

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
  Models/                     Entities (database tables)
  Data/                       AppDbContext, seed data, migrations
  Contracts/                  Request/response shapes sent over HTTP
  Controllers/                API endpoints
```
