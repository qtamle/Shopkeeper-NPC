using UnityEngine;

// M1 test: on Play, send one request and print the reply in the Console.
public class LlmSmokeTest : MonoBehaviour
{
    [SerializeField] string endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    [SerializeField] string model = "gemini-3.5-flash-lite";
    [SerializeField, Range(0f, 2f)] float temperature = 0.7f;
    [SerializeField] string systemPrompt = "You are a helpful assistant. Answer in one short sentence.";
    [SerializeField] string userPrompt = "Say hello to a Unity developer.";

    async void Start()
    {
        var client = new LlmClient(endpoint, model);
        var messages = new[]
        {
            new ChatMessage("system", systemPrompt),
            new ChatMessage("user", userPrompt),
        };

        Debug.Log($"[LLM] Sending to {model}...");
        LlmResult result = await client.SendAsync(messages, temperature);

        if (result.ok)
            Debug.Log($"[LLM] ({result.latencyMs} ms) {result.text}");
        else
            Debug.LogError($"[LLM] Failed after {result.latencyMs} ms: {result.error}");
    }
}
