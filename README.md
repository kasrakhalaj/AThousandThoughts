# After Hours

A low-poly endless night drive for Game Design HW2. Choose one of five cars, start the engine, and dodge traffic through changing districts, fog, and rain. Collect coins and fuel to keep the run going.

## Run from source

1. Open the repository root with **Unity 6000.6.7f1**. Let Unity restore the packages and import the assets.
2. Open `Assets/Game/Scenes/AfterHours.unity` and enter Play Mode.
3. Select a car and press **Enter** to start the engine. Hold **W** to drive.

The scene, vehicle prefabs and game catalog are included. The project uses URP, TextMeshPro/uGUI and the New Input System.

## Controls

| Input | Action |
| --- | --- |
| Enter | Start the engine |
| W / up arrow | Accelerate |
| S / down arrow | Brake |
| A / D or left / right arrows | Steer one lane while rolling |
| Q / E | Shift down / up |
| R | Select forward / reverse |
| Hold C | Look behind |
| Escape | Pause / resume |
| N / B | Next / previous track |
| M | Pause / resume music |

Brake takes priority over gas. Selecting reverse brakes to a stop first. Hold W to move in the selected direction. In reverse, steering follows the camera view. Holding C while driving forward keeps forward steering. The dashboard shows speed, RPM, acceleration and shift hints.

## Windows build

In Unity, choose **After Hours > Build Windows player**. The Windows x64 player and asset credits appear in `Build/AfterHours/` inside the project. Keep the complete player folder together when moving it.

## Optional music

Create a `Music` folder beside the executable or at the repository root for Play Mode. The Editor also checks `../Music` when the project has no local music folder. Put MP3s in that folder and use subfolders for artist playlists. Restart after changing the library.

The game works with an empty library. Personal music and original recording downloads stay outside version control.

## Tests and asset credits

Run the Edit Mode and Play Mode suites in **Window > General > Test Runner**. The suites exercise run rules, steering, gears, camera, traffic, pooling, music and weather. They work with or without a local music library.

See [CREDITS.md](CREDITS.md) for third-party assets, source links and terms. This is the local source repository for the assignment. The repository and its history include third-party source assets, so review their redistribution terms before publishing the source.
