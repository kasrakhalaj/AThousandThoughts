# Manual six-speed gearbox and recorded gear/gas audio

Date: 2026-11-04 (Tehran). Baseline: `4337608`. Unity 6000.6.7f1, Windows x86_64, URP, New Input System.

## Delivered behavior

The driver selects gears with E/right shoulder up and Q/left shoulder down. W gas, S brake, A/D lanes, R reverse and music/menu controls retain their existing bindings. ManualGearUpgrade adds only the two new actions/four bindings; the original maps, actions and binding IDs/content compare equal (`Validation/manual-input-preservation.json`). Asset GUIDs are preserved, and the startup scene is not rewritten.

ManualTransmission starts in first. An accepted request reserves the next gear for 0.36s, cuts drive torque through an automatic clutch, then commits it. Requests while shifting or in reverse are ignored; gears are bounded 1-6. A downshift that would exceed the next gear's redline is rejected without a fake successful shift sound. R resets forward gear selection to first. Pausing freezes the shift interval.

Per-gear redlines are 45/85/130/180/240/300km/h. Wrong high gears at low speed have reduced torque. A lower gear cannot accelerate indefinitely; its limiter cuts recorded engine playback/drive power. A rolling overspeed car coasts rather than being snapped down to a lower speed. Well-timed upshifts preserve the gradual 300km/h driving model. Engine RPM follows the selected gear; upshifts lower it and downshifts raise it. The HUD shows selected/pending gear, RPM/redline bar, up/down hints, lugging and rejected-downshift feedback.

## Real sound sources

- Mihacappy, 412_gearsw.wav, Freesound #871568: recorded IZ-412 gear changes, CC0.
- overmedium, v8 engine rev.wav, Freesound #651534: recorded throttle blips/idle, CC0.

Both official source/license pages were checked. Public HQ MP3 previews, HTML and download metadata are retained in `../SourceAudio/ManualGears`. Three short lever variants plus gas-on, gas-off and downshift-blip cues are prepared as mono 44.1kHz PCM16 with filtering, fades and peak-safe gain. Exact excerpts/source checksums are in ManualGearSoundSources.json; Tools/prepare_manual_gear_audio.py reproduces them. Existing field-recorded player engine layers continue underneath. These are stylized mixed car recordings, not make-matched engine simulations.

An accepted shift plays one lever cue; a downshift adds one rev-match blip. Gas attack/release cues are debounced, so held W does not replay a transient every frame. Dedicated sources pause/resume with gameplay and stop on menu/game over. Music remains independent. Existing collision/pickup effects retain their original implementation.

## Validation

EditMode: **25/25 passed**, 0 failed/skipped, **0.39s** (`Validation/manual-editmode.json`). Five new transmission tests cover request timing, bounds/reset/reverse, over-rev rejection, power-cut coasting, redlines/lugging and reaching 300 with actual selected gears. Existing vehicle/run/traffic rules also pass. Full integration/runtime/build evidence follows below.

PlayMode: **15/15 passed**, 0 failed/skipped, **87.34s** (`Validation/manual-playmode-initial.json`). The new keyboard integration verifies menu-disabled E, no automatic shift while W is held, the 45km/h first-gear limiter, one throttle attack for held W, E upshift/power-cut interval, pause/audio freeze and resume, selected gear/RPM HUD, a second upshift, Q over-rev rejection with no success cue, and a safe downshift with blip. The full-speed component test now explicitly requests appropriate gears and still reaches approximately 300km/h. Existing controls/reverse/camera/traffic/music/weather/lives/scoring tests also pass. Total: **40 tests**.

Live moving-player preview used actual W/E/Q actions from the avoidance probe; it sampled gears 1-5 and five accepted shift cues (including a downshift), ending in game over after about 25 samples. It is an activity/audio check, not proof of human difficulty balance or automatic survival. Dedicated source DSP taps measured non-silent peaks of approximately **0.0922 RMS** for the recorded lever and **0.0418 RMS** for pedal/blip output (`Validation/manual-drive.json`). These meter checks establish playback, not a subjective listening study. `Images/manual-gears.png` was visually reviewed: gear/RPM/redline meter and Q/E prompts are readable, with live traffic and the radio. The 300km/h/sixth-gear capability is established by the clear-component integration test and pure drivetrain test, not this traffic-survival run.

## Windows handoff

Final Windows x86_64 BuildReport: **Succeeded, 0 errors, 1 bundled TextMeshPro shader pragma deprecation warning**, **117.12s**, 115,003,178 bytes before music (`Validation/manual-build.json`, `manual-build-messages.json`). Vendor shader code was not edited; Burst stayed enabled. The delivered managed assembly contains ManualTransmission, the new shift input accessors and recorded cue fields; its SHA-256 is recorded in `manual-build-identity.json`. BUILD-INFO.txt beside the executable identifies the manual gearbox update.

The fresh executable launched, remained responding and produced a 37-line Direct3D 12 startup log with no matching exception/missing-reference/shader-error lines (`Validation/manual-startup.json`). The owned smoke-test process was stopped afterward. Standalone validation covers startup; shifting/audio/UI scenarios ran in the Editor. All 27 built MP3s match the source library (`manual-music.json`). New source/asset metadata has no duplicate GUIDs or missing .meta entries (`manual-assets.json`).

The Editor returned to Edit Mode; original score 19 and original input routing/background behavior were verified (`manual-cleanup.json`). The existing New Input System, prior bindings, startup scene, Project Settings and package manifests are retained. Final Console ground truth is recorded in `manual-console.json`; historical local Burst startup/pipe errors remain in the log.

Artifact: `../Build/AfterHours/AfterHours.exe`. Ready for local Windows play. Human difficulty/audio taste, other PCs, physical gamepad operation and formal performance profiling remain unverified. The gear/pedal sources are real edited recordings; pitch/volume/rev-limiter control is stylized game audio.
