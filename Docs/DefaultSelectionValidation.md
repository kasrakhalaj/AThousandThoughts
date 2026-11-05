# Default car and opening music

Date: 2026-11-05 (Tehran). Baseline: c626c51, clean working tree.

Fresh startup selects TRISTAR RACER by catalog name, falling back to the first car if the catalog does not contain it. Manual car selection and restart/menu behavior remain available.

Music categories start with Singles, then All Music, followed by artist folders alphabetically. The opening playlist pins Kavinsky - Nightcall first and DVRST Tell Me What Alternative second. The DVRST file remains in its artist playlist and is also listed in Singles; All Music includes each scanned file once. Remaining tracks retain alphabetical order, shuffle remains opt-in, and an empty Singles playlist falls back to All Music for playback. No audio file is renamed, moved, duplicated or re-encoded.

Existing selection, music/playback and lamp/reverse checks are adjusted for the requested defaults. Validation and build evidence follows below.

Existing music/playback/weather check: **1/1 passed**, 6.86s (`Validation/defaults-music.json`). It verified startup Singles/Nightcall, next-track DVRST/Tell Me What, play/pause, retained Pink Floyd category and the complete All Music count, then existing weather transitions.

Existing selection/ignition/steering/reverse/pause/pooling/restart check: **1/1 passed**, 14.25s (`Validation/defaults-selection.json`). It verified Tristar Racer is selected initially and the next-car control wraps correctly.

Existing all-car lighting/damage/reverse check: **1/1 passed**, 4.95s (`Validation/defaults-lights.json`). The test explicitly selects the docLorean for its authored reverse lenses after changing the initial default. Total targeted validation: **3/3 existing checks passed**, zero failures/skips.

Fresh menu preview verified logical selection and active model both Tristar Racer, category Singles, Nightcall actively playing, category order Singles / All Music / DVRST / Enya / Hylics / Pink Floyd, and four Singles entries (`Validation/defaults-startup.json`). The screenshot `Images/default-car-radio.png` was visually inspected. Play Mode was exited before building.

Windows build: **Succeeded**, 0 errors, 0 warnings, 41.53s (`Validation/defaults-build.json`). The latest AfterHours-TrafficGaps executable contains the new car/playlist code and all 27 source music files (`Validation/defaults-build-identity.json`). The fresh executable initialized Direct3D 12, stayed responsive, and logged no exception/missing-reference/shader/compiler-error matches (`Validation/defaults-player-startup.json`). Only the owned smoke process was stopped. No input, source audio/art, prefab/catalog, scene, package or Project Settings changes.
