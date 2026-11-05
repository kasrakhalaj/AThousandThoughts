# Authored player-car lights

Date: 2026-11-05 (Tehran). Baseline: e1bf745, clean working tree. Unity 6000.6.7f1, URP 17.6.0, Windows x86_64, New Input System unchanged.

## Report and confirmed cause

The player cars used generated spheres for some headlights and cubes for other headlights, taillights and reverse lamps. Reproduction: inspect all five cars from front and rear with ignition on. NightVisualUpgrade.UpgradeCar used primitive shapes at guessed XY coordinates and projected them onto each body. Several rear lamps landed on the bumper below the real clusters; the Tristar front lamps missed its actual headlight outlines. This was not a missing headlight beam or a fog failure.

Original source FBXs contain gradientEmmisive material slots within their body meshes. The imported material conversion discarded emission maps, so their authored lens faces were left unlit and separate generated primitives were overlaid. Source material inventory: Validation/player-lamps-source-materials.json. Alternatives ruled out by source inspection and before/after renders: missing lamp geometry, bad imported car orientation, or an exclusively URP shader failure. Baseline Console retains the existing Burst named-pipe compiler startup errors; no Console was cleared or compiler disabled.

## Correction

PlayerLampUpgrade extracts the authored headlight and red tail/brake lens triangles in normalized car coordinates, offsets them 2mm to avoid z-fighting, and saves persistent meshes under Assets/Game/Generated/PlayerLamps. Two front spotlights are positioned at the actual headlight centers. A rear downward-facing spill light illuminates behind the car rather than flooding body panels and the spare wheel. The original bodies, FBXs, textures, transforms, car prefab GUIDs, audio, input actions, traffic and scene remain intact.

Only known lens faces are selected: wheel accents, time-machine details, dark glass, number plates, covered roof spots and ordinary paint do not become headlights. White reverse lenses are extracted from docLorean, Landyroamer and Toyoyo; Beatall and Tristar have no distinct authored white reverse lens, so no extra cube is invented for them. The physical reverse control and reverse state remain supported for all cars.

NightVisualUpgrade now delegates player lamps to this repair, and initial model authoring no longer creates the obsolete Tail lamp cubes. Reapplying twice succeeds while retaining mesh asset paths/GUIDs (Validation/player-lamps-idempotent.json).

## Verification

Five front and five rear URP prefab renders were inspected before/after at the same camera/lighting settings. Current contact sheet: Images/player-lamps.png. These are isolated prefab renders, not gameplay screenshots and do not include the gameplay Bloom/fog pass. The exact geometry regression checks every copied lamp vertex against an original transformed FBX vertex within 2.2mm and rejects built-in primitive meshes. Ignition/brake/damage/reverse checks exercise the runtime light controller; reverse verification uses the docLorean, which has real reverse lenses.

Final test, runtime and build evidence follows below.

EditMode: **30/30 passed**, zero failures/skips, 0.66s, including all five source-geometry cases (`Validation/player-lamps-editmode.json`). Unity initially kept an outdated test-assembly source list; the geometry cases were moved into the existing vehicle test source, then their presence in the compiled assembly and the five reported results were verified. No test was disabled or waived.

PlayMode regression: **18/18 passed**, zero failures/skips, 95.36s (`Validation/player-lamps-playmode.json`). Includes all-car light wiring/materials, ignition, damage-flash preservation, actual reverse-lens emission, manual gears/audio, forward/reverse movement, camera look-back, traffic and pooling. Total: **48 checks passed**.

Live Tristar drive (`Validation/player-lamps-drive.json`): game frames advanced through 31 telemetry samples, with selected gears and real forward movement. The run ended at GameOver; it is a visual smoke check, not an endurance claim. `Images/player-lamps-gameplay.png` was visually inspected at about 161km/h: the original rear strips remain on the car and glow through the gameplay Bloom/fog treatment, with no bumper-mounted cubes. The original selection, high score and input/background settings were restored and Play Mode exited (`Validation/player-lamps-cleanup.json`).

Windows build: **Succeeded**, 0 errors, 1 existing vendor TMP shader deprecation warning, 136.48s, 115,035,402 bytes reported (`Validation/player-lamps-build.json`, `Validation/player-lamps-build-messages.json`). All 26 authored head/tail/reverse mesh names occur in the built asset data; asset SHA-256 values are recorded in `Validation/player-lamps-build-identity.json`. Fresh player startup remained responsive, initialized Direct3D 12 and logged no exception/missing-reference/shader/compiler-error matches (`Validation/player-lamps-player-startup.json`). The owned smoke-test process was stopped. Final compilation: completed, no errors (`Validation/player-lamps-final-compile.json`). All 26 mesh assets include .meta files. Source art, car prefab .meta GUIDs, Packages, Project Settings, input and scene files have no changes.
