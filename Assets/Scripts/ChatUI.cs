using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds a visual-novel style dialogue box in code (last question, NPC name, reply, input) and passes questions to NpcController.
// Pure UI: it knows nothing about prompts or the LLM. The full history lives in NpcController; this only shows the latest turn.
[RequireComponent(typeof(NpcController))]
public class ChatUI : MonoBehaviour
{
    static readonly Color NameColor = new Color(0.96f, 0.89f, 0.74f);
    static readonly Color ReplyColor = new Color(0.98f, 0.96f, 0.92f);
    static readonly Color MutedColor = new Color(0.68f, 0.64f, 0.60f);
    static readonly Color GoldColor = new Color(0.78f, 0.62f, 0.38f, 0.8f);
    const string ItemColorHex = "#ffcf70";
    const string PriceColorHex = "#9be3a8";

    [Tooltip("Optional TTF font, e.g. EB Garamond. Empty = Unity's built-in font.")]
    [SerializeField] Font font;

    NpcController npc;
    Text questionText;
    Text nameText;
    Text replyText;
    Text placeholderText;
    InputField inputField;
    bool busy;

    void Awake()
    {
        npc = GetComponent<NpcController>();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
    }

    void Start()
    {
        // NpcController loads the persona in Awake, so the name is ready here.
        nameText.text = npc.NpcName;
        placeholderText.text = $"Whisper to {npc.NpcName}...";
        ShowReply($"Talk to {npc.NpcName}.", MutedColor, FontStyle.Italic);
        inputField.ActivateInputField();
    }

    async void Send()
    {
        string question = inputField.text.Trim();
        // busy: one request at a time, so pressing Enter twice can't send the same question twice.
        if (busy || question.Length == 0) return;

        busy = true;
        inputField.text = "";
        questionText.text = $"You: {question}";
        ShowReply("...", MutedColor, FontStyle.Italic);

        NpcReply reply = await npc.AskAsync(question);

        // Play mode may have stopped while we were waiting for the network.
        if (this == null) return;

        // Fallback lines are greyed out only to help us spot them while testing.
        ShowReply(reply.text, reply.isFallback ? MutedColor : ReplyColor, FontStyle.Normal, reply.output?.mentioned_items);

        busy = false;
        inputField.ActivateInputField();
    }

    void ShowReply(string text, Color color, FontStyle style, MentionedItem[] mentioned = null)
    {
        replyText.text = Highlight(Sanitize(text), mentioned);
        replyText.color = color;
        replyText.fontStyle = style;
    }

    // Legacy Text reads "<...>" as formatting tags. Removing '<' from the model's text means only our own tags can format it.
    static string Sanitize(string text) => text.Replace('<', '‹');

    // Highlights the items the generator listed in mentioned_items, using the name and price from inventory.json,
    // not the model's own numbers. So only a real item at its real price lights up; a wrong price stays plain.
    string Highlight(string text, MentionedItem[] mentioned)
    {
        if (mentioned == null) return text;

        foreach (string id in mentioned.Select(m => m.id).Distinct())
        {
            Item item = npc.FindItem(id);
            if (item == null) continue;
            text = Wrap(text, Regex.Escape(item.name), ItemColorHex);
            text = Wrap(text, $@"\b{item.price}\s+{Regex.Escape(npc.Currency ?? "gold")}\b", PriceColorHex);
        }
        return text;
    }

    static string Wrap(string text, string pattern, string colorHex)
        => Regex.Replace(text, pattern, m => $"<color={colorHex}><b>{m.Value}</b></color>", RegexOptions.IgnoreCase);

    void OnInputEndEdit(string _)
    {
        // onEndEdit also fires when the field loses focus; only send on Enter.
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            Send();
    }

    // ---------- UI construction ----------
    // Positions are in reference pixels from the bottom of a 1920x1080 screen; the box sits on the counter.

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

        // Darkens the counter so light text stays readable on the wood.
        var shade = CreateRect("Shade", root, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 360));
        AddImage(shade, new Color(0, 0, 0, 0.45f));

        questionText = AddText(Row("Question", root, 1400, 310, 350), 24, MutedColor, TextAnchor.MiddleCenter, FontStyle.Italic);
        nameText = AddText(Row("Name", root, 600, 262, 314), 44, NameColor, TextAnchor.MiddleCenter);
        AddImage(Row("Divider", root, 360, 256, 258), GoldColor);

        replyText = AddText(Row("Reply", root, 1400, 120, 250), 34, ReplyColor, TextAnchor.MiddleCenter);
        // Long replies shrink instead of spilling out of the box.
        replyText.resizeTextForBestFit = true;
        replyText.resizeTextMinSize = 22;
        replyText.resizeTextMaxSize = 34;
        // Rich text only here, for our highlight tags. The model's own '<' is removed first (see Sanitize).
        replyText.supportRichText = true;

        // Input field: invisible background (still clickable), text sits on a thin gold underline.
        var inputRect = Row("Input", root, 900, 40, 95);
        AddImage(inputRect, new Color(0, 0, 0, 0));
        inputField = inputRect.gameObject.AddComponent<InputField>();
        var inputText = AddText(Stretch("Text", inputRect), 28, ReplyColor, TextAnchor.MiddleCenter);
        placeholderText = AddText(Stretch("Placeholder", inputRect), 28, MutedColor, TextAnchor.MiddleCenter, FontStyle.Italic);
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.onEndEdit.AddListener(OnInputEndEdit);
        AddImage(Row("Underline", root, 900, 36, 38), GoldColor);

        var hint = CreateRect("Hint", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(480, 40), new Vector2(720, 95));
        AddText(hint, 22, MutedColor, TextAnchor.MiddleLeft).text = "Press [Enter]";
    }

    // A box centred horizontally, between yMin and yMax from the bottom of the screen.
    static RectTransform Row(string name, Transform parent, float width, float yMin, float yMax)
        => CreateRect(name, parent, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-width / 2, yMin), new Vector2(width / 2, yMax));

    static RectTransform Stretch(string name, Transform parent)
        => CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

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

    static void AddImage(RectTransform rect, Color color)
    {
        rect.gameObject.AddComponent<Image>().color = color;
    }

    Text AddText(RectTransform rect, int size, Color color, TextAnchor alignment, FontStyle style = FontStyle.Normal)
    {
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        // Player and model text is shown as-is: a stray "<b>" in a reply must not change the formatting.
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        // A soft shadow keeps text readable on the busy wood texture.
        var shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(2, -2);
        return text;
    }
}
