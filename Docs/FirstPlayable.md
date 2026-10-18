# Scaredy Cat first playable

Open **Assets/Scenes/Apartment.unity** and press Play. The intro follows the apartment spline, pauses on Knot 3 to introduce the dancing owner, opens the bedroom door, and finishes on Cheeto under the spotlight. The camera then returns to first person. Space skips the intro. The owner keeps dancing until Cheeto enters the nearby trigger, then starts wandering through the apartment. Slap or break props, rub familiar objects, and knock the owner down in each room to empty the **Menace Meter**.

## Controls and tuning

- WASD: move. Mouse: look. Shift: sprint. Space: jump.
- Hold left mouse: reveal/wind up the paw. Release: strike. A quick release swats; 0.8 seconds reaches full force.
- Approach a marked-object target and hold E for 1.4 seconds when the prompt appears. The green bar fills while you hold; releasing E or leaving range cancels. Completing **Mark Territory** removes its WorldUI prompt, adds the green outline, emits a green particle burst, and gives a small camera bump.
- During owner dialogue, press Enter or left click for the next line; Space skips the whole cutscene.
- Escape: pause, resume, adjust mouse sensitivity/camera motion, or restart.
- F1 in MechanicsLab: test controls. The panel locks gameplay input while the world keeps running.

Tuning assets live in **Assets/ScaredyCatContent/Settings**: Movement, Attack, Rewards, and Feedback. Audio clips can replace the temporary synthesized sounds independently. Runtime menu adjustments do not modify the assets.

The cat and paw use procedural transforms only. No cat Animator Controller or animation clips are needed. The paw is part of the player prefab's camera hierarchy and has no gameplay collider: a short sweep resolves the actual contact. Body art is visible during cinematic views. World and paw render at 288 pixels high; the HUD renders at the display resolution.

## Three-stage apartment run

The top-right timer starts when the intro returns control, continues through owner dialogue, pauses with Escape, and stops as soon as Menace reaches zero. A restart resets it. Each prop gives 3 Menace once for a valid slap; a prepared fragile prop gives another 10 once when it breaks. Mass and launch multiplier are editable on each `MischiefProp`; lighter props travel farther under the same impulse. The kitchen has two cups and four plates with editor-prepared OpenFracture fragments. Two books, a sofa pillow, and a spare toilet roll are launchable.

The sofa, bedroom bed, kitchen sink counter, bathroom sink, and living-room coffee table are Mark Territory targets. Each grants 8 once and changes only its assigned renderers to the `Marked` rendering layer. Their indicators render through the full-resolution WorldUI camera. The red `Smashable` and green `Marked` outlines are configured together on the active PC renderer. The owner grants 12 once in each of the four room volumes; knockdowns in the doorway gaps give no room reward.

A cat-body collision does not fracture a fragile prop. Fracture by collision needs a recent paw-attributed hit, including a chain reaction from another struck prop. Four apartment paintings have colliders and kinematic Rigidbodies: they stay on the wall until a paw hit releases them, then fall and grant their one-time slap reward. To author another knockable wall prop, add a Collider, Rigidbody, `ImpulseAttribution`, and `MischiefProp`; turn on **Start Kinematic Until Hit** and turn off **Breakable**. Tune its mass and launch multiplier on `MischiefProp`.

At Menace 66 and 33, stage dialogue waits for ragdoll recovery, stops roaming, frames the owner, and then restores roaming. The purple head mist falls to 60% and 25% of its starting emission while the vignette fades to 0.16 and 0.08. Crossed milestones queue in order. At zero, the timer stops immediately, the owner sits on the sofa, the cat moves to the lap anchor, final dialogue plays, and the victory screen shows the final time. The stage dialogue, timer, vignette, victory panel, and WorldUI indicators are serialized in the apartment scene under **Apartment Stage Content**.

## Test each system

Open **Assets/Scenes/MechanicsLab.unity**. It starts directly in gameplay. F1 exposes station teleports, arbitrary attack charge, owner knockdown, prop reset, fracture, and agro reduction. Use the steps, slope, ceiling, occluding wall, and table to tune movement and targeting.

A prop reset restores physical state but deliberately retains its reward history. **Start again** resets the entire run, including props, camera triggers, owner, and rewards.

Run the **ScaredyCat.EditModeTests** and **ScaredyCat.PlayModeTests** assemblies from Window > General > Test Runner. Play Mode tests load both scenes and verify the complete ending/restart path in addition to individual mechanics. Physics assertions use tolerances and state transitions rather than exact simulation trajectories.

## Scene and prefab authoring

