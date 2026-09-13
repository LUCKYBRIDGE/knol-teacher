# Prehistory Race Environment Art Spec

This document defines the large transparent environment illustrations for the vertical prehistoric picker race. These assets are scenery, not gameplay colliders.

## Shared production contract

- Format: transparent RGBA PNG.
- Master canvas: 1536×1024 px.
- Keep roughly 6–12% transparent breathing room around the visible silhouette; target 8%.
- Runtime display is intentionally much smaller (roughly 160–260 WPF px). Detail must survive downscaling.
- Match the existing race art family: readable 2D/2.5D classroom illustration, slight three-quarter/top-down view, medium silhouette clarity, muted earth colors, restrained cel shading.
- Do not bake a rectangular background, card, label, caption, frame, or opaque sky panel into the PNG.
- Avoid photorealism, fantasy props, chibi styling, glossy mobile-game rendering, or highly saturated toy-like colors.
- Shadows, if used, must follow the object silhouette with alpha transparency. Never add a rectangular drop-shadow plate.
- Keep large dark masses away from the road-facing edge so racers remain readable when the prop approaches the course boundary.
- Environment PNG bounds are never collision bounds. `Image.IsHitTestVisible` remains false and gameplay physics stays authoritative.
- Missing files must remain missing on screen. Do not replace them with C# `Path`, SVG, emoji, text cards, or procedural programmer art.

## Reserved assets and map placement

| Period | File | Reserved prop | Approx. map position | Purpose |
| --- | --- | --- | --- | --- |
| Paleolithic | `prehistoric_paleo_cave_entrance.png` | `paleo-cave-entrance-slot` | left edge, Y 120 | Natural cave entrance framing the opening section |
| Paleolithic | `prehistoric_paleo_rock_cliff.png` | `paleo-rock-cliff-slot` | right edge, Y 620 | Rocky hunting-landscape cliff / boulder cluster |
| Neolithic | `prehistoric_neolithic_hut.png` | `neo-hut-slot` | left edge, Y 1240 | Settled-life pit house / hut |
| Neolithic | `prehistoric_neolithic_reeds.png` | `neo-reeds-slot` | right edge, Y 2010 | Riverbank reeds and waterside vegetation |
| Bronze Age | `prehistoric_bronze_field.png` | `bronze-field-slot` | left edge, Y 2430 | Grain field and field ridges representing expanded agriculture |
| Bronze Age | `prehistoric_bronze_settlement.png` | `bronze-settlement-slot` | right edge, Y 2860 | Settlement-edge scenery suggesting larger village scale |

## Per-asset visual guidance

### Paleolithic cave entrance

- Natural irregular stone opening, not a fantasy dungeon gate.
- The interior can be dark, but the outer rock silhouette must remain readable against the map ground.
- Optional sparse moss/grass is acceptable; avoid torches, signage, carved symbols, or modern-looking masonry.
- The road-facing right edge should taper rather than form a large rectangular wall.

### Paleolithic rock cliff

- A compact layered rock/cliff cluster that reads from a small projector-scale rendering.
- Use broad rock planes rather than dozens of tiny pebbles.
- Keep the left road-facing edge visually open; it should frame the course, not look like an invisible physical wall.

### Neolithic hut

- A simple prehistoric pit-house / hut reconstruction suitable for Korean elementary social-studies context.
- Use natural wood, thatch, earth and rope materials.
- Avoid a modern cabin, tiled roof, metal fittings, windows with glass, or elaborate fantasy village decoration.
- Keep the silhouette simple enough to read at roughly 225×190 WPF px.

### Neolithic reeds

- Group several large reed/grass silhouettes rather than creating a dense photographic marsh texture.
- Small hints of water-edge stones or low vegetation are acceptable.
- Do not create an opaque water rectangle; all negative space remains alpha-transparent.

### Bronze Age field

- Show cultivated grain rows / dry-field ridges rather than a modern mechanized farm.
- No tractor, fencing wire, plastic, irrigation hardware, road signs, or modern crop geometry.
- The field should read as a low horizontal edge element and must not visually cover the racing road.

### Bronze Age settlement

- Suggest a larger settlement using two or three simplified prehistoric structures and low wooden/earth context.
- Keep architecture generic and historically cautious; it is environmental context, not a precise archaeological reconstruction claim.
- Avoid castle walls, tiled roofs, banners, metal gates, or fantasy fortification styling.

## Gameplay separation

All six reserved environment props are:

- `RaceMapVisualRole.Decoration`
- `RaceMapInteractionRole.None`
- without `GameplayColliderKey`
- placed so their visual center is outside the drivable road at their vertical location
- rendered behind racers and UI with negative Z-index

If an environment feature later needs gameplay collision, add a separate explicit simulation binding. Do not infer physics from PNG dimensions or alpha pixels.
