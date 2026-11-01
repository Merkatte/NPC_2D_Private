# Terrain Tile Contract

Resolve these eight dimensions before producing a functional terrain set. Reuse approved values from the assignment, task record and inspected same-purpose assets. Ask only about unresolved choices that affect the outcome; do not ask the user to fill a new form every time.

| Dimension | Resolve |
|---|---|
| Reference and format | Approved style/reference roles; logical cell size, view, intended scale, palette/shading treatment, transparency and requested formats |
| Required inventory | Material fills, N/E/S/W borders, convex/concave corners, transition families and exact requested decorative variants/counts |
| Direction semantics | Coordinate convention and material occupancy; “NW corner” alone is insufficient |
| Connection contract | Each edge's material profile, crossing positions, width/color/shadow treatment and valid neighbor relationships |
| Variation permissions | Pixels/features that may change, connection structure that must remain stable, and any explicitly requested structural alternatives |
| Decoration delivery | Decorations baked into terrain, optional editable layers or separate overlays as actually requested |
| Atlas/delivery layout | Stable IDs, row/column order, dimensions, padding/gutters, preserved existing cells and required individual/sheet/editable outputs |
| Completion evidence | Inventory coverage, requested join examples/repetition, style comparison, actual preservation and disclosed remaining gaps |

## Inventory Without Assumed Full Coverage

List only requested or necessary components; do not turn an interior-variant task into a full autotile family. Conversely, do not omit requested center tiles because corners are more visually prominent.

A compact row can be:
`tile ID | materials/function | direction/occupancy | compatible edge profiles | decoration variant | output cell`.

Keep the agreed number of plain/decorated interior variants explicit. For example, “plain grass, flower grass, grass clumps, pebble grass” are four content requirements if requested; four duplicates of plain grass do not satisfy them. Separate layers can support editing but do not replace composed requested tiles.

For existing compatible families, reuse their actual connectors and IDs. If approved input tiles are already incompatible, surface that conflict before changing their geometry or claiming a pack-only task fixed them.

## Unambiguous Directions and Joins

For a conventional image grid, record x increasing right and y increasing down; N/E/S/W refer to top/right/bottom/left. If the existing project uses another convention, name it and map to sheet coordinates.

Define material A/B occupancy in each corner. Example only: NW convex land means land occupies the NW corner with water elsewhere; NW concave land means water cuts into the NW corner with land elsewhere. Both involve N/W boundary crossings, but are different tiles. Do not assume this example replaces the chosen family definition.

Record actual connector positions/profile from the approved straight edges. Midpoints are optional design choices, not a global law. A corner must match its specific neighbors in material, profile, lip/bank/shadow and edge color; position alone is insufficient. If varying shape near the edge would break these properties, keep that region stable.

Distinguish a visual diagonal segment within a corner from diagonal-only cell connectivity or a new saddle/crossing case. Add a new connectivity class only when requested. Do not rotate the whole artwork blindly to produce missing orientations when lighting/detail orientation would change.

## Preserve the Requested Product

- “More flower/stone/grass variety” preserves tile topology and approved style unless the user asks to change them.
- “Flowers inside the grass tile” delivers the composed grass tile, not just transparent flowers for the user to place.
- “Pack these tiles into one sheet” preserves selected tile pixels and places them in the agreed cells; no creative redraw, automatic trim/rotation or hidden resampling.
- “Fix this corner” preserves passing neighbors and style; if the neighbors must change, report the affected scope to root.
- A connector-preserving edit is not permission to replace rich approved art with flat procedural bands.
- A concept can explore appearance; unresolved geometry must remain labeled as concept work, never silently promoted to a complete tile family.

## Reuse Historical Evidence Carefully

The DefenseConcept20261028 records are failure/technique examples, not default approved art or universal dimensions:
- unified-terrain-v1 requested explicit corner directions and midpoint joins but retained paired arches and repeated orientations;
- WaterV2CornersV4 retained visually rich art but reported approximate connectors;
- SimpleShoreV5/PlantedShoreV6 demonstrated exact tile-copy mosaics with deliberately simplified art under that task's direction.

Do not inherit their 64px cells, palettes, simplified appearance or claims of approval into a new task. Use the current user's actual reference choices. Do not read the entire history for routine work; the present contract and actual assets govern.
