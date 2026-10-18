# First playable verification

The lower sections record the original generated first playable and its historical prop totals. The current apartment stage checks are listed first.

## Three-stage apartment validation

The current apartment scene was imported and saved with Unity **6000.3.11f1** in an isolated project copy while the main Editor remained open. Scene validation found **four non-overlapping owner reward areas, five Rub targets, six kitchen dishes with saved OpenFracture pieces, and four launchable props**. The six kitchen dishes each have eight saved fragments. The apartment now has 14 `MischiefProp` objects total: four existing vases, six dishes, and four launchables.

The updated **Edit Mode suite passes 11/11**. It covers one-time slap, break, Rub, and per-room owner reward identities. The updated **Play Mode suite passes 21/21**. The apartment checks cover Rub indicator removal and Marked render-layer assignment, nearby NavMesh approaches, red Smashable layers on breakables, full-resolution WorldUI versus 288-pixel world output, stronger launches for light props after a physics step, room-by-room owner rewards and doorway gaps, timer pause and stop, ordered stage playback and skipping, owner sitting state, victory UI, and restart. These are automated behavior checks; use the Game view to tune the sofa seat and lap anchor visually.

A targeted Play Mode run also passed with one 100-point ledger event crossing 66, 33, and 0 at once: the first and last dialogue shots advanced line by line, the middle sequence skipped, and the ending reached the victory screen.

The stage scene objects are in **Apartment Stage Content**. Open that hierarchy to edit room trigger volumes, interaction anchors, dialogue shots and lines, timer/dialogue/vignette/victory UI, mist, and the sofa ending anchors. The old generated HUD retains its existing content, while the duplicate pixel compositor on it is disabled so the authored CameraOutputCompositor drives both outputs.

Validated with Unity **6000.3.11f1**, using an isolated copy of this project so the open working Editor was not interrupted.

## October 2026 follow-up

The latest gameplay edit adds a four-block Zoomies meter on the Apartment HUD, a Shift/left-bumper power dash with stronger attributed hits, Xbox mappings for movement, look, paw, Mark Territory, jump, pause, dialogue, and menu navigation, plus authored bathroom-door and PancakeTower broken roots. The pause title/body are now left to the scene instead of rewritten every frame. These edits have not yet had a Unity Play Mode or WebGL build pass.

`MischiefProp` now assigns both `Default` and `Smashable` rendering layers to intact renderers at startup. The scene's 41 Smashable-only renderer overrides were changed from mask 256 to 257, so clearing the outline on a nonbreakable prop leaves a visible Default layer. The extra release impulse for kinematic props also respects Launch Multiplier. This follow-up has not had a Unity Play Mode pass.

The Menace Meter now interpolates from red to green as its fill decreases, pulses on each reward, and flashes at 66, 33, and 0. Colors, pulse scale, and timings are editable on the Apartment `HudView`. This follow-up has not had a Unity Play Mode pass.

Successful `MischiefProp` paw and Zoomie hits now receive a short pre-impact slowdown and near-freeze hit-stop, timed with unscaled time. Settings are in `Feedback.asset`. This follow-up has not had a Unity Play Mode pass.

The apartment and Mechanics Lab scene HUD text was converted to TextMesh Pro. The apartment scene now has four serialized owner roam anchors, four paintings that become physical on paw contact, a Mark Territory screen prompt, and readable WorldUI labels. The authored timer text was moved above and within the existing `Timer` image. The owner sit and cat lap transforms remain editable under `Apartment Stage Content > Owner sitting on sofa`.

The isolated Unity editor imported and saved both updated scenes without C# compile errors. This follow-up did not run the test suites or a visual Game-view pass; the earlier counts below are historical results from before these changes.

The October 18 gameplay follow-up clears the bedroom panel colliders when the intro opens it, places generated NavMeshLink endpoints on sampled NavMesh positions, and assigns the owner a looping Humanoid Macarena clip with a 30% destination-pause chance. Mark Territory now requires a 1.4-second hold and has a progress bar plus green particles and a light camera bump. The stage dialogue output is assigned to the authored `Owner line` TMP object, and the cat scales to 62% at the ending by default. Edit the three stage `Cutscene Sequence` components' **Shots > Caption** fields to author custom lines; keep **Wait For Advance** on for step-by-step playback.

