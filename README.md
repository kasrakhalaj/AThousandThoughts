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
| S/down arrow | Brake; hold at a stop to reverse |
| R | Change between forward and reverse |
| W/up arrow | Return to forward driving |
| Escape | Pause/resume |
| N / B | Next / previous song |
| M | Pause/resume music |

Steering follows screen space while the camera faces the road behind the reversing car. The car's orientation remains physically forward. Gamepad D-pad, triggers, west button and Start are also bound; keyboard/mouse is the primary validation target.

## Music

In the Editor the game reads **`../Music`**, the existing HW2 music folder. Each nested folder becomes a playlist; root-level tracks belong to Singles. All Music combines the library. Supported runtime files are MP3. Only the active track is loaded. Category controls, shuffle, volume, track progress and an audio-reactive spectrum are included. The library is scanned at startup; restart the game after adding or removing files.

For a Windows player, the `Music` folder belongs **beside the executable**. `After Hours > Build Windows player` creates `../Build/AfterHours` and copies the active MP3 library there. Original lossless files and backups in `../SourceAudio` are not game content and are not copied.

## Required architecture

- `GameManager` is the Singleton and state owner: Menu, Starting, Playing, Paused, GameOver.
- `RunModel` owns lives, fuel, score and progressive difficulty. Distance score is a forward high-water mark.
- `DriverInput` clones the serialized Input Actions asset. Driving actions are disabled in menus/pause/results; system/music actions remain available. The UI uses InputSystemUIInputModule.
- StationaryCar, MovingCar, CoinPickup and FuelPickup inherit abstract `Interactable` through virtual/override behavior.
- `RoadWorld` prewarms a fixed pool of road segments, cars and pickups. Road sections recycle in both directions. Revisiting recycled sections does not regenerate scoring pickups. No repeated gameplay Instantiate/Destroy.
- Merging traffic signals first, checks a reserved target corridor, and only merges away from the player's immediate reaction zone.
- `WeatherDirector` transitions among clear/rain/fog zones and occasional fireworks. Lights, smoke, sparks, pickup particles and restrained speed-reactive underglow cover the visual brief without changing the grounded art direction.
- High score uses `PlayerPrefs` key `AfterHours.HighScore.v1`.

## Tests and development

Use Unity Test Runner: `AfterHours.EditMode` for run rules; `AfterHours.PlayMode` for ignition, New Input System steering/reverse, camera, pause, pool capacity, restart, damage, high-score persistence, music and weather. Tests restore the pre-existing high-score preference.

The Unity Pipeline package supports local Editor control and is a development dependency. No online game services, ads, accounts or multiplayer are required.

This is the first playable implementation. Track balancing, camera comfort, weather density, audio mixing and car-specific presentation should be refined through player feedback. Weapons are deliberately deferred.
