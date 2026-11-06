"""Prepare the R34 (Tristar) and R35 (docLorean) recording profiles.

Original recordings live in ../SourceAudio/GTR; see CREDITS.md for attribution
and the source licenses. Existing asset paths/metas are intentionally retained.
Requires ffmpeg and numpy. No synthesized engine sound is generated.
"""
from pathlib import Path
import hashlib
import json
import subprocess
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCES = ROOT.parent / "SourceAudio/GTR"
OUT = ROOT / "Assets/Game/Audio/Performance"
RATE = 44100
LABELS = ["idle", "mid", "load", "gas-on", "gas-off", "downshift"]
PROFILES = [
    {
        "car": "Tristar", "recording": "Nissan Skyline GT-R R34",
        "file": "R34-LowFps.opus", "author": "LowFps",
        "page": "https://commons.wikimedia.org/wiki/File:Nissan_Skyline_GT-R_-_engine_and_exhaust_sound_(YouTube_NpqxDM7pejs).opus",
        "original": "https://www.youtube.com/watch?v=NpqxDM7pejs",
        "license": "CC BY 3.0", "licenseURL": "https://creativecommons.org/licenses/by/3.0/",
        "highpass": 90, "lowpass": 6500,
        "spans": [(20.55,21.8),(33.5,34.7),(477.4,479.0),(23.85,24.45),(29.02,29.55),(19.45,20.05)],
    },
    {
        "car": "docLorean", "recording": "Nissan GT-R R35 2012",
        "file": "R35-PolePosition.wav", "author": "Pole Position Production",
        "page": "https://sonniss.com/gameaudiogdc/",
        "original": "Sonniss GDC 2020, Part 8: Pole Position - Nissan GT-R R35 2012/Nissan_GTR_t6_Onbrd_Drive_Ramps_Engine_Left_Valve_DPA4062.wav",
        "license": "Sonniss GDC Game Audio Bundle license v2.0 (2026-08-27)",
        "licenseURL": "https://sonniss.com/gdc-bundle-license/",
        "highpass": 65, "lowpass": 7500,
        "spans": [(2.0,4.5),(17.15,18.95),(146.1,147.2),(27.0,27.65),(148.1,148.8),(10.3,11.0)],
    },
]


def prepare():
    OUT.mkdir(parents=True, exist_ok=True)
    report = {"prepared": "2026-11-06", "processing": "Real recording excerpts, mono PCM16 44.1kHz, high/low pass, DC removal, 80ms loop overlap, cue fades, RMS normalization capped at 0.82 peak. Source vehicle recordings used as stylized voices for the fictional player cars.", "profiles": []}
    for profile in PROFILES:
        source = SOURCES / profile["file"]
        raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(source), "-af", f"highpass=f={profile['highpass']},lowpass=f={profile['lowpass']}", "-ac", "1", "-ar", str(RATE), "-f", "f32le", "-"], capture_output=True, check=True).stdout
        x = np.frombuffer(raw, np.float32)
        entry = {k: v for k, v in profile.items() if k not in ("spans",)}
        entry["sourceSHA256"] = hashlib.sha256(source.read_bytes()).hexdigest()
        entry["clips"] = []
        for index, ((a, b), label) in enumerate(zip(profile["spans"], LABELS)):
            y = x[int(a*RATE):int(b*RATE)].copy()
            if len(y) < RATE/4:
                raise ValueError(f"Missing or short excerpt: {profile['car']} {label}")
            y -= y.mean()
            if index < 3:
                n = int(.08*RATE)
                blend = np.linspace(0, 1, n)
                bridge = y[-n:]*(1-blend) + y[:n]*blend
                y = np.concatenate([y[n:-n], bridge])
            else:
                n = int(.008*RATE); y[:n] *= np.linspace(0, 1, n)
                n = int(.055*RATE); y[-n:] *= np.linspace(1, 0, n)
            y *= min(.14/max(1e-8, float(np.sqrt(np.mean(y*y)))), .82/max(1e-8, float(np.max(np.abs(y)))))
            path = OUT / f"{profile['car']}-{label}.wav"
            pcm = (y*32767).astype("<i2")
            with wave.open(str(path), "wb") as w:
                w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE); w.writeframes(pcm.tobytes())
            entry["clips"].append({"asset": path.relative_to(ROOT).as_posix(), "excerpt": [a,b], "loop": index < 3, "duration": len(y)/RATE, "rms": float(np.sqrt(np.mean(y*y))), "peak": float(np.max(np.abs(y))), "assetSHA256": hashlib.sha256(path.read_bytes()).hexdigest()})
        report["profiles"].append(entry)
    (ROOT / "Docs/PerformanceCarSoundSources.json").write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
    print("Prepared 12 R34/R35 clips; existing Unity GUIDs preserved.")


if __name__ == "__main__":
    prepare()
