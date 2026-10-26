# Crimson Doom UI Toolkit HUD and scene rendering

Open `Assets/Scenes/GameView.unity`. The HUD root is
`[UI Toolkit] Crimson Doom Game UI`. Characters and the background render
normally through Main Camera. The Toolkit battlefield is transparent.

## Add or edit a character

1. Add a GameObject or prefab to the Hierarchy with a SpriteRenderer and a
   Sprite assigned. An empty GameObject cannot render by itself.
2. Enable the GameObject and all its parents, and enable its SpriteRenderer.
   Keep the renderer color alpha above zero and use a sprite-compatible material.
3. Set its Transform directly in the normal Inspector. For the current 2D camera,
   use Z=0.4, rotation zero initially, and a nonzero scale. Main Camera is at
   Z=-10 with orthographic size 5; at 16:9 its visible XY range is approximately
   X=-8.89 to 8.89 and Y=-5 to 5. Frame the object in Scene View with F, then check
   Game View. Move it or adjust the camera if its visible art is outside the view.
4. Use a layer included in Main Camera's Culling Mask. Default/layer 0 is included
   in the current scene. Background has Sorting Order 0; use 1 or higher for a
   character on the same Sorting Layer.
5. For animation, add an enabled Animator with an Animator Controller and make
   the desired sprite clip its default state. Use sprite-only animation keys if
   you want to adjust placement manually; Transform curves, root motion and
   animated renderer-enabled/color properties are controlled by the Animator.
6. Save the scene outside Play Mode. Ordinary Unity Play Mode edits are discarded
   when Play Mode ends.

No entry in GameViewUIToolkit's Character Animations list is required for
visibility. This applies to Wrag_Attack and to new objects added during Play
Mode. Visibility follows the actual Hierarchy, renderer and camera settings.
Voss_Attack and Voss_Hurt were manually inactive in the user's latest scene;
that choice is retained rather than overridden.

## What was removed

The old view drew an opaque background and copies of registered character
sprites. That covered newly added world objects, including Wrag_Attack. It also
activated actors, forced SpriteRenderer.enabled, changed Animator culling/root
motion settings and rewrote stored Transforms every LateUpdate. All of that
character presentation ownership, duplicate actor images, generated enemy-frame
playback and automatic gameplay animation switching has been removed.

GameViewUIToolkit now reads character bounds only to project transparent target
buttons through Main Camera. It does not change a character's Transform, active
state, renderer visibility, Animator enabled state, culling or root motion.
The current Voss/Wrag objects have only Transform, SpriteRenderer and Animator
components; their clips contain sprite keys and no float/Transform curves.

Main Camera is the rendering camera. Raycast Camera's Camera component is
manually disabled in the scene to prevent an additional perspective render over
Main Camera. Its existing reference and ScreenPointToRay input use remain valid.
The original root Canvas component is also manually disabled to avoid duplicate
legacy HUD/card rendering behind the transparent Toolkit HUD. Its GameObject,
children, scripts and references remain active. To use the old HUD, disable the
Toolkit root and enable that Canvas component manually.

## Optional animation controls

The UI component's custom Inspector still offers Transform fields, a Sprite
object field, Frame in Scene and an explicit edit-mode sprite-sequence preview.
These edit the real object only when you change a field or press Play Preview.
Stopping preview restores its temporary sprite, without restoring Transforms.
You can use the ordinary Transform Inspector instead.

The gear panel's Character Animations list is optional. A binding adds a button
which restarts an already enabled Animator only when clicked. It does not enable
an inactive object or change any visibility/settings. Restart Idle similarly
restarts the configured idle Animators only when clicked. Public
PlayHeroAnimation and PlayCharacterAnimation methods remain explicit hooks.

## Gameplay HUD

GameView.uxml and GameView.uss define the screen. Components/HandCard.uxml is the
card template. GameViewPanelSettings uses a 1672 x 941 reference resolution and
sorting order 100 above the camera image. The HUD layout supports compact,
narrow and portrait sizes, a scrolling hand and an options overlay.

The gameplay Player and opponents were inactive in this scene. DESIGN PREVIEW
therefore uses reference hand/status values. When an existing player initializes,
the view reads currentHand, subscribes to status events and delegates card
selection/targeting to existing Card and InputHandler instances. It does not
create a new turn system or game state. A 100 ms schedule detects hand changes;
target geometry reads the actual camera and renderer bounds each frame.

GameHandler.SelectedCard and InputHandler.IsPointerInputBlocked remain the
compatibility hooks. Disabling the Toolkit unsubscribes its callbacks. Reference
currency, energy and intent values remain separate from live gameplay data.

## Verification

See Documentation/CrimsonDoomUI/Visibility/ManualSceneRendering.md for the current
Play Mode checks and screenshot. Earlier visibility reports describe superseded
approaches and are retained only as historical evidence.
