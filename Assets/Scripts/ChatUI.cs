using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds a minimal chat screen in code (log + input + send button) and passes questions to NpcController.
// Pure UI: it knows nothing about prompts or the LLM.
[RequireComponent(typeof(NpcController))]
public class ChatUI : MonoBehaviour
{
    const int FontSize = 28;

    NpcController npc;
    Text logText;
    InputField inputField;
    Button sendButton;
    Font font;

    readonly List<string> lines = new List<string>();
    bool busy;

    void Awake()
    {
        npc = GetComponent<NpcController>();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
    }

    void Start()
    {
        AddLine($"<color=#9aa0a6>Talk to {npc.NpcName}. Type a message and press Enter.</color>");
        inputField.ActivateInputField();
    }

    async void Send()
    {
        string question = inputField.text.Trim();
        // busy: one request at a time, so a double click can't send the same question twice.
        if (busy || question.Length == 0) return;

        busy = true;
        sendButton.interactable = false;
        inputField.text = "";
        AddLine($"<b><color=#8ab4f8>You:</color></b> {question}");
        AddLine($"<i><color=#9aa0a6>{npc.NpcName} is thinking...</color></i>");

        LlmResult result = await npc.AskAsync(question);

        // Play mode may have stopped while we were waiting for the network.
        if (this == null) return;

        lines.RemoveAt(lines.Count - 1);
        AddLine(result.ok
            ? $"<b><color=#fdd663>{npc.NpcName}:</color></b> {result.text}"
            : $"<color=#f28b82>(error: {result.error})</color>");

        busy = false;
        sendButton.interactable = true;
        inputField.ActivateInputField();
    }

    void AddLine(string line)
    {
        lines.Add(line);
        logText.text = string.Join("\n\n", lines);
    }

    void OnInputEndEdit(string _)
    {
        // onEndEdit also fires when the field loses focus; only send on Enter.
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            Send();
    }

    // ---------- UI construction ----------

    void BuildUI()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("ChatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        var background = CreateRect("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        background.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f);

        // Chat log: text is bottom-aligned and overflows upward, the mask hides the oldest lines.
        var logArea = CreateRect("Log", root, new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 130), new Vector2(-40, -40));
        logArea.gameObject.AddComponent<RectMask2D>();
        logText = CreateText("LogText", logArea, "", TextAnchor.LowerLeft);
        logText.verticalOverflow = VerticalWrapMode.Overflow;

        // Input field
        var inputRect = CreateRect("Input", root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 30), new Vector2(-260, 100));
        inputRect.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.25f);
        inputField = inputRect.gameObject.AddComponent<InputField>();
        var inputText = CreateText("Text", inputRect, "", TextAnchor.MiddleLeft, 20);
        inputText.supportRichText = false;
        var placeholder = CreateText("Placeholder", inputRect, "Ask the shopkeeper...", TextAnchor.MiddleLeft, 20);
        placeholder.color = new Color(1, 1, 1, 0.35f);
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.onEndEdit.AddListener(OnInputEndEdit);

        // Send button
        var buttonRect = CreateRect("Send", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-240, 30), new Vector2(-40, 100));
        buttonRect.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.82f, 0.36f);
        sendButton = buttonRect.gameObject.AddComponent<Button>();
        var buttonText = CreateText("Text", buttonRect, "Send", TextAnchor.MiddleCenter);
        buttonText.color = Color.black;
        sendButton.onClick.AddListener(Send);
    }

    static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    Text CreateText(string name, Transform parent, string content, TextAnchor alignment, float padding = 0)
    {
        var rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(padding, 0), new Vector2(-padding, 0));
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = FontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.text = content;
        return text;
    }
}
