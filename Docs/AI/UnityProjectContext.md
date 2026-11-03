# After Hours project context

Analyzed 2026-11-03. This project was created for HW2 after reading `../HW2.pdf`; initially only the vehicle FBXs, music library and assignment were present. No prior HW2 scenes/code existed.

## Confirmed environment

- Root: `E:/MyGames/HW2/AfterHours`.
- Unity 6000.6.7f1, revision b22767f04f04; Universal 3D template, URP 17.6.0, PC_RPAsset renderer.
- New Input System 1.20.0; exclusive activeInputHandler=1. Action maps Driving and System live in `Assets/Game/Generated/AfterHours.inputactions`.
- uGUI/TextMeshPro (2.6.0 package), Unity Test Framework 1.8.0.
- Official Unity Pipeline 0.9.1-exp.1 is available through the local unity CLI, project selector required. Current grouped commands include script eval, capture game_view, console get, editor.playmode start/stop, test run.
- Windows x86_64 is the initial platform. No networking or monetization.

## Structure and boundaries

- `Assets/Game/Scripts`: AfterHours.Runtime; references Input System, TMP/uGUI and URP/core.
- `Assets/Game/Editor`: authoring/build tools in default Editor assembly.
- `Assets/Game/Tests/EditMode` and `PlayMode`: independent test assemblies.
- `Assets/Game/Generated`: authored prefabs, URP materials, catalog, input asset and volume profile. Preserve GUIDs.
- `Assets/Art/Vehicles`: user-supplied source FBXs, unchanged.
- `Assets/Art/Kenney`: publisher environment models, textures and licenses.
- `../Music`: active external MP3 library; `../SourceAudio` originals are outside the project.

## Scene and architecture

Startup/build scene: `Assets/Game/Scenes/AfterHours.unity`. Serialized GameBootstrap references the catalog and main camera. It explicitly initializes GameManager, driver, bounded pools, weather/effects, audio, music and HUD in dependency order.

GameManager is the assignment-required Singleton. RunModel owns deterministic run rules. CarDriver moves along three lane centers and supports signed speed, braking and reverse; FollowCamera changes viewpoint behind the travel direction. Interactable is the shared abstract base for traffic/pickups. RoadWorld keeps 17 road sections and fixed pools of 48 stationary cars, 48 moving cars, 144 coins and 24 fuel cans. Scoring pickups do not respawn on recycled old road; forward distance only scores once.

Use PascalCase APIs, private state, explicit Initialize methods and namespace AfterHours. Physics-free swept proximity checks own gameplay interactions; model geometry never owns rules. Runtime HUD uses anchored uGUI/TMP with a single InputSystemUIInputModule.

## Constraints and validation

Keep the New Input System, three lanes/lives, fuel death, rising speed/traffic density, pooling, inheritance, Singleton, PlayerPrefs, night sky/headlights/underglow/fireworks/particles/Bloom, menus/HUD and audio required by HW2. User prefers grounded low-poly scenery with rain/fog and quiet sections. Gun is for later.

Authoring tool refuses to overwrite the existing game scene. Modify existing assets narrowly; do not regenerate wholesale. Package changes use PackageManager API. Keep music/source backups and build artifacts out of source control.

See `Docs/Validation.md` for actual evidence and limitations; do not infer runtime or build success from this context document. A local Git repository is initialized; no remote is configured. Vehicle publisher/license details remain unprovided. No commit existed at initial inspection. Final Windows player is outside the project at `../Build/AfterHours/AfterHours.exe`; it includes the external music folder (27 tracks at handoff).
