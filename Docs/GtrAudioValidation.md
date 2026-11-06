# R34/R35 player engine recordings

2026-11-06; baseline a0d2e18.

At the user's request, the 12 Ferrari-derived performance clips were replaced in place. Tristar Racer now uses real Nissan Skyline GT-R R34 exhaust/driving audio by LowFps (CC BY 3.0); docLorean uses a real 2012 Nissan GT-R R35 onboard engine recording by Pole Position Production from Sonniss GDC 2020 (Sonniss bundle license v2.0). Full attribution, license links and modifications are in CREDITS.md. Source vehicles provide stylized voices for the fictional game cars. Their pitch multipliers are 1.0 before the existing runtime RPM control.

Each voice has idle/mid/load loops and gas-on/off/downshift cues. Excerpts are filtered, converted to mono 44.1kHz PCM16, normalized and crossfaded/faded. Tools/prepare_performance_audio.py reproduces them using originals retained in ../SourceAudio/GTR. Docs/PerformanceCarSoundSources.json contains exact excerpts and hashes. All existing audio metadata/GUIDs and profile references were preserved; only the two profile pitch values changed in the catalog. The existing authoring menu applies those same values. Runtime driving/input/audio logic, music, weather, scene and packages were not changed.

Validation:

- All 12 WAVs have new content, correct format, bounded/non-silent levels, unchanged metadata and matching manifest hashes: Validation/gtr-audio-assets.json.
- Unity loaded all 12 referenced clips and read their PCM data; lengths match the new manifest: Validation/gtr-audio-imported-clips.json. Tools/ValidateGtrAudio.cs repeats this check.
- PerformanceVoicesFollowCarSelectionAndReturnToTheDefault: 1/1 passed, 17.47s. Checks R34/R35 profile selection, fallback to the default for another car, default Tristar engine playback and gas cues. Evidence: Validation/gtr-audio-selection-playmode.json.
- ManualGearKeysAudioAndPauseBehaveAsOneTransmission: 1/1 passed, 14.44s. Checks gas cue debouncing, shifts/clutch, pause/resume and downshift cues. Evidence: Validation/gtr-audio-gears-playmode.json.
- These are two focused PlayMode checks, not a rerun of the previous 65-check suite. Imported data/playback tests do not establish subjective listening quality; no direct listening claim is made.

The first authoring request timed out while the Editor reloaded; an accompanying test-request transport failure is retained in Validation/gtr-audio-import-reload.json. The operation was subsequently applied and validated. Existing Burst named-pipe diagnostics remain in the Editor log and were not suppressed. See Validation/gtr-audio-baseline.json.

Windows build: succeeded in 49.14s, zero build errors, one existing TextMeshPro shader pragma deprecation warning. Output: `E:/MyGames/HW2/Build/AfterHours-GTR/AfterHours.exe`. Tools/BuildGtrPlayer.cs creates this build and copies external music and CREDITS.md. All 27 source MP3 hashes match the packaged songs; packaged credits match the project. Evidence: Validation/gtr-audio-build.json and gtr-audio-build-identity.json.

The standalone player was responsive and initialized Direct3D 12. Startup log had no exception, missing-reference, compiler or shader-error matches. The pre-existing D3D12 info-queue interface-query diagnostic appeared before graphics initialization continued. Startup-only evidence: Validation/gtr-audio-player-startup.json. This does not claim a full manual drive or subjective audition.

The two additional Console error entries during this pass were recurrences of the existing Burst named-pipe failure, not new audio/runtime or C# compilation errors. Current Console ground truth reports compilationFailed=false, consoleErrors=0 and consoleWarnings=1. Original diagnostics were retained, not cleared. The Editor is outside Play Mode, no longer compiling, high score is restored to 19 and input background behavior is ResetAndDisableNonBackgroundDevices. Evidence: Validation/gtr-audio-console-final.json and gtr-audio-final-state.json.
