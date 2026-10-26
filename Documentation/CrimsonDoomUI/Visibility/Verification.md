> Historical report: superseded by [scene-owned rendering](ManualSceneRendering.md). Runtime visibility/Transform ownership and Toolkit actor copies described below have been removed.

# Voss animation visibility verification

Unity 6000.6.7f1, GameView scene, Play Mode, 1920 x 1080 Game View.

## Findings and fixes

- The previous Toolkit hero image used a single Idle-specific texture UV crop
  for every animation. Attack frames are 216 x 384; Idle and Hurt are 768 x 768.
  It also bypassed the Sprite's own rect, which is incorrect for sliced sheets.
  Hero and enemy images now consume Sprite assets directly and derive geometry
  from their own rect, pivot and pixels per unit.
- Playback previously enabled a SpriteRenderer without ensuring the GameObject
  or its Animator was active. Animator references omitted from a binding also
  disabled its button even when the object had an Animator. Playback now
  activates the referenced hierarchy, enables its Animator, and resolves an
  omitted Animator from the object. It records original states for restoration.
- Preserving Transform only around Animator.Rebind/Update(0) did not protect
  subsequent animation evaluations. Rebind is removed from playback. The view
  owns the configured local pose and applies it after animation in LateUpdate,
  along with selected-renderer visibility. Custom Inspector edits explicitly
  update the owned pose. Animator culling is AlwaysAnimate and root motion is
  disabled while the view owns playback.
- The camera was at Z=0, near plane 0.3, with actor planes at Z=0.4. Moving the
  orthographic Main Camera to Z=-10 provides rotation clearance without changing
  XY framing or scale. Both actors use layer 0; the camera's culling mask is -1.
  Their unlit materials, opaque tint, sorting order 1 above background order 0,
  scene references, active states, and Scene Visibility settings were checked.
- The Scene View had been angled in perspective. A Frame in Scene control
  provides a 2D front view of the selected object.

The follow-up runtime visibility failure was caused by the slot model itself:
SetActiveCharacterRenderer disabled all non-selected renderers, and LateUpdate
repeated that every frame. Idle was selected on startup, so Attack and Hurt
were always disabled until individually selected. The previous fix had retained
that behavior and did not meet the independent visibility requirement.

Show All Animation Objects now defaults to true and is saved in GameView.
Every configured renderer stays enabled, including at startup. Every object
has an independent Toolkit image because enabling world renderers alone would
leave them behind the opaque Toolkit background. Selecting an animation only
changes which object is restarted/targeted. It does not hide other objects.
Mutually exclusive poses remain an explicit opt-in by disabling the setting.

The follow-up check confirmed all three renderers enabled at startup with no
button interaction, three independently visible images, advancing frames,
unchanged Transforms, and no other object hidden after clicking Attack, Hurt
or Idle. New captures: [all objects in Game View](all-objects-game.png) and
[all objects in Scene View](all-objects-scene.png). Earlier captures below show
the previous individual selection checks.

## Play Mode checks

| Check | Result |
| --- | --- |
| Attack via Toolkit animation button | Active scene renderer and matching Toolkit Sprite; visible in both views |
| Hurt via Toolkit animation button | Active scene renderer and matching Toolkit Sprite; visible in both views |
| Frames over real editor/player updates | Sprite references advance for Attack, Hurt and a newly added pose |
| Actor placement over playback | Original local position, rotation and scale remain unchanged |
| Camera inclusion | Both actor bounds intersect the main camera frustum; centers are in viewport |
| Deliberately hostile animation | A temporary clip sets position X=42, scale X=0 and renderer enabled=false; LateUpdate restores X=0.18, scale X=1 and enabled=true, also across subsequent real frames |
| Add a new binding in Play Mode | New button appears without rebuilding the scene or restarting Play Mode |
| Inactive new object / disabled Animator | Selecting its button activates and enables it; AlwaysAnimate is applied |
| Omitted Animator reference | Falls back to the SpriteRenderer object's Animator |
| Nonzero XYZ rotation / nonuniform scale | New pose retains its configured Transform over playback |
| Disable and re-enable Toolkit | Renderer, active state, Animator enabled/culling/root-motion ownership is released and original states restored |
| Timed gameplay Attack | Returns to Idle after the clip duration |
| Reset to Idle button | Focused Toolkit NavigationSubmit restores Idle |
| Animate battlefield art toggle | Idle UI sprite stays frozen while disabled; an explicitly selected animation still advances with unchanged Transform |

Regression clips and temporary objects were created only in memory during Play
Mode. They are not saved into the scene or the source animation assets.

## Captures

- [Attack in Game View](attack-game-verified.png)
- [Attack in Scene View](attack-scene-verified.png)
- [Hurt in Game View](hurt-game.png)
- [Hurt in Scene View](hurt-scene-verified.png)

The small Attack sprite reflects its 216 x 384 source frames at 100 pixels per
unit and its configured scale of 1. Its size is editable independently; playback
does not silently resize it to Idle's 768 x 768 source dimensions.

Compilation succeeded. The existing Visual Scripting node-options error and
DOTween metadata warnings remain outside this change. Screenshot tooling during
the initial Play Mode transition also reported PlayerLoop reentry; captures
were repeated after opening the Game View and allowing playback to run.
