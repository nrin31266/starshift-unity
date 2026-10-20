using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public static class VerifyPacingRefinement
{
    static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static EncounterRunner Runner=>UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession Session=>Runner.GetComponent<GameplaySession>();
    static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
    static async Task Until(Func<bool> p,int ms,string m){var end=Time.realtimeSinceStartup+ms/1000f;while(!p()&&Time.realtimeSinceStartup<end)await Task.Delay(15);Require(p(),m);}
    static async Task<string> Capture(string n){var p=System.IO.Path.GetFullPath("../.design/m5-refinement/screenshots/"+n+".png");ScreenCapture.CaptureScreenshot(p);await Task.Delay(160);return p;}
    static void Evidence(string name,object result)=>System.IO.File.WriteAllText("../.design/m5-refinement/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
    static LevelDefinition Level(int n)=>StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission0"+n+".asset");
    static void Start(Rig rig,int mission,int encounter,bool standalone=true){Runner.StopRun();Runner.Flow.LoadLevel(Level(mission));Runner.StartRun("ENCOUNTER_"+encounter,standalone,true,null,null);rig.Protect();rig.Place(new Vector2(0,-3.8f));}
    public static object BeginA(){_=A();return new{started=true};}
    static async Task A(){Evidence("batch-a",new{status="RUNNING"});try{using(var rig=new Rig(1600,6,true)){
      await Until(()=>Runner.Flow.CanContinue,4000,"Intro tap readiness");await rig.MouseEvent(new Vector2(500,400),true);await rig.MouseEvent(new Vector2(500,400),false);
      var approach=Time.fixedTime;await Until(()=>Runner.Flow.IntroPhase==MissionIntroPhase.MissionTitle,4500,"Signature approach");var signature=Time.fixedTime-approach;Require(signature>3&&signature<3.5f,"3.3s signature changed");
      await Until(()=>Runner.SpawnedCount>0,1500,"Mission overlay blocks spawn");
      var mission=UnityEngine.Object.FindObjectsByType<GameplayAnnouncement>().Single(x=>x.IsMissionTitle);Require(mission.GetComponent<CanvasGroup>().alpha>0,"Mission title gone before entry");await Capture("mission-title-entry-1600");
      var gaps=new List<float>();
      Start(rig,1,2);await Until(()=>Runner.Elapsed>1.5f,3000,"First entry");await Capture("m1-mirrored-entry-1600");foreach(var enemy in EnemyHealth.Active.ToArray())if(enemy.IsAlive)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);var cleared=Runner.Elapsed;
      await Until(()=>Runner.BeatStartTime(1)>=0,2500,"Fast clear chaining");var gap=Runner.Elapsed-cleared;gaps.Add(gap);Require(gap<=1.8f,"Fast-clear dead-air "+gap);
      Start(rig,1,2);await Until(()=>Runner.Elapsed>1.5f,3000,"First entry one survivor");foreach(var enemy in EnemyHealth.Active.Where(e=>e.IsAlive).Skip(1).ToArray())enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);await Until(()=>Runner.BeatStartTime(1)>=0,2500,"One survivor chaining");Require(Runner.ActiveEnemyCount>1,"One survivor blocked reinforcement");
      Start(rig,1,2);Time.timeScale=2;await Until(()=>Runner.BeatStartTime(1)>=0,6000,"Slow fallback");Require(Runner.BeatStartTime(1)>=6.9f&&Runner.BeatStartTime(1)<=7.1f,"Fallback timing");await Capture("m1-slow-fallback-1600");await Until(()=>!Runner.HasActiveRun,14000,"Natural exit completion");
      Evidence("batch-a",new{status="PASS",signature,fastClearGaps=gaps,slowFallback=7,oneSurvivor=true,naturalExit=true,titleOverlap=true});
    }}catch(Exception e){Evidence("batch-a",new{status="FAILED",error=e.ToString()});}}
    public static object BeginB(){_=B();return new{started=true};}
    static async Task B(){Evidence("batch-b",new{status="RUNNING"});try{using(var rig=new Rig(1600,6)){
      Start(rig,2,1);await Until(()=>Runner.Elapsed>1.5f,3000,"Raider entry");await Capture("raider-mirrored-1600");foreach(var enemy in EnemyHealth.Active.ToArray())if(enemy.IsAlive)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);var cleared=Runner.Elapsed;await Until(()=>Runner.BeatStartTime(1)>=0,2500,"Raider chain");float deadAir=Runner.Elapsed-cleared;Require(deadAir<=1.8f,"Raider dead air");
      Start(rig,2,2,false);var config=Runner.Current.HazardStream;var first=HazardStreamSchedule.Build(config,Runner.Current.Duration);var second=HazardStreamSchedule.Build(config,Runner.Current.Duration);
      Require(first.Count>=16&&first.Count<=24&&first.Select(x=>JsonUtility.ToJson(x)).SequenceEqual(second.Select(x=>JsonUtility.ToJson(x))),"Stream determinism/count");Require(first.Max(x=>x.SpawnScale)-first.Min(x=>x.SpawnScale)>.5f,"Stream scale variety");
      rig.Unprotect();await rig.DragTo(new Vector3(-7,-4,0));await rig.MouseEvent(new Vector2(400,200),false);var began=Time.realtimeSinceStartup;int peak=0;bool captured=false;
      while(Runner.CurrentIndex==1&&Time.realtimeSinceStartup-began<20){var active=EnemyHealth.Active.Where(e=>e.IsAlive&&!e.GetComponent<EntryHoldFlight>().IsOutsideView).ToArray();peak=Math.Max(peak,active.Length);if(Runner.Elapsed>5&&!captured){await Capture("asteroid-stream-1600");captured=true;}if(rig.Gate.State==PlayerGameplayState.GameOver)throw new InvalidOperationException("Storm impossible for parked safe-route test");await Task.Delay(40);}
      Require(Runner.CurrentIndex==2,"Storm timer cleanup");Require(!EnemyHealth.Active.Any(e=>e.IsAlive&&e.name.Contains("StormAsteroid")),"Hazard carryover");rig.Protect();
      Start(rig,2,3);await Task.Delay(3500);await Capture("bomber-escort-1600");Require(Runner.ActiveEnemyCount==7,"Escort composition");var bomber=EnemyHealth.Active.First(e=>e.IsAlive&&e.name.Contains("Bomber"));await rig.KillWithPulse(bomber);
      Evidence("batch-b",new{status="PASS",raiderDeadAir=deadAir,deterministic=true,normalAsteroids=first.Count,peakVisibleIncludingResources=peak,stormHazardCleanup=true,escort=7,bomberRealPulseKill=true,lives=rig.Lives.CurrentLives});
    }}catch(Exception e){Evidence("batch-b",new{status="FAILED",error=e.ToString()});}}

    public static object BeginC(){_=C();return new{started=true};}
    static async Task C(){Evidence("batch-c",new{status="RUNNING"});try{using(var rig=new Rig(1600,6)){
      Start(rig,3,1);await Until(()=>Runner.Elapsed>1.5f,3000,"Sine entry");await Capture("sine-wings-1600");foreach(var e in EnemyHealth.Active.ToArray())if(e.IsAlive)e.ReceivePlayerHit(e.CurrentHp,rig.Player);var cleared=Runner.Elapsed;await Until(()=>Runner.BeatStartTime(1)>=0,2500,"Sine chaining");var gap=Runner.Elapsed-cleared;Require(gap<=1.8f,"Sine dead air");
      Start(rig,3,2);await Until(()=>Runner.Elapsed>1.6f,3000,"Pincer entry");Require(Runner.SpawnedCount==8,"Pincer wings not synchronized");await Capture("pincer-overlap-1600");foreach(var e in EnemyHealth.Active.ToArray())if(e.IsAlive)e.ReceivePlayerHit(e.CurrentHp,rig.Player);float pincerClear=Runner.Elapsed;
      await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&!e.Definition.BodyDamagesPlayer),2500,"Pressure Supply trigger");float supplyStart=Runner.Elapsed;Require(supplyStart-pincerClear<=1.8f,"Supply dead air");
      var supply=EnemyHealth.Active.Single(e=>e.IsAlive&&!e.Definition.BodyDamagesPlayer);await Until(()=>supply.GetComponent<EntryHoldFlight>().IsHolding,3500,"Supply entry");await Capture("supply-pressure-window-1600");await rig.KillWithPulse(supply);
      var crate=UnityEngine.Object.FindObjectsByType<RewardPickup>().First(p=>p.Crate!=null);await rig.DragTo(crate.transform.position);if(crate.IsActive)rig.Place(crate.transform.position);await Until(()=>!crate.IsActive,1000,"Crate collect");await Until(()=>Runner.BeatStartTime(1)>=0,2500,"Supply reinforcement");
      Start(rig,3,2);Time.timeScale=2;await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&!e.Definition.BodyDamagesPlayer),6000,"Supply fallback");await Until(()=>Runner.BeatStartTime(1)>=0,11000,"Supply escape chaining");Require(!UnityEngine.Object.FindObjectsByType<RewardPickup>().Any(p=>p.Crate!=null),"Escape crate");
      Evidence("batch-c",new{status="PASS",sineDeadAir=gap,pincerSynchronized=true,supplyStart,supplyDestroyRealPulse=true,supplyEscape=true,reinforcementAfterOpportunity=true});
    }}catch(Exception e){Evidence("batch-c",new{status="FAILED",error=e.ToString()});}}

    public static object BeginD(){_=D();return new{started=true};}
    static async Task D(){Evidence("batch-d",new{status="RUNNING"});try{using(var rig=new Rig(1600,6)){
      Start(rig,1,1,false);await Until(()=>Runner.Elapsed>1.4f,2500,"Minor setup");foreach(var e in EnemyHealth.Active.ToArray())if(e.IsAlive)e.ReceivePlayerHit(e.CurrentHp,rig.Player);var cutoff=Time.fixedTime;await Until(()=>Runner.CurrentIndex==1&&Runner.SpawnedCount>0,3500,"Minor transition");float minor=Time.fixedTime-cutoff;Require(minor<=2.2f,"Minor stacked timers "+minor);
      var transitions=new List<object>();
      for(int mission=1;mission<=2;mission++) {
        Start(rig,mission,mission==1?2:3,false);
        await Until(()=>Runner.Elapsed>1.5f,2500,"Mission setup");
        foreach(var e in EnemyHealth.Active.ToArray())if(e.IsAlive)e.ReceivePlayerHit(e.CurrentHp,rig.Player);
        if(mission==1){await Until(()=>Runner.BeatStartTime(1)>=0,2500,"Mission second beat");await Until(()=>Runner.SpawnedCount==10,1800,"Second wing schedule");foreach(var e in EnemyHealth.Active.ToArray())if(e.IsAlive)e.ReceivePlayerHit(e.CurrentHp,rig.Player);}
        foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>())pickup.Release();
        var region=Runner.GetComponent<RegionCampaignFlow>();await Until(()=>region.IsMissionTravel,2000,"Mission travel");var began=Time.fixedTime;
        Require(rig.Gate.CanInput&&Session.CanPause,"Mission travel blocked control/Pause");
        await rig.MouseEvent(new Vector2(350,250),false);await rig.MouseEvent(new Vector2(350,250),true);
        var from=rig.Player.transform.position;await rig.MouseEvent(new Vector2(450,250),true);Require(rig.Player.transform.position.x>from.x+.3f,"Travel drag ignored");
        Session.Pause();var frozen=Time.fixedTime;await Task.Delay(250);Require(Time.fixedTime==frozen,"Mission travel Pause clock");Session.Resume();await rig.MouseEvent(new Vector2(450,250),false);await rig.MouseEvent(new Vector2(450,250),true);
        Require(rig.Input.FireHeld,"Mission "+mission+" fresh input after Pause not acquired");await Capture("mission-"+mission+"-travel-control-1600");var before=rig.Player.transform.position;
        await Until(()=>Runner.Flow.Level.Id=="L"+(mission+1),3000,"Next Mission");Require(rig.Input.FireHeld,"Mission "+mission+" handoff lost held input (preserve="+Runner.PreservePlayerInput+")");Require(Vector3.Distance(before,rig.Player.transform.position)<.7f,"Mission teleported player");
        await Until(()=>Runner.SpawnedCount>0,1200,"Next Mission entry");float duration=Time.fixedTime-began;Require(duration>=2.6f&&duration<=3.2f,"Mission timing "+duration);
        var title=UnityEngine.Object.FindObjectsByType<GameplayAnnouncement>().Single(x=>x.IsMissionTitle);
        Require(title.GetComponent<CanvasGroup>().alpha>0,"Mission title disappeared before enemy entry");
        await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&!e.GetComponent<EntryHoldFlight>().IsOutsideView),1700,"Visible enemy entry");
        Require(title.GetComponent<CanvasGroup>().alpha>0,"Mission title gone before visible entry");
        await Capture("mission-"+mission+"-title-overlap-visible-entry-1600");
        transitions.Add(new{from=mission,to=mission+1,seconds=duration,titleOverlap=true});
      }
      Evidence("batch-d",new{status="PASS",minorTransitionToSpawn=minor,transitions,dragActive=true,pause=true,heldPreserved=true,noTeleport=true});
    }}catch(Exception e){Evidence("batch-d",new{status="FAILED",error=e.ToString()});}}

    public static object BeginStorms(){_=Storms();return new{started=true};}
    static async Task Storms(){Evidence("storm-correction",new{status="RUNNING"});try{using(var rig=new Rig(1600,6)){
      var results=new List<object>();
      foreach(int mission in new[]{2,4}){
        Start(rig,mission,mission==2?2:1);var encounter=Runner.Current;var config=encounter.HazardStream;
        var actions=HazardStreamSchedule.Build(config,encounter.Duration);var repeat=HazardStreamSchedule.Build(config,encounter.Duration);
        Require(actions.Select(JsonUtility.ToJson).SequenceEqual(repeat.Select(JsonUtility.ToJson)),"Determinism");
        Require(actions.Count==config.Count,"Configured asteroid count truncated");
        foreach(var action in actions)Require(Vector2.Angle(action.ExitAnchor-action.EntryAnchor,config.DominantDirection)<=3.01f&&action.ContinuousTransit,"Mixed normal asteroid directions");
        foreach(var resource in encounter.Groups){var phase=resource.Sequence.Phases[0];Require(Vector2.Angle(phase.To-phase.From,config.DominantDirection)<.01f,"Resource outside flow");}
        rig.Unprotect();await rig.DragTo(new Vector3(mission==2?-7:7,-4,0));await rig.MouseEvent(new Vector2(400,200),false);
        var began=Time.realtimeSinceStartup;int peak=0;var tracked=new Dictionary<EnemyHealth,(Vector3 point,float age)>();float maxAngle=0;
        while(Runner.HasActiveRun&&Time.realtimeSinceStartup-began<22){
          foreach(var e in EnemyHealth.Active.Where(e=>e.IsAlive&&e.name.Contains("Asteroid"))){
            var viewport=Camera.main.WorldToViewportPoint(e.transform.position);
            var age=(float)typeof(EntryHoldFlight).GetField("elapsed",Fields).GetValue(e.GetComponent<EntryHoldFlight>());
            if(tracked.TryGetValue(e,out var previous)&&age>previous.age){var delta=(Vector2)(viewport-previous.point);if(delta.magnitude>.0005f){var angle=Vector2.Angle(delta,config.DominantDirection);maxAngle=Math.Max(maxAngle,angle);Require(angle<=3.5f,"Runtime asteroid changed flow: "+angle);}}
            tracked[e]=(viewport,age);
          }
          peak=Math.Max(peak,EnemyHealth.Active.Count(e=>e.IsAlive&&!e.GetComponent<EntryHoldFlight>().IsOutsideView));
          if(Runner.Elapsed>4.9f&&Runner.Elapsed<5.15f)await Capture("storm-"+mission+"-single-flow-1600");
          Require(rig.Gate.State!=PlayerGameplayState.GameOver,"No-shoot safe lane failed");await Task.Delay(60);
        }
        Require(!Runner.HasActiveRun,"Storm timeout");results.Add(new{mission,count=actions.Count,peak,maxAngle,lives=rig.Lives.CurrentLives,noShooting=true});rig.Protect();
      }
      Evidence("storm-correction",new{status="PASS",results});
    }}catch(Exception e){Evidence("storm-correction",new{status="FAILED",error=e.ToString()});}}

    public static object BeginE(){_=E(1600,true);return new{started=true};}
    public static object BeginWideJourney(){_=E(2000,false);return new{started=true};}
    static async Task E(int width,bool bossChecks)
    {
      string evidence=bossChecks?"batch-e":"journey-wide";Evidence(evidence,new{status="RUNNING"});
      try{using(var rig=new Rig(width,6)){
        Start(rig,4,2,false);await Until(()=>UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>()!=null,2000,"Boss spawn");
        var boss=UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>();var health=boss.Health;
        float reserveImpact=0;
        if(bossChecks){
          await Until(()=>boss.GetComponentsInChildren<BossAttackModule>().Sum(m=>m.ShotsFired)>0,14000,"Boss combat");
          var hp=health.CurrentHp;var reserve=rig.Player.GetComponent<EmergencyMissileReserve>();Require(reserve.TryUse(),"Reserve use");
          var view=Camera.main.GetComponent<GameplayViewController>();
          await Until(()=>!reserve.IsInFlight,2500,"Reserve detonation");reserveImpact=view.ImpactRemaining;
          Require(hp-health.CurrentHp==Mathf.RoundToInt(health.MaxHp*.1f),"Boss reserve percentage");
          await Capture("reserve-impact-"+width);await Task.Delay(400);
          Require(view.ImpactRemaining<=0,"Reserve impact did not recover");
        }
        var completion=Runner.CompletionCount;var died=Time.fixedTime;health.ReceivePlayerHit(health.CurrentHp,rig.Player);
        Require(Runner.Stage==EncounterStage.BossPostDeath&&health.gameObject.activeInHierarchy,"Staged boss death");
        Require(UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length==0,"Damaging boss shots remained");
        await Capture("boss-post-defeat-"+width);
        await Until(()=>Runner.CompletionCount==completion+1,4000,"Boss death completion");var deathSeconds=Time.fixedTime-died;
        Require(deathSeconds>=2.35f&&deathSeconds<=2.6f,"Boss death signature changed: "+deathSeconds);
        Require(UnityEngine.Object.FindObjectsByType<RewardPickup>().Count(p=>p.IsActive&&p.Item!=null&&p.Crate==null)==2,"Boss direct pair missing");
        await Capture("boss-reward-before-travel-"+width);
        var journey=Runner.GetComponent<PlanetJourney>();var region=Runner.GetComponent<RegionCampaignFlow>();
        await Until(()=>journey.IsActive,7000,"Post-reward travel start");var began=Time.fixedTime;
        var samples=new List<object>();var captured=new HashSet<PlanetJourneyPhase>();
        while(!journey.IsAwaitingTap){
          Require(!rig.Gate.CanInput&&!Session.CanPause,"Major cinematic permits gameplay");
          float phaseElapsed=(float)typeof(PlanetJourney).GetField("elapsed",Fields).GetValue(journey);
          if(phaseElapsed>.35f&&captured.Add(journey.Phase)){
            samples.Add(new{phase=journey.Phase.ToString(),seconds=Time.fixedTime-began,heading=rig.Player.transform.eulerAngles.z,background=Camera.main.backgroundColor.ToString()});
            await Capture("planet-"+journey.Phase.ToString().ToLowerInvariant()+"-"+width);
          }
          Require(Time.fixedTime-began<9,"Journey stalled");await Task.Delay(30);
        }
        float travelSeconds=Time.fixedTime-began;Require(travelSeconds>=7.1f&&travelSeconds<7.5f,"Major travel duration "+travelSeconds);
        await Capture("planet-reveal-tap-"+width);var settled=rig.Player.transform.position;
        await Task.Delay(800);await Capture("planet-stable-tap-"+width);Require(journey.IsAwaitingTap&&Vector3.Distance(settled,rig.Player.transform.position)<.01f,"Tap state unstable");
        Require(UnityEngine.Object.FindObjectsByType<Text>().Any(t=>t.isActiveAndEnabled&&t.text=="Chạm để tiếp tục"),"Tap hint missing");
        Session.Pause();Require(!Session.IsPaused,"Pause accepted during major cinematic");
        await rig.MouseEvent(new Vector2(500,400),false);await rig.MouseEvent(new Vector2(500,400),true);
        var approachStart=Time.fixedTime;await rig.MouseEvent(new Vector2(500,400),false);
        await Until(()=>journey.Phase==PlanetJourneyPhase.Approach,600,"Tap did not start approach");
        await rig.MouseEvent(new Vector2(600,400),true);
        await Until(()=>region.DestinationReady,4500,"Right destination");var signature=Time.fixedTime-approachStart;
        Require(signature>=3.25f&&signature<=3.55f,"Approach signature duration "+signature);
        Require(Runner.Flow.Direction==PlayDirection.Right&&rig.Gate.CanInput&&!Runner.HasActiveRun,"Right-ready skeleton");
        Require(!rig.Input.FireHeld,"Cinematic input leaked into gameplay");
        var camera=Camera.main;Require(Mathf.Abs(camera.projectionMatrix.m11-1/camera.orthographicSize)<.001f,"Projection matrix leaked");
        Require(EnemyHealth.Active.Count==0&&Runner.Flow.Level.Id=="L4","Fake next Mission content");
        await Capture("right-ready-"+width);
        var before=rig.Player.transform.position;await rig.MouseEvent(new Vector2(600,400),false);await rig.MouseEvent(new Vector2(600,400),true);await rig.MouseEvent(new Vector2(700,400),true);
        Require(rig.Player.transform.position.x>before.x+.3f&&rig.Input.FireHeld,"Right-ready fresh input unusable");
        Require(Runner.CompletionCount==completion+1,"Repeated boss completion");
        Evidence(evidence,new{status="PASS",width,deathSeconds,travelSeconds,signature,reserveImpact,samples,stableTap=true,tapConsumed=true,freshInput=true,rightReady=true,projectionRestored=true,reward=true});
      }}catch(Exception e){Evidence(evidence,new{status="FAILED",error=e.ToString()});}
    }

    public static object BeginIntegrationChecks(){_ = IntegrationChecks();return new{started=true};}
    static async Task IntegrationChecks()
    {
      Evidence("integration",new{status="RUNNING",test="integration"});
      try{using(var rig=new Rig(1600,6))using(var storage=new TestStorageScope()){
        var owner=Runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();
        var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"starshift-m5-integration-"+Guid.NewGuid().ToString("N"),"save.json");
        var data=new StarShift.Persistence.SaveEnvelope{SchemaVersion=2,HasCampaign=true,Campaign=new StarShift.Persistence.CampaignSave{MissionId="L2",Checkpoint=new StarShift.Persistence.CheckpointData{EncounterId="ENCOUNTER_3",Lives=3,Missiles=2,WeaponLevels=new[]{2,1,0},Stats=new StarShift.Persistence.RunStatsData{Score=123,Shards=2}}},Profile=new StarShift.Persistence.PlayerProfile{StellarShardBalance=17,UnlockedMissions=new[]{"L1","L2","L3","L4"},Records=Array.Empty<StarShift.Persistence.MissionRecord>()},Settings=StarShift.Persistence.SettingsData.Defaults()};
        Func<StarShift.Persistence.SaveEnvelope,bool> validate=e=>StarShift.Persistence.SaveValidation.IsValid(e,(m,c)=>Runner.GetComponent<RegionCampaignFlow>().FindMission(m)?.ResolveStart(c)>=0,5);
        var store=new StarShift.Persistence.JsonSaveStore(path);Require(store.Write(data,validate),"Fixture write");
        owner.UseTestStorage(path);Require(owner.Continue(),"Later M2 Continue");rig.Protect();
        Require(Runner.Flow.Level.Id=="L2"&&Runner.CurrentIndex==2&&rig.Player.GetComponent<EmergencyMissileReserve>().Current==2,"Continue boundary");
        Session.Pause();Require(owner.RetryEncounter(),"M2 retry");rig.Protect();Require(Runner.CurrentIndex==2&&Runner.GetComponent<RunRewards>().Score==123,"Retry checkpoint");
        owner.ExitIsolated();owner.UseTestStorage(path);Require(owner.Continue(),"Fresh storage Continue");rig.Protect();Require(Runner.Flow.Level.Id=="L2"&&Runner.CurrentIndex==2,"Fresh storage mission catalog");
        owner.ExitIsolated();data.Campaign.MissionId="L4";data.Campaign.Checkpoint.EncounterId="ENCOUNTER_2";Require(store.Write(data,validate),"Boss fixture write");owner.UseTestStorage(path);Require(owner.Continue(),"Boss Continue");rig.Protect();
        await Until(()=>UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>()!=null,1500,"Boss retry spawn");var boss=UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>();boss.Health.ReceivePlayerHit(Mathf.CeilToInt(boss.Health.MaxHp*.51f),rig.Player);await Task.Delay(100);Require(boss.IsPhaseB,"Boss phase fixture");
        Session.Pause();Require(owner.RetryEncounter(),"Boss Retry");rig.Protect();await Until(()=>UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>()!=null,1500,"Boss Retry respawn");boss=UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>();Require(boss.Health.CurrentHp==boss.Health.MaxHp&&!boss.IsPhaseB,"Boss retry retained runtime HP/phase");
        owner.ExitIsolated();var original=System.IO.File.ReadAllText(path);Require(owner.BeginReplay(),"L4 Replay");rig.Protect();Require(rig.Player.GetComponent<WeaponLoadout>().Level(WeaponBranch.Homing)==1,"Replay preset");
        var enemy=EnemyHealth.Active.FirstOrDefault(e=>e.IsAlive);if(enemy!=null)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);owner.ExitIsolated();Require(original==System.IO.File.ReadAllText(path),"Replay modified campaign bytes");
        data.Campaign.Status=StarShift.Persistence.CampaignStatus.RegionTransition;Require(store.Write(data,validate),"Transition fixture");owner.UseTestStorage(path);Require(owner.Continue(),"Transition Continue");
        await Until(()=>Runner.GetComponent<PlanetJourney>().IsAwaitingTap,9500,"Transition resume reveal");
        await rig.MouseEvent(new Vector2(500,400),false);await rig.MouseEvent(new Vector2(500,400),true);await rig.MouseEvent(new Vector2(500,400),false);
        await Until(()=>Runner.GetComponent<RegionCampaignFlow>().DestinationReady,4500,"Transition resume");Require(EnemyHealth.Active.Count==0&&owner.PersistentSnapshot.Profile.Records.Length==0,"Transition repeated fight/records");
        owner.ExitIsolated();owner.UseTestStorage(path);Require(owner.Continue()&&Runner.GetComponent<RegionCampaignFlow>().DestinationReady,"Ready Continue");
        Evidence("integration",new{status="PASS",test="integration",laterM2Continue=true,freshStore=true,retry=true,bossRetry=true,replayBytesIsolated=true,preset=true,transitionResume=true,readyResume=true});
      }}catch(Exception e){Evidence("integration",new{status="FAILED",test="integration",error=e.ToString()});}
    }

    public static object BeginVisuals(){_=Visuals();return new{started=true};}
    static async Task Visuals(){Evidence("visuals",new{status="RUNNING"});try{
      using(var rig=new Rig(2000,6)){
        foreach(var step in new (int mission,int encounter,int delay,string name)[]{(1,1,1400,"m1-carrier"),(2,1,1500,"raider"),(2,3,3500,"bomber"),(3,1,1500,"sine"),(3,2,2000,"pincer"),(3,3,3200,"elite")}){
          Start(rig,step.mission,step.encounter);await Task.Delay(step.delay);await Capture("review-"+step.name+"-2000");
        }
      }
      Evidence("visuals",new{status="PASS",width=2000});
    }catch(Exception e){Evidence("visuals",new{status="FAILED",error=e.ToString()});}}

    public static object BeginBoundedCleanup(){_=BoundedCleanup();return new{started=true};}
    static async Task BoundedCleanup(){Evidence("bounded-cleanup",new{status="RUNNING"});try{using(var rig=new Rig(1600,6)){
      Start(rig,2,1);await Until(()=>Runner.Elapsed>4,5500,"Normal combat missile setup");
      var reserve=rig.Player.GetComponent<EmergencyMissileReserve>();var view=Camera.main.GetComponent<GameplayViewController>();
      Require(reserve.TryUse(),"Normal combat reserve activation");await Until(()=>!reserve.IsInFlight,2500,"Normal reserve detonation");
      float normalImpact=view.ImpactRemaining;Require(normalImpact>0,"Normal reserve impact absent");await Capture("normal-reserve-impact-1600");
      view.RequestView(new GameplayViewSettings(7.5f,new Rect(0,0,1,1)),.1f);await Task.Delay(350);
      Require(view.ImpactRemaining<=0&&Mathf.Abs(Camera.main.projectionMatrix.m11-1/Camera.main.orthographicSize)<.001f,"Normal reserve projection did not recover");
      Start(rig,1,2);await Until(()=>Runner.BeatStartTime(1)>=0,8500,"Timeline fallback");await Until(()=>Runner.SpawnedCount==10,1500,"Full schedule");
      var definition=UnityEngine.Object.Instantiate(StarShift.Editor.RegionAAssetPaths.Load<SingleFireDefinition>("Assets/StarShift/Data/Enemies/ScoutSingleFire.asset"));
      typeof(SingleFireDefinition).GetField("projectileSpeed",Fields).SetValue(definition,.1f);
      var muzzle=new GameObject("BoundedCleanupProbe");muzzle.transform.position=new Vector3(0,3,0);
      var shot=UnityEngine.Object.Instantiate(definition.ProjectilePrefab);shot.Launch(definition,muzzle.transform,null,rig.Player,p=>p.ResetForPool());
      var naturalSeconds=shot.VisibleFlightTimeLimit;Require(naturalSeconds>100,"Probe projectile not lingering");
      foreach(var e in EnemyHealth.Active.ToArray())if(e.IsAlive)e.ReceivePlayerHit(e.CurrentHp,rig.Player);
      await Until(()=>Runner.Stage==EncounterStage.ProjectileDrain,1000,"Final Timeline did not use drain");var began=Time.fixedTime;
      Session.Pause();var position=shot.transform.position;await Task.Delay(200);Require(shot.transform.position==position&&Runner.Stage==EncounterStage.ProjectileDrain,"Cleanup advanced during Pause");Session.Resume();
      await Until(()=>!shot.GetComponent<BoxCollider2D>().enabled,1000,"Fade damage not disabled");
      await Until(()=>Runner.CompletionCount==1,1000,"Lingering projectile blocked completion");float boundedSeconds=Time.fixedTime-began;
      Require(boundedSeconds<.9f&&!shot.gameObject.activeSelf,"Projectile drain not bounded");
      UnityEngine.Object.Destroy(shot.gameObject);UnityEngine.Object.Destroy(muzzle);UnityEngine.Object.Destroy(definition);
      Evidence("bounded-cleanup",new{status="PASS",naturalSeconds,boundedSeconds,normalImpact,pause=true,damageOffBeforeRelease=true});
    }}catch(Exception e){Evidence("bounded-cleanup",new{status="FAILED",error=e.ToString()});}}

    static Task fullRegionTask;
    public static object FullRegionStatus() => AppDomain.CurrentDomain.GetAssemblies()
      .Select(a=>a.GetType("VerifyPacingRefinement"))
      .Where(t=>t!=null).Select(t=>t.GetField("fullRegionTask",BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null) as Task)
      .Where(t=>t!=null).Select(t=>new{task=t.Status.ToString(),playing=Application.isPlaying,mission=Runner.Flow.Level.Id,encounter=Runner.CurrentIndex,elapsed=Runner.Elapsed}).ToArray();
    public static object BeginFullRegion(){
      foreach(var batch in new[]{"batch-a","batch-b","batch-c","batch-d","storm-correction","batch-e","journey-wide","integration","bounded-cleanup","visuals","title-pause"})
        Require(Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("../.design/m5-refinement/"+batch+".json"))["status"].ToString()=="PASS","Prerequisite failed: "+batch);
      Require(!System.IO.File.Exists("../.design/m5-refinement/full-region.json"),"Full Region run already attempted; inspect existing evidence");
      fullRegionTask = FullRegion();return new{started=true};
    }
    static async Task FullRegion()
    {
      Evidence("full-region",new{status="RUNNING",test="full-region"});
      try{using(var rig=new Rig(1600,6))using(var storage=new TestStorageScope()){
        var owner=Runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();
        var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"starshift-m5-"+Guid.NewGuid().ToString("N"),"save.json");
        owner.UseTestStorage(path);Require(owner.NewGame(),"Campaign NewGame");rig.Protect();
        var start=Time.fixedTime;var wall=Time.realtimeSinceStartup;string previous="L1";float boundary=start;
        var durations=new List<object>();bool paused=false,died=false,switched=false,reserveUsed=false;int peak=0;
        var captured=new HashSet<string>();var gaps=new List<float>();float emptySince=-1;var encounters=new List<object>();string previousEncounter="";float encounterStart=start;
        while(!Runner.GetComponent<RegionCampaignFlow>().DestinationReady && Time.realtimeSinceStartup-wall<600){
          if(Runner.Flow.CanContinue)Session.BeginMission();
          var id=Runner.Flow.Level.Id;
          if(id!=previous){durations.Add(new{mission=previous,seconds=Time.fixedTime-boundary});previous=id;boundary=Time.fixedTime;}
          if(rig.Gate.CanFire && !rig.Input.FireHeld){await rig.MouseEvent(new Vector2(400,200),false);await rig.MouseEvent(new Vector2(400,200),true);}
          var enemies=EnemyHealth.Active.Where(e=>e.IsAlive&&!e.GetComponent<EntryHoldFlight>().IsOutsideView).ToArray();peak=Math.Max(peak,enemies.Length);
          if(rig.Gate.CanInput && enemies.Length>0){var target=enemies.OrderBy(e=>Mathf.Abs(e.transform.position.x-rig.Player.transform.position.x)).First();rig.Place(new Vector2(target.transform.position.x,-3.8f));}
          if(rig.Gate.CanInput)foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive))if(pickup.transform.position.y<-.5f)rig.Place(pickup.transform.position);
          var key=id+"-"+Runner.CurrentIndex;
          if(key!=previousEncounter){if(previousEncounter!="")encounters.Add(new{encounter=previousEncounter,seconds=Time.fixedTime-encounterStart});previousEncounter=key;encounterStart=Time.fixedTime;emptySince=-1;}
          if(Runner.Stage==EncounterStage.Running&&Runner.SpawnedCount>0){
            if(Runner.ActiveEnemyCount==0){if(emptySince<0)emptySince=Time.fixedTime;}
            else if(emptySince>=0){gaps.Add(Time.fixedTime-emptySince);emptySince=-1;}
          }else emptySince=-1;
          Evidence("full-region-live",new{status="RUNNING",seconds=Time.fixedTime-start,mission=id,encounter=Runner.CurrentIndex,elapsed=Runner.Elapsed,stage=Runner.Stage.ToString()});
          if(Runner.Stage==EncounterStage.Running && Runner.Elapsed>4 && captured.Add(key))await Capture("full-"+key);
          if(!paused && id=="L2" && Runner.CurrentIndex==1){Session.Pause();var t=Runner.Elapsed;await Task.Delay(250);Require(t==Runner.Elapsed,"Full run pause");Session.Resume();paused=true;}
          if(!died && id=="L2" && Runner.CurrentIndex==2 && Runner.Elapsed>3){rig.Unprotect();Require(rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null),"Full death");await Until(()=>rig.Gate.State==PlayerGameplayState.PostEntryInvincibility,3000,"Full respawn");rig.Protect();died=true;}
          if(!switched && id=="L3" && rig.Gate.CanInput){
             Runner.GetComponent<RewardDirector>().Spawn(StarShift.Editor.RegionAAssetPaths.Load<PickupDefinition>("Assets/StarShift/Data/Rewards/PlasmaCore.asset"),null,rig.Player.transform.position);
             await Task.Delay(100);var loadout=rig.Player.GetComponent<WeaponLoadout>();
             if(loadout.Level(WeaponBranch.Plasma)>0){loadout.Cycle();switched=true;}
          }
          if(!reserveUsed && id=="L4" && Runner.CurrentIndex==1 && Runner.Elapsed>5)reserveUsed=rig.Player.GetComponent<EmergencyMissileReserve>().TryUse();
          await Task.Delay(100);
        }
        Require(Runner.GetComponent<RegionCampaignFlow>().DestinationReady,"Full Region timeout");
        durations.Add(new{mission=previous,seconds=Time.fixedTime-boundary});
        var snapshot=owner.PersistentSnapshot;Require(snapshot.Campaign.Status==StarShift.Persistence.CampaignStatus.RegionReady,"Region save marker");
        Require(snapshot.Profile.Records.Length==4 && snapshot.Profile.UnlockedMissions.Length==4,"Mission records/unlocks");
        Require(paused&&died&&switched&&reserveUsed,"Full run coverage");
        encounters.Add(new{encounter=previousEncounter,seconds=Time.fixedTime-encounterStart});
        Evidence("full-region",new{status="PASS",test="full-region",encounters,emptyFieldSamples=gaps,averageEmptyField=gaps.Count==0?0:gaps.Average(),maxEmptyField=gaps.Count==0?0:gaps.Max(),seconds=Time.fixedTime-start,durations,peak,paused,died,switched,reserveUsed,protectedPlayer=true,injectedPlasmaCore=true,savePath=path,records=snapshot.Profile.Records.Length});
      }}catch(Exception e){Evidence("full-region",new{status="FAILED",test="full-region",error=e.ToString()});}
    }

    public static object BeginTitlePause(){_=TitlePause();return new{started=true};}
    static async Task TitlePause(){Evidence("title-pause",new{status="RUNNING"});try{using(var rig=new Rig(1600,6)){
      Start(rig,2,1);await Until(()=>Runner.Elapsed>.3f,2000,"Title setup");
      var title=UnityEngine.Object.FindObjectsByType<GameplayAnnouncement>().Single(x=>x.IsMissionTitle);
      var group=title.GetComponent<CanvasGroup>();Require(group.alpha>0,"Initial title missing");
      await rig.MouseEvent(new Vector2(500,400),true);Require(rig.Player.GetComponent<PulseLaserWeapon>().ShotsFired>0,"Fresh-play weapon pool/fire unavailable");
      Session.Pause();var elapsed=(float)typeof(GameplayAnnouncement).GetField("elapsed",Fields).GetValue(title);
      await Task.Delay(250);Require(group.alpha==0&&(float)typeof(GameplayAnnouncement).GetField("elapsed",Fields).GetValue(title)==elapsed,"Pause destroyed/advanced cue");
      Session.Resume();await Task.Delay(100);Require(group.alpha>0,"Resume lost gameplay title");
      Start(rig,2,3,false);await Until(()=>Runner.SpawnedCount==Runner.Current.EnemyCount,2000,"Mission ending setup");
      foreach(var e in EnemyHealth.Active.ToArray())if(e.IsAlive)e.ReceivePlayerHit(e.CurrentHp,rig.Player);
      foreach(var reward in UnityEngine.Object.FindObjectsByType<RewardPickup>())reward.Release();
      await Until(()=>Runner.GetComponent<RegionCampaignFlow>().IsMissionTravel,1500,"Mission travel setup");
      await Until(()=>title.GetComponentsInChildren<Text>().Any(t=>t.text=="NHIỆM VỤ 03")&&group.alpha>.2f,3000,"Incoming Mission title");
      Session.Pause();var fixedTime=Time.fixedTime;await Task.Delay(250);Require(group.alpha==0&&Time.fixedTime==fixedTime,"Late Mission pause");
      Session.Resume();await Task.Delay(80);Require(group.alpha>0,"Resume lost incoming Mission title");
      await Until(()=>Runner.Flow.Level.Id=="L3"&&Runner.SpawnedCount>0,1800,"Incoming Mission entry");Require(group.alpha>0,"Pause broke title/entry overlap");
      await Capture("title-resume-overlap-1600");
      Evidence("title-pause",new{status="PASS",gameplayTitle=true,lateMissionTravelTitle=true,clockFrozen=true,entryOverlap=true});
    }}catch(Exception e){Evidence("title-pause",new{status="FAILED",error=e.ToString()});}}

    sealed class TestStorageScope : IDisposable
    {
        readonly StarShift.Persistence.CampaignLifecycle owner=Runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();
        readonly string originalPath;
        readonly string originalBytes;
        public TestStorageScope(){originalPath=owner.SavePath;originalBytes=System.IO.File.Exists(originalPath)?Convert.ToBase64String(System.IO.File.ReadAllBytes(originalPath)):null;}
        public void Dispose(){owner.ExitIsolated();owner.UseTestStorage(originalPath);var current=System.IO.File.Exists(originalPath)?Convert.ToBase64String(System.IO.File.ReadAllBytes(originalPath)):null;Require(originalBytes==current,"Original save bytes changed");}
    }

    public sealed class Rig : IDisposable
    {
        private readonly List<EntryHoldFlight> frozenFlights = new List<EntryHoldFlight>();
        public void Freeze(EnemyHealth enemy)
        {
            var flight=enemy.GetComponent<EntryHoldFlight>();
            if(!flight.enabled)return;
            frozenFlights.Add(flight);flight.enabled=false;
        }
        public void RestoreFlights()
        {
            foreach(var flight in frozenFlights)if(flight!=null)flight.enabled=true;
            frozenFlights.Clear();
        }
        public readonly GameObject Player = GameObject.Find("Player");
        public readonly PlayerGameplayGate Gate;
        public readonly PlayerLives Lives;
        public readonly InputSystemPointerInput Input;
        public readonly int FirstFrame = Time.frameCount;
        readonly Mouse mouse;
        readonly Touchscreen touch;
        readonly Mouse oldMouse;
        readonly List<InputDevice> disabled = new List<InputDevice>();
        readonly InputSettings.EditorInputBehaviorInPlayMode oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        readonly InputSettings.BackgroundBehavior oldBackground = InputSystem.settings.backgroundBehavior;
        readonly object gameView;
        readonly object gameViewZoom;
        readonly Vector2 oldGameViewZoom, oldGameViewPan;
        readonly PropertyInfo selectedSize;
        readonly object oldSize;
        readonly GameplayViewController view;
        readonly GameplayViewSettings oldView;
        readonly float oldScale = Time.timeScale;
        readonly bool oldAudio = AudioListener.pause;
        readonly string[] definitions;
        Vector2 pointer = new Vector2(400, 220);
        readonly CameraProfile profile;
        public Rig(int width, float size, bool intro = false)
        {
            Require(Application.isPlaying, "Play Mode required.");
            Session.enabled = false; Runner.StopRun(); Time.timeScale = 1; AudioListener.pause = false; Session.enabled = true;
            Gate = Player.GetComponent<PlayerGameplayGate>(); Lives = Player.GetComponent<PlayerLives>(); Input = Player.GetComponent<InputSystemPointerInput>();
            var life = Player.GetComponent<PlayerDamageLifecycle>(); life.enabled = false; life.enabled = true;
            Place(new Vector2(0, -3.8f)); Protect();
            view = Camera.main.GetComponent<GameplayViewController>(); oldView = view.TargetView;
            view.RequestView(new GameplayViewSettings(size, new Rect(0, 0, 1, 1)), 0);
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"); gameView = Resources.FindObjectsOfTypeAll(type).FirstOrDefault() ?? EditorWindow.GetWindow(type);
            selectedSize = type.GetProperty("selectedSizeIndex"); oldSize = selectedSize.GetValue(gameView);
            gameViewZoom = gameView.GetType().GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameView);
            oldGameViewZoom = (Vector2)gameViewZoom.GetType().GetProperty("scale").GetValue(gameViewZoom);
            oldGameViewPan = (Vector2)gameViewZoom.GetType().GetProperty("translation").GetValue(gameViewZoom);
            type.GetMethod("SetCustomResolution", Fields).Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Closure QA " + width + "x900" });
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            oldMouse = Mouse.current;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreInput;
            foreach (var d in InputSystem.devices)
                if (d.native && d.enabled && (d is Mouse || d is Touchscreen)) { disabled.Add(d); InputSystem.DisableDevice(d); }
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent(); touch = InputSystem.AddDevice<Touchscreen>();
            definitions = Configs().Select(EditorJsonUtility.ToJson).ToArray();
            profile = ScriptableObject.CreateInstance<CameraProfile>();
            profile.View = new GameplayViewSettings(size,new Rect(0,0,1,1));
            Runner.StartRun("LEVEL_START", false, !intro, null, profile);
        }
        public void Protect() { Player.GetComponent<PlayerDamageLifecycle>().enabled = false; Gate.SetState(PlayerGameplayState.PostEntryInvincibility); UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0); }
        public void Unprotect() { Player.GetComponent<PlayerDamageLifecycle>().enabled = true; Gate.SetState(PlayerGameplayState.Active); }
        public void Place(Vector2 position) { Player.GetComponent<Rigidbody2D>().position = position; Player.transform.position = new Vector3(position.x, position.y, 0); }
        public async Task MouseEvent(Vector2 position, bool held)
        {
            pointer = position;
            var state = new MouseState { position = position };
            if (held) state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, state); await Task.Delay(75);
        }
        public async Task TouchEvent(int id, TouchPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = phase, position = position }); await Task.Delay(80);
        }
        public async Task KillWithPulse(EnemyHealth target)
        {
            await MouseEvent(pointer, false); await MouseEvent(pointer, true);
            var current = Camera.main.WorldToScreenPoint(Player.transform.position);
            var wanted = Camera.main.WorldToScreenPoint(target.transform.position);
            pointer += new Vector2(wanted.x - current.x, 0);
            await MouseEvent(pointer, true);
            try { await Until(() => !target.IsAlive, 2500, "Real held Pulse did not kill Scout."); }
            catch
            {
                await Capture("pulse-failure");
                var hitList=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {position=pointer},hitList);
                throw new InvalidOperationException("Pulse failure: held="+Input.FireHeld+" canFire="+Gate.CanFire+" state="+Gate.State+
                    " player="+Player.transform.position+" target="+target.transform.position+" hp="+target.CurrentHp+
                    " pointer="+pointer+" hits="+string.Join(",",hitList.Select(h=>h.gameObject.name))+
                    " shots="+UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length+" mouse="+mouse.deviceId+" pressed="+mouse.leftButton.isPressed+
                    " current="+Mouse.current?.deviceId+" focused="+Application.isFocused);
            }
            await MouseEvent(pointer, false);
        }
        public async Task DragTo(Vector3 world)
        {
            var screen=Camera.main.WorldToScreenPoint(Player.transform.position);
            var target=Camera.main.WorldToScreenPoint(world);
            var delta=new Vector2(target.x-screen.x,target.y-screen.y);
            var anchor=new Vector2(Screen.width*.5f,350);
            await MouseEvent(anchor,false);await MouseEvent(anchor,true);
            await MouseEvent(anchor+Vector2.ClampMagnitude(delta,650),true);
        }
        static EncounterDefinition[] Configs() => new[] { "A", "B" }.Select(id => StarShift.Editor.RegionAAssetPaths.Load<EncounterDefinition>("Assets/StarShift/Data/Fixtures/Encounters/L1Encounter" + id + ".asset")).ToArray();
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) RestoreInput();
        }
        private void RestoreInput()
        {
            // Dừng Play/reload có thể hủy async probe trước khi using chạy Dispose.
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= RestoreInput;
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            foreach (var d in disabled) if (d.added) InputSystem.EnableDevice(d);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            InputSystem.settings.backgroundBehavior = oldBackground;
        }
        public void Dispose()
        {
            RestoreInput();
            RestoreFlights();
            Session.enabled = false; Runner.StopRun(); Time.timeScale = 1;
            var life = Player.GetComponent<PlayerDamageLifecycle>(); life.enabled = false; life.enabled = true; Input.CancelGesture();
            view.SetBaseline(Runner.Flow.Level.Camera.View); view.RequestView(oldView, 0); gameView.GetType().GetMethod("SizeSelectionCallback").Invoke(gameView, new object[] { oldSize, null });
            gameViewZoom.GetType().GetMethod("SetTransform").Invoke(gameViewZoom, new object[] { oldGameViewPan, oldGameViewZoom });
            UnityEngine.Object.Destroy(profile);
            Session.enabled = true; Time.timeScale = oldScale; AudioListener.pause = oldAudio;
            Require(definitions.SequenceEqual(Configs().Select(EditorJsonUtility.ToJson)), "QA mutated encounter config assets.");
        }
    }
}
