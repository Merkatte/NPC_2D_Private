# Project Art Routing

Use this reference to select the nearest existing asset family. Always inspect the actual approved image and its `.meta`; the examples below document current patterns, not immutable global rules.

## Current Family References

| Asset family | Starting references | Current observable pattern |
|---|---|---|
| Character/NPC | `Assets/Art/Generated/merchant-npc-female.png`, approved player or worker sprites supplied by the assignment | Chibi proportions, readable dark outline, warm earthy palette, soft pixel-art rendering, transparent canvas. Current merchant reference is 1254×1254 with centered framing and PPU 650. |
| Front-facing buildings | `town-hall-front.png`, `inn-front.png`, `restaurant-front.png`, `warehouse-american-barn-front.png` | Front elevation, warm timber/stone materials, dark readable outline, dense but symmetric architectural detail, transparent exterior silhouette. Current town hall reference is 1536×1024 with PPU 150. |
| World props and crops | `farmer-hoe.png`, `guard-sword.png`, `worker-cargo-basket.png`, `crop-*-stage-*.png` | Warm outlined pixel-art treatment. Crop references use bottom-center pivot and commonly PPU 500; inspect the matching crop stage `.meta`. |
| Merchant UI | `Assets/Art/Generated/UI/` | Warm wood, paper, olive, green, and terracotta surfaces with beveled or outlined edges. Many panels are sliced sprites, but border, PPU, filter, and compression differ by element. Copy only from the closest same-purpose sibling. |
| Farm tiles | `Assets/Art/Generated/Tiles/` | 32-pixel tile cells, point filtering, PPU 32, multiple-sprite sheets and Tile assets. Preserve exact grid and adjacency requirements from the selected sibling sheet. |

## Selection Rules

1. Prefer references named by the assignment.
2. Otherwise select two or three references from the same asset family and intended on-screen scale.
3. Use the closest same-purpose sibling `.meta` for proposed import settings.
4. If candidates disagree on view, rendering density, or scale, report the conflict instead of averaging them.
5. Never use files under `Assets/Plugins` as project art references.

## ImportSpec Fields

Return the fields that the later Unity import step needs:

- `textureType` and `spriteMode`;
- `pixelsPerUnit`;
- `pivot`;
- `filterMode`;
- `compression`;
- `alphaIsTransparency`;
- `wrapMode`;
- mipmap enablement, sRGB handling, and sprite mesh type;
- slicing rectangles, borders, or tile cell size when applicable.

Do not claim these settings were applied unless Unity importer evidence proves it.
