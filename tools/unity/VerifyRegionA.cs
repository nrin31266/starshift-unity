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

public static class VerifyRegionA
{
    static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static EncounterRunner Runner=>UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession Session=>Runner.GetComponent<GameplaySession>();
    static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
    static async Task Until(Func<bool> p,int ms,string m){var end=Time.realtimeSinceStartup+ms/1000f;while(!p()&&Time.realtimeSinceStartup<end)await Task.Delay(25);Require(p(),m);}
    static async Task<string> Capture(string n){var p=System.IO.Path.GetFullPath("../.design/m5/"+n+".png");ScreenCapture.CaptureScreenshot(p);await Task.Delay(150);return p;}
    static void Evidence(object result)=>System.IO.File.WriteAllText("../.design/m5/batch-progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
    public static object BeginBatch(int mission){_ = RunMission(mission);return new{started=true,mission};}
    static async Task RunMission(int mission)
    {
      Evidence(new{status="RUNNING",mission});
      try{
       using(var rig=new Rig(1600,6)){
        Runner.StopRun();Runner.Flow.LoadLevel(StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission0"+mission+".asset"));
        Runner.StartRun("LEVEL_START",false,true,null,null);rig.Protect();
        var start=Time.fixedTime;var wall=Time.realtimeSinceStartup;int peak=0;var captured=new HashSet<int>();
        var durations=new List<object>();int previous=0;float boundary=start;
        await rig.MouseEvent(new Vector2(400,200),true);
        while(!Session.IsClear && Time.realtimeSinceStartup-wall<180){
         Time.timeScale=3;
         if(!rig.Input.FireHeld){await rig.MouseEvent(new Vector2(400,200),false);await rig.MouseEvent(new Vector2(400,200),true);}
         if(Runner.CurrentIndex!=previous){durations.Add(new{encounter=previous+1,seconds=Time.fixedTime-boundary});boundary=Time.fixedTime;previous=Runner.CurrentIndex;}
         var enemies=EnemyHealth.Active.Where(e=>e.IsAlive&&!e.GetComponent<EntryHoldFlight>().IsOutsideView).ToArray();peak=Math.Max(peak,enemies.Length);
         if(enemies.Length>0){var target=enemies.OrderBy(e=>Mathf.Abs(e.transform.position.x-rig.Player.transform.position.x)).First();rig.Place(new Vector2(target.transform.position.x,-3.8f));}
         if(Runner.Elapsed>4 && captured.Add(Runner.CurrentIndex))await Capture("mission"+mission+"-encounter"+(Runner.CurrentIndex+1));
         foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive)){
          if(pickup.transform.position.y<-.8f)rig.Place(pickup.transform.position);
         }
         await Task.Delay(100);
        }
        Require(Session.IsClear,"Mission did not complete via real primary fire");
        durations.Add(new{encounter=previous+1,seconds=Time.fixedTime-boundary});
        Evidence(new{status="PASS",mission,simulatedSeconds=Time.fixedTime-start,peak,durations,protectedPlayer=true,primary="Pulse",score=Runner.GetComponent<RunRewards>().Score,frames=Time.frameCount-rig.FirstFrame});
       }
      }catch(Exception e){Evidence(new{status="FAILED",mission,error=e.ToString()});}
    }
    public static object BeginBossChecks(){_ = BossChecks();return new{started=true};}
    static async Task BossChecks()
    {
      Evidence(new{status="RUNNING",test="boss"});
      try{using(var rig=new Rig(1600,6)){
        Runner.StopRun();Runner.Flow.LoadLevel(StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission04.asset"));
        Runner.StartRun("BOSS",false,true,null,null);rig.Protect();
        await Until(()=>UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>()!=null,2000,"Boss spawn");
        var boss=UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>();var health=boss.Health;
        Require(health.Definition.MissileTier==MissileDamageTier.Boss,"Boss missile classification");
        await Until(()=>boss.GetComponentsInChildren<BossAttackModule>().Sum(m=>m.ShotsFired)>=5,16000,"Phase A attacks");
        await Capture("screenshots/boss-phase-a-1600");
        Session.Pause();var hp=health.CurrentHp;var shots=boss.GetComponentsInChildren<BossAttackModule>().Sum(m=>m.ShotsFired);await Task.Delay(250);Require(hp==health.CurrentHp && shots==boss.GetComponentsInChildren<BossAttackModule>().Sum(m=>m.ShotsFired),"Pause advanced boss");Session.Resume();
        var reserve=rig.Player.GetComponent<EmergencyMissileReserve>();Require(reserve.TryUse(),"Reserve activation");await Until(()=>!reserve.IsInFlight,2000,"Reserve detonation");
        Require(hp-health.CurrentHp==Mathf.RoundToInt(health.MaxHp*.1f),"Reserve boss damage");
        await Capture("screenshots/boss-reserve-impact-1600");
        var view=Camera.main.GetComponent<GameplayViewController>();view.RequestView(new GameplayViewSettings(8,new Rect(0,0,1,1)),.1f);await Task.Delay(350);
        Require(view.ImpactRemaining<=0 && Mathf.Abs(Camera.main.projectionMatrix.m11-1/Camera.main.orthographicSize)<.001f,"Projection stuck after boss missile");
        rig.Unprotect();Require(rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null),"Boss death accepted");await Until(()=>rig.Gate.State==PlayerGameplayState.PostEntryInvincibility,3000,"Boss respawn");rig.Protect();
        health.ReceivePlayerHit(health.CurrentHp-health.MaxHp/2,rig.Player);await Until(()=>boss.IsPhaseB,1000,"Phase B threshold");await Capture("screenshots/boss-phase-b-1600");
        await Until(()=>boss.GetComponentsInChildren<BossAttackModule>()[2].ShotsFired>0,18000,"Phase B new sweep");
        var before=Runner.CompletionCount;health.ReceivePlayerHit(health.CurrentHp,rig.Player);
        Require(Runner.Stage==EncounterStage.BossPostDeath && health.gameObject.activeInHierarchy,"Staged defeat boundary");
        Require(UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length==0,"Postdefeat hostile shots");
        await Capture("screenshots/boss-death-1600");
        await Until(()=>Runner.CompletionCount==before+1,5000,"Boss completion");
        Require(UnityEngine.Object.FindObjectsByType<RewardPickup>().Any(p=>p.IsActive&&p.Crate!=null),"Boss reward missing");
        await Until(()=>Runner.GetComponent<RegionCampaignFlow>().DestinationReady,16000,"Region transition");
        Require(Runner.Flow.Direction==PlayDirection.Right && !rig.Gate.CanFire && !Runner.HasActiveRun,"Destination skeleton");
        await Capture("screenshots/region-ready-1600");Evidence(new{status="PASS",test="boss",phaseA=true,phaseB=true,reservePercent=10,pause=true,respawn=true,death=true,reward=true,transition=true,completion=Runner.CompletionCount});
      }}catch(Exception e){Evidence(new{status="FAILED",test="boss",error=e.ToString()});}
    }

