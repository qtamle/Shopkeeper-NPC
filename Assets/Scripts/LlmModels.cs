using System;

// Request/response shapes for the OpenAI-compatible "chat completions" format.
// Gemini, OpenAI and most other providers accept this same shape, so only the
// endpoint and model name change if we switch provider.

[Serializable]
public class ChatMessage
{
    // "system" = rules for the model, "user" = the player, "assistant" = the model's earlier replies.
    public string role;
    public string content;

    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[Serializable]
public class ChatRequest
{
    public string model;
    // The API keeps no memory: every request must carry the whole conversation.
    public ChatMessage[] messages;
    // 0 = most predictable, higher = more varied wording.
    public float temperature;
}

[Serializable]
public class ChatResponse
{
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public ChatMessage message;
    // "stop" = finished normally, "length" = cut off by the token limit.
    public string finish_reason;
}

// What LlmClient hands back: never throws, the caller checks ok.
public class LlmResult
{
    public bool ok;
    public string text;
    public string error;
    public long latencyMs;

    public static LlmResult Success(string text, long ms) =>
        new LlmResult { ok = true, text = text, latencyMs = ms };

    public static LlmResult Fail(string error, long ms) =>
        new LlmResult { ok = false, error = error, latencyMs = ms };
}
