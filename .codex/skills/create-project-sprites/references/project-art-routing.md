# Project Art Routing

Use this reference to select the nearest existing asset family. Inspect the actual approved image and the same-purpose reference `.meta` used for import defaults. The examples below document patterns, not immutable global rules; explicit authorized assignment values take precedence.

## Current Family References

| Asset family | Starting references | Current observable pattern |
|---|---|---|
| Character/NPC | `Assets/Art/Generated/NPC/merchant-npc-female.png`, `Assets/Art/Generated/NPC/player-dotween-transparent.png`, approved player or worker sprites supplied by the assignment | Chibi proportions, readable dark outline, warm earthy palette, soft pixel-art rendering, transparent canvas. Current merchant reference is 1254×1254 with centered framing and PPU 650. NPCGirl uses PPU 500; keep each existing importer unchanged when organizing assets. |
| Front-facing buildings | `Assets/Art/Generated/Buildings/`: `TownHall/town-hall-front.png`, `Inn/inn-front.png`, `Restaurant/restaurant-front.png`, `Warehouse/warehouse-american-barn-front.png`; other folders: `GuardPost`, `MerchantCaravan`, `Farm` | Front elevation, warm timber/stone materials, dark readable outline, dense but symmetric architectural detail, transparent exterior silhouette. Current town hall reference is 1536×1024 with PPU 150. |
| World props and crops | Props remain in `Assets/Art/Generated/`: `farmer-hoe.png`, `guard-sword.png`, `worker-cargo-basket.png`. Crop stages and item icons live in `Assets/Art/Generated/Crops/Carrot/` and `Crops/Potato/`; rigged carrot stages live in `Crops/Carrot/Rigged/`. | Warm outlined pixel-art treatment. Crop references use bottom-center pivot and commonly PPU 500; inspect the matching crop stage `.meta`. Preserve existing importer and rig data when moving assets. |
| Merchant UI | `Assets/Art/Generated/UI/` | Warm wood, paper, olive, green, and terracotta surfaces with beveled or outlined edges. Many panels are sliced sprites, but border, PPU, filter, and compression differ by element. Copy only from the closest same-purpose sibling. |
| Farm tiles | `Assets/Art/Generated/Tiles/` | 32-pixel tile cells, point filtering, PPU 32, multiple-sprite sheets and Tile assets. Preserve exact grid and adjacency requirements from the selected sibling sheet. |

For terrain production, use [create-terrain-tiles](../../create-terrain-tiles/SKILL.md) for inventory, connection profiles, pixel-art quality and atlas preservation. The farm row is one existing family, not a universal 32px/PPU rule. Concept outputs such as DefenseConcept20261028 are not automatically approved production references; use the current assignment's selected assets and resolved values.

## Selection Rules

1. Prefer references named by the assignment.
2. Otherwise select two or three references from the same asset family and intended on-screen scale.
3. Use explicit authorized assignment values for specified output/import fields. Fill only unspecified fields from the closest same-purpose sibling `.meta`.
4. If candidate reference families disagree on view, rendering density or scale and the assignment does not resolve the choice, report the conflict instead of averaging them. A deliberate authorized override is not itself a conflict.
5. Never use files under `Assets/Plugins` as project art references.

## Target Settings and Comparison

For each applicable field, use its explicit authorized target when supplied, otherwise the selected reference default. Compare the proposed value against that target. Record explicit overrides and their source in the normal worker report, outside any serialized importSpec with a fixed schema.

For example, reference pivot (0.5, 0.5) plus an explicit bottom-center requirement yields target pivot (0.5, 0.0). Keep unspecified PPU/filter settings from the appropriate reference. This difference is intentional; do not change the reference's importer to make both match.

Report contradictory or unsupported requirements to root. A reference supplies defaults, not permission to override the user or rewrite existing importer/rig data.

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

These are proposed handoff values. Assembly applies them; do not claim they were applied without actual Unity importer evidence. Keep ordinary handoff concise. Only selected legacy v1 requires the exact existing serialized schema and its permitted fields; do not expand that schema to add provenance, slicing or extra report fields.
