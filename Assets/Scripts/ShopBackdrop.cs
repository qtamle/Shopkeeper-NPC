using UnityEngine;
using UnityEngine.UI;

// Draws the static shop scene behind the chat: background, Mira, and an optional counter in front of her.
// Pure visuals: no game logic. Any sprite left empty is skipped (the background falls back to a plain colour).
public class ShopBackdrop : MonoBehaviour
{
    // Same reference size as ChatUI, so both canvases scale the same way.
    static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

    [SerializeField] Sprite background;
    [SerializeField] Sprite character;
    [Tooltip("Optional. Drawn over the character so she looks like she stands behind the counter.")]
    [SerializeField] Sprite counter;

    [Tooltip("Character height in reference pixels (the screen is 1080 tall).")]
    [SerializeField] float characterHeight = 760f;
    [Tooltip("Gap between the bottom of the screen and the character's bottom edge. The counter hides her lower part.")]
    [SerializeField] float characterBottom = 150f;
    [Tooltip("Horizontal offset from the screen centre. Negative moves her left.")]
    [SerializeField] float characterX = 0f;

    RectTransform characterRect;
    RectTransform counterRect;

    void Awake()
    {
        var canvasGo = new GameObject("ShopCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // ChatUI's canvas uses the default order 0, so -1 keeps the whole scene behind the chat.
        canvas.sortingOrder = -1;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // Inside one canvas, children draw in order: background, then character, then counter on top.
        var bg = CreateImage("Background", root, background);
        Stretch(bg.rectTransform);
        if (background == null) bg.color = new Color(0.12f, 0.12f, 0.14f);

        if (character != null) characterRect = CreateImage("Character", root, character).rectTransform;
        if (counter != null) counterRect = CreateImage("Counter", root, counter).rectTransform;

        ApplyLayout();
    }

    // Lets you tune characterHeight / characterBottom in the Inspector during Play mode.
    // Play mode changes are lost when you stop, so copy the final numbers back afterwards.
    void OnValidate()
    {
        if (Application.isPlaying) ApplyLayout();
    }

    void ApplyLayout()
    {
        if (characterRect != null)
        {
            // Bottom-centre anchor; width follows the sprite's aspect ratio so she is never stretched.
            characterRect.anchorMin = characterRect.anchorMax = characterRect.pivot = new Vector2(0.5f, 0f);
            characterRect.sizeDelta = new Vector2(characterHeight * Aspect(character), characterHeight);
            characterRect.anchoredPosition = new Vector2(characterX, characterBottom);
        }

        if (counterRect != null)
        {
            // Full width along the bottom; height follows the sprite's aspect ratio.
            counterRect.anchorMin = new Vector2(0f, 0f);
            counterRect.anchorMax = new Vector2(1f, 0f);
            counterRect.pivot = new Vector2(0.5f, 0f);
            counterRect.sizeDelta = new Vector2(0f, ReferenceResolution.x / Aspect(counter));
            counterRect.anchoredPosition = Vector2.zero;
        }
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.sprite = sprite;
        // Decoration only: don't let it catch clicks meant for the chat.
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static float Aspect(Sprite sprite) => sprite.rect.width / sprite.rect.height;
}
