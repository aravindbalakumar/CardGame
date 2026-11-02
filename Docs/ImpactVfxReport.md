# Larger Voss impact VFX

The existing Wrag impact is now **1.35 scale instead of 0.55**, approximately **2.45 times larger in each dimension**.

## Change

- Modified `Assets/Scenes/GameView.unity`, on the existing `[UI Toolkit] Crimson Doom Game UI` object's `GameViewUIToolkit.impactVfxScale` field. The scene diff against the start of this task contains exactly this one property change.
- Reused `Assets/Eric VFX Studio/Free Game VFX/Prefab/FX_Purple_Hit_02.prefab`.
- No runtime scripts, UXML, USS, Animator controllers, shared prefabs, materials, particle settings, or character objects were modified for this task.

## Inspection and scaling

Inspected the prefab's complete transform hierarchy and all nine particle systems: root, Pos, hit, flare, Fragments, Ray, sheet, glow (1), and flare (1). The particles use local simulation. Existing child scales (including Fragments/Ray at approximately 1.2 and sheet at 1.3), Start Size, Shape, lifetime, speed and duration settings are retained. Full inspection: [prefab-audit.json](ImpactVerification/prefab-audit.json).

The existing spawner applies the serialized multiplier to the spawned root and sets every child system to Hierarchy scaling. Increasing this multiplier uniformly enlarges the child transforms, particles, and emission shapes without separately multiplying Start Size or distorting the authored proportions. The shared prefab remains byte-for-byte identical (SHA-256 `62fabd532fd0cabf505802508be0fb006af68b403dc07b09c148817ecc188fb3`).

The existing hit position calculation, prefab orientation, sorting, playback and cleanup remain intact. Both samples appeared at world position `(3.75320, 0.04320, -0.2)`, on Wrag's torso. The effect is spawned directly there; there is no travelling effect or projectile.

## Unity Play Mode verification

Tested `GameView` in Unity 6000.6.7f1 through the existing card-button and enemy-target click handlers, comparing scales 0.55 and 1.35. The final comparison passed **33 checks**, with zero failures: [results.json](ImpactVerification/results.json).

- Five-card initialization, card selection and enemy targeting succeeded.
- Impact remained synchronized with HolySpell animation frame 20.
- All nine systems retained uniform scaling, authored orientation, duration, lifetime, Start Size and Shape settings.
- Wrag death followed impact after the existing two-second game-time delay, within one rendered frame.
- Both sequences completed, cleaned up the effect, released the selection/input lock, and retained five cards, the turn index and character transforms.
- Before/after captures use matching random seeds and particle age (0.12 seconds); both emitted 25 particles. Simulation was temporarily held for screenshots and resumed to complete the sequence. Temporary test clock and Scene View settings were restored.
- Visually checked the Game View and clean renders from the Scene View camera. The larger burst stays centered on Wrag, retains its proportions, and leaves the cards and health UI readable. Conservative particle-renderer bounds extend beyond the viewport even at the original size; their `offscreen` flag in the raw results describes renderer bounds, not visible pixel clipping.

| View | Original 0.55 | Enlarged 1.35 |
| --- | --- | --- |
| Game View | [Before](ImpactVerification/before-game.png) | [After](ImpactVerification/after-game.png) |
| Scene View camera | [Before](ImpactVerification/before-scene.png) | [After](ImpactVerification/after-scene.png) |

No new runtime errors or exceptions were captured during the final comparison. The existing Visual Scripting warning about 134 skipped cached node options recurred; this change does not modify that package or its node cache. See [console-summary.json](ImpactVerification/console-summary.json).

The attack, death, counterattack/AI, damage and turn-flow implementations are unchanged. Counterattack behavior was not independently re-tested as part of this visual scale adjustment.

Temporary Editor test code was removed from `Assets` after testing; its non-compiling reference copy is retained as [CrimsonImpactVerification.cs.txt](ImpactVerification/CrimsonImpactVerification.cs.txt).

Final cleanup compilation completed successfully with zero compile errors: [cleanup-compilation.json](ImpactVerification/cleanup-compilation.json). Unity was left out of Play Mode with the scene saved, and the temporary Play/Stop/Save allowlist entries were removed.
