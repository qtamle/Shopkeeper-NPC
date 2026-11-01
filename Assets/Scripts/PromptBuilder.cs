using System.Collections.Generic;
using System.Linq;

// Turns persona + inventory + the player's message into the messages array for the LLM.
// The wording lives in system_prompt.txt, not here, so the prompt can change without touching code.
public class PromptBuilder
{
    readonly string systemPrompt;

    public PromptBuilder(string template, Persona persona, Inventory inventory)
    {
        // Built once: persona and inventory don't change during a chat.
        systemPrompt = template
            .Replace("{name}", persona.name)
            .Replace("{role}", persona.role)
            .Replace("{personality}", persona.personality)
            .Replace("{speaking_style}", persona.speaking_style)
            .Replace("{rules}", string.Join("\n", persona.rules.Select((r, n) => $"{n + 1}. {r}")))
            .Replace("{currency}", inventory.currency)
            .Replace("{items}", string.Join("\n", inventory.items.Select(i =>
                $"- {i.name} (id: {i.id}): {i.description} (Price: {i.price} {inventory.currency}, Stock: {i.stock})")));
    }

    public string SystemPrompt => systemPrompt;

    // The API is stateless: every request = system prompt + past turns + the new question.
    public ChatMessage[] Build(IEnumerable<ChatMessage> history, string playerMessage)
    {
        var messages = new List<ChatMessage> { new ChatMessage("system", systemPrompt) };
        messages.AddRange(history);
        messages.Add(new ChatMessage("user", playerMessage));
        return messages.ToArray();
    }
}
