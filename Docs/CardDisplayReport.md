# Five-card sprite display and hover outline

Implemented and verified on 2026-11-02 in the existing GameView scene, using Unity 6000.6.7f1.

## Behavior

- The existing bottom UI shows exactly **five** card elements in live gameplay and preview mode, including during battle playback. The original gameplay hand also initializes five cards.
- Card artwork is randomly selected from the imported sprites in `Assets/Sprites/Cards/`. The folder currently contains 21 correctly imported card sprites. No importer changes were necessary.
- Draws sample without replacement, after excluding null and duplicate sprite references. Five unique sprites are shown whenever at least five unique sprites exist. With fewer sprites, the available unique sprites are exhausted before reuse. With no available sprites, five labelled placeholders remain and no old artwork is dereferenced as a fallback.
- A normal UI refresh or selection change keeps the current artwork stable. Game initialization or a changed sprite catalog generates a new draw.
- The existing Editor discovery and project-change subscription automatically include newly imported sprites. The existing `CardSpriteBuildProcessor` includes the current folder catalog in built scene copies, so builds do not rely on runtime AssetDatabase calls or a manually maintained card list. That build processor did not need modification.
- Pointer enter reveals a gold outline; pointer leave removes it. The outline ignores picking, uses absolute positioning, and does not alter card dimensions, spacing, rotation, or click handling. Existing hover transforms and selected-card styling are preserved.
- Artwork is randomized independently of the existing gameplay definitions. Existing card data, effects, targeting, selection callbacks, and the direct-impact battle sequence remain in use; new gameplay rules are not inferred from the baked text in the artwork.

## Files modified

| File | Change |
| --- | --- |
| `Assets/Scripts/Game/GameValues.cs` | Changes the existing shared hand limit from six to five. |
| `Assets/Scripts/Game/Card.cs` | Safely handles a missing legacy card texture during initialization. This matters because the existing gameplay definitions also reference textures in the card folder. |
| `Assets/UI/CrimsonDoom/Scripts/GameViewUIToolkit.cs` | Reuses the folder catalog for both live and preview artwork; draws five cached sprites without duplicates; caps rendering at five even if an external hand is oversized; handles empty/sparse catalogs; retains the existing selection callbacks; registers independent pointer enter/leave outline classes; guards discovery when the folder is absent. |
| `Assets/UI/CrimsonDoom/Components/HandCard.uxml` | Adds one pointer-ignoring outline element inside the existing button. |
| `Assets/UI/CrimsonDoom/GameView.uss` | Adds outline visibility rules and full-card-sprite/placeholder rendering rules. Existing card dimensions, row layout, spacing, positioning, and selection rules are unchanged. |

No scene, character, Animator, VFX, ScriptableObject card definition, or original sprite importer was intentionally modified for this update. The original UI template and styling remain in place; full-face sprites replace the old layered card images/text.

## Play Mode results

**53 assertions passed with zero failures.** See `CardVerification/results.json`.

- Exactly five live UI buttons and five original Canvas-backed card objects initialize.
- Every displayed image is a Sprite asset from the requested folder; the initial draw contains five unique sprites.
- Thirty additional draws each contain exactly five unique sprites and produce different selections.
- Ordinary refreshes preserve the current random artwork.
- An externally oversized hand still renders only five cards.
- Synthetic UI Toolkit pointer enter/leave events show and clear the resolved outline opacity. Its picking mode remains Ignore and the button layout dimensions remain unchanged.
- Clicking while hovered selects normally. Leaving removes the hover outline without clearing selection; clicking again deselects normally.
- Empty, null, and sparse catalogs remain safe and render five elements. A null legacy texture also initializes without an exception.
- A temporary newly imported Sprite is discovered automatically and appears in the live catalog through the existing project-change event. Deleting it refreshes the catalog again while retaining five displayed cards. The temporary asset and metadata were removed.
- A card with the new artwork still starts and completes the existing direct-impact battle. Five cards remain visible throughout, selection clears normally, and the two-second death timing is retained.
- A second actual Play Mode startup passes its four startup checks and produces a different five-sprite selection. See `CardVerification/second-startup.json`.
- The Console inspection found no new runtime Error/Exception/Assert entries during the final full verification.
- Compilation feedback after removal of the temporary verification helper reports zero C# errors. GameView finishes in Edit Mode, saved and clean, with its existing 16 scene roots.

The temporary helper was removed from Assets. Reviewable test source, results, Console/compilation evidence, and screenshots are retained outside compiled game assets:

- `CardVerification/CrimsonCardVerification.cs.txt`
- `CardVerification/results.json`
- `CardVerification/second-startup.json`
- `CardVerification/console.json`
- `CardVerification/compilation.json`
- `CardVerification/hover.png`
- `CardVerification/five-cards.png`

Existing unrelated editor-cache/importer warnings documented in `RuntimeStabilityReport.md` were not suppressed or changed by this update.
