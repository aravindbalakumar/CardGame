# Crimson Doom runtime stability report

Date: 2026-11-02

Status: **GameView's tested runtime paths pass.** Source fixes compile successfully, the camera change is saved, and both Play Mode verification phases passed. Remaining legacy/vendor asset findings are listed below. No new gameplay features were implemented.

## Scope and preservation

- Inspected the running Unity Editor for this project (6000.6.7f1), GameView's hierarchy and components, gameplay scripts, prefabs, Animator controllers, UXML/USS, graph serialization, Console history, and Editor.log.
- GameView is the sole enabled build scene. Recovery snapshots and vendor sample scenes were distinguished from the active gameplay scene.
- Preserved existing local changes, all 16 scene roots, character objects, card assets, animations, VFX assets, and the inactive legacy objects. No GameObjects were created or deleted.
- Did not implement or extend Voss-versus-Wrag combat. Existing battle-sequence code was left untouched.
- Pre-edit copies of the main affected files are in `Temp/CrimsonDoomStability/baseline/`. Console evidence is in `Temp/CrimsonDoomStability/`.

## Findings and changes

Observed Console/history findings are distinguished below from defects established by source inspection. Play Mode results and their limits are recorded below.

| Finding | Root cause | Change / affected files |
| --- | --- | --- |
| 40,018 captured repeats of “Branching condition evaluated as false, but there is no child to run.” | The repeating Warg graph's `isMyTurn` branch had no false-path destination. | Connected that output to the existing terminal join and turn-reset node. Updated both authoring-port connections and baked graph parent/child references in `Assets/Scripts/Game/BehaviourTree/Warg.asset`. No graph nodes added or removed. |
| Bot initialization and actor cleanup defects (source inspection) | `BotPlayer.Initialize` hid the base virtual method and subscribed separately. Derived `OnDestroy` methods hid base cleanup, and actor targeting/click subscriptions were only removed when the entire game closed. | Made bot initialization a proper override, retained base status initialization, removed duplicate event subscription/cleanup paths, and made actor destruction remove every actor subscription. Modified `BotPlayer.cs`, `AbstractActor.cs`, and `Player2D.cs`. Optional graph-less legacy bots remain supported. |
| Invalid bot turn-variable assignment (source inspection) | `StartTurn` supplied the string `"true"` to the boolean `isMyTurn` variable. Initial armor also used MaxArmor instead of current Armor. | Supply a boolean and current armor in `BotPlayer.cs`. The existing BehaviorGraphAgent retains ownership of starting its graph. |
| Cast node always failed to retain a selected skill (source inspection) | A local `skillIndex` shadowed the instance field, leaving the field at -1. Invalid input returned the nonterminal initialization status. | Reset the per-execution fields, assign the instance index, and return Failure for invalid bindings in `CastSkillOfTypeAction.cs`. The existing `UseSkill` placeholder was not implemented. |
| Missing script on legacy Player prefab (Unity prefab scan) | `UserPlayer.cs` and its original metadata had been deleted while the prefab retained their GUID. Git history identifies the original script. | Restored `Assets/Scripts/UserPlayer.cs` and its original `.meta` identity, adapting initialization to the existing actor lifecycle. In `Player.cs`, removed a self-assignment, exposed the existing loadout to that subclass, and preserved the old `_collider` serialized field using FormerlySerializedAs. The original prefab itself was preserved. Unity's subsequent scan resolves its script. |
| Overhead UI could throw during disable/destroy, lose unrelated listeners, and stop updating after re-enable (source inspection) | Cleanup assumed `LoadStausUI` had assigned an actor, removed every slider listener, and did not rebind after re-enable. | Clean up only this component's subscriptions, handle the intentionally unbound state, rebind on enable/reassignment, and refresh the initial health label in `PlayerStatusOverheadUI.cs`. |
| Selection/click failure paths (source inspection) | Actor delegates could have no subscribers; explicit null card selection dereferenced a null card; the raycast callback applied the action to `this` instead of the hit actor. | Use optional delegate invocation, support selection clearing, and apply the existing card action to the validated hit actor in `Player2D.cs` and `BotPlayer.cs`. Optional outlines are checked before use. |
| Incorrect input-raycaster setup (scene inspection) | Active Wrag has a 3D BoxCollider, but Main Camera only had Physics2DRaycaster. Its pointer handler could not receive a 3D physics hit. | Added one PhysicsRaycaster to the existing Main Camera. `GameHandler.cs` applies and restores targeting masks for both physics raycasters. **Saved in GameView.unity; the actual EventSystem raycast reaches the existing Wrag collider in Play Mode.** |
| Targeting-mask and singleton lifetime defects (source inspection) | Deselecting did not restore the original event mask. Destroying a duplicate handler could broadcast global shutdown; the owner did not explicitly clear its singleton. | Restore authored masks, restrict shutdown to the owning instance, and clear ownership in `GameHandler.cs`. |
| UI Toolkit null access (source inspection) | Refresh could access `localPlayer.SelectedCard` while a handler existed but localPlayer was absent. | Require the player reference for that refresh path in `GameViewUIToolkit.cs`. |
| Two historical UI Toolkit `RebuildHand` NullReferenceExceptions (Editor.log) | The recorded stack traces refer to an earlier source revision and visual-tree initialization/live reload. The precise historical null cannot be established from the current files. | Current live-hand construction, health labels, and disable/re-enable callbacks pass in Play Mode with the UIDocument and view component temporarily enabled. Their authored disabled state was restored. The historical exception did not recur. |

