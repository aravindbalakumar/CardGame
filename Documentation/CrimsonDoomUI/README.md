# Crimson Doom UI reference implementation

Open `Assets/Scenes/GameView.unity`. The existing `[UI Toolkit] Crimson Doom Game UI` object owns the UIDocument and controller. The original hierarchy and gameplay references are retained; legacy Canvas components and their raycasters are disabled. `Wrag_Attack` is inactive so it does not overlap `Wrag_Idle`.

## Editing

- Layout and HUD: `Assets/UI/CrimsonDoom/GameView.uxml`.
- Styles, sprite bindings and responsive rules: `Assets/UI/CrimsonDoom/GameView.uss`.
- Hand template: `Assets/UI/CrimsonDoom/Components/HandCard.uxml`.
- Runtime bindings and animation preview: `Assets/UI/CrimsonDoom/Scripts/GameViewUIToolkit.cs`.
- Scaling: the existing PanelSettings asset uses Scale With Screen Size, a 1672 × 941 reference resolution, and a width/height match of 0.5.
- Body typography: Crimson Text Regular, supplied with its OFL license in `Assets/Font/CrimsonText`.
- Characters and background: edit the existing scene Transforms, SpriteRenderers and Animators. Their placement accounts for transparent margins in the source sprites. The existing custom Inspector also supports animation Transform editing.

The settings button exposes Voss Idle, Attack and Hurt previews. Attack and Hurt show one hero pose at a time, run the existing Animator, and restore original object visibility after the clip. Restart Idle or disabling the controller also restores visibility. The public `PlayHeroAttack` and `PlayHeroHurt` methods use the same path.

## Verification

The scene was run in the Unity Editor in Play Mode at 1672 × 941, 1280 × 720, and 1600 × 1200. The adjacent reports record one UIDocument, zero enabled Canvases, five hand cards, and viewport checks for the main controls. `PlayMode-1672x941.png` is the final Game view capture. Both Voss Attack and Hurt were invoked and checked for correct pose visibility and automatic return to Idle.

The scene's gameplay Player objects are inactive in the supplied scene. Consequently the existing design-preview path supplies the reference hand and HUD values. End Turn remains disabled in that state. When a live Player is initialized, the existing hand, health, armor, selection and targeting bindings take over. The preview draw/discard values are 12/0; live mode continues to show the real hand count and an unavailable discard count because the gameplay API exposes no discard pile. No energy, currency, enemy-intent or turn system was invented.

The supplied animated warg and UI sprite sheets differ from the artwork in the reference. Their original assets and animations are reused; the layout, sizes, typography, colors and spacing approximate the reference with those editable assets. Existing Visual Scripting serialization warnings were present before this work.
