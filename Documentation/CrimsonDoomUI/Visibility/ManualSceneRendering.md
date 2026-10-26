# Scene-owned character rendering

Unity 6000.6.7f1, GameView scene.

Wrag_Attack was active, its SpriteRenderer was enabled on layer 0, its sorting
order was 2, and its bounds were inside Main Camera. It was absent from the UI
animation binding list. The old opaque Toolkit battlefield covered its camera
render and displayed only copies of configured actors. A previous change also
rewrote registered actor transforms and visibility every frame.

The current implementation removes that rendering layer and all runtime actor
state ownership. The HUD is transparent over Main Camera; the scene renders
characters and background. No registration or UI-generated copy is needed.
The separate Raycast Camera no longer renders a second perspective image, and
the old root Canvas component no longer renders a duplicate legacy HUD. Their
objects, input references and gameplay components are retained.

## Play Mode checks

- Wrag_Attack is visible and its animation advances without a UI binding.
- All actor Transforms, activeSelf and SpriteRenderer.enabled values match the
  scene on entering Play Mode. Manually inactive Voss poses stay inactive.
- Changing Wrag_Attack position, XYZ rotation and nonuniform scale directly at
  runtime remains effective over subsequent real frames, without a notification
  call or custom Inspector requirement.
- Disabling its SpriteRenderer manually stays disabled. Deactivating the object
  manually stays inactive. Toolkit does not undo either action.
- Disabling and re-enabling GameViewUIToolkit does not alter actor settings.
- A temporary cloned character added during Play Mode, absent from all UI
  bindings, renders inside the camera frustum and its animation advances.
- Main Camera's culling mask includes Default, actor material/tint/sort settings
  were checked, and the UI root background alpha is zero.
- Character clips contain only sprite keys; none contain Transform curves.
- Scripts compile without errors. Source scan confirms no runtime character
  Transform/visibility/activation/Animator-settings assignments remain in the HUD.

The temporary clone and runtime edits were not saved into the scene.

[Current Game View](manual-scene-game-final.png)
