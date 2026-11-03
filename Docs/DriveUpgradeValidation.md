# Manual gas, recorded audio and 2018 traffic validation

Date: 2026-11-03. Baseline: `4615053`. Unity 6000.6.7f1, Windows x86_64, URP and exclusive New Input System.

## Delivered behavior

W supplies gas, S brakes and A/D steer. The existing Forward action supplies throttle; no Input Actions IDs or bindings were replaced. Continuous pedals recognize an already-held key when driving is enabled after ignition/pause. Ignition leaves the player stationary without gas. Releasing gas coasts. R selects gear, and S always brakes, including when W is also held. The dashboard shows actual rounded km/h, a speed bar, gear and gas/coast/brake state. The forward speed limit rises from 30 to 44 m/s with run difficulty.

Normal AI traffic uses all seven supplied Quaternius 2018 models. Speeds start at 13–26 m/s and increase with difficulty. Following behavior brakes for slower traffic and the player; periodic overtakers replenish traffic around a stopped/slow player. Wheels rotate with travel. Rare hazard-lit stopped cars preserve the assignment's stationary obstacles. All road/traffic/pickup pools remain bounded; total interactable capacity stays 264.

Player ignition and three engine layers, AI engines and rain use edited CC0 field recordings. The player has a separate, layered recording; nearby AI uses a quieter spatial source with distance fade. Music controls remain independent. Source/license/excerpt information is in CREDITS.md and RecordedSoundSources.json. No exact engine-to-vehicle-make match is claimed.

## Automated validation

- EditMode: **5/5 passed**, no failed/skipped tests, **0.17 s** (`Validation/drive-editmode.json`). Checks cover scoring, fuel, lives, difficulty limits and reset.
- Initial PlayMode: **6/6 passed**, **29.93 s** (`Validation/drive-playmode.json`).
- Final PlayMode after the held-pedal initialization and brake-priority checks: **7/7 passed**, no failed/skipped tests, **39.27 s** (`Validation/drive-playmode-final.json`).

The integration suite exercises actual New Input System keyboard events: ignition with/without held W, acceleration, coasting, braking (including simultaneous gas/brake), speed display, steering, reverse/camera behavior, pause/resume, pool reuse, restart, damage/lives/high score, music/weather, all-car lamp emission, moving traffic, real-clip configuration, engine playback and traffic audio pause/resume. The fixture isolates its keyboard, handles the unfocused Editor and restores input settings and the saved high score.

Asset audit: **885 identifiers**, no duplicate GUIDs, no missing metadata. All seven copied traffic FBXs match their supplied originals by SHA-256 (`Validation/drive-assets.json`). Project Settings and package manifests are unchanged.

## Runtime and visual review

Reviewed front/rear views of all seven traffic models, including authored lamp shapes, body colors, orientation and scale (`Images/traffic-2018.png`). The authoring tool uses a preview scene and preserves the startup scene. Source FBXs are unchanged.

Live control snapshots showed 0 km/h after ignition without gas, approximately **108.7 km/h** while holding W (HUD 109), **96.4 km/h** after release (HUD 97), and braking toward a stop with S (`Validation/drive-preview.json`). These are sequential snapshots, not a precise timing benchmark. The HUD refreshes at 10 Hz, so it can trail instantaneous speed during hard braking. Reviewed the actual composited gas/brake/rain views. The heavy-rain/fog view was staged by moving to a weather zone; model lineup previews were also staged. Those captures do not replace the keyboard integration checks.

Live DSP output sampling confirmed non-silent recorded player idle (approximately **0.0258 RMS** at gain .22) and rainfall (**0.0175 RMS** at gain .18). A staged nearby moving AI source measured **0.00384 RMS** at about 14.9 m, gain .085 and spatial blend 1 (`Validation/drive-traffic-audio-near.json`); its far sample at about 217 m was silent, consistent with its 42 m distance fade. Output taps were primed before the final readings. Early snapshots sampling only the idle layer can be zero while another layer is active or before readback is primed. These meter checks establish playback/relative level, not a subjective listening study.

All preview objects/devices were removed on leaving Play Mode. The original input routing/background behavior and high-score preference were restored. The Editor was returned to Edit Mode.

## Windows build

The initial updated Windows player built successfully in **45.96 s**, with **0 errors and 1 bundled TextMeshPro debug-symbol pragma warning** (`Validation/drive-build.json`). Vendor shader code was not edited.

After the final brake-priority refinement, one build attempt failed while launching Burst AOT compilation (`Validation/drive-build-burst-failure.json`). The report identified the native Burst compiler, with empty compiler stdout/stderr; it was not a C# compilation failure. An unchanged retry succeeded with Burst still enabled. **Final BuildReport: Succeeded, 0 errors, 0 warnings, 28.89 s, 114,666,298 bytes** before external music (`Validation/drive-build-final.json`). No package/project settings were changed to work around the failure. Editor logs retain the known Burst startup errors and that failed attempt; the precise intermittent compiler-startup cause remains unresolved. No new gameplay/shader exceptions were observed.

The standalone executable launched on Direct3D 12, stayed running/responding, and its startup log contained no gameplay exceptions, missing references or shader errors. The owned smoke-test process was stopped after validation. Standalone coverage is startup; the interactive/visual scenarios were verified in the Editor. All **27** built MP3s match the source music library by SHA-256 (`Validation/drive-music.json`).

Artifact: `../Build/AfterHours/AfterHours.exe`. Ready with the existing local Burst tooling limitation. Other PCs, physical gamepads, extended difficulty balancing and a formal performance budget were not tested in this pass.
