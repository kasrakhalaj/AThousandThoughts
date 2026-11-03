# Traffic behavior and spawning validation

Date: 2026-11-03. Baseline: `02d409b`. Unity 6000.6.7f1, Windows x86_64, URP, exclusive New Input System.

## Behavior

RoadWorld keeps the existing 17 road chunks and 264 pooled interactables (48 stopped cars, 48 moving cars, 144 coins, 24 fuel). Queue reuse cycles through all seven existing moving models. No scene, prefab, input action, package or Project Settings changes are required.

A route cycle contains sparse, normal, busy and six quiet sections. Quiet locations vary by cycle and work at negative road coordinates too. Spawn intervals are jittered (approximately 7/4.5/2.6 seconds before difficulty adjustment); traffic budgets grow continuously with difficulty instead of adding obstacle rows at thresholds. Cars are staggered independently of coins. Rare stopped hazards remain.

Except for initial population before ignition, a spawn must be outside the current camera, within the streamed road, outside quiet road, clear of occupied/reserved lanes, at least 75 metres and four closing-seconds from the player, and preserve an escape lane reachable from the player's lane. A failed attempt is skipped. Rear arrivals retain varied cruising speeds even when the player is stationary; faster-player overtakers are bounded at 38 m/s. The road window favors the selected viewing/travel direction, including reverse. Quiet sections stop new traffic and retire hidden cars; visible cars clear naturally, so quiet does not promise instant emptiness at its boundary.

MovingCar exposes cruising, following, signaling, changing-lane and recovering states. Drivers pass slower traffic when the adjacent lane offers sufficient benefit. Cautious drivers can return right; balanced/assertive drivers otherwise hold their lane. Profiles vary headway and signal duration. Lane checks account for the player's current and committed lane, signed closing speeds, vehicles ahead/behind, and other merge reservations. Signaling rechecks/cancels when the gap is lost. During the crossing, both occupied lanes are sensed for longitudinal braking; cars finish their reserved crossing instead of abruptly snapping sideways. A cooldown limits repeated lane changes.

Rain and fog jointly reduce desired speed by up to 41% and increase following headway by up to 85%. Player controls, headlights, recorded audio, fuel/lives, music and scoring remain integrated with the existing architecture.

## Evidence

- Editor compilation: completed with no C# errors.
- EditMode: 10/10 passed, zero failed/skipped, 0.24 seconds (`Validation/traffic-editmode.json`). Covers existing run rules plus signed closing gaps, reverse spawn reaction time, stopping/following/weather calculations, and route pacing across positive/negative cycles.
- Initial PlayMode: 6/11 passed (`Validation/traffic-playmode-initial.json`). Four new scenario setups requested a spawn inside quiet road and were corrected to use non-quiet hidden road; one older audio assertion selected a car that could leave the pool window and was narrowed to a nearby moving car. The assertions were retained.
- Second PlayMode: 11/11 passed in 48.93 seconds (`Validation/traffic-playmode-second.json`). Includes full overtake completion, competing reservations, cancellation for the player, repeated activation reset, visible/overlapping/full-road spawn rejection, reverse-camera rejection/hidden acceptance, quiet-road draining without visible disappearance, weather spacing, and the previous driving/audio/UI/music/lives/persistence regressions.
- Runtime inspection found rear replenishment clamped all stationary-player arrivals to 20 m/s. Replenishment now keeps a random road speed and applies a catch-up minimum only when needed. Unconditional right-lane returns were also restricted to cautious drivers to avoid every driver forming one procession. Final validation follows below.

Manual previews use staged road positions. `Images/traffic-ai.png` shows the actual composited game view at a stationary player on a normal-flow road. Frame/time snapshots establish that the game was advancing; staged captures are not evidence that every traffic encounter is naturally generated. The original saved high score is restored after runtime work. Extended player balancing and a formal CPU/frame-time budget remain outside this pass.

## Final result

- Final PlayMode after the live-run refinements: **11/11 passed**, zero failed/skipped, **49.00 seconds** (`Validation/traffic-playmode-final.json`). Together with EditMode, **21 tests passed**.
- Final live run: frame 17/time 0.28 at setup to frame 4068/time 131.10; the preview used time scale 3 after staging a stationary player at z=350. The final snapshot shows nine cars spanning all three lanes, approximately 13.0-28.1 m/s, cruising/recovery/lane-changing states, all three driver styles, three player lives, and the same 264-object pool (`Validation/traffic-runtime-final.json`). `Images/traffic-ai-final.png` was visually reviewed. This is a stationary-player scenario, not a full-speed endurance test.
- Windows x86_64 build: **Succeeded, 0 errors, 0 warnings, 41.79 seconds**, 114,671,418 bytes before music (`Validation/traffic-build.json`). Burst remained enabled; no package/settings workaround was applied.
- The updated standalone launched on Direct3D 12, remained responding and produced a 37-line startup log with no matching exception/missing-reference/shader-error lines (`Validation/traffic-startup.json`). The log retains a D3D12 info-queue-interface query message; startup and rendering continued. The owned smoke-test process was stopped afterward. Standalone coverage is startup; gameplay scenarios ran in the Editor.
- All **27 built MP3s** match the external source library by SHA-256 (`Validation/traffic-music.json`). Asset audit: **936 GUIDs**, no duplicates or missing metadata (`Validation/traffic-assets.json`).
- Editor is back in Edit Mode. Original high score **19** was restored and verified (`Validation/traffic-cleanup.json`). Input bindings, scenes, prefabs, Project Settings and package manifests have no diff.
- Historical Console capture retains the known Burst pipe/startup tooling errors and two transient C# diagnostics from the intermediate interval between writing MovingCar and RoadWorld. Final compilation/build succeeded; Console ground truth reports compilationFailed=false, 0 current errors and 0 warnings (`Validation/traffic-console.json`). No unresolved gameplay errors were found.

Artifact: `../Build/AfterHours/AfterHours.exe`. Ready for local Windows play, with extended balance/player feedback, other computers and formal performance profiling still unverified.
