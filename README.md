# QuizMaster

A full-stack quiz platform. Users take official quizzes, write their own, share them with the community, and upload their own documents, which are split into chunks and indexed for semantic search.

## Stack

| Layer | Tools |
|---|---|
| Frontend | React 19, TypeScript, Vite, React Router, TanStack Query, React Hook Form + Zod, CSS Modules |
| Backend | ASP.NET Core 10 Web API, ASP.NET Core Identity |
| Database | PostgreSQL 18 + pgvector, EF Core 10 (Npgsql) |
| AI | Gemini embeddings (`gemini-embedding-001`) through Microsoft.Extensions.AI |

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

# 4. Choose the admin account's password and add your Gemini API key (both stored outside the repo)
dotnet user-secrets set "Seed:AdminPassword" "<your password>" --project backend/QuizMaster.Api
dotnet user-secrets set "Gemini:ApiKey" "<your key>" --project backend/QuizMaster.Api

# 5. Run the API
dotnet run --project backend/QuizMaster.Api
```

A free Gemini API key comes from [Google AI Studio](https://aistudio.google.com). The API refuses to start without one. The free tier's data may be used by Google, so only upload documents that aren't sensitive.

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
| Your documents, upload, chunks and search | `/documents`, `/documents/{id}` |
| Manage users (admin) | `/admin/users` |

On startup the API creates the `Admin` and `User` roles and, if it doesn't exist yet, the admin account `admin@quizmaster.local` (configured in `appsettings.Development.json`). Passwords need at least 8 characters with an uppercase letter, a lowercase letter, a digit and a symbol. Changing the secret later does not change an existing admin's password.

## Authentication

Accounts use ASP.NET Core Identity with cookie authentication. Signing in sets an `HttpOnly`, `SameSite=Strict` cookie that the browser sends with every request; there are no tokens to store in JavaScript. Every endpoint requires a signed-in user unless it is explicitly marked anonymous.

Display names are unique, ignoring upper/lower case and surrounding spaces (like emails, a normalized copy is stored with a unique index). They also can't contain "admin" in any spelling (`ADMIN`, `4dm1n`, `a.d.m.i.n`, accented or full-width letters), so nobody can pass for the admin account.

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
| GET | `/api/quizzes/{id}/play` | Signed in | Questions and options to play (no answer key); the frontend shuffles the question order |
| GET | `/api/quizzes/{id}/edit` | Owner / admin | Full quiz including the correct answers, for the editor |
| POST | `/api/quizzes` | Signed in | Create a quiz (starts private) |
| POST | `/api/quizzes/official` | Admin | Create an official quiz |
| PUT | `/api/quizzes/{id}` | Owner / admin | Replace the title, description and all questions |
| DELETE | `/api/quizzes/{id}` | Owner / admin | Delete a quiz (past attempts are kept) |
| POST | `/api/quizzes/{id}/publish` | Owner | Share the quiz in the Community feed |
| POST | `/api/quizzes/{id}/unpublish` | Owner | Make the quiz private again |
| POST | `/api/quizzes/{id}/attempts` | Signed in | Submit answers in the order they were shown; the server grades them and saves the attempt in that order |
| GET | `/api/attempts` | Signed in | Your past attempts, newest first |
| GET | `/api/attempts/{id}` | Signed in | One of your attempts: each question, your answer and whether it was right |
| GET | `/api/documents` | Signed in | Your documents with their indexing status, newest first |
| GET | `/api/documents/{id}` | Owner | One document's status (the frontend polls this while it's being indexed) |
| POST | `/api/documents` | Signed in | Upload a PDF, Markdown or text file (multipart field `file`, up to 10 MB); indexing runs in the background |
| GET | `/api/documents/{id}/chunks` | Owner | The text chunks the document was split into |
| GET | `/api/documents/{id}/search?q=&top=` | Owner | The chunks most similar to a question (default 5, at most 10) |
| DELETE | `/api/documents/{id}` | Owner | Delete the document, its chunks and the stored file |
| GET | `/api/admin/users?search=` | Admin | Accounts with their roles; optional `search` matches display name or email |
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

A quiz you can't see returns `404`, one you can see but not change returns `403`. Answers are graded on the server, and the answer key is only sent to people who can edit the quiz. Results say whether each answer was right, but not which option was correct, so a quiz can't be learned by submitting it empty.

Each attempt stores a copy of the questions and options as they were when you played, so editing or deleting the quiz later doesn't change your past results.

## Documents and search

Documents are private: only their owner can see, search or delete them, and anyone else gets `404`. The upload checks the extension, the size and the first bytes of the file (a PDF must start with `%PDF-`). The file is stored under `backend/QuizMaster.Api/App_Data/documents` with a generated name, never the uploaded one.

A background worker indexes one document at a time:

1. Extract the text. PDFs are read with PdfPig, which only reads the text layer, so scanned PDFs fail with "No text found". Reading stops early once there is clearly too much text for the chunk limit, so a small compressed file that unpacks into a huge amount of text can't tie up the worker.
2. Split it into chunks of at most 1000 characters. A chunk ends at a paragraph or sentence end where possible, headings stay with the paragraph after them, and consecutive chunks share about 150 characters of whole sentences, so an idea that crosses a boundary is complete in at least one chunk.
3. Check the limit: a document that splits into more than 200 chunks (roughly 60 pages) fails here, before anything is sent to Gemini.
4. Embed the chunks with `gemini-embedding-001` (768 numbers per chunk), 50 per request, waiting and retrying if the free tier's rate limit is reached.
5. Save all chunks at once and mark the document **Ready**. Any error marks it **Failed** with a message.

The status goes Pending → Processing → Ready or Failed. Documents still Pending or Processing when the API stops are picked up again on the next start.

The vectors live in PostgreSQL itself (pgvector), in `document_chunks.embedding`, with an HNSW index for cosine distance. A search embeds the question the same way and returns the closest chunks of that one document. The chunk size, overlap, chunk limit and storage folder are in the `Documents` section of `appsettings.json`. The Gemini endpoint and model names are in the `Gemini` section.

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
  Services/                   Quiz validation and grading, Gemini settings
  Services/Documents/         Upload checks, file storage, text extraction, chunker, indexing worker, search
  App_Data/documents/         Uploaded files (not in Git)
frontend/
  vite.config.ts              Dev server and the /api proxy
  src/main.tsx                Entry point: TanStack Query and the router
  src/router.tsx              All routes
  src/api/                    fetch wrapper, shared types, query client
  src/components/             Layout, form fields, quiz cards and other shared pieces
  src/features/               Pages and API calls, grouped by feature (auth, quizzes, attempts, documents, admin)
  src/lib/                    Small helpers (date and size formatting)
  src/routes/                 Not-found and error pages
```
