# Art prompts (for the M4 video)

Time box: one session (~2 h). If it runs over, keep the plain background and record the video anyway.

## Style block (paste at the start of every prompt)

```
2D anime fantasy illustration, cel shading, clean lineart, warm candlelight, painterly background
```

## Assets

| File | Size | Prompt |
| --- | --- | --- |
| `Assets/Art/Background/shop_bg.png` | 1920×1080 | `{style}, interior of a small fantasy village item shop, brick wall, wooden shelves with potions, daggers and torches, wooden counter across the lower third, empty, no people, front view, 16:9` |
| `Assets/Art/Character/mira.png` | ~1000 px tall, transparent | `{style}, character design, young female shopkeeper, warm but slightly greedy smile, {outfit: my choice}, half body, arms resting on a counter, plain white background, front view` |
| `Assets/Art/Background/counter.png` (optional) | 1920 wide, transparent above the counter | Crop the counter out of `shop_bg.png` |
| `Assets/Art/Items/<item id>.png` (optional) | 256×256 | `{style}, game item icon, {description from inventory.json}, centered, plain background, no text` |

Item icons are named after the `id` in `inventory.json` (for example `potion_minor_health.png`), so code can later find an icon from `mentioned_items`.

## Rules
- Generate Mira first, then use the chosen image as the reference for every later generation.
- Same model, same aspect ratio, same seed when the tool allows it.
- The reference image is for layout and mood only. Mira's hair, outfit and colours are my own design (the repo is public).
- Remove the background from `mira.png` before importing.
