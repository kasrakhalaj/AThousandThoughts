# Hold-to-look-back camera

Date: 2026-11-04 (Tehran). Baseline: `c058fab`. Unity 6000.6.7f1, URP, Windows x86_64, New Input System.

## Behavior

Hold C or the controller right-stick button to look behind while driving forward. Release it to restore the normal view. R remains the actual forward/reverse gear selector. Manual peeking keeps gas, selected gear/RPM, forward movement and normal forward steering; automatic reversing retains its screen-space steering and rear-facing camera behavior.

CarDriver.LookingBack is separate from the physical RearView flag. CameraRearView chooses the perspective. FollowCamera.PrepareView performs a quick cut at press/release before traffic streaming, avoiding a slow pass through the car and ensuring spawn-visibility checks use the newly requested perspective. During a held rear glance, the camera follows longitudinal movement directly so fast forward travel cannot overtake it. Ordinary reverse transitions preserve the previous smoothing. RoadWorld biases its existing 17 pooled sections toward the viewed road; traffic movement, encounter budgets, scoring and audio remain based on physical travel. Visible rear traffic is not recycled while it is being watched. No extra road/interactable pools are added.

The action belongs to Driving, so it is disabled in menus/pause; its initial-state check recognizes C held through ignition or resume. Reset clears the request. A HUD label marks the manual rear glance, with a permanent C hint in the controls row.

## Wiring

LookBackUpgrade.Apply adds one action and two bindings. Existing map/action/binding IDs and contents compare equal (`Validation/look-back-input-preservation.json`). The startup scene, existing keyboard mappings, manual gearbox, audio catalog, package manifests and Project Settings require no changes.

## Evidence

Targeted PlayMode: **2/2 passed**, zero failed/skipped, **18.81s** (`Validation/look-back-targeted.json`). Actual synthetic keyboard events cover W+C driving, positive forward speed/distance, unchanged gear/physical reverse state, rear camera position/direction, visible-rear spawn rejection, forward A steering, release-to-forward, C held through ignition, pause/release/resume and reset/menu behavior. Broader regression/runtime/build evidence follows below.

Full regression after the high-speed follow fix: **25/25 EditMode passed** (0.03s) and **18/18 PlayMode passed** (92.25s), zero failed/skipped. The added camera regression repeatedly advances the player in large steps while C is held and verifies that the rear-facing camera stays ahead of the player with the player inside its front half-space. Results: `Validation/look-back-editmode.json`, `Validation/look-back-playmode.json`.

Live keyboard-driven preview (`Validation/look-back-drive.json`): four held-C samples at 140.5–170.6 km/h, physical reverse false throughout, selected gears 4–5, rear camera exactly 10.5m ahead of the car and facing backward. After release at 14.23s it faces forward again. The run continued to GameOver; it is a controls/camera preview, not a collision-free endurance result. Screenshot `Images/look-back.png` was visually inspected: front of player car and headlights visible, rear road populated with buildings/fog, radio beside rather than covering the car, rear-view label legible. The first preview exposed high-speed follow lag; this final preview and the added regression cover the fix.

Probe cleanup returned to Edit Mode, cleared its optional look-back flag, and restored the saved high score (19) and original input/background routing (`Validation/look-back-cleanup.json`). All 27 music MP3s compare byte-for-byte with the source (`Validation/look-back-music.json`).

Windows x86_64 build: **Succeeded**, 0 errors, 1 existing vendor TMP shader deprecation warning, 60.53s, 115,004,986 bytes reported (`Validation/look-back-build.json`, `Validation/look-back-build-messages.json`). Built `AfterHours.Runtime.dll` contains the look-back action and both camera-state getters; SHA-256 identity recorded in `Validation/look-back-build-identity.json`. The fresh executable launched, initialized the Input System and Direct3D 12, stayed responsive, and produced no exception/missing-reference/shader/compiler-error matches in the startup log (`Validation/look-back-player-startup.json`). Only the owned smoke-test process was stopped afterward. Final compilation reports completed with no errors (`Validation/look-back-final-compile.json`). Packages, Project Settings and scene files remain untouched.
