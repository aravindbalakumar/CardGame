# Crimson Doom parallel UI Toolkit view

Open `Assets/Scenes/GameView.unity` and enter Play Mode. The new root is
`[UI Toolkit] Crimson Doom Game UI`. Disable that object to reveal the original
Canvas. The original Canvas, its components, references, active flags, assets,
and serialized hierarchy are preserved.

`GameView.uxml` and `GameView.uss` define the screen. `Components/HandCard.uxml`
is the reusable card view. `GameViewUIToolkit` adapts existing game objects.
`GameViewPanelSettings.asset` uses a 1672 × 941 reference resolution, screen-size
scaling with a 0.5 width/height match, and sorting order 100 above the Canvas.
`RuntimeTheme.tss` imports Unity's runtime theme.

The layout uses flex regions, a horizontally scrolling hand, and geometry-based
compact, narrow, and portrait classes. Absolute placement is limited to layered
art, card contents, targeting hit areas, and the options overlay. Existing sprite
subassets are referenced directly; no original textures, sprite slicing, or font
imports were changed. Battlefield art reuses the hero frames and the existing
36-frame dark rider/warg sheet. The energy ornament is USS.

## Current scene and bindings

The gameplay `Player`, `Goblin`, and `Goblin_spearman` objects were already
inactive. This remains unchanged. Consequently, the default screen explicitly
shows **DESIGN PREVIEW** with the reference hand, health, currency, energy, and
intent text. Preview cards can be inspected; they do not perform game actions.
End Turn and unavailable navigation actions are disabled.

When the existing player initializes, the view reads `currentHand`, subscribes
to `OnPlayerUIUpdate`, shows actual card data/costs/effects, and tracks the
original card selection. Empty live hands stay empty. Repeated card assets map
to separate existing card instances. Health and block are event-driven. A 100 ms
schedule detects list/selection changes and advances art; it rebuilds the hand
only when its contents or existing card hierarchy change.

Card selection calls the existing `Card.OnCardSelected`; targeting invokes the
existing `InputHandler.OnRaycastHit` route. End Turn calls the existing
`Player.OnTurnEnd`, which currently processes status durations, and clears
selection. The game has no broader turn manager or energy-spending system.
`SetTurnAvailable(bool)` is an optional hook for a future turn controller.
`RefreshHand()` provides an immediate refresh after external list mutations.
The opponent panel is bound to the first configured opponent; the reference
art remains the scene's rider/warg presentation.

The Toolkit actor images independently follow their own GameObject's Transform:
`Voss_Idle` controls the player and `Wrag_Idle` controls the enemy. Position,
scale (including mirroring), and Z rotation update even when the GameObjects
are inactive or the animation option is off. Their targeting areas follow the
images. The fixed `Battlefield World Bounds` rectangle maps world coordinates
onto the background's centered cover layout; neither actor is a reference for
the other. Matching the sprites' world-space foot heights aligns their ground.

The existing game has no currency, energy pool, discard pile, journal, map
navigation, or intent API. Reference-only values do not become new game state;
unavailable live values display a dash and preview intent is hidden in live mode.
The gear opens UI display options, and the book/hand buttons focus the hand.

Only two compatibility additions were made outside this folder:

- `GameHandler.SelectedCard`: read-only access to the existing selection.
- `InputHandler.IsPointerInputBlocked`: an optional callback that prevents world
  raycasts while the full-screen Toolkit view owns input. Disabling the view
  unsubscribes the callback and restores the original input path.

The adapter deliberately delegates to existing Canvas `Card` instances. During
the later Canvas removal, migrate that selection API and card creation first;
removing those instances now would remove the existing gameplay selection path.

## Verification

Verified in Unity 6000.6.4f1, including Play Mode and composited Game View captures:

- 1920 × 1080, 1280 × 1024, 2560 × 1080, and 900 × 1200 layouts.
- Six-card live hand, distinct repeated-card selection, self-targeted Heal
  (16 → 26 HP; label `26/30`), selection clearing, empty live hand, and turn gating.
- Options opening/closing, Toolkit disable/re-enable, Canvas fallback, and input
  gate release/re-registration.
- Compilation, UXML/USS import, and referenced assets.
- Scene serialization comparison against the scene at the start of this task.

No new UI compilation, parsing, or runtime errors remain. Existing DOTween
PluginImporter version warnings and a Visual Scripting package exception
(`84 node options failed to load`) were observed separately and are not hidden
or changed by this implementation.

Screenshots and the preservation report are under `Documentation/CrimsonDoomUI`.
The explicit editor menu `Crimson Doom > UI Toolkit > Install in GameView` can
install the view in a copy of GameView that does not already contain its root.
It does not run automatically or touch Canvas objects.
