"""Prepare engine loops and ignition from attributed CC0 field recordings."""
from pathlib import Path
import json
import subprocess
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT.parent / 'SourceAudio' / 'FieldRecordings'
OUTPUT = ROOT / 'Assets' / 'Game' / 'Audio' / 'Recorded'
RATE = 44100
JOBS = [
    ('ignition', 'Recorded-ignition', 0, 2.7, 0, .13),
    ('player-engine', 'Recorded-player-idle', 26, 29.5, .20, .14),
    ('player-engine', 'Recorded-player-mid', 52, 54.5, .20, .14),
    ('player-engine', 'Recorded-player-load', 47.1, 49.2, .16, .14),
    ('traffic-engine', 'Recorded-traffic-engine', 5.5, 9.5, .25, .12),
]


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    sources = {item['name']: item for item in json.loads((SOURCE / 'sources.json').read_text())}
    prepared = []
    for source, name, start, end, crossfade, rms_target in JOBS:
        process = subprocess.run([
            'ffmpeg', '-v', 'error', '-i', str(SOURCE / (source + '.mp3')),
            '-af', 'highpass=f=45,lowpass=f=9000', '-ac', '1', '-ar', str(RATE), '-f', 'f32le', '-'
        ], capture_output=True, check=True)
        samples = np.frombuffer(process.stdout, dtype=np.float32).copy()[int(start * RATE):int(end * RATE)]
        samples -= samples.mean()
        if crossfade:
            count = int(crossfade * RATE)
            amount = np.linspace(0, 1, count, dtype=np.float32)
            join = samples[-count:] * (1 - amount) + samples[:count] * amount
            samples = np.concatenate((samples[count:-count], join))
        else:
            samples[:441] *= np.linspace(0, 1, 441)
            samples[-2205:] *= np.linspace(1, 0, 2205)
        gain = min(rms_target / max(1e-8, float(np.sqrt(np.mean(samples ** 2)))), .86 / max(1e-8, float(np.max(np.abs(samples)))))
        samples *= gain
        target = OUTPUT / (name + '.wav')
        with wave.open(str(target), 'wb') as output:
            output.setnchannels(1)
            output.setsampwidth(2)
            output.setframerate(RATE)
            output.writeframes((samples * 32767).astype('<i2').tobytes())
        prepared.append(dict(sources[source], asset=str(target.relative_to(ROOT)).replace('\\', '/'),
                             excerpt=[start, end], crossfadeSeconds=crossfade,
                             durationSeconds=len(samples) / RATE, rms=float(np.sqrt(np.mean(samples ** 2))),
                             processing='HQ MP3 preview decoded; mono, 45 Hz high-pass, 9 kHz low-pass, gain, loop crossfade or edge fades'))
        print(f'{name}: {len(samples) / RATE:.2f}s, RMS {np.sqrt(np.mean(samples ** 2)):.3f}')
    (ROOT / 'Docs' / 'RecordedSoundSources.json').write_text(json.dumps(prepared, indent=2) + '\n', encoding='utf-8', newline='\n')


if __name__ == '__main__':
    main()
