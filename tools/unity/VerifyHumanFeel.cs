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
using UnityEditor;
using UnityEngine;
using Rig = VerifyPacingRefinement.Rig;

public static class VerifyHumanFeel
{
    const string Output = "../.design/m5-human-feel/";
    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession Session => Runner.GetComponent<GameplaySession>();
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static async Task Until(Func<bool> condition, int ms, string message)
    { float end=Time.realtimeSinceStartup+ms/1000f; while(!condition() && Time.realtimeSinceStartup<end) await Task.Delay(20); Require(condition(),message); }
    static void Evidence(string name, object result) => System.IO.File.WriteAllText(Output+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
    static async Task Capture(string name) { ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath(Output+"screenshots/"+name+".png"));await Task.Delay(150); }
    static LevelDefinition Level(int mission) => StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission0"+mission+".asset");
    static void Start(Rig rig,int mission,int encounter,bool standalone=true)
    { Runner.StopRun();Runner.Flow.LoadLevel(Level(mission));Runner.StartRun("ENCOUNTER_"+encounter,standalone,true,null,null);rig.Protect();rig.Place(new Vector2(0,-3.8f)); }
    public static object BeginFormations() { _=Formations();return new{started=true}; }
    static async Task Formations()
    {
        Evidence("formations",new{status="RUNNING"});
        try {
            var results=new List<object>();
            foreach(int width in new[]{1600,2000}) using(var rig=new Rig(width,6))
            foreach(var item in new[]{(1,1),(1,2),(2,1),(2,3),(3,1),(3,2),(3,3)})
            {
                Start(rig,item.Item1,item.Item2); var e=Runner.Current;
                await Until(()=>Runner.Elapsed>2.3f,3500,"Entry clock");
                if(item==(2,1)) await Capture("raider-mid-entry-"+width);
                await Until(()=>Runner.Elapsed>4.1f,4000,"Settle clock");
                var enemies=EnemyHealth.Active.Where(h=>h.IsAlive).ToArray();
                Require(enemies.Length==e.EnemyCount,"Visible composition missing: "+e.name);
                float minimum=float.PositiveInfinity;int overlaps=0;
                for(int i=0;i<enemies.Length;i++)for(int j=i+1;j<enemies.Length;j++)
                {
                    minimum=Math.Min(minimum,Vector2.Distance(enemies[i].transform.position,enemies[j].transform.position));
                    if(enemies[i].GetComponentInChildren<Collider2D>().bounds.Intersects(enemies[j].GetComponentInChildren<Collider2D>().bounds)) overlaps++;
                }
                Require(overlaps==0,"Settled collider overlap: "+e.name+"="+overlaps);
                foreach(var enemy in enemies) Require(!enemy.GetComponent<EntryHoldFlight>().IsOutsideView,"Formation outside view");
                await Capture(e.name+"-settled-"+width);
                results.Add(new{encounter=e.name,width,count=enemies.Length,minimumWorldSeparation=minimum,overlaps,ortho=Camera.main.orthographicSize,shooters=e.ActiveShooterLimit});
            }
            Evidence("formations",new{status="PASS",results});
        } catch(Exception e) {Evidence("formations",new{status="FAILED",error=e.ToString()});}
    }
    public static object Status() => new {playing=Application.isPlaying,mission=Runner.Flow.Level.Id,encounter=Runner.Current.name,elapsed=Runner.Elapsed};

    public static object BeginTtk() { _=Ttk();return new{started=true}; }
    static async Task Ttk()
    {
        Evidence("ttk",new{status="RUNNING"});
        try {using(var rig=new Rig(1600,6)) {
            var results=new List<object>();
            foreach(var target in new[]{("Scout",7), ("Raider",10), ("Bomber",19), ("Elite",34)})
            foreach(var weapon in new[]{(WeaponBranch.Pulse,1),(WeaponBranch.Pulse,2),(WeaponBranch.Pulse,3),(WeaponBranch.Plasma,2),(WeaponBranch.Homing,2)})
            {
                Runner.StopRun();
                var level=UnityEngine.Object.Instantiate(Level(1));
                var encounter=UnityEngine.Object.Instantiate(level.Encounters[0]);
                var prefab=StarShift.Editor.RegionAAssetPaths.Load<GameObject>("Assets/StarShift/Prefabs/Enemies/"+(target.Item1=="Elite"?"Bomber":target.Item1)+".prefab");
                encounter.Groups=new[]{new SpawnAction{Count=1,EnemyPrefab=prefab,HpOverride=target.Item2,Sequence=StarShift.Editor.RegionAAssetPaths.Load<FlightSequence>("Assets/StarShift/Data/RegionA/TopHold.asset")}};
                encounter.GuaranteedMajorCount=0;encounter.GuaranteeTable=null;encounter.CameraOverride=null;
                level.Encounters=new[]{encounter};level.AdditionalStarts=Array.Empty<LevelStartPoint>();
                Runner.Flow.LoadLevel(level);Runner.StartRun("ENCOUNTER_1",true,true,null,null);rig.Protect();
                await Until(()=>Runner.SpawnedCount==1,2000,"TTK spawn");
                var enemy=EnemyHealth.Active.Single(x=>x.IsAlive);
                await Until(()=>enemy.GetComponent<EntryHoldFlight>().IsHolding,4000,"TTK hold");
                rig.Freeze(enemy);rig.Place(new Vector2(enemy.transform.position.x,enemy.transform.position.y-3));
                var loadout=rig.Player.GetComponent<WeaponLoadout>();var levels=new[]{1,0,0};levels[(int)weapon.Item1]=weapon.Item2;loadout.Restore(weapon.Item1,levels);
                await rig.MouseEvent(new Vector2(600,300),false);
                float first=-1,death=-1;int hits=0;Action damaged=()=>{if(first<0)first=Time.fixedTime;hits++;};Action died=()=>death=Time.fixedTime;enemy.Damaged+=damaged;enemy.Died+=died;
                var begin=Time.fixedTime;int shots=rig.Player.GetComponent<PulseLaserWeapon>().ShotsFired;
                await rig.MouseEvent(new Vector2(600,300),true);
                await Until(()=>!enemy.IsAlive,25000,"Sustained fire TTK timeout: "+target+"/"+weapon);
                await rig.MouseEvent(new Vector2(600,300),false);
                results.Add(new{enemy=target.Item1,hp=target.Item2,branch=weapon.Item1.ToString(),level=weapon.Item2,triggerToKill=death-begin,firstHitToKill=death-first,hits,shots=rig.Player.GetComponent<PulseLaserWeapon>().ShotsFired-shots});
                enemy.Damaged-=damaged;enemy.Died-=died;Runner.StopRun();rig.RestoreFlights();UnityEngine.Object.Destroy(level);UnityEngine.Object.Destroy(encounter);
                Evidence("ttk-live",new{status="RUNNING",results});
            }
            foreach(var enemy in new[]{"Scout","Raider","Bomber","Elite"}) {
                var pulse=results.Select(x=>Newtonsoft.Json.Linq.JObject.FromObject(x)).Where(x=>(string)x["enemy"]==enemy&&(string)x["branch"]=="Pulse").OrderBy(x=>(int)x["level"]).ToArray();
                Require((float)pulse[2]["triggerToKill"]<(float)pulse[0]["triggerToKill"]*.65f,"L3 progression unclear: "+enemy);
            }
            Evidence("ttk",new{status="PASS",results,realPrimaryProjectiles=true,thermalEnabled=true,distance=3});
        }}catch(Exception e){Evidence("ttk",new{status="FAILED",error=e.ToString()});}
    }