## Automated tests

**10/10 Edit Mode and 17/17 Play Mode tests pass.**

| Area | Verified behavior |
| --- | --- |
| Rules | Charge duration/clamping/cancellation, independent control leases, player attribution eligibility, reward deduplication, single completion notification |
| Movement | Acceleration/braking, sprint wall collision, buffered and coyote jumps, low ceilings, walkable slope, table-height jump, falling out of bounds |
| Swats | Charge increases force, compound collider gets one strike, walls block targets, pause and narrative takeover cancel charges |
| Destruction | Direct and collision-triggered fracture, collision-chain attribution, ambient breaks give no rewards, fragment attribution, 128-fragment budget |
| Owner | Humanoid animation state advances, agent wanders and drives the speed parameter, repeated knockdown/recovery, duplicate downed hits do not reward, off-NavMesh fallback is clear |
| Camera | Intro/skip, independently owned locks, pause freezes shot time, lost pose/look target returns control, pixel texture resizing and point filtering |
| Loop | Apartment has at least 130 break-only agro, props alone complete the ending, scene restart restores rewards and intact props |

The Play Mode suite loads the actual generated scenes and prefabs. It does not rely on substitute runtime implementations. The owner wandering test allows its authored idle pauses and still requires movement before a fixed deadline.

## Desktop result

The macOS development build at **Builds/ScaredyCat.app** passed the automated session on an **Apple M1 MacBook Air** at **1280 × 720**, with a 288-pixel world and a 60 FPS cap:

- Owner navigation, zero-agro ending, completion menu, and replay through an Input System mouse click all passed.
- All **14 breakables** remained intact after initial scene settling. Peak active debris was **128**.
- **392 sampled frames** during destruction, ragdoll activity, and the ending: **16.92 ms average**, **17.05 ms 95th percentile**, **66.14 ms worst frame**. Screenshot capture is included in the sample; this is a short development-build measurement, not an uncapped throughput benchmark.
- **Zero gameplay errors**, with no missing-NavMesh or uninitialized-Animator warnings in the final player log.
- Intro, first-person paw, acceptance tableau, pause, and completion screenshots were visually inspected. HUD elements remain sharp while the world and paw share the pixel treatment.

Raw XML results, the desktop JSON report, screenshots, and build/player logs are saved locally under **Logs/FirstPlayable**. Builds and logs are intentionally excluded from Git. Generated scene/prefab references and metadata were also checked for missing GUIDs: none were found.

## Reproduction

Use **Window > General > Test Runner**, or run the Unity executable with these arguments (omit `-quit`; the runner exits itself):

```text
-batchmode -nographics -projectPath <project> -runTests -testPlatform EditMode -assemblyNames ScaredyCat.EditModeTests -testResults <results.xml>
-batchmode -projectPath <project> -runTests -testPlatform PlayMode -assemblyNames ScaredyCat.PlayModeTests -testResults <results.xml>
```

Play Mode tests need graphics initialization for the RenderTexture assertions. Only one Editor process may open a given project at a time.

The opt-in standalone smoke runner used for the original first-playable pass has been removed. The recorded run above remains as a historical baseline; use the Edit Mode and Play Mode suites for current gameplay checks.

The generated assets include two 12-fragment vase variants, a movable book, a jointed owner, the player prefab, baked navigation for both scenes, four tuning assets, and the three editable narrative sequences. Apartment offers **188 potential prop agro** (14 breakables and two books), including **140 from breaks alone**. Not every break also earns a knock; each outcome has independent eligibility.

The walk cycle remains a provisional trim of the supplied Mixamo clip; sounds are replaceable synthesized placeholders. Automated trajectories and frame sampling do not replace a human pass for preferred movement, animation, and audio feel.
