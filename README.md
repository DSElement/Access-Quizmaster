# QuizMaster

A full-stack quiz platform. Users take official quizzes, write their own, share them with the community, and generate quizzes from their own documents with AI.

## Stack

| Layer | Tools |
|---|---|
| Frontend | React 19, TypeScript, Vite, React Router, TanStack Query, React Hook Form + Zod, CSS Modules |
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

In a second terminal, start the frontend:

```powershell
cd frontend
npm install
npm run dev
```

Open <http://localhost:5173>. The Vite dev server forwards every `/api` request to the API on port 5080, so the browser only ever talks to one address and the auth cookie works without any CORS setup.

| Page | Address |
|---|---|
| Official and your own quizzes | `/quizzes`, `/quizzes?tab=mine` |
| Community (shared quizzes) | `/community` |
| Quiz details, play, edit | `/quizzes/{id}`, `/quizzes/{id}/play`, `/quizzes/{id}/edit` |
| Write a quiz (admins: `?official=true`) | `/quizzes/new` |
| Your results | `/attempts`, `/attempts/{id}` |
| Manage users (admin) | `/admin/users` |

On startup the API creates the `Admin` and `User` roles and, if it doesn't exist yet, the admin account `admin@quizmaster.local` (configured in `appsettings.Development.json`). Passwords need at least 8 characters with an uppercase letter, a lowercase letter, a digit and a symbol. Changing the secret later does not change an existing admin's password.

## Authentication

Accounts use ASP.NET Core Identity with cookie authentication. Signing in sets an `HttpOnly`, `SameSite=Strict` cookie that the browser sends with every request; there are no tokens to store in JavaScript. Every endpoint requires a signed-in user unless it is explicitly marked anonymous.

The cookie also carries the user's roles. Once a minute the API checks the cookie against the database and reissues it, so a role change reaches a signed-in user within about a minute without them signing in again.

## API

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/api/health` | Anyone | Health check, including the database connection |
| POST | `/api/auth/register` | Anyone | Create an account and sign in |
| POST | `/api/auth/login` | Anyone | Sign in (sets the auth cookie) |
| POST | `/api/auth/logout` | Anyone | Sign out (clears the auth cookie) |
| GET | `/api/auth/me` | Signed in | The current user and their roles |
| GET | `/api/quizzes?scope=` | Anyone | Quizzes you can see; optional `scope` is `official`, `mine` (signed in) or `community` |
| GET | `/api/quizzes/{id}` | Anyone | Quiz details and what you're allowed to do with it (no questions) |
| GET | `/api/quizzes/{id}/play` | Signed in | Questions and options to play (no answer key) |
| GET | `/api/quizzes/{id}/edit` | Owner / admin | Full quiz including the correct answers, for the editor |
| POST | `/api/quizzes` | Signed in | Create a quiz (starts private) |
| POST | `/api/quizzes/official` | Admin | Create an official quiz |
| PUT | `/api/quizzes/{id}` | Owner / admin | Replace the title, description and all questions |
| DELETE | `/api/quizzes/{id}` | Owner / admin | Delete a quiz (past attempts are kept) |
| POST | `/api/quizzes/{id}/publish` | Owner | Share the quiz in the Community feed |
| POST | `/api/quizzes/{id}/unpublish` | Owner | Make the quiz private again |
| POST | `/api/quizzes/{id}/attempts` | Signed in | Submit answers; the server grades them and saves the attempt |
| GET | `/api/attempts` | Signed in | Your past attempts, newest first |
| GET | `/api/attempts/{id}` | Signed in | One of your attempts with every answer |
| GET | `/api/admin/users` | Admin | All accounts with their roles |
| POST | `/api/admin/users/{id}/promote` | Admin | Give an account the Admin role |

## Quizzes and permissions

Every quiz is **Official** (maintained by admins, no owner), **Private** (only its owner sees it) or **Public** (shared in the Community feed).

| Action | Who |
|---|---|
| See | Everyone sees official and public quizzes; private ones only their owner |
| Create | Any signed-in user creates private quizzes; admins also create official ones |
| Edit | The owner; admins for official quizzes |
| Delete | Whoever can edit; admins can also remove community quizzes |
| Publish / unpublish | Only the owner |

A quiz you can't see returns `404`, one you can see but not change returns `403`. Answers are graded on the server, and the answer key is only sent to people who can edit the quiz.

Each attempt stores a copy of the questions and options as they were when you played, so editing or deleting the quiz later doesn't change your past results.

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
| Run the frontend (from `frontend/`) | `npm run dev` |
| Type-check and build the frontend | `npm run build` |
| Lint the frontend | `npm run lint` |

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
  Services/                   Quiz validation and grading
frontend/
  vite.config.ts              Dev server and the /api proxy
  src/main.tsx                Entry point: TanStack Query and the router
  src/router.tsx              All routes
  src/api/                    fetch wrapper, shared types, query client
  src/components/             Layout, form fields, quiz cards and other shared pieces
  src/features/               Pages and API calls, grouped by feature (auth, quizzes, attempts, admin)
  src/lib/                    Small helpers (date formatting)
  src/routes/                 Not-found and error pages
```
