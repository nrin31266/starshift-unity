using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class VerifyGameplayFeel
{
    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession Session => Runner.GetComponent<GameplaySession>();
    static CampaignLifecycle Owner => Runner.GetComponent<CampaignLifecycle>();
    static PlayerDamageLifecycle Player => UnityEngine.Object.FindAnyObjectByType<PlayerDamageLifecycle>();
    static EmergencyMissileReserve Reserve => Player.GetComponent<EmergencyMissileReserve>();
    static PlayerGameplayGate Gate => Player.GetComponent<PlayerGameplayGate>();
    static PlayerPickupReceiver Receiver => Player.GetComponent<PlayerPickupReceiver>();
    static PickupDefinition Item(string id) => AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/"+id+".asset");
    static void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
    static void Set(UnityEngine.Object target,string field,UnityEngine.Object value) {var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static async Task Until(Func<bool> condition,float timeout,string message)
    {
        var end=Time.realtimeSinceStartup+timeout;
        while(!condition() && Time.realtimeSinceStartup<end)await Task.Delay(10);
        Require(condition(),message);
    }
    static void Evidence(string name,object value)
    {
        Directory.CreateDirectory("../.design/gameplay-feel");
        File.WriteAllText("../.design/gameplay-feel/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    }
    static void Start(string id="ENCOUNTER_2")
    {
        Runner.StopRun();Time.timeScale=1;
        Player.RestoreCheckpoint(3);Runner.StartRun(id,false,true,null,null);
        Gate.SetState(PlayerGameplayState.PostEntryInvincibility);
    }
    static string Fixture()
    {
        var path=Path.Combine(Path.GetTempPath(),"starshift-feel-"+Guid.NewGuid().ToString("N"),"save.json");
        var envelope=new SaveEnvelope {SchemaVersion=SaveValidation.CurrentVersion,HasCampaign=true,
            Campaign=new CampaignSave {MissionId=Runner.Flow.Level.Id,Checkpoint=new CheckpointData {EncounterId="ENCOUNTER_2",Lives=3,Missiles=2,WeaponLevels=new[]{1,0,0},Stats=new RunStatsData()}},
            Profile=new PlayerProfile {StellarShardBalance=17,UnlockedMissions=new[]{Runner.Flow.Level.Id},Records=Array.Empty<MissionRecord>()},Settings=SettingsData.Defaults()};
        Require(new JsonSaveStore(path).Write(envelope,e=>SaveValidation.IsValid(e,(m,c)=>m==Runner.Flow.Level.Id && Runner.Flow.Level.ResolveStart(c)>=0,5)),"Fixture write");
        Owner.UseTestStorage(path);Require(Owner.Continue(),"Fixture Continue");return path;
    }
    public static object Thermal()
    {
        var results=new System.Collections.Generic.List<object>();
        foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})
        {
            var profile=Player.GetComponent<WeaponLoadout>().Definition(branch).ThermalProfile;
            var state=new WeaponHeatState();float firing=0,recovery=0;
            while(!state.IsOverheated && firing<20){state.Tick(profile,true,.01f);firing+=.01f;}
            while(state.IsOverheated && recovery<3){state.Tick(profile,true,.01f);recovery+=.01f;}
            Require(firing>=9 && firing<=12 && recovery>=1.15f && recovery<=1.3f,"Thermal target: "+branch+" "+firing+"/"+recovery);
            state.Reset();state.Tick(profile,true,4);var warm=state.Heat;
            state.Tick(profile,false,.2f);Require(Mathf.Approximately(state.Heat,warm),"Quick release cooled too freely: "+branch);
            state.Tick(profile,false,.3f);var releasedHeat=warm-state.Heat;
            Require(Mathf.Abs(releasedHeat-6)<.02f,"Cooling after delay changed: "+branch+" "+releasedHeat);
            results.Add(new{branch=branch.ToString(),firing,recovery,halfSecondReleaseHeatLost=releasedHeat});
        }
        var result=new{pass=true,results};Evidence("thermal",result);return result;
    }
    public static async Task<object> ProjectionReset()
    {
        var readyFrame=Time.frameCount;
        await Until(()=>Time.frameCount>readyFrame,1,"Initial gameplay frame");
        Runner.StopRun();
        var camera=Camera.main;var view=camera.GetComponent<GameplayViewController>();
        var original=view.TargetView;var originalScale=Time.timeScale;
        int renders=0,shifted=0;Matrix4x4 rendered=default;
        Action<ScriptableRenderContext,Camera> observe=(context,c)=>
        {
            if(c!=camera)return;
            rendered=c.projectionMatrix;renders++;
            if(Mathf.Abs(rendered.m03)+Mathf.Abs(rendered.m13)>.00001f)shifted++;
        };
        RenderPipelineManager.beginCameraRendering+=observe;
        try
        {
            Time.timeScale=1;view.CancelImpact();camera.aspect=16f/9;
            view.RequestView(new GameplayViewSettings(6,new Rect(0,0,1,1)),0);
            view.TriggerReserveImpact();
            await Until(()=>shifted>0,1,"Impact not observed in render");
            await Until(()=>view.ImpactRemaining<=0,1,"Impact did not expire");
            int before=renders;
            await Until(()=>renders>before,1,"Post-impact render");
            Require(Mathf.Abs(rendered.m03)+Mathf.Abs(rendered.m13)<.00001f,"Impact offset leaked");
            var sample=camera.transform.position+camera.transform.forward*10+camera.transform.up*2;
            var initialY=camera.WorldToViewportPoint(sample).y;
            view.RequestView(new GameplayViewSettings(9,new Rect(0,0,1,1)),.08f);
            await Until(()=>!view.IsTransitioning,1,"RequestView transition");before=renders;
            await Until(()=>renders>before,1,"Zoom render");
            Require(Mathf.Abs(rendered.m11-1f/9)<.0001f && camera.WorldToViewportPoint(sample).y<initialY-.04f,"Rendered RequestView framing stuck");
            var previousX=rendered.m00;camera.aspect=20f/9;before=renders;
            await Until(()=>renders>before,1,"Aspect render");
            Require(Mathf.Abs(rendered.m00-1f/(9*camera.aspect))<.0001f && rendered.m00<previousX,"Rendered aspect stuck");
            camera.orthographicSize=7;
            Require(Mathf.Abs(camera.projectionMatrix.m11-1f/7)<.0001f,"Direct ortho stuck after render");
            // Reset phải hoạt động cả khi không đang sở hữu render offset.
            camera.projectionMatrix=Matrix4x4.identity;view.CancelImpact();camera.orthographicSize=8;
            Require(Mathf.Abs(camera.projectionMatrix.m11-1f/8)<.0001f,"CancelImpact retained custom projection");
            camera.projectionMatrix=Matrix4x4.identity;view.enabled=false;camera.orthographicSize=10;
            Require(Mathf.Abs(camera.projectionMatrix.m11-.1f)<.0001f,"OnDisable retained custom projection");
            var result=new{pass=true,shifted,renders,requestViewRendered=true,aspectRendered=true,directOrtho=true,cancelReset=true,disableReset=true};
            Evidence("projection-reset",result);return result;
        }
        finally
        {
            RenderPipelineManager.beginCameraRendering-=observe;
            view.enabled=true;view.CancelImpact();camera.ResetAspect();view.RequestView(original,0);Time.timeScale=originalScale;
        }
    }

    public static async Task<object> PresentationNullSafety()
    {
        var presentation=Player.GetComponent<EmergencyMissilePresentation>();
        var fields=new[]{"burst","persistence","view"};
        var saved=fields.Select(f=>new SerializedObject(presentation).FindProperty(f).objectReferenceValue).ToArray();
        var view=Camera.main.GetComponent<GameplayViewController>();
        var results=new System.Collections.Generic.List<object>();
        foreach(var scenario in new[]{"full","burst-null","persistence-null","view-null","shake-disabled"})
        {
            string path=null;EnemyHealth target=null;bool flightEnabled=false,attackEnabled=false;
            int detonations=0;bool shook=false;
            Action observed=()=>{detonations++;shook=view.ImpactRemaining>0;};
            try
            {
                path=Fixture();
                for(int i=0;i<fields.Length;i++)Set(presentation,fields[i],saved[i]);
                if(scenario.EndsWith("-null"))Set(presentation,scenario.Replace("-null",""),null);
                if(scenario=="shake-disabled")
                {
                    var settings=SettingsData.Defaults();settings.ScreenShakeEnabled=false;
                    Require(Owner.SetSettings(settings),"Shake setting fixture");
                }
                await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive && e.Definition.BodyDamagesPlayer),3,"Null matrix target spawn");
                target=EnemyHealth.Active.First(e=>e.IsAlive && e.Definition.BodyDamagesPlayer);
                var flight=target.GetComponent<EntryHoldFlight>();var attack=target.GetComponent<SingleFireAttack>();
                flightEnabled=flight.enabled;attackEnabled=attack.enabled;flight.enabled=false;attack.enabled=false;
                target.transform.position=Camera.main.ViewportToWorldPoint(new Vector3(.5f,.7f,-Camera.main.transform.position.z));
                target.GetComponent<Rigidbody2D>().position=target.transform.position;Physics2D.SyncTransforms();
                view.CancelImpact();Reserve.Detonated+=observed;var stock=Reserve.Current;
                Require(Reserve.TryUse() && Reserve.Current==stock-1,"Null matrix activation/stock: "+scenario);
                await Until(()=>detonations==1,1,"Null matrix detonation: "+scenario);
                Require(!target.IsAlive && Reserve.Current==stock-1,"Null matrix damage/stock: "+scenario);
                Require(shook==(scenario!="view-null" && scenario!="shake-disabled"),"Null matrix shake policy: "+scenario);
                await Task.Delay(80);
                results.Add(new{scenario,damage=true,stock=true,detonations,shook});
            }
            finally
            {
                Reserve.Detonated-=observed;
                for(int i=0;i<fields.Length;i++)Set(presentation,fields[i],saved[i]);
                if(target!=null){target.GetComponent<EntryHoldFlight>().enabled=flightEnabled;target.GetComponent<SingleFireAttack>().enabled=attackEnabled;}
                presentation.ResetForSession();Owner.ExitIsolated();
                if(path!=null && Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);
            }
        }
        var result=new{pass=true,results};Evidence("presentation-null-safety",result);return result;
    }

    public static async Task<object> MissileAndImpact()
    {
        var path=Fixture();
        var originals=new System.Collections.Generic.List<(EnemyHealth enemy,EnemyDefinition definition)>();
        var copies=new System.Collections.Generic.List<EnemyDefinition>();
        var camera=Camera.main;var view=camera.GetComponent<GameplayViewController>();
        var position=camera.transform.position;var rotation=camera.transform.rotation;var baseline=camera.projectionMatrix;
        int rendered=0,restored=0;float maxOffset=0;
        int detonations=0;Action onDetonation=()=>detonations++;Reserve.Detonated+=onDetonation;
        Action<ScriptableRenderContext,Camera> before=(context,c)=>{if(c!=camera)return;var matrix=c.projectionMatrix;var offset=Mathf.Max(Mathf.Abs(matrix.m03-baseline.m03),Mathf.Abs(matrix.m13-baseline.m13));if(offset>0){rendered++;maxOffset=Mathf.Max(maxOffset,offset);}};
        Action<ScriptableRenderContext,Camera> after=(context,c)=>{if(c==camera && c.projectionMatrix==baseline)restored++;};
        RenderPipelineManager.beginCameraRendering+=before;RenderPipelineManager.endCameraRendering+=after;
        try
        {
            Require(Reserve.Current==2,"Reserve checkpoint restore");
            Require(Receiver.TryCollect(Item("MissileAmmo")) && Reserve.Current==3 && !Receiver.TryCollect(Item("MissileAmmo")),"Ammo cap and pickup");
            Session.Pause();Require(!Reserve.TryUse() && Reserve.Current==3,"Missile used while paused");Session.Resume();
            await Until(()=>UnityEngine.Object.FindObjectsByType<EnemyHealth>().Count(e=>e.IsAlive)>=6,3,"Targets spawn");
            var targets=UnityEngine.Object.FindObjectsByType<EnemyHealth>().Where(e=>e.IsAlive).Take(6).ToArray();
            for(int i=0;i<targets.Length;i++)
            {
                var e=targets[i];originals.Add((e,e.Definition));
                e.GetComponent<EntryHoldFlight>().enabled=false;e.GetComponent<SingleFireAttack>().enabled=false;
                var copy=UnityEngine.Object.Instantiate(e.Definition);copies.Add(copy);
                var data=new SerializedObject(copy);data.FindProperty("maxHp").intValue=i==2?1000:i==1?100:20;
                data.FindProperty("missileTier").enumValueIndex=i==2?2:i==1?1:0;
                if(i==4)data.FindProperty("bodyDamagesPlayer").boolValue=false;
                data.ApplyModifiedPropertiesWithoutUndo();
                Set(e,"definition",copy);e.gameObject.SetActive(false);e.gameObject.SetActive(true);
                var p=new Vector3(-3+i*1.5f,1.5f,0);if(i==3)p.x=100;
                if(i==5)p=camera.ViewportToWorldPoint(new Vector3(1.005f,.65f,-camera.transform.position.z));
                e.transform.position=p;e.GetComponent<Rigidbody2D>().position=p;
            }
            Physics2D.SyncTransforms();
            var button=UnityEngine.Object.FindObjectsByType<Button>().Single(b=>b.name=="EmergencyMissile");
            int fired=0;Action onFire=()=>fired++;Reserve.Fired+=onFire;
            try{button.onClick.Invoke();}finally{Reserve.Fired-=onFire;}
            Require(fired==1 && Reserve.Current==2 && !Reserve.TryUse(),"Activation/cooldown/UI");
            Require(Reserve.IsInFlight && targets[0].CurrentHp==20 && detonations==0 && view.ImpactRemaining==0 && !Player.TryReceiveHit(null),"Launch resolved damage early or lacked protection");
            Session.Pause();var progress=Reserve.FlightProgress;await Task.Delay(120);
            Require(Reserve.FlightProgress==progress && detonations==0,"Pause advanced missile flight");Session.Resume();
            await Until(()=>!Reserve.IsInFlight,1,"Missile never detonated");
            Require(detonations==1 && !targets[5].IsAlive,"Detonation duplicate or missed partially visible enemy");
            Require(!targets[0].IsAlive && targets[1].CurrentHp==40 && targets[2].CurrentHp==900 && targets[3].CurrentHp==20 && targets[4].CurrentHp==20,"Missile tier/visible/friendly damage: "+string.Join(",",targets.Select(t=>t.CurrentHp+"/"+t.MaxHp+":"+t.Definition.MissileTier)));
            Require(view.ImpactRemaining>0 && !Player.TryReceiveHit(null),"Impact/protection");
            for(int i=0;i<10;i++)view.TriggerReserveImpact();
            Require(view.ImpactRemaining<=.18f,"Impact stacking");
            await Until(()=>view.ImpactRemaining<=0,1,"Impact expiry");
            Require(rendered>0 && restored>0 && maxOffset<=view.ImpactStrength*2+.00001f,"Render impulse missing/unbounded: "+rendered+" "+maxOffset);
            Require(camera.transform.position==position && camera.transform.rotation==rotation && camera.projectionMatrix==baseline,"Camera gameplay pose/projection drift");
            var stock=Reserve.Current;Session.Pause();Session.RetryEncounter();Require(Reserve.Current==2,"Retry missile stock");
            Reserve.TryUse();Require(Reserve.Current==1,"Consume after retry");
            Require(Owner.Flush(),"Flush");Owner.ExitIsolated();Owner.UseTestStorage(path);Require(Owner.Continue() && Reserve.Current==2,"Continue missile checkpoint");
            var settings=SettingsData.Defaults();settings.ScreenShakeEnabled=false;Require(Owner.SetSettings(settings),"Shake preference");
            Require(Reserve.TryUse() && view.ImpactRemaining==0,"Shake setting ignored");
            await Until(()=>!Reserve.IsInFlight,1,"Shake-disabled missile stalled");Require(view.ImpactRemaining==0,"Detonation ignored shake preference");
            var saved=File.ReadAllBytes(path);
            Require(Owner.BeginReplay() && Reserve.Current==1,"Replay preset reserve");Reserve.TryUse();Owner.ExitIsolated();
            Require(File.ReadAllBytes(path).SequenceEqual(saved),"Replay reserve save leak");
            var legacy=Newtonsoft.Json.JsonConvert.DeserializeObject<SaveEnvelope>(System.Text.Encoding.UTF8.GetString(saved));legacy.SchemaVersion=1;
            File.WriteAllText(path,JsonUtility.ToJson(legacy));
            var store=new JsonSaveStore(path);Require(store.Load(e=>SaveValidation.IsValid(e,(m,c)=>Runner.Flow.Level.ResolveStart(c)>=0,5),out var migrated)==SaveLoadStatus.Loaded && migrated.SchemaVersion==2 && migrated.Campaign.Checkpoint.Missiles==1 && migrated.Profile.StellarShardBalance==17,"V1 migration");
            var result=new{pass=true,delayedDetonation=true,pauseFlight=true,edgeTargetHit=true,normalKilled=true,largeHp=40,bossHp=900,offscreenUntouched=true,nonHostileUntouched=true,rendered,restored,maxOffset,checkpointStock=true,migration=true};Evidence("missile-impact",result);return result;
        }
        finally
        {
            foreach(var pair in originals)if(pair.enemy!=null){Set(pair.enemy,"definition",pair.definition);pair.enemy.GetComponent<EntryHoldFlight>().enabled=true;pair.enemy.GetComponent<SingleFireAttack>().enabled=true;}
            foreach(var copy in copies)UnityEngine.Object.Destroy(copy);
            RenderPipelineManager.beginCameraRendering-=before;RenderPipelineManager.endCameraRendering-=after;
            Reserve.Detonated-=onDetonation;
            Owner.ExitIsolated();if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);
        }
    }
    public static async Task<object> ShieldRespawn()
    {
        Start();Gate.SetState(PlayerGameplayState.Active);
        var lives=Player.GetComponent<PlayerLives>();var loadout=Player.GetComponent<WeaponLoadout>();
        loadout.ApplyCore(WeaponBranch.Pulse);Receiver.TryCollect(Item("ShieldCell"));
        Require(Player.TryReceiveHit(null) && lives.CurrentLives==3 && loadout.ActiveLevel==2 && !Player.TryReceiveHit(null),"Shield break fairness");
        Session.Pause();await Task.Delay(100);Require(Gate.State==PlayerGameplayState.PostEntryInvincibility,"Pause advanced shield protection");Session.Resume();
        await Until(()=>Gate.State==PlayerGameplayState.Active,1.5f,"Shield protection expiration");
        var start=Time.time;Require(Player.TryReceiveHit(null),"Death hit");
        await Until(()=>Gate.State==PlayerGameplayState.Respawning,2,"Respawn begin");var death=Time.time-start;
        await Until(()=>Gate.CanInput,2,"Return control");var total=Time.time-start;
        Require(death>=.58f && death<=.68f && total<1.3f && lives.CurrentLives==2 && loadout.ActiveLevel==1,"Death/entrance duration: "+death+" "+total);
        var result=new{pass=true,death,controlAt=total,shieldProtected=true};Evidence("shield-respawn",result);Runner.StopRun();return result;
    }
    public static async Task<object> Pickups()
    {
        Start();Player.GrantBriefProtection(120);var director=Runner.GetComponent<RewardDirector>();
        var shard=director.Spawn(Item("StellarShard"),null,new Vector3(5,2,0));
        var spawnedAt=Time.fixedTime;
        float previousY=shard.transform.position.y;int bounces=0;float lastDelta=0;
        var until=Time.realtimeSinceStartup+5;
        while(shard.IsActive && Time.realtimeSinceStartup<until)
        {
            await Task.Delay(10);var delta=shard.transform.position.y-previousY;
            if(delta>.001f && lastDelta<-.001f)bounces++;
            if(Mathf.Abs(delta)>.001f)lastDelta=delta;
            previousY=shard.transform.position.y;
        }
        Require(!shard.IsActive && bounces==2,"Shard bounces/exit: "+bounces);
        var shardVisibleSeconds=Time.fixedTime-spawnedAt;
        var item=UnityEngine.Object.Instantiate(Item("Overdrive"));item.DriftSpeed=4;
        try
        {
            var pickup=director.Spawn(item,null,new Vector3(-5,-4.8f,0));
            await Until(()=>!pickup.IsActive,1.5f,"Powerup did not exit viewport");
            Require(director.ActivePickups==0,"Pickup pool count leak");
        }
        finally{UnityEngine.Object.Destroy(item);}
        var result=new{pass=true,shardBounces=bounces,shardVisibleSeconds,powerupViewportExit=true};Evidence("pickup-motion",result);Runner.StopRun();return result;
    }
    public static async Task<object> EmptyEncounter()
    {
        var samples=new System.Collections.Generic.List<object>();
        for(int encounter=2;encounter<=4;encounter++)
        {
            Start("ENCOUNTER_"+encounter);Player.GrantBriefProtection(120);Time.timeScale=4;
            int starts=0;Action<EncounterDefinition,int> started=(d,t)=>starts++;
            Runner.EncounterStarted+=started;
            var definition=Runner.Current;float completedAt=-1;int spawned=0;
            Action<EncounterDefinition> completed=d=>{completedAt=Runner.Elapsed;spawned=Runner.SpawnedCount;};
            Runner.Completed+=completed;
            var until=Time.realtimeSinceStartup+12;
            try
            {
                while(Runner.CurrentIndex==encounter-1 && Time.realtimeSinceStartup<until)
                {
                    await Task.Delay(10);
                }
                Require(Runner.CurrentIndex==encounter && starts==1,"No-shoot transition timeout "+encounter);
                Require(completedAt>0 && completedAt<definition.Duration-.1f,"Still waiting for full timer: "+encounter+" "+completedAt);
                var expected=definition.EnemyCount+definition.OptionalEvents.Where(e=>e.Group!=null).Sum(e=>e.Group.Count);
                Require(spawned==expected,"Skipped pending group/event: "+encounter+" "+spawned+"/"+expected);
                samples.Add(new{encounter,completedAt,configuredDuration=definition.Duration,spawned,next=Runner.CurrentIndex+1});
            }
            finally{Runner.EncounterStarted-=started;Runner.Completed-=completed;Runner.StopRun();Time.timeScale=1;}
        }
        var result=new{pass=true,noShoot=true,samples};Evidence("encounter-pacing",result);return result;
    }
    public static async Task<object> ClearHandoffDelay()
    {
        var original=Runner.Flow.Level;var originalScale=Time.timeScale;
        var level=UnityEngine.Object.Instantiate(original);
        var first=UnityEngine.Object.Instantiate(original.Encounters[0]);
        var second=UnityEngine.Object.Instantiate(original.Encounters[1]);
        var samples=new System.Collections.Generic.List<object>();
        try
        {
            first.Groups=new[]{first.Groups[0]};first.Groups[0].Count=1;first.Groups[0].StartTime=0;
            first.OptionalEvents=Array.Empty<OptionalEvent>();
            second.Groups=Array.Empty<SpawnAction>();second.OptionalEvents=Array.Empty<OptionalEvent>();
            second.Duration=30;second.CompleteWhenScheduleExhausted=false;
            level.Encounters=new[]{first,second};level.AdditionalStarts=Array.Empty<LevelStartPoint>();level.Intro.Enabled=false;
            Runner.StopRun();Runner.Flow.LoadLevel(level);
            first.ClearPauseSeconds = 2f; first.HandoffTravelSeconds = 1.5f;
            foreach(var scenario in new[]{"last-shot","last-shot-with-hostile","reserve-clear","ships-exited"})
            {
                string path=null;EnemyHealth target=null;PlayerProjectile pulse=null;EnemyProjectile hostile=null;
                SingleFireDefinition fire=null;GameObject muzzle=null;
                bool flightEnabled=false,attackEnabled=false;float killedAt=-1;int starts=0;
                Action died=()=>killedAt=Time.fixedTime;
                Action<EncounterDefinition,int> started=(d,t)=>{if(d==second)starts++;};
                try
                {
                    first.Completion=scenario=="reserve-clear" || scenario=="ships-exited"?EncounterCompletion.Timeline:EncounterCompletion.Eliminate;
                    first.CompleteWhenScheduleExhausted=scenario=="reserve-clear" || scenario=="ships-exited";
                    path=Fixture();Require(Owner.NewGame(),"Handoff fixture NewGame");Time.timeScale=1;
                    await Until(()=>Runner.CurrentIndex==0 && Runner.SpawnedCount==1,1,"Handoff target spawn");
                    target=EnemyHealth.Active.Single(e=>e.IsAlive && e.Definition.BodyDamagesPlayer);
                    var flight=target.GetComponent<EntryHoldFlight>();var attack=target.GetComponent<SingleFireAttack>();
                    flightEnabled=flight.enabled;attackEnabled=attack.enabled;flight.enabled=false;attack.enabled=false;
                    target.transform.position=new Vector3(0,1,0);target.GetComponent<Rigidbody2D>().position=target.transform.position;
                    Physics2D.SyncTransforms();target.Died+=died;Runner.EncounterStarted+=started;
                    bool hostileReleased=false;
                    if(scenario=="last-shot-with-hostile")
                    {
                        fire=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<SingleFireDefinition>("Assets/StarShift/Data/Enemies/ScoutSingleFire.asset"));
                        var data=new SerializedObject(fire);data.FindProperty("projectileSpeed").floatValue=1;data.ApplyModifiedPropertiesWithoutUndo();
                        muzzle=new GameObject("HandoffHostileProbe");muzzle.transform.position=new Vector3(5,3,0);
                        hostile=UnityEngine.Object.Instantiate(fire.ProjectilePrefab);
                        hostile.Launch(fire,muzzle.transform,target.gameObject,Player.gameObject,p=>{hostileReleased=true;p.ResetForPool();});
                    }
                    if(scenario=="reserve-clear")Require(Reserve.TryUse(),"Handoff reserve launch");
                    else if(scenario=="ships-exited")
                    {
                        flight.Configure(Vector2.zero,Vector2.zero,Vector2.zero,.05f,new Vector2(3,3),.05f,.05f);
                        flight.enabled=true;
                        await Task.Delay(160);
                        target.transform.position=new Vector3(3,3,0);
                        Physics2D.SyncTransforms();
                    }
                    else
                    {
                        var definition=Player.GetComponent<WeaponLoadout>().Definition(WeaponBranch.Pulse);
                        pulse=UnityEngine.Object.Instantiate(definition.ProjectilePrefab);
                        pulse.Launch(definition,target.transform.position-Vector3.up*.6f,Vector2.up,target.CurrentHp,0,Player.gameObject,p=>p.ResetForPool());
                    }
                    if(scenario!="ships-exited")
                        await Until(()=>killedAt>=0 && Runner.Stage!=EncounterStage.Running,1,"Handoff final kill");
                    await Until(()=>Runner.Stage==EncounterStage.Handoff,2,"Handoff enter");
                    var handoffStart=Time.fixedTime;
                    var presentation=UnityEngine.Object.FindAnyObjectByType<StarShift.UI.MissionIntroPresentation>();
                    Require(Runner.IsClearPause && !Runner.IsHandoffTravel,"Expected ClearPause active");
                    Require(presentation==null || !presentation.IsCueVisible,"Cue must be hidden during clear pause");
                    Session.Pause();var frozen=Time.fixedTime;await Task.Delay(100);
                    Require(Time.fixedTime==frozen && Runner.CurrentIndex==0,"Pause advanced clear wait");Session.Resume();
                    await Until(()=>Runner.IsHandoffTravel,3,"Travel enter");
                    var clearPauseSeconds=Time.fixedTime-handoffStart;
                    Require(Mathf.Abs(clearPauseSeconds-first.EffectiveClearPauseSeconds)<.08f,"Inconsistent clear pause: "+scenario+" "+clearPauseSeconds);
                    Require(presentation==null || presentation.IsCueVisible,"Cue must be visible during handoff travel");
                    await Until(()=>Runner.CurrentIndex==1 && Runner.Stage==EncounterStage.Running,3,"Handoff successor");
                    var travelSeconds=Time.fixedTime-(handoffStart+clearPauseSeconds);
                    Require(starts==1 && Mathf.Abs(travelSeconds-first.EffectiveHandoffTravelSeconds)<.08f,"Inconsistent travel duration: "+scenario+" "+travelSeconds);
                    Require(scenario!="last-shot-with-hostile" || hostileReleased,"Handoff skipped hostile cleanup");
                    samples.Add(new{scenario,clearPauseSeconds,expectedPause=first.EffectiveClearPauseSeconds,travelSeconds,expectedTravel=first.EffectiveHandoffTravelSeconds,starts,pauseFreeze=true});
                }
                finally
                {
                    Runner.EncounterStarted-=started;
                    if(target!=null){target.Died-=died;target.GetComponent<EntryHoldFlight>().enabled=flightEnabled;target.GetComponent<SingleFireAttack>().enabled=attackEnabled;}
                    if(pulse!=null)UnityEngine.Object.Destroy(pulse.gameObject);
                    if(hostile!=null)UnityEngine.Object.Destroy(hostile.gameObject);
                    if(muzzle!=null)UnityEngine.Object.Destroy(muzzle);
                    if(fire!=null)UnityEngine.Object.Destroy(fire);
                    Owner.ExitIsolated();if(path!=null)Directory.Delete(Path.GetDirectoryName(path),true);
                }
            }
            var result=new{pass=true,samples};Evidence("clear-handoff-delay",result);return result;
        }
        finally
        {
            Runner.StopRun();Runner.Flow.LoadLevel(original);Time.timeScale=originalScale;
            UnityEngine.Object.Destroy(first);UnityEngine.Object.Destroy(second);UnityEngine.Object.Destroy(level);
        }
    }

    public static async Task<object> PickupCarryOver()
    {
        var originalScale=Time.timeScale;var originalAudio=AudioListener.pause;
        var original=Runner.Flow.Level;
        var level=UnityEngine.Object.Instantiate(original);
        var first=UnityEngine.Object.Instantiate(original.Encounters[0]);
        var second=UnityEngine.Object.Instantiate(original.Encounters[1]);
        var director=Runner.GetComponent<RewardDirector>();
        var cleanup=Runner.GetComponent<EncounterCleanup>();
        var path=Fixture();
        float completionTime=-1;
        Action<EncounterDefinition> onCompleted=d=>{if(d==first)completionTime=Time.fixedTime;};
        Runner.Completed+=onCompleted;
        try
        {
            first.Completion=EncounterCompletion.Timeline;first.Duration=.15f;
            first.ClearPauseSeconds=.6f;first.HandoffTravelSeconds=.4f;
            first.CompleteWhenScheduleExhausted=false;first.Groups=Array.Empty<SpawnAction>();first.OptionalEvents=Array.Empty<OptionalEvent>();
            second.Duration=30;second.Groups=Array.Empty<SpawnAction>();second.OptionalEvents=Array.Empty<OptionalEvent>();second.CompleteWhenScheduleExhausted=false;
            level.Encounters=new[]{first,second};level.AdditionalStarts=Array.Empty<LevelStartPoint>();level.Intro.Enabled=false;
            Runner.StopRun();Runner.Flow.LoadLevel(level);Require(Owner.Continue(),"Carry fixture Continue");
            // Continue bắt đầu encounter 2; NewGame cung cấp checkpoint 1 mà không ghi save thật.
            Require(Owner.NewGame(),"Carry fixture NewGame");
            Time.timeScale=1;AudioListener.pause=false;
            await Until(()=>Runner.Stage==EncounterStage.Running,8,"Carry fixture intro");
            Player.GrantBriefProtection(30);
            Vector3 At(float x)=>Camera.main.ViewportToWorldPoint(new Vector3(x,.85f,-Camera.main.transform.position.z));
            var item=director.Spawn(Item("Overdrive"),null,At(.1f));
            var shard=director.Spawn(Item("StellarShard"),null,At(.3f));
            var crate=director.Spawn(null,AssetDatabase.LoadAssetAtPath<RewardDropTable>("Assets/StarShift/Data/Rewards/TestMissileAmmo.asset"),At(.9f));
            Require(item!=null && shard!=null && crate!=null,"Carry spawn");
            var token=cleanup.Token;var rewardToken=cleanup.RewardToken;
            await Until(()=>Runner.Stage==EncounterStage.Handoff,.6f,"Pickup delayed minor handoff with reward grace");
            Require(item.IsActive && shard.IsActive && crate.IsActive,"Completion deleted pickups");
            Session.Pause();var remaining=shard.Remaining;var position=shard.transform.position;
            await Task.Delay(120);Require(shard.Remaining==remaining && shard.transform.position==position,"Pause advanced carried pickup");Session.Resume();
            await Until(()=>Runner.CurrentIndex==1 && Runner.Stage==EncounterStage.Running,5,"Carry successor missing");
            var handoffSeconds=Time.fixedTime-completionTime;
            var expectedHandoff=first.EffectiveClearPauseSeconds+first.EffectiveHandoffTravelSeconds;
            Require(completionTime>=0 && handoffSeconds>=.9f && Mathf.Abs(handoffSeconds-expectedHandoff)<.08f,"Minor breathing pause missing/too long: "+handoffSeconds);
            Require(cleanup.Token!=token && cleanup.RewardToken==rewardToken && !cleanup.Register(item,token),"Stale encounter callback accepted");
            Require(item.IsActive && shard.IsActive && crate.IsActive && item.Remaining<11 && shard.Remaining<4,"Handoff deleted or renewed lifetime");
            var shards=Owner.PersistentSnapshot.Profile.StellarShardBalance;
            Require(shard.TryCollect() && Owner.PersistentSnapshot.Profile.StellarShardBalance==shards+1,"Carried Shard collection/profile credit");
            Require(item.TryCollect() && crate.TryCollect() && director.ActivePickups==0,"Carried powerup/crate collection");
            Require(Owner.Flush(),"Carry flush");
            var stranded=director.Spawn(Item("Overdrive"),null,At(.9f));
            Session.Pause();Require(Owner.RetryEncounter(),"Carry retry");
            Require(!stranded.IsActive && director.ActivePickups==0 && !shard.TryCollect(),"Retry retained world reward/callback");
            Require(Owner.PersistentSnapshot.Profile.StellarShardBalance==shards+1,"Retry rolled back Profile");
            var expires=director.Spawn(Item("StellarShard"),null,At(.9f));
            await Until(()=>!expires.IsActive,5,"Carried reward stopped natural expiry");
            var reset=director.Spawn(Item("Overdrive"),null,At(.9f));Runner.StopRun();
            Require(!reset.IsActive && director.ActivePickups==0,"Run reset retained pickup");
            var result=new{pass=true,powerupShardCrate=true,handoffSeconds,naturalLifetime=true,pauseFreeze=true,staleTokenRejected=true,profileCredit=true,retryAndResetCleanup=true};
            Evidence("pickup-carry-over",result);return result;
        }
        finally
        {
            Runner.Completed-=onCompleted;
            Owner.ExitIsolated();Runner.Flow.LoadLevel(original);
            UnityEngine.Object.Destroy(first);UnityEngine.Object.Destroy(second);UnityEngine.Object.Destroy(level);
            Directory.Delete(Path.GetDirectoryName(path),true);
            Time.timeScale=originalScale;AudioListener.pause=originalAudio;
        }
    }
    public static object BeginChecks()
    {
        Evidence("progress",new{status="running"});_=Checks();return new{started=true};
    }
    static async Task Checks()
    {
        try { Thermal();await MissileAndImpact();await ShieldRespawn();await Pickups();await EmptyEncounter();Evidence("progress",new{status="PASS"}); }
        catch(Exception e){Evidence("progress",new{status="FAILED",error=e.ToString()});}
    }
    static (Type type, object rig) ReuseRig(int width)
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("VerifyM4Testbed")).LastOrDefault(t=>t!=null);
        Require(type!=null,"Run VerifyM4Testbed.Weapons once to load shared verifier fixture.");
        var rigType=type.GetNestedType("Rig",BindingFlags.NonPublic);
        return (type,Activator.CreateInstance(rigType,new object[]{width,6f,false}));
    }
    static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method).Invoke(target,args);
    public static async Task<object> HomingMechanics()
    {
        var fixture=ReuseRig(1600);
        using((IDisposable)fixture.rig)
        {
            fixture.type.GetMethod("Restart",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{fixture.rig,"ENCOUNTER_2",true,false});
            await Task.Delay(2200);
            var all=UnityEngine.Object.FindObjectsByType<EnemyHealth>().Where(e=>e.IsAlive).ToArray();
            Require(all.Length>=2,"Homing targets");
            foreach(var enemy in all){Call(fixture.rig,"Freeze",enemy);enemy.transform.position=new Vector3(50,4,0);enemy.GetComponent<Rigidbody2D>().position=enemy.transform.position;}
            for(int i=0;i<2;i++){all[i].transform.position=new Vector3(.8f+i*.55f,0,0);all[i].GetComponent<Rigidbody2D>().position=all[i].transform.position;}
            var before=all.Take(2).Select(e=>e.CurrentHp).ToArray();
            var definition=Player.GetComponent<WeaponLoadout>().Definition(WeaponBranch.Homing);
            var shot=UnityEngine.Object.Instantiate(definition.ProjectilePrefab);bool released=false;
            try
            {
                shot.Launch(definition,new Vector3(0,-3,0),Vector2.up,2,.85f,Player.gameObject,p=>{released=true;p.ResetForPool();});
                await Task.Delay(100);var angle=Vector3.Angle(shot.transform.up,Vector3.up);
                Require(angle>1 && angle<18,"Bounded tracking missing");
                await Until(()=>released,2,"Missile hit");
                Require(all[0].CurrentHp==before[0]-2 && all[1].CurrentHp==before[1]-2,"Splash double/missing hit");
                var collider=shot.GetComponent<BoxCollider2D>().size;
                foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})
                {
                    var d=Player.GetComponent<WeaponLoadout>().Definition(branch);
                    shot.Launch(d,new Vector3(0,-4,0),Vector2.up,1,0,Player.gameObject,p=>p.ResetForPool());
                    var visual=shot.GetComponentInChildren<SpriteRenderer>();var trail=shot.GetComponentInChildren<TrailRenderer>();
                    Require(shot.transform.localScale==Vector3.one && shot.GetComponent<BoxCollider2D>().size==collider,"Visual scale changed collider");
                    Require(d.ProjectileSprite!=null && visual.sprite==d.ProjectileSprite && trail.emitting==(d.TrailSeconds>0),"Pool reuse retained wrong sprite/trail");
                    shot.Release();Require(trail.positionCount==0 && !trail.emitting,"Pool retained trail vertices");
                }
                Require(new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing}.Select(b=>Player.GetComponent<WeaponLoadout>().Definition(b).ProjectileSprite).Distinct().Count()==3,"Primary silhouettes not distinct");
                var result=new{pass=true,trackingAngle=angle,splash=true,visualOnlyScale=true,distinctSprites=true,poolTrailReset=true};Evidence("homing",result);return result;
            }
            finally{UnityEngine.Object.Destroy(shot.gameObject);}
        }
    }
    public static async Task<object> ReserveImpactPreview()
    {
        var fixture=ReuseRig(1600);
        using((IDisposable)fixture.rig)
        {
            var path=Fixture();var camera=Camera.main;var view=camera.GetComponent<GameplayViewController>();
            var folder=Path.GetFullPath("../.design/reserve-impact/frames");Directory.CreateDirectory(folder);
            var recorder=new GameObject("ReserveImpactCapture").AddComponent<ReserveImpactFrameRecorder>();
            int detonations=0;float maxX=0,maxY=0;
            var samples=new System.Collections.Generic.List<object>();
            var position=camera.transform.position;var rotation=camera.transform.rotation;
            var playerPosition=Player.transform.position;var matrix=camera.projectionMatrix;
            var start=Time.time;
            Action detonation=()=>detonations++;
            Action<ScriptableRenderContext,Camera> observe=(context,c)=>
            {
                if(c!=camera)return;
                var m=c.projectionMatrix;
                maxX=Mathf.Max(maxX,Mathf.Abs(m.m03-matrix.m03)*Screen.width*.5f);
                maxY=Mathf.Max(maxY,Mathf.Abs(m.m13-matrix.m13)*Screen.height*.5f);
                samples.Add(new{frame=recorder.Frames.Count,seconds=Time.time-start,impact=view.ImpactRemaining,clipX=m.m03-matrix.m03,clipY=m.m13-matrix.m13});
            };
            try
            {
                await Task.Delay(1700);playerPosition=Player.transform.position;
                position=camera.transform.position;rotation=camera.transform.rotation;matrix=camera.projectionMatrix;
                Reserve.Detonated+=detonation;RenderPipelineManager.beginCameraRendering+=observe;
                start=Time.time;recorder.Begin(55);
                await Until(()=>recorder.Frames.Count>=8,2,"Preview pre-roll");
                Require(Reserve.TryUse(),"Preview missile launch");
                await Until(()=>recorder.Frames.Count>=55 && recorder.Pending==0,4,"Preview frame capture");
                Require(!recorder.Failed,"Preview GPU readback failed");
                Require(detonations==1 && view.ImpactRemaining==0,"Preview detonation/expiry");
                Require(maxX>12 && maxX<52 && maxY<30,"Impact not clearly visible or excessive: "+maxX+"/"+maxY);
                Require(camera.transform.position==position && camera.transform.rotation==rotation && camera.projectionMatrix==matrix,"Preview camera drift");
                Require(Vector3.Distance(Player.transform.position,playerPosition)<.001f,"Shake moved Player");
                // Encode sau khi quay xong để PNG IO không làm hụt frame impact.
                var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false);
                try
                {
                    for(int i=0;i<recorder.Frames.Count;i++)
                    {
                        texture.LoadRawTextureData(recorder.Frames[i]);texture.Apply();
                        File.WriteAllBytes(Path.Combine(folder,"frame-"+i.ToString("D3")+".png"),texture.EncodeToPNG());
                    }
                }
                finally {UnityEngine.Object.Destroy(texture);}
                var result=new{pass=true,strength=view.ImpactStrength,duration=.14f,width=Screen.width,height=Screen.height,frames=recorder.Frames.Count,maxPixelsX=maxX,maxPixelsY=maxY,detonations,playerStable=true,cameraStable=true,folder,samples};
                Evidence("reserve-impact-preview",result);return result;
            }
            finally
            {
                Reserve.Detonated-=detonation;RenderPipelineManager.beginCameraRendering-=observe;
                UnityEngine.Object.Destroy(recorder.gameObject);
                Owner.ExitIsolated();if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);
            }
        }
    }

    public static async Task<object> Capture(int width)
    {
        var fixture=ReuseRig(width);var paths=new System.Collections.Generic.List<string>();
        using((IDisposable)fixture.rig)
        {
            Directory.CreateDirectory("../.design/gameplay-feel/screenshots");
            foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})
            {
                fixture.type.GetMethod("Restart",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{fixture.rig,"ENCOUNTER_2",true,false});
                var loadout=Player.GetComponent<WeaponLoadout>();
                foreach(var b in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})while(loadout.Level(b)<3)loadout.ApplyCore(b);
                while(loadout.ActiveBranch!=branch)loadout.Cycle();
                Call(fixture.rig,"Place",new Vector2(-1.8f,-3.8f));
                await (Task)Call(fixture.rig,"MouseEvent",new Vector2(400,220),true);await Task.Delay(450);
                var path=Path.GetFullPath("../.design/gameplay-feel/screenshots/"+branch+"-"+width+".png");ScreenCapture.CaptureScreenshot(path);await Task.Delay(100);paths.Add(path);
                await (Task)Call(fixture.rig,"MouseEvent",new Vector2(400,220),false);
            }
            Reserve.RestoreCheckpoint(3);Require(Reserve.TryUse(),"Visual missile activation");
            await Task.Delay(200);
            var launch=Path.GetFullPath("../.design/gameplay-feel/screenshots/reserve-flight-"+width+".png");ScreenCapture.CaptureScreenshot(launch);await Task.Delay(40);paths.Add(launch);
            await Until(()=>!Reserve.IsInFlight,1,"Capture detonation timeout");
            var blast=Path.GetFullPath("../.design/gameplay-feel/screenshots/reserve-impact-"+width+".png");ScreenCapture.CaptureScreenshot(blast);await Task.Delay(150);paths.Add(blast);
            Require(paths.All(File.Exists),"Capture missing");
            var missile=UnityEngine.Object.FindObjectsByType<Button>().Single(b=>b.name=="EmergencyMissile").GetComponent<RectTransform>();
            var weapon=GameObject.Find("GameplayUI").transform.Find("SafeArea/CombatHud/Weapon").GetComponent<RectTransform>();
            var corners=new Vector3[4];missile.GetWorldCorners(corners);var missileRect=Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
            weapon.GetWorldCorners(corners);var weaponRect=Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
            Require(!missileRect.Overlaps(weaponRect),"Reserve button overlaps weapon selector");
        }
        var result=new{pass=true,width,paths};Evidence("visual-"+width,result);return result;
    }

    public static object BeginFullMission()
    {
        Evidence("full-mission",new{status="running"});_=FullMissionWithIntro();return new{started=true};
    }
    static async Task FullMissionWithIntro()
    {
        var original=Runner.Flow.Level;var enabled=original.Intro.Enabled;
        var copy=UnityEngine.Object.Instantiate(original);copy.Intro.Enabled=true;
        try
        {
            Require(original.Intro.Enabled==enabled,"Intro fixture aliased original");
            Runner.StopRun();Runner.Flow.LoadLevel(copy);
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("VerifyM4Testbed")).Last(t=>t!=null);
            var result=await (Task<object>)type.GetMethod("FullMission").Invoke(null,new object[]{1600,true});
            Evidence("full-mission",result);
        }
        catch(Exception e){Evidence("full-mission",new{pass=false,error=e.ToString()});}
        finally{Runner.StopRun();Runner.Flow.LoadLevel(original);UnityEngine.Object.Destroy(copy);}
    }

    public static async Task<object> ButtonInput()
    {
        var fixture=ReuseRig(1600);
        using((IDisposable)fixture.rig)
        {
            Reserve.RestoreCheckpoint(2);await Task.Delay(120);
            var button=UnityEngine.Object.FindObjectsByType<Button>().Single(b=>b.name=="EmergencyMissile");
            var point=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            var input=Player.GetComponent<InputSystemPointerInput>();var weapon=Player.GetComponent<PulseLaserWeapon>();
            await (Task)Call(fixture.rig,"MouseEvent",point,false);
            var position=Player.transform.position;var shots=weapon.ShotsFired;
            await (Task)Call(fixture.rig,"MouseEvent",point,true);await (Task)Call(fixture.rig,"MouseEvent",point,false);
            Require(Reserve.Current==1 && Player.transform.position==position && weapon.ShotsFired==shots && !input.FireHeld,"Reserve click leaked drag/fire or did not activate");
            var result=new{pass=true,stock=Reserve.Current,pointerConsumed=true,cameraDidNotMovePlayer=true};Evidence("button-input",result);return result;
        }
    }

    public static async Task<object> TerminalCueCleanup()
    {
        Start();Player.RestoreCheckpoint(1);Gate.SetState(PlayerGameplayState.Active);
        var presentation=Player.GetComponent<EmergencyMissilePresentation>();
        var gate=Gate;
        bool fired=false;
        Exception callbackError=null;
        Action pending=()=>
        {
            if(gate.State!=PlayerGameplayState.GameOver)return;
            try
            {
                typeof(EmergencyMissilePresentation).GetMethod("OnDetonated",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(presentation,null);
                fired=true;
            }
            catch(Exception error){callbackError=error;}
        };
        gate.StateChanged+=pending;
        try
        {
            Require(Player.TryReceiveHit(null),"Terminal setup hit");
            await Until(()=>Session.IsTerminal,2,"GameOver timeout");
            Require(callbackError==null,"Terminal cue callback failed: "+callbackError);
            Require(fired && !Player.transform.Find("MissileBurst").GetComponent<LineRenderer>().enabled && Camera.main.GetComponent<GameplayViewController>().ImpactRemaining==0,"Terminal froze pending cue");
            var result=new{pass=true,gameOver=true,burstCleared=true,impactCleared=true};Evidence("terminal-cue",result);return result;
        }
        finally{gate.StateChanged-=pending;Runner.StopRun();}
    }

    public static async Task<object> MissileCancellation()
    {
        var path=Fixture();
        int detonations=0;Action detonate=()=>detonations++;Reserve.Detonated+=detonate;
        try
        {
            Require(Reserve.TryUse() && Reserve.Current==1,"Cancellation launch");
            await Task.Delay(80);Session.Pause();
            await Task.Delay(30);
            var rocket=Player.transform.Find("MissileFlight").GetComponent<SpriteRenderer>();
            var trail=rocket.GetComponentInChildren<TrailRenderer>();
            var position=rocket.transform.position;var progress=Reserve.FlightProgress;
            await Task.Delay(100);
            Require(Reserve.IsInFlight && Reserve.FlightProgress==progress && rocket.transform.position==position && detonations==0,"Paused missile advanced");
            Require(Owner.RetryEncounter(),"Retry mid-flight rejected");
            Require(!Reserve.IsInFlight && Reserve.Current==2 && !rocket.enabled && trail.positionCount==0,"Retry retained missile visual/pending state");
            await Task.Delay(450);Require(detonations==0,"Old flight detonated after Retry");
            Require(Reserve.TryUse(),"StopRun launch");Runner.StopRun();
            await Task.Delay(450);Require(!Reserve.IsInFlight && detonations==0 && !rocket.enabled,"Stopped run detonated later");
            Require(Owner.Continue() && Reserve.TryUse(),"Continue launch");Owner.ExitIsolated();
            Require(!Reserve.IsInFlight && !rocket.enabled && trail.positionCount==0,"Exit retained pending missile");
            var result=new{pass=true,pauseVisualFrozen=true,retryCancelled=true,stockRestored=true,stoppedRunCancelled=true,exitCancelled=true};
            Evidence("missile-cancellation",result);return result;
        }
        finally
        {
            Reserve.Detonated-=detonate;Owner.ExitIsolated();
            Directory.Delete(Path.GetDirectoryName(path),true);
        }
    }

}

public sealed class ReserveImpactFrameRecorder : MonoBehaviour
{
    public readonly System.Collections.Generic.List<byte[]> Frames=new System.Collections.Generic.List<byte[]>();
    public int Pending {get;private set;}
    public bool Failed {get;private set;}
    public void Begin(int count)=>StartCoroutine(Record(count));
    System.Collections.IEnumerator Record(int count)
    {
        for(int i=0;i<count;i++)
        {
            yield return new WaitForEndOfFrame();
            var target=RenderTexture.GetTemporary(Screen.width,Screen.height,0,RenderTextureFormat.ARGB32);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(target);
            int index=Frames.Count;Frames.Add(null);Pending++;
            AsyncGPUReadback.Request(target,0,TextureFormat.RGBA32,request=>
            {
                if(request.hasError)Failed=true;
                else Frames[index]=request.GetData<byte>().ToArray();
                RenderTexture.ReleaseTemporary(target);Pending--;
            });
        }
    }
}
