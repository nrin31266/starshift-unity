using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Audio;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.UI;
using UnityEditor;
using UnityEngine;

public sealed class AudioVerifyPointer : PlayerPointerInput
{
    public bool Held;
    public override bool FireHeld => Held;
    public override Vector2 ReadDelta()=>Vector2.zero;
    public override void ResetDrag(){}
}

public static class VerifyAudioFinal
{
    const string Output="../.design/audio-final/";
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static Task current;
    static EncounterRunner runner;
    static AudioDirector audio;
    static GameShell shell;
    static ShellView view;
    static PlayerGameplayGate gate;
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Fields).GetValue(o);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static async Task Until(Func<bool> done,int ms,string message)
    {float end=Time.realtimeSinceStartup+ms/1000f;while(!done()&&Time.realtimeSinceStartup<end)await Task.Delay(15);Check(done(),message);}
    static AudioSource Music => audio.GetComponentsInChildren<AudioSource>().Single(s=>s.isPlaying&&s.clip==audio.CurrentMusic);
    static AudioSource[] Voices=>audio.GetComponentsInChildren<AudioSource>().Where(s=>s.name.StartsWith("Voice")).ToArray();
    static void Evidence(string name,object result)=>File.WriteAllText(Output+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
    public static object Edit()
    {
        Check(!Application.isPlaying,"Edit Mode required");Directory.CreateDirectory(Output);
        var palette=AssetDatabase.LoadAssetAtPath<AudioPalette>("Assets/StarShift/Audio/AudioPalette.asset");
        Check(palette.Cues.Length==Enum.GetValues(typeof(AudioCue)).Length,"Cue coverage");
        Check(palette.Cues.All(c=>c.Clip!=null&&c.Volume>0&&c.MaxVoices>0),"Invalid cue");
        Check(palette.Cues.Select(c=>c.Id).Distinct().Count()==palette.Cues.Length,"Duplicate cue IDs");
        foreach(var id in new[]{AudioCue.InterceptorCharge,AudioCue.SniperCharge,AudioCue.Overheat,AudioCue.BossBeamWarning,AudioCue.BossCurtainWarning,AudioCue.BossShelterWarning,AudioCue.BossEscortWarning})
        {var cue=palette.Cues.Single(c=>c.Id==id);Check(cue.Tier==0&&cue.Variation==0,"Unstable warning "+id);}
        var clips=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/StarShift/Audio"});Check(clips.Length==43,"Unused/missing audio exports");
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/StarShift/Prefabs"}))
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach(var h in root.GetComponentsInChildren<EnemyHealth>(true))Check(h.GetComponent<EnemyAudioPresentation>()!=null,"Enemy binding "+root.name);
            foreach(var m in root.GetComponentsInChildren<BossAttackModule>(true))Check(m.GetComponent<EnemyAudioPresentation>()!=null,"Boss binding "+root.name);
        }
        var result=new{status="PASS",clips=clips.Length,cues=palette.Cues.Length};Evidence("edit",result);return result;
    }
    public static object Begin()
    {
        Check(Application.isPlaying,"Play Mode required");Check(current==null||current.IsCompleted,"Audio probe already running");
        Directory.CreateDirectory(Output);Evidence("runtime",new{status="RUNNING"});current=Run(false,"all");return new{started=true};
    }
    public static object BeginCombat(string scope="all")
    {
        Check(Application.isPlaying,"Play Mode required");Check(current==null||current.IsCompleted,"Audio probe already running");
        Directory.CreateDirectory(Output);Evidence("combat",new{status="RUNNING"});current=Run(true,scope);return new{started=true};
    }
    public static object BeginFinal()
    {
        Check(Application.isPlaying,"Play Mode required");Check(current==null||current.IsCompleted,"Audio probe already running");
        Directory.CreateDirectory(Output);Evidence("final",new{status="RUNNING",scope="flow"});current=Final();return new{started=true};
    }
    static async Task Final()
    {
        try
        {
            await Run(false,"all");Check(Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Output+"runtime.json"))["status"].ToString()=="PASS","Flow failed");
            foreach(var scope in new[]{"player","enemies","travel","boss"})
            {
                Evidence("final",new{status="RUNNING",scope});await Run(true,scope);
                var result=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Output+"combat.json"));Check(result["status"].ToString()=="PASS",scope+" failed: "+result);
                Check(Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Output+"save-guard.json"))["status"].ToString()=="PASS","Save bytes changed");
            }
            Evidence("final",new{status="PASS",scopes=new[]{"flow","player","enemies","travel","boss"},productionSaveBytesUnchanged=true,listening="User BGM candidates positive; final subjective mix NOT VERIFIED"});
        }
        catch(Exception e){Evidence("final",new{status="FAIL",error=e.ToString()});}
    }
    static async Task Run(bool combatOnly,string scope)
    {
        string original=null,temp=null;byte[][] guard=null;LevelDefinition originalLevel=null;
        CampaignLifecycle save=null;
        try
        {
            runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();audio=runner.GetComponent<AudioDirector>();shell=runner.GetComponent<GameShell>();
            view=UnityEngine.Object.FindAnyObjectByType<ShellView>();gate=UnityEngine.Object.FindAnyObjectByType<PlayerGameplayGate>();save=runner.GetComponent<CampaignLifecycle>();
            Check(!runner.HasActiveRun&&!shell.IsBusy,"Idle Menu required");
            original=save.SavePath;originalLevel=runner.Flow.Level;guard=new[]{"",".bak",".invalid"}.Select(s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null).ToArray();
            temp=Path.Combine(Path.GetTempPath(),"starshift-audio-"+Guid.NewGuid().ToString("N"),"save.json");save.UseTestStorage(temp);shell.Boot();
            if(combatOnly){await Combat(scope);return;}
            await Task.Delay(1100);var clip=audio.CurrentMusic;var musicSource=Music;float musicTime=Music.time;
            view.Hangar.onClick.Invoke();await Task.Delay(220);view.StoreMode.onClick.Invoke();await Task.Delay(220);
            view.BackButtons.First(b=>b.gameObject.activeInHierarchy).onClick.Invoke();view.Settings.onClick.Invoke();await Task.Delay(220);
            Check(audio.CurrentMusic==clip&&Music==musicSource&&Music.time>musicTime,"Hub restarted during navigation");
            view.BackButtons.First(b=>b.gameObject.activeInHierarchy).onClick.Invoke();
            audio.Apply(new SettingsData{MusicVolume=0,SfxVolume=0});audio.Play(AudioCue.Confirm);await Task.Delay(80);
            Check(audio.GetComponentsInChildren<AudioSource>().All(s=>!s.isPlaying||s.volume==0),"Volume setting not applied to all channels");audio.Apply(SettingsData.Defaults());
            int frame=Time.frameCount;await Task.Delay(100);Check(Time.frameCount>frame,"Editor frozen");
            view.NewGame.onClick.Invoke();await Until(()=>!shell.IsBusy&&shell.Screen==ShellScreen.Playing,5000,"New Game");
            await Until(()=>runner.Flow.CanContinue,4000,"Intro ready");runner.GetComponent<GameplaySession>().BeginMission();
            await Until(()=>runner.Stage==EncounterStage.Running,6000,"Combat started");
            gate.GetComponent<PlayerDamageLifecycle>().GrantBriefProtection(1000);await Task.Delay(1000);
            Check(audio.CurrentMusic.name=="bgm-combat","Combat BGM");
            var session=runner.GetComponent<GameplaySession>();musicTime=Music.time;clip=audio.CurrentMusic;musicSource=Music;
            session.Pause();await Task.Delay(500);Check(Music.time>musicTime&&Music.volume<.08f,"Pause stopped/restarted music or failed duck");
            session.Resume();await Task.Delay(700);Check(audio.CurrentMusic==clip&&Music==musicSource&&Music.volume>Field<AudioPalette>(audio,"palette").GameplayMusicLevel*.85f,"Resume music restoration");
            audio.ClearGameplay();int accepted=audio.AcceptedCues;for(int i=0;i<100;i++)audio.Play(AudioCue.Coin);Check(audio.AcceptedCues==accepted+1,"Coin spam");
            audio.ClearGameplay();for(int i=0;i<2;i++){audio.Play(AudioCue.Pulse);audio.Play(AudioCue.Plasma);audio.Play(AudioCue.HeavyDeath);audio.Play(AudioCue.HomingImpact);}
            Check(audio.Play(AudioCue.InterceptorCharge),"Interceptor starved by common SFX");Check(audio.Play(AudioCue.BossBeamWarning),"Boss warning starved");
            await Task.Delay(50);var warnings=Voices.Where(s=>s.isPlaying&&s.priority==0).ToArray();
            Check(warnings.Length==2&&warnings.All(s=>s.pitch==1&&s.outputAudioMixerGroup.name=="SFX"),"Warning priority/palette/routing");
            Check(Voices.Length==16&&Voices.All(s=>!s.loop),"Unbounded/looping SFX sources");
            Evidence("pressure",new{status="PASS",reservedWarningVoices=warnings.Length,voicePool=Voices.Length});
            await Task.Delay(1000);musicTime=Music.time;clip=audio.CurrentMusic;
            var lives=gate.GetComponent<PlayerLives>();var damage=gate.GetComponent<PlayerDamageLifecycle>();gate.GetComponent<PlayerTemporaryEffects>().Clear();damage.RestoreCheckpoint(3);gate.SetCinematicLock(false);
            Check(damage.TryReceiveHit(null),"Life loss: state="+gate.State+", canDamage="+gate.CanTakeDamage+", lives="+lives.CurrentLives+", scale="+Time.timeScale);await Task.Delay(150);Check(audio.CurrentMusic==clip&&Music.time>musicTime,"Life loss restarted music");
            await Until(()=>gate.State==PlayerGameplayState.PostEntryInvincibility,5000,"Respawn");
            lives.RestoreCheckpoint(1);gate.SetState(PlayerGameplayState.Active);Check(damage.TryReceiveHit(null),"Fatal death");
            await Until(()=>session.IsTerminal&&shell.Screen==ShellScreen.Results,2000,"Game Over");await Task.Delay(500);
            Check(audio.CurrentMusic==clip&&Music.isPlaying&&Music.volume<.04f,"Game Over continuity/duck");
            Check(Voices.Any(s=>s.isPlaying&&s.clip.name=="game-over"),"Game Over stinger");
            view.GameOverRetry.onClick.Invoke();await Until(()=>!shell.IsBusy&&!session.IsTerminal,5000,"Retry");await Task.Delay(1000);
            Check(audio.CurrentMusic.name=="bgm-combat"&&Music.volume>Field<AudioPalette>(audio,"palette").GameplayMusicLevel*.85f,"Retry music recovery");
            shell.Boot();await Task.Delay(1000);Check(audio.CurrentMusic.name=="bgm-hub","Return to Hub");
            Check(!Voices.Take(14).Any(s=>s.isPlaying),"Stuck gameplay audio after Menu");
            Evidence("flow",new{status="PASS",hubNavigation=true,settings=true,pauseResume=true,deathRespawn=true,gameOverRetry=true,menuCleanup=true});
            Evidence("runtime",new{status="PASS",automatedPlayback=true,listening="NOT VERIFIED"});
        }
        catch(Exception e){Evidence(combatOnly?"combat":"runtime",new{status="FAIL",error=e.ToString()});}
        finally
        {
            if(save!=null&&original!=null)
            {
                await Until(()=>!shell.IsBusy,5000,"Shell cleanup");Time.timeScale=1;runner.GetComponent<RegionCampaignFlow>().Cancel();runner.StopRun();save.ExitIsolated();runner.Flow.LoadLevel(originalLevel);save.UseTestStorage(original);shell.Boot();
                string[] suffix={"",".bak",".invalid"};bool unchanged=true;
                for(int i=0;i<suffix.Length;i++){var after=File.Exists(original+suffix[i])?File.ReadAllBytes(original+suffix[i]):null;unchanged&=guard[i]==null?after==null:after!=null&&guard[i].SequenceEqual(after);}
                Evidence("save-guard",new{status=unchanged?"PASS":"FAIL",productionBytesUnchanged=unchanged});
                if(temp!=null&&Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
            }
        }
    }
    static bool Hearing(string clip)=>Voices.Any(s=>s.isPlaying&&s.clip.name==clip);
    static async Task Start(string mission,string point="LEVEL_START")
    {
        var region=runner.GetComponent<RegionCampaignFlow>();region.Cancel();runner.StopRun();
        runner.Flow.LoadLevel(region.FindMission(mission));gate.GetComponent<PlayerDamageLifecycle>().RestoreCheckpoint(3);
        runner.StartRun(point,true,true,null,null);gate.SetCinematicLock(false);gate.GetComponent<PlayerDamageLifecycle>().GrantBriefProtection(1000);
        view.Show(ShellScreen.Playing);Time.timeScale=1;UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);await Task.Delay(70);
    }
    static async Task MixPressure()
    {
        audio.ClearGameplay(); audio.IsPaused=false;
        audio.Apply(new SettingsData{MusicVolume=0,SfxVolume=1});
        try
        {
            foreach(var cue in new[]{AudioCue.HeavyDeath,AudioCue.EnemyDeath,AudioCue.AsteroidBreak,AudioCue.HomingImpact})Check(audio.Play(cue),"Common pressure "+cue);
            Check(audio.Play(AudioCue.Plasma),"Plasma starved by explosions");
            Check(audio.Play(AudioCue.Coin),"Coin starved by explosions");
            var plasma=Voices.Single(v=>v.isPlaying&&v.clip.name=="plasma");
            var coin=Voices.Single(v=>v.isPlaying&&v.clip.name=="coin");
            float plasmaNormal=plasma.volume,coinNormal=coin.volume;
            Check(plasmaNormal>=.48f&&coinNormal>=.34f,"Weapon/pickup gain collapsed under common pressure");
            Check(audio.Play(AudioCue.Overheat)&&audio.Play(AudioCue.Vent),"Heat cues starved");
            Check(plasma.volume>=.26f&&coin.volume>=.22f,"Warning over-ducked weapon/pickup");
            Check(Voices.Single(v=>v.isPlaying&&v.clip.name=="vent").volume>=.5f,"Vent lost under warning");
            var data=new float[1024];float peak=0;double energy=0;int samples=0;
            for(int i=0;i<40;i++)
            {
                AudioListener.GetOutputData(data,0);
                foreach(float value in data){peak=Mathf.Max(peak,Mathf.Abs(value));energy+=value*value;samples++;}
                await Task.Delay(10);
            }
            Check(peak>.03f&&peak<.98f,"Stress mix silent/clipped "+peak);
            Evidence("mix",new{status="PASS",plasmaNormal,coinNormal,stressPeak=peak,stressRms=Math.Sqrt(energy/samples),musicMuted=true,reservedWeaponAndPickup=true,listening="NOT VERIFIED"});
        }
        finally{audio.ClearGameplay();audio.Apply(SettingsData.Defaults());}
    }
    static async Task Combat(string scope)
    {
        if(scope=="mix") await MixPressure();
        if(scope=="all"||scope=="player")
        {
        await Start("L1");var weapon=gate.GetComponent<PulseLaserWeapon>();var loadout=gate.GetComponent<WeaponLoadout>();
        var oldPointer=Field<PlayerPointerInput>(weapon,"pointerInput");var pointer=gate.gameObject.AddComponent<AudioVerifyPointer>();
        try
        {
            typeof(PulseLaserWeapon).GetField("pointerInput",Fields).SetValue(weapon,pointer);
            foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})
            {
                if(branch!=WeaponBranch.Pulse)loadout.ApplyCore(branch,true);
                int before=weapon.ShotsFired;pointer.Held=true;
                await Until(()=>weapon.ShotsFired>before,1000,"Weapon did not fire "+branch);
                string clip=branch==WeaponBranch.Homing?"homing":branch.ToString().ToLowerInvariant();
                await Until(()=>Hearing(clip),800,"Weapon cue "+branch);pointer.Held=false;await Task.Delay(400);
            }
            loadout.Cycle();pointer.Held=true;await Until(()=>weapon.IsOverheated,15000,"Heat MAX");
            await Until(()=>Hearing("overheat"),300,"Overheat warning");await Until(()=>Hearing("vent"),600,"Single vent cue");pointer.Held=false;
            var receiver=gate.GetComponent<PlayerPickupReceiver>();
            foreach(var id in new[]{"StellarShard","ShieldCell","ExtraLife"})
            {
                if(id=="ExtraLife")gate.GetComponent<PlayerLives>().RestoreCheckpoint(2);
                Check(receiver.TryCollect(AssetDatabase.LoadAssetAtPath<StarShift.Rewards.PickupDefinition>("Assets/StarShift/Data/Rewards/"+id+".asset")),"Pickup "+id);await Task.Delay(140);
            }
            var damage=gate.GetComponent<PlayerDamageLifecycle>();gate.SetState(PlayerGameplayState.Active);Check(damage.TryReceiveHit(null),"Shield absorb");
            await Until(()=>Hearing("shield-break"),300,"Shield break cue");
        }
        finally{pointer.Held=false;typeof(PulseLaserWeapon).GetField("pointerInput",Fields).SetValue(weapon,oldPointer);UnityEngine.Object.Destroy(pointer);}
        Evidence("player",new{status="PASS",weapons=true,heatVent=true,shieldPickup=true});
        }
        if(scope=="all"||scope=="enemies")
        {
            await Start("L1");runner.StopRun();gate.SetState(PlayerGameplayState.Active);gate.SetCinematicLock(false);
            gate.GetComponent<PlayerDamageLifecycle>().GrantBriefProtection(1000);
            foreach(var name in new[]{"Interceptor","Sniper","Bulwark"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/Enemies/"+name+".prefab");
                var root=UnityEngine.Object.Instantiate(prefab);root.SetActive(false);
                try
                {
                    var flight=root.GetComponent<EntryHoldFlight>();flight.Configure(new Vector2(.5f,.75f),new Vector2(.5f,.75f),Vector2.zero,.02f,new Vector2(.5f,1.3f),60,0);
                    var attack=root.GetComponent<SingleFireAttack>();attack.SetEncounterControlled();root.SetActive(true);
                    int cycles=name=="Interceptor"?2:1;
                    for(int cycle=0;cycle<cycles;cycle++)
                    {
                        if(cycle>0){root.SetActive(false);root.SetActive(true);}
                        await Until(()=>flight.IsHolding,2000,"Enemy hold "+name);await Task.Delay(400);audio.ClearGameplay();
                        audio.Play(AudioCue.Pulse);audio.Play(AudioCue.Plasma);audio.Play(AudioCue.HeavyDeath);audio.Play(AudioCue.HomingImpact);
                        bool started=false;await Until(()=>started||(started=attack.TryRequestAttack()),4000,"Enemy request "+name);
                        var charge=name=="Interceptor"?"interceptor-charge":name=="Sniper"?"sniper-charge":"heavy-charge";
                        await Until(()=>Hearing(charge),500,"Enemy warning "+name);
                        if(name!="Bulwark")Check(Voices.Single(v=>v.isPlaying&&v.clip.name==charge).priority==0,"Critical enemy priority "+name);
                        var shot=name=="Interceptor"?"interceptor-dash":name=="Sniper"?"sniper-fire":"heavy-shot";
                        await Until(()=>Hearing(shot),3000,"Enemy attack audio "+name);await Task.Delay(700);
                    }
                }
                finally{UnityEngine.Object.Destroy(root);}
            }
            Evidence("enemies",new{status="PASS",interceptorPressureAndReuse=true,sniperAndBulwark=true});
        }
        if(scope=="all"||scope=="travel")
        {
        foreach(var mission in new[]{"L1","L4"})
        {
            await Start(mission);runner.StopRun();gate.SetState(PlayerGameplayState.Active);gate.SetCinematicLock(false);
            var region=runner.GetComponent<RegionCampaignFlow>();var target=region.NextMission;await Until(()=>audio.CurrentMusic!=null&&audio.CurrentMusic.name=="bgm-combat",2500,"Combat before travel");var source=Music;var clip=audio.CurrentMusic;
            Check(region.QueueAdvance(),"Travel start "+mission);await Task.Delay(500);Check(audio.CurrentMusic==clip&&Music==source,"Travel restarted Combat "+mission);
            if(mission=="L4")
            {var journey=runner.GetComponent<PlanetJourney>();await Until(()=>journey.IsAwaitingTap,11000,"Region reveal");Check(journey.AcceptTap(),"Region tap");}
            await Until(()=>runner.Flow.Level==target&&runner.HasActiveRun,10000,"Travel arrival "+mission);
        }
        Evidence("travel",new{status="PASS",missionAndRegion=true});
        }
        if(scope=="all"||scope=="boss")
        {
        foreach(var mission in new[]{"L8","L4"})
        {
            await Start(mission,"BOSS");await Until(()=>runner.GetComponentInChildren<BossPhaseRunner>()?.GetComponent<EntryHoldFlight>().IsHolding==true,12000,"Boss entry "+mission);
            var driver=runner.GetComponentInChildren<BossPhaseRunner>();driver.enabled=false;var boss=driver.Health;
            Check(audio.CurrentMusic.name=="bgm-boss","Boss BGM "+mission);
            foreach(var module in boss.GetComponentsInChildren<BossAttackModule>())
            {
                driver.CancelModules();UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ClearProjectiles();audio.ClearGameplay();
                if(!module.gameObject.activeInHierarchy)continue;
                int fired=module.ShotsFired;bool started=false;await Until(()=>started||(started=module.BeginAttack()),4000,"Boss skill start "+module.name);
                await Until(()=>Voices.Any(s=>s.isPlaying&&s.priority==0&&s.clip.name.StartsWith("boss-")),700,"Boss warning "+module.name);
                await Until(()=>module.CurrentPhase==AttackPhase.Attack||module.ShotsFired>fired,4000,"Boss skill fire "+module.name);await Task.Delay(160);
            }
            driver.CancelModules();audio.ClearGameplay();boss.SetDamageBlocked(false);boss.ReceivePlayerHit(boss.MaxHp,gate.gameObject);
            await Until(()=>Hearing("boss-final"),3500,"Staged boss final "+mission);
            if(mission=="L4")
            {
                var sequence=boss.GetComponent<BossDeathSequence>();
                // Ép bỏ qua cửa sổ LateUpdate cuối để kiểm completion cue không phụ thuộc frame.
                typeof(BossDeathSequence).GetField("elapsed",Fields).SetValue(sequence,Field<float>(sequence,"duration")-.001f);
            }
            await Until(()=>Hearing("victory"),1800,"Victory stinger "+mission);
        }
        Evidence("boss",new{status="PASS",skillsAndStagedVictory=true,completionEventAndSkippedLastFrame=true});
        }
        var data=new float[2048];float peak=0;
        for(int i=0;i<35;i++){AudioListener.GetOutputData(data,0);peak=Mathf.Max(peak,data.Max(x=>Mathf.Abs(x)));await Task.Delay(15);}
        Check(peak>0&&peak<1,"Silent/clipped DSP output "+peak);
        shell.Boot();await Task.Delay(1000);Check(!Voices.Take(14).Any(s=>s.isPlaying),"Combat cleanup");
        Evidence("combat",new{status="PASS",scope,dspSamplePeak=peak,listening="NOT VERIFIED"});
    }

}
