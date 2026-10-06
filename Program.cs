using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using CommunityToolkit.VectorData.Qdrant;
using Qdrant.Client;
using System.Security.Cryptography;
using System.Text;
using OpenAI;
using OpenAI.Chat;

// ─── config ────────────────────────────────────────────────────────────────

var chatModelId = "llama3.2:3b";
var embeddingModelId = "embeddinggemma";
var ollamaEndpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434/v1/";
var qdrantHost = Environment.GetEnvironmentVariable("QDRANT_HOST") ?? "localhost";
var openAIClient = new OpenAIClient(
    new ApiKeyCredential("ollama"),
    new OpenAIClientOptions
    {
        Endpoint = new Uri(ollamaEndpoint),
        NetworkTimeout = TimeSpan.FromMinutes(5)
    });

// ─── ingestion ─────────────────────────────────────────────────────────────

var embeddingClient = openAIClient.GetEmbeddingClient(embeddingModelId);
var embeddingOptions = new OpenAI.Embeddings.EmbeddingGenerationOptions();

var qdrantClient = new QdrantClient(qdrantHost, 6334);
var vectorStore = new QdrantVectorStore(qdrantClient, true);
var collection = vectorStore.GetCollection<Guid, Chunk>(
    "company-docs-embeddinggemma-768");

await collection.EnsureCollectionExistsAsync();

var docsPath = Path.Combine(AppContext.BaseDirectory, "Docs");
var embedded = 0;
var reused = 0;

foreach (var file in Directory.GetFiles(docsPath, "*.md"))
{
    var source = Path.GetFileName(file);
    var text = await File.ReadAllTextAsync(file);
    var category = InferCategory(source);
    var chunkIndex = 0;

    foreach (var chunk in Chunker.Split(text, chunkSize: 700, overlap: 100))
    {
        var id = CreateChunkId(source, chunkIndex++);
        var existing = await collection.GetAsync(id);

        if (existing is not null &&
            existing.Text == chunk &&
            existing.Category == category)
        {
            reused++;
            continue;
        }

        var result = await embeddingClient.GenerateEmbeddingAsync(
            $"title: {source} | text: {chunk}",
            embeddingOptions);

        var vector = result.Value.ToFloats().ToArray();

        if (vector.Length != 768)
            throw new InvalidOperationException(
                $"Expected 768 embedding dimensions, received {vector.Length}.");

        await collection.UpsertAsync(new Chunk
        {
            Id = id,
            Text = chunk,
            Source = source,
            Category = category,
            Vector = new ReadOnlyMemory<float>(vector)
        });

        embedded++;
    }
}

Console.WriteLine(
    $"Ready: {embedded} chunks embedded, {reused} reused from Qdrant.\n");

static Guid CreateChunkId(string source, int index)
{
    var hash = SHA256.HashData(
        Encoding.UTF8.GetBytes($"{source}:{index}"));

    return new Guid(hash.AsSpan(0, 16));
}

// ─── agent ─────────────────────────────────────────────────────────────────

var kb = new KnowledgeBase(collection, embeddingClient, embeddingOptions);

var agent = openAIClient
    .GetChatClient(chatModelId)
    .AsAIAgent(
        instructions:
            "You are a helpful company assistant for Acme Software Ltd. " +
            "For any question about internal policies, IT, HR, benefits, expenses, or products, " +
            "always call search_knowledge_base before answering. " +
            "You may pass a category filter (HR, IT, Finance, Product) when the topic is clear. " +
            "If the knowledge base returns no relevant result, say so clearly rather than guessing. " +
            "Cite the source document when you use retrieved information.",
        name: "CompanyAssistant",
        tools: [AIFunctionFactory.Create(kb.SearchAsync, name: "search_knowledge_base")]);

var session = await agent.CreateSessionAsync();

Console.WriteLine($"Company assistant ready (model: {chatModelId}).");
Console.WriteLine("Ask anything about company policies, IT, or HR. Type 'exit' to quit.\n");

// ─── chat loop ─────────────────────────────────────────────────────────────

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    Console.Write("Assistant: ");
    await foreach (var update in agent.RunStreamingAsync(input, session))
        Console.Write(update.Text);

    Console.WriteLine("\n");
}

// ─── helpers ───────────────────────────────────────────────────────────────

static string InferCategory(string filename) =>
    Path.GetFileNameWithoutExtension(filename).ToLowerInvariant() switch
{
    "leave-policy" or "benefits-handbook" or "onboarding-guide" => "HR",
    "it-helpdesk-faq" => "IT",
    "expense-policy" => "Finance",
    "product-faq" => "Product",
    _ => "General",
};

// ─── types ─────────────────────────────────────────────────────────────────

class Chunk
{
    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [VectorStoreData]
    public string Text { get; set; } = "";

    [VectorStoreData]
    public string Source { get; set; } = "";

    [VectorStoreData(IsIndexed = true)]
    public string Category { get; set; } = "";

    [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}

static class Chunker
{
    public static IEnumerable<string> Split(string text, int chunkSize, int overlap)
    {
        int start = 0;
        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);
            yield return text[start..end];
            if (end == text.Length) break;
            start += chunkSize - overlap;
        }
    }
}

class KnowledgeBase(
    VectorStoreCollection<Guid, Chunk> collection,
    OpenAI.Embeddings.EmbeddingClient embeddingClient,
    OpenAI.Embeddings.EmbeddingGenerationOptions embeddingOptions)
{
    [Description(
        "Search the company knowledge base for information about HR policies, IT support, " +
        "benefits, onboarding, expenses, or products. " +
        "Always call this tool before answering questions about internal company topics.")]
    public async Task<string> SearchAsync(
        [Description("The user's question, rephrased as a focused search query")] string query,
        [Description("Optional category filter: HR, IT, Finance, Product. Leave empty to search all.")] string? category = null,
        [Description("Number of results to return. Default is 3.")] int topK = 3)
    {
        var result = await embeddingClient.GenerateEmbeddingAsync(
            $"task: search result | query: {query}",
            embeddingOptions);
        var queryVector = new ReadOnlyMemory<float>(result.Value.ToFloats().ToArray());

        var options = new VectorSearchOptions<Chunk>
        {
            Filter = !string.IsNullOrEmpty(category)
                ? r => r.Category == category
                : null,
        };

        var results = new List<string>();
        await foreach (var hit in collection.SearchAsync(queryVector, topK, options))
            results.Add($"[Source: {hit.Record.Source} | Score: {hit.Score:P0}]\n{hit.Record.Text}");

        return results.Count > 0
            ? string.Join("\n\n---\n\n", results)
            : "No relevant information found in the knowledge base for this query.";
    }
}
