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

## Font

Liberation Sans and TextMeshPro essential resources. Liberation Sans font license is included under `Assets/TextMesh Pro/Fonts` (SIL Open Font License).

## Audio

Engine, ignition, rain, collision and pickup sounds are original synthesized audio generated for this project. Their reproducible generator is `../Tools/make_drive_audio.py`.

The personal MP3 music library was supplied by the student. It is stored outside Unity Assets and read from the local Music folder. Original artist/song filenames are preserved. The music is not part of the Kenney assets. Original-format backups are retained outside the active library.

## Assistance

OpenAI Codex assisted with planning, C# implementation, asset preparation, audio conversion, synthesized effects, documentation and validation. The student directs the game design and supplies the music and vehicle assets. This acknowledgement follows the assignment's external-source disclosure requirement.
