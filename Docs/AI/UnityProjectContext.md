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

Manual driving replaces auto-cruise: DriverInput.Throttle aliases the existing Forward action (IDs/bindings unchanged). W/right trigger supplies continuous gas, release coasts, S always brakes and R selects gear. The brake wins when both pedals are held. Pedal actions opt into initial-state checks so holding a pedal through ignition/resume works. VehicleDynamics integrates throttle torque, rolling/aero drag, coasting, brake priority and direction changes, with a 300km/h forward limit and 10m/s reverse cap. RunModel.AccelerationScale adds up to 12% power with distance while traffic difficulty rises. Six gear ranges are shared with CarAudio; CarDriver reports actual acceleration and PreviousPosition. RoadWorld uses exact relative swept x/z contact intervals rather than an enlarged distance threshold, preventing early false hits at high speed. GameHud shows acceleration in travel direction; camera FOV is capped at 74 degrees at maximum speed. The HUD displays rounded km/h, a speed bar, gear and pedal state.

AI traffic uses seven Quaternius Realistic Car Pack - Nov 2018 FBXs under Assets/Art/RealCars2018, with its CC0 license. TrafficAudioUpgrade.Apply authors normalized models, meshes extracted from the source Headlights/TailLights submeshes, seven moving variants, and a hazard-lit stationary SUV. After the density correction, RoadWorld budgets nearby encounters (at least 300m (five travel-seconds at high speed) ahead/100m behind), seeds 2-3 staggered candidates per non-quiet section, replenishes at 0.8/1.4/2.5s busy/normal/sparse intervals and retires passed hidden cars. Quiet sections occupy 3/24 sections, busy 6/24; initial calm is only the first section. Fully opaque squared-exponential fog can conceal spawns inside the camera frustum (below 0.01% transmitted car color); near-view spawning still requires reaction/gap checks. Far-away AI passes no longer reserve an escape lane for a player hundreds of metres away, and overtaking benefit scales with weather/profile. RoadWorld owns a paced traffic director with sparse/normal/busy/quiet route sections, gradual difficulty budgets and jittered replenishment. TrafficRules contains shared following, closing-speed, spawn-reaction and route rules. MovingCar uses cruising/following/signaling/changing/recovery states and cautious/balanced/assertive headways. Lane reservations include the player and its committed lane; both occupied lanes are sensed while crossing. Rain/fog lower speeds and lengthen headways. New traffic is checked for camera visibility, space, closing time and an escape lane; initial pre-ignition population is the only camera-visibility exception. Quiet sections stop replenishment and remove hidden cars, letting visible traffic clear naturally. Seventeen road chunks are biased toward the selected travel direction to cover the reverse view. Pools use queues to cycle model variants; total capacity remains 264. Stopped hazards are rare. NightVisualUpgrade and player-model repair preserve the upgraded traffic variants.

Active engine/ignition/rain assets live under Assets/Game/Audio/Recorded. Catalog engineSound is player idle; engineMidSound/engineLoadSound provide RPM layers, trafficEngineSound is a separate real recording, and ignition duration follows the clip length (currently 2.7 seconds). CarAudio blends three player loops with simulated gear/pedal pitch changes. Player gain is .22–.30; AI sources use .085, 3D distance fade (3–42 m), mild Doppler and lower voice priority. Traffic audio pauses with gameplay; music remains independent. See CREDITS.md and Docs/RecordedSoundSources.json for sources and excerpts. Tools/prepare_recorded_audio.py prepares loops from source MP3s retained in ../SourceAudio/FieldRecordings.

Use PascalCase APIs, private state, explicit Initialize methods and namespace AfterHours. Physics-free swept proximity checks own gameplay interactions; model geometry never owns rules. Runtime HUD uses anchored uGUI/TMP with a single InputSystemUIInputModule.

