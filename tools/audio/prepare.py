"""Export only selected, licensed library audio. All packs stay in external staging."""
import argparse, array, hashlib, json, math, subprocess, sys, wave
from pathlib import Path

RATE=44100
PACKS={
    'kenney': ('Kenney Sci-Fi Sounds','https://kenney.nl/assets/sci-fi-sounds','CC0'),
    'digital': ('Kenney Digital Audio','https://kenney.nl/assets/digital-audio','CC0'),
    'interface': ('Kenney Interface Sounds','https://kenney.nl/assets/interface-sounds','CC0'),
    'lrsf': ('Little Robot Sound Factory — Sci-Fi Sound Effects Library','https://opengameart.org/content/sci-fi-sound-effects-library','CC BY 3.0'),
}
# Mỗi family chọn chất liệu riêng; không dùng cùng tone sweep đổi pitch cho mọi cue.
SELECTED={
 'pulse':('lrsf/Laser_09.wav',.17,''), 'plasma':('kenney/Audio/laserLarge_002.ogg',.34,'equalizer=f=1800:t=q:w=0.8:g=5,acompressor=threshold=0.16:ratio=3:attack=1:release=55'),
 'homing':('kenney/Audio/thrusterFire_000.ogg',.48,'highpass=f=110,equalizer=f=600:t=q:w=0.8:g=3,acompressor=threshold=0.16:ratio=2:attack=2:release=60'), 'homing-impact':('kenney/Audio/explosionCrunch_001.ogg',.6,'highpass=f=90,equalizer=f=1700:t=q:w=1:g=3'),
 'player-hit':('kenney/Audio/impactMetal_001.ogg',.12,''), 'shield-absorb':('kenney/Audio/forceField_001.ogg',.19,''),
 'shield-break':('kenney/Audio/forceField_004.ogg',.4,''), 'player-death':('kenney/Audio/explosionCrunch_004.ogg',.85,''),
 'hostile-shot':('kenney/Audio/laserSmall_004.ogg',.12,''),
 'interceptor-charge':('lrsf/WarpDrive_02.wav',.65,'atrim=duration=0.65,areverse,highpass=f=280'),
 'interceptor-dash':('kenney/Audio/thrusterFire_003.ogg',.28,''),
 'sniper-charge':('lrsf/Laser_03.wav',.65,'atrim=duration=0.65,areverse,highpass=f=550'),
 'sniper-fire':('lrsf/Laser_00.wav',.32,''),
 'heavy-charge':('kenney/Audio/spaceEngineLarge_004.ogg',.55,'lowpass=f=1100'),
 'heavy-shot':('lrsf/Laser_06.wav',.42,'lowpass=f=4500'),
 'boss-enter':('lrsf/Robot_Activated_00.wav',.85,'lowpass=f=1800'),
 'boss-cannon-warning':('kenney/Audio/spaceEngineLarge_001.ogg',.6,'lowpass=f=800'),
 'boss-beam-warning':('lrsf/Alarm_Loop_00.wav',.65,''),
 'boss-curtain-warning':('kenney/Audio/forceField_000.ogg',.65,'highpass=f=400'),
 'boss-shelter-warning':('lrsf/Robot_Activated_00.wav',.65,'highpass=f=700'),
 'boss-escort-warning':('interface/Audio/glitch_003.ogg',.45,''),
 'overheat':('interface/Audio/error_006.ogg',.2,'highpass=f=1000'),
 'vent':('generated/pressure-release',.58,'highpass=f=1100,lowpass=f=10000,aeval=val(0)*exp(-5*t)'),
 'armor-break':('kenney/Audio/impactMetal_003.ogg',.3,''),
 'enemy-death':('kenney/Audio/explosionCrunch_000.ogg',.34,'highpass=f=140,equalizer=f=1900:t=q:w=0.8:g=4,acompressor=threshold=0.16:ratio=2.5:attack=2:release=60'),
 'heavy-death':('kenney/Audio/explosionCrunch_002.ogg',.62,'equalizer=f=1600:t=q:w=0.8:g=3,acompressor=threshold=0.16:ratio=2.5:attack=2:release=60'),
 'asteroid-break':('kenney/Audio/explosionCrunch_003.ogg',.3,'highpass=f=350,lowpass=f=6500,equalizer=f=2500:t=q:w=1:g=3'),
 'boss-burst':('kenney/Audio/explosionCrunch_003.ogg',.5,'highpass=f=80'),
 'boss-final':('kenney/Audio/lowFrequency_explosion_001.ogg',1.05,'highpass=f=75,equalizer=f=220:t=q:w=0.8:g=4,acompressor=threshold=0.18:ratio=2:attack=4:release=90'),
 'coin':('interface/Audio/glass_001.ogg',.2,'highpass=f=900,equalizer=f=3000:t=q:w=1:g=4'),
 'power-up':('digital/Audio/powerUp7.ogg',.4,''),
 'extra-life':('lrsf/Jingle_Achievement_00.wav',1.15,'atempo=1.3'),
 'confirm':('interface/Audio/confirmation_001.ogg',.14,''),
 'back':('interface/Audio/back_002.ogg',.14,''),
 'error':('interface/Audio/error_006.ogg',.24,''),
 'warp-start':('lrsf/WarpDrive_00.wav',.7,'atrim=duration=0.7,areverse,highpass=f=200'),
 'warp-pass':('lrsf/WarpDrive_01.wav',1.2,'atempo=1.8,highpass=f=300,equalizer=f=2000:t=q:w=0.8:g=3'),
 'warp-arrival':('lrsf/WarpDrive_02.wav',.75,'highpass=f=130,equalizer=f=1200:t=q:w=1:g=3'),
 'victory':('lrsf/Jingle_Win_00.wav',2.4,'atempo=1.35'),
 'game-over':('lrsf/Jingle_Lose_00.wav',2.9,'atempo=1.6'),
}

