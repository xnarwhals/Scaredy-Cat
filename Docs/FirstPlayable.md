# Scaredy Cat first playable

Open **Assets/Scenes/Apartment.unity** and press Play. The intro follows the apartment spline, pauses on Knot 3 to introduce the dancing owner, opens the bedroom door, and finishes on Cheeto under the spotlight. The camera then returns to first person. Space skips the intro. The owner keeps dancing until Cheeto enters the nearby trigger, then starts wandering through the apartment. Break and knock props or swat the owner to empty the agro meter.

## Controls and tuning

- WASD: move. Mouse: look. Shift: sprint. Space: jump.
- Hold left mouse: reveal/wind up the paw. Release: strike. A quick release swats; 0.8 seconds reaches full force.
- Escape: pause, resume, adjust mouse sensitivity/camera motion, or restart.
- F1 in MechanicsLab: test controls. The panel locks gameplay input while the world keeps running.

Tuning assets live in **Assets/ScaredyCatContent/Settings**: Movement, Attack, Rewards, and Feedback. Audio clips can replace the temporary synthesized sounds independently. Runtime menu adjustments do not modify the assets.

The cat and paw use procedural transforms only. No cat Animator Controller or animation clips are needed. The paw is part of the player prefab's camera hierarchy and has no gameplay collider: a short sweep resolves the actual contact. Body art is visible during cinematic views. World and paw render at 288 pixels high; the HUD renders at the display resolution.

## Test each system

Open **Assets/Scenes/MechanicsLab.unity**. It starts directly in gameplay. F1 exposes station teleports, arbitrary attack charge, owner knockdown, prop reset, fracture, and agro reduction. Use the steps, slope, ceiling, occluding wall, and table to tune movement and targeting.

A prop reset restores physical state but deliberately retains its reward history. **Start again** resets the entire run, including props, camera triggers, owner, and rewards.

Run the **ScaredyCat.EditModeTests** and **ScaredyCat.PlayModeTests** assemblies from Window > General > Test Runner. Play Mode tests load both scenes and verify the complete ending/restart path in addition to individual mechanics. Physics assertions use tolerances and state transitions rather than exact simulation trajectories.

## Scene and prefab authoring

- **Player**: `Assets/Prefabs/Player.prefab`; Rigidbody capsule, input, motor, head anchor, body art, camera, and paw. Assign scene feedback to PawAttack and CameraDirector when placing this prefab in another scene.
- **Owner**: `Assets/ScaredyCatContent/Prefabs/Owner.prefab`; NavMeshAgent, separate wandering and ragdoll components, standing collider, and jointed Humanoid skeleton. Assign wander points and scene agro/feedback references.
- **Vase / Vase Tall / Book**: generated reusable prop prefabs. Assign AgroMeter, DebrisBudget, and FeedbackDirector references. Preserve the stable parent that holds MischiefProp and its reward identity.
- **Navigation**: baked from environment colliders on Default. Cat, owner, props, paw, and debris have dedicated layers. Dynamic debris is excluded from navigation and does not collide with the cat or other debris.

`Scaredy Cat > Build or Rebuild First Playable` prepares both scenes and the generated assets through Unity's serialization APIs. It replaces the **Scaredy Cat Systems** hierarchy and generated prefab content; save hand-authored additions outside that hierarchy before rebuilding. Existing tuning assets are retained. Imported source art is reused, with normalized meshes/material copies in ScaredyCatContent; the household model importer enables mesh readability for editor fracture generation.

The owner uses a Humanoid Idle/Walk blend tree driven by agent velocity, with root motion disabled. The walking cycle is a provisional trim from the supplied Start Walking clip. Replace the Walk child motion in **ScaredyCatContent/Animations/Owner.controller** with a proper in-place loop when available. Recovery blends from the physical bone poses to idle and resumes the agent only at a clear NavMesh location.

## Narrative sequences

Create a `CutsceneSequence` and add shots in its Inspector. Each shot has a spline range or camera pose, optional look target and offset, lens, duration, blend-in time, caption, easing curve, and entry event. `ApartmentIntroDirector` configures the apartment's authored spline as four beats: approach the owner, hold on Knot 3, continue while opening the bedroom door, and hold on Cheeto before returning to first person. It owns the two world-space intro cards (`ownerName`, `ownerDetails`, `catName`, and `catDetails`) and leaves the owner dancing until `StartOwnerActivity` is entered. `ApartmentNavMeshLinks` connects the existing doorway regions for owner navigation.

Call `CutsceneManager.Play(sequence)` from gameplay or a `NarrativeTrigger`. Ordinary sequences queue and wait for grounded movement. `Skip()` finishes the active sequence; `Cancel()` clears queued work and restores the player view. `PlayPriority()` replaces queued work for the ending. Completed and skipped sequences invoke their completion UnityEvent; cancellation and invalid-target termination only send the typed lifecycle notification. Entry events for skipped shots do not run: persistent ending state belongs to GameFlow, not cosmetic shot callbacks.

Input locks are owned leases. A sequence releases only its own lock, so skipping while another system holds control cannot accidentally enable movement. Pausing cancels the paw charge and pauses simulation, including the narrative clock. Narrative playback itself keeps simulation alive.

## Runtime boundaries

- `GameInput` owns a cloned Input Action asset; it never toggles the shared project asset.
- `CatMotor` owns Rigidbody locomotion; `CameraDirector` owns the final camera transform.
- `PawAttack` creates a `HitContext`; `ISwatReceiver` implementations decide the physical response.
- `ImpulseAttribution` carries a bounded player-causality lifetime across collision chains.
- Props and owner report `MischiefEvent` outcomes. `AgroLedger` deduplicates prop/event identities and owner recovery cycles, clamps at zero, and announces completion once.
- `DebrisBudget` retires expired/oldest fragments and preserves their assets for station resets.
- `GameFlow` owns the intro/play/ending/complete transition and pause/debug locks.
- `FeedbackDirector` handles sounds, particles, and bounded camera offsets. Impact pauses affect only the paw presentation, never global simulation speed.

**Scaredy Cat > Build macOS Development Player** (or `ScaredyCat.Editor.FirstPlayableBuilder.BuildDesktop` in batch mode) creates a development macOS build in **Builds/ScaredyCat.app**. Apartment is the startup scene; MechanicsLab is the second scene. Switch the build target in Unity for another desktop platform and install its build support first.

The world uses one low-resolution texture. A geometry-free display camera lets URP submit the full-resolution overlay canvas; CameraDirector remains the sole owner of the world view. Splines is pinned to **2.9.1**, the [released package for Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.splines.html).

See [Verification.md](Verification.md) for the automated checks and desktop validation procedure.
