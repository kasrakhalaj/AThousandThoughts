"""Prepare short game cues from attributed CC0 gear-lever and throttle field recordings."""
from pathlib import Path
import os
import hashlib
import json
import subprocess
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(os.environ.get("HW2_SOURCE_AUDIO", ROOT.parent / "SourceAudio")) / "ManualGears"
OUTPUT = ROOT / 'Assets/Game/Audio/Recorded'
RATE = 44100
JOBS = [
    ('gear-lever', 'Recorded-gear-shift-1', .26, .66, .15),
    ('gear-lever', 'Recorded-gear-shift-2', 2.57, 3.07, .15),
    ('gear-lever', 'Recorded-gear-shift-3', 4.94, 5.36, .15),
    ('throttle-rev', 'Recorded-throttle-on', .60, 1.45, .13),
    ('throttle-rev', 'Recorded-downshift-blip', .70, 1.25, .13),
    ('throttle-rev', 'Recorded-throttle-off', 3.80, 4.70, .13),
]


def main():
    sources = {s['name']: s for s in json.loads((SOURCE / 'sources.json').read_text(encoding='utf-8'))}
    prepared = []
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for source, name, begin, end, target_rms in JOBS:
        raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', str(SOURCE / (source + '.mp3')),
            '-af', 'highpass=f=65,lowpass=f=10000', '-ac', '1', '-ar', str(RATE), '-f', 'f32le', '-'], capture_output=True, check=True).stdout
        audio = np.frombuffer(raw, dtype=np.float32).copy()[int(begin * RATE):int(end * RATE)]
        audio -= audio.mean()
        fade_in, fade_out = min(220, len(audio)), min(2205, len(audio))
        audio[:fade_in] *= np.linspace(0, 1, fade_in)
        audio[-fade_out:] *= np.linspace(1, 0, fade_out)
        audio *= min(target_rms / max(1e-8, float(np.sqrt(np.mean(audio ** 2)))), .72 / max(1e-8, float(np.max(np.abs(audio)))))
        path = OUTPUT / (name + '.wav')
        with wave.open(str(path), 'wb') as f:
            f.setnchannels(1); f.setsampwidth(2); f.setframerate(RATE); f.writeframes((audio * 32767).astype('<i2').tobytes())
        item = dict(sources[source], asset=str(path.relative_to(ROOT)).replace('\\', '/'), excerpt=[begin, end],
            durationSeconds=len(audio) / RATE, rms=float(np.sqrt(np.mean(audio ** 2))), peak=float(np.max(np.abs(audio))),
            sourceSHA256=hashlib.sha256((SOURCE / (source + '.mp3')).read_bytes()).hexdigest(),
            processing='HQ MP3 preview; mono 44.1kHz PCM16, 65Hz high-pass/10kHz low-pass, DC removal, edge fades and gain. No synthesized sound.')
        prepared.append(item)
        print(name, round(item['durationSeconds'], 3), round(item['rms'], 3), round(item['peak'], 3))
    (ROOT / 'Docs/ManualGearSoundSources.json').write_text(json.dumps(prepared, indent=2) + '\n', encoding='utf-8', newline='\n')


if __name__ == '__main__':
    main()
