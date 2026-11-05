Latest traffic formation-gap fix: [TrafficGapValidation.md](TrafficGapValidation.md).

Latest storm/fast-traffic update: [StormPaceValidation.md](StormPaceValidation.md).

Latest authored player-lamp correction: [PlayerLampValidation.md](PlayerLampValidation.md).

Latest hold-to-look-back validation: [LookBackValidation.md](LookBackValidation.md).

Latest manual shifting and recorded gear/gas audio validation: [ManualGearValidation.md](ManualGearValidation.md).

Latest 300km/h acceleration and high-speed validation: [AccelerationValidation.md](AccelerationValidation.md).

Latest density correction and moving-player verification: [TrafficDensityValidation.md](TrafficDensityValidation.md).

Current traffic behavior/spawn validation: [TrafficUpgradeValidation.md](TrafficUpgradeValidation.md).

# First playable validation

For the current manual-throttle, recorded-audio and 2018 traffic update, see [DriveUpgradeValidation.md](DriveUpgradeValidation.md). The sections below retain the earlier playable/night-pass history.

Date: 2026-11-03. Scope: the initial HW2 implementation, Windows desktop, Unity 6000.6.7f1, URP 17.6.0, New Input System only.

## Baseline

The original HW2 folder had an assignment PDF, vehicle source assets and an external music library, with no existing Unity project or tests. The project was created using the Universal 3D template. No previous HW2 code or scenes were replaced.

## Automated checks

Five EditMode tests passed: reverse/revisited-road scoring, fuel depletion/refill limits, three-hit lives, difficulty bounds and restart reset.

The final PlayMode run passed **3/3**, with zero failed/skipped tests, in 13.93 seconds. Raw results are in `Validation/playmode-results.json`.

- Selection, ignition, actual New Input System keyboard events, steering, reverse speed/camera/screen-relative controls, pause/resume, bounded pool reuse and restart.
- Collision cooldown, three-hit Game Over, PlayerPrefs high score and returning to menu.
- Music folder discovery, category list, successful MP3 playback, track switching, pause/resume, rain and fog zones.

Tests snapshot and restore the high-score preference. The keyboard test explicitly routes its synthetic device to the Game view while testing, then restores the prior editor input setting.

The initial runtime run found a MaterialPropertyBlock created in a MonoBehaviour field initializer. It was moved into explicit initialization. A camera reference lost during a development hot reload was resolved by restarting Play Mode with proper initialization. Initial car normalization mixed model-local rotations with world-space bounds; the authoring repair now computes mesh bounds directly and normalizes in world space. All five models now have a 4.45 m longitudinal length and plausible width/height.

## Assets and settings

- One enabled build scene: `Assets/Game/Scenes/AfterHours.unity`.
- Serialized catalog references five player cars, both traffic types, both pickup types, road segment, environment variants, Input Actions, particles and six sound effects.
- `activeInputHandler: 1` confirms exclusive New Input System.
- Runtime UI has one EventSystem and InputSystemUIInputModule.
- Asset metadata audit: **790 identifiers**, zero duplicate GUIDs and zero missing meta files, excluding diagnostic screenshots.
- Custom night-sky shader reported supported with zero shader compiler messages in the Editor.
- Roads, scenery and interaction objects are prewarmed; the runtime recycling test confirms pool capacity stays constant when the road window moves.

## Visual review

Reviewed the actual composited Game view at 16:9, including car selection, car materials, road surroundings, driving HUD and radio. Fixed nested HUD anchors for the fuel bar, track progress and spectrum. The radio follows a camera-relative point beside the car in both travel directions. Text uses an overflow mode supported by the bundled font.

## Windows build and launch

Windows x86_64 build completed successfully through `AfterHoursBuilder.BuildWindows`. The builder copies the categorized MP3 library beside the executable. Original FLACs/backups remain outside the game build.

The first standalone launch created a Direct3D 12 device on the local NVIDIA RTX 4050 and remained running/responding. Its log contained no gameplay exceptions or shader errors. It revealed font ellipsis fallback warnings, corrected by explicitly using the supported text truncation mode before the final build.

The final Windows rebuild also succeeded after the radio placement/text changes. Its standalone launch remained running/responding and its startup log contained no gameplay exceptions, shader errors or font ellipsis warnings. The final build's music folder contains all **27 MP3s** present at handoff, verified byte-for-byte against the source library. The source library gained a track during implementation; discovery/copying includes it automatically. Both smoke-test player processes were stopped after validation.

Reviewed final captures under `Images/`: menu, driving, rain and reverse. Preview positions/weather/reverse state were staged for visual review; the separate PlayMode suite establishes keyboard behavior. The Editor was returned to Edit Mode and preview-generated high scores were cleared before handoff.

## Limitations

- This is a first playable, not a final balance/art pass. Longer play sessions, camera comfort and traffic difficulty need player feedback.
- Automated controls cover keyboard/mouse input infrastructure. Gamepad bindings exist but have not been tested on physical hardware.
- Standalone launch smoke testing complements the Editor integration suite; it does not establish behavior on other PCs.
- The Editor logged Burst compiler server pipe errors during startup/recompilation. These are an unresolved local tooling issue separate from first-party C# compilation; subsequent runtime tests and both Windows builds succeeded. The standalone startup log has no corresponding error. Historical diagnostics remain in ignored local logs.
- No remote repository has been configured or published. No LMS upload, final report or submission ZIP has been prepared.
- Exact vehicle publisher/license details were absent from the supplied asset folder and remain recorded in CREDITS.md.
- Weapons are deferred by design.