# Một ignition/crack layer ngắn; không tăng số runtime explosion voices.
LAYERS={
 'homing':('kenney/Audio/impactMetal_000.ogg',.14,.10),
 'homing-impact':('kenney/Audio/lowFrequency_explosion_001.ogg',.35,.50),
 'heavy-death':('kenney/Audio/lowFrequency_explosion_000.ogg',.25,.55),
 'boss-burst':('kenney/Audio/lowFrequency_explosion_000.ogg',.3,.40),
 'boss-final':('kenney/Audio/explosionCrunch_002.ogg',.55,.40),
}

def pcm(samples):
    data=array.array('h',(round(max(-1,min(1,x))*32767) for x in samples))
    if sys.byteorder!='little':data.byteswap()
    return data.tobytes()

def main():
    parser=argparse.ArgumentParser();parser.add_argument('source',type=Path);args=parser.parse_args()
    output=Path(__file__).resolve().parents[2]/'app/Assets/StarShift/Audio';(output/'SFX').mkdir(exist_ok=True);(output/'Music').mkdir(exist_ok=True)
    records=[]
    for name,(source,seconds,effect) in SELECTED.items():
        filters=['silenceremove=start_periods=1:start_threshold=-55dB:start_silence=0.003']
        if effect:filters.append(effect)
        filters.extend([f'atrim=duration={seconds}', 'asetpts=PTS-STARTPTS', 'highpass=f=65', 'afade=t=in:d=0.003',f'afade=t=out:st={seconds-.035}:d=0.035'])
        inputs=['-f','lavfi','-i','anoisesrc=color=white:seed=745:d=0.58'] if source.startswith('generated/') else ['-i',str(args.source/source)]
        layer=LAYERS.get(name)
        processing=['-af',','.join(filters)]
        if layer:
            layer_source,layer_gain,layer_seconds=layer
            inputs.extend(['-i',str(args.source/layer_source)])
            graph=f'[0:a]aformat=channel_layouts=mono[a];[1:a]aformat=channel_layouts=mono,highpass=f=120,atrim=duration={layer_seconds},volume={layer_gain}[b];[a][b]amix=inputs=2:duration=longest:normalize=0,'+','.join(filters)+'[mix]'
            processing=['-filter_complex',graph,'-map','[mix]']
        raw=subprocess.check_output(['ffmpeg','-v','error',*inputs,'-ac','1','-ar',str(RATE),*processing,'-f','f32le','-'])
        samples=array.array('f',raw)
        if sys.byteorder!='little':samples.byteswap()
        assert samples,source
        # Dù source ngắn hơn excerpt, giữ tail fade riêng để không phát hard cut.
        tail=min(round(.02*RATE),len(samples))
        for i in range(tail):samples[-tail+i]*=(tail-1-i)/max(1,tail-1)
        peak=max(abs(x) for x in samples);assert peak>.0001,source
        destination=output/'SFX'/(name+'.wav')
        with wave.open(str(destination),'wb') as w:w.setparams((1,2,RATE,0,'NONE','not compressed'));w.writeframes(pcm(x*.68/peak for x in samples))
        pack,url,license=('StarShift original sound design','tools/audio/prepare.py','Project original; no third-party sample') if source.startswith('generated/') else PACKS[source.split('/')[0]]
        records.append(dict(clip=name,source=source,pack=pack,url=url,license=license,edit=', '.join(filters)+'; final tail fade 20ms; mono; static peak -3.35 dBFS'))
        if layer: records[-1]['layer']=dict(source=layer[0],gain=layer[1],seconds=layer[2],highpass=120)
    for role,source,pack in [('hub','Short Space Casual Loop #1.wav','Music Loops Mini Set 2'),('combat','Short Space Casual Loop #3.wav','Music Loops Mini Set 2'),('boss','Drive With Me.wav','Music Loops Mini Set')]:
        with wave.open(str(args.source/source)) as w:
            assert w.getsampwidth()==2 and w.getnchannels()==2 and w.getframerate()==RATE
            samples=array.array('h',w.readframes(w.getnframes()))
        if sys.byteorder!='little':samples.byteswap()
        samples=[x/32768 for x in samples];peak=max(abs(x) for x in samples);rms=math.sqrt(sum(x*x for x in samples)/len(samples));gain=min(.68/peak,.11/rms)
        fade=round(.02*RATE)*2;joined=samples[fade:-fade]
        for i in range(fade):
            alpha=(i//2)/(fade//2-1);joined.append(samples[-fade+i]*(1-alpha)+samples[i]*alpha)
        destination=output/'Music'/('bgm-'+role+'.wav')
        with wave.open(str(destination),'wb') as w:w.setparams((2,2,RATE,0,'NONE','not compressed'));w.writeframes(pcm(x*gain for x in joined))
        records.append(dict(clip='bgm-'+role,source=source,pack='Andrew Sitkov / '+pack,license='Andrew Sitkov Royalty-Free Music License',edit='static RMS target -19.17 dBFS, peak <= -3.35 dBFS; 20ms tail/head loop splice; PCM source, Unity Vorbis streaming'))
    for r in records:
        paths=list(output.rglob(r['clip']+'.wav'));assert len(paths)==1
        r['sha256']=hashlib.sha256(paths[0].read_bytes()).hexdigest()
    assert len(records)==43
    Path(__file__).with_name('sources.json').write_text(json.dumps(records,indent=2)+'\n');print(f'Exported {len(records)} selected clips; one broadband pressure-release vent, no tonal procedural synth')

if __name__=='__main__':main()
