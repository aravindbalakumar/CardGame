# Crimson Doom direct-impact battle sequence

Implemented and verified on 2026-11-02 in the existing GameView scene, using Unity 6000.6.7f1. Source files were edited in the workspace and opened in VS Code.

## Result

Selecting any of the six existing bottom UI Toolkit cards selects its original Canvas-backed card and waits for a Wrag click. Clicking Wrag starts the existing HolySpell animation on Voss. At attack frame 20, one impact effect appears directly on Wrag's current body position. The existing Wrag death animation starts two gameplay seconds later. Input and selection are restored on completion or interruption.

There is no projectile, launcher, travelling effect, or movement of an effect between the characters. No character GameObjects, Animator controllers, clips, VFX prefabs, or card assets were created or duplicated.

## Assets and hierarchy inspected

- The current scene has `Voss_Player` and its existing `Assets/Animation/Voss.controller`. Its `HolySpellAnim` state already references `Assets/Animation/HolySpellAnim.anim`: 36 frames at 12 fps, three seconds long. Frame 20 is the bright attack flash at approximately 1.667 seconds.
- A separate `HolySpell_Voss` GameObject is absent from the current hierarchy. It exists in an older recovery snapshot, but restoring/duplicating a character was unnecessary. The spell reference points at the existing `Voss_Player` SpriteRenderer, and its Animator switches from idle to the existing spell state. Actor and collider components stay active.
- The opponent is the existing `Idle_Warg` object. The existing death object is spelled `Death_Warg` in this scene; its controller plays `DeathWragAnim`, a three-second, nonlooping sprite animation. Both retain their authored transforms and assets.
- Inspected the existing Eric VFX hit prefabs and their particle systems. Selected **`Assets/Eric VFX Studio/Free Game VFX/Prefab/FX_Purple_Hit_02.prefab`**, an authored URP magic-hit burst with nine particle systems. It is instantiated once at Wrag's hit point with scale 0.55; authored materials and colors are retained. Render sorting puts it ahead of the character sprite. Its root remains stationary at the hit position.

## Modified files

| File | Change |
| --- | --- |
| `Assets/UI/CrimsonDoom/Scripts/GameViewCombatSequence.cs` | Replaced the old projectile path and one-second delay with a direct impact and fixed two-second deadline. Uses the existing Voss Animator state and Wrag death pose. Adds cancellation, visibility restoration, and timing/position readback. Removes casting/projectile/burst configuration that is no longer used. |
| `Assets/UI/CrimsonDoom/Scripts/GameViewUIToolkit.cs` | Connects existing card/enemy callbacks to the sequence. Any selected card can use the Wrag confirmation button; its effect still applies to a target allowed by the existing card data. Locks action buttons during playback and restores the original legacy Canvas and GraphicRaycaster when Toolkit closes. |
| `Assets/Scripts/GameHandler.cs` | Adds an optional presentation hook and an in-progress lock around the existing card event. `ProcessCardAction` remains the effect owner and is called exactly once at impact. Its damage/status calculations and turn methods are unchanged. |
| `Assets/Scripts/Game/Card.cs` | Rejects extra legacy card clicks during the active sequence before changing selection visuals. |
| `Assets/Scripts/Game/ActorAndPlayers/BaseClasses/Player2D.cs` | Guards selection during playback and routes the existing raycast callback through the same card-event dispatcher. Null selection remains available for cleanup. |
| `Assets/Scenes/GameView.unity` | Enables the existing UI Toolkit object, UIDocument, and view; assigns Voss's spell reference and the selected impact prefab; corrects `legacyCardParent` from an empty `Content` object to the actual `Canvas/Bottom` card container. Existing character objects/controllers are reused. |

No UXML/USS layout changes were made. During Play Mode the old Canvas's rendering and GraphicRaycaster are temporarily disabled while Toolkit owns the view, keeping all six original card objects active for their callbacks and data. Disabling Toolkit restores those original components. This avoids rendering both existing UI layers simultaneously.