    public static object BeginFullRegion(){_ = FullRegion();return new{started=true};}
    static async Task FullRegion()
    {
      Evidence(new{status="RUNNING",test="full-region"});
      try{using(var rig=new Rig(1600,6)){
        var owner=Runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();
        var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"starshift-m5-"+Guid.NewGuid().ToString("N"),"save.json");
        owner.UseTestStorage(path);Require(owner.NewGame(),"Campaign NewGame");rig.Protect();
        var start=Time.fixedTime;var wall=Time.realtimeSinceStartup;string previous="L1";float boundary=start;
        var durations=new List<object>();bool paused=false,died=false,switched=false,reserveUsed=false;int peak=0;
        var captured=new HashSet<string>();
        while(!Runner.GetComponent<RegionCampaignFlow>().DestinationReady && Time.realtimeSinceStartup-wall<600){
          if(Runner.Flow.CanContinue)Session.BeginMission();
          var id=Runner.Flow.Level.Id;
          if(id!=previous){durations.Add(new{mission=previous,seconds=Time.fixedTime-boundary});previous=id;boundary=Time.fixedTime;}
          if(rig.Gate.CanFire && !rig.Input.FireHeld){await rig.MouseEvent(new Vector2(400,200),false);await rig.MouseEvent(new Vector2(400,200),true);}
          var enemies=EnemyHealth.Active.Where(e=>e.IsAlive&&!e.GetComponent<EntryHoldFlight>().IsOutsideView).ToArray();peak=Math.Max(peak,enemies.Length);
          if(rig.Gate.CanInput && enemies.Length>0){var target=enemies.OrderBy(e=>Mathf.Abs(e.transform.position.x-rig.Player.transform.position.x)).First();rig.Place(new Vector2(target.transform.position.x,-3.8f));}
          if(rig.Gate.CanInput)foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive))if(pickup.transform.position.y<-.5f)rig.Place(pickup.transform.position);
          var key=id+"-"+Runner.CurrentIndex;
          if(Runner.Stage==EncounterStage.Running && Runner.Elapsed>4 && captured.Add(key))await Capture("screenshots/full-"+key);
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
        Evidence(new{status="PASS",test="full-region",seconds=Time.fixedTime-start,durations,peak,paused,died,switched,reserveUsed,protectedPlayer=true,injectedPlasmaCore=true,savePath=path,records=snapshot.Profile.Records.Length});
      }}catch(Exception e){Evidence(new{status="FAILED",test="full-region",error=e.ToString()});}
    }

    public static object BeginIntegrationChecks(){_ = IntegrationChecks();return new{started=true};}
    static async Task IntegrationChecks()
    {
      Evidence(new{status="RUNNING",test="integration"});
      try{using(var rig=new Rig(1600,6)){
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
        await Until(()=>UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>()!=null,1500,"Boss retry spawn");var boss=UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>();boss.Health.ReceivePlayerHit(130,rig.Player);await Task.Delay(100);Require(boss.IsPhaseB,"Boss phase fixture");
        Session.Pause();Require(owner.RetryEncounter(),"Boss Retry");rig.Protect();await Until(()=>UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>()!=null,1500,"Boss Retry respawn");boss=UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>();Require(boss.Health.CurrentHp==boss.Health.MaxHp&&!boss.IsPhaseB,"Boss retry retained runtime HP/phase");
        owner.ExitIsolated();var original=System.IO.File.ReadAllText(path);Require(owner.BeginReplay(),"L4 Replay");rig.Protect();Require(rig.Player.GetComponent<WeaponLoadout>().Level(WeaponBranch.Homing)==1,"Replay preset");
        var enemy=EnemyHealth.Active.FirstOrDefault(e=>e.IsAlive);if(enemy!=null)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);owner.ExitIsolated();Require(original==System.IO.File.ReadAllText(path),"Replay modified campaign bytes");
        data.Campaign.Status=StarShift.Persistence.CampaignStatus.RegionTransition;Require(store.Write(data,validate),"Transition fixture");owner.UseTestStorage(path);Require(owner.Continue(),"Transition Continue");
        await Until(()=>Runner.GetComponent<RegionCampaignFlow>().DestinationReady,12000,"Transition resume");Require(EnemyHealth.Active.Count==0&&owner.PersistentSnapshot.Profile.Records.Length==0,"Transition repeated fight/records");
        owner.ExitIsolated();owner.UseTestStorage(path);Require(owner.Continue()&&Runner.GetComponent<RegionCampaignFlow>().DestinationReady,"Ready Continue");
        Evidence(new{status="PASS",test="integration",laterM2Continue=true,freshStore=true,retry=true,bossRetry=true,replayBytesIsolated=true,preset=true,transitionResume=true,readyResume=true});
      }}catch(Exception e){Evidence(new{status="FAILED",test="integration",error=e.ToString()});}
    }

    public static object BeginContentChecks(){_ = ContentChecks();return new{started=true};}
    static async Task ContentChecks()
    {
      Evidence(new{status="RUNNING",test="content"});
      try{using(var rig=new Rig(2000,6)){
        var owner=Runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();owner.ExitIsolated();
        Func<int,LevelDefinition> level=n=>StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission0"+n+".asset");
        Runner.Flow.LoadLevel(level(1));Runner.StartRun("LEVEL_START",false,true,null,null);rig.Protect();owner.AdvanceMission(level(2));rig.Protect();
        Require(Runner.Stage==EncounterStage.Intro&&Runner.SpawnedCount==0&&Runner.Flow.IntroPhase==MissionIntroPhase.MissionTitle,"Arrival card must precede spawn");
        await Until(()=>Runner.Stage==EncounterStage.Running,5000,"Arrival card completion");
        await Until(()=>UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length>=2,10000,"Raider short burst");
        Runner.StopRun();Runner.Flow.LoadLevel(level(3));Runner.StartRun("ENCOUNTER_2",false,true,null,null);rig.Protect();Time.timeScale=3;
        await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&!e.Definition.BodyDamagesPlayer),10000,"Supply spawn");Time.timeScale=1;
        var supply=EnemyHealth.Active.First(e=>e.IsAlive&&!e.Definition.BodyDamagesPlayer);await Capture("screenshots/supply-introduction-2000");
        supply.ReceivePlayerHit(supply.CurrentHp,rig.Player);await Task.Delay(100);var crate=UnityEngine.Object.FindObjectsByType<RewardPickup>().Single(p=>p.Crate!=null);
        Require(Runner.Stage==EncounterStage.Running,"Supply became completion gate");rig.Place(crate.transform.position);await Until(()=>!crate.IsActive,1500,"Supply crate collection");
        Require(Runner.GetComponent<RunRewards>().SupplyCaptured==1,"Supply major reward");
        Runner.StartRun("ENCOUNTER_2",false,true,null,null);rig.Protect();Time.timeScale=3;
        await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&!e.Definition.BodyDamagesPlayer),10000,"Supply escape spawn");
        await Until(()=>Runner.CurrentIndex==2,18000,"Supply escape blocked timeline");
        Require(!UnityEngine.Object.FindObjectsByType<RewardPickup>().Any(p=>p.Crate!=null),"Escape granted crate");
        Runner.StartRun("ENCOUNTER_3",true,true,null,null);rig.Protect();Time.timeScale=1;await Task.Delay(2500);
        var elite=EnemyHealth.Active.Single(e=>e.Modifier!=null);Require(elite.MaxHp==11&&Mathf.Abs(elite.transform.localScale.x-1.12f)<.001f,"Elite modifier");await Capture("screenshots/elite-2000");
        Runner.StopRun();Runner.Flow.LoadLevel(level(2));Runner.StartRun("ENCOUNTER_3",true,true,null,null);rig.Protect();await Task.Delay(100);
        Require(EnemyHealth.Active.All(e=>e.Modifier==null&&e.transform.localScale==Vector3.one),"Elite pool reset");
        int count=Runner.GetComponentsInChildren<EnemyHealth>(true).Length;Runner.StartRun("ENCOUNTER_3",true,true,null,null);rig.Protect();await Task.Delay(100);
        Require(count==Runner.GetComponentsInChildren<EnemyHealth>(true).Length,"Enemy pool grew after warmup");
        Runner.StopRun();Runner.Flow.LoadLevel(level(4));Runner.StartRun("BOSS",true,true,null,null);rig.Protect();await Task.Delay(2600);await Capture("screenshots/boss-phase-a-2000");
        Evidence(new{status="PASS",test="content",arrivalBeforeSpawn=true,raiderBurst=true,supplyDestroy=true,crate=true,supplyEscape=true,elite=true,poolReset=true,enemyInstances=count,landscapeWidths=new[]{1600,2000}});
      }}catch(Exception e){Evidence(new{status="FAILED",test="content",error=e.ToString()});}
    }

    sealed class Rig : IDisposable
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
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"); gameView = EditorWindow.GetWindow(type);
            selectedSize = type.GetProperty("selectedSizeIndex"); oldSize = selectedSize.GetValue(gameView);
            gameViewZoom = gameView.GetType().GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameView);
            oldGameViewZoom = (Vector2)gameViewZoom.GetType().GetProperty("scale").GetValue(gameViewZoom);
            oldGameViewPan = (Vector2)gameViewZoom.GetType().GetProperty("translation").GetValue(gameViewZoom);
            type.GetMethod("SetCustomResolution", Fields).Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Closure QA " + width + "x900" });
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            oldMouse = Mouse.current;
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
        public void Dispose()
        {
            RestoreFlights();
            Session.enabled = false; Runner.StopRun(); Time.timeScale = 1;
            InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(touch);
            foreach (var d in disabled) InputSystem.EnableDevice(d);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor; InputSystem.settings.backgroundBehavior = oldBackground;
            var life = Player.GetComponent<PlayerDamageLifecycle>(); life.enabled = false; life.enabled = true; Input.CancelGesture();
            view.SetBaseline(Runner.Flow.Level.Camera.View); view.RequestView(oldView, 0); gameView.GetType().GetMethod("SizeSelectionCallback").Invoke(gameView, new object[] { oldSize, null });
            gameViewZoom.GetType().GetMethod("SetTransform").Invoke(gameViewZoom, new object[] { oldGameViewPan, oldGameViewZoom });
            UnityEngine.Object.Destroy(profile);
            Session.enabled = true; Time.timeScale = oldScale; AudioListener.pause = oldAudio;
            Require(definitions.SequenceEqual(Configs().Select(EditorJsonUtility.ToJson)), "QA mutated encounter config assets.");
        }
    }
}
