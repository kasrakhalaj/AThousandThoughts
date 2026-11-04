# Sources and credits

## Environment

Kenney environment packs, downloaded from the publisher on 2026-11-03. License files are retained alongside their assets under `Assets/Art/Kenney`.

- Nature Kit: https://kenney.nl/assets/nature-kit
- City Kit (Roads): https://kenney.nl/assets/city-kit-roads
- City Kit (Suburban): https://kenney.nl/assets/city-kit-suburban
- City Kit (Commercial): https://kenney.nl/assets/city-kit-commercial

These packs are CC0. The download manifest and archive checksums are retained outside the Unity project in `../SourceAssets/Kenney/sources.json`. Three-lane road geometry and road paint are original procedural geometry; Kenney models provide the surrounding scenery and props.

## Vehicles

Five FBX vehicle models and related props were supplied by the student in `../Assets/Vehicles` and copied unchanged into `Assets/Art/Vehicles`. The student described these as free itch.io assets. The exact publisher URL/license file was not included in the supplied folder; record those details before final submission. Generated prefab wrappers normalize model scale and convert materials for URP while preserving the source files.

AI traffic uses Quaternius's **Realistic Car Pack - Nov 2018**, supplied in `../Assets/Vehicles/Realistic Car Pack - Nov 2018`. Seven FBXs and the included CC0 1.0 license were copied unchanged to `Assets/Art/RealCars2018`. Publisher: https://quaternius.com/ (the supplied license also credits https://www.patreon.com/quaternius). Generated wrappers normalize scale, convert materials and extract the authored lamp geometry into independently lit meshes. Original FBXs are not modified.

## Font

Liberation Sans and TextMeshPro essential resources. Liberation Sans font license is included under `Assets/TextMesh Pro/Fonts` (SIL Open Font License).

## Audio

Active engine, ignition and rain sounds are edited **real field recordings**, downloaded from the public Freesound HQ MP3 previews on 2026-11-03. Each source page identifies its recording as CC0 1.0:

- Player engine layers: **Renault 19 underhood acceleration**, by **lovretta** — https://freesound.org/people/lovretta/sounds/141240/
- Ignition/key turn: **Car Ignition Key - Engine Starting Running Idle 2.wav**, by **RutgerMuller** — https://freesound.org/people/RutgerMuller/sounds/50898/
- AI engine: **Car engine start up running and turning off.wav**, by **sound_catcher99** — https://freesound.org/people/sound_catcher99/sounds/425158/
- Rain: **Rainfall**, by **colorsCrimsonTears** — https://freesound.org/people/colorsCrimsonTears/sounds/532681/

License: https://creativecommons.org/publicdomain/zero/1.0/. Source MP3s, downloaded source pages and download manifest are retained in `../SourceAudio/FieldRecordings`. `Docs/RecordedSoundSources.json` records download URLs, excerpts and processing for the engine/ignition/rain clips. Preparation uses mono conversion, filtering, level adjustment and loop crossfades; no engine or rain tones are synthesized. The player layers come from portions of the same recording with runtime pitch/crossfade changes. They are stylized engine characters, not exact recordings of each depicted vehicle make.

Collision and pickup effects remain original synthesized audio generated for this project. Their generator is `../Tools/make_drive_audio.py`. Older synthesized engine/ignition/rain files remain as unused development assets; the catalog points to `Audio/Recorded` clips.

The personal MP3 music library was supplied by the student. It is stored outside Unity Assets and read from the local Music folder. Original artist/song filenames are preserved. The music is not part of the Kenney assets. Original-format backups are retained outside the active library.

## Manual gearbox and throttle cues

Additional real recordings downloaded from Freesound public HQ previews on 2026-11-04, both CC0 1.0:

- **412_gearsw.wav**, **Mihacappy** — https://freesound.org/people/Mihacappy/sounds/871568/ — recorded gear changes inside an IZ-412. Three lever/clunk excerpts provide shift variation.
- **v8 engine rev.wav**, **overmedium** — https://freesound.org/people/overmedium/sounds/651534/ — recorded throttle blips/idling. Short excerpts provide gas-on, gas-off and downshift rev-match cues.

Sources, HTML license evidence and originals are retained outside the Unity project in `../SourceAudio/ManualGears`. `Docs/ManualGearSoundSources.json` records exact URLs, excerpts, checksums and processing; `Tools/prepare_manual_gear_audio.py` reproduces the mono PCM clips. These are stylized layers alongside the existing engine recording, not exact make-matched car sounds. No synthesized gear or throttle sound was generated. Runtime limiter pulse and engine pitch/volume control act on the real recordings.

## Assistance

OpenAI Codex assisted with planning, C# implementation, asset preparation, audio conversion, synthesized effects, documentation and validation. The student directs the game design and supplies the music and vehicle assets. This acknowledgement follows the assignment's external-source disclosure requirement.
