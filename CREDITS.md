# Sources and credits

## Interface fonts

- **Cinzel Bold** by the Cinzel Project Authors. https://github.com/NDISCOVER/Cinzel
- **Barlow Condensed SemiBold** by the Barlow Project Authors. https://github.com/jpt/barlow

Both fonts use the SIL Open Font License 1.1. The source fonts and license notices are under `Assets/Game/Fonts`, and the notices ship in `Licenses` beside the Windows player.

## Environment

Kenney environment packs, downloaded from the publisher on 2026-11-03. License files are retained alongside their assets under `Assets/Art/Kenney`.

- Nature Kit: https://kenney.nl/assets/nature-kit
- City Kit (Roads): https://kenney.nl/assets/city-kit-roads
- City Kit (Suburban): https://kenney.nl/assets/city-kit-suburban
- City Kit (Commercial): https://kenney.nl/assets/city-kit-commercial

These packs are CC0. The download manifest and archive checksums are retained outside the Unity project in `../SourceAssets/Kenney/sources.json`. Three-lane road geometry and road paint are original procedural geometry; Kenney models provide the surrounding scenery and props. The district city pass reuses these four packs; sidewalks, service structures, the café canopy, furniture, utility wires and overpass approaches are original procedural geometry. Selected lit windows follow existing imported glass faces.

## Vehicles

The five player cars are from **FREE – Low Poly Car Starter Pack** by **designersoup** (https://designersoup.itch.io/low-poly-car-pack-1), supplied by the student. The publisher permits use and modification in games and lists restrictions on repackaging the source models and textures. Generated prefabs normalize the FBXs and convert their materials for URP.

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

Collision and pickup effects are synthesized for this project. The original generator is `../Tools/make_drive_audio.py`. The earlier synthesized engine, ignition and rain placeholders and the superseded mono rain clip were retired.

The personal MP3 music library was supplied by the student. It is stored outside Unity Assets and read from the local Music folder. Original artist/song filenames are preserved. The music is not part of the Kenney assets. Original-format backups are retained outside the active library.

## Manual gearbox and throttle cues

Additional real recordings downloaded from Freesound public HQ previews on 2026-11-04, both CC0 1.0:

- **412_gearsw.wav**, **Mihacappy** — https://freesound.org/people/Mihacappy/sounds/871568/ — recorded gear changes inside an IZ-412. Three lever/clunk excerpts provide shift variation.
- **v8 engine rev.wav**, **overmedium** — https://freesound.org/people/overmedium/sounds/651534/ — recorded throttle blips/idling. Short excerpts provide gas-on, gas-off and downshift rev-match cues.

Sources, HTML license evidence and originals are retained outside the Unity project in `../SourceAudio/ManualGears`. `Docs/ManualGearSoundSources.json` records exact URLs, excerpts, checksums and processing; `Tools/prepare_manual_gear_audio.py` reproduces the mono PCM clips. These are stylized layers alongside the existing engine recording, not exact make-matched car sounds. No synthesized gear or throttle sound was generated. Runtime limiter pulse and engine pitch/volume control act on the real recordings.

## Assistance

OpenAI Codex assisted with planning, C# implementation, asset preparation, audio conversion, synthesized effects, documentation and validation. The student directs the game design and supplies the music and vehicle assets. This acknowledgement follows the assignment's external-source disclosure requirement.

## Performance-car engine voices — updated 2026-11-06

The Ferrari-derived profiles were replaced at the user's request. The 12 clips under `Assets/Game/Audio/Performance` now use these real recordings:

- **Tristar Racer — Nissan Skyline GT-R R34:** LowFps, *BUMPER CAM NISSAN SKYLINE GT-R R34 - PURE SOUND AT NÜRBURGRING*. Original: https://www.youtube.com/watch?v=NpqxDM7pejs . Audio source and license declaration: https://commons.wikimedia.org/wiki/File:Nissan_Skyline_GT-R_-_engine_and_exhaust_sound_(YouTube_NpqxDM7pejs).opus . Licensed under **Creative Commons Attribution 3.0**, https://creativecommons.org/licenses/by/3.0/ . Changes: excerpts, mono conversion, filtering, DC removal, loop crossfades, cue fades and level normalization. No endorsement is implied.
- **docLorean — Nissan GT-R R35 2012:** **Pole Position Production**, `Nissan_GTR_t6_Onbrd_Drive_Ramps_Engine_Left_Valve_DPA4062.wav`, from the **Sonniss GDC 2020 Game Audio Bundle**, part 8. Collection: https://sonniss.com/gameaudiogdc/ . Used under the **Sonniss GDC Game Audio Bundle license v2.0**, effective 27 August 2026: https://sonniss.com/gdc-bundle-license/ . Changes: excerpts, mono conversion, filtering, DC removal, loop crossfades, cue fades and level normalization. Sound effects used under the Sonniss GDC bundle licence, incorporated in this game; original recording remains the property of its rights holder.

Each profile has recorded idle, mid/load, gas-on/off and downshift cues. The Nissan recordings supply stylized voices for the fictional player vehicles; they are not recordings of those fictional models. Both profile pitch multipliers are 1.0 before the existing runtime RPM response. Other cars keep their previous recordings. No synthesized replacement engine sound was generated.

Original Nissan recordings and download metadata are retained outside Unity in `../SourceAudio/GTR`. `Docs/PerformanceCarSoundSources.json` records source URLs, licenses, excerpts and checksums; `Tools/prepare_performance_audio.py` reproduces preparation. The superseded Ferrari originals remain in `../SourceAudio/PerformanceCars` and Git history; they are no longer the performance profiles' audio. Credits are copied alongside the GTR Windows player.

## Recorded weather

- Light rain uses **gentle steady spring rain city**, recorded by **Garuda1982** and released under CC0 1.0. https://freesound.org/people/Garuda1982/sounds/852917/
- Heavy rain uses **Tropical Rain 1.wav**, recorded by **Doc Jon** and released under **CC BY 4.0** (https://creativecommons.org/licenses/by/4.0/). The game uses a stereo excerpt with filtering, level matching and a loop crossfade. https://freesound.org/people/Doc%20Jon/sounds/420635/
- Storm thunder uses **thunder crack.wav** and **thunder crack 2.wav**, field recordings by **cognito perceptu** released under CC0 1.0. The game uses stereo excerpts with filtering, level matching and fades. https://freesound.org/people/cognito%20perceptu/sounds/347853/ and https://freesound.org/people/cognito%20perceptu/sounds/347854/

Exact excerpts and processing are recorded in `Docs/WeatherSoundSources.json`. Original downloaded recordings are retained outside the project in `../SourceAudio/Weather`.
