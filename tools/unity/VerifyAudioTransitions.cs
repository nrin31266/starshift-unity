using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarShift.Audio;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class AudioTransitionObserver:MonoBehaviour
{
    public AudioDirector Audio;
    readonly Dictionary<AudioSource,(bool playing,AudioClip clip,float time)> previous=new Dictionary<AudioSource,(bool,AudioClip,float)>();
    public readonly Dictionary<string,int> Starts=new Dictionary<string,int>();
    public int Count(string clip)=>Starts.TryGetValue(clip,out int count)?count:0;
    void LateUpdate()
    {
        if(Audio==null)return;
        foreach(var source in Audio.GetComponentsInChildren<AudioSource>().Where(s=>!s.loop))
        {
            previous.TryGetValue(source,out var old);
            if(source.isPlaying&&source.clip!=null&&(!old.playing||old.clip!=source.clip||source.time+.01f<old.time))
            {string name=source.clip.name;Starts[name]=Count(name)+1;}
            previous[source]=(source.isPlaying,source.clip,source.clip!=null?source.time:0);
        }
    }
}
public static class VerifyAudioTransitions
{
    const string Output="../.design/audio-followup/";
    static Task current;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Evidence(string name,object value){Directory.CreateDirectory(Output);File.WriteAllText(Output+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));}
    static async Task Until(Func<bool> done,int ms,string message)
    {float end=Time.realtimeSinceStartup+ms/1000f;while(!done()&&Time.realtimeSinceStartup<end)await Task.Delay(10);Check(done(),message);}
    public static object Begin()
    {
        Check(Application.isPlaying,"Play Mode required");Check(current==null||current.IsCompleted,"Probe busy");
        Evidence("transitions",new{status="RUNNING"});current=Run();return new{started=true};
    }
    static async Task Run()
    {
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();var audio=runner.GetComponent<AudioDirector>();
        var save=runner.GetComponent<CampaignLifecycle>();var shell=runner.GetComponent<GameShell>();var region=runner.GetComponent<RegionCampaignFlow>();
        var session=runner.GetComponent<GameplaySession>();var view=UnityEngine.Object.FindAnyObjectByType<ShellView>();
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerGameplayGate>();var damage=player.GetComponent<PlayerDamageLifecycle>();
        var originalLevel=runner.Flow.Level;string original=null,temp=null;byte[][] before=null;AudioTransitionObserver monitor=null;
        try
        {
            Check(!runner.HasActiveRun&&!shell.IsBusy,"Idle Menu required");
            original=save.SavePath;before=new[]{"",".bak",".invalid"}.Select(s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null).ToArray();
            temp=Path.Combine(Path.GetTempPath(),"starshift-audio-flow-"+Guid.NewGuid().ToString("N"),"save.json");save.UseTestStorage(temp);shell.Boot();
            monitor=runner.gameObject.AddComponent<AudioTransitionObserver>();monitor.Audio=audio;await Task.Delay(150);
            view.NewGame.onClick.Invoke();await Until(()=>!shell.IsBusy&&runner.Flow.CanContinue,5000,"New Game briefing");
            await Task.Delay(1300);Check(monitor.Count("warp-start")==0,"Ignition played before start tap");
            Check(audio.CurrentMusic.name=="bgm-hub","Briefing left Hub before tap");
            session.BeginMission();await Until(()=>monitor.Count("warp-start")==1,500,"No ignition on start tap");
            await Until(()=>runner.Stage==EncounterStage.Running,6000,"Combat entry");damage.GrantBriefProtection(1000);
            Check(monitor.Count("warp-start")==1,"Duplicate initial ignition");
            session.Pause();await Task.Delay(250);session.Resume();await Task.Delay(250);Check(monitor.Count("warp-start")==1,"Pause resumed ignition");
            shell.Boot();await Task.Delay(200);int continued=monitor.Count("warp-start");view.Continue.onClick.Invoke();
            await Until(()=>!shell.IsBusy&&runner.HasActiveRun&&runner.Stage==EncounterStage.Running,5000,"Checkpoint Continue");
            damage.GrantBriefProtection(1000);await Task.Delay(1300);Check(monitor.Count("warp-start")==continued,"Continue replayed ignition");
            // Về đầu L1 để đo tiếng gió đúng pha bay, không phát trong clear pause.
            region.Cancel();runner.StopRun();runner.Flow.LoadLevel(region.FindMission("L1"));runner.StartRun("ENCOUNTER_1",false,true,null,null);
            player.SetCinematicLock(false);damage.GrantBriefProtection(1000);view.Show(ShellScreen.Playing);Time.timeScale=1;
            int windBefore=monitor.Count("transition-wind");
            await Until(()=>runner.SpawnedCount==runner.Current.EnemyCount&&EnemyHealth.Active.All(e=>!e.IsAlive||e.CanReceiveDamage),4000,"Foundation targets");
            foreach(var enemy in EnemyHealth.Active.Where(e=>e.IsAlive).ToArray())enemy.ReceivePlayerHit(enemy.CurrentHp,player.gameObject);
            foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive).ToArray())pickup.Release();
            await Until(()=>runner.IsClearPause,1500,"Clear pause");await Task.Delay(200);Check(monitor.Count("transition-wind")==windBefore,"Wind during stationary clear pause");
            await Until(()=>monitor.Count("transition-wind")>windBefore,1800,"No wind on encounter travel");
            runner.StopRun();int missionDeparture=monitor.Count("warp-start"),missionArrival=monitor.Count("warp-arrival");
            var combatSource=audio.GetComponentsInChildren<AudioSource>().Single(s=>s.isPlaying&&s.clip==audio.CurrentMusic);var combatClip=audio.CurrentMusic;
            Check(region.QueueAdvance(),"Same-region travel request");
            await Until(()=>runner.Flow.Level.Id=="L2"&&runner.HasActiveRun,5000,"Same-region arrival");await Task.Delay(150);
            Check(monitor.Count("warp-start")==missionDeparture+1&&monitor.Count("warp-arrival")==missionArrival+1,"Same-region duplicated/missing travel cues");
            Check(audio.CurrentMusic==combatClip&&combatSource.isPlaying,"Mission transition cut music");
            region.Cancel();runner.StopRun();runner.Flow.LoadLevel(region.FindMission("L4"));runner.StartRun("BOSS",false,true,null,null);
            player.SetCinematicLock(false);damage.GrantBriefProtection(1000);view.Show(ShellScreen.Playing);Time.timeScale=1;
            await Until(()=>runner.GetComponentInChildren<BossPhaseRunner>()?.GetComponent<EntryHoldFlight>().IsHolding==true,10000,"Vanguard enter");
            var boss=runner.GetComponentInChildren<BossPhaseRunner>();boss.CancelModules();boss.enabled=false;boss.Health.SetDamageBlocked(false);
            int departure=monitor.Count("warp-start");boss.Health.ReceivePlayerHit(boss.Health.CurrentHp,player.gameObject);
            await Until(()=>runner.Stage==EncounterStage.RewardGrace,5000,"Boss reward grace");
            foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive).ToArray())pickup.Release();
            var journey=runner.GetComponent<PlanetJourney>();await Until(()=>journey.IsActive,4000,"Region departure");
            await Task.Delay(100);Check(monitor.Count("warp-start")==departure+1,"Region departure ignition count");
            await Until(()=>journey.IsAwaitingTap,10000,"Region waiting tap");await Task.Delay(350);
            Check(monitor.Count("warp-start")==departure+1,"Ignition repeated while waiting");
            Check(journey.AcceptTap(),"Region tap");await Task.Delay(250);
            Check(monitor.Count("warp-start")==departure+1,"Region approach tap reignited engine");
            await Until(()=>runner.Flow.Level.Id=="L5"&&runner.HasActiveRun,5000,"L5 arrival");await Task.Delay(250);
            Check(monitor.Count("warp-start")==departure+1,"Landing restarted ignition");
            Evidence("transitions",new{status="PASS",noIgnitionBeforeTap=true,oneIgnitionOnTap=true,pauseResume=true,continueWithoutIgnition=true,handoffWind=true,sameRegionSingleIgnitionAndArrival=true,bossRegionSingleIgnition=true,starts=monitor.Starts});
        }
        catch(Exception e){Evidence("transitions",new{status="FAIL",error=e.ToString(),starts=monitor?.Starts});}
        finally
        {
            if(monitor!=null)UnityEngine.Object.Destroy(monitor);
            if(original!=null)
            {
                await Until(()=>!shell.IsBusy,5000,"Shell cleanup");region.Cancel();runner.StopRun();save.ExitIsolated();runner.Flow.LoadLevel(originalLevel);save.UseTestStorage(original);shell.Boot();
                string[] suffix={"",".bak",".invalid"};bool same=true;
                for(int i=0;i<suffix.Length;i++){var after=File.Exists(original+suffix[i])?File.ReadAllBytes(original+suffix[i]):null;same&=before[i]==null?after==null:after!=null&&before[i].SequenceEqual(after);}
                Evidence("save-guard",new{status=same?"PASS":"FAIL",productionBytesUnchanged=same});
                if(temp!=null&&Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
            }
        }
    }
}
