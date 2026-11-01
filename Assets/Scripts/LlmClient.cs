using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Sends one chat request and returns the model's text. Knows nothing about NPCs.
public class LlmClient
{
    const string ApiKeyEnvVar = "LLM_API_KEY";

    readonly string endpoint;
    readonly string model;
    readonly int timeoutSeconds;

    public LlmClient(string endpoint, string model, int timeoutSeconds = 30)
    {
        this.endpoint = endpoint;
        this.model = model;
        this.timeoutSeconds = timeoutSeconds;
    }

    public async Task<LlmResult> SendAsync(ChatMessage[] messages, float temperature)
    {
        var timer = Stopwatch.StartNew();

        // Read the key at call time, never store it in a serialized field (it would end up in the scene file).
        string apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
        if (string.IsNullOrEmpty(apiKey))
            return LlmResult.Fail($"{ApiKeyEnvVar} is not set. Set it, then restart Unity Hub.", 0);

        var body = new ChatRequest { model = model, messages = messages, temperature = temperature };
        byte[] json = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));

        using var request = new UnityWebRequest(endpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(json);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.timeout = timeoutSeconds;

        // Wait frame by frame instead of blocking, so the game keeps running while the model thinks.
        var operation = request.SendWebRequest();
        while (!operation.isDone)
            await Awaitable.NextFrameAsync();

        long ms = timer.ElapsedMilliseconds;

        if (request.result != UnityWebRequest.Result.Success)
        {
            // The response body explains HTTP errors (bad model name, quota...). It never contains the key.
            string detail = Truncate(request.downloadHandler?.text, 300);
            return LlmResult.Fail($"{request.result} {request.responseCode}: {request.error} {detail}", ms);
        }

        ChatResponse response;
        try
        {
            response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            return LlmResult.Fail("Could not parse response JSON: " + e.Message, ms);
        }

        string text = response?.choices != null && response.choices.Length > 0
            ? response.choices[0].message?.content
            : null;

        if (string.IsNullOrWhiteSpace(text))
            return LlmResult.Fail("Empty reply from the model.", ms);

        return LlmResult.Success(text.Trim(), ms);
    }

    static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + "...";
}