## Gameplay behavior preserved

- Enemy-target cards apply their existing effects to Wrag at impact. Self-target cards still apply their existing effects to the player, using the Wrag click as confirmation of the requested visual sequence. No card targeting masks or data assets were rewritten.
- The existing hand contents, effect calculations, and turn index are preserved. The presentation does not add card consumption, forced turn advancement, or forced health changes.
- Every requested attack plays the death animation. A Wrag that survives the card returns to its original idle visibility afterward. If the actual card effect defeats Wrag, the final authored death pose persists and further attacks on that defeated target are blocked. The visual sequence does not silently kill a surviving opponent.
- Missing required animation/VFX references reject the sequence with a configuration warning before applying a card. UI disable, lost/disabled animation components, invalidated references, and interrupted selection cancel safely and release the input lock. Spawned impact particles are cleaned up.
- Timing uses the same scaled gameplay clock as the animations and particles, so pausing freezes the sequence. Death is scheduled at impact time + 2.0 seconds and executes on the corresponding player frame. A one-nanosecond numeric tolerance prevents binary rounding from adding an extra whole frame; playback remains limited to Unity's frame resolution.

## Play Mode verification

**99 assertions passed, zero failures**, using the real UI Toolkit clickable callbacks and runtime coroutines. Test results are in `BattleVerification/results.json`.

| Coverage | Result |
| --- | --- |
| All six existing cards | Each selects its original card, enables Wrag confirmation, waits without starting an attack, and retains toggle deselection. |
| Complete damage-card sequence | BloodTithe: select → click Wrag → HolySpell → direct impact → death animation → cleanup. Existing damage applies exactly once. |
| Complete self-card sequence | BlessedWard follows the same visual sequence while preserving its self-armor effect and Wrag's health. |
| Attack frame | Impact begins at HolySpell frame 20, within one animation update. |
| Impact placement | One particle prefab instance appears at the computed current Wrag body position, emits visible particles, and stays at that position. |
| Impact-to-death delay | BloodTithe: **1.9999999999999964 seconds**; BlessedWard: **2.0000000000000000 seconds**. Both are **2.000 seconds** at the recorded precision. Tests used 60 fps capture timing and checked that death did not occur early. |
| Death animation | Existing `Death_Warg` Animator enters `DeathWragAnim` and advances through its middle frames. The bot's GameObject stays active. |
| Duplicate input | Repeated enemy clicks and legacy card callbacks cannot start another sequence or replace its card. |
| UI interruption | Disabling the view cancels playback, clears selection, restores legacy Canvas visibility, and unlocks actions. Re-enabling installs one presentation handler. |
| Missing impact reference | Intentional null-reference fixture rejects the attack safely with one expected configuration warning. The correct reference was restored. |
| Animator interruption after impact | Cancels pending death, removes the effect, restores visibility, and releases input. |
| Actual defeat | A lethal damage fixture completes the sequence and holds the existing final death pose; a defeated Wrag cannot restart the sequence. |
| Preservation | Existing character transforms, scene root count, six-card hand, and turn index remain unchanged. |
| Compilation and final startup/teardown | Passed after removal of temporary test code. The configured scene is saved and normal startup does not start a sequence on its own. |
| Console | No new runtime Error/Exception/Assert groups during final verification. One expected configuration warning was generated by the deliberate missing-VFX test. Pre-existing Visual Scripting/DOTween warnings remain separate from this change. |

The temporary verification script and its metadata were removed from Assets. Its reviewable source is retained as `BattleVerification/CrimsonBattleVerification.cs.txt`; it is not compiled into the project. Timing results, final compilation/Console evidence, and screenshots are retained alongside it:

- `BattleVerification/results.json`
- `BattleVerification/compilation.json`
- `BattleVerification/final-console.json`
- `BattleVerification/impact.png`
- `BattleVerification/death.png`

Previously documented missing legacy/vendor assets and editor-cache warnings were not replaced or suppressed. See `RuntimeStabilityReport.md` for those independent findings.