- **Player**: `Assets/Prefabs/Player.prefab`; Rigidbody capsule, input, motor, head anchor, body art, camera, and paw. Assign scene feedback to PawAttack and CameraDirector when placing this prefab in another scene.
- **Owner**: `Assets/ScaredyCatContent/Prefabs/Owner.prefab`; NavMeshAgent, separate wandering and ragdoll components, standing collider, and jointed Humanoid skeleton. Assign wander points and scene agro/feedback references.
- **Vase / Vase Tall / Book**: generated reusable prop prefabs. Assign AgroMeter, DebrisBudget, and FeedbackDirector references. Preserve the stable parent that holds MischiefProp and its reward identity.
- **Apartment stage content**: keep the four room reward volumes separate at doorways. Mark Territory targets have explicit interaction anchors and renderer lists. The six kitchen dishes have saved fragment meshes in `Assets/ScaredyCatContent/Meshes`; their breaking does not cut meshes at runtime. The sofa sitting and lap anchors, stage shots, and all new HUD objects are scene-authored.
- **Navigation**: baked from environment colliders on Default. Cat, owner, props, paw, and debris have dedicated layers. Dynamic debris is excluded from navigation and does not collide with the cat or other debris.
- **Owner roam anchors**: `Apartment Stage Content > Owner roam anchors` holds one point in each room. The scene OwnerBrain references all four. It picks the least recently visited room with a complete NavMesh path, moves at 2.25 m/s, briefly pauses, and retries another point if stuck. There is a 30% chance it pauses for a 3.2-second looping Macarena at a destination. The `Macarena Dance` FBX is configured to retarget onto the owner's Humanoid avatar. Move the anchors to adjust coverage; keep them on the baked NavMesh and preserve doorway links.
- **Bedroom access**: the doorway NavMeshLink is for NavMeshAgents such as the owner. The cat uses Rigidbody movement, so the intro also disables the moving bedroom door's colliders when it opens. Skipping the intro opens the door and clears those colliders too. `ApartmentNavMeshLinks` samples both link endpoints onto the baked NavMesh.
- **Ending pose**: select `Apartment Stage Content > Owner sitting on sofa` and adjust its world position and Y rotation to place the owner's root on the sofa. Then select its child `Cat lap anchor` and adjust its local position and Y rotation for the cat. `GameFlow.acceptanceOwner` and `acceptanceCat` reference these two transforms. Make adjustments outside Play mode so they persist. The ending teleports the owner and cat to the anchors before playing the sit animation and final dialogue.
- **Text and timer**: scene UI text uses TextMesh Pro, with the font asset under `Assets/TextMesh Pro`. `Stage HUD > Timer > Speedrun timer` is the timer text above the authored timer image. `Stage HUD > Nearby E interaction` is the readable screen prompt; the five `Mark Territory indicator` canvases remain on the crisp WorldUI layer.
- **Writing stage dialogue**: in the Apartment hierarchy select `Apartment Stage Content > Stage 1 - building trust`, `Stage 2 - making room`, or `Stage 3 - welcome home`. In its `Cutscene Sequence` component, expand **Shots** and write one line in each shot's **Caption** field. Leave **Wait For Advance** enabled to step through lines with Enter or left click; Space skips the full sequence. The Stage HUD's `StagePresentation.dialogue` points to `Owner line`, and `StagePresentation` shows it during stage and ending dialogue. Intro spline captions are created by `ApartmentIntroDirector.ConfigureSequence()` in code, so edit the four `SplineShot` caption strings there. The intro owner/cat cards are separate authored UI objects.
- **Ending cat size**: select `Scaredy Cat Systems` and adjust `GameFlow > Ending Cat Scale Multiplier` (default 0.62). The ending applies that scale only when the cat is placed at the lap anchor.

`Scaredy Cat > Build or Rebuild First Playable` prepares the historical first playable systems. It replaces the **Scaredy Cat Systems** hierarchy and generated prefab content; running it after the apartment stage setup requires reconnecting the scene-authored stage content. Existing tuning assets are retained. Imported source art is reused, with normalized meshes/material copies in ScaredyCatContent; the household model importer enables mesh readability for editor fracture generation.

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
- Props, Mark Territory targets, and owner report `MischiefEvent` outcomes. `AgroLedger` deduplicates slap/break/mark identities and owner room IDs, clamps at zero, and announces completion once. The existing `RubTarget` and `AgroMeter` types remain internal names for compatibility.
- `DebrisBudget` retires expired/oldest fragments and preserves their assets for station resets.
- `GameFlow` owns the intro/play/ending/complete transition and pause/debug locks.
- `FeedbackDirector` handles sounds, particles, and bounded camera offsets. Impact pauses affect only the paw presentation, never global simulation speed.

**Scaredy Cat > Build macOS Development Player** (or `ScaredyCat.Editor.FirstPlayableBuilder.BuildDesktop` in batch mode) creates a development macOS build in **Builds/ScaredyCat.app**. Apartment is the startup scene; MechanicsLab is the second scene. Switch the build target in Unity for another desktop platform and install its build support first.

The active camera compositor uses a 288-pixel-high texture for the world and a separate display-resolution texture for `WorldUI`; the old HUD compositor component is disabled in Apartment. A geometry-free display camera submits the full-resolution overlay canvas. CameraDirector remains the sole owner of the camera pose, and the WorldUI camera copies that pose after CameraDirector updates. Splines is pinned to **2.9.1**, the [released package for Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.splines.html).

See [Verification.md](Verification.md) for the automated checks and desktop validation procedure.
