# First playable verification

This is the verification baseline for the generated first playable. The authored apartment intro, owner activity trigger, and doorway NavMesh links have since changed and are not covered by this historical run.

Validated with Unity **6000.3.11f1**, using an isolated copy of this project so the open working Editor was not interrupted.

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
