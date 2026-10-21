using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Dev;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEngine;

public sealed class ClosurePointer : PlayerPointerInput
{
    public Vector2 Delta;
    public override bool FireHeld => false;
    public override Vector2 ReadDelta(){var result=Delta;Delta=Vector2.zero;return result;}
    public override void ResetDrag()=>Delta=Vector2.zero;
}

public static class VerifyArtClosure
{
    const string Output="../.design/art-closure/";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static EncounterRunner runner;
    static PlayerDamageLifecycle player;
    static PlayerGameplayGate gate;
    static PlayerTemporaryEffects effects;
    static RelativeDragMovement movement;
    static ClosurePointer pointer;
    public static Task CurrentTask {get;private set;}
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static async Task Until(Func<bool> condition,int ms,string message)
    {float end=Time.realtimeSinceStartup+ms/1000f;while(!condition()&&Time.realtimeSinceStartup<end)await Task.Delay(10);Check(condition(),message);}
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
    static PickupDefinition Item(string id)=>AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/"+id+".asset");
    static void Evidence(string id,object value)=>File.WriteAllText(Output+id+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    static async Task Capture(string id)
    {string path=Path.GetFullPath(Output+"screenshots/"+id+".png");if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);await Until(()=>File.Exists(path),2000,"Screenshot "+id);}
    static void Place(Vector2 viewport)
    {var camera=Camera.main;movement.SetCinematicPosition(camera.ViewportToWorldPoint(new Vector3(viewport.x,viewport.y,-camera.transform.position.z)));}
    static void Protect(){gate.SetCinematicLock(false);gate.SetState(PlayerGameplayState.Active);player.GrantBriefProtection(1000);}
    static void Drag(Vector2 worldDelta)
    {var camera=Camera.main;float height=camera.orthographicSize*2;pointer.Delta=new Vector2(worldDelta.x/height/camera.aspect*camera.pixelWidth,worldDelta.y/height*camera.pixelHeight);}
    static async Task Start(string mission,string point="LEVEL_START")
    {
        runner.GetComponent<RegionCampaignFlow>().Cancel();runner.StopRun();
        runner.Flow.LoadLevel(runner.GetComponent<RegionCampaignFlow>().FindMission(mission));
        player.RestoreCheckpoint(3);
        runner.StartRun(point,true,true,null,null);Protect();
        UnityEngine.Object.FindAnyObjectByType<ShellView>().Show(ShellScreen.Playing);Time.timeScale=1;
        UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);await Task.Delay(50);
    }
    public static object Begin(string scope="all")
    {
        Check(CurrentTask==null||CurrentTask.IsCompleted,"Probe already running");
        Directory.CreateDirectory(Output+"screenshots");Evidence("verification",new{status="running",scope});
        CurrentTask=Run(scope);return new{started=true,scope,evidence=Output+"verification.json"};
    }
    static async Task Run(string scope)
    {
        try
        {
            Check(Application.isPlaying,"Play Mode required");runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
            var save=runner.GetComponent<CampaignLifecycle>();Check(!runner.HasActiveRun||save.Mode==SessionMode.DevTest,"Idle Menu or own DevTest required");
            var shell=runner.GetComponent<GameShell>();var original=save.SavePath;var oldLevel=runner.Flow.Level;
            var bytes=new[]{"",".bak",".invalid"}.ToDictionary(s=>s,s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null);
            Directory.CreateDirectory(Output+"save-guard");foreach(var p in bytes)if(p.Value!=null)File.WriteAllBytes(Output+"save-guard/"+(p.Key==""?"primary":p.Key.Substring(1))+".json",p.Value);
            var temp=Path.Combine(Path.GetTempPath(),"starshift-closure-"+Guid.NewGuid().ToString("N"),"save.json");
            player=UnityEngine.Object.FindAnyObjectByType<PlayerDamageLifecycle>();gate=player.GetComponent<PlayerGameplayGate>();effects=player.GetComponent<PlayerTemporaryEffects>();movement=player.GetComponent<RelativeDragMovement>();
            var input=player.GetComponent<InputSystemPointerInput>();bool oldInput=input.enabled;var inputField=typeof(RelativeDragMovement).GetField("pointerInput",Private);var oldPointer=inputField.GetValue(movement);
            Check(!player.GetComponents<MonoBehaviour>().Any(c=>c!=null&&c.GetType().Name=="ClosurePointer"),"Another closure probe owns Player");
            pointer=player.gameObject.AddComponent<ClosurePointer>();var view=Camera.main.GetComponent<GameplayViewController>();var oldView=view.TargetView;
            try
            {
                runner.GetComponent<RegionCampaignFlow>().Cancel();runner.StopRun();save.UseTestStorage(temp);shell.Boot();if(scope!="e2e"){input.enabled=false;inputField.SetValue(movement,pointer);}else input.enabled=true;
                view.RequestView(new GameplayViewSettings(9,new Rect(0,0,1,1)),0);
                if(scope=="all"||scope=="carry")await Carry();
                if(scope=="all"||scope=="strike")await Strike();
                if(scope=="all"||scope=="beams")await Beams();
                if(scope=="all"||scope=="boss")await BossSkills();
                if(scope=="all"||scope=="dev")await DevRoutes();
                if(scope=="all"||scope=="art")await ArtFlow();
                if(scope=="death-world")await DeathWorld();
                if(scope=="e2e")
                {
                    UnityEngine.Object.FindAnyObjectByType<ShellView>().Show(ShellScreen.Playing);gate.SetCinematicLock(false);
                    var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("VerifyM4Testbed")).Last(t=>t!=null);
                    var result=await (Task<object>)type.GetMethod("FullMission").Invoke(null,new object[]{1600,true});Evidence("e2e",result);
                }
            }
            finally
            {
                await Until(()=>!shell.IsBusy,5000,"Pending shell operation before save restore");
                Time.timeScale=1;runner.GetComponent<RegionCampaignFlow>().Cancel();runner.StopRun();inputField.SetValue(movement,oldPointer);UnityEngine.Object.Destroy(pointer);input.enabled=oldInput;
                foreach(var driver in runner.GetComponentsInChildren<BossPhaseRunner>(true))driver.enabled=true;
                save.ExitIsolated();runner.Flow.LoadLevel(oldLevel);save.UseTestStorage(original);shell.Boot();view.RequestView(oldView,0);Camera.main.ResetAspect();
                foreach(var p in bytes){var after=File.Exists(original+p.Key)?File.ReadAllBytes(original+p.Key):null;Check(p.Value==null?after==null:after!=null&&p.Value.SequenceEqual(after),"Production save changed");}
                if(Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
            }
            Evidence("verification",new{status="PASS",scope,productionSaveGuard=true});
        }
        catch(Exception error){Evidence("verification",new{status="FAIL",scope,error=error.ToString()});}
    }
    static async Task Carry()
    {
        foreach(var mission in new[]{"L1","L4"})
        {
            await Start(mission);runner.StopRun();Protect();
            effects.Apply(Item("ShieldCell"));effects.Apply(Item("Overdrive"));effects.Apply(Item("SupportDrone"));
            var loadout=player.GetComponent<WeaponLoadout>();loadout.ApplyCore(WeaponBranch.Plasma,true);loadout.ApplyCore(WeaponBranch.Plasma,true);
            UnityEngine.Object.FindAnyObjectByType<EmergencyMissileReserve>().RestoreCheckpoint(2);
            var region=runner.GetComponent<RegionCampaignFlow>();var target=region.NextMission;Check(region.QueueAdvance(),"Advance rejected "+mission);
            if(mission=="L4")
            {
                var journey=runner.GetComponent<PlanetJourney>();await Until(()=>journey.IsAwaitingTap,10000,"Region destination");
                Check(effects.HasShield&&effects.OverdriveRemaining>0&&effects.DroneRemaining>0,"Major travel cleared buffs");Check(journey.AcceptTap(),"Destination tap");
            }
            await Until(()=>runner.Flow.Level==target&&runner.HasActiveRun,10000,"Next mission "+mission);
            Check(effects.HasShield&&effects.OverdriveRemaining>0&&effects.DroneRemaining>0,"Mission arrival cleared buffs "+mission);
            Check(loadout.ActiveBranch==WeaponBranch.Plasma&&loadout.ActiveLevel==2&&UnityEngine.Object.FindAnyObjectByType<EmergencyMissileReserve>().Current==2,"Arrival lost loadout/ammo");
            await Capture("carry-"+mission);
        }
        gate.SetCinematicLock(false);gate.SetState(PlayerGameplayState.Active);Check(player.TryReceiveHit(null)&&!effects.HasShield,"Carried shield did not absorb");
        Check(player.GetComponent<PlayerLives>().CurrentLives==3,"Shield lost life");
        gate.SetState(PlayerGameplayState.Active);Check(player.TryReceiveHit(null),"Life-loss fixture rejected");
        Check(!effects.HasShield&&effects.OverdriveRemaining==0&&effects.DroneRemaining==0,"Life loss retained buffs");
        await Start("L1");Check(!effects.HasShield&&effects.OverdriveRemaining==0&&effects.DroneRemaining==0,"Fresh run retained buffs");
        Evidence("carry",new{status="PASS",samePlanet=true,regionTravel=true,shieldOverdriveDronePreserved=true,loadoutAndAmmoPreserved=true,shieldAbsorbs=true,deathAndNewRunClear=true});
    }
    static async Task Strike()
    {
        await Start("L1");runner.StopRun();Protect();Place(new Vector2(.35f,.25f));
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/Enemies/Interceptor.prefab");var root=UnityEngine.Object.Instantiate(prefab);root.SetActive(false);
        try
        {
            var flight=root.GetComponent<EntryHoldFlight>();flight.Configure(new Vector2(.5f,.75f),new Vector2(.5f,.75f),Vector2.zero,.02f,new Vector2(.5f,1.3f),60,0);
            root.GetComponent<SingleFireAttack>().SetEncounterControlled();root.SetActive(true);var attack=root.GetComponent<DirectedAttack>();
            Check(root.GetComponent<EnemyFirePresentation>()==null,"Duplicate muzzle cue remains");
            var warning=Field<SpriteRenderer>(root.GetComponent<DirectedAttackTelegraph>(),"warning");
            var launch=Field<SpriteRenderer>(root.GetComponent<DirectedAttackTelegraph>(),"launch");
            for(int reuse=0;reuse<2;reuse++)
            {
                if(reuse>0){root.SetActive(false);Place(new Vector2(.68f,.25f));root.SetActive(true);}
                await Until(()=>flight.IsHolding,2000,"Interceptor hold");await Task.Delay(400);Check(attack.TryRequestAttack(),"Interceptor request");await Task.Delay(45);
                var target=Camera.main.WorldToViewportPoint(player.transform.position);Check(Vector2.Distance(attack.AttackAim,target)<.015f,"Strike target reused old aim");
                await Until(()=>warning.enabled&&Vector2.Distance(warning.transform.position,player.transform.position)<.3f,300,"Lock cue not at current target; strike="+Field<bool>(root.GetComponent<DirectedAttackTelegraph>(),"strike")+" warning="+warning.transform.position+" player="+player.transform.position+" enabled="+warning.enabled);
                await Capture("strike-lock-"+reuse);
                bool lateLockCaptured=false;float chargeEnd=Time.realtimeSinceStartup+2;
                while(attack.AttacksFired==0&&Time.realtimeSinceStartup<chargeEnd)
                {
                    Check(warning.enabled&&launch.enabled,"Strike charge cue disappeared before commit");
                    Check(warning.bounds.size.x/(Camera.main.orthographicSize*2)*900>=95,"Strike lock hidden inside player hull");
                    Check(Vector2.Distance(launch.transform.position,root.transform.position)<.1f,"Charge cue left attacker");
                    if(!lateLockCaptured&&attack.Phase==AttackPhase.Lock&&Field<float>(root.GetComponent<DirectedAttackTelegraph>(),"chargeAge")>.55f)
                    {
                        await Capture("strike-late-lock-"+reuse);lateLockCaptured=true;
                        Time.timeScale=0;await Until(()=>Time.deltaTime==0,500,"Pause frame");var frozen=warning.transform.localScale;var frame=launch.sprite;await Task.Delay(100);
                        Check(warning.transform.localScale==frozen&&launch.sprite==frame,"Paused strike warning advanced");Time.timeScale=1;
                    }
                    await Task.Delay(15);
                }
                Check(attack.AttacksFired>0&&lateLockCaptured,"Strike commit or readable late lock missing");
                await Until(()=>!attack.IsBusy,2500,"Strike return");await Task.Delay(40);Check(!warning.enabled&&!launch.enabled,"Recovery left a ghost target/charge cue");
                await Capture("strike-return-"+reuse);
            }
        }
        finally{UnityEngine.Object.Destroy(root);}
        Evidence("strike",new{status="PASS",singleCue=true,freshAim=true,noReturnGhost=true,poolReuse=true,fullChargeVisible=true,attackerChargeCue=true,lockOutsideHull=true,pause=true});
    }
    static async Task<EnemyHealth> Boss(string mission)
    {
        await Start(mission,"BOSS");await Until(()=>runner.GetComponentInChildren<BossPhaseRunner>()?.GetComponent<EntryHoldFlight>().IsHolding==true,12000,"Boss entry "+mission);
        var driver=runner.GetComponentInChildren<BossPhaseRunner>();driver.enabled=false;UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ClearProjectiles();return driver.Health;
    }
    static async Task Beams()
    {
        foreach(float aspect in new[]{1600f/900,2000f/900})foreach(int sign in new[]{-1,1})
        {
            Camera.main.aspect=aspect;var boss=await Boss("L8");
            var module=boss.GetComponentsInChildren<BossAttackModule>().First(m=>Field<DirectedAttack>(m,"directed")?.Definition.Sweep==true);
            var attack=Field<DirectedAttack>(module,"directed");Protect();Place(new Vector2(.35f,.5f));bool started=false;await Until(()=>started||(started=module.BeginAttack()),6000,"Sweep start");
            await Until(()=>attack.IsActive,2500,"Sweep active");await Task.Delay(600);
            float lane=Camera.main.WorldToViewportPoint(attack.DamageBounds.center).y;
            Place(new Vector2(.35f,lane-sign*.10f));await Task.Delay(65);
            var lives=player.GetComponent<PlayerLives>();lives.RestoreCheckpoint(3);effects.Clear();gate.SetState(PlayerGameplayState.Active);
            var before=player.transform.position;Drag(Vector2.up*(sign*.22f*Camera.main.orthographicSize*2));
            await Until(()=>lives.CurrentLives==2,400,"Fast drag tunneled through Warden laser "+aspect+" sign "+sign);
            Check(Mathf.Abs(player.GetComponent<Rigidbody2D>().position.y-before.y)>.5f,"Actual relative drag did not move");await Capture("sweep-hit-"+Mathf.RoundToInt(aspect*900)+"-"+sign);
        }
        Evidence("beams",new{status="PASS",fastDragAcrossLaser=true,bothDragDirections=true,aspects=new[]{1600,2000},actualRelativeMovement=true});
    }
    static async Task BossSkills()
    {
        var boss=await Boss("L4");Protect();Place(new Vector2(.85f,.2f));
        var module=boss.GetComponentsInChildren<BossAttackModule>().First(m=>Field<AttackBeamPresentation>(m,"beam")!=null);
        var presentation=Field<AttackBeamPresentation>(module,"beam");var line=Field<LineRenderer>(presentation,"column");Check(module.BeginAttack(),"Lane beam start");
        await Until(()=>module.IsBeamActive,2000,"Lane beam active");float from=line.GetPosition(1).x;await Task.Delay(350);float right=line.GetPosition(1).x;
        Check(right>from+.1f,"Vanguard laser did not follow right");Place(new Vector2(.15f,.2f));await Task.Delay(350);Check(line.GetPosition(1).x<right-.1f,"Vanguard laser did not reverse left");await Capture("vanguard-tracking");
        module.CancelAttack();Check(!boss.GetComponent<EntryHoldFlight>().MotionPaused,"Cancel retained motion lock");
        var barrage=boss.GetComponentInChildren<BossBarrage>();var curtain=barrage.GetComponent<BossAttackModule>();Check(curtain.BeginAttack(),"Curtain start");
        var lanes=new List<float>();var openings=new List<Vector2>();int previous=barrage.PulsesFired;float end=Time.realtimeSinceStartup+12;
        while(curtain.IsBusy&&Time.realtimeSinceStartup<end)
        {
            var core=Field<LineRenderer>(Field<AttackBeamPresentation>(barrage,"lowerBandPresentation"),"core");Check(!core.enabled,"Debug field border visible");
            if(barrage.PulsesFired!=previous)
            {
                previous=barrage.PulsesFired;lanes.Add(barrage.SafeLane);
                var shots=UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Where(p=>p.Owner==boss.gameObject).ToArray();float newest=shots.Max(p=>p.transform.position.y);
                var boxes=shots.Where(p=>p.transform.position.y>newest-.1f).Select(p=>p.GetComponent<BoxCollider2D>().bounds).OrderBy(b=>b.center.x).ToArray();
                float x=Camera.main.ViewportToWorldPoint(new Vector3(barrage.SafeLane,.5f,10)).x;
                openings.Add(new Vector2(boxes.Where(b=>b.center.x<x).Max(b=>b.max.x),boxes.Where(b=>b.center.x>x).Min(b=>b.min.x)));
                await Capture("curtain-row-"+lanes.Count);
            }
            await Task.Delay(10);
        }
        Check(lanes.Count==3&&Mathf.Abs(lanes[0]-lanes[1])>.25f&&Mathf.Abs(lanes[0]-lanes[2])<.001f,"Curtain did not force readable zigzag");
        Check(openings.Max(g=>g.x)>openings.Min(g=>g.y),"A stationary safe column survives all curtain rows");
        Evidence("boss",new{status="PASS",noStationarySafeColumn=true,boundedTrackingBothDirections=true,cancelReleasesMotion=true,curtainLanes=lanes,noDebugBorders=true});
    }
    static async Task DeathWorld()
    {
        var fire=UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();
        var session=runner.GetComponent<GameplaySession>();var shell=runner.GetComponent<GameShell>();
        var view=UnityEngine.Object.FindAnyObjectByType<ShellView>();var lives=player.GetComponent<PlayerLives>();
        foreach(var kind in new[]{"lane","curtain","sweep","ion"})
        {
            bool fatal=kind=="sweep";var boss=await Boss(kind=="lane"||kind=="curtain"?"L4":"L8");
            var driver=boss.GetComponent<BossPhaseRunner>();
            var module=boss.GetComponentsInChildren<BossAttackModule>().First(m=>kind=="lane"?Field<AttackBeamPresentation>(m,"beam")!=null:
                kind=="sweep"?Field<DirectedAttack>(m,"directed")?.Definition.Sweep==true:
                Field<BossBarrage>(m,"barrage")?.Definition.Kind==(kind=="curtain"?BossBarrageKind.ProjectileCurtain:BossBarrageKind.IonShelter));
            Protect();Place(new Vector2(.15f,.2f));bool started=false;await Until(()=>started||(started=module.BeginAttack()),6000,"Death world skill start "+kind);
            typeof(BossPhaseRunner).GetField("current",Private).SetValue(driver,module);driver.enabled=true;
            await Until(()=>module.IsBeamActive||Field<DirectedAttack>(module,"directed")?.IsActive==true||Field<BossBarrage>(module,"barrage")?.IsActive==true,3000,"Active skill "+kind);
            var hostileDefinition=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<SingleFireDefinition>("Assets/StarShift/Data/RegionA/Boss/VanguardCurtainProjectile.asset"));
            var friendlyDefinition=UnityEngine.Object.Instantiate(player.GetComponent<WeaponLoadout>().ActiveDefinition);
            typeof(SingleFireDefinition).GetField("projectileSpeed",Private).SetValue(hostileDefinition,.1f);
            typeof(PulseLaserDefinition).GetField("projectileSpeed",Private).SetValue(friendlyDefinition,.1f);
            typeof(PulseLaserDefinition).GetField("projectileLifetime",Private).SetValue(friendlyDefinition,10f);
            var hostile=UnityEngine.Object.Instantiate(hostileDefinition.ProjectilePrefab);var friendly=UnityEngine.Object.Instantiate(friendlyDefinition.ProjectilePrefab);
            var emitter=new GameObject("DeathWorldShot").transform;emitter.position=Camera.main.ViewportToWorldPoint(new Vector3(.08f,.6f,9));emitter.position=new Vector3(emitter.position.x,emitter.position.y,0);
            try
            {
                hostile.Launch(hostileDefinition,emitter,boss.gameObject,player.gameObject,p=>p.ResetForPool(),0,Vector2.down);
                friendly.Launch(friendlyDefinition,emitter.position+Vector3.right,Vector2.up,1,0,player.gameObject,p=>p.ResetForPool());
                effects.Clear();lives.RestoreCheckpoint(fatal?1:3);gate.SetState(PlayerGameplayState.Active);
                int generation=runner.Generation,spawned=runner.SpawnedCount;var position=hostile.transform.position;
                Check(player.TryReceiveHit(boss.gameObject),"Death world hit "+kind);
                if(fatal)Check(!runner.NotifyBossDefeated(generation),"Fatal death allowed simultaneous boss completion");
                await Task.Delay(120);
                Check(module.IsBusy&&boss.gameObject.activeInHierarchy,"Player death cancelled skill/actor "+kind);
                Check(hostile.Owner!=null&&friendly.Owner!=null&&hostile.transform.position!=position,"Player death cleared/froze flying shots "+kind);
                Check(!fire.CanFire&&fire.CanContinueAttack&&!module.BeginAttack(),"Death fire permissions "+kind);
                Check(!player.TryReceiveHit(boss.gameObject),"Duplicate death hit "+kind);
                await Capture("death-world-dying-"+kind);
                if(fatal)
                {
                    await Until(()=>session.IsTerminal&&shell.Screen==ShellScreen.Results,2000,"Game Over overlay");
                    Check(runner.Generation==generation&&runner.SpawnedCount==spawned,"Fatal death advanced or cleaned runner");
                    Check(boss.gameObject.activeInHierarchy&&hostile.Owner!=null&&friendly.Owner!=null&&module.IsBusy,"Game Over erased battlefield");
                    Check(Time.timeScale==0&&!gate.CanInput&&!gate.CanTakeDamage,"Game Over permissions/clock");
                    var frozen=hostile.transform.position;await Task.Delay(120);Check(hostile.transform.position==frozen,"Game Over scene moved");
                    await Capture("death-world-game-over");view.ResultMenu.onClick.Invoke();await Until(()=>!shell.IsBusy&&shell.Screen==ShellScreen.Menu,4000,"Game Over Menu");
                    Check(!boss.gameObject.activeInHierarchy&&hostile.Owner==null&&friendly.Owner==null,"Menu did not clean retained battlefield");
                }
                else
                {
                    await Until(()=>gate.State==PlayerGameplayState.PostEntryInvincibility,5000,"Respawn "+kind);
                    Check(lives.CurrentLives==2&&boss.gameObject.activeInHierarchy,"Respawn lost life/actor "+kind);
                    await Until(()=>fire.CanFire,2000,"Respawn grace "+kind);Protect();
                    driver.enabled=false;module.CancelAttack();Check(module.BeginAttack(),"Hard cancel fixture "+kind);
                    fire.Block();await Task.Delay(80);Check(!module.IsBusy,"Hard block stopped cancelling "+kind);
                }
            }
            finally
            {
                Time.timeScale=1;UnityEngine.Object.Destroy(hostile.gameObject);UnityEngine.Object.Destroy(friendly.gameObject);UnityEngine.Object.Destroy(emitter.gameObject);
                UnityEngine.Object.Destroy(hostileDefinition);UnityEngine.Object.Destroy(friendlyDefinition);
            }
        }
        foreach(var name in new[]{"Interceptor","Sniper","Bomber"})
        {
            await Start("L1");runner.StopRun();Protect();Place(new Vector2(.15f,.2f));
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/Enemies/"+name+".prefab"));root.SetActive(false);
            try
            {
                var flight=root.GetComponent<EntryHoldFlight>();flight.Configure(new Vector2(.5f,.75f),new Vector2(.5f,.75f),Vector2.zero,.02f,new Vector2(.5f,1.3f),60,0);
                var attack=root.GetComponent<SingleFireAttack>();attack.SetEncounterControlled();root.SetActive(true);
                await Until(()=>flight.IsHolding,2000,"Death enemy hold "+name);bool started=false;
                await Until(()=>started||(started=attack.TryRequestAttack()),3000,"Death enemy charge "+name);
                effects.Clear();lives.RestoreCheckpoint(3);gate.SetState(PlayerGameplayState.Active);Check(player.TryReceiveHit(root),"Death enemy hit "+name);
                await Task.Delay(100);Check(attack.IsTelegraphing&&root.activeInHierarchy,"Death cancelled enemy charge "+name);
                await Capture("death-world-enemy-"+name);
                await Until(()=>!attack.IsTelegraphing,4000,"Committed enemy attack did not finish "+name);
            }
            finally{UnityEngine.Object.Destroy(root);}
        }
        var save=runner.GetComponent<CampaignLifecycle>();save.ExitIsolated();shell.Boot();Check(save.NewGame()&&save.Continue(),"Retry campaign fixture");view.Show(ShellScreen.Playing);
        Protect();lives.RestoreCheckpoint(1);gate.SetState(PlayerGameplayState.Active);Check(player.TryReceiveHit(null),"Retry fatal hit");
        await Until(()=>session.IsTerminal&&shell.Screen==ShellScreen.Results,2000,"Retry Game Over");
        Check(session.CanRetry,"Retained world blocked Retry");view.GameOverRetry.onClick.Invoke();
        await Until(()=>!shell.IsBusy&&!session.IsTerminal&&runner.HasActiveRun,5000,"Retry retained world");
        Check(lives.CurrentLives==3&&Time.timeScale>0,"Retry did not restore checkpoint/clock");
        Evidence("death-world",new{status="PASS",skills=4,enemyAttacks=3,respawn=true,hostileAndFriendlyShots=true,noDuplicateDeath=true,noNewAttack=true,hardCancel=true,gameOverRetainsWorld=true,noFatalSpawnOrProgression=true,menuCleanup=true,checkpointRetry=true});
    }
    static async Task DevRoutes()
    {
        foreach(var driver in runner.GetComponentsInChildren<BossPhaseRunner>(true))driver.enabled=true;
        var dev=runner.GetComponent<DevFastStart>();var before=EditorJsonUtility.ToJson(dev);var region=runner.GetComponent<RegionCampaignFlow>();
        try
        {
            foreach(var mission in new[]{"L1","L4"})
            {
                region.Cancel();runner.StopRun();var so=new SerializedObject(dev);so.FindProperty("devModeEnabled").boolValue=true;so.FindProperty("startLevel").objectReferenceValue=region.FindMission(mission);so.FindProperty("startPoint").stringValue="TRANSITION";so.FindProperty("skipIntro").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
                Check(dev.TryStart(),"Dev transition rejected "+mission);Check(region.IsTravelling&&runner.GetComponent<CampaignLifecycle>().Mode==SessionMode.DevTest,"Dev route not isolated/travelling");
                if(mission=="L1")await Until(()=>runner.Flow.Level.Id=="L2"&&runner.HasActiveRun,5000,"Dev same-planet arrival: level="+runner.Flow.Level.Id+" active="+runner.HasActiveRun+" travel="+region.IsTravelling+" scale="+Time.timeScale);
                else Check(runner.GetComponent<PlanetJourney>().IsActive,"Dev region route did not enter journey");
            }
            region.Cancel();runner.StopRun();var config=new SerializedObject(dev);config.FindProperty("startPoint").stringValue="BOSS_PHASE_B";config.ApplyModifiedPropertiesWithoutUndo();Check(dev.TryStart(),"Dev phase B rejected");
            Protect();await Until(()=>runner.GetComponentInChildren<BossPhaseRunner>()?.IsPhaseB==true,12000,"Dev phase B route");await Capture("dev-phase-b");
            Evidence("dev-runtime",new{status="PASS",samePlanetTransition=true,regionTransition=true,bossPhaseB=true,devTestMode=true});
        }
        finally{region.Cancel();runner.StopRun();EditorJsonUtility.FromJsonOverwrite(before,dev);}
    }

    static async Task ArtFlow()
    {
        await Start("L1");runner.StopRun();Protect();Place(new Vector2(.5f,.25f));await Task.Delay(50);
        var receiver=player.GetComponent<PlayerPickupReceiver>();var feedback=player.GetComponent<PlayerCombatPresentation>();var reward=Field<SpriteRenderer>(feedback,"reward");
        foreach(var pair in new[]{new[]{"StellarShard","pickup-coin"},new[]{"PulseCore","pickup-collect-upgrade"},new[]{"ShieldCell","pickup-shield"}})
        {
            Check(receiver.TryCollect(Item(pair[0])),"Collection "+pair[0]);await Task.Delay(100);
            Check(reward.enabled&&reward.sprite.name.StartsWith(pair[1]+"_",StringComparison.Ordinal),"Collection bank "+pair[0]);await Capture("collect-"+pair[0]);await Task.Delay(300);
        }
        await Start("L1");Protect();await Until(()=>runner.Stage==EncounterStage.Running,6000,"Missile art fixture entry");
        var reserve=UnityEngine.Object.FindAnyObjectByType<EmergencyMissileReserve>();reserve.RestoreCheckpoint(3);Check(reserve.TryUse(),"Reserve art launch rejected");
        var missileFlash=Field<SpriteRenderer>(UnityEngine.Object.FindAnyObjectByType<EmergencyMissilePresentation>(),"flash");
        await Until(()=>missileFlash.enabled,3000,"Missile explosion missing");await Task.Delay(70);var first=missileFlash.sprite;await Task.Delay(60);Check(missileFlash.sprite!=first&&missileFlash.sprite.name.StartsWith("missile-splash_",StringComparison.Ordinal),"Missile splash did not animate");
        Time.timeScale=0;await Task.Delay(30);first=missileFlash.sprite;await Task.Delay(80);Check(missileFlash.sprite==first,"Missile splash ignored Pause");await Capture("missile-splash");Time.timeScale=1;
        foreach(var mission in new[]{"L4","L8"})
        {
            var boss=await Boss(mission);var shields=boss.GetComponent<BossShieldNetwork>();
            if(shields!=null)
            {
                foreach(var node in boss.GetComponentsInChildren<EnemyHealth>().Where(h=>h!=boss))node.ReceivePlayerHit(node.CurrentHp,player.gameObject);
                await Until(()=>shields.IsExposed,4000,"Death fixture expose");
            }
            await Until(()=>boss.CanReceiveDamage,2000,"Death fixture damage gate");boss.ReceivePlayerHit(boss.CurrentHp,player.gameObject);
            var death=boss.GetComponent<BossDeathSequence>();Check(death.IsPlaying,"Boss death did not start");await Task.Delay(2080);
            var bursts=Field<SpriteRenderer[]>(death,"explosions");var final=bursts[bursts.Length-1];Check(final.bounds.size.x>1.4f,"Final boss explosion too small");Check(final.enabled&&final.sprite.name.StartsWith("boss-blast_",StringComparison.Ordinal),"Final boss explosion missing near completion");
            Time.timeScale=0;await Task.Delay(40);var frozen=final.sprite;await Task.Delay(100);Check(final.sprite==frozen,"Boss death animation ignored Pause");await Capture("boss-final-"+mission);Time.timeScale=1;
            await Until(()=>!death.IsPlaying,1500,"Boss final callback did not finish");Check(runner.CompletionCount==1,"Boss death duplicate/missing completion");
        }
        Evidence("art",new{status="PASS",distinctCoinCoreShieldCollection=true,bossFinalTimedToCompletion=true,deathPause=true,missileAnimationAndPause=true,onceOnlyCompletion=true});
    }

}
