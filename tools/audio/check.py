"""Check selected exports for corruption, clipping, abrupt tails, and source drift."""
import array, hashlib, json, math, subprocess, sys, wave
from pathlib import Path
root=Path(__file__).resolve().parents[2]
rows=json.loads(Path(__file__).with_name('sources.json').read_text())
results=[]
for row in rows:
    path=next((root/'app/Assets/StarShift/Audio').rglob(row['clip']+'.*'))
    assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'],f'Source drift: {path}'
    samples=array.array('f',subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','f32le','-ar','44100','-']))
    if sys.byteorder!='little': samples.byteswap()
    assert samples and all(math.isfinite(x) for x in samples),f'Invalid samples: {path}'
    peak=max(abs(x) for x in samples); rms=math.sqrt(sum(x*x for x in samples)/len(samples));dc=sum(samples)/len(samples)
    assert 0.02<peak<.9 and rms>.001,f'Silent/clipped export: {path} {peak}'
    if row["clip"] in {"plasma","coin","vent"}: assert rms>.08,f'Weak feedback body: {path} {rms}'
    assert abs(dc)<.03,f'DC offset: {path}'
    if '/SFX/' in str(path): assert abs(samples[-1])<.025,f'Hard tail: {path}'
    channels=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=channels','-of','json',str(path)]))['streams'][0]['channels']
    results.append(dict(clip=row['clip'],seconds=round(len(samples)/44100/channels,3),peak=round(peak,4),rms=round(rms,4),seamDelta=round(max(abs(samples[-channels+c]-samples[c]) for c in range(channels)),4)))
assert len(results)==44
print(json.dumps(dict(status='PASS',clips=results),indent=2))
