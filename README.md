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
# Start the database (the first run downloads the image)
docker compose up -d

# Check that it is healthy
docker compose ps
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
compose.yaml          Database container (PostgreSQL + pgvector)
QuizMaster.slnx       .NET solution
```
