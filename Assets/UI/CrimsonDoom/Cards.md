# Crimson Doom cards

The existing `GameViewUIToolkit` controller and `HandCard.uxml` render both the hand and the collection. The book button toggles the collection; the draw-pile button returns to the hand. Collection selection inspects artwork. Hand selection uses the original `Card.OnCardSelected` and `GameHandler` targeting/effect path.

## Adding cards

1. Place a card-face image in `Assets/Sprites/Cards/` (subfolders are supported). The established face dimensions are 416 × 716. The entire texture is rendered without stretching or relying on sprite slice names.
2. Unity automatically updates the generated `Assets/UI/CrimsonDoom/Resources/CrimsonDoomCardCatalog.asset`. New images appear in the collection, including while Play Mode is running once Unity imports them. Imports, renames, moves and deletions reconcile the catalog. The same index is regenerated before builds and loaded through Resources in players; runtime code never uses AssetDatabase.
3. For playable behavior, create a **Card** ScriptableObject in `Assets/ScriptableObjects/Cards/` and set its existing actions, target group and energy usage. Set `fullCardTexture`, or give its metadata name the same name as the image (ignoring spaces, punctuation and the `Crimson_Doom_` filename prefix). Explicit texture bindings take priority. Existing `metaData.cardTexture` remains available for legacy rendering and unmatched-face fallback.
4. Add the definition to the player's existing loadout through the usual gameplay setup. Artwork discovery deliberately does not change a player's deck or invent card effects. New definitions in the live hand render automatically.

The catalog uses asset GUIDs for identity and stable path ordering, so repeated import callbacks do not append duplicate entries. Distinct assets with the same title remain distinct; use explicit texture references if names are ambiguous. Multiple copies of the same definition in a gameplay hand remain separate selectable cards.

## Presentation and maintenance

Full artwork contains baked text. In the playable hand, opaque title, cost, action and description labels display the actual ScriptableObject values over those areas. This keeps the original five cards' behavior intact even when the illustration's printed values differ. The collection displays unmodified artwork; tooltips include linked gameplay effects when available.

Card widths never shrink as the list grows. Horizontal scrolling, keyboard focus scrolling, clipped viewport edges, gold selection tint and consistent margins keep large hands usable. Empty hands display an empty state. Cards with no matching complete face show their original art and gameplay labels.

The generated catalog can also be rebuilt through **Crimson Doom → UI Toolkit → Rebuild Card Catalog**. Keep the generated asset and its `.meta` in source control. Use normal Unity imports before building; loose files copied beside an already-built player are not a content delivery system.
