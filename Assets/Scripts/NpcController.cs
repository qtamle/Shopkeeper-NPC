using System;
using System.Threading.Tasks;
using UnityEngine;

// Runs one NPC turn. M2: prompt → LLM → reply. Checking, retry and fallback are added in M5–M6.
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

    LlmClient client;
    PromptBuilder promptBuilder;

    public string NpcName { get; private set; } = "NPC";
    public bool IsReady => promptBuilder != null;

    void Awake()
    {
        try
        {
            var persona = JsonUtility.FromJson<Persona>(personaJson.text);
            var inventory = JsonUtility.FromJson<Inventory>(inventoryJson.text);
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

    public async Task<LlmResult> AskAsync(string playerMessage)
    {
        if (!IsReady)
            return LlmResult.Fail("NPC data is not loaded.", 0);

        ChatMessage[] messages = promptBuilder.Build(playerMessage);
        LlmResult result = await client.SendAsync(messages, temperature);

        if (result.ok)
            Debug.Log($"[NPC] ({result.latencyMs} ms) {result.text}");
        else
            Debug.LogError($"[NPC] Failed after {result.latencyMs} ms: {result.error}");

        return result;
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
