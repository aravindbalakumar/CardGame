# Wrag counterattack and one-second death delay

Implemented in the existing `GameView` scene and existing turn manager. No characters, animation assets, projectiles, UI layouts, or shared VFX assets were added or replaced.

## Behavior

1. Select a card and click Wrag. The existing HolySpell attack and enlarged direct impact play as before. Wrag's death pose now starts **one second after impact**, changed from two seconds.
2. Once Voss's sequence completes, the existing End Turn button becomes available.
3. End Turn advances `GameHandler` from Voss to Wrag. Card selection, targeting and repeated End Turn clicks are locked while Wrag acts.
4. The existing `Biting_Wrag` hierarchy object plays `BitingWragAnim`. At frame 21 of its 36-frame, 12 fps clip, the existing impact appears directly on Voss, his hurt state starts, and Wrag's configured skill applies damage once.
5. After the bite and hurt reaction finish, the original idle presentation and player turn return. Five cards remain available, selection clears, and completing the next Voss attack enables End Turn again.

If the presentation is interrupted, it restores visibility, removes its VFX, releases its input lock and finishes Wrag's turn. Missing animation/VFX/skill references produce an actionable warning and safely end that turn. Lethal damage leaves battle input disabled rather than starting another attack.

## Root causes and integration

The original Toolkit End Turn handler called only `OnTurnEnd()`; it did not advance the turn manager. `BotPlayer.UseSkill()` and `BotPlayer.EndTurn()` were empty. These missing connections are now completed through the existing actor list, `StartTurn`/`EndTurn` methods and damage dispatcher.

The `BotTurnAnimation` presentation hook claims the configured Wrag turn while keeping the Behavior Graph's turn gate closed, preventing it from competing with the bite sequence. Other bot presentations retain the graph path. No second turn manager was introduced. Card and bot primary actions share the original action dispatch and `TakeDamage` calculation.

## Reused animations and VFX

- Wrag: `Biting_Wrag`, using `Assets/Sprites/28102026/Wrag/Biting_Wrag/BitingWragAnim.anim`, a non-looping three-second clip. Inspection of the bite frames placed the hit at the closing jaws, frame 21 (normalized time 0.5833333).
- Voss: `Voss_Player` already has `Base Layer.HurtVossAnim` in `Assets/Animation/Voss.controller`, backed by the non-looping three-second `Assets/Animation/HurtVossAnim.anim`. This is the existing hurt presentation used for the requested `Voss_Hurt` reaction; no duplicate hurt character was created. Direct state playback avoids waiting for the controller's idle exit-time transition.
- Impact: `Assets/Eric VFX Studio/Free Game VFX/Prefab/FX_Purple_Hit_02.prefab`, retaining the existing **1.35** instance scale and Hierarchy scaling on its child systems. Voss's actual hurt sprite supplies the torso position, with the existing camera-depth offset. There is no travelling effect.

## Files and scene objects modified

| File | Change |
| --- | --- |
| `Assets/UI/CrimsonDoom/Scripts/GameViewCombatSequence.cs` | One-second death deadline; bite/hurt/impact sequencing; reuse existing visibility, input lock and cleanup. |
| `Assets/UI/CrimsonDoom/Scripts/GameViewUIToolkit.cs` | End Turn eligibility, turn-change subscription, interaction guards and counterattack presentation hook. |
| `Assets/Scripts/GameHandler.cs` | Guarded advancement of its existing turn list, current-actor state/events, shared primary skill-action dispatch. |
| `Assets/Scripts/Game/BotPlayer.cs` | Complete skill application and End Turn; reject duplicate skill applications during one turn. |
| `Assets/Scenes/GameView.unity` | Existing `[UI Toolkit] Crimson Doom Game UI` component: reference `Biting_Wrag`, hit time 21/36, Voss torso point (0.5, 0.58). |

The scene diff adds only those three serialized fields. `Biting_Wrag`, `Idle_Warg`, `Death_Warg` and `Voss_Player` retain their authored transforms, controllers and components. Their visibility changes only during runtime playback. No UXML or USS files were modified.

## Unity Play Mode results

Tested in Unity 6000.6.7f1 through the existing card, enemy-target and End Turn button callbacks. **108 checks passed, zero failed.** See [results.json](CounterattackVerification/results.json), [scene audit](CounterattackVerification/scene-audit.json), and [counterattack Game View capture](CounterattackVerification/counterattack-hit-game.png).

- Voss's impact-to-death delay measured **1.000000 seconds** across five attack cycles.
- Wrag's impact occurred at normalized animation time **0.5833342**; Voss's hurt state started at normalized time **0.0** in that same frame.
- Direct impact position was `(-3.93, 0.25424, 0.20002)`, matching Voss's visible hurt torso. Particles emitted without travelling.
- The starting scene's configured Wrag attack value of 5 passed through the unchanged damage calculation, removing 2 HP: **50 → 48**, once. A subsequent normal attack removed **48 → 46**. Duplicate clicks and a second `UseSkill` call did not apply additional damage.
- Pause/resume, selection restoration, five-card display, UI disable/re-enable during windup, deliberate missing-reference recovery, and lethal-hit completion all passed. Scene root count remained 16, with one Voss actor and one Wrag actor.
- No runtime errors or exceptions were captured. One warning was expected from the deliberate missing-reference test. Existing Visual Scripting node-cache and DOTween importer-version warnings also recurred; see [console summary](CounterattackVerification/console-summary.json) and [runtime error capture](CounterattackVerification/runtime-errors.json).

The temporary Editor verification script was removed from `Assets` after testing. Its non-compiling reference copy remains in [CrimsonCounterattackVerification.cs.txt](CounterattackVerification/CrimsonCounterattackVerification.cs.txt).

Final cleanup compilation succeeded with zero errors: [compilation result](CounterattackVerification/cleanup-compilation.json). Unity is out of Play Mode with the scene saved. Temporary Play/Stop/Save allowlist entries were removed.
