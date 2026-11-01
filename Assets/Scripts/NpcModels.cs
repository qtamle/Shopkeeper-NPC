using System;
using UnityEngine;

// Data classes for persona.json and inventory.json. Field names must match the JSON keys exactly,
// because JsonUtility maps by name and silently leaves unmatched fields empty.

[Serializable]
public class Persona
{
    public string name;
    public string role;
    public string personality;
    public string speaking_style;
    public string[] rules;
    // Shown when the model's output can't be used, so the player never sees an error.
    public string fallback_line;
}

[Serializable]
public class Inventory
{
    public string currency;
    public Item[] items;
}

[Serializable]
public class Item
{
    public string id;
    public string name;
    public string description;
    public int price;
    public int stock;
}

// What the generator must return. mentioned_items is what the M5 code check will verify.
[Serializable]
public class GeneratorOutput
{
    public string reply;
    public MentionedItem[] mentioned_items;

    // Never throws: the model's output is untrusted input, a bad answer must not crash the game.
    public static bool TryParse(string raw, out GeneratorOutput output, out string error)
    {
        output = null;
        error = null;

        // Without JSON mode models often wrap JSON in ```json ... ```; keep only the {...} part.
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            error = "No JSON object in the output.";
            return false;
        }

        try
        {
            output = JsonUtility.FromJson<GeneratorOutput>(raw.Substring(start, end - start + 1));
        }
        catch (Exception e)
        {
            error = "Invalid JSON: " + e.Message;
            return false;
        }

        // Valid JSON is not enough: JsonUtility leaves missing fields empty instead of failing.
        if (output == null || string.IsNullOrWhiteSpace(output.reply))
        {
            error = "JSON has no \"reply\".";
            return false;
        }

        output.mentioned_items ??= new MentionedItem[0];
        return true;
    }
}

// What NpcController hands to the UI: always something the player can read.
public class NpcReply
{
    public string text;
    public bool isFallback;
    public GeneratorOutput output;
}

[Serializable]
public class MentionedItem
{
    public string id;
    public int price;
}
