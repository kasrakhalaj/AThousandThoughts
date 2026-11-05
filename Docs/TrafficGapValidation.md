# Traffic formation gaps

Date: 2026-11-05 (Tehran). Baseline: 430c469, clean working tree. Unity 6000.6.7f1, Windows x86_64, URP, existing New Input System.

## Symptom and confirmed cause

The player can encounter three cars abreast with no usable lane. Existing RoadWorld.HasEscapeLane checked a narrow 22m slice only at spawning/merge decisions. MovingCar then followed traffic only in its own occupied lanes, so cars with different speeds could converge into a full-width row after being accepted. Distant merges also bypassed the escape check without forecasting when the player would reach them.

Three controlled PlayMode reproductions failed before the production fix (`Validation/three-lane-before.json`, 0/3): a faster third car catches a two-car row, three independently spaced speeds converge together, and a hidden spawn closes the last lane shortly afterward. Scenarios use existing moving-car pools and real Tick/Spawn APIs; no private speed/state fields are injected.

## Correction

TrafficRules supplies a bounded 24–90m stagger distance based on the actual player/row closing speed, allowing two lane changes plus reaction time. Placement and near-player merge checks project traffic 4s ahead in 0.5s steps and include current/reserved lane occupancy. Distant merges that can complete before the player reaches them remain allowed.

RoadWorld also checks formations continuously during driving. The trailing participant in a projected full-width row yields longitudinally, retaining a staggered gap. A stable traffic-list tie order prevents all cars in a level row from braking together. Virtual lead positions anticipate two other cars converging in the future; simply checking already-aligned pairs would brake too late. Existing same-lane following, brake limits, signals, weather and fast-driver pace remain in force. Visible cars are not deleted or teleported to open a gap.

A fixed 96-entry snapshot is refreshed once per world tick for the forecast calculations; ordinary gameplay limits active traffic to 42. Direct queries refresh it outside Tick so controlled scene/test changes do not use stale data. No per-frame forecast collections or native Transform calls inside the forecast pair loops are introduced. There are no changes to player controls, speed/gear tuning, input assets, prefab pools, weather profiles, packages or scenes.

## Verification

Before/after regression, nearby behavior, live traffic and Windows build evidence follows below. A forecast is a bounded constant-speed prediction, not a guarantee against driving into a known obstacle or every possible steering choice.

Initial corrected targeted run: **3/3 passed**, zero failed/skipped, 10.14s (`Validation/three-lane-targeted.json`). EditMode: **35/35 passed**, zero failed/skipped, 0.45s (`Validation/three-lane-editmode.json`). A further role check explicitly preserves the leading car's movement so a forecast cannot make an entire row brake together. Full regression results follow below.

Final full PlayMode regression: **23/23 passed**, zero failures/skips, 112.28s (`Validation/three-lane-playmode.json`). Includes all three wall reproductions, continued leading-car movement, original early density, distant safe overtakes, conflicting merge reservations, spawn visibility, reverse driving, storms, fast traffic, lights, gearbox and look-back. Combined with 35 EditMode checks: **58 unique checks passed**.

First natural drive (seed 4821) completed the full 65 simulated seconds still playing: 2360 monitored frames, **0** frames with a physical full-width row in the reaction zone, up to 25 active cars, AI peak 182.56km/h and four cars ahead at the 8.1s/9.2s samples (`Validation/three-lane-drive-4821.json`). Screenshot `Images/traffic-gaps.png` was visually inspected: traffic is staggered and a clear lane remains in the shown encounter.

The probe checks actual lateral collision widths, rather than reservation masks, and groups cars within 8m of a row anchor. It monitors rows within max(45m, 20m + 2 seconds of closing distance) on either side of the player. These measurements cover the tested runs, not every random traffic sequence or future player input.

Three live traffic seeds (4821, 9773, 24017): **6481 monitored frames, 0 blocked-row frames** under that definition. The first two completed the full 65 simulated seconds still playing; the third ended at GameOver from ordinary driving collisions. The feature preserves challenge and does not auto-steer the player. Per-run data: `Validation/three-lane-drive-4821.json`, `Validation/three-lane-drive-9773.json`, `Validation/three-lane-drive-24017.json`. Probe cleanup restored the existing score and input/background routing, reset optional seed/start/capture settings and exited Play Mode (`Validation/three-lane-cleanup.json`).

The user's existing Windows player was still running from `../Build/AfterHours/AfterHours.exe` (PID 36804). It was left running; the corrected standalone is built into `../Build/AfterHours-TrafficGaps/AfterHours.exe` with its own Music folder.

Windows build: **Succeeded**, 0 errors, 0 warnings, 62.16s, 115,038,986 bytes reported (`Validation/three-lane-build.json`). The fresh runtime assembly contains the formation/snapshot/stagger code and has a recorded SHA-256; all 27 MP3s are present with matching source file sizes (`Validation/three-lane-build-identity.json`). The new executable launched, initialized Direct3D 12, stayed responsive and logged no exception/missing-reference/shader/compiler-error matches (`Validation/three-lane-player-startup.json`). Only the owned smoke-test process in the new folder was stopped. Final script compilation reports completed without errors (`Validation/three-lane-final-compile.json`). The earlier Unity API compile error was corrected using stable traffic-list ordering; no warning/error was suppressed.
