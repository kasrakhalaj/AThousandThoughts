# Traffic density and visible AI correction

Date: 2026-11-04, Tehran. Baseline commit: `8827415`. Unity 6000.6.7f1, Windows x86_64, URP, New Input System.

## Report and cause

The user reported too few cars and that the improved AI appeared absent. Inspection confirmed MovingCar.Behavior exists in the loaded Editor assembly and TryChooseTrafficLane exists in the built managed assembly. No standalone was running during inspection, so the exact executable previously played by the user remains unknown.

A 65-second held-gas drive reproduced the sparse encounters: normal sections averaged 0.72 cars ahead within 180m; busy 0.90, sparse 0.81. Thirty of 54 non-quiet one-second samples had no car ahead in that range. The global budget counted distant vehicles across the whole streamed road; replenishment was slow and mostly behind the faster player. Initial cars were too dispersed, and quiet sections occupied one quarter of the route (`Validation/density-before.json`). Earlier stopped-player tests did not expose this adequately.

## Correction

- Budget traffic in the encounter area: 300m ahead/100m behind in the selected travel direction. Sparse/normal/busy targets are 7/12/16 plus progressive difficulty. Global traffic remains bounded at 42 active cars with the existing 264-object pool.
- Seed 2-3 staggered candidates per non-quiet section before ignition, retaining spacing and an escape route. Replenish at approximately 2.5/1.4/0.8s sparse/normal/busy intervals with jitter/difficulty; favor traffic ahead. Passed, hidden vehicles recycle earlier.
- Fully opaque fog can hide a spawn inside the frustum. The calculation conservatively follows RoadFog's squared exponential at the top of a 2m car, with less than 0.01% vehicle color transmitted. Clear-road frustum checks, closing-time safety, occupancy/reservations and escape checks remain in force. No car is forced into an unsafe visible gap.
- Quiet stretches now cover 3/24 sections rather than 6/24; busy covers 6/24. Quiet roads stop replenishment and drain hidden traffic, while already-visible cars continue naturally.
- Dense-traffic validation exposed over-restrictive passing: a far-away merge was reserving an empty escape lane for a player well behind it. This protection is now limited to the player's actual closing-time reaction corridor. AI followers can react immediately to reservations, while player checks retain the longer prediction margin. Passing benefit scales with weather/profile, allowing useful overtakes at reduced fog/rain speeds.

## Validation method

`Tools/DriveTrafficProbe.cs` runs outside Assets through Pipeline script run. It holds W through a synthetic New Input System keyboard, uses simple adjacent-lane obstacle avoidance, samples actual player-loop movement every simulated second and restores input routing/background settings, device filters, random state, time scale and high score. Runs use seed 4821 and time scale 2, with a 65-second limit or game over. The probe is not a human skill or survivability test. Counts are cars 8-180m ahead and within 180m nearby, plus one-second near-player merge observations. Scene camera and weather advance normally; no traffic is repositioned during a drive. An automatic screenshot at 12 seconds captures actual driving.

Density-only intermediate run: normal averaged 4.56 cars ahead; busy 6.69, sparse 5.20; no non-quiet sample was empty. It ended in game over after about 59 seconds using the simple avoidance driver. No near-player merge was sampled, motivating the passing correction above (`Validation/density-after.json`). Final results follow below.

## Final moving-player result

The final drive sampled 53 seconds before the simple avoidance driver reached game over. Normal sections averaged **5.23 cars ahead**, busy **7.58**, sparse **4.89**. All 47 non-quiet samples contained traffic ahead. Near-player signaling/changing-lane states appeared in **12 non-quiet samples**, compared with 3/54 in the baseline and none in the density-only intermediate run. These are one-second observations, not counts of unique overtakes. Quiet-section samples still contained previously visible traffic; the short quiet sections do not guarantee instantaneous emptiness (`Validation/density-final.json`).

The actual 12-second driving capture shows approximately 111 km/h, three lives, nearby traffic in multiple lanes and more cars ahead (`Images/density-driving.png`). It was captured automatically from the real player loop with held W, not a staged car lineup. Before/after runs followed the same route/start/seed and input policy; steering and survival diverge with changed traffic. This supports encounter density and visible AI activity, not a guarantee of collision-free autoplay or final difficulty balance.

The first final-run attempt encountered an intermediate C# local-name conflict; compilation was corrected before running the successful final drive. One diagnostic-probe attempt ran without an initialized manager and was cleaned up; the retained utility now rejects that setup before changing input state. These were development/validation failures, not suppressed gameplay exceptions.

A full regression run caught a nearby second driver accepting a gap after the first signaling driver slowed down (12/13 passed). The signaled maneuver now owns a minimum 24m reservation regardless of its current speed, retaining the earlier reservation guarantee (`Validation/density-playmode-reservation-failure.json`). The snapshot immediately above predates that final safety refinement; the repeated drive and final suite below verify the delivered version.

Delivered-code repeated drive after the reservation fix: 42 one-second samples, ending in game over at about 42 seconds using the simple avoidance driver. Normal averaged **6.24 cars ahead**, busy **7.33**, sparse **5.12**; all 37 non-quiet samples contained traffic ahead, with signaling/changing-lane states in **11** of those samples (`Validation/density-verified.json`). The screenshot was refreshed by this drive. These runs demonstrate the denser and active traffic; difficulty balance remains subject to real player feedback and use of braking.

## Automated final checks

- Editor compilation completed with compilationFailed=false and no C# errors.
- EditMode: **11/11 passed**, 0 failed/skipped, **0.23s** (`Validation/density-editmode-final.json`).
- Final PlayMode: **13/13 passed**, 0 failed/skipped, **64.95s** (`Validation/density-playmode-final.json`). This includes the existing controls/audio/weather/pool/lives tests, the new early-acceleration density check, a distant-crowded-pass regression, nearby merge reservation/cancellation/completion, and reverse-camera spawn checks. Total: **24 tests passed**.
- No input bindings, scenes, prefabs, package manifests or Project Settings were changed.

## Windows handoff

Final Windows x86_64 BuildReport: **Succeeded, 0 errors, 0 warnings, 63.75s**, 114,672,442 bytes before music (`Validation/density-build.json`). Burst remained enabled. The delivered assembly contains both the new nearby-budget accessor and fog-spawn rule; its SHA-256 is recorded in `Validation/density-build-identity.json`. A BUILD-INFO.txt beside the executable identifies the density correction.

The fresh executable launched and remained responding, with a 37-line Direct3D 12 startup log and no matching exceptions/missing-reference/shader-error lines (`Validation/density-startup.json`). Standalone coverage is startup; driving and AI scenarios were exercised in the Editor. The owned startup-check process was stopped afterward. All 27 built songs match the source library (`Validation/density-music.json`).

Editor returned to Edit Mode; original score 19 and original input routing/background settings were verified (`Validation/density-cleanup.json`). Final Console ground truth reports compilationFailed=false, 0 current errors and 0 warnings (`Validation/density-console.json`); the historical capture still contains the known Burst tooling and intermediate development failures.

Artifact: `../Build/AfterHours/AfterHours.exe`. Ready for local Windows play. The denser traffic is intentionally more challenging; human balance feedback, other PCs and formal performance profiling remain unverified.