## Verification performed

| Check | Result |
| --- | --- |
| Unity compilation and domain reload after source changes | Passed. Both script assemblies compiled with zero C# errors. Following temporary helper removal, final compile job `fb334bec`, cycle 12, also completed with zero errors. Evidence: `Verification/final-compilation.json`. |
| Behavior Graph reimport and Editor readback | Passed. Warg remains a baked graph with one root, 36 authoring nodes, and eight blackboard variables. |
| Managed-reference integrity check on the behavior assets | No unresolved local managed-reference IDs found. |
| Missing-script scan across loaded scenes and prefabs | Reduced from two affected prefabs to one: `CardSlot (0).prefab`, described below. |
| Loaded-scene missing-reference scan | Zero reported issues. This scan does not certify optional references or excluded vendor assets. |
| UI Toolkit initial Edit Mode object toggle | Inconclusive for UI rendering: both UIDocument and view components were authored disabled. Superseded by the successful Play Mode live-UI tests with both components enabled temporarily. |
| Play Mode core verification | Passed 22 checks: both actors initialize; exactly six hand entries and six Canvas cards spawn; exactly two actor-click subscriptions; card selection; matching 2D/3D masks; actual EventSystem physics hit on Wrag; opponent damage; null/same-card deselection; mask restoration; player/bot status effects; boolean bot turn input; both active animations advance; three unbound legacy overhead UI cleanup calls complete safely. Evidence: `Verification/play-core.json`. |
| Play Mode live UI verification | Passed 11 checks including shared startup prerequisites, graph turn reset across real player frames, six live UI cards, correct health text, callback removal/rebinding without duplicates, and restoring original UI visibility. Evidence: `Verification/play-ui.json`. |
| Repeating bot warning | No recurrence during the Play sessions, including 100 explicitly stepped frames. Its retained historical log group still ends at sequence 45147, predating all verification runs. |
| Console inspection | No new gameplay Error/Exception/Assert entries during the successful Play Mode checks. History retains two corrected compile diagnostics from the temporary helper's first API-signature mismatch; these are not unresolved project errors. No log suppression or manual Console clearing was used. The DOTween importer warnings below still occur on domain reload. |
| Source diff whitespace check | Passed. |
| Play Mode startup, teardown, and repeated entry | Multiple real Play Mode entries/exits completed. Successful test phases used real player frames. An initial same-frame test expectation was corrected to allow the graph's frame-dependent join to finish; its turn reset then passed. Full new battle behavior was not exercised or implemented. |
| Save GameView camera component | Saved successfully after the user authorized the requested allowlist. Existing pre-task scene edits were retained. |
| Temporary helper cleanup | Removed the helper and its metadata from Assets after testing. Its reviewable source is retained as `Verification/CrimsonDoomStabilityVerification.cs.txt`, outside compiled project code. |
| Final smoke after helper removal | Startup initialized the player and spawned exactly six cards; teardown produced no new runtime errors. GameView finished saved and clean with 16 roots. An additional frame-step request was interrupted by teardown after two frames; the earlier completed 100-frame test provides the reported frame coverage. Console evidence: `Verification/final-runtime-console.json`. |
| Permission cleanup | Restored the original empty UnitySkills allowlist after verification. The three temporary entries added with user authorization were removed. |

