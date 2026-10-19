using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.Rewards;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyPersistence
{
    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static CampaignLifecycle Owner => Runner.GetComponent<CampaignLifecycle>();
    static GameplaySession Session => UnityEngine.Object.FindAnyObjectByType<GameplaySession>();
    static PlayerDamageLifecycle Player => UnityEngine.Object.FindAnyObjectByType<PlayerDamageLifecycle>();
    static RunRewards Rewards => Runner.GetComponent<RunRewards>();
    static WeaponLoadout Loadout => Player.GetComponent<WeaponLoadout>();
    static PlayerLives Lives => Player.GetComponent<PlayerLives>();
    static PlayerGameplayGate Gate => Player.GetComponent<PlayerGameplayGate>();
    static readonly string Evidence = Path.GetFullPath("../.design/m4-b");
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static string Json<T>(T value) => Newtonsoft.Json.JsonConvert.SerializeObject(value);
    static PickupDefinition Item(string name) => AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/" + name + ".asset");
    static async Task Until(Func<bool> condition, float seconds, string label)
    {
        var until = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < until) await Task.Delay(10);
        Require(condition(), label);
    }
    static bool Valid(SaveEnvelope e) => SaveValidation.IsValid(e, (m,c) => m == "L1" && c.StartsWith("ENCOUNTER_") && int.TryParse(c.Substring(10),out var n) && n >= 1 && n <= 5, 5);
    static SaveEnvelope Fixture(int encounter = 1) => new SaveEnvelope { SchemaVersion = SaveValidation.CurrentVersion, HasCampaign = true,
        Settings = SettingsData.Defaults(), Profile = new PlayerProfile { StellarShardBalance = 19, HighScore = 100,
            UnlockedMissions = new[] { "L1" }, Records = Array.Empty<MissionRecord>() },
        Campaign = new CampaignSave { MissionId = "L1", Checkpoint = new CheckpointData { EncounterId = "ENCOUNTER_"+encounter,
            Lives = 4, Missiles = 2, ActiveWeapon = WeaponBranch.Plasma, WeaponLevels = new[] { 2,3,1 }, Stats = new RunStatsData { Score = 200, Shards = 4 } } } };
    public static object Core()
    {
        var directory = Path.Combine(Path.GetTempPath(), "starshift-save-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory,"save.json");
        try
        {
            var store = new JsonSaveStore(path);
            Require(store.Load(Valid,out _) == SaveLoadStatus.Missing,"No save");
            var empty=new CampaignProgress(store,Valid);
            Require(!empty.Snapshot.HasCampaign && !empty.Dirty,"Missing save must have no campaign, including DTO copy");
            var data = Fixture();
            Require(store.Write(data,Valid),"Initial write: "+store.Diagnostic);
            Require(store.Load(Valid,out var read) == SaveLoadStatus.Loaded && Json(data)==Json(read),"Disk roundtrip");
            data.Profile.StellarShardBalance = 20;
            Require(store.Write(data,Valid),"Overwrite");
            File.WriteAllText(path+".tmp","{broken");
            Require(store.Load(Valid,out read)==SaveLoadStatus.Loaded && read.Profile.StellarShardBalance==20,"Temp must not load");
            File.WriteAllText(path,"{broken");
            Require(store.Load(Valid,out read)==SaveLoadStatus.RecoveredBackup && read.Profile.StellarShardBalance==19,"Backup recovery");
            Require(store.Write(read,Valid) && File.ReadAllText(path+".invalid")=="{broken","Repair preserves diagnosis");
            File.Delete(path+".bak");
            File.WriteAllText(path,"{broken");
            Require(store.Load(Valid,out read)==SaveLoadStatus.Invalid && read==null,"Malformed reject");
            File.WriteAllText(path,"{\"SchemaVersion\":99}");
            Require(store.Load(Valid,out _)==SaveLoadStatus.UnsupportedVersion && !store.Write(data,Valid),"Version reject/no overwrite");
            File.WriteAllText(path,"{\"SchemaVersion\":1}");
            Require(store.Load(Valid,out _)==SaveLoadStatus.Invalid,"Required roots");
            data=Fixture(); data.Campaign.Checkpoint.EncounterId="BAD"; Require(!Valid(data),"Bad encounter");
            data=Fixture(); data.Campaign.MissionId="BAD"; Require(!Valid(data),"Bad mission");
            data=Fixture(); data.Campaign.Checkpoint.ActiveWeapon=(WeaponBranch)99; Require(!Valid(data),"Bad enum");
            data=Fixture(); data.Campaign.Checkpoint.Lives=0; Require(!Valid(data),"Bad lives");
            data=Fixture(); data.Campaign.Checkpoint.WeaponLevels[1]=4; Require(!Valid(data),"Bad level");
            data=Fixture(); data.Settings.MusicVolume=float.NaN; Require(!Valid(data),"Bad settings");
            data=Fixture(); Require(store.Write(data,Valid),"Replace invalid with explicit valid state");
            var service=new CampaignProgress(store,Valid);
            service.CreditEncounterShards(3); Require(service.Snapshot.Profile.StellarShardBalance==22,"Shard credit");
            service.CreditEncounterShards(3); service.CreditEncounterShards(2);
            Require(service.Snapshot.Profile.StellarShardBalance==22,"Retry highwater dedupe");
            service.CreditEncounterShards(5); Require(service.Snapshot.Profile.StellarShardBalance==24,"Incremental highwater");
            Require(service.Flush(),"Dirty flush");
            service=new CampaignProgress(store,Valid); service.CreditEncounterShards(5);
            Require(service.Snapshot.Profile.StellarShardBalance==24,"Reload dedupe");
            service.CommitClear(500,12); service.CommitClear(700,4); Require(service.Flush(),"Clear flush");
            service=new CampaignProgress(store,Valid); service.CommitClear(1000);
            Require(service.Snapshot.Profile.HighScore==500 && service.Snapshot.Profile.Records.Length==1 && service.Snapshot.Profile.Records[0].BestTimeSeconds==12,"Clear once");
            var settings=SettingsData.Defaults();settings.MusicVolume=.4f;settings.VibrationEnabled=false;
            Require(service.SetSettings(settings),"Settings write");
            Require(service.NewGame("L1",Fixture().Campaign.Checkpoint),"New Game");
            service=new CampaignProgress(store,Valid);
            Require(service.Snapshot.Profile.HighScore==500 && service.Snapshot.Profile.StellarShardBalance==24 && service.Snapshot.Settings.MusicVolume==.4f && !service.Snapshot.Settings.VibrationEnabled,"New game preserves profile/settings");
            var bad=SettingsData.Defaults();bad.SfxVolume=-1;Require(!service.SetSettings(bad),"Invalid settings rejected");
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Path.Combine(Evidence,"core.json"),Json(new { pass=true, tests="disk roundtrip/overwrite/temp/corrupt/backup/version/roots/IDs/enums/ranges/highwater/reload/clear/newgame/settings" }));
            return new { pass=true, directory, removed=true };
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory,true); }
    }
    public static object BestTimeGuard()
    {
        var directory=Path.Combine(Path.GetTempPath(),"starshift-best-time-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new JsonSaveStore(Path.Combine(directory,"save.json"));
            var data=Fixture();
            data.Profile.Records=new[]{new MissionRecord{MissionId="L1",BestTimeSeconds=120}};
            Require(store.Write(data,Valid),"Best-time fixture");
            var service=new CampaignProgress(store,Valid);
            var inputs=new[]{0f,100f,140f,-1f};
            var expected=new[]{120f,100f,100f,100f};
            for(int i=0;i<inputs.Length;i++)
            {
                Require(service.NewGame("L1",Fixture().Campaign.Checkpoint),"Best-time next campaign");
                service.CommitClear(500,inputs[i]);Require(service.Flush(),"Best-time flush");
                service=new CampaignProgress(store,Valid);
                Require(service.Snapshot.Profile.Records[0].BestTimeSeconds==expected[i],"Best-time guard: "+inputs[i]);
            }
            return new{pass=true,inputs,expected,diskReload=true};
        }
        finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }

    static LevelDefinition legacyLevel;
    static LevelDefinition LegacyLevel()
    {
        if (legacyLevel != null) return legacyLevel;
        legacyLevel = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/StarShift/Data/Levels/L1.asset"));
        legacyLevel.Id = "L1"; legacyLevel.hideFlags = HideFlags.DontSave; return legacyLevel;
    }
    static string UseFixture(int index)
    {
        var directory=Path.Combine(Path.GetTempPath(),"starshift-runtime-"+Guid.NewGuid().ToString("N"));
        var path=Path.Combine(directory,"save.json");
        Require(new JsonSaveStore(path).Write(Fixture(index),Valid),"Runtime fixture");
        Owner.UseTestStorage(path, LegacyLevel()); Require(Owner.Continue(),"Continue fixture "+Owner.Diagnostic);
        return path;
    }
    static async Task UntilWithKills(Func<bool> condition,float seconds,PickupDefinition keepReward=null)
    {
        var until=Time.realtimeSinceStartup+seconds;
        while(!condition() && Time.realtimeSinceStartup<until)
        {
            if(keepReward!=null && Runner.Stage==EncounterStage.Running)
                Runner.GetComponent<RewardDirector>().Spawn(keepReward,null,Camera.main.ViewportToWorldPoint(new Vector3(.9f,.8f,10)));
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>().Where(e=>e.IsAlive).ToArray()) enemy.ReceivePlayerHit(100000,Player.gameObject);
            await Task.Delay(20);
        }
        Require(condition(),"Completion boundary timed out: "+Runner.Stage);
    }
    public static async Task<object> Retry(int encounter)
    {
        var path=UseFixture(encounter);
        try
        {
            var expected=Json(Owner.PersistentSnapshot.Campaign.Checkpoint);
            var token=Runner.Generation;
            var receiver=Player.GetComponent<PlayerPickupReceiver>();
            var shard=Item("StellarShard");
            Require(shard!=null,"Shard fixture missing");
            var director=Runner.GetComponent<RewardDirector>();
            var drop=director.Spawn(shard,null,Player.transform.position);
            Require(drop!=null,"Drop spawn");
            await Until(()=>Rewards.Shards>4,2,"Real drop collection");
            director.Spawn(shard,null,Camera.main.ViewportToWorldPoint(new Vector3(.9f,.8f,10)));
            if (encounter==5)
            {
                Time.timeScale=4;
                await UntilWithKills(()=>Runner.Stage==EncounterStage.RewardGrace,15,shard);
                Time.timeScale=1;
                Require(!Session.IsClear,"Must retry before final clear commit");
            }
            var credited=Owner.PersistentSnapshot.Profile.StellarShardBalance;
            Require(credited>19,"Profile did not receive Shard");
            Rewards.AddKill(100);Loadout.ApplyCore(WeaponBranch.Pulse);
            var effect=Item("Overdrive"); Require(receiver.TryCollect(effect),"Effect fixture");
            Require(Player.TryReceiveHit(null),"Death");
            Require(Lives.CurrentLives==3 && Loadout.ActiveLevel==2,"Ordinary death downgrade");
            Session.Pause(); Require(Session.IsPaused,"Pause");
            var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b=>b.name=="RestartEncounter");
            Require(button.interactable,"Retry UI disabled");button.onClick.Invoke();
            Require(!Session.IsPaused && Runner.Current.Id=="ENCOUNTER_"+encounter && Runner.Generation>token,"Retry launch");
            Require(Json(Owner.PersistentSnapshot.Campaign.Checkpoint)==expected,"Checkpoint changed");
            Require(Lives.CurrentLives==4 && Loadout.ActiveLevel==3 && Rewards.Score==200 && Rewards.Shards==4,"Restore lives/loadout/stats");
            var effects=Player.GetComponent<PlayerTemporaryEffects>();var weapon=Player.GetComponent<PulseLaserWeapon>();
            Require(effects.OverdriveRemaining==0 && effects.DroneRemaining==0 && !effects.HasShield && weapon.Heat==0,"Transient reset");
            Require(!UnityEngine.Object.FindObjectsByType<RewardPickup>().Any(p=>p.IsActive) && UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length==0 && UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length==0,"Combat cleanup");
            Require(receiver.TryCollect(shard),"Retry shard collect");Require(Owner.PersistentSnapshot.Profile.StellarShardBalance==credited,"Retry duplicate credit");
            Require(Owner.Flush(),"Flush before reload");
            var body=File.ReadAllText(path);
            Owner.ExitIsolated(); Owner.UseTestStorage(path, LegacyLevel());Require(Owner.Continue(),"Recreate service/reload");
            Require(Rewards.Score==200 && Lives.CurrentLives==4 && Loadout.ActiveLevel==3,"Continue checkpoint");
            Require(receiver.TryCollect(shard) && Owner.PersistentSnapshot.Profile.StellarShardBalance==credited,"Continue duplicate credit");
            Require(File.ReadAllText(path)==body,"No IO per pickup");
            if (encounter==4)
            {
                Require(Player.TryReceiveHit(null),"Respawn smoke hit");
                await Until(()=>Gate.State==PlayerGameplayState.PostEntryInvincibility,3,"Ordinary respawn");
                Require(Runner.HasActiveRun && Lives.CurrentLives==3,"Respawn must continue encounter");
                while(Lives.CurrentLives>0) { Gate.SetState(PlayerGameplayState.Active);Require(Player.TryReceiveHit(null),"GameOver hit"); }
                await Until(()=>Session.IsTerminal,2,"GameOver smoke");
                Require(Session.CanRetry,"GameOver Retry availability");Session.RetryEncounter();
                Require(Lives.CurrentLives==4 && !Session.IsTerminal && !AudioListener.pause && Time.timeScale>0,"GameOver retry restore");
            }
            Directory.CreateDirectory(Evidence);File.WriteAllText(Path.Combine(Evidence,"retry-"+encounter+".json"),Json(new {pass=true,encounter,credited,token,restoredGeneration=Runner.Generation}));
            return new {pass=true,encounter,credited};
        }
        finally { Owner.ExitIsolated();Directory.Delete(Path.GetDirectoryName(path),true); }
    }
    public static async Task<object> Lifecycle()
    {
        var productionPath=Path.Combine(Application.persistentDataPath,"starshift-save-v1.json");
        var productionBefore=File.Exists(productionPath)?File.ReadAllBytes(productionPath):null;
        var path=UseFixture(1);
        var originalScale=Time.timeScale;
        var originalAudio=AudioListener.pause;
        try
        {
            var profileBefore=Json(Owner.PersistentSnapshot.Profile);
            var settings=SettingsData.Defaults();settings.SfxVolume=.35f;
            Require(Owner.SetSettings(settings),"Settings persistence backend");
            Require(Owner.NewGame(),"New Game backend");
            Require(Json(Owner.PersistentSnapshot.Profile)==profileBefore && Rewards.Score==0 && Lives.CurrentLives==3 && Loadout.ActiveLevel==1,"New Game boundary");
            Require(Session.Flow.IsAwaitingStart,"Mission intro missing");
            Time.timeScale=4;
            await Until(()=>Session.Flow.CanContinue,3,"Intro Continue readiness");
            Session.BeginMission();
            await Until(()=>!Session.Flow.IsCinematic,5,"Intro gameplay");
            var frame=Time.frameCount;
            await UntilWithKills(()=>Runner.CurrentIndex==2 && Runner.Stage==EncounterStage.Running,30);
            Require(Time.frameCount>frame && Owner.PersistentSnapshot.Campaign.Checkpoint.EncounterId=="ENCOUNTER_3","Checkpoint 3");
            Require(Player.GetComponent<PlayerPickupReceiver>().TryCollect(Item("StellarShard")),"Prequit shard");
            Session.Pause();Require(Owner.Flush(),"Save before reload");
            var checkpoint=Json(Owner.PersistentSnapshot.Campaign.Checkpoint);
            var saved=File.ReadAllText(path);
            var scene=Runner.gameObject.scene.path;
            var old=Runner;
            var operation=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scene,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            await Until(()=>operation.isDone && Runner!=null && Runner!=old,10,"Scene recreation");
            Owner.UseTestStorage(path, LegacyLevel());Require(Owner.Continue(),"Continue after scene recreation");
            Require(Runner.CurrentIndex==2 && !Session.Flow.IsIntroducing && Rewards.Score==Owner.PersistentSnapshot.Campaign.Checkpoint.Stats.Score,"Correct Continue boundary/no intro");
            Require(Json(Owner.PersistentSnapshot.Campaign.Checkpoint)==checkpoint && File.ReadAllText(path)==saved,"Continue must not recapture/credit");
            Require(Player.GetComponent<PulseLaserWeapon>().Heat==0 && !Player.GetComponent<PlayerTemporaryEffects>().HasShield && !UnityEngine.Object.FindObjectsByType<RewardPickup>().Any(p=>p.IsActive),"Clean reload transient state");
            Require(Mathf.Approximately(Owner.PersistentSnapshot.Settings.SfxVolume,.35f),"Settings survive reload");
            int clears=0;Runner.MissionCompleted+=()=>clears++;
            Time.timeScale=4;
            await UntilWithKills(()=>Session.IsClear,30);
            Require(clears==1 && Owner.PersistentSnapshot.Campaign.Status==CampaignStatus.MissionCleared,"Exactly once persisted Clear");
            Require(Owner.PersistentSnapshot.Profile.Records.Length==1,"Clear record");
            var before=File.ReadAllText(path);var beforeData=Json(Owner.PersistentSnapshot);
            Require(!Owner.Continue(),"Cleared campaign cannot resume stale last checkpoint");
            Require(File.ReadAllText(path)==before,"Continue clear duplicated commit");
            Require(Owner.BeginReplay(),"Replay unlocked mission");
            Require(Owner.Mode==SessionMode.Replay,"Replay mode");
            Require(Player.GetComponent<PlayerPickupReceiver>().TryCollect(Item("StellarShard")),"Replay reward");
            Loadout.ApplyCore(WeaponBranch.Pulse);Loadout.ApplyCore(WeaponBranch.Pulse);
            Require(Player.TryReceiveHit(null),"Replay death");
            Require(Lives.CurrentLives==2 && Loadout.ActiveLevel==2,"Replay mutation");
            Time.timeScale=4;await UntilWithKills(()=>Session.IsClear,35);
            Owner.ExitIsolated();
            Require(File.ReadAllText(path)==before && Json(Owner.PersistentSnapshot)==beforeData,"Replay bytes/data changed");
            var dev=Runner.GetComponent<StarShift.Dev.DevFastStart>();
            var config=new SerializedObject(dev);var enabled=config.FindProperty("devModeEnabled").boolValue;
            var start=config.FindProperty("startPoint").stringValue;var skip=config.FindProperty("skipIntro").boolValue;
            try
            {
                config.FindProperty("devModeEnabled").boolValue=true;config.FindProperty("startPoint").stringValue="ENCOUNTER_5";
                config.FindProperty("skipIntro").boolValue=true;config.ApplyModifiedPropertiesWithoutUndo();
                dev.Replay();Require(Owner.Mode==SessionMode.DevTest,"DevFastStart write permission");
                Player.GetComponent<PlayerPickupReceiver>().TryCollect(Item("StellarShard"));Loadout.ApplyCore(WeaponBranch.Pulse);
                Require(Player.TryReceiveHit(null),"Dev death");
                Time.timeScale=4;await UntilWithKills(()=>Session.IsClear,15);
                Owner.ExitIsolated();Require(File.ReadAllText(path)==before && Json(Owner.PersistentSnapshot)==beforeData,"DevFastStart bytes/data changed");
            }
            finally
            {
                config.Update();config.FindProperty("devModeEnabled").boolValue=enabled;config.FindProperty("startPoint").stringValue=start;
                config.FindProperty("skipIntro").boolValue=skip;config.ApplyModifiedPropertiesWithoutUndo();
            }
            // Failure lifecycle dùng bản sao; backup tự chứa Profile/Campaign cùng một transaction.
            File.WriteAllText(path,"{truncated");
            Owner.UseTestStorage(path, LegacyLevel());
            Require(Owner.LoadStatus==SaveLoadStatus.RecoveredBackup,"Runtime backup recovery");
            if(File.Exists(path+".bak"))File.Delete(path+".bak");
            Owner.UseTestStorage(path, LegacyLevel());Require(Owner.LoadStatus==SaveLoadStatus.Invalid && !Owner.Continue() && !Owner.NewGame(),"Invalid save must expose failure without wiping profile");
            File.Delete(path);Owner.UseTestStorage(path, LegacyLevel());Require(Owner.LoadStatus==SaveLoadStatus.Missing && !Owner.Continue(),"No save Continue");
            Require(Owner.NewGame(),"Explicit New Game from missing save");
            Require(Owner.PersistentSnapshot.Campaign.Checkpoint.EncounterId=="ENCOUNTER_1","Old checkpoint leaked");
            Owner.ExitIsolated();
            var productionAfter=File.Exists(productionPath)?File.ReadAllBytes(productionPath):null;
            Require((productionBefore==null && productionAfter==null) || (productionBefore!=null && productionAfter!=null && productionBefore.SequenceEqual(productionAfter)),"Production save changed");
            var result=new {pass=true, sceneRecreated=true, checkpoint="ENCOUNTER_3",campaignClears=1,totalMissionCompletions=clears,replayBytesUnchanged=true,devFastStartBytesUnchanged=true,productionUnchanged=true,corruptRecovery=true};
            Directory.CreateDirectory(Evidence);File.WriteAllText(Path.Combine(Evidence,"lifecycle.json"),Json(result));return result;
        }
        finally
        {
            Owner.ExitIsolated();Time.timeScale=originalScale;AudioListener.pause=originalAudio;
            if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);
        }
    }
    public static object BeginLifecycle()
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Path.Combine(Evidence,"lifecycle-progress.json"),"{\"status\":\"running\"}");
        _=RunLifecycle();return new { started=true };
    }
    static async Task RunLifecycle()
    {
        try { await Lifecycle();File.WriteAllText(Path.Combine(Evidence,"lifecycle-progress.json"),"{\"status\":\"PASS\"}"); }
        catch(Exception e) { File.WriteAllText(Path.Combine(Evidence,"lifecycle-progress.json"),Json(new {status="FAILED",error=e.ToString()})); }
    }

    public static async Task<object> ActiveIsolation()
    {
        var path=UseFixture(4);
        try
        {
            var before=File.ReadAllText(path);var snapshot=Json(Owner.PersistentSnapshot);
            Require(Owner.BeginReplay(),"Active campaign Replay");
            Player.GetComponent<PlayerPickupReceiver>().TryCollect(Item("StellarShard"));
            Loadout.ApplyCore(WeaponBranch.Plasma);Loadout.Cycle();
            Require(Player.TryReceiveHit(null),"Active replay death");
            Owner.ExitIsolated();
            Require(File.ReadAllText(path)==before && Json(Owner.PersistentSnapshot)==snapshot,"Active campaign isolation");
            Require(Owner.Continue() && Runner.Current.Id=="ENCOUNTER_4" && Lives.CurrentLives==4 && Loadout.ActiveLevel==3,"Campaign Continue after replay");
            int starts=0;Action started=()=>starts++;Session.MissionStarted+=started;
            try { Require(Owner.Continue() && starts==1,"Exactly one MissionStarted notification"); }
            finally { Session.MissionStarted-=started; }
            var token=Runner.Generation;
            Session.Pause();var elapsed=Runner.Elapsed;await Task.Delay(80);
            Require(Runner.Elapsed==elapsed && Time.timeScale==0,"Pause freeze");Session.Resume();
            Require(Time.timeScale>0 && !AudioListener.pause,"Resume clock");
            Require(!Runner.NotifyBossPresentationComplete(token-1),"Stale callback accepted");
            Owner.Flush();
            // Explicit app-pause message kiểm boundary dirty flush, không giả Android OS kill.
            Require(Player.GetComponent<PlayerPickupReceiver>().TryCollect(Item("StellarShard")),"Lifecycle dirty reward");
            Require(Owner.Dirty && File.ReadAllText(path)==before,"Dirty pickup must not write");
            Owner.SendMessage("OnApplicationPause",true);Require(!Owner.Dirty && File.ReadAllText(path)!=before,"App pause flush");
            var frozen=Owner.PersistentSnapshot;
            frozen.Campaign.Checkpoint.WeaponLevels[0]=3;frozen.Profile.StellarShardBalance=0;
            Require(Owner.PersistentSnapshot.Campaign.Checkpoint.WeaponLevels[0]==2 && Owner.PersistentSnapshot.Profile.StellarShardBalance>0,"Snapshot alias mutation");
            Directory.CreateDirectory(Evidence);File.WriteAllText(Path.Combine(Evidence,"active-isolation.json"),Json(new {pass=true,checkpoint="ENCOUNTER_4",activeCampaignBytesUnchanged=true,appPauseFlush=true,staleCallbackRejected=true}));
            return new {pass=true};
        }
        finally {Owner.ExitIsolated();Directory.Delete(Path.GetDirectoryName(path),true);}
    }

}