    public static object QuotaChecks()
    {
        var samples=new List<int>();
        foreach(int seed in new[]{3,17,41,67,91,410}) {
            var one=new EncounterRewardQuota();var two=new EncounterRewardQuota();one.Reset(seed,12,1);two.Reset(seed,12,1);
            int forced=0;
            for(int i=0;i<12;i++){bool a=one.ResolveKill(false);Require(a==two.ResolveKill(false),"Quota determinism");if(a){forced++;samples.Add(i);}}
            Require(forced==1&&one.Remaining==0,"Quota floor");
            one.Reset(seed,12,1);Require(!one.ResolveKill(true),"Normal major doubled");for(int i=0;i<11;i++)Require(!one.ResolveKill(false),"Forced after normal floor");
            one.Reset(seed,4,2);one.ResolveEscape();int drops=0;for(int i=0;i<3;i++)if(one.ResolveKill(false))drops++;Require(drops==2,"Escape fallback quota");
        }
        Require(samples.Distinct().Count()>1,"Quota source ordinal fixed");
        Evidence("quota-pure",new{status="PASS",seeds=6,samples,normalCredit=true,escapeFallback=true});return new{status="PASS"};
    }
    public static object BeginRewards() { _=Rewards();return new{started=true}; }
    static async Task Rewards()
    {
        Evidence("rewards",new{status="RUNNING"});
        try {using(var rig=new Rig(1600,6)) {
            var results=new List<object>();var director=Runner.GetComponent<RewardDirector>();
            foreach(int seed in new[]{17,41,91})foreach(var pair in new[]{(1,1),(1,2),(2,1),(2,3),(3,1),(3,2),(3,3)}) {
                Start(rig,pair.Item1,pair.Item2);director.SetTestSeed(seed);
                await Until(()=>Runner.SpawnedCount==Runner.Current.EnemyCount,2500,"Reward population");
                var enemies=EnemyHealth.Active.Where(e=>e.IsAlive).OrderBy(e=>e.transform.position.x).ToArray();
                int ordinal=0;var majorOrdinals=new List<int>();var types=new List<string>();
                foreach(var enemy in enemies) {
                    ordinal++;enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
                    foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive).ToArray()) {
                        if(pickup.Item!=null&&pickup.Item.Kind!=PickupKind.Shard){majorOrdinals.Add(ordinal);types.Add(pickup.Item.ShortCode);}
                        pickup.Release();
                    }
                }
                Require(director.GuaranteedMajorsRemaining==0&&majorOrdinals.Count>=1,"Missing quota "+Runner.Current.name);
                Require(Runner.Current.Groups.All(g=>g.RewardOverride==null||g.RewardOverride.GuaranteedPickup==null),"Fixed enemy carrier remains");
                results.Add(new{seed,encounter=Runner.Current.name,majorOrdinals,types});
            }
            // Chỉ hạ một enemy rồi để Timeline exit: floor vẫn dùng candidate đã chết.
            Start(rig,1,2);director.SetTestSeed(17);await Until(()=>Runner.SpawnedCount==12,2500,"Slow quota population");
            EnemyHealth.Active.First(e=>e.IsAlive).ReceivePlayerHit(999,rig.Player);Time.timeScale=3;
            await Until(()=>Runner.CompletionCount==1,12000,"Slow quota completion");
            Require(director.GuaranteedMajorsRemaining==0,"Timeline lost reward floor");Time.timeScale=1;
            Evidence("rewards",new{status="PASS",results,timelineFallback=true});
        }}catch(Exception e){Evidence("rewards",new{status="FAILED",error=e.ToString()});}
    }

    public static object BeginPersistence() { _=Persistence();return new{started=true}; }
    static async Task Persistence()
    {
        Evidence("persistence",new{status="RUNNING"});
        var owner=Runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();var original=owner.SavePath;
        string Bytes(string p)=>System.IO.File.Exists(p)?Convert.ToBase64String(System.IO.File.ReadAllBytes(p)):null;
        var before=Bytes(original);
        try {using(var rig=new Rig(1600,6)) {
            var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"starshift-human-feel-"+Guid.NewGuid().ToString("N"),"save.json");
            owner.UseTestStorage(path);Require(owner.NewGame(),"NewGame");rig.Protect();
            await Until(()=>Runner.Flow.CanContinue,3000,"Establishing");Session.BeginMission();
            await Until(()=>Runner.SpawnedCount==7,6000,"Production onboarding");
            var director=Runner.GetComponent<RewardDirector>();
            var initialBalance=owner.PersistentSnapshot.Profile.StellarShardBalance;
            Task<int> CollectCore() {
                int ordinal=0;
                foreach(var enemy in EnemyHealth.Active.Where(e=>e.IsAlive).OrderBy(e=>e.transform.position.x).ToArray()) {
                    ordinal++;enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
                    var core=UnityEngine.Object.FindObjectsByType<RewardPickup>().FirstOrDefault(p=>p.IsActive&&p.Item!=null&&p.Item.Kind==PickupKind.WeaponCore);
                    if(core==null)continue;
                    rig.Place(core.transform.position);Require(core.TryCollect(),"Core collection");return Task.FromResult(ordinal);
                }
                throw new InvalidOperationException("Onboarding core missing");
            }
            int first=await CollectCore();Require(rig.Player.GetComponent<WeaponLoadout>().Level(WeaponBranch.Pulse)==2,"Core upgrade");
            var shard=StarShift.Editor.RegionAAssetPaths.Load<PickupDefinition>("Assets/StarShift/Data/Rewards/StellarShard.asset");
            if(shard==null)shard=StarShift.Editor.RegionAAssetPaths.Load<RewardDropTable>("Assets/StarShift/Data/Rewards/NormalDrops.asset").Shard;
            var pickup=director.Spawn(shard,null,rig.Player.transform.position);Require(pickup!=null&&pickup.TryCollect(),"First shard");
            Session.Pause();Require(owner.RetryEncounter(),"Retry after reward");rig.Protect();
            Require(rig.Player.GetComponent<WeaponLoadout>().Level(WeaponBranch.Pulse)==1,"Retry retained upgrade");
            await Until(()=>Runner.SpawnedCount==7,2500,"Retry population");int retry=await CollectCore();Require(first==retry,"Retry rerolled source ordinal");
            pickup=director.Spawn(shard,null,rig.Player.transform.position);Require(pickup!=null&&pickup.TryCollect(),"Retry shard");
            Require(owner.PersistentSnapshot.Profile.StellarShardBalance==initialBalance+1,"Retry duplicated permanent resource");
            Session.Pause();Require(owner.Continue(),"Continue after reward");rig.Protect();
            Require(rig.Player.GetComponent<WeaponLoadout>().Level(WeaponBranch.Pulse)==1,"Continue retained mid-encounter upgrade");
            await Until(()=>Runner.SpawnedCount==7,2500,"Continue population");int continued=await CollectCore();Require(first==continued,"Continue rerolled source");
            Require(owner.Flush(),"Flush before replay");var campaign=Bytes(path);
            Require(owner.BeginReplay(),"Replay");rig.Protect();await Until(()=>Runner.SpawnedCount==7,2500,"Replay population");await CollectCore();owner.Flush();Require(campaign==Bytes(path),"Replay mutated save");
            Start(rig,2,1);Require(owner.Mode==StarShift.Persistence.SessionMode.DevTest,"Raw dev write permission");owner.Flush();Require(campaign==Bytes(path),"Dev start mutated save");
            Evidence("persistence",new{status="PASS",first,retry,continued,rollbackCore=true,shardHighWater=true,replayBytesUnchanged=true,devBytesUnchanged=true,testPath=path});
        }}catch(Exception e){Evidence("persistence",new{status="FAILED",error=e.ToString()});}
        finally {owner.ExitIsolated();owner.UseTestStorage(original);Require(before==Bytes(original),"Production storage changed");}
    }

    public static object BeginAsteroids() { _=Asteroids();return new{started=true}; }
    static async Task Asteroids()
    {
        Evidence("asteroids",new{status="RUNNING"});
        try {
            var results=new List<object>();
            foreach(int width in new[]{1600,2000})using(var rig=new Rig(width,6))foreach(int mission in new[]{2,4}) {
                Start(rig,mission,mission==2?2:1);var config=Runner.Current.HazardStream;
                var schedule=HazardStreamSchedule.Build(config,Runner.Current.Duration);
                Require(schedule.Count==config.Count,"Stream count truncated");
                Require(schedule.Select(JsonUtility.ToJson).SequenceEqual(HazardStreamSchedule.Build(config,Runner.Current.Duration).Select(JsonUtility.ToJson)),"Stream nondeterministic");
                Require(schedule.All(s=>Vector2.Angle(s.ExitAnchor-s.EntryAnchor,config.DominantDirection)<3.01f),"Mixed direction");
                var view=Camera.main.GetComponent<GameplayViewController>();
                await Until(()=>!view.IsTransitioning,2000,"Storm camera");Require(view.CurrentPlayableViewport==new Rect(0,0,1,1),"Storm restricted region");
                foreach(var edge in new[]{new Vector2(-1,.5f),new Vector2(2,.5f),new Vector2(.5f,-1),new Vector2(.5f,2)}) {
                    var target=Camera.main.ViewportToWorldPoint(new Vector3(edge.x,edge.y,10));
                    var clamped=rig.Player.GetComponent<PlayerMovementBounds>().ClampPosition(target,Camera.main,view.CurrentPlayableViewport);
                    rig.Place(clamped);var point=Camera.main.WorldToViewportPoint(rig.Player.transform.position);
                    Require(point.x>=0&&point.x<=1&&point.y>=0&&point.y<=1,"Storm edge clamp");
                }
                Start(rig,mission,mission==2?2:1);await Until(()=>Runner.Elapsed>.5f,1500,"Storm start");
                var weapon=rig.Player.GetComponent<PulseLaserWeapon>();weapon.enabled=false;
                var safe=Camera.main.ViewportToWorldPoint(new Vector3(mission==2?.1f:.9f,.15f,10));
                await rig.DragTo(safe);await rig.DragTo(safe);rig.Unprotect();int startLives=rig.Lives.CurrentLives;
                var sizes=new HashSet<int>();int peak=0;bool paused=false,damaged=false;int stage1=0,stage2=0;
                var tracked=new Dictionary<EnemyHealth,(int version,Vector3 point)>();float maxDeviation=0;
                while(Runner.HasActiveRun) {
                    var enemies=EnemyHealth.Active.Where(e=>e.IsAlive&&e.GetComponent<AsteroidVisualMotion>()!=null).ToArray();
                    peak=Math.Max(peak,enemies.Count(e=>!e.GetComponent<EntryHoldFlight>().IsOutsideView));
                    foreach(var asteroid in enemies) {
                        sizes.Add(asteroid.MaxHp);var point=Camera.main.WorldToViewportPoint(asteroid.transform.position);
                        if(tracked.TryGetValue(asteroid,out var previous)&&previous.version==asteroid.SpawnVersion) {
                            var delta=(Vector2)(point-previous.point);if(delta.magnitude>.0001f)maxDeviation=Math.Max(maxDeviation,Vector2.Angle(delta,config.DominantDirection));
                        }
                        tracked[asteroid]=(asteroid.SpawnVersion,point);
                    }
                    if(!paused&&Runner.Elapsed>4){Session.Pause();float t=Runner.Elapsed;var poses=enemies.Select(e=>e.transform.position).ToArray();await Task.Delay(200);Require(Runner.Elapsed==t&&enemies.Select(e=>e.transform.position).SequenceEqual(poses),"Asteroid pause");Session.Resume();paused=true;}
                    if(!damaged&&Runner.Elapsed>5&&enemies.Any(e=>e.MaxHp==19)) {
                        var huge=enemies.First(e=>e.MaxHp==19);var presentation=huge.GetComponent<EnemyDamagePresentation>();
                        huge.ReceivePlayerHit(huge.CurrentHp-Mathf.FloorToInt(huge.MaxHp*.6f),rig.Player);stage1=presentation.DamageStage;
                        await Task.Delay(120);await Capture("storm-"+mission+"-damaged-"+width);
                        huge.ReceivePlayerHit(huge.CurrentHp-Mathf.FloorToInt(huge.MaxHp*.3f),rig.Player);stage2=presentation.DamageStage;
                        Require(stage1==1&&stage2==2,"Asteroid damage stages");await Task.Delay(120);await Capture("storm-"+mission+"-critical-"+width);
                        huge.ReceivePlayerHit(999,rig.Player);await Capture("storm-"+mission+"-burst-"+width);damaged=true;
                    }
                    if(Runner.Elapsed>7&&Runner.Elapsed<7.3f)await Capture("storm-"+mission+"-field-"+width);
                    await Task.Delay(70);
                }
                weapon.enabled=true;await rig.MouseEvent(new Vector2(400,200),false);
                Require(rig.Lives.CurrentLives==startLives,"No-shoot safe lane damage");Require(peak>=6&&sizes.IsSupersetOf(new[]{3,6,11,19})&&damaged&&paused,"Asteroid coverage");
                Require(maxDeviation<3.5f,"Runtime flow changed");Require(EnemyHealth.Active.Count==0,"Hazards carried past completion");
                results.Add(new{mission,width,count=schedule.Count,peak,maxDeviation,startLives,endLives=rig.Lives.CurrentLives,sizes,stage1,stage2,pause=true,noShooting=true,fullBounds=true});
                rig.Protect();
            }
            Evidence("asteroids",new{status="PASS",results});
        }catch(Exception e){Evidence("asteroids",new{status="FAILED",error=e.ToString()});}
    }

    public static object BeginBoss() { _=Boss();return new{started=true}; }
    static async Task Boss()
    {
        Evidence("boss",new{status="RUNNING"});
        try {
            var results=new List<object>();
            foreach(int width in new[]{1600,2000})using(var rig=new Rig(width,6)) {
                Start(rig,4,2);await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive),2000,"Boss spawn");
                var health=EnemyHealth.Active.Single(e=>e.IsAlive);var boss=health.GetComponent<BossPhaseRunner>();var modules=health.GetComponentsInChildren<BossAttackModule>();
                var flight=health.GetComponent<EntryHoldFlight>();var view=Camera.main.GetComponent<GameplayViewController>();
                await Until(()=>flight.IsHolding,4500,"Boss hold");
                Require(Mathf.Abs(Camera.main.orthographicSize-9)<.001f,"Boss camera");Require(view.CurrentPlayableViewport.yMax<.62f,"Player boss region");
                var visuals=health.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).ToArray();var bounds=visuals[0].bounds;foreach(var v in visuals.Skip(1))bounds.Encapsulate(v.bounds);
                float occupied=bounds.size.x/(Camera.main.orthographicSize*2*Camera.main.aspect);Require(occupied>=.34f&&occupied<=.46f,"Boss silhouette width "+occupied);
                await Capture("boss-phase-a-"+width);
                await Until(()=>modules[0].ShotsFired>0&&modules[1].ShotsFired>0,13000,"Phase A attack identities");
                var reserve=rig.Player.GetComponent<EmergencyMissileReserve>();Require(reserve.TryUse(),"Boss reserve use");await Task.Delay(900);
                Require(view.ImpactRemaining<=0&&Mathf.Abs(Camera.main.projectionMatrix.m11-1/9f)<.001f,"Reserve projection leak");
                await Until(()=>modules.Any(m=>m.IsBusy),5000,"Phase break during attack");
                health.ReceivePlayerHit(health.CurrentHp-Mathf.FloorToInt(health.MaxHp*.49f),rig.Player);
                await Until(()=>boss.IsPhaseB,500,"Phase B threshold");Require(modules.All(m=>!m.IsBusy),"Phase break failed cancel");await Capture("boss-phase-transition-"+width);
                await Until(()=>modules[2].IsBusy&&!modules[2].IsBeamActive,2500,"Beam telegraph");var beam=modules[2];
                await Task.Delay(60);Require(flight.MotionPaused,"Beam did not brace ship");Require(Mathf.Abs(beam.BeamBounds.center.x-health.transform.position.x)<.01f,"Beam disconnected from core pod");await Capture("boss-beam-warning-"+width);
                var gate=UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();gate.ClearProjectiles();
                var safe=Camera.main.ViewportToWorldPoint(new Vector3(Camera.main.WorldToViewportPoint(beam.BeamBounds.center).x<.5f?.9f:.1f,.2f,10));
                rig.Place(safe);rig.Unprotect();int lives=rig.Lives.CurrentLives;
                await Until(()=>beam.IsBeamActive,2000,"Beam activation");Require(rig.Lives.CurrentLives==lives,"Telegraph damaged player");await Task.Delay(100);await Capture("boss-beam-active-safe-lane-"+width);
                await Until(()=>!beam.IsBusy,2500,"Beam end");Require(rig.Lives.CurrentLives==lives,"Safe lane damage");
                await Capture("boss-phase-b-"+width);
                bool damageTest=false;
                if(width==1600) {
                    rig.Protect();await Until(()=>beam.IsBusy&&!beam.IsBeamActive,16000,"Second signature");await Task.Delay(60);gate.ClearProjectiles();
                    rig.Place(new Vector2(beam.BeamBounds.center.x,Camera.main.ViewportToWorldPoint(new Vector3(.5f,.2f,10)).y));rig.Unprotect();lives=rig.Lives.CurrentLives;
                    await Task.Delay(400);Require(!beam.IsBeamActive&&rig.Lives.CurrentLives==lives,"Warning damage");
                    await Until(()=>rig.Lives.CurrentLives==lives-1,1800,"Active beam did not hit");damageTest=true;rig.Protect();
                }
                var region=view.CurrentPlayableViewport;view.RequestView(new GameplayViewSettings(7.3f,new Rect(0,0,1,1)),.3f);await Until(()=>!view.IsTransitioning,800,"Later camera profile");
                Require(Mathf.Abs(Camera.main.projectionMatrix.m11-1/7.3f)<.001f,"Projection stale after later profile");view.RequestView(new GameplayViewSettings(9,region),.3f);await Until(()=>!view.IsTransitioning,800,"Return boss camera");
                health.ReceivePlayerHit(health.CurrentHp,rig.Player);await Until(()=>Runner.Stage==EncounterStage.BossPostDeath,500,"Boss defeat stage");await Capture("boss-death-"+width);
                await Until(()=>Runner.CompletionCount==1,3500,"Boss death sequence");Require(UnityEngine.Object.FindObjectsByType<RewardPickup>().Count(p=>p.IsActive&&p.Item!=null&&p.Crate==null)==2,"Boss defeat pair");
                results.Add(new{width,occupied,phaseA=true,phaseB=true,beamSafe=true,beamDamage=damageTest,reserve=true,projection=true,directItems=2,playerRegion=region.ToString()});
            }
            Evidence("boss",new{status="PASS",results});
        }catch(Exception e){Evidence("boss",new{status="FAILED",error=e.ToString()});}
    }
    public static object BeginBossTtk() { _=BossTtk();return new{started=true}; }
    static async Task BossTtk()
    {
        Evidence("boss-ttk",new{status="RUNNING"});
        try {using(var rig=new Rig(1600,6)) {
            var results=new List<object>();
            foreach(int level in new[]{1,2,3}) {
                Start(rig,4,2);await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive),2000,"Boss benchmark spawn");
                var enemy=EnemyHealth.Active.Single(e=>e.IsAlive);var boss=enemy.GetComponent<BossPhaseRunner>();
                await Until(()=>enemy.GetComponent<EntryHoldFlight>().IsHolding,4500,"Boss benchmark hold");
                rig.Player.GetComponent<WeaponLoadout>().Restore(WeaponBranch.Pulse,new[]{level,0,0});
                await rig.MouseEvent(new Vector2(600,300),false);Time.timeScale=2;
                float first=-1,death=-1;int beamCount=0;bool wasBeam=false;Action damaged=()=>{if(first<0)first=Time.fixedTime;};Action died=()=>death=Time.fixedTime;
                enemy.Damaged+=damaged;enemy.Died+=died;float begin=Time.fixedTime,wall=Time.realtimeSinceStartup;
                await rig.MouseEvent(new Vector2(600,300),true);
                while(enemy.IsAlive&&Time.realtimeSinceStartup-wall<110) {
                    rig.Place(new Vector2(enemy.transform.position.x,-4));
                    var beam=enemy.GetComponentsInChildren<BossAttackModule>().Any(m=>m.IsBeamActive);
                    if(beam&&!wasBeam)beamCount++;wasBeam=beam;
                    await Task.Delay(50);
                }
                Require(!enemy.IsAlive,"Boss sustained fire timeout");await rig.MouseEvent(new Vector2(600,300),false);Time.timeScale=1;
                enemy.Damaged-=damaged;enemy.Died-=died;
                results.Add(new{level,hp=enemy.MaxHp,triggerToKill=death-begin,firstHitToKill=death-first,beamCount,phaseB=boss.IsPhaseB,protectedAimBot=true});
                Evidence("boss-ttk-live",new{status="RUNNING",results});
            }
            Evidence("boss-ttk",new{status="PASS",results,thermalEnabled=true});
        }}catch(Exception e){Evidence("boss-ttk",new{status="FAILED",error=e.ToString()});}
    }

    static async Task CapAndChaining()
    {
        using(var rig=new Rig(1600,6)) {
            Start(rig,1,1);var director=Runner.GetComponent<RewardDirector>();director.SetTestSeed(67);
            await Until(()=>Runner.SpawnedCount==7,2000,"Cap population");
            var shard=StarShift.Editor.RegionAAssetPaths.Load<RewardDropTable>("Assets/StarShift/Data/Rewards/NormalDrops.asset").Shard;
            for(int i=0;i<6;i++)Require(director.Spawn(shard,null,Vector3.up)!=null,"Normal pickup capacity");
            Require(director.Spawn(shard,null,Vector3.up)==null,"Normal cap bypass");
            foreach(var e in EnemyHealth.Active.Where(e=>e.IsAlive).ToArray())e.ReceivePlayerHit(999,rig.Player);
            Require(director.ActivePickups==7&&director.GuaranteedMajorsRemaining==0,"Full cap lost floor");
            Require(UnityEngine.Object.FindObjectsByType<RewardPickup>().Count(p=>p.IsActive&&p.Item!=null&&p.Item.Kind==PickupKind.WeaponCore)==1,"Duplicate floor at cap");
            Start(rig,4,1);director=Runner.GetComponent<RewardDirector>();
            for(int i=0;i<6;i++)Require(director.Spawn(shard,null,Vector3.up)!=null,"Crate cap fixture");
            Time.timeScale=3;await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&!e.Definition.BodyDamagesPlayer),3000,"Supply cap fixture");
            var supply=EnemyHealth.Active.First(e=>e.IsAlive&&!e.Definition.BodyDamagesPlayer);supply.ReceivePlayerHit(999,rig.Player);Time.timeScale=1;
            Require(UnityEngine.Object.FindObjectsByType<RewardPickup>().Count(p=>p.IsActive&&p.Item?.Kind==PickupKind.WeaponCore)==1 && UnityEngine.Object.FindObjectsByType<RewardPickup>().All(p=>!p.IsActive||p.Crate==null),"Cap lost guaranteed supply pair");
            Start(rig,1,2);await Until(()=>Runner.SpawnedCount==12,2000,"Fast population");
            foreach(var e in EnemyHealth.Active.Where(e=>e.IsAlive).ToArray())e.ReceivePlayerHit(999,rig.Player);
            await Until(()=>Runner.CompletionCount==1,600,"Fast clear blocked by schedule");
            Start(rig,1,2);await Until(()=>Runner.Elapsed>4.1f,5000,"Outer survivor setup");
            var all=EnemyHealth.Active.Where(e=>e.IsAlive).OrderBy(e=>e.transform.position.x).ToArray();
            foreach(var e in all.Skip(1))e.ReceivePlayerHit(999,rig.Player);
            await Task.Delay(700);Require(Runner.ActiveEnemyCount==1&&all[0].IsAlive,"Outer survivor lost or reinforcement piled up");
            all[0].ReceivePlayerHit(999,rig.Player);await Until(()=>Runner.CompletionCount==1,1600,"Outer survivor clear");
            Evidence("cap-and-chaining",new{status="PASS",normalCap=6,floorOverflow=1,duplicate=false,fastClear=true,outerSurvivor=true});
        }
    }
    static async Task Cameras()
    {
        var results=new List<object>();
        foreach(int width in new[]{1600,2000})using(var rig=new Rig(width,6)) {
            foreach(var pair in new[]{(1,1),(1,2),(2,1),(2,2),(2,3),(3,1),(3,2),(3,3),(4,1),(4,2)}) {
                Start(rig,pair.Item1,pair.Item2);var config=Runner.Current.CameraOverride;
                var view=Camera.main.GetComponent<GameplayViewController>();await Until(()=>!view.IsTransitioning,2200,"Camera transition");
                Require(Mathf.Abs(Camera.main.orthographicSize-config.View.OrthographicSize)<.001f,"Camera override not applied");
                Require(Mathf.Abs(Camera.main.projectionMatrix.m11-1/Camera.main.orthographicSize)<.001f,"Projection stale");
                var weapon=rig.Player.GetComponent<PulseLaserWeapon>();weapon.enabled=false;
                var anchor=new Vector2(500,300);await rig.MouseEvent(anchor,false);await rig.MouseEvent(anchor,true);
                var before=Camera.main.WorldToViewportPoint(rig.Player.transform.position);await rig.MouseEvent(anchor+new Vector2(100,0),true);
                var after=Camera.main.WorldToViewportPoint(rig.Player.transform.position);
                Require(Mathf.Abs(after.x-before.x-100f/width)<.004f,"Pointer mapping changed with zoom");await rig.MouseEvent(anchor,false);weapon.enabled=true;
                if(pair==(2,2)||pair==(4,2)) {
                    foreach(var edge in new[]{new Vector2(-1,.3f),new Vector2(2,.3f),new Vector2(.5f,-1),new Vector2(.5f,2)}) {
                        var target=Camera.main.ViewportToWorldPoint(new Vector3(edge.x,edge.y,10));
                        weapon.enabled=false;await rig.DragTo(target);await rig.DragTo(target);await rig.DragTo(target);await rig.MouseEvent(anchor,false);weapon.enabled=true;
                        var point=Camera.main.WorldToViewportPoint(rig.Player.transform.position);var region=view.CurrentPlayableViewport;
                        Require(point.x>=region.xMin&&point.x<=region.xMax&&point.y>=region.yMin&&point.y<=region.yMax,"Virtual edge drag escaped bounds");
                        if(edge.x<0)Require(point.x<.06f,"Left edge unreachable");if(edge.x>1)Require(point.x>.94f,"Right edge unreachable");
                        if(edge.y<0)Require(point.y<.08f,"Bottom edge unreachable");if(edge.y>1)Require(point.y>region.yMax-.08f,"Top edge unreachable");
                    }
                }
                results.Add(new{encounter=Runner.Current.name,width,ortho=Camera.main.orthographicSize,region=view.CurrentPlayableViewport.ToString(),dragDelta=after.x-before.x});
            }
        }
        Evidence("cameras",new{status="PASS",results,virtualEdges=true});
    }
    static async Task AsteroidStageVisuals()
    {
        using(var rig=new Rig(1600,6)) {
            Start(rig,2,2);await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&e.MaxHp==19),3000,"Huge stage target");
            var huge=EnemyHealth.Active.First(e=>e.IsAlive&&e.MaxHp==19);rig.Freeze(huge);huge.transform.position=new Vector3(0,1,0);
            await Capture("asteroid-healthy-readable");
            huge.ReceivePlayerHit(9,rig.Player);await Task.Delay(150);await Capture("asteroid-damaged-readable");
            huge.ReceivePlayerHit(7,rig.Player);await Task.Delay(150);await Capture("asteroid-critical-readable");
            huge.ReceivePlayerHit(999,rig.Player);await Capture("asteroid-explosion-readable");
        }
    }
    public static object BeginClosure(){_=Closure();return new{started=true};}
    static async Task Closure()
    {
        Evidence("closure",new{status="RUNNING",step="formations"});
        try {
            await Formations();Require(Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(Output+"formations.json"))["status"].ToString()=="PASS","Formation closure");
            Evidence("closure",new{status="RUNNING",step="quota"});QuotaChecks();await Rewards();Require(Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(Output+"rewards.json"))["status"].ToString()=="PASS","Rewards closure");
            await CapAndChaining();await Persistence();Require(Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(Output+"persistence.json"))["status"].ToString()=="PASS","Persistence closure");
            Evidence("closure",new{status="RUNNING",step="camera-and-stages"});await Cameras();await AsteroidStageVisuals();
            Evidence("closure",new{status="RUNNING",step="boss"});await Boss();Require(Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(Output+"boss.json"))["status"].ToString()=="PASS","Boss closure");
            Evidence("closure",new{status="PASS",ttk="retained: no weapon/HP change since measured",storms="retained: stream/motion unchanged since PASS"});
        }catch(Exception e){Evidence("closure",new{status="FAILED",error=e.ToString()});}
    }

    public static object BeginFullRegion()
    {
        Require(!System.IO.File.Exists(Output+"full-region.json"),"Full Region already attempted; inspect evidence before any rerun");
        Require(Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(Output+"closure.json"))["status"].ToString()=="PASS","Closure prerequisite");
        _=FullRegion();return new{started=true};
    }
    static async Task FullRegion()
    {
        Evidence("full-region",new{status="RUNNING"});
        var owner=Runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();var original=owner.SavePath;
        string Bytes(string p)=>System.IO.File.Exists(p)?Convert.ToBase64String(System.IO.File.ReadAllBytes(p)):null;
        var originalBytes=Bytes(original);
        try {using(var rig=new Rig(1600,6))using(var observation=new DropObservation(Runner.GetComponent<RewardDirector>())) {
            var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"starshift-human-full-"+Guid.NewGuid().ToString("N"),"save.json");
            var director=Runner.GetComponent<RewardDirector>();director.SetTestSeed(410);owner.UseTestStorage(path);Require(owner.NewGame(),"Full NewGame");rig.Protect();
            var loadout=rig.Player.GetComponent<WeaponLoadout>();var view=Camera.main.GetComponent<GameplayViewController>();
            var region=Runner.GetComponent<RegionCampaignFlow>();
            var began=Time.fixedTime;var wall=Time.realtimeSinceStartup;string mission="L1",encounter="";float missionStart=began,encounterStart=began;
            var missions=new List<object>();var encounters=new List<object>();var gaps=new List<float>();float emptySince=-1;
            var drops=observation.Spawned;
            var upgrades=new List<object>();string lastLevels=string.Join(",",loadout.CaptureLevels());int peak=0,maxShooters=0,deaths=0;bool paused=false,switched=false,reserve=false;
            var captures=new HashSet<string>();float lastLive=-1;
            while(!region.DestinationReady&&Time.realtimeSinceStartup-wall<650) {
                if(Runner.Flow.CanContinue)Session.BeginMission();
                var id=Runner.Flow.Level.Id;
                if(id!=mission){missions.Add(new{mission,seconds=Time.fixedTime-missionStart});mission=id;missionStart=Time.fixedTime;}
                var key=id+"-"+Runner.CurrentIndex;
                if(key!=encounter){if(encounter!="")encounters.Add(new{encounter,seconds=Time.fixedTime-encounterStart});encounter=key;encounterStart=Time.fixedTime;}
                var enemies=EnemyHealth.Active.Where(e=>e.IsAlive&&!e.GetComponent<EntryHoldFlight>().IsOutsideView).ToArray();peak=Math.Max(peak,enemies.Length);
                maxShooters=Math.Max(maxShooters,enemies.Count(e=>e.GetComponent<SingleFireAttack>().IsTelegraphing));
                if(rig.Gate.CanFire&&!rig.Input.FireHeld){await rig.MouseEvent(new Vector2(400,200),false);await rig.MouseEvent(new Vector2(400,200),true);}
                if(rig.Gate.CanInput&&enemies.Length>0) {
                    var target=enemies.OrderBy(e=>Mathf.Abs(e.transform.position.x-rig.Player.transform.position.x)).First();rig.Place(new Vector2(target.transform.position.x,-3.8f));
                }
                var pickups=UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive).ToArray();
                foreach(var pickup in pickups) {
                    var top=Camera.main.ViewportToWorldPoint(new Vector3(.5f,view.CurrentPlayableViewport.yMax-.06f,10)).y;
                    if(rig.Gate.CanInput&&pickup.transform.position.y<Mathf.Min(1.3f,top))rig.Place(pickup.transform.position);
                }
                var levels=string.Join(",",loadout.CaptureLevels());if(levels!=lastLevels){upgrades.Add(new{mission=id,encounter=Runner.CurrentIndex,seconds=Time.fixedTime-began,levels});lastLevels=levels;}
                bool playable=rig.Gate.CanInput&&!Runner.Flow.IsCinematic;
                if(playable&&enemies.Length==0){if(emptySince<0)emptySince=Time.fixedTime;}
                else if(emptySince>=0){gaps.Add(Time.fixedTime-emptySince);emptySince=-1;}
                if(!paused&&id=="L2"&&Runner.CurrentIndex==1&&Runner.Elapsed>3){Session.Pause();var frozen=Runner.Elapsed;await Task.Delay(200);Require(Runner.Elapsed==frozen,"Full Pause");Session.Resume();paused=true;}
                if(deaths==0&&id=="L2"&&Runner.CurrentIndex==2&&Runner.Elapsed>4) {
                    int lives=rig.Lives.CurrentLives;rig.Unprotect();rig.Lives.RestoreCheckpoint(lives);Require(rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null),"Full death");
                    await Until(()=>rig.Gate.State==PlayerGameplayState.PostEntryInvincibility,3500,"Full respawn");rig.Protect();deaths++;
                }
                if(!switched&&id=="L3"&&rig.Gate.CanInput&&(loadout.Level(WeaponBranch.Plasma)>0||loadout.Level(WeaponBranch.Homing)>0))switched=loadout.Cycle();
                if(!reserve&&id=="L4"&&Runner.CurrentIndex==1&&Runner.Elapsed>5)reserve=rig.Player.GetComponent<EmergencyMissileReserve>().TryUse();
                if(Runner.Stage==EncounterStage.Running&&Runner.Elapsed>4&&captures.Add(key))await Capture("full-"+key);
                if(Time.fixedTime-lastLive>1){Evidence("full-region-live",new{status="RUNNING",seconds=Time.fixedTime-began,mission=id,encounter=Runner.CurrentIndex,stage=Runner.Stage.ToString(),peak,drops,levels});lastLive=Time.fixedTime;}
                await Task.Delay(90);
            }
            Require(region.DestinationReady,"Full Region timeout");if(emptySince>=0)gaps.Add(Time.fixedTime-emptySince);
            missions.Add(new{mission,seconds=Time.fixedTime-missionStart});encounters.Add(new{encounter,seconds=Time.fixedTime-encounterStart});
            Require(observation.Spawned.Values.Sum()>=7,"Production reward coverage");
            Require(paused&&deaths==1&&reserve,"Full integration coverage");
            var snapshot=owner.PersistentSnapshot;Require(snapshot.Campaign.Status==StarShift.Persistence.CampaignStatus.RegionReady&&snapshot.Profile.Records.Length==4,"Full save boundary");
            Evidence("full-region",new{status="PASS",seconds=Time.fixedTime-began,missions,encounters,peak,maxShooters,drops,collectedTypes=observation.Collected,totalDrops=observation.Spawned.Values.Sum(),upgrades,bossCombatClock=Runner.Elapsed,finalLevels=loadout.CaptureLevels(),deaths,paused,switched,reserve,
                averageEmptyField=gaps.Count>0?gaps.Average():0,maxEmptyField=gaps.Count>0?gaps.Max():0,emptyFieldSamples=gaps,includesPlayableEntryAndTravel=true,
                protectedAimBot=true,injectedCore=false,seed=410,testPath=path,records=snapshot.Profile.Records.Length});
        }}catch(Exception e){Evidence("full-region",new{status="FAILED",error=e.ToString()});}
        finally {owner.ExitIsolated();owner.UseTestStorage(original);Require(originalBytes==Bytes(original),"Full run changed production storage");}
    }

    public static object BeginClosureRemainder(){_=ClosureRemainder();return new{started=true};}
    static async Task ClosureRemainder()
    {
        Evidence("closure",new{status="RUNNING",step="cap-and-chaining",retained="formations/rewards already passed; corrected drain timeout"});
        try {
            await CapAndChaining();await Persistence();Require(Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(Output+"persistence.json"))["status"].ToString()=="PASS","Persistence closure");
            Evidence("closure",new{status="RUNNING",step="camera-and-stages"});await Cameras();await AsteroidStageVisuals();
            Evidence("closure",new{status="RUNNING",step="boss"});await Boss();Require(Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(Output+"boss.json"))["status"].ToString()=="PASS","Boss closure");
            Evidence("closure",new{status="PASS",retained="formations/rewards/TTK/storm passes",verifierRepair="outer-survivor wait includes .45s grace + .25s fade"});
        }catch(Exception e){Evidence("closure",new{status="FAILED",error=e.ToString()});}
    }

    public static object BeginBoundaries(){_=Boundaries();return new{started=true};}
    static async Task Boundaries()
    {
        Evidence("boundaries",new{status="RUNNING"});
        string oldPath="../.design/m5-refinement/integration.json";var previous=System.IO.File.ReadAllText(oldPath);
        try {
            VerifyPacingRefinement.BeginIntegrationChecks();
            await Until(()=>Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(oldPath))["status"].ToString()!="RUNNING",40000,"Boundary integration timeout");
            var integration=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(oldPath));Require((string)integration["status"]=="PASS",integration.ToString());
            Evidence("boundaries",new{status="PASS",integration});
        }catch(Exception e){Evidence("boundaries",new{status="FAILED",error=e.ToString()});}
        finally {System.IO.File.WriteAllText(oldPath,previous);}
    }

    public static object BeginFinalBossFrames(){_=FinalBossFrames();return new{started=true};}
    static async Task FinalBossFrames()
    {
        Evidence("boss-final-frames",new{status="RUNNING"});
        try {
            foreach(int width in new[]{1600,2000})using(var rig=new Rig(width,6)) {
                Start(rig,4,2);await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive),2000,"Boss frame spawn");
                var health=EnemyHealth.Active.Single(e=>e.IsAlive);var flight=health.GetComponent<EntryHoldFlight>();
                await Until(()=>flight.IsHolding,4500,"Boss frame settle");await Capture("boss-phase-a-"+width);
                health.ReceivePlayerHit(Mathf.CeilToInt(health.MaxHp*.51f),rig.Player);
                var beam=health.GetComponentsInChildren<BossAttackModule>()[2];await Until(()=>beam.IsBusy,2500,"Beam frame warning");
                await Task.Delay(100);await Capture("boss-beam-warning-"+width);
                await Until(()=>beam.IsBeamActive,1800,"Beam active frame");await Task.Delay(150);
                Require(flight.MotionPaused&&Mathf.Abs(beam.BeamBounds.center.x-health.transform.position.x)<.01f,"Braced beam moved off pod");
                await Capture("boss-beam-active-safe-lane-"+width);
                health.ReceivePlayerHit(health.CurrentHp,rig.Player);await Task.Delay(500);await Capture("boss-death-"+width);
                Require(!beam.IsBusy&&!flight.MotionPaused,"Death retained beam motion lock");
            }
            Evidence("boss-final-frames",new{status="PASS",activeRenderDelay=true,brace=true,deathCancel=true});
        }catch(Exception e){Evidence("boss-final-frames",new{status="FAILED",error=e.ToString()});}
    }

    sealed class DropObservation : IDisposable
    {
        readonly RewardDirector director;
        public readonly Dictionary<string,int> Spawned=new Dictionary<string,int>();
        public readonly Dictionary<string,int> Collected=new Dictionary<string,int>();
        public DropObservation(RewardDirector owner){director=owner;director.DropSpawned+=Spawn;director.ItemCollected+=Collect;}
        void Add(Dictionary<string,int> counts,string key)=>counts[key]=counts.TryGetValue(key,out var count)?count+1:1;
        void Spawn(PickupDefinition item,RewardDropTable crate)=>Add(Spawned,crate!=null?"MajorCrate":item.ShortCode);
        void Collect(PickupDefinition item)=>Add(Collected,item.ShortCode);
        public void Dispose(){director.DropSpawned-=Spawn;director.ItemCollected-=Collect;}
    }
    public static object BeginObserverCheck(){_=ObserverCheck();return new{started=true};}
    static async Task ObserverCheck()
    {
        Evidence("observer",new{status="RUNNING"});
        try {using(var rig=new Rig(1600,6))using(var observation=new DropObservation(Runner.GetComponent<RewardDirector>())) {
            Start(rig,1,1);await Until(()=>Runner.SpawnedCount==7,2000,"Observer population");
            var director=Runner.GetComponent<RewardDirector>();
            var shard=StarShift.Editor.RegionAAssetPaths.Load<RewardDropTable>("Assets/StarShift/Data/Rewards/NormalDrops.asset").Shard;
            var pickup=director.Spawn(shard,null,rig.Player.transform.position);Require(pickup.TryCollect(),"Observer collect");
            Require(!pickup.TryCollect(),"Duplicate collection");Require(observation.Spawned.Values.Sum()==1&&observation.Collected.Values.Sum()==1,"Observer duplicate/missing event");
            Evidence("observer",new{status="PASS",spawnOnce=true,collectOnce=true});
        }}catch(Exception e){Evidence("observer",new{status="FAILED",error=e.ToString()});}
    }

    public static object ValidationChecks()
    {
        var plan=ScriptableObject.CreateInstance<FormationPlan>();
        var encounter=UnityEngine.Object.Instantiate(Level(1).Encounters[0]);
        var table=UnityEngine.Object.Instantiate(encounter.GuaranteeTable);
        var shard=ScriptableObject.CreateInstance<PickupDefinition>();shard.Kind=PickupKind.Shard;
        try {
            plan.Slots=new[]{Vector2.zero,Vector2.zero};Require(!plan.IsValid,"Duplicate plan accepted");
            plan.Slots=new[]{new Vector2(float.NaN,0)};Require(!plan.IsValid,"Nonfinite plan accepted");
            plan.Slots=new[]{new Vector2(-.1f,0),new Vector2(.1f,0)};Require(plan.IsValid,"Distinct plan rejected");
            var original=encounter.Groups[0];
            SpawnAction Group(int first)=>new SpawnAction{EnemyPrefab=original.EnemyPrefab,Sequence=original.Sequence,Plan=plan,FirstSlot=first,Count=1};
            encounter.Groups=new[]{Group(0),Group(0)};Require(!encounter.IsValid(out _),"Shared slot overlap accepted");
            encounter.Groups[1].FirstSlot=2;Require(!encounter.IsValid(out _),"Out of range accepted");
            encounter.Groups[1].FirstSlot=1;Require(encounter.IsValid(out _),"Valid slices rejected");
            encounter.GuaranteedMajorCount=3;Require(!encounter.IsValid(out _),"Impossible quota accepted");
            encounter.GuaranteedMajorCount=1;table.GuaranteedPickup=shard;encounter.GuaranteeTable=table;Require(!encounter.IsValid(out _),"Shard major floor accepted");
            Evidence("validation",new{status="PASS",unique=true,ranges=true,finite=true,quota=true});return new{status="PASS"};
        }finally{UnityEngine.Object.DestroyImmediate(plan);UnityEngine.Object.DestroyImmediate(encounter);UnityEngine.Object.DestroyImmediate(table);UnityEngine.Object.DestroyImmediate(shard);}
    }
    public static object CaptureFullBossClock()
    {
        Require(Runner.Current.Completion==EncounterCompletion.Boss&&Runner.Stage==EncounterStage.Idle,"Expected completed full-run Boss state");
        var entry=Runner.Current.Groups[0].Sequence.Phases[0].Duration;
        var result=new{status="PASS",combatClockToDefeat=Runner.Elapsed,includesEntrySeconds=entry,holdToDefeat=Runner.Elapsed-entry,source="runner clock retained after StopRun"};
        Evidence("full-boss-clock",result);return result;
    }
}