## Unresolved findings and requirements

1. **CardSlot (0).prefab missing script.** GUID `86449087259f263409e1e1337bc46363` has no matching source/metadata in the inspected project or the historical revisions checked. The component has no recoverable custom serialized fields identifying its behavior. Guessing a replacement or deleting it could alter the original design. Restore the original script and GUID from a backup, or establish what this legacy component should do before replacing it. No active GameView dependency on this prefab was found.
2. **Visual Scripting node-cache warning remains.** The final reload again reported 134 outdated node options skipped; Editor.log contains 3,787 historical SerializationExceptions associated with these failed cache rows, including old ray-tracing, Timeline, and renamed game types. These stacks originate in Visual Scripting's editor node cache, not a gameplay script. Regenerate the node library through `Edit > Project Settings > Visual Scripting > Node Library > Regenerate Nodes`, then check whether any source-level references still fail. This regeneration was not performed or claimed as fixed. The warning remains explicitly visible in the final Console evidence; no gameplay exception accompanied it.
3. **DOTween importer warning.** Its DLL metadata uses PluginImporter serializedVersion 1, below this Editor's supported minimum. A normal reimport completed but did not rewrite that old metadata. An Editor-supported importer reserialization or compatible vendor metadata is needed. The DLL itself compiled successfully; no package or plugin file was deleted or replaced.
4. **Unresolved serialized asset GUIDs.** The dependency audit found 12 non-recovery asset files with unresolved external references. This is a static finding, not evidence of 12 active runtime exceptions. They were preserved rather than remapped to guessed assets:
   - `Assets/Animation/Voss_old.controller`: missing animation clip GUID `073be72ba9e669944aad9c78ce3345c1`. This is the older controller; active character objects retain their existing controllers.
   - `Assets/Prefabs/CardSlot (0).prefab`: missing script listed above.
   - Six `Assets/Settings/*_PipelineAsset_ForwardRenderer.asset` files and `Assets/URP/Cusom Renderer.asset`: unresolved legacy `xrSystemData` GUID `60e1133243b97e347b653163a8c01b64`. Check migration requirements for the installed URP version before modifying renderer settings.
   - `Assets/URP/UniversalRenderPipelineGlobalSettings.asset`: unresolved falloff lookup texture GUID `5688ab254e4c0634f8d6c8e0792331ca`. Restore the matching resource or let the appropriate render-pipeline migration regenerate it.
   - Vendor demo `SM_VFX_Background_Plate_01.prefab`: missing mesh GUID `89209d2c2872ccb44adf7a6e4142fa6b`.
   - Vendor demo `PostProProfile Zap.asset`: four unresolved post-processing script GUIDs. Restore the original compatible demo dependencies or migrate the profile using the intended package version. Gameplay particle prefabs were preserved.
5. **Existing gameplay placeholders.** `BotPlayer.UseSkill`, `BotPlayer.EndTurn`, the UI Toolkit `Target` path, and `StatCompareCondition` contain unfinished behavior. They were not expanded into gameplay features during this stabilization task. Console stability alone will not establish that a complete battle or turn loop exists.

## Completion and limits

The user authorized the requested permission change, allowing scene saving and actual Play Mode verification. The tested GameView paths now run without new runtime errors or the repeating bot warning. This is not a claim that every vendor sample scene, missing legacy asset, or unfinished gameplay placeholder is complete. The unresolved asset findings require their original resources or a separate migration decision. New feature work remains unstarted.