The night visual pass adds VehicleLights to all five player models and the two traffic visuals. CarDriver excludes lamp renderers from damage flashes and updates ignition/braking/reverse states. MovingCar resolves its named Turn indicator explicitly. RoadChunk prewarms 64 trees, 40 shrubs/grass, 10 rocks, 8 houses, 6 buildings, 8 props and 8 faceted hills per segment, enabling biome-appropriate groups. WeatherDirector drives shader globals for RoadFog.shader and sky haze; RenderSettings.fog is disabled to avoid duplicate fog. PC_Renderer contains Unity's built-in FullScreenPassRendererFeature named Road distance and ground fog, with depth and color input before post-processing. Preserve this renderer subasset and its material. NightVisualUpgrade.Apply performs targeted authoring repairs without replacing the scene.

## Constraints and validation

Keep the New Input System, three lanes/lives, fuel death, rising speed/traffic density, pooling, inheritance, Singleton, PlayerPrefs, night sky/headlights/underglow/fireworks/particles/Bloom, menus/HUD and audio required by HW2. User prefers grounded low-poly scenery with rain/fog and quiet sections. Gun is for later.

Authoring tool refuses to overwrite the existing game scene. Modify existing assets narrowly; do not regenerate wholesale. Package changes use PackageManager API. Keep music/source backups and build artifacts out of source control.

See `Docs/Validation.md` for actual evidence and limitations; do not infer runtime or build success from this context document. A local Git repository is initialized; no remote is configured. Vehicle publisher/license details remain unprovided. No commit existed at initial inspection. Final Windows player is outside the project at `../Build/AfterHours/AfterHours.exe`; it includes the external music folder (27 tracks at handoff).

PlayMode tests run in an unfocused Editor through Pipeline. Their fixture temporarily uses IgnoreFocus as well as AllDeviceInputAlwaysGoesToGameView, restricts the cloned gameplay actions to its synthetic keyboard, then restores both settings. Routing alone still permits the Input System to disable keyboards on focus loss. Cleanup resets the test run via MainMenu before restoring PlayerPrefs, preventing OnApplicationQuit from resaving a test score. Keep these changes scoped to tests.

## Manual gearbox update

ManualTransmission owns selected/pending gears 1-6, the 0.36s clutch interval, request sequence and unsafe-downshift feedback. Gas alone no longer shifts. VehicleDynamics.StepInGear applies gear-dependent low-RPM torque and power-cut redline limits (45/85/130/180/240/300km/h), preserving momentum during shifts. E/right shoulder shifts up; Q/left shoulder down. ManualGearUpgrade.Apply adds these two actions to the existing Driving map and wires six new PCM field-recorded cues; all prior action/map/binding IDs are preserved. New catalog fields are gearShiftSounds, throttleOnSound, throttleOffSound and downshiftBlipSound. CarAudio observes accepted shift sequences, uses selected-gear RPM, plays a lever variant once, adds a rev-match blip on downshift and debounces pedal one-shots. Dedicated gear/pedal sources pause with gameplay. HUD adds RPM, a redline bar and shift/rejection/lugging prompts. R resets the forward gearbox to first and preserves the separate reverse-driving behavior. Source metadata: Docs/ManualGearSoundSources.json. Reproduction: Tools/prepare_manual_gear_audio.py. No new packages or input-system switch.

## Hold-to-look-back update

Driving.LookBack binds C and Gamepad/rightStickPress, uses held state plus initial-state checks, and is added idempotently by LookBackUpgrade.Apply. Existing maps/actions/bindings are retained. CarDriver.LookingBack is a camera request; CameraRearView combines it with the physical RearView. The physical flag continues to own steering, RPM/audio and traffic movement/budget direction. FollowCamera.PrepareView cuts directly to/from a manual rear glance and runs before RoadWorld.Tick so visibility checks use the requested view. Normal automatic reverse transitions keep their smoothing. RoadWorld's 17-section window follows CameraRearView, covering the road behind during a forward-driving glance; pooling remains fixed. A held C through ignition/resume is recognized, release restores the travel view and MainMenu/reset clears it. HUD adds C and a temporary look-back label. See Docs/LookBackValidation.md.
