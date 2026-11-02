# Voss animation speed

Updated only `Assets/Animation/Voss.controller`:

- Idle: **0.25 → 1.0**, removing the quarter-speed playback (approximately 21.33 → 5.33 seconds per cycle).
- HolySpell, rod attack and hurt states: **1.0 → 1.5**, making each three-second clip play in two seconds.

Clips, transitions, scene objects, Wrag's animation speeds, VFX and runtime scripts are unchanged. Saving through Unity also upgraded the controller's serialization version metadata.

Play Mode smoke verification passed **31 checks**, including the actual card → target → Voss attack → End Turn → Wrag counterattack → player-control sequence. The faster spell's impact occurred at **1.1167 seconds**, within one rendered frame of the expected frame-20 timing (1.1111 seconds). Wrag's impact-to-death delay remained **1.0000 second**. Hurt/impact synchronization, damage, repeated-click protection and selection restoration passed. No runtime errors or exceptions were captured.

Evidence: [results](VossSpeedVerification/results.json), [runtime errors](VossSpeedVerification/runtime-errors.json). Temporary Editor tooling was removed after testing; reference source copies remain in `VossSpeedVerification`.
