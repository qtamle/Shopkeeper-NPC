using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Runs one NPC turn: prompt (with history) → LLM → parse JSON → reply or fallback. Checking and retry come in M5–M6.
public class NpcController : MonoBehaviour
{
    [Header("LLM")]
    [SerializeField] string endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    [SerializeField] string model = "gemini-3.5-flash-lite";
    [SerializeField, Range(0f, 2f)] float temperature = 0.7f;

    [Header("Data")]
    [SerializeField] TextAsset personaJson;
    [SerializeField] TextAsset inventoryJson;
    [SerializeField] TextAsset systemPromptTemplate;

    [Header("Memory")]
    [Tooltip("How many past player+NPC exchanges are re-sent with each request.")]
    [SerializeField, Min(0)] int historyTurns = 6;

    [Header("Debug")]
    [Tooltip("Print every message sent in each request, to see what the model actually receives.")]
    [SerializeField] bool logRequestMessages = true;

    LlmClient client;
    PromptBuilder promptBuilder;
    Persona persona;
    Inventory inventory;
    // Alternating user / assistant messages. This list IS Mira's memory; the API remembers nothing.
    readonly List<ChatMessage> history = new List<ChatMessage>();

    public string NpcName { get; private set; } = "NPC";
    public bool IsReady => promptBuilder != null;
    public string Currency => inventory?.currency;

    // The real item from inventory.json, or null if the model used an id we don't sell.
    public Item FindItem(string id) => inventory?.items?.FirstOrDefault(i => i.id == id);

    void Awake()
    {
        try
        {
            persona = JsonUtility.FromJson<Persona>(personaJson.text);
            inventory = JsonUtility.FromJson<Inventory>(inventoryJson.text);
            promptBuilder = new PromptBuilder(systemPromptTemplate.text, persona, inventory);
            NpcName = persona.name;
        }
        catch (Exception e)
        {
            // Our own data files: if they are broken, fail loudly so we fix them.
            Debug.LogError("[NPC] Could not load persona/inventory/prompt: " + e.Message);
            return;
        }

        client = new LlmClient(endpoint, model);
        Debug.Log("[NPC] System prompt:\n" + promptBuilder.SystemPrompt);
    }

    public async Task<NpcReply> AskAsync(string playerMessage)
    {
        if (!IsReady)
            return Fallback("NPC data is not loaded.");

        ChatMessage[] messages = promptBuilder.Build(history, playerMessage);
        if (logRequestMessages)
            Debug.Log($"[NPC] Request with {messages.Length} messages:\n" +
                      string.Join("\n", messages.Select((m, i) => $"#{i} [{m.role}] {Preview(m.content)}")));

        LlmResult result = await client.SendAsync(messages, temperature, jsonMode: true);

        if (!result.ok)
            return Fallback($"LLM call failed after {result.latencyMs} ms: {result.error}");

        if (!GeneratorOutput.TryParse(result.text, out var output, out string parseError))
            return Fallback($"{parseError} Raw output: {result.text}");

        Debug.Log($"[NPC] ({result.latencyMs} ms, {messages.Length} messages) reply: {output.reply}\n" +
                  $"mentioned_items: {string.Join(", ", output.mentioned_items.Select(i => $"{i.id}={i.price}"))}");

        // Store the raw JSON, not just the reply: the model copies the format of its own past answers.
        // Failed turns are not stored, so one broken answer can't teach the model a broken format.
        history.Add(new ChatMessage("user", playerMessage));
        history.Add(new ChatMessage("assistant", result.text));
        TrimHistory();

        return new NpcReply { text = output.reply, output = output };
    }

    // First 80 characters on one line, so a long system prompt doesn't flood the Console.
    static string Preview(string s)
    {
        string oneLine = s.Replace("\n", " ");
        return oneLine.Length <= 80 ? oneLine : oneLine.Substring(0, 80) + $"... ({s.Length} chars)";
    }

    void TrimHistory()
    {
        int maxMessages = historyTurns * 2;
        if (history.Count > maxMessages)
            history.RemoveRange(0, history.Count - maxMessages);
    }

    NpcReply Fallback(string reason)
    {
        Debug.LogError("[NPC] Using fallback line. " + reason);
        string line = string.IsNullOrWhiteSpace(persona?.fallback_line)
            ? "(fallback_line is empty in persona.json)"
            : persona.fallback_line;
        return new NpcReply { text = line, isFallback = true };
    }

#if UNITY_EDITOR
    // Called when the component is added: fill in the default data files so there is nothing to drag.
    void Reset()
    {
        personaJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/persona.json");
        inventoryJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/inventory.json");
        systemPromptTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/system_prompt.txt");
    }
#endif
}
