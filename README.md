# QuizMaster

## Overview

Developed as a trainee project at The Access Group, QuizMaster supports manual and AI-assisted quiz authoring, quiz management, and server-side grading. The web app uses React/TypeScript, ASP.NET Core, EF Core with SQL Server LocalDB, Microsoft sign-in, and Ollama. A separate C# console app provides local RAG over fictional company documents using Ollama and Qdrant.

## Project Structure

```text
backend/
  Program.cs              API startup, authentication, CORS, Swagger
  Controllers/            Quiz endpoints and authorization
  Contracts/              Request and response DTOs
  Data/                   EF context, seed data, generated migrations
  Models/                 Quiz, question, and answer-option entities
  Repositories/           SQL persistence
  Services/               Grading, validation, local quiz agent
frontend/src/
  App.tsx                 View selection and application shell
  api.ts                  HTTP client and API error handling
  auth.ts                 MSAL configuration and token acquisition
  components/             Library, authoring, generation, play, owner controls
tests/QuizMaster.Tests/   Backend unit, API, agent, and opt-in live tests
scripts/                  Setup, live verification, quiz model definition
Program.cs                Separate RAG console entry point
Docs/                     Fictional Acme documents indexed by RAG
documentation/            Implementation notes, RAG guide, example source text
compose.yaml              Ollama, Qdrant, RAG console container
QuizMaster.slnx           .NET solution
```

Dependency versions: [frontend/package.json](frontend/package.json), [backend/QuizMaster.Api.csproj](backend/QuizMaster.Api.csproj), [AccessRag.csproj](AccessRag.csproj), and [the test project](tests/QuizMaster.Tests/QuizMaster.Tests.csproj). Keep `frontend/package-lock.json` and `.config/dotnet-tools.json` versioned.

## Commands

Run from the repository root in PowerShell. Full-app setup requires Windows, the .NET 10 SDK, Node.js/npm, SQL Server LocalDB, and Docker with Compose.

- **Setup:** start Docker and stop the API/frontend before running:

  ```powershell
  .\scripts\Initialize.ps1
  ```

  Starts services, downloads models, creates the quiz model, applies SQL migrations, and installs frontend dependencies. Preserves an existing `frontend/.env`.

- **Development:** run in separate terminals:

  ```powershell
  dotnet run --project backend
  ```

  ```powershell
  npm run dev --prefix frontend
  ```

  UI: <http://localhost:5173>. API/Swagger: <http://localhost:5023/swagger>.

- **Build:**

  ```powershell
  dotnet build QuizMaster.slnx
  npm run build --prefix frontend
  ```

- **Tests:** default suites run without live SQL or Ollama:

  ```powershell
  dotnet test tests/QuizMaster.Tests/QuizMaster.Tests.csproj
  npm test --prefix frontend
  ```

- **Single tests:**

  ```powershell
  dotnet test tests/QuizMaster.Tests/QuizMaster.Tests.csproj --filter FullyQualifiedName~AllCorrectAnswersProduceOneHundredPercent
  npm test --prefix frontend -- src/workflows.test.tsx -t "keeps answers and the submit button available after a grading failure"
  ```

- **Live verification:** choose one; stop the API/frontend first because both rerun setup:

  ```powershell
  .\scripts\Verify-Live.ps1
  .\scripts\Verify-Live.ps1 -SqlOnly
  ```

  Full verification includes SQL and real-model generation. `-SqlOnly` checks SQL and the frontend. No lint command is configured.

- **Browser walkthrough:** sign in with Microsoft, create and play a manual quiz, generate a quiz from source text, then edit its metadata and delete it from the library.

- **RAG console:** after services/models are initialized:

  ```powershell
  docker compose run --rm --build app
  ```

  Type `exit` to quit. See [documentation/RAG.md](documentation/RAG.md) for the standalone workflow.

## Architecture & Conventions

- React uses local component state and a view union in `App.tsx`. Persisted quizzes come from the API; MSAL manages authentication.
- Browser requests go through `frontend/src/api.ts`, which acquires a current bearer token and handles HTTP/Problem Details errors.
- Controllers validate requests and enforce scope/ownership; a scoped EF repository saves complete quiz graphs atomically. The server grades submissions; play responses exclude answer keys.
- The quiz agent uses `Microsoft.Extensions.AI` with Ollama. `get_quiz_context` supplies source text and existing quiz titles; `create_quiz` accepts four-option drafts with one correct-answer label. Only validated output is saved.
- RAG runs independently of quiz generation. It chunks `Docs/`, stores embeddings in Qdrant, and reuses unchanged chunks. The root `Dockerfile` and Compose `app` service run the console.
- Endpoint contracts and implementation scope are documented in [documentation/IMPLEMENTATION.md](documentation/IMPLEMENTATION.md).

## Guardrails & Gotchas

- Required frontend variables are `VITE_API_BASE_URL`, `VITE_ENTRA_CLIENT_ID`, and `VITE_REDIRECT_URI`; see [frontend/.env.example](frontend/.env.example). These are public browser settings. Keep secrets out of all `VITE_*` variables.
- Backend settings live in [backend/appsettings.json](backend/appsettings.json). Use .NET user secrets or an ignored `appsettings.Development.json` for private overrides; keep the shared settings file versioned.
- Defaults use the lab's shared Microsoft Entra registration with personal Microsoft accounts. An independent registration must match backend `AzureAd:ClientId` and frontend `VITE_ENTRA_CLIENT_ID`, expose `access_as_user`, and register the SPA redirects `http://localhost:5173` and `http://localhost:5023/swagger/oauth2-redirect.html`. No client secret is used.
- Ports must match redirect URIs and CORS settings. Frontend server settings are in `frontend/vite.config.ts`; the API launch profile is in `backend/Properties/launchSettings.json`.
- The local quiz model is defined in [scripts/quiz-model.json](scripts/quiz-model.json); endpoint and timeout are in backend configuration. Inference uses Ollama exclusively. CPU generation can take minutes; run live model tests and browser generation separately. Review generated answers for factual accuracy.
- EF migration designers and the model snapshot under `backend/Data/Migrations/` are generated. Generate schema changes with the pinned `dotnet ef` tool; retain migrations in Git. Do not edit `node_modules/`, `bin/`, `obj/`, or `dist/`.
- Live tests require `AI_LABS_LIVE_SQL=1` or `AI_LABS_LIVE_AI=1`; `Verify-Live.ps1` sets/restores these flags. SQL checks create/delete isolated `AiLabsTests_*` databases; AI checks generate 3, 5, and 10 questions, including a 20,000-character source.
- Local `.env` files, private configuration, certificates, database files, generated output, and Compose overrides are ignored. Docker volumes hold runtime data; `docker compose down -v` removes Compose-managed volumes. `Docs/` contains fictional training examples, not The Access Group policies.