## Night visual pass — 2026-11-03

Baseline commit: `1a99c98`. Scope: moon visibility, working distance fog, fuller scenery and lights on all five cars. No input bindings or gameplay control implementation changed.

Unity compiled the changed runtime/editor/test assemblies. Both custom shaders reported supported. Asset metadata audit: **809 identifiers**, no duplicate GUIDs and no missing meta files, excluding diagnostic screenshots. Serialized startup scene and Input Actions are retained; the temporary preloaded-assets setting change was restored to the baseline.

A full PlayMode run passed **5/5** with zero failed/skipped tests in **17.90 seconds** (`Validation/night-visual-results.json`). It includes the prior driving/music/damage checks plus all-car lamp configuration/emission, damage-flash isolation, reverse lamps, early fog state/shader globals and denser scenery recycling.

After the final lamp-position adjustments, the focused all-car lighting/damage/reverse check passed again, **1/1 in 5.74 seconds** (`Validation/night-visual-lights-final.json`).

Earlier full runs intermittently missed simulated Enter/R presses (4/5, then 3/5 during cleanup validation). The driving-flow check also passed in isolation (8.26 seconds). Longer key timing alone still produced 4/5. Restricting the cloned gameplay Action Asset to its synthetic keyboard produced a 5/5 run in 18.66 seconds, followed by another 3/5 run; this did not resolve the focus-related failure. Those results remain in the validation JSON files.

The installed Input System's InputSettings documentation explicitly states that AllDeviceInputAlwaysGoesToGameView still respects backgroundBehavior and can disable devices based on focus. The fixture was setting routing but leaving that background rule active while Pipeline drove an unfocused Editor. Setup now temporarily uses IgnoreFocus, asserts the synthetic keyboard is enabled, lets initialization settle, holds key states for 80 ms and isolates its gameplay Action Asset to the synthetic keyboard. Teardown restores both routing and background behavior. Production input code, project settings and bindings are unchanged.

With that focus handling corrected, **two consecutive full suites passed 5/5**, in **18.73 seconds** and **18.34 seconds**, with no failed/skipped tests (`Validation/night-visual-background-input.json` and `Validation/night-visual-background-repeat.json`). After the final suite, the Editor was in Edit Mode, the original high score was preserved, and routing/background settings were restored to PointersAndKeyboardsRespectGameViewFocus / ResetAndDisableNonBackgroundDevices.

Reviewed front/rear close-ups of every car. The review found and fixed disabled lamp emission material variants and corrected two cars' lamp positions to their body fixtures. Reviewed the composited driving HUD/moon/town capture and same-camera forest clear/fog captures at 650 m. Dense fog was approximately 0.889 blend / 0.01895 density; it visibly obscured distant road/trees while keeping the nearby car readable. Captures are staged, including removal of nearby traffic for an unobstructed scenery view. Integration tests separately exercise real keyboard actions. See `Images/night-upgrade.png`, `Images/lights-five-cars.png`, `Images/fog-clear.png` and `Images/fog-dense.png`.

The local Editor still logs the pre-existing Burst compiler pipe failures. Its license-service reconnect dialog was recovered during reopening. These tooling issues are recorded separately from the successful C# compilation and runtime checks. Computer use was stopped by the user's Escape key; no further desktop automation was used.

Play Mode was exited and the high-score preference captured before this validation session was restored after the staged drive. Final cleanup revealed that GameManager.OnApplicationQuit could resave the test run after test teardown restored the preference. Teardown now returns to the menu and resets the test run before restoring preferences. The post-suite check confirms the original score remains preserved after Play Mode exits. These final changes affect the test assembly only; the built player uses the same validated production assets/code. Longer-session performance and testing on other PCs remain outside this visual pass.

The updated Windows x86_64 build succeeded in **225.98 seconds**. BuildReport reports **0 errors, 1 warning**, with a 111,876,274-byte player before the external music library. The warning is the bundled TextMeshPro/Mobile/Distance Field shader's deprecated `enable_d3d11_debug_symbols` directive; vendor shader files were not changed. Raw summary: `Validation/night-visual-build.json`.

The Editor also emitted one native assertion, `Callback unregistration failed. Callback not registered.`, from `BuildPipeline.BuildPlayer` during the successful build. Its precise cause is unresolved; it is separate from the BuildReport error count. The Console summary preserves this alongside the existing Burst failures. No gameplay exceptions or shader errors appeared in the standalone startup log. The Windows player launched on Direct3D 12, remained running/responding, and was stopped after the smoke check. This is startup coverage; the visual scenarios and keyboard interactions were verified in the Editor, not by controlling the standalone window.

All **27** built MP3 files matched the source library by SHA-256, with no missing/mismatched files. The refreshed executable is `../Build/AfterHours/AfterHours.exe`. Status: ready with the documented Editor tooling limitations.
