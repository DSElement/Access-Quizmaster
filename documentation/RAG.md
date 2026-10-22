# ACCESS · Local RAG Assistant

**C# / .NET 10 · Ollama · Qdrant · Docker Compose**

A practical project developed during ACCESS, building on labs 1–4: a console assistant that searches fictional company documents before answering questions about HR, IT, expenses, and products.


## What it demonstrates

- **Ingestion:** Markdown → 700-character chunks with 100-character overlap → 768-dimensional embeddings → Qdrant.
- **Retrieval:** Embed a question, search by cosine similarity, optionally filter by category, and supply passages to the assistant.
- **Local AI:** `llama3.2:3b` generates answers; `embeddinggemma` embeds documents and queries.
- **Persistence:** Stable chunk IDs let unchanged documents reuse stored embeddings after relaunch. The assistant is instructed to cite source filenames.

## Run it

Install Docker Desktop with Linux containers and Docker Compose. No hosted API key is needed; models run on the CPU by default. Internet is needed for the initial image/model downloads. A host .NET SDK is unnecessary for the container workflow.

From this repository folder:

```powershell
docker compose up -d --wait qdrant ollama
docker compose exec ollama ollama pull llama3.2:3b
docker compose exec ollama ollama pull embeddinggemma
docker compose run --rm --build app
```

Try **“How many days of annual leave do employees get?”** or **“How do I report an IT issue?”** Type `exit` to close the app. Repeat the last command to relaunch; unchanged documents should report `0 chunks embedded` and a nonzero reused count.

Model pulls are required once per fresh model volume; repeating them checks for updates. Service health checks confirm readiness, not model availability.

## How Compose fits together

`compose.yaml` defines three services, their network, health checks, and storage. Qdrant and Ollama run in the background; `compose run` starts an interactive app container and removes it on exit. Inside the network, the app connects to `qdrant:6334` and `ollama:11434`.

Host endpoints: [Qdrant dashboard](http://localhost:6333/dashboard), Qdrant gRPC at `localhost:6334`, and Ollama at `localhost:11434`. The application is a console program.

```powershell
docker compose down
```

This stops/removes Compose services while retaining their named volumes. Automatic restart policies are omitted.

**On the original development machine:** an ignored `compose.override.yaml` reuses `lab4_qdrant_data` and `lab4_ollama_data`, including existing embeddings and downloaded models. Keep the old `lab4-qdrant` and `lab4-ollama` containers stopped: they share these volumes and host ports. A fresh clone uses its own volumes. Avoid `down -v` when you want to retain Compose-managed data.

## Working on it

Edit `Program.cs` and `Docs/`, then rerun `docker compose run --rm --build app`. Changed chunks are embedded again; unchanged chunks are reused.

Current learning-project limits: chat history lasts only for the current session; deleted documents or shortened files leave obsolete chunks; retrieval has no minimum similarity threshold; model-selected tool calls and citations need checking. Similarity scores are not confidence percentages.
