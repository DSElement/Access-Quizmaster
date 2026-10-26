using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace QuizMaster.Api.Services;

// Gemini 3 models attach a "thought signature" to each tool call (tool_calls[].extra_content) and reject the next
// request unless the call is sent back with it. The OpenAI SDK drops fields it doesn't know, so this policy
// remembers the signatures from responses and puts them back into the tool calls of later requests.
public sealed class GeminiThoughtSignaturePolicy : PipelinePolicy
{
    // Tool call ids are unique, so old entries are only cleared to keep memory bounded.
    private const int MaxRemembered = 10_000;

    private readonly ConcurrentDictionary<string, JsonNode> signatures = new();

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        AddSignatures(message);
        ProcessNext(message, pipeline, currentIndex);
        RememberSignatures(message);
    }

    public override async ValueTask ProcessAsync(
        PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        AddSignatures(message);
        await ProcessNextAsync(message, pipeline, currentIndex);
        RememberSignatures(message);
    }

    private void RememberSignatures(PipelineMessage message)
    {
        if (!IsChatCompletion(message) || !message.BufferResponse || message.Response is not { IsError: false } response)
            return;

        JsonNode? toolCalls;
        try
        {
            toolCalls = JsonNode.Parse(response.Content.ToStream())?["choices"]?[0]?["message"]?["tool_calls"];
        }
        catch (JsonException)
        {
            // Not this policy's job to fail the request; the SDK reports a broken response itself.
            return;
        }
        if (toolCalls is not JsonArray calls) return;

        if (signatures.Count > MaxRemembered) signatures.Clear();

        foreach (var call in calls)
        {
            if (call?["id"]?.GetValue<string>() is { } id && call["extra_content"] is { } extra)
                signatures[id] = extra.DeepClone();
        }
    }

    private void AddSignatures(PipelineMessage message)
    {
        if (!IsChatCompletion(message) || message.Request.Content is not { } content || signatures.IsEmpty)
            return;

        using var buffer = new MemoryStream();
        content.WriteTo(buffer);
        var body = JsonNode.Parse(buffer.ToArray());
        if (body?["messages"] is not JsonArray messages) return;

        var changed = false;
        foreach (var call in messages.SelectMany(m => m?["tool_calls"] as JsonArray ?? []).OfType<JsonObject>())
        {
            if (call["extra_content"] is null &&
                call["id"]?.GetValue<string>() is { } id &&
                signatures.TryGetValue(id, out var extra))
            {
                call["extra_content"] = extra.DeepClone();
                changed = true;
            }
        }

        if (changed)
            message.Request.Content = BinaryContent.Create(BinaryData.FromString(body.ToJsonString()));
    }

    private static bool IsChatCompletion(PipelineMessage message) =>
        message.Request.Uri?.AbsolutePath.EndsWith("/chat/completions", StringComparison.Ordinal) == true;
}
