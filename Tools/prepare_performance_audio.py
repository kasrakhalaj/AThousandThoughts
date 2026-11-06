"""Prepare two stylized performance-car voices from a CC0 Ferrari field recording."""
from pathlib import Path
import json,hashlib,subprocess,wave
import numpy as np
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT.parent/'SourceAudio/PerformanceCars/370278.mp3';OUT=ROOT/'Assets/Game/Audio/Performance';OUT.mkdir(parents=True,exist_ok=True);RATE=44100
x=np.frombuffer(subprocess.run(['ffmpeg','-v','error','-i',str(SOURCE),'-af','highpass=f=65,lowpass=f=8500','-ac','1','-ar',str(RATE),'-f','f32le','-'],capture_output=True,check=True).stdout,np.float32)
jobs={'Tristar':[(114.2,116.3),(0.3,1.8),(10.2,11.8),(9.4,10.2),(12.8,13.5),(9.6,10.1)],'docLorean':[(116.5,118.7),(128.1,129.9),(170.2,172.2),(169.3,170),(172.4,173.2),(169.5,170.1)]};report=[]
for car,spans in jobs.items():
 for index,((a,b),label) in enumerate(zip(spans,['idle','mid','load','gas-on','gas-off','downshift'])):
  y=x[int(a*RATE):int(b*RATE)].copy();y-=y.mean()
  if index<3:
   n=int(.12*RATE);blend=np.linspace(0,1,n);bridge=y[-n:]*(1-blend)+y[:n]*blend;y=np.concatenate([y[n:-n],bridge])
  else:
   n=220;y[:n]*=np.linspace(0,1,n);n=2205;y[-n:]*=np.linspace(1,0,n)
  y*=min(.14/max(1e-8,float(np.sqrt(np.mean(y*y)))),.82/max(1e-8,float(np.max(np.abs(y)))))
  p=OUT/f'{car}-{label}.wav'
  with wave.open(str(p),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(RATE);w.writeframes((y*32767).astype('<i2').tobytes())
  report.append({'asset':p.relative_to(ROOT).as_posix(),'excerpt':[a,b],'loop':index<3,'duration':len(y)/RATE,'rms':float(np.sqrt(np.mean(y*y))),'peak':float(np.max(np.abs(y)))})
(ROOT/'Docs/PerformanceCarSoundSources.json').write_text(json.dumps({'page':'https://freesound.org/people/biholao/sounds/370278/','author':'biholao','title':'Acceleration 1.wav','license':'CC0 1.0','sourceSHA256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'processing':'HQ preview, mono PCM16 44.1kHz, high/low pass, DC removal, 120ms loop overlap, cue fades, gain normalization. Stylized voices, not make-matched recordings.','clips':report},indent=2)+'\n')
