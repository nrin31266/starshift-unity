using System;
using System.Collections.Generic;
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
using Rig = VerifyPacingRefinement.Rig;

public static class VerifyBalancePass
{
    const string Output="../.design/m5-balance-pass/";
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static EncounterRunner Runner=>UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession Session=>Runner.GetComponent<GameplaySession>();
    static T Asset<T>(string path) where T:UnityEngine.Object=>StarShift.Editor.RegionAAssetPaths.Load<T>("Assets/StarShift/"+path+".asset");
    static LevelDefinition Level(int n)=>Asset<LevelDefinition>("Data/RegionA/Mission0"+n);
    static PickupDefinition Item(string n)=>Asset<PickupDefinition>("Data/Rewards/"+n);
    static void Require(bool b,string message){if(!b)throw new InvalidOperationException(message);}
    static void Evidence(string n,object value)=>File.WriteAllText(Output+n+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    static async Task Until(Func<bool> condition,int ms,string message)
    {float end=Time.realtimeSinceStartup+ms/1000f;while(!condition()&&Time.realtimeSinceStartup<end)await Task.Delay(15);Require(condition(),message);}
    static void Start(Rig rig,int mission,int encounter=0)
    {Runner.StopRun();Runner.Flow.LoadLevel(Level(mission));Runner.StartRun("ENCOUNTER_"+(encounter+1),true,true,null,null);rig.Protect();}
    static void Set(object o,string n,object value)=>o.GetType().GetField(n,Fields).SetValue(o,value);
    static async Task Capture(string n){ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output+"screenshots/"+n+".png"));await Task.Delay(150);}
    public static object BeginSystems(){_=Systems();return new{started=true};}
    static async Task Systems()
    {
        Evidence("systems",new{status="RUNNING"});
        var results=new List<object>();
        try{using(var rig=new Rig(1600,6)){
            Start(rig,1);var loadout=rig.Player.GetComponent<WeaponLoadout>();var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();
            foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing}){
                loadout.ResetForSession();var core=Item(branch==WeaponBranch.Pulse?"PulseCore":branch==WeaponBranch.Plasma?"PlasmaCore":"HomingCore");
                while(loadout.Level(branch)<WeaponLoadout.MaximumLevel){Require(receiver.TryCollect(core),"Core upgrade");Require(loadout.ActiveBranch==branch,"Core did not auto-select");}
                Require(!receiver.TryCollect(core)&&loadout.Level(branch)==5,"Max core boundary");
                for(int level=1;level<=5;level++){
                    var levels=new[]{1,0,0};levels[(int)branch]=level;loadout.Restore(branch,levels);
                    var before=rig.Player.GetComponent<PulseLaserWeapon>().ShotsFired;
                    await rig.MouseEvent(new Vector2(600,300),true);await Task.Delay(550);await rig.MouseEvent(new Vector2(600,300),false);
                    var emitted=rig.Player.GetComponent<PulseLaserWeapon>().ShotsFired-before;
                    Require(emitted>=loadout.ActiveDefinition.Pattern(level).ProjectileCount,"Pattern did not fire "+branch+level);
                    results.Add(new{branch=branch.ToString(),level,emitted});
                }
            }
            loadout.Restore(WeaponBranch.Pulse,new[]{5,4,5});loadout.ApplyDeathDowngrade();Require(loadout.CaptureLevels().SequenceEqual(new[]{4,4,5}),"Active-only death downgrade");
            var roller=new RewardRoller();roller.Reset(410);loadout.Restore(WeaponBranch.Pulse,new[]{5,5,5});
            var table=Asset<RewardDropTable>("Data/Rewards/SupplyCrateDrops");
            Require(roller.SelectMajor(table,receiver,rig.Lives,loadout,false,true)==null,"Maxed supply core filter");
            Require(roller.SelectMajor(table,receiver,rig.Lives,loadout,false,false)?.Kind!=PickupKind.WeaponCore,"Support filter");
            Runner.StopRun();var levelClone=UnityEngine.Object.Instantiate(Level(1));var e=UnityEngine.Object.Instantiate(levelClone.Encounters[0]);
            e.Groups=new[]{new SpawnAction{Count=1,EnemyPrefab=StarShift.Editor.RegionAAssetPaths.Load<GameObject>("Assets/StarShift/Prefabs/Enemies/SupplyDrone.prefab"),Sequence=Asset<FlightSequence>("Data/RegionA/TopHold"),RewardOverride=table}};
            e.EntryBeats=Array.Empty<EntryBeatRule>();e.GuaranteedMajorCount=0;e.OptionalEvents=Array.Empty<OptionalEvent>();levelClone.Encounters=new[]{e};levelClone.AdditionalStarts=Array.Empty<LevelStartPoint>();
            Runner.Flow.LoadLevel(levelClone);Runner.StartRun("LEVEL_START",true,true,null,null);rig.Protect();loadout.Restore(WeaponBranch.Pulse,new[]{2,1,1});
            await Until(()=>EnemyHealth.Active.Any(x=>x.IsAlive),2000,"Supply spawn");var supply=EnemyHealth.Active.First(x=>x.IsAlive);
            var director=Runner.GetComponent<RewardDirector>();var fillers=new List<RewardPickup>();for(int i=0;i<6;i++){var filler=director.Spawn(Item("Overdrive"),null,new Vector3(-6+i,3,0));Require(filler!=null,"Fill pickup cap");fillers.Add(filler);}
            rig.Freeze(supply);supply.transform.position=new Vector3(0,1,0);supply.GetComponent<Rigidbody2D>().position=supply.transform.position;
            int count=director.PickupsSpawned;rig.Gate.SetState(PlayerGameplayState.Dying);supply.ReceivePlayerHit(supply.CurrentHp,rig.Player);

            Require(director.PickupsSpawned-count==2,"Supply must emit exactly two pickups at full cap");
            var all=UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive).ToArray();
            Require(all.All(p=>p.Crate==null),"Supply created BOX");var corePickup=all.Single(p=>p.Item?.Kind==PickupKind.WeaponCore);
            Require(!corePickup.TryCollect(),"Closed gate collected supply");rig.Protect();
            Require(Runner.GetComponent<RunRewards>().SupplyCaptured==0,"Uncollected supply awarded capture");
            var supportPickup=all.Single(p=>p!=corePickup&&!fillers.Contains(p));foreach(var clutter in fillers)clutter.Release();
            var start=corePickup.transform.position;await Task.Delay(180);await Capture("supply-pair");await Task.Delay(230);Require(Vector3.Distance(start,corePickup.transform.position)>.05f,"No gentle ejection");
            Session.Pause();var paused=corePickup.transform.position;await Task.Delay(160);Require(Vector3.Distance(paused,corePickup.transform.position)<.001f,"Ejection moved while paused");Session.Resume();rig.Protect();
            var coreBranch=corePickup.Item.Branch;Require(corePickup.TryCollect()&&loadout.ActiveBranch==coreBranch,"Direct core collection/switch");
            Require(Runner.GetComponent<RunRewards>().SupplyCaptured==1,"First supply collection missing");Require(supportPickup.TryCollect(),"Support collection");Require(Runner.GetComponent<RunRewards>().SupplyCaptured==1,"Supply counted more than once");Runner.StopRun();UnityEngine.Object.Destroy(levelClone);UnityEngine.Object.Destroy(e);
            var pressure=new List<object>();
            foreach(var test in new[]{(1,0,1),(3,1,4),(3,2,6)}){
                Start(rig,test.Item1,test.Item2);EncounterDefinition copy=null;LevelDefinition lc=null;
                if(test.Item3==6){Runner.StopRun();lc=UnityEngine.Object.Instantiate(Level(3));copy=UnityEngine.Object.Instantiate(lc.Encounters[2]);copy.ActiveShooterLimit=6;copy.ShotSpacing=.06f;lc.Encounters=new[]{copy};lc.AdditionalStarts=Array.Empty<LevelStartPoint>();Runner.Flow.LoadLevel(lc);Runner.StartRun("LEVEL_START",true,true,null,null);rig.Protect();}
                int peak=0;float end=Time.realtimeSinceStartup+10;while(Time.realtimeSinceStartup<end){peak=Math.Max(peak,EnemyHealth.Active.Count(x=>x.IsAlive&&x.GetComponent<SingleFireAttack>()?.IsTelegraphing==true));await Task.Delay(20);}
                Require(peak<=test.Item3&&peak>=test.Item3,"Shooter budget did not reach expected limit: "+test+" peak="+peak);pressure.Add(new{limit=test.Item3,peak});
                await Capture("pressure-"+test.Item3);Runner.StopRun();if(lc!=null)UnityEngine.Object.Destroy(lc);if(copy!=null)UnityEngine.Object.Destroy(copy);
            }
            Start(rig,4,1);await Until(()=>UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>()!=null,2500,"Boss spawn");
            var boss=UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>();int priorShots=boss.GetComponentsInChildren<BossAttackModule>().Sum(m=>m.ShotsFired);await Until(()=>boss.GetComponentsInChildren<BossAttackModule>().Sum(m=>m.ShotsFired)-priorShots>=11,16000,"Boss cannon/fan density");await Capture("boss-denser-fire");
            Evidence("systems",new{status="PASS",patterns=results,pressure,supplyPair=true,capOverflow=true,autoSwitch=true,deathDowngrade=true,bossProjectileCount=boss.GetComponentsInChildren<BossAttackModule>().Sum(m=>m.ShotsFired)-priorShots});
        }}catch(Exception ex){Evidence("systems",new{status="FAILED",error=ex.ToString(),results});}
    }

    public static object BeginCameras(){_=Cameras();return new{started=true};}
    static async Task Cameras()
    {
        Evidence("cameras",new{status="RUNNING"});var results=new List<object>();
        try{foreach(int width in new[]{1600,2000})using(var rig=new Rig(width,6)){
            Runner.StopRun();var level=UnityEngine.Object.Instantiate(Level(1));level.Intro=JsonUtility.FromJson<MissionIntroSettings>(JsonUtility.ToJson(Level(1).Intro));level.Intro.Enabled=false;
            Runner.Flow.LoadLevel(level);Runner.StartRun("LEVEL_START",true,false,null,null);Require(Runner.Stage==EncounterStage.Running,"Intro.Enabled=false ignored");
            Runner.StopRun();level.Intro.Enabled=true;Runner.StartRun("LEVEL_START",true,false,null,null);Require(Runner.Flow.IsAwaitingStart,"Enabled intro missing");
            await Until(()=>Runner.Flow.CanContinue,3000,"Intro readiness");Session.BeginMission();
            await Until(()=>Runner.Flow.IntroPhase==MissionIntroPhase.Brake,4000,"Brake");
            var camera=Camera.main;float previous=camera.orthographicSize;float largestReverse=0;var sizes=new List<float>();
            var presentation=UnityEngine.Object.FindAnyObjectByType<StarShift.UI.MissionIntroPresentation>();var ship=(SpriteRenderer)presentation.GetType().GetField("ship",Fields).GetValue(presentation);
            Vector3 lastShip=Vector3.zero;bool hadShip=false;
            while(Runner.Stage==EncounterStage.Intro){float size=camera.orthographicSize;largestReverse=Math.Max(largestReverse,size-previous);previous=size;sizes.Add(size);if(ship.gameObject.activeInHierarchy){lastShip=camera.WorldToViewportPoint(ship.transform.position);hadShip=true;}await Task.Delay(10);}
            var playerViewport=camera.WorldToViewportPoint(rig.Player.transform.position);float firstGap=hadShip?Vector2.Distance(lastShip,playerViewport):0;
            Require(largestReverse<.001f&&Mathf.Abs(camera.orthographicSize-5.8f)<.002f,"Initial zoom reversed/snapped");Require(firstGap<.015f,"Initial intro handoff jump "+firstGap);rig.Protect();
            results.Add(new{width,initialIntroGap=firstGap,largestReverse,initialFinalSize=camera.orthographicSize});UnityEngine.Object.Destroy(level);
            Start(rig,1,1);Runner.StartRun("ENCOUNTER_2",false,true,null,null);rig.Protect();await Until(()=>Runner.SpawnedCount==12,3000,"M1 formation");
            foreach(var enemy in EnemyHealth.Active.Where(x=>x.IsAlive).ToArray())enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
            await Until(()=>Runner.GetComponent<RegionCampaignFlow>().IsMissionTravel,5000,"Mission travel");
            previous=camera.orthographicSize;float reverse=0,largestStep=0;var transition=new List<object>();
            while(Runner.Flow.Level.Id=="L1"||Runner.Elapsed<1){float size=camera.orthographicSize;reverse=Math.Max(reverse,previous-size);largestStep=Math.Max(largestStep,Mathf.Abs(size-previous));previous=size;transition.Add(new{frame=Time.frameCount,size,mission=Runner.Flow.Level.Id});await Task.Delay(10);}
            Require(reverse<.002f&&largestStep<.10f&&Mathf.Abs(camera.orthographicSize-6.6f)<.002f,"Mission boundary camera snap/reversal");results.Add(new{width,missionReverse=reverse,largestStep,transition});
            Runner.StopRun();Runner.Flow.LoadLevel(Level(4));Require(Runner.GetComponent<RegionCampaignFlow>().QueueAdvance(),"Planet journey queue");
            var journey=Runner.GetComponent<PlanetJourney>();await Until(()=>journey.IsAwaitingTap,9000,"Planet reveal");Session.BeginMission();
            await Until(()=>journey.Phase==PlanetJourneyPhase.Brake,3500,"Second brake");
            float gap=0;lastShip=Vector3.zero;hadShip=false;var frames=new List<object>();int screenshot=0;
            while(journey.IsActive){if(ship.gameObject.activeInHierarchy){lastShip=camera.WorldToViewportPoint(ship.transform.position);hadShip=true;}frames.Add(new{frame=Time.frameCount,size=camera.orthographicSize,ship=lastShip.ToString(),player=camera.WorldToViewportPoint(rig.Player.transform.position).ToString()});if(++screenshot==15)await Capture("second-brake-"+width);await Task.Delay(10);}
            gap=hadShip?Vector2.Distance(lastShip,camera.WorldToViewportPoint(rig.Player.transform.position)):0;Require(gap<.015f,"Second intro handoff jumped "+gap);
            Require(rig.Gate.CanInput&&!rig.Input.FireHeld,"Second intro input handoff");await Capture("second-ready-"+width);results.Add(new{width,secondIntroGap=gap,frames});
        }Evidence("cameras",new{status="PASS",results});}catch(Exception ex){Evidence("cameras",new{status="FAILED",error=ex.ToString(),results});}
    }

    public static object BeginPersistence(){_=Persistence();return new{started=true};}
    static async Task Persistence()
    {
        Evidence("persistence",new{status="RUNNING"});var owner=Runner.GetComponent<CampaignLifecycle>();string original=owner.SavePath;
        string Bytes(string p)=>File.Exists(p)?Convert.ToBase64String(File.ReadAllBytes(p)):null;string before=Bytes(original);
        try{using(var rig=new Rig(1600,6)){
            string path=Path.Combine(Path.GetTempPath(),"starshift-five-level-"+Guid.NewGuid().ToString("N"),"save.json");
            var data=new SaveEnvelope{SchemaVersion=SaveValidation.CurrentVersion,HasCampaign=true,Settings=SettingsData.Defaults(),Profile=new PlayerProfile{UnlockedMissions=new[]{"L1"},Records=Array.Empty<MissionRecord>()},Campaign=new CampaignSave{MissionId="L1",Checkpoint=new CheckpointData{EncounterId=Level(1).Encounters[0].Id,Lives=3,Missiles=1,ActiveWeapon=WeaponBranch.Pulse,WeaponLevels=new[]{5,4,5},Stats=new RunStatsData()}}};
            bool Valid(SaveEnvelope d)=>SaveValidation.IsValid(d,(m,e)=>m=="L1"&&e==Level(1).Encounters[0].Id,5);
            var store=new JsonSaveStore(path);Require(store.Write(data,Valid),"Lv5 save write");Require(store.Load(Valid,out var read)==SaveLoadStatus.Loaded&&read.Campaign.Checkpoint.WeaponLevels.SequenceEqual(new[]{5,4,5}),"Lv5 disk roundtrip");
            data.Campaign.Checkpoint.WeaponLevels[0]=6;Require(!Valid(data),"Lv6 accepted");data.Campaign.Checkpoint.WeaponLevels=new[]{3,2,1};Require(Valid(data),"Old Lv1-3 save rejected");
            owner.UseTestStorage(path);Require(owner.Continue(),"Continue Lv5");rig.Protect();var loadout=rig.Player.GetComponent<WeaponLoadout>();
            Require(loadout.CaptureLevels().SequenceEqual(new[]{5,4,5}),"Lv5 restore");Require(rig.Player.GetComponent<PlayerPickupReceiver>().TryCollect(Item("PlasmaCore"))&&loadout.ActiveBranch==WeaponBranch.Plasma&&loadout.ActiveLevel==5,"Lv4->5 autoswitch");
            Session.Pause();Require(owner.RetryEncounter(),"Retry");rig.Protect();Require(loadout.ActiveBranch==WeaponBranch.Pulse&&loadout.CaptureLevels().SequenceEqual(new[]{5,4,5}),"Retry upgrade rollback");
            Session.Pause();Require(owner.Continue(),"Continue");rig.Protect();Require(loadout.ActiveLevel==5,"Continue lost Lv5");Require(owner.Flush(),"Flush");var campaign=Bytes(path);
            Require(owner.BeginReplay(),"Replay");rig.Protect();rig.Player.GetComponent<PlayerPickupReceiver>().TryCollect(Item("PlasmaCore"));owner.Flush();Require(campaign==Bytes(path),"Replay wrote campaign");Start(rig,2);owner.Flush();Require(campaign==Bytes(path),"Dev wrote campaign");
            Evidence("persistence",new{status="PASS",fiveLevelRoundtrip=true,oldSaveAccepted=true,invalidSixRejected=true,retryRollback=true,replayDevIsolation=true,productionUnchanged=before==Bytes(original)});
        }}catch(Exception ex){Evidence("persistence",new{status="FAILED",error=ex.ToString()});}finally{owner.ExitIsolated();owner.UseTestStorage(original);Require(before==Bytes(original),"Production save changed");}
        await Task.CompletedTask;
    }
    public static object BeginProgression(){_=Progression();return new{started=true};}
    static async Task Progression()
    {
        Evidence("progression",new{status="RUNNING"});var timings=new List<object>();var quotaResults=new List<object>();
        try{using(var rig=new Rig(1600,6)){
            var loadout=rig.Player.GetComponent<WeaponLoadout>();var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();
            foreach(var test in new[]{(WeaponBranch.Pulse,1),(WeaponBranch.Pulse,2),(WeaponBranch.Pulse,3),(WeaponBranch.Pulse,4),(WeaponBranch.Pulse,5),(WeaponBranch.Plasma,3),(WeaponBranch.Plasma,5),(WeaponBranch.Homing,3),(WeaponBranch.Homing,5)}){
                Runner.StopRun();var lc=UnityEngine.Object.Instantiate(Level(1));var e=UnityEngine.Object.Instantiate(lc.Encounters[0]);
                e.Groups=new[]{new SpawnAction{Count=1,EnemyPrefab=StarShift.Editor.RegionAAssetPaths.Load<GameObject>("Assets/StarShift/Prefabs/Enemies/Bomber.prefab"),HpOverride=19,Sequence=Asset<FlightSequence>("Data/RegionA/TopHold")}};
                e.GuaranteedMajorCount=0;e.EntryBeats=Array.Empty<EntryBeatRule>();e.OptionalEvents=Array.Empty<OptionalEvent>();lc.Encounters=new[]{e};lc.AdditionalStarts=Array.Empty<LevelStartPoint>();
                Runner.Flow.LoadLevel(lc);Runner.StartRun("LEVEL_START",true,true,null,null);rig.Protect();
                await Until(()=>EnemyHealth.Active.Any(x=>x.IsAlive),1500,"TTK spawn");var enemy=EnemyHealth.Active.Single(x=>x.IsAlive);rig.Freeze(enemy);enemy.transform.position=new Vector3(0,2,0);enemy.GetComponent<Rigidbody2D>().position=enemy.transform.position;rig.Place(new Vector2(0,-1));
                var levels=new[]{1,0,0};levels[(int)test.Item1]=test.Item2;loadout.Restore(test.Item1,levels);
                await rig.MouseEvent(new Vector2(600,300),false);float begin=Time.fixedTime,death=0;Action died=()=>death=Time.fixedTime;enemy.Died+=died;
                await rig.MouseEvent(new Vector2(600,300),true);if(test.Item2==5)await Capture("weapon-"+test.Item1+"-lv5");
                await Until(()=>!enemy.IsAlive,12000,"Real projectile TTK "+test);await rig.MouseEvent(new Vector2(600,300),false);
                timings.Add(new{branch=test.Item1.ToString(),level=test.Item2,hp=19,seconds=death-begin});enemy.Died-=died;Runner.StopRun();rig.RestoreFlights();UnityEngine.Object.Destroy(lc);UnityEngine.Object.Destroy(e);
            }
            var pulse=timings.Select(Newtonsoft.Json.Linq.JObject.FromObject).Where(x=>(string)x["branch"]=="Pulse").ToArray();
            for(int i=1;i<pulse.Length;i++)Require((float)pulse[i]["seconds"]<=(float)pulse[i-1]["seconds"]+.03f,"Pulse progression slower");
            foreach(int seed in new[]{410,417,503}){
                string previous=null;for(int attempt=0;attempt<2;attempt++){
                    Start(rig,3,0);var director=Runner.GetComponent<RewardDirector>();director.SetTestSeed(seed);await Until(()=>Runner.SpawnedCount==12,2200,"Quota population");
                    var signature=new List<string>();int ordinal=0;Action<PickupDefinition,RewardDropTable> observe=(item,crate)=>signature.Add(ordinal+":"+(item!=null?item.ShortCode:"BOX"));director.DropSpawned+=observe;
                    foreach(var enemy in EnemyHealth.Active.Where(x=>x.IsAlive).OrderBy(x=>x.transform.position.x).ThenBy(x=>x.transform.position.y).ToArray()){ordinal++;int before=director.PickupsSpawned;enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);Require(director.PickupsSpawned-before<=1,"Quota doubled normal drop");}
                    director.DropSpawned-=observe;Require(director.GuaranteedMajorsRemaining==0&&signature.Count>=2,"Two-item floor unmet");string value=string.Join("|",signature);if(previous!=null)Require(value==previous,"Retry rerolled quota");previous=value;
                }quotaResults.Add(new{seed,signature=previous});
            }
            Start(rig,1);await rig.MouseEvent(new Vector2(600,300),true);await Task.Delay(400);await rig.MouseEvent(new Vector2(600,300),false);float heat=rig.Player.GetComponent<PulseLaserWeapon>().Heat;
            Require(receiver.TryCollect(Item("PlasmaCore")),"Thermal switch core");Require(loadout.ActiveBranch==WeaponBranch.Plasma,"Thermal auto-selection");
            Require(loadout.Cycle()&&loadout.ActiveBranch==WeaponBranch.Pulse&&Mathf.Abs(rig.Player.GetComponent<PulseLaserWeapon>().Heat-heat)<.01f,"Core switch reset branch heat");
            Evidence("progression",new{status="PASS",realProjectileTimings=timings,quotaResults,thermalPreserved=true,thermalEnabled=true,distance=3});
        }}catch(Exception ex){Evidence("progression",new{status="FAILED",error=ex.ToString(),timings,quotaResults});}
    }
    public static object BeginFullRegion()
    {
        const string path=Output+"human-regression/full-region.json";
        Require(!File.Exists(path),"Full region already attempted; inspect result first");
        foreach(var name in new[]{"systems","cameras","persistence","progression"})Require((string)Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Output+name+".json"))["status"]=="PASS","Prerequisite "+name);
        _=(Task)typeof(VerifyHumanFeel).GetMethod("FullRegion",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);return new{started=true};
    }

    public static object BeginBossVisual(){_=BossVisual();return new{started=true};}
    static async Task BossVisual()
    {
        Evidence("boss-visual",new{status="RUNNING"});
        try{using(var rig=new Rig(1600,9)){
            Start(rig,4,1);await Until(()=>UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>()!=null,2000,"Boss visual spawn");
            var boss=UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>();var modules=boss.GetComponentsInChildren<BossAttackModule>();int cannonBase=modules[0].ShotsFired,missileBase=modules[1].ShotsFired;
            await Until(()=>modules[0].ShotsFired-cannonBase>=6,10000,"Cannon six shots");await Task.Delay(500);await Capture("boss-cannon-six");
            await Until(()=>modules[1].ShotsFired-missileBase>=5,6000,"Missile five shots");await Task.Delay(500);await Capture("boss-missile-five");
            Evidence("boss-visual",new{status="PASS",cannon=modules[0].ShotsFired-cannonBase,missiles=modules[1].ShotsFired-missileBase});
        }}catch(Exception ex){Evidence("boss-visual",new{status="FAILED",error=ex.ToString()});}
    }

    public static object DevControl()
    {
        var dev=Runner.GetComponent<StarShift.Dev.DevFastStart>();string original=EditorJsonUtility.ToJson(dev);
        try{
            Set(dev,"devModeEnabled",true);Set(dev,"startLevel",Level(1));Set(dev,"startPoint","LEVEL_START");Set(dev,"skipIntro",true);dev.Replay();
            Require(Runner.Stage==EncounterStage.Running&&!Runner.Flow.IsIntroducing,"Dev Skip Intro ignored");
            Set(dev,"skipIntro",false);dev.Replay();Require(Runner.Flow.IsAwaitingStart,"Dev enabled intro missing");
            Evidence("dev-control",new{status="PASS",realProductionAsset=AssetDatabase.GetAssetPath(Runner.Flow.Level),skipTrue=true,skipFalse=true});return new{pass=true};
        }finally{Runner.StopRun();EditorJsonUtility.FromJsonOverwrite(original,dev);}
    }

}
