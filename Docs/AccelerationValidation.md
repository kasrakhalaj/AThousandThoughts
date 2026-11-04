# Acceleration, 300 km/h and high-speed challenge

Date: 2026-11-04 (Tehran). Baseline: `cf6da8c`. Unity 6000.6.7f1, Windows x86_64, URP, exclusive New Input System.

## Intended behavior

W supplies gas, S brakes, A/D steer and R selects gear through the existing Input Actions. A new plain C# VehicleDynamics rule integrates throttle torque against rolling/aero resistance. Base torque falls from 5.6 to 1.6 m/s² through the speed range before drag; low-speed acceleration is stronger, and approaching the limit takes longer. Full forward speed is capped at 300 km/h (83.333m/s); reverse stays at 10m/s. Releasing gas coasts, the brake wins over gas and a direction change brakes through zero before powering the other way. Distance adds up to 12% acceleration power; it does not gate access to 300km/h.

The HUD displays real instantaneous acceleration in travel direction (positive speeding up, negative slowing down) in m/s² alongside speed and a 300km/h limit. Six simulated gears share their thresholds/progress with the recorded engine layers, avoiding the old repeated RPM resets beyond fifth gear. Camera FOV widens smoothly to a bounded 74 degrees. Camera, screen-space steering and the streamed road window follow actual travel direction while a gear change brakes through zero; selecting reverse at high forward speed does not turn the view away from upcoming traffic.

Harder high-speed traffic adds up to three nearby cars to the local budget, replenishes up to 15% faster, and counts five travel-seconds ahead with a 300m minimum. Fog-hidden spawning now chooses the greater of concealment distance and closing-time warning distance, preserving reaction time at 300km/h. Existing dense traffic, three lanes/lives, fuel, weather, signals and quiet sections remain integrated.

High-speed contacts use intersecting swept x/z time intervals for player/traffic motion. This replaces the old expanded final-distance threshold, which could report a collision before a fast car actually reached an obstacle on a dropped frame. Pickups use the same sweep. A test that advances just CarDriver on a clear road verifies reaching 300 and HUD/braking integration; it does not assert that full-gas traffic survival is automatic.

## Validation

EditMode: **20/20 passed**, zero failed/skipped, **0.05s** (`Validation/acceleration-editmode.json`). These include nine new acceleration/gear/frame-rate/high-speed sweep cases alongside the existing run and traffic safety rules. Integration/runtime/build evidence follows below. No input action IDs/bindings, scenes, prefabs, package manifests or Project Settings are intentionally changed.

PlayMode: **14/14 passed**, zero failed/skipped, **78.41s** (`Validation/acceleration-playmode-initial.json`). The new integration test uses a real held W action to power CarDriver through a clear component simulation, verifies 300km/h/D6 and the acceleration HUD, then holds gas+brake to confirm deceleration/brake priority. Previous traffic density, merge reservation/cancellation/completion, pause, reverse camera, music/audio, lights, lives and saved-score checks also pass. Total: **34 tests passed**.

Runtime moving-player preview: the held-gas avoidance driver reached approximately **271.8km/h** before game over after 44 one-second samples. The actual 12-second capture shows approximately 175km/h, D5, +2.9m/s² and nearby traffic (`Images/acceleration-driving.png`, `Validation/acceleration-drive.json`). This run includes impacts and is not the uninterrupted clear-road acceleration timing. A final reverse-camera correction is covered by the final integration rerun below; normal forward motion is unchanged by that correction.

Final PlayMode after the travel-direction camera/window/steering correction: **14/14 passed**, zero failed/skipped, **68.02s** (`Validation/acceleration-playmode-final.json`). The high-speed integration also selects R while still moving forward and verifies the view stays ahead; the existing reverse-driving tests verify the rear view after reversing. Total unique final checks remains **34** (20 EditMode + 14 PlayMode).

## Windows handoff

Final Windows x86_64 BuildReport: **Succeeded, 0 errors, 0 warnings, 55.36s**, 114,675,002 bytes before music (`Validation/acceleration-build.json`). Burst remained enabled; no package/settings workaround was applied. The delivered managed assembly contains VehicleDynamics, the acceleration HUD accessor and actual-direction view accessor; its SHA-256 is recorded in `Validation/acceleration-build-identity.json`. BUILD-INFO.txt beside the executable identifies this update.

The fresh executable launched, remained responding and produced a 37-line Direct3D 12 startup log with no matching exception/missing-reference/shader-error lines (`Validation/acceleration-startup.json`). The owned smoke-test process was stopped afterward. Standalone coverage is startup; gameplay and visual checks ran in the Editor. All 27 built MP3s match their source files (`Validation/acceleration-music.json`).

Editor returned to Edit Mode; the saved score and original input routing/background behavior were restored by the fixtures/probe and verified (`Validation/acceleration-cleanup.json`). Final Console ground truth reports compilationFailed=false, 0 current errors and 0 warnings (`Validation/acceleration-console.json`). The existing local Burst startup/pipe issue remains in historical logs. Unity's cold startup took about 453s; this delayed validation but did not require a settings/package change.

Artifact: `../Build/AfterHours/AfterHours.exe`. Ready for local Windows play. This is arcade kinematics with tuned acceleration/drag, not a make-specific tire/suspension simulation. Human difficulty balance, other PCs, physical gamepads and a formal performance budget remain unverified.
