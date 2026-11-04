# After Hours

A low-poly endless night drive for Game Design HW2. Select one of five cars, start the engine, dodge traffic, collect coins and fuel, and listen to a personal music library through changing rain and fog.

## Open and play

Open this folder with Unity **6000.6.7f1**. Open `Assets/Game/Scenes/AfterHours.unity`, then enter Play Mode. The project uses URP, TextMeshPro/uGUI, and the **New Input System only** (`activeInputHandler = 1`).

The scene and serialized catalog/prefabs are already authored. `After Hours > Create initial game scene` is an initial authoring tool; it deliberately refuses to overwrite the existing scene.

## Controls

| Input | Action |
| --- | --- |
| Enter / Start button | Start the engine |
| A/D or left/right arrows | Change lane |
| S/down arrow | Brake (takes priority over gas) |
| R | Select forward or reverse gear |
| C (hold) / right-stick click (hold) | Look behind while continuing to drive |
| E / right shoulder | Shift up one gear |
| Q / left shoulder | Shift down one gear |
| W/up arrow | Hold for gas in the selected gear |
| Escape | Pause/resume |
| N / B | Next / previous song |
| M | Pause/resume music |

Hold C to check traffic behind while driving forward; release C to restore the normal view. This camera glance keeps forward steering, gas, gear and engine RPM behavior. R still selects actual reverse driving. Steering follows screen space while the car is reversing. The car's orientation remains physically forward. Gamepad D-pad, triggers, west button and Start are also bound; keyboard/mouse is the primary validation target.

The engine starts while the car remains stopped. Hold W to accelerate, release it to coast and press S to brake. R selects forward or reverse gear; W supplies gas in either gear. Braking takes priority when both pedals are held. The dashboard shows actual km/h, a speed bar, gear and gas/coast/brake state. The forward limit is 300 km/h. Gas supplies acceleration against rolling/aerodynamic resistance; the final 100 km/h takes much longer than the first. With well-timed manual shifts, base clear-road tuning is approximately 0-100 in 7s, 0-200 in 16s and 0-300 in 39s. Holding gas alone stays in the selected gear. Distance progression adds a small acceleration bonus while traffic gets harder. The dashboard shows signed acceleration along travel in m/s²; six selected gears drive actual torque, per-gear rev limits and engine RPM. E shifts up, Q shifts down, and an automatic clutch briefly cuts power for 0.36s. First through sixth redlines are approximately 45/85/130/180/240/300 km/h. Early high gears lug; a downshift that would over-rev is rejected. The RPM bar and shift prompts guide the driver.

## Music

In the Editor the game reads **`../Music`**, the existing HW2 music folder. Each nested folder becomes a playlist; root-level tracks belong to Singles. All Music combines the library. Supported runtime files are MP3. Only the active track is loaded. Category controls, shuffle, volume, track progress and an audio-reactive spectrum are included. The library is scanned at startup; restart the game after adding or removing files.

For a Windows player, the `Music` folder belongs **beside the executable**. `After Hours > Build Windows player` creates `../Build/AfterHours` and copies the active MP3 library there. Original lossless files and backups in `../SourceAudio` are not game content and are not copied.

## Required architecture

- `GameManager` is the Singleton and state owner: Menu, Starting, Playing, Paused, GameOver.
- `RunModel` owns lives, fuel, score and progressive difficulty. Distance score is a forward high-water mark.
- `DriverInput` clones the serialized Input Actions asset. Driving actions are disabled in menus/pause/results; system/music actions remain available. The UI uses InputSystemUIInputModule.
- StationaryCar, MovingCar, CoinPickup and FuelPickup inherit abstract `Interactable` through virtual/override behavior.
- `RoadWorld` prewarms a fixed pool of road segments, cars and pickups. Road sections recycle in both directions. Revisiting recycled sections does not regenerate scoring pickups. No repeated gameplay Instantiate/Destroy.
- Traffic drivers cruise, follow, signal, change lanes and recover before another maneuver. Cautious, balanced and assertive profiles use different headways and signal durations. Drivers overtake slower traffic; cautious drivers also return right when safe. Other drivers hold their lane until a pass offers enough benefit.
- Target-lane checks include the player, committed player steering, traffic ahead/behind and other merge reservations. Following observes both occupied lanes during a merge. Rain/fog lower desired speeds and increase spacing.
- A bounded traffic director maintains traffic within 300 metres ahead/100 behind, with sparse, normal, busy and shorter quiet stretches. At high speed the encounter range expands to five seconds ahead, the budget grows and replenishment speeds up. Distant cars no longer consume the encounter budget. New traffic must be outside the camera or fully concealed by opaque distance fog, have sufficient closing-time/vehicle gaps, and leave a reachable escape lane. The streamed road window follows forward/reverse viewing. Quiet stretches stop replenishment and retire hidden cars while visible cars clear naturally.
- Normal traffic uses all seven Quaternius 2018 car models with rotating wheels, lights and quiet recorded engines. Queue-based reuse cycles through model variants. Rare hazard-lit stopped cars preserve the assignment's stationary obstacles.
- `WeatherDirector` transitions among clear/rain/fog zones and occasional fireworks. Lights, smoke, sparks, pickup particles and restrained speed-reactive underglow cover the visual brief without changing the grounded art direction.
- The moon sits within the driving view. Towns have houses and buildings on both sides; forest stretches use layered trees, bushes, grass, rocks and distant faceted hills. Scenery is prewarmed and recycled with the road.
- Fog uses a depth-based URP fullscreen pass before post-processing. An early patch fades in from 110 m, becomes dense around 185–270 m and clears by 350 m; later weather zones include longer fog/rain stretches.
- Each of the five cars has fitted headlight lenses and road beams, red tail/brake lights and white reverse lamps. Ignition controls them; traffic cars also carry lights. Damage flashes preserve lamp emission.
- High score uses `PlayerPrefs` key `AfterHours.HighScore.v1`.

Player ignition, idle, mid/load engine layers, traffic engines and rain use actual CC0 recordings. Three newly recorded lever-clunk variants, gas-on/off excerpts and a downshift rev blip provide gearbox/pedal feedback. A held gas input does not spam rev one-shots. New source credits are in `Docs/ManualGearSoundSources.json`. Engine layers respond to throttle, speed and simulated gear changes; AI sources are quieter and spatial, with distance fade and mild Doppler. Music remains independently controlled. Credits and source excerpts are recorded in `CREDITS.md` and `Docs/RecordedSoundSources.json`. `Tools/prepare_recorded_audio.py` reproduces preparation from the source recordings retained outside the project.

## Tests and development

Use Unity Test Runner: `AfterHours.EditMode` for run and traffic safety rules; `AfterHours.PlayMode` for ignition, New Input System steering/reverse, camera, pause, pool capacity, restart, damage, high-score persistence, music/weather, safe spawning, reverse-camera visibility, merge reservations/cancellation/completion and quiet-road draining. Tests restore the pre-existing high-score preference.

The Unity Pipeline package supports local Editor control and is a development dependency. No online game services, ads, accounts or multiplayer are required.

This is the first playable implementation. Track balancing, camera comfort, weather density, audio mixing and car-specific presentation should be refined through player feedback. Weapons are deliberately deferred.
