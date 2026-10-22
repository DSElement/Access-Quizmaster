# QuizMaster implementation

The project follows the final lab designs rather than reproducing the teaching sequence. The separate StudyHub specification and optional homework features are outside this application.

## Four increments

1. **Persistent API:** ASP.NET Core, EF Core/LocalDB, the complete quiz/question/option schema, deterministic playable seed data, Microsoft bearer authentication, scoped data access, and owner-protected writes.
2. **Browser workflow:** MSAL sign-in, an API-backed library, atomic manual authoring, details, server-graded play, and owner title/description editing and deletion. There is no browser-only quiz store or separate offline runner.
3. **Local quiz agent:** the final Lab 10 tool-calling design through `Microsoft.Extensions.AI`, using Ollama exclusively. The agent obtains existing titles and the source text from `get_quiz_context`, submits a typed quiz through `create_quiz`, receives validation feedback when needed, and leaves persistence to the API. The source enters the conversation only after the metadata lookup, and a valid submission ends inference immediately.
4. **Verification and setup:** focused xUnit/API/agent tests, React workflow tests, opt-in SQL and Ollama checks, migrations, a pinned EF tool, and reproducible PowerShell setup instructions.

The root console project remains independent. Its compile and Docker inputs exclude the web application and test trees. `Docs/` still contains only the original six company documents; project documentation lives in `documentation/`.

## Data and API contracts

The SQL database stores quizzes, ordered questions, and ordered answer options. Each quiz has a server-assigned ID, UTC creation time, and the creator's validated `oid`. Question and option IDs are also server-assigned. Deleting a quiz cascades through its children. Results are computed per submission and are not stored as attempt history.

| Method and route | Access | Behavior |
|---|---|---|
| `GET /quizzes` | Public | Safe metadata summaries, question counts, and caller-derived `canEdit`. |
| `GET /quizzes/{id}` | Signed in with API scope | Safe ordered question/option details. |
| `POST /quizzes` | Signed in with API scope | Creates metadata and an optional nested question collection in one save. |
| `PUT /quizzes/{id}` | Owner | Replaces title/description; preserves questions and ownership. |
| `DELETE /quizzes/{id}` | Owner | Deletes the quiz and its questions/options. |
| `POST /quizzes/{id}/questions` | Owner | Appends one validated question. |
| `POST /quizzes/generate` | Signed in with API scope | Runs the local agent and saves the validated quiz atomically. |
| `GET /quizzes/{id}/play` | Public | Returns questions/options with no answer key. |
| `POST /quizzes/{id}/evaluate` | Public | Grades selected option IDs and returns the score and answer breakdown. |
| `GET /health` | Public | Process liveness only; does not assert database/model readiness. |

The browser still requires sign-in before showing the app, matching the final guides. It sends a fresh MSAL token on API calls so ownership controls reflect the current account. Server ownership and scope checks remain authoritative.

Create accepts `{ title, description?, questions: [{ text, options: [{ text, isCorrect }] }] }`. Omitting `questions` creates an empty quiz for the API's metadata-then-add-question workflow. The browser requires at least one complete question. No read DTO or create/generate response includes `isCorrect`; it appears in authoring requests and graded results only.

Manual questions need at least two nonblank options and exactly one correct option. Generated quizzes require exactly the requested number of questions and four options per question. The generation request accepts 200–20,000 source characters, 3–10 questions, and an optional title. A supplied title takes precedence over the model's title.

The AI submission tool uses a smaller draft contract: question text, `optionA` through `optionD`, and one `correctOption` label (`A`–`D`). Mapping that draft produces exactly four options with one correctness flag. General quiz validation still checks all text and the requested count. The tool contract does not change the public authoring API. The local `quizmaster-qwen` model is built from the pinned Qwen instruct tag with a 16,384-token context; its definition lives in `scripts/quiz-model.json`.

Missing answers are incorrect. Unknown question IDs, duplicate answer entries, and option IDs belonging to another question are rejected. Percentages use one decimal place. Input errors use `400`, unauthenticated writes use `401`, non-owner writes use `403`, and missing resources use `404`. Generation failures use `502`; a generation timeout uses `504`. Error responses with explanatory content use Problem Details.

## Corrections to the reference solution

- The public list no longer serializes EF entities containing answer keys.
- Question addition checks ownership before modifying a quiz.
- Manual and AI authoring save a complete graph in one transaction.
- Generated titles are captured rather than replaced by a timestamp regardless of the agent's instructions.
- SQL preserves question/option order explicitly.
- The browser reloads persisted quizzes after refresh instead of maintaining a second transient list.
- A failed grade submission leaves selections and the submission controls available.
- Native modal dialogs provide focus containment and keyboard cancellation for owner actions.

## Intentional boundaries

RAG is a separate console assistant; it is not part of quiz generation. There is no hosted-model fallback, local authentication substitute, development login bypass, response-repair parser, or automatic switch of model/database. Missing configuration fails explicitly. The API and UI handle expected failures at their boundaries.

There are no uploads, tutor screens, dashboards, attempt history, tags, pagination, question-editing endpoints, extra agent tools, or server-enforced topic uniqueness. The existing RAG index behavior and documented limitations are preserved.
