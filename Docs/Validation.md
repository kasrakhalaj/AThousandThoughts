# First playable validation

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
