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
using UnityEditor;
using UnityEngine;
using Rig = VerifyPacingRefinement.Rig;

public static class VerifyRegionB
{
    const string EvidenceFolder="../.design/m6/";
    static EncounterRunner Runner=>UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession Session=>Runner.GetComponent<GameplaySession>();
    static CampaignLifecycle Save=>Runner.GetComponent<CampaignLifecycle>();
    static RegionCampaignFlow Region=>Runner.GetComponent<RegionCampaignFlow>();
    public static Task CurrentTask { get; private set; }
    static void Check(bool c,string m){if(!c)throw new InvalidOperationException(m);}
    static void Evidence(string name,object o){Directory.CreateDirectory(EvidenceFolder);File.WriteAllText(EvidenceFolder+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(o,Newtonsoft.Json.Formatting.Indented));}
    static async Task Until(Func<bool> condition,int ms,string message)
    {var end=Time.realtimeSinceStartup+ms/1000f;while(!condition()&&Time.realtimeSinceStartup<end)await Task.Delay(25);Check(condition(),message);}
    static async Task Capture(string name)
    {var player=GameObject.Find("Player").GetComponent<PlayerDamagePresentation>();var enabled=player.enabled;player.enabled=false;
        Directory.CreateDirectory(EvidenceFolder+"screenshots");ScreenCapture.CaptureScreenshot(Path.GetFullPath(EvidenceFolder+"screenshots/"+name+".png"));await Task.Delay(120);player.enabled=enabled;}
    static void Start(Rig rig,int mission,int encounter=0,bool standalone=true)
    {
        Runner.StopRun();Runner.Flow.LoadLevel(Region.FindMission("L"+mission));Runner.StartRun("ENCOUNTER_"+(encounter+1),standalone,true,null,null);rig.Protect();
        rig.Place(Camera.main.ViewportToWorldPoint(new Vector3(mission>=5?.17f:.5f,mission>=5?.5f:.17f,-Camera.main.transform.position.z)));
    }
    static object Begin(string name,Func<Task<object>> work)
    {
        Check(Application.isPlaying,"Play mode required");Check(CurrentTask==null||CurrentTask.IsCompleted,"Another M6 probe is live");
        Evidence(name,new{status="RUNNING"});CurrentTask=Run(name,work);return new{started=name};
    }
    static async Task Run(string name,Func<Task<object>> work)
    {EditorApplication.LockReloadAssemblies();try{Evidence(name,await work());}catch(Exception e){Evidence(name,new{status="FAILED",error=e.ToString()});}finally{EditorApplication.UnlockReloadAssemblies();}}
    public static object Inspect()
    {
        var catalog=Region.Missions;Check(catalog.Length==8,"Catalog must end at L8");
        foreach(var level in catalog)Check(level.IsValid(out var error),level.name+": "+error);
        Check(catalog.Skip(4).All(l=>l.Direction==PlayDirection.Right),"Region B direction");
        int missing=0,references=0,missingBeamMaterials=0;
        var paths=AssetDatabase.FindAssets("",new[]{"Assets/StarShift/Data/RegionB","Assets/StarShift/Prefabs/Enemies"}).Select(AssetDatabase.GUIDToAssetPath);
        foreach(var path in paths)
        foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            var game=obj as GameObject;
            var objects=game!=null?game.GetComponentsInChildren<Component>(true).Where(c=>c!=null).Cast<UnityEngine.Object>():new[]{obj};
            if(game!=null)missingBeamMaterials+=game.GetComponentsInChildren<LineRenderer>(true).Count(l=>l.sharedMaterial==null);
            if(game!=null)missing+=game.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            foreach(var item in objects)
            {
                if(item==null)continue;var it=new SerializedObject(item).GetIterator();
                while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue==null&&it.objectReferenceEntityIdValue!=default)references++;
            }
        }
        Check(missingBeamMaterials==0,"Missing beam materials: "+missingBeamMaterials);
        Check(missing==0&&references==0,"Missing scripts/references "+missing+"/"+references);
        var dev=new SerializedObject(Runner.GetComponent<DevFastStart>()).FindProperty("devModeEnabled").boolValue;Check(!dev,"Saved Dev mode enabled");
        return new{status="PASS",missions=catalog.Length,regionBEncounters=catalog.Skip(4).Sum(l=>l.Encounters.Length),missingScripts=missing,missingReferences=references,devMode=dev};
    }
    public static object BeginFormations(int width)=>Begin("formations-"+width,()=>Formations(width));
    public static object BeginClosureLayouts(int width)=>Begin("closure-layouts-"+width,()=>Formations(width,true));
    static async Task<object> Formations(int width,bool closureOnly=false)
    {
        using(var rig=new Rig(width,6))
        {
            Time.timeScale=2;var evidence=new List<object>();int totalOverlaps=0;
            foreach(var item in (closureOnly?new[]{(2,0),(3,2),(6,0),(7,1),(8,0),(8,1)}:new[]{(1,0),(1,1),(2,0),(2,2),(3,0),(3,1),(3,2),(5,0),(5,1),(5,2),(6,0),(6,2),(7,0),(7,1),(7,2),(8,0),(8,1)}))
            {
                Start(rig,item.Item1,item.Item2);await Until(()=>Runner.Elapsed>4.0f,4500,"Formation settle");
                var enemies=EnemyHealth.Active.Where(e=>e.IsAlive&&e.GetComponent<EntryHoldFlight>()!=null&&e.transform.IsChildOf(Runner.transform)).ToArray();
                var overlaps=new List<string>();
                for(int i=0;i<enemies.Length;i++)for(int j=i+1;j<enemies.Length;j++)
                {
                    var a=enemies[i].GetComponentsInChildren<Collider2D>().Where(c=>c.enabled&&c.gameObject.activeInHierarchy).ToArray();
                    var b=enemies[j].GetComponentsInChildren<Collider2D>().Where(c=>c.enabled&&c.gameObject.activeInHierarchy).ToArray();
                    if(a.Any(x=>b.Any(y=>x.bounds.Intersects(y.bounds))))overlaps.Add(enemies[i].name+"/"+enemies[j].name);
                }
                if(closureOnly)foreach(var enemy in enemies.Where(e=>!e.GetComponent<EntryHoldFlight>().IsStriking))
                {
                    var hull=enemy.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r=>r.name=="HullPlaceholder");if(hull==null)continue;
                    var min=Camera.main.WorldToViewportPoint(hull.bounds.min);var max=Camera.main.WorldToViewportPoint(hull.bounds.max);
                    Check(min.x>=0&&min.y>=0&&max.x<=1&&max.y<=.94f,"Formation hull clipped/under top HUD: "+enemy.name+" "+min+" / "+max);
                    var viewport=new Rect(min.x,min.y,max.x-min.x,max.y-min.y);
                    Check(!viewport.Overlaps(new Rect(.935f,0,.065f,.20f)),"Formation hull under weapon/missile HUD: "+enemy.name);
                }
                totalOverlaps+=overlaps.Count;
                await Capture($"L{item.Item1}-E{item.Item2+1}-{width}");
                evidence.Add(new{mission=item.Item1,encounter=item.Item2+1,count=enemies.Length,overlaps});
            }
            var result=new{status=totalOverlaps==0?"PASS":"FAILED",width,totalOverlaps,formations=evidence};return result;
        }
    }
    public static object BeginMechanics()=>Begin("mechanics",Mechanics);
    static async Task<object> Mechanics()
    {
        using(var rig=new Rig(1600,6))
        {
            Time.timeScale=2;Start(rig,6);await Until(()=>Runner.Elapsed>7,6000,"Sniper activation");
            var sniper=EnemyHealth.Active.First(e=>e.IsAlive&&e.name.StartsWith("Sniper"));var beam=sniper.GetComponent<DirectedAttack>();
            await Until(()=>beam.AttacksFired>0,7000,"Sniper never fired");
            Session.Pause();float elapsed=Runner.Elapsed;var pose=sniper.transform.position;await Task.Delay(200);Check(Runner.Elapsed==elapsed&&pose==sniper.transform.position,"Pause advanced sniper");Session.Resume();
            Start(rig,7,2);await Until(()=>Runner.Elapsed>4,4000,"Dual Sniper entry");int attacks=0,maxActive=0;float stop=Runner.Elapsed+12;
            while(Runner.Elapsed<stop){var beams=UnityEngine.Object.FindObjectsByType<DirectedAttack>();maxActive=Math.Max(maxActive,beams.Count(a=>a.IsBusy));attacks=beams.Sum(a=>a.AttacksFired);await Task.Delay(30);}
            Check(maxActive<=1&&attacks>=2,"Dual Sniper telegraphs were not staggered");
            Start(rig,7,1);await Until(()=>Runner.Elapsed>9,6500,"Carrier spawn");
            var carrier=EnemyHealth.Active.First(e=>e.IsAlive&&e.name.StartsWith("Carrier"));var spawner=carrier.GetComponent<EscortSpawner>();
            Check(spawner.SpawnCount>0&&spawner.LiveCount<=spawner.Capacity,"Carrier cap/spawn");
            var interceptor=EnemyHealth.Active.First(e=>e.IsAlive&&e.name.StartsWith("Interceptor"));var strike=interceptor.GetComponent<DirectedAttack>();
            await Until(()=>strike.AttacksFired>0,7000,"Interceptor strike");
            var before=spawner.SpawnCount;carrier.ReceivePlayerHit(carrier.CurrentHp,rig.Player);await Task.Delay(1300);Check(spawner.SpawnCount==before,"Dead Carrier kept spawning");
            var retained=EnemyHealth.Active.Where(e=>e.IsAlive&&e.name.StartsWith("WardenDrone")).ToArray();
            Check(retained.All(e=>e.GetComponent<EntryHoldFlight>().IsExiting),"Carrier minions did not retreat");
            Start(rig,6,1);var schedule=Runner.Current.Groups;Check(schedule.All(g=>!g.Required&&g.ContinuousTransit),"Corridor copied storm stream");
            await Until(()=>Runner.Elapsed>7,6000,"Corridor transit");await Capture("corridor-1600");
            var moving=EnemyHealth.Active.Where(e=>e.IsAlive&&e.GetComponent<AsteroidVisualMotion>()!=null).Select(e=>(e,e.transform.position)).ToArray();await Task.Delay(150);
            Check(moving.Any(e=>e.e.IsAlive&&e.e.transform.position.x<e.position.x-.05f),"Right-direction debris did not cross left: "+string.Join(";",moving.Select(e=>e.e.name+":"+e.position+" -> "+e.e.transform.position+" alive="+e.e.IsAlive)));
            await Until(()=>!Runner.HasActiveRun,15000,"Corridor ExitDrain stuck");Check(!EnemyHealth.Active.Any(e=>e.IsAlive),"Corridor leaked objects");
            return new{status="PASS",sniper=true,pause=true,dualSniperMaxBusy=maxActive,dualSniperAttacks=attacks,carrier=true,interceptor=true,carrierDeathStopsSpawn=true,corridor=true,naturalExitDrain=true};
        }
    }
    public static object BeginBoss()=>Begin("boss",Boss);
    static async Task<object> Boss()
    {
        using(var rig=new Rig(1600,6))
        {
            Time.timeScale=2;Start(rig,8,1,false);await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossShieldNetwork>()!=null),1500,"Warden spawn");
            var boss=EnemyHealth.Active.First(e=>e.GetComponent<BossShieldNetwork>()!=null);var shield=boss.GetComponent<BossShieldNetwork>();var phase=boss.GetComponent<BossPhaseRunner>();
            int hp=boss.CurrentHp;boss.ReceivePlayerHit(99999,rig.Player);Check(boss.CurrentHp==hp,"Arrival core damage");
            var nodes=boss.GetComponentsInChildren<EnemyHealth>().Where(e=>e!=boss).ToArray();foreach(var node in nodes)node.ReceivePlayerHit(9999,rig.Player);Check(nodes.All(e=>e.IsAlive),"Entry modules melted");
            await Until(()=>boss.GetComponent<EntryHoldFlight>().IsHolding,4500,"Warden entry");
            boss.ReceivePlayerHit(9999,rig.Player);Check(boss.CurrentHp==hp,"Shield core received damage");
            await Until(()=>boss.GetComponentsInChildren<DirectedAttack>().Any(a=>a.AttacksFired>0),7000,"Beam Sweep missing");
            await Until(()=>boss.GetComponentsInChildren<EscortSpawner>().Any(a=>a.SpawnCount>0),12000,"Drone Spawn missing");
            Check(boss.GetComponentsInChildren<BossAttackModule>().Any(a=>a.ShotsFired>=5),"Plasma Burst missing");
            await Capture("warden-protected-1600");
            nodes[0].ReceivePlayerHit(nodes[0].CurrentHp,rig.Player);await Task.Delay(100);Check(shield.RemainingNodes==2&&!shield.IsExposed,"First module contract");
            foreach(var node in nodes.Skip(1))node.ReceivePlayerHit(node.CurrentHp,rig.Player);
            await Until(()=>shield.IsExposed&&phase.IsPhaseB,2500,"Shield collapse/exposed core");await Capture("warden-exposed-1600");
            Check(nodes.All(n=>!n.IsAlive)&&boss.CanReceiveDamage,"Exposed core damage gate");
            await Until(()=>boss.GetComponentsInChildren<DirectedAttack>().Any(a=>a.AttacksFired>0),7000,"Exposed beam rotation");
            var count=Runner.CompletionCount;boss.ReceivePlayerHit(boss.CurrentHp,rig.Player);Check(Runner.Stage==EncounterStage.BossPostDeath,"Boss bypassed staged death");
            await Until(()=>Runner.CompletionCount==count+1,4000,"Boss death reward resolution");
            Check(UnityEngine.Object.FindObjectsByType<RewardPickup>().Length>0,"Boss reward missing");
            foreach(var reward in UnityEngine.Object.FindObjectsByType<RewardPickup>())reward.Release();
            await Until(()=>Session.IsClear,2000,"Region B victory");Check(Runner.Flow.Level.Id=="L8"&&Region.NextMission==null,"Loaded Region C");
            await Capture("region-b-victory-1600");
            return new{status="PASS",entryProtection=true,shieldProtection=true,threeModules=true,beamSweep=true,droneSpawn=true,plasmaBurst=true,collapse=true,phase2=true,stagedDeath=true,bossReward=true,exactlyOnce=Runner.CompletionCount==count+1,victory=true};
        }
    }
    public static object BeginTapInput()=>Begin("tap-input",async()=>{
        Runner.StopRun();Runner.Flow.LoadLevel(Region.FindMission("L1"));
        var devices=UnityEngine.InputSystem.InputSystem.devices.ToArray();
        var disabledFlag=typeof(UnityEngine.InputSystem.InputDevice).GetProperty("disabledInFrontend",BindingFlags.Instance|BindingFlags.NonPublic);
        var disabled=devices.Select(d=>(bool)disabledFlag.GetValue(d)).ToArray();
        using(var rig=new Rig(1600,6,true))
        {
            await Until(()=>Runner.Flow.CanContinue,4500,"Continue hint not ready");
            await rig.MouseEvent(new Vector2(500,400),false);
            await rig.MouseEvent(new Vector2(500,400),true);
            await rig.MouseEvent(new Vector2(500,400),false);
            await Until(()=>!Runner.Flow.IsAwaitingStart,1000,"UI tap did not start approach");
        }
        Check(devices.SequenceEqual(UnityEngine.InputSystem.InputSystem.devices),"Synthetic devices leaked");
        // Mất focus có thể disable runtime hợp lệ; probe không được để lại disable thủ công.
        Check(devices.Select(d=>(bool)disabledFlag.GetValue(d)).SequenceEqual(disabled),"Native input state not restored");
        return new{status="PASS",uiTap=true,inputRestored=true};
    });
    public static object BeginPlayer()=>Begin("player-right",async()=>{
        using(var rig=new Rig(1600,6))
        {
            Start(rig,5);var life=rig.Player.GetComponent<PlayerDamageLifecycle>();var loadout=rig.Player.GetComponent<WeaponLoadout>();var weapon=rig.Player.GetComponent<PulseLaserWeapon>();
            loadout.Restore(WeaponBranch.Pulse,new[]{3,3,3});
            var source=new Vector2(400,300);await rig.MouseEvent(source,false);await rig.MouseEvent(source,true);
            var before=Camera.main.WorldToScreenPoint(rig.Player.transform.position);await rig.MouseEvent(source+new Vector2(55,35),true);
            var delta=(Vector2)(Camera.main.WorldToScreenPoint(rig.Player.transform.position)-before);Check((delta-new Vector2(55,35)).sqrMagnitude<4,"Right relative drag "+delta);
            var branches=new List<string>();
            for(int i=0;i<3;i++) {var shots=weapon.ShotsFired;await Task.Delay(350);Check(weapon.ShotsFired>shots,"Branch did not fire "+loadout.ActiveBranch+" held="+rig.Input.FireHeld+" canFire="+rig.Gate.CanFire+" heat="+weapon.Heat+" mouse="+UnityEngine.InputSystem.Mouse.current?.deviceId+" pressed="+UnityEngine.InputSystem.Mouse.current?.leftButton.isPressed);branches.Add(loadout.ActiveBranch.ToString());var warm=weapon.Heat;loadout.Cycle();Check(weapon.Heat==warm,"Switch reset shared heat");}
            await rig.MouseEvent(source,false);var hot=weapon.Heat;rig.Gate.SetState(PlayerGameplayState.Entry);await Task.Delay(700);Check(weapon.Heat<hot,"Fire-disabled heat stopped cooling");rig.Protect();
            Session.Pause();var paused=weapon.Heat;var fixedTime=Time.fixedTime;await Task.Delay(180);Check(weapon.Heat==paused&&Time.fixedTime==fixedTime,"Pause changed thermal/clock");Session.Resume();rig.Protect();
            await rig.MouseEvent(source,false);await rig.MouseEvent(source,true);await Task.Delay(120);rig.Unprotect();int lives=rig.Lives.CurrentLives;int level=loadout.ActiveLevel;
            Check(life.TryReceiveHit(null),"Right death rejected");Check(rig.Lives.CurrentLives==lives-1&&loadout.ActiveLevel==Math.Max(1,level-1),"Death penalty changed");
            await rig.MouseEvent(source+new Vector2(200,100),true);await Until(()=>rig.Gate.State==PlayerGameplayState.PostEntryInvincibility,2500,"Right respawn");
            var pose=rig.Player.transform.position;var fired=weapon.ShotsFired;await rig.MouseEvent(source+new Vector2(220,110),true);await Task.Delay(150);
            Check(rig.Input.FireHeld&&weapon.ShotsFired>fired,"Respawn lost held fire: held="+rig.Input.FireHeld+", shots="+weapon.ShotsFired+"/"+fired+", state="+rig.Gate.State+", focus="+Application.isFocused);Check(rig.Player.transform.position.x>pose.x,"Respawn held drag lost");Check(!rig.Gate.CanTakeDamage,"Respawn protection lost");
            return new{status="PASS",relativeDrag=delta.ToString(),branches,sharedHeat=true,blockedCooling=true,pause=true,death=true,downgrade=true,rightRespawn=true,heldContinuity=true};
        }
    });
    public static object BeginBossBalance()=>Begin("boss-balance",()=>BossBalance(false));
    public static object BeginHomingBalance()=>Begin("homing-balance",()=>BossBalance(true));
    static async Task<object> BossBalance(bool homingOnly)
    {
        using(var rig=new Rig(1600,6))
        {
            Time.timeScale=3;var results=new List<object>();
            foreach(int level in new[]{3,5})foreach(WeaponBranch branch in (homingOnly?new[]{WeaponBranch.Homing}:Enum.GetValues(typeof(WeaponBranch)).Cast<WeaponBranch>()))
            {
                Start(rig,8,1);rig.Player.GetComponent<WeaponLoadout>().Restore(branch,new[]{level,level,level});
                await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossShieldNetwork>()!=null),1000,"Balance boss spawn");
                var core=EnemyHealth.Active.First(e=>e.GetComponent<BossShieldNetwork>()!=null);var nodes=core.GetComponentsInChildren<EnemyHealth>().Where(e=>e!=core).ToArray();
                var beams=core.GetComponentsInChildren<DirectedAttack>();var modules=core.GetComponentsInChildren<BossAttackModule>();var spawners=core.GetComponentsInChildren<EscortSpawner>();
                var began=Time.fixedTime;var real=Time.realtimeSinceStartup;float exposed=-1;
                await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);
                while(core.IsAlive&&Time.realtimeSinceStartup-real<70)
                {
                    var target=nodes.FirstOrDefault(n=>n.CanReceiveDamage)??(core.CanReceiveDamage?core:null);
                    if(target!=null){var viewport=Camera.main.WorldToViewportPoint(target.transform.position);rig.Place(Camera.main.ViewportToWorldPoint(new Vector3(branch==WeaponBranch.Plasma?.38f:branch==WeaponBranch.Homing?.29f:.17f,viewport.y,-Camera.main.transform.position.z)));}
                    if(core.GetComponent<BossShieldNetwork>().IsExposed&&exposed<0)exposed=Time.fixedTime-began;
                    await Task.Delay(25);
                }
                Check(!core.IsAlive,"Boss weapon pacing timeout: "+branch+" L"+level+" core="+core.CurrentHp+" nodes="+string.Join(",",nodes.Select(n=>n.CurrentHp)));Check(exposed>0,"Build bypassed shield loop");
                results.Add(new{branch=branch.ToString(),level,forward=branch==WeaponBranch.Plasma?.38f:branch==WeaponBranch.Homing?.29f:.17f,totalSeconds=Time.fixedTime-began,shieldSeconds=exposed,beamSweeps=beams.Sum(a=>a.AttacksFired),plasmaShots=modules.Sum(a=>a.ShotsFired),drones=spawners.Sum(a=>a.SpawnCount),coreHp=core.MaxHp});
                Evidence("boss-balance-live",new{status="RUNNING",results});
                await rig.MouseEvent(new Vector2(400,300),false);
            }
            return new{status="PASS",protectedBot=true,simSpeed=3,results};
        }
    }
    sealed class Storage : IDisposable
    {
        readonly string original=Save.SavePath;
        readonly string bytes=File.Exists(Save.SavePath)?Convert.ToBase64String(File.ReadAllBytes(Save.SavePath)):null;
        public readonly string Path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"starshift-m6-"+Guid.NewGuid().ToString("N"),"save.json");
        public void Dispose(){Save.ExitIsolated();Save.UseTestStorage(original);Check(bytes==(File.Exists(original)?Convert.ToBase64String(File.ReadAllBytes(original)):null),"Production save changed");}
        public void Seed(int mission,int encounter,CampaignStatus status=CampaignStatus.Active)
        {
            Save.ExitIsolated();
            var data=new SaveEnvelope{SchemaVersion=2,HasCampaign=true,Campaign=new CampaignSave{MissionId="L"+mission,Status=status,Checkpoint=new CheckpointData{EncounterId="ENCOUNTER_"+encounter,Lives=3,Missiles=2,WeaponLevels=new[]{3,2,2},Stats=new RunStatsData{Score=321}}},Profile=new PlayerProfile{StellarShardBalance=25,UnlockedMissions=Enumerable.Range(1,mission).Select(n=>"L"+n).ToArray(),Records=Array.Empty<MissionRecord>()},Settings=SettingsData.Defaults()};
            Check(new JsonSaveStore(Path).Write(data,d=>SaveValidation.IsValid(d,(m,e)=>Region.FindMission(m)?.ResolveStart(e)>=0,5)),"Fixture write failed");Save.UseTestStorage(Path);Check(Save.Continue(),"Fixture Continue "+Save.Diagnostic);
        }
    }
    public static object BeginPersistence()=>Begin("persistence",Persistence);
    static async Task<object> Persistence()
    {
        using(var rig=new Rig(1600,6))using(var store=new Storage())
        {
            Time.timeScale=3;store.Seed(6,3);rig.Protect();Check(Runner.Flow.Level.Id=="L6"&&Runner.CurrentIndex==2,"Continue catalog");
            var loadout=rig.Player.GetComponent<WeaponLoadout>();loadout.ApplyCore(WeaponBranch.Plasma);Session.Pause();Check(Save.RetryEncounter(),"Retry rejected");rig.Protect();Check(loadout.Level(WeaponBranch.Plasma)==2&&Runner.CurrentIndex==2,"Retry loadout/checkpoint");
            var original=File.ReadAllText(store.Path);Save.ExitIsolated();Check(Save.BeginReplay(),"Replay unavailable");rig.Protect();Save.ExitIsolated();Check(File.ReadAllText(store.Path)==original,"Replay wrote campaign");
            store.Seed(8,2);rig.Protect();
            await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossShieldNetwork>()!=null&&e.GetComponentsInChildren<EnemyHealth>().Any(n=>n!=e&&n.CanReceiveDamage)),3000,"Boss Retry setup");
            var damagedBoss=EnemyHealth.Active.First(e=>e.GetComponent<BossShieldNetwork>()!=null);var version=damagedBoss.SpawnVersion;
            var firstNode=damagedBoss.GetComponentsInChildren<EnemyHealth>().First(n=>n!=damagedBoss);firstNode.ReceivePlayerHit(firstNode.CurrentHp,rig.Player);await Task.Delay(60);
            Session.Pause();Check(Save.RetryEncounter(),"Boss Retry rejected");rig.Protect();
            await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossShieldNetwork>()!=null),1500,"Boss Retry spawn");
            var freshBoss=EnemyHealth.Active.First(e=>e.GetComponent<BossShieldNetwork>()!=null);Check(freshBoss.SpawnVersion!=version&&!freshBoss.GetComponent<BossPhaseRunner>().IsPhaseB,"Boss Retry retained phase/version");
            Check(freshBoss.GetComponentsInChildren<EnemyHealth>().All(n=>n.CurrentHp==n.MaxHp)&&freshBoss.GetComponent<BossShieldNetwork>().RemainingNodes==3,"Boss Retry retained module damage");
            foreach(var status in new[]{CampaignStatus.RegionTransition,CampaignStatus.RegionReady})
            {
                store.Seed(4,2,status);
                if(status==CampaignStatus.RegionTransition){await Until(()=>Runner.GetComponent<PlanetJourney>().IsAwaitingTap,5500,"L4 transition reveal");Check(Runner.Flow.BeginApproach(),"Journey tap rejected");await Until(()=>Runner.Flow.Level.Id=="L5",3000,"L4 to L5 handoff");}
                Check(Runner.Flow.Level.Id=="L5"&&Runner.Flow.Direction==PlayDirection.Right,"Legacy L4 marker not resumed into L5");
                Check(Save.PersistentSnapshot.Campaign.MissionId=="L5"&&Save.PersistentSnapshot.Campaign.Status==CampaignStatus.Active,"L5 entry not persisted");
                Check(Save.PersistentSnapshot.Profile.Records.Length==0,"Transition granted duplicate record");
            }
            store.Seed(8,2,CampaignStatus.CampaignCompleted);Check(Session.IsClear&&!Runner.HasActiveRun,"Completed Continue replayed boss");var completed=File.ReadAllText(store.Path);Save.Flush();Check(completed==File.ReadAllText(store.Path),"Completed Continue rewrote save");
            return new{status="PASS",continueL6=true,retry=true,bossRetry=true,replayIsolation=true,legacyTransition=true,legacyReady=true,l5Checkpoint=true,campaignCompletedContinue=true,productionUntouched=true};
        }
    }
    public static object BeginHandoff()=>Begin("astra-to-crimson",async()=>{
        using(var rig=new Rig(1600,6))using(var store=new Storage())
        {
            Time.timeScale=2;store.Seed(4,2);rig.Protect();await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossPhaseRunner>()!=null&&e.CanReceiveDamage),4000,"Vanguard arrival");
            var boss=EnemyHealth.Active.First(e=>e.GetComponent<BossPhaseRunner>()!=null);boss.ReceivePlayerHit(boss.CurrentHp,rig.Player);
            await Until(()=>Runner.Stage==EncounterStage.RewardGrace||Region.IsTravelling,4500,"Vanguard staged death/reward");
            foreach(var p in UnityEngine.Object.FindObjectsByType<RewardPickup>())p.Release();
            await Until(()=>Runner.GetComponent<PlanetJourney>().IsAwaitingTap,6500,"Planet reveal");Check(Runner.Flow.BeginApproach(),"Accept transition tap");
            await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);
            var before=rig.Player.transform.position;await rig.MouseEvent(new Vector2(440,320),true);Check(rig.Input.FireHeld&&Vector3.Distance(before,rig.Player.transform.position)>.1f,"Approach input locked");
            float maxCameraStep=0;var size=Camera.main.orthographicSize;var real=Time.realtimeSinceStartup;Vector3 last=rig.Player.transform.position;
            while(Runner.Flow.Level.Id!="L5"&&Time.realtimeSinceStartup-real<4){last=rig.Player.transform.position;await Task.Delay(16);maxCameraStep=Math.Max(maxCameraStep,Math.Abs(Camera.main.orthographicSize-size));size=Camera.main.orthographicSize;}
            Check(Runner.Flow.Level.Id=="L5"&&Runner.Flow.Direction==PlayDirection.Right,"Real handoff did not start Mission05");
            Check(rig.Input.FireHeld&&Vector3.Distance(last,rig.Player.transform.position)<.6f,"L5 handoff lost gesture/pose");Check(maxCameraStep<.9f,"Camera snap at handoff: "+maxCameraStep);
            Check(Save.PersistentSnapshot.Campaign.MissionId=="L5"&&Save.PersistentSnapshot.Profile.Records.Count(r=>r.MissionId=="L4")==1,"L4/L5 save transaction");
            await Capture("astra-to-crimson-mission05-1600");
            return new{status="PASS",vanguardDeath=true,reward=true,journey=true,approachInput=true,heldGesture=true,maxCameraStep,l4RecordOnce=true,l5Checkpoint=true,productionUntouched=true};
        }
    });
    public static object BeginSupply()=>Begin("supply-right",async()=>{
        using(var rig=new Rig(1600,6))
        {
            Time.timeScale=2;Start(rig,6,1);rig.Player.GetComponent<WeaponLoadout>().Restore(WeaponBranch.Pulse,new[]{3,2,2});
            await Until(()=>EnemyHealth.Active.Any(e=>e.name.StartsWith("SupplyDrone")&&e.CanReceiveDamage),8000,"Corridor Supply entry");
            var supply=EnemyHealth.Active.First(e=>e.name.StartsWith("SupplyDrone")&&e.IsAlive);var screen=Camera.main.WorldToViewportPoint(supply.transform.position);
            rig.Place(Camera.main.ViewportToWorldPoint(new Vector3(.20f,screen.y,-Camera.main.transform.position.z)));
            await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);await Until(()=>!supply.IsAlive,2000,"Right weapon did not destroy Supply");await rig.MouseEvent(new Vector2(400,300),false);
            var pair=UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive&&p.Crate==null&&p.Item.Kind!=PickupKind.Shard).ToArray();
            Check(pair.Length==2&&pair.Count(p=>p.Item.Kind==PickupKind.WeaponCore)==1,"Supply direct Core/support pair missing");
            Check(!UnityEngine.Object.FindObjectsByType<RewardPickup>().Any(p=>p.Crate!=null),"Production spawned legacy BOX");
            var positions=pair.Select(p=>p.transform.position).ToArray();await Capture("supply-right-separation-1600");await Task.Delay(600);
            Check(pair.Select((p,i)=>p.transform.position.x<positions[i].x-.1f).All(x=>x),"Right rewards did not drift left");
            Check(Mathf.Abs(pair[0].transform.position.y-pair[1].transform.position.y)>.7f,"Supply pair did not separate vertically");
            await Capture("supply-right-drift-1600");
            foreach(var item in pair){rig.Place(item.transform.position);await Until(()=>!item.IsActive,1000,"Direct Supply pickup collection");}
            Check(Runner.GetComponent<RunRewards>().SupplyCaptured==1&&Runner.GetComponent<RunRewards>().CoresCollected==1,"Supply collection accounting");
            return new{status="PASS",rightHeldWeapon=true,destroyed=true,noIntermediateBox=true,coreAndSupport=true,collected=true,captureOnce=true};
        }
    });
    public static object BeginPresentation()=>Begin("presentation",async()=>{
        var captures=new List<string>();
        foreach(int width in new[]{1600,2000})using(var rig=new Rig(width,6))
        {
            Time.timeScale=2;
            foreach(var item in new[]{(6,0),(7,1),(8,1)})
            {
                Start(rig,item.Item1,item.Item2);await Until(()=>Runner.Elapsed>4.2f,4000,"Presentation settle");
                if(item.Item1==8)await Until(()=>UnityEngine.Object.FindObjectsByType<DirectedAttack>().Any(a=>a.Definition.Sweep&&a.IsBusy&&!a.IsActive),5000,"Warden beam charge");
                var name=$"final-L{item.Item1}-E{item.Item2+1}-{width}";await Capture(name);captures.Add(name);
                if(item.Item1==8)Check(UnityEngine.Object.FindObjectsByType<LineRenderer>().Any(l=>l.name=="SweepDirection"&&l.enabled),"Sweep direction cue missing");
            }
        }
        return new{status="PASS",captures,sweepDirectionTelegraph=true};
    });
    public static object BeginTravelRetry()=>Begin("mission-travel-retry",async()=>{
        using(var rig=new Rig(1600,6))using(var store=new Storage())
        {
            Time.timeScale=3;store.Seed(5,3);rig.Protect();await Until(()=>Runner.SpawnedCount==Runner.Current.EnemyCount&&EnemyHealth.Active.Where(e=>e.IsAlive).All(e=>e.CanReceiveDamage),3500,"Mission5 travel setup");
            foreach(var enemy in EnemyHealth.Active.ToArray())if(enemy.CanReceiveDamage)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
            foreach(var item in UnityEngine.Object.FindObjectsByType<RewardPickup>())item.Release();
            await Until(()=>Region.IsMissionTravel,2500,"Mission5 travel");Check(Save.PersistentSnapshot.Campaign.MissionId=="L6","Incoming checkpoint not committed");Session.Pause();Check(Save.RetryEncounter(),"Travel Retry rejected");rig.Protect();
            Check(Runner.Flow.Level.Id=="L6"&&Runner.CurrentIndex==0,"Travel Retry restored outgoing mission after L6 was committed");
            Check(Save.PersistentSnapshot.Campaign.MissionId=="L6"&&Save.PersistentSnapshot.Campaign.Checkpoint.EncounterId=="ENCOUNTER_1","Retry corrupted incoming checkpoint");
            return new{status="PASS",incomingMission="L6",checkpoint="ENCOUNTER_1",productionUntouched=true};
        }
    });
    public static object BeginShardBalance()=>Begin("shard-balance",async()=>{
        using(var rig=new Rig(1600,6))using(var store=new Storage())
        {
            Time.timeScale=3;
            var rewards=Runner.GetComponent<RunRewards>();
            var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();
            var shard=AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/StellarShard.asset");
            var hud=UnityEngine.Object.FindAnyObjectByType<StarShift.UI.GameplayHud>();
            var label=(UnityEngine.UI.Text)typeof(StarShift.UI.GameplayHud).GetField("shardLabel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            Action<int> collect=count=>{for(int i=0;i<count;i++)Check(receiver.TryCollect(shard),"Shard collection rejected");};
            Func<int,Task> assertHud=async expected=>{await Task.Delay(90);Check(Save.ShardBalance==expected&&label.text==expected.ToString(),"Wallet/HUD mismatch: "+Save.ShardBalance+" / "+label.text+" expected "+expected);};
            Func<int,int,Task> clearTo=async (mission,expected)=>{
                var deadline=Time.realtimeSinceStartup+12;
                while(Runner.Flow.Level.MissionNumber!=mission&&Time.realtimeSinceStartup<deadline)
                {
                    foreach(var enemy in EnemyHealth.Active.ToArray())if(enemy.CanReceiveDamage)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
                    foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>())pickup.Release();
                    if(Runner.GetComponent<PlanetJourney>().IsAwaitingTap)Check(Runner.Flow.BeginApproach(),"Journey tap rejected");
                    Check(Save.ShardBalance==expected,"Transition changed wallet");await Task.Delay(25);
                }
                Check(Runner.Flow.Level.MissionNumber==mission,"Shard transition timed out");rig.Protect();await assertHud(expected);
            };
            store.Seed(5,3);rig.Protect();await assertHud(25);collect(3);await assertHud(28);
            await clearTo(6,28);Check(rewards.Shards==0,"Mission-local stats did not reset");
            collect(2);await assertHud(30);Session.Pause();Check(Save.RetryEncounter(),"Shard Retry rejected");rig.Protect();await assertHud(30);
            collect(2);await assertHud(30);collect(1);await assertHud(31);
            Check(Save.Flush()&&Save.Continue(),"Wallet Continue failed");rig.Protect();await assertHud(31);
            Check(Save.NewGame(),"Wallet New Game failed");await assertHud(31);
            Check(Save.BeginReplay(),"Wallet Replay failed");collect(2);await assertHud(2);
            Check(Save.PersistentSnapshot.Profile.StellarShardBalance==31,"Replay changed persistent wallet");
            Save.ExitIsolated();Check(Save.Continue(),"Continue after Replay failed");await assertHud(31);
            store.Seed(4,2);rig.Protect();collect(5);await clearTo(5,30);
            Start(rig,5,2,false);await assertHud(0);collect(4);await clearTo(6,4);
            Start(rig,4,1,false);await assertHud(0);collect(5);await clearTo(5,5);
            Session.Pause();Check(Save.RetryEncounter(),"Dev wallet Retry failed");rig.Protect();await assertHud(5);
            Start(rig,5);await assertHud(0);
            return new{status="PASS",hud=true,sameRegion=true,crossRegion=true,missionStatsReset=true,retryDeduplication=true,continueWallet=true,newGamePreservesWallet=true,replayIsolation=true,devCarryAndReset=true,productionUntouched=true};
        }
    });
    public static object BeginVictoryVisual()=>Begin("victory-visual",async()=>{
        using(var rig=new Rig(1600,6))
        {
            Time.timeScale=3;Start(rig,8,1,false);
            await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossShieldNetwork>()!=null&&e.GetComponentsInChildren<EnemyHealth>().Any(n=>n!=e&&n.CanReceiveDamage)),2500,"Victory visual entry");
            var boss=EnemyHealth.Active.First(e=>e.GetComponent<BossShieldNetwork>()!=null);
            foreach(var node in boss.GetComponentsInChildren<EnemyHealth>().Where(n=>n!=boss))node.ReceivePlayerHit(node.CurrentHp,rig.Player);
            await Until(()=>boss.CanReceiveDamage,2000,"Victory visual core exposure");boss.ReceivePlayerHit(boss.CurrentHp,rig.Player);
            await Until(()=>Runner.Stage==EncounterStage.RewardGrace,2500,"Victory visual reward");foreach(var reward in UnityEngine.Object.FindObjectsByType<RewardPickup>())reward.Release();
            await Until(()=>Session.IsClear,1200,"Victory terminal state");await Task.Delay(2400);
            var hud=UnityEngine.Object.FindAnyObjectByType<StarShift.UI.GameplayHud>();var type=hud.GetType();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var overlay=(GameObject)type.GetField("overlay",flags).GetValue(hud);var title=(UnityEngine.UI.Text)type.GetField("title",flags).GetValue(hud);
            var next=(UnityEngine.UI.Button)type.GetField("continueButton",flags).GetValue(hud);
            Check(overlay.activeSelf&&title.text=="CHIẾN THẮNG · REGION B"&&!next.gameObject.activeSelf,"Victory overlay/next route incorrect");
            await Capture("final-victory-1600");
            return new{status="PASS",overlay=true,title=title.text,noNextMissionButton=true,frozen=Time.timeScale==0};
        }
    });
    public static object BeginDevRoutes()=>Begin("dev-routes",async()=>{
        using(var rig=new Rig(1600,6))
        {
            Time.timeScale=3;var dev=Runner.GetComponent<DevFastStart>();var config=EditorJsonUtility.ToJson(dev);var savePath=Save.SavePath;
            string Bytes()=>File.Exists(savePath)?Convert.ToBase64String(File.ReadAllBytes(savePath)):null;var before=Bytes();var routes=new List<string>();
            void Set(string name,object value)=>typeof(DevFastStart).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(dev,value);
            try
            {
                Set("devModeEnabled",true);Set("skipIntro",true);Set("standalone",true);
                foreach(var spec in new[]{(4,"BOSS"),(4,"TRANSITION"),(5,"LEVEL_START"),(6,"SNIPER"),(6,"CORRIDOR"),(7,"CARRIER"),(7,"DUAL_SNIPER"),(8,"PRE_BOSS"),(8,"BOSS"),(8,"BOSS_PHASE_B"),(8,"VICTORY")})
                {
                    Set("startLevel",Region.FindMission("L"+spec.Item1));Set("startPoint",spec.Item2);dev.Replay();rig.Protect();
                    if(spec.Item2=="BOSS_PHASE_B")await Until(()=>UnityEngine.Object.FindObjectsByType<BossPhaseRunner>().Any(b=>b.IsPhaseB),4500,"Boss phase dev route");
                    else if(spec.Item2=="VICTORY"){await Until(()=>Session.IsClear,9000,"Victory dev route");}
                    else if(spec.Item2=="TRANSITION"){Check(Region.IsTravelling,"Dev transition did not queue journey");}
                    else {await Until(()=>Runner.SpawnedCount>0,1500,"Dev route did not spawn "+spec.Item2);}
                    Check(Runner.Flow.Level.Id=="L"+spec.Item1&&Save.Mode==SessionMode.DevTest,"Dev route escaped isolated mode");routes.Add(spec.Item1+"/"+spec.Item2);
                }
                Check(before==Bytes(),"Dev wrote production save");
            }
            finally{Runner.StopRun();EditorJsonUtility.FromJsonOverwrite(config,dev);}
            return new{status="PASS",routes,productionUntouched=true};
        }
    });
    public static object BeginFullRegion()=>Begin("full-region-b",FullRegion);
    static async Task<object> FullRegion()
    {
        using(var rig=new Rig(1600,6))using(var store=new Storage())
        {
            store.Seed(5,1);rig.Protect();Time.timeScale=3;
            var started=Time.fixedTime;var clock=Time.realtimeSinceStartup;var seen=new HashSet<string>();var attacks=new HashSet<string>();var durations=new List<object>();var previous="L5";float boundary=Time.fixedTime;int peak=0;int loops=0;
            var loadout=rig.Player.GetComponent<WeaponLoadout>();
            await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);
            while(!Session.IsClear&&Time.realtimeSinceStartup-clock<240)
            {
                Check(rig.Lives.CurrentLives>0,"Full run GameOver");
                var key=Runner.Flow.Level.Id+"-"+Runner.CurrentIndex;seen.Add(key);
                if(previous!=Runner.Flow.Level.Id){durations.Add(new{mission=previous,seconds=Time.fixedTime-boundary,weaponLevels=loadout.CaptureLevels()});previous=Runner.Flow.Level.Id;boundary=Time.fixedTime;}
                peak=Math.Max(peak,EnemyHealth.Active.Count(e=>e.IsAlive));
                var targets=EnemyHealth.Active.Where(e=>e.CanReceiveDamage).OrderBy(e=>e.name.Contains("Carrier")?0:e.GetComponentInParent<BossShieldNetwork>()!=null?1:2).ThenBy(e=>e.CurrentHp).ToArray();
                if(targets.Length>0&&rig.Gate.CanInput)
                {
                    var target=targets[0];var point=Camera.main.WorldToViewportPoint(target.transform.position);
                    rig.Place(Camera.main.ViewportToWorldPoint(new Vector3(loadout.ActiveBranch==WeaponBranch.Plasma?.38f:loadout.ActiveBranch==WeaponBranch.Homing?.29f:.17f,Mathf.Clamp(point.y,.08f,.92f),-Camera.main.transform.position.z)));
                }
                foreach(var a in UnityEngine.Object.FindObjectsByType<DirectedAttack>())if(a.AttacksFired>0)attacks.Add(a.Definition.Kind.ToString());
                if(++loops%150==0)loadout.Cycle();
                if(loops%60==0)
                {
                    var pickup=UnityEngine.Object.FindObjectsByType<RewardPickup>().FirstOrDefault(p=>p.IsActive&&Camera.main.WorldToViewportPoint(p.transform.position).x<.6f);
                    if(pickup!=null)rig.Place(pickup.transform.position);
                }
                if(!rig.Input.FireHeld&&rig.Gate.CanInput){await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);}
                Evidence("full-region-live",new{status="RUNNING",mission=Runner.Flow.Level.Id,encounter=Runner.CurrentIndex+1,stage=Runner.Stage.ToString(),seconds=Time.fixedTime-started,enemies=EnemyHealth.Active.Count});
                await Task.Delay(40);
            }
            Check(Session.IsClear,"Full Region B timed out");durations.Add(new{mission=previous,seconds=Time.fixedTime-boundary});
            var snapshot=Save.PersistentSnapshot;Check(snapshot.Campaign.Status==CampaignStatus.CampaignCompleted&&snapshot.Campaign.MissionId=="L8","Victory save missing");
            Check(snapshot.Profile.Records.Length==4,"Expected Mission05-08 records exactly once");Check(snapshot.Profile.UnlockedMissions.Length==8,"Next mission unlocks missing");Check(seen.Count==11,"Not all Region B encounters played: "+seen.Count);
            Save.ExitIsolated();Check(Save.Continue()&&Session.IsClear,"Reload victory");
            return new{status="PASS",seconds=Time.fixedTime-started,durations,encounters=seen.OrderBy(x=>x).ToArray(),peakEnemies=peak,actualHeldWeapons=true,protectedBot=true,simSpeed=3,records=4,victorySaved=true,victoryContinue=true,productionUntouched=true};
        }
    }

    public static object BeginPickupDirections()=>Begin("pickup-directions",async()=>{
        var results=new List<object>();
        foreach(int width in new[]{1600,2000})foreach(int mission in new[]{1,5})
        using(var rig=new Rig(width,6))
        {
            var result=await VerifyPickupMotion.Run(mission);results.Add(new{width,mission,result});
        }
        return new{status="PASS",results};
    });
    public static object BeginHomingPaths()=>Begin("homing-paths",async()=>{
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var results=new List<object>();
        T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,flags).GetValue(o);
        foreach(int mission in new[]{1,5})using(var rig=new Rig(1600,6))
        foreach(var spec in new[]{(level:1,targets:1),(level:5,targets:1),(level:5,targets:3),(level:5,targets:2)})
        {
            Time.timeScale=2;rig.RestoreFlights();Start(rig,mission);await Until(()=>Runner.Elapsed>4.3f,4000,"Homing targets entering");Time.timeScale=1;
            var enemies=EnemyHealth.Active.Where(e=>e.CanReceiveDamage).ToArray();Check(enemies.Length>=3,"Homing fixture population");
            foreach(var e in enemies){rig.Freeze(e);e.SetDamageBlocked(true);}
            Vector3 At(float x,float y){var p=PlayDirectionSpace.Point(new Vector2(x,y),Runner.Flow.Direction);return Camera.main.ViewportToWorldPoint(new Vector3(p.x,p.y,-Camera.main.transform.position.z));}
            var targets=enemies.Take(spec.targets).ToArray();
            for(int i=0;i<targets.Length;i++)
            {
                targets[i].transform.position=At(targets.Length==1?.5f:.3f+.4f*i/(targets.Length-1),.72f);targets[i].SetDamageBlocked(false);
                typeof(EnemyHealth).GetField("<CurrentHp>k__BackingField",flags).SetValue(targets[i],1000);
            }
            Physics2D.SyncTransforms();rig.Place(At(.5f,.17f));
            var loadout=rig.Player.GetComponent<WeaponLoadout>();var weapon=rig.Player.GetComponent<PulseLaserWeapon>();
            loadout.Restore(WeaponBranch.Homing,new[]{5,5,spec.level});
            await rig.MouseEvent(new Vector2(400,300),false);var fired=weapon.ShotsFired;await rig.MouseEvent(new Vector2(400,300),true);
            await Until(()=>weapon.ShotsFired>=fired+spec.level,1000,"Held Homing volley");await rig.MouseEvent(new Vector2(400,300),false);
            var shots=UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Where(p=>p.Owner==rig.Player&&Field<bool>(p,"flying")).ToArray();Check(shots.Length==spec.level,"Unexpected volley count");
            var assigned=shots.Select(p=>Field<EnemyHealth>(p,"trackingTarget")).ToArray();Check(assigned.All(t=>targets.Contains(t)),"Invalid homing target");
            var loads=targets.Select(t=>assigned.Count(a=>a==t)).ToArray();Check(loads.Max()-loads.Min()<=1,"Volley clustered on one target");
            var start=shots.Select(p=>p.transform.position).ToArray();var firstSign=Mathf.Sign(Field<float>(shots[0],"launchTurnRate"));
            Session.Pause();var fixedTime=Time.fixedTime;await Task.Delay(180);Check(Time.fixedTime==fixedTime&&shots.Select((p,i)=>p.transform.position==start[i]).All(x=>x),"Pause advanced Homing");Session.Resume();
            var path=shots.Select(p=>new List<Vector3>{p.transform.position}).ToArray();var clock=Time.fixedTime;
            await Task.Delay(100);await Capture($"homing-L{mission}-lv{spec.level}-targets{spec.targets}-arc");
            while(Time.fixedTime-clock<.9f)
            {
                for(int i=0;i<shots.Length;i++)if(Field<bool>(shots[i],"flying"))path[i].Add(shots[i].transform.position);
                await Task.Delay(20);
            }
            float Curve(List<Vector3> points){var a=points[0];var d=points.Last()-a;return points.Max(p=>Vector3.Cross(d,p-a).magnitude/Mathf.Max(.001f,d.magnitude));}
            var curves=path.Select(Curve).ToArray();Check(spec.targets==1?curves.Count(c=>c>.012f)>=(spec.level==5?4:1):curves.Any(c=>c>.10f),"Homing path flattened: "+string.Join(",",curves));
            await Until(()=>shots.All(p=>!Field<bool>(p,"flying")),5000,"Missile volley failed to resolve");
            Check(targets.All(t=>t.CurrentHp<1000)&&targets.Sum(t=>1000-t.CurrentHp)==2*spec.level,"Assigned targets missed damage: "+string.Join(",",targets.Select(t=>1000-t.CurrentHp)));
            var received=targets.Select(t=>1000-t.CurrentHp).ToArray();
            if(spec.level==1)
            {
                fired=weapon.ShotsFired;await rig.MouseEvent(new Vector2(400,300),true);await Until(()=>weapon.ShotsFired>fired,500,"Second single missile");await rig.MouseEvent(new Vector2(400,300),false);
                var second=UnityEngine.Object.FindObjectsByType<PlayerProjectile>().First(p=>p.Owner==rig.Player&&Field<bool>(p,"flying"));Check(Mathf.Sign(Field<float>(second,"launchTurnRate"))==-firstSign,"Single volleys did not alternate arcs");
            }
            if(spec.targets>1)
            {
                foreach(var shot in shots)shot.Release();
                var config=loadout.ActiveDefinition;var probe=UnityEngine.Object.Instantiate(config.ProjectilePrefab);int version=targets[0].SpawnVersion;
                try
                {
                    probe.Launch(config,At(.5f,.17f),PlayDirectionSpace.Offset(Vector2.up,Runner.Flow.Direction),2,0,rig.Player,p=>p.ResetForPool(),targets[0],1);
                    targets[0].gameObject.SetActive(false);targets[0].gameObject.SetActive(true);rig.Freeze(targets[0]);targets[0].SetDamageBlocked(true);
                    await Task.Delay(60);Check(Field<EnemyHealth>(probe,"trackingTarget")!=targets[0]&&Field<EnemyHealth>(probe,"trackingTarget")!=null,"Stale pooled target was followed");
                    var replacement=Field<EnemyHealth>(probe,"trackingTarget");replacement.ReceivePlayerHit(replacement.CurrentHp,rig.Player);await Task.Delay(60);
                    Check(Field<EnemyHealth>(probe,"trackingTarget")!=replacement,"Dead target was followed");
                    probe.ResetForPool();Check(Field<float>(probe,"launchRemaining")==0&&Field<float>(probe,"launchTurnRate")==0,"Pooled arc state leaked");
                    var pulse=AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>("Assets/StarShift/Data/Weapons/PulseLaserLv1.asset");
                    Check(pulse!=null,"Pulse fixture missing");probe.Launch(pulse,At(.5f,.17f),PlayDirectionSpace.Offset(Vector2.up,Runner.Flow.Direction),1,0,rig.Player,p=>p.ResetForPool());
                    Check(Field<EnemyHealth>(probe,"trackingTarget")==null&&Field<float>(probe,"launchRemaining")==0,"Homing state leaked into Pulse");
                }
                finally{UnityEngine.Object.Destroy(probe.gameObject);}
            }
            results.Add(new{mission,spec.level,spec.targets,loads,curves,damage=received,pause=true});
            Evidence("homing-paths-live",new{status="RUNNING",results});
        }
        return new{status="PASS",results,singleVolleyAlternates=true,deadAndPooledTargetReacquisition=true,poolBranchReset=true};
    });
    public static object BeginPressure()=>Begin("feedback-pressure",async()=>{
        var results=new List<object>();
        using(var rig=new Rig(1600,6))
        foreach(var spec in new[]{(mission:1,encounter:1),(mission:3,encounter:2),(mission:6,encounter:0),(mission:7,encounter:1),(mission:4,encounter:1),(mission:8,encounter:1)})
        {
            Time.timeScale=3;Start(rig,spec.mission,spec.encounter);await Until(()=>Runner.Elapsed>4.5f,4000,"Pressure entry");
            var coordinator=Runner.GetComponent<EncounterFireCoordinator>();int max=0,peakShots=0,spawns=0;float quiet=0,maxQuiet=0;bool seen=false;
            var end=Runner.Elapsed+22;var last=Runner.Elapsed;var observed=new HashSet<string>();var origins=new List<float>();
            while(Runner.HasActiveRun&&Runner.Elapsed<end)
            {
                var boss=EnemyHealth.Active.FirstOrDefault(e=>e.GetComponent<BossPhaseRunner>()!=null);
                var active=UnityEngine.Object.FindObjectsByType<SingleFireAttack>().Count(a=>a.isActiveAndEnabled&&a.IsTelegraphing)+(boss!=null&&boss.GetComponent<BossPhaseRunner>().IsAttacking?1:0);
                max=Math.Max(max,active);Check(active<=Runner.Current.ActiveShooterLimit&&active<=4,"Attack cap exceeded: "+active);
                if(active>0){seen=true;quiet=0;}else if(seen){quiet+=Runner.Elapsed-last;maxQuiet=Math.Max(maxQuiet,quiet);}last=Runner.Elapsed;
                peakShots=Math.Max(peakShots,UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length);
                foreach(var e in EnemyHealth.Active.Where(e=>e.name.StartsWith("WardenDrone")))
                {
                    var key=e.GetEntityId().ToString()+":"+e.SpawnVersion;
                    if(observed.Add(key))
                    {
                        spawns++;if(boss!=null)origins.Add(Vector3.Distance(e.transform.position,boss.transform.position));
                    }
                }
                await Task.Delay(20);
            }
            Check(seen&&maxQuiet>=2.8f,"No readable collective rest L"+spec.mission+" E"+(spec.encounter+1)+": "+maxQuiet+", seen="+seen+", max="+max);
            if(spec.mission==4||spec.mission==8)Check(spawns>0,"Boss failed to launch escorts");
            results.Add(new{spec.mission,encounter=spec.encounter+1,maxActive=max,maxQuiet,peakProjectiles=peakShots,spawns,escortFirstDistances=origins});
            Evidence("feedback-pressure-live",new{status="RUNNING",results});
        }
        return new{status="PASS",results};
    });
    public static object BeginCoverAndIdle()=>Begin("feedback-cover-idle",async()=>{
        using(var rig=new Rig(1600,6))
        {
            Time.timeScale=2;Start(rig,6);await Until(()=>Runner.Elapsed>4.5f,4000,"Cover formation entry");
            var enemies=EnemyHealth.Active.Where(e=>e.CanReceiveDamage).ToArray();var a=enemies.Select(e=>e.transform.position).ToArray();await Task.Delay(600);
            var deltas=enemies.Select((e,i)=>(Vector2)(Camera.main.WorldToViewportPoint(e.transform.position)-Camera.main.WorldToViewportPoint(a[i]))).ToArray();
            Check(deltas.All(d=>d.magnitude>.001f)&&deltas.All(d=>(d-deltas[0]).magnitude<.003f),"Idle did not move as one formation");
            foreach(var e in enemies)rig.Freeze(e);
            var sniper=enemies.First(e=>e.name.StartsWith("Sniper"));var guards=enemies.Where(e=>e.name.StartsWith("Bulwark")).ToArray();
            var lane=Camera.main.WorldToViewportPoint(sniper.transform.position).y;var guard=guards.OrderBy(e=>Mathf.Abs(Camera.main.WorldToViewportPoint(e.transform.position).y-lane)).First();
            int hp=guard.CurrentHp,sniperHp=sniper.CurrentHp;Check(hp>=48,"Region bonus missing from tank HP: "+hp);
            rig.Place(Camera.main.ViewportToWorldPoint(new Vector3(.17f,lane,-Camera.main.transform.position.z)));rig.Player.GetComponent<WeaponLoadout>().Restore(WeaponBranch.Pulse,new[]{3,2,2});
            await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);
            await Until(()=>guard.CurrentHp<hp,2000,"Front tank failed to intercept Pulse");await rig.MouseEvent(new Vector2(400,300),false);
            Check(sniper.CurrentHp==sniperHp&&guard.IsAlive,"Rear Sniper was not protected by front tank");
            var view=Camera.main.GetComponent<GameplayViewController>();Check(view.CurrentPlayableViewport.xMax<=.60f,"Right movement entered rear line");
            var bounds=rig.Player.GetComponent<PlayerMovementBounds>();var clamped=bounds.ClampPosition(Camera.main.ViewportToWorldPoint(new Vector3(1,1,-Camera.main.transform.position.z)),Camera.main,view.CurrentPlayableViewport);
            Check(Camera.main.WorldToViewportPoint(clamped).x<view.CurrentPlayableViewport.xMax,"Footprint clamp failed");
            await Capture("feedback-tank-cover-1600");
            return new{status="PASS",idleDelta=deltas.Select(d=>d.ToString("F5")).ToArray(),tankHp=hp,damageIntercepted=hp-guard.CurrentHp,sniperProtected=true,bounds=true};
        }
    });
    public static object BeginGoldRain()=>Begin("feedback-gold-rain",async()=>{
        using(var rig=new Rig(1600,6))using(var store=new Storage())
        {
            Time.timeScale=2;store.Seed(8,2);rig.Protect();await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossShieldNetwork>()!=null&&e.GetComponentsInChildren<EnemyHealth>().Any(n=>n!=e&&n.CanReceiveDamage)),4000,"Gold Warden entry");
            var boss=EnemyHealth.Active.First(e=>e.GetComponent<BossShieldNetwork>()!=null);Check(boss.MaxHp==520&&boss.GetComponentsInChildren<EnemyHealth>().Where(n=>n!=boss).All(n=>n.MaxHp==110),"Boss HP tuning missing");
            foreach(var node in boss.GetComponentsInChildren<EnemyHealth>().Where(n=>n!=boss))node.ReceivePlayerHit(node.CurrentHp,rig.Player);
            await Until(()=>boss.CanReceiveDamage,2000,"Gold collapse");boss.ReceivePlayerHit(boss.CurrentHp,rig.Player);
            await Until(()=>Runner.Stage==EncounterStage.RewardGrace,5000,"Gold staged death");Time.timeScale=1;
            var gold=UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.Item!=null&&p.Item.Kind==PickupKind.Shard).ToArray();
            Check(gold.Length==36,"Expected Warden gold rain, got "+gold.Length);Check(gold.Max(p=>p.transform.position.y)-gold.Min(p=>p.transform.position.y)>8,"Gold rain collapsed to one lane");
            await Capture("feedback-warden-gold-rain-1600");var old=gold.Select(p=>p.transform.position).ToArray();await Task.Delay(700);
            Check(gold.Select((p,i)=>p.transform.position.x<old[i].x).All(x=>x),"Gold did not drift left");
            foreach(var reward in UnityEngine.Object.FindObjectsByType<RewardPickup>())reward.Release();await Until(()=>Session.IsClear,1800,"Gold reward grace completion");
            Check(Save.PersistentSnapshot.Campaign.Status==CampaignStatus.CampaignCompleted,"Gold path lost campaign completion");
            return new{status="PASS",gold=36,spread=true,leftDrift=true,stagedDeath=true,campaignEnd=true,productionUntouched=true};
        }
    });

    public static object BeginVanguardTuning()=>Begin("feedback-vanguard",async()=>{
        using(var rig=new Rig(1600,6))using(var store=new Storage())
        {
            Time.timeScale=3;store.Seed(4,2);rig.Protect();rig.Player.GetComponent<WeaponLoadout>().Restore(WeaponBranch.Pulse,new[]{3,3,3});
            await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossPhaseRunner>()!=null),1000,"Vanguard spawn");
            var boss=EnemyHealth.Active.First(e=>e.GetComponent<BossPhaseRunner>()!=null);var phase=boss.GetComponent<BossPhaseRunner>();var spawner=boss.GetComponent<EscortSpawner>();
            Check(boss.MaxHp==680&&spawner!=null,"Vanguard tuning missing");var start=Time.fixedTime;var real=Time.realtimeSinceStartup;bool phase2=false;int peak=0,spawned=0;
            await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);
            while(boss.IsAlive&&Time.realtimeSinceStartup-real<80)
            {
                var p=Camera.main.WorldToViewportPoint(boss.transform.position);rig.Place(Camera.main.ViewportToWorldPoint(new Vector3(p.x,.17f,-Camera.main.transform.position.z)));
                phase2|=phase.IsPhaseB;peak=Math.Max(peak,spawner.LiveCount);spawned=Math.Max(spawned,spawner.SpawnCount);await Task.Delay(25);
            }
            Check(!boss.IsAlive&&phase2&&spawned>0&&peak<=3,"Vanguard real fight failed");await rig.MouseEvent(new Vector2(400,300),false);
            await Until(()=>Runner.Stage==EncounterStage.RewardGrace,3500,"Vanguard staged reward");var gold=UnityEngine.Object.FindObjectsByType<RewardPickup>().Count(p=>p.Item!=null&&p.Item.Kind==PickupKind.Shard);Check(gold==28,"Vanguard gold regression");
            return new{status="PASS",realHeldPulse=true,level=3,hp=680,seconds=Time.fixedTime-start,phase2,spawned,peakEscorts=peak,gold,productionUntouched=true};
        }
    });

    public static object BeginObjectives()=>Begin("feedback-objectives",async()=>{
        var results=new List<object>();using(var rig=new Rig(1600,6))
        foreach(var spec in new[]{(mission:2,encounter:0),(mission:3,encounter:0),(mission:6,encounter:0),(mission:8,encounter:0)})
        {
            Time.timeScale=4;Start(rig,spec.mission,spec.encounter);
            Check(Runner.Current.Completion==EncounterCompletion.Eliminate,"Objective migration missing");
            var duration=Runner.Current.Duration;await Until(()=>Runner.Elapsed>duration+.5f,16000,"Required encounter did not stay active");
            Check(Runner.Stage==EncounterStage.Running&&EnemyHealth.Active.Any(e=>e.IsAlive),"Required enemies escaped/auto-cleared");
            await Capture($"feedback-required-L{spec.mission}-1600");var elapsed=Runner.Elapsed;var real=Time.realtimeSinceStartup;
            while(Runner.HasActiveRun&&Time.realtimeSinceStartup-real<8)
            {
                foreach(var e in EnemyHealth.Active.ToArray())if(e.CanReceiveDamage)e.ReceivePlayerHit(e.CurrentHp,rig.Player);
                foreach(var p in UnityEngine.Object.FindObjectsByType<RewardPickup>())p.Release();await Task.Delay(30);
            }
            Check(!Runner.HasActiveRun,"Required clear or optional-minion cleanup stuck");
            results.Add(new{spec.mission,encounter=spec.encounter+1,heldPastSeconds=elapsed,requiredKills=true,cleanup=true});
        }
        return new{status="PASS",results};
    });

    public static object BeginBoundaryHint()=>Begin("boundary-hint",async()=>{
        var results=new List<object>();
        foreach(int mission in new[]{1,5})using(var rig=new Rig(1600,6))
        {
            Start(rig,mission,mission==1?1:0);await Until(()=>Runner.Elapsed>4.3f,6000,"Boundary setup");
            UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().Block();
            var camera=Camera.main;var view=camera.GetComponent<GameplayViewController>();var r=view.CurrentPlayableViewport;
            Check(r.xMin==0&&r.yMin==0&&(mission==1?r.width==1&&Mathf.Abs(r.yMax-.55f)<.001f:r.height==1&&Mathf.Abs(r.xMax-.59f)<.001f),"Unexpected boxed playable region "+r);
            var hint=camera.transform.Find("BoundaryHint").GetComponent<SpriteRenderer>();
            Check(!hint.enabled&&camera.GetComponentsInChildren<Transform>().All(t=>!t.name.StartsWith("RestrictedArea")),"Persistent area mask remains");
            await Capture($"feedback-boundary-idle-L{mission}-1600");
            var move=rig.Player.GetComponent<RelativeDragMovement>();var bounds=rig.Player.GetComponent<PlayerMovementBounds>();
            var limit=camera.ViewportToWorldPoint(new Vector3(mission==1?.5f:r.xMax,mission==1?r.yMax:.5f,-camera.transform.position.z));
            rig.Place(bounds.ClampPosition(limit,camera,r));
            var anchor=new Vector2(400,300);await rig.MouseEvent(anchor,false);await rig.MouseEvent(anchor,true);
            Check(!hint.enabled,"Holding without pushing showed boundary");
            var delta=mission==1?new Vector2(0,100):new Vector2(100,0);
            await rig.MouseEvent(anchor+delta,true);Check(!hint.enabled,"Boundary appeared before deliberate hold delay");
            await Task.Delay(330);Check(hint.enabled&&hint.color.a>0&&hint.color.a<=.24f,"Boundary hold feedback missing");
            var a=camera.WorldToScreenPoint(hint.bounds.min);var b=camera.WorldToScreenPoint(hint.bounds.max);
            Check(Mathf.Abs((mission==1?b.y-a.y:b.x-a.x)-2)<.15f,"Hint is not a thin two-pixel segment");
            Check(Mathf.Abs((mission==1?(b.x-a.x)/camera.pixelWidth:(b.y-a.y)/camera.pixelHeight)-.14f)<.002f,"Hint spans too much background");
            await Capture($"feedback-boundary-held-L{mission}-1600");
            await rig.MouseEvent(anchor+delta*.7f,true);await Task.Delay(180);Check(!hint.enabled&&move.BoundaryPressure==Vector2.zero,"Inward drag failed to clear hint");
            await rig.MouseEvent(anchor+delta*1.7f,true);await Task.Delay(330);Check(hint.enabled,"Repeated outward hold missing hint");
            await rig.MouseEvent(anchor+delta*1.7f,false);await Task.Delay(180);Check(!hint.enabled&&move.BoundaryPressure==Vector2.zero,"Release retained boundary hint/pressure");
            results.Add(new{mission,viewport=r.ToString(),delay=.2,thicknessPixels=2,segment=.14,idleHidden=true,inwardCleared=true});
        }
        return new{status="PASS",results};
    });
    public static object BeginHomingEmpty()=>Begin("homing-empty",async()=>{
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var results=new List<object>();
        T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,flags).GetValue(o);
        foreach(int mission in new[]{1,5})using(var rig=new Rig(1600,6))
        {
            Start(rig,mission,mission==1?1:0);await Until(()=>Runner.Elapsed>4.3f,6000,"Empty Homing setup");
            UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().Block();
            foreach(var e in EnemyHealth.Active.ToArray()){rig.Freeze(e);e.SetDamageBlocked(true);}
            var weapon=rig.Player.GetComponent<PulseLaserWeapon>();var loadout=rig.Player.GetComponent<WeaponLoadout>();
            var forward=PlayDirectionSpace.Offset(Vector2.up,Runner.Flow.Direction);var lateral=PlayDirectionSpace.Offset(Vector2.right,Runner.Flow.Direction);
            for(int level=1;level<=5;level++)
            {
                foreach(var p in UnityEngine.Object.FindObjectsByType<PlayerProjectile>())if(p.Owner==rig.Player)p.Release();
                loadout.Restore(WeaponBranch.Homing,new[]{5,5,level});
                var origin=Field<Transform>(weapon,"muzzle").position;
                await rig.MouseEvent(new Vector2(400,300),false);int fired=weapon.ShotsFired;
                await rig.MouseEvent(new Vector2(400,300),true);await Until(()=>weapon.ShotsFired>=fired+level,1500,"Empty volley did not fire");await rig.MouseEvent(new Vector2(400,300),false);
                var shots=UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Where(p=>p.Owner==rig.Player&&Field<bool>(p,"flying")).ToArray();
                Check(shots.Length==level&&shots.All(p=>Field<EnemyHealth>(p,"trackingTarget")==null),"Empty volley acquired a target/count mismatch");
                await Task.Delay(350);
                var x=shots.Select(p=>Vector2.Dot(p.transform.position-origin,lateral)).OrderBy(v=>v).ToArray();
                var y=shots.OrderBy(p=>Vector2.Dot(p.transform.position-origin,lateral)).Select(p=>Vector2.Dot(p.transform.position-origin,forward)).ToArray();
                for(int i=0;i<level;i++){Check(Mathf.Abs(x[i]+x[level-1-i])<.003f,"Asymmetric Homing fan "+string.Join(",",x));Check(Mathf.Abs(y[i]-y[level-1-i])<.003f,"Asymmetric Homing depth");}
                if(level%2==1)Check(Mathf.Abs(x[level/2])<.001f,"Odd Homing center left axis");
                var gaps=x.Skip(1).Select((v,i)=>v-x[i]).ToArray();if(gaps.Length>1)Check(gaps.Max()/gaps.Min()<1.15f,"Uneven Homing fan gaps");
                if(level==5)await Capture($"feedback-homing-empty-L{mission}-lv5-1600");
                results.Add(new{mission,level,lateral=x,centerStraight=level%2==1,gaps});
            }
        }
        return new{status="PASS",results};
    });

    public static object BeginThreatProtection()=>Begin("closure-threats",async()=>{
        var results=new List<object>();
        foreach(var spec in new[]{(m:2,e:0),(m:3,e:2),(m:6,e:0),(m:6,e:2),(m:7,e:1),(m:7,e:2),(m:8,e:0)})
        foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})
        using(var rig=new Rig(1600,6))
        {
            Time.timeScale=2;Start(rig,spec.m,spec.e);var weapon=rig.Player.GetComponent<PulseLaserWeapon>();weapon.ResetForSession();
            rig.Player.GetComponent<WeaponLoadout>().Restore(branch,new[]{3,3,3});
            await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<DirectedAttack>()!=null),2000,"Threat spawn");
            var threat=EnemyHealth.Active.First(e=>e.GetComponent<DirectedAttack>()!=null);var attack=threat.GetComponent<DirectedAttack>();
            var guards=EnemyHealth.Active.Where(e=>e.name.StartsWith("Bulwark")).ToArray();Check(guards.Length>0,"Threat has no frontline");
            await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);
            var real=Time.realtimeSinceStartup;
            while(threat.IsAlive&&attack.AttacksFired==0&&Time.realtimeSinceStartup-real<10)
            {
                var lane=PlayDirectionSpace.LocalPoint(Camera.main.WorldToViewportPoint(threat.transform.position),Runner.Flow.Direction).x;
                var p=PlayDirectionSpace.Point(new Vector2(lane,spec.m>=5?.25f:.17f),Runner.Flow.Direction);
                rig.Place(Camera.main.ViewportToWorldPoint(new Vector3(p.x,p.y,-Camera.main.transform.position.z)));await Task.Delay(25);
            }
            Check(threat.IsAlive&&attack.AttacksFired>0,$"Threat died/waited before first attack L{spec.m} E{spec.e+1} {branch}: HP={threat.CurrentHp}, clock={Runner.Elapsed}");
            Check(guards.Any(g=>g.IsAlive),"All guards lost before first threat attack");
            await rig.MouseEvent(new Vector2(400,300),false);
            if(branch==WeaponBranch.Pulse)await Capture($"closure-guard-L{spec.m}-E{spec.e+1}-1600");
            results.Add(new{spec.m,encounter=spec.e+1,branch=branch.ToString(),threat=threat.name,hp=threat.CurrentHp,firstAttackSeconds=Runner.Elapsed,guardsAlive=guards.Count(g=>g.IsAlive)});
            Evidence("closure-threats-live",new{status="RUNNING",results});
        }
        return new{status="PASS",results};
    });
    public static object BeginBossBarrage()=>Begin("closure-barrages",async()=>{
        var results=new List<object>();
        foreach(int width in new[]{1600,2000})foreach(int mission in new[]{4,8})using(var rig=new Rig(width,6))
        {
            Start(rig,mission,1);await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossPhaseRunner>()!=null&&e.GetComponent<EntryHoldFlight>().IsHolding),6000,"Barrage boss arrival");
            var boss=EnemyHealth.Active.First(e=>e.GetComponent<BossPhaseRunner>()!=null);var runner=boss.GetComponent<BossPhaseRunner>();
            var controls=EnemyHealth.Active.Select(e=>(Behaviour)e.GetComponent<SingleFireAttack>()).Where(c=>c!=null)
                .Concat(boss.GetComponentsInChildren<EscortSpawner>().Cast<Behaviour>()).Concat(new Behaviour[]{runner}).Distinct().ToArray();
            var enabledStates=controls.Select(c=>c.enabled).ToArray();
            try
            {
            runner.enabled=false;
            bool shieldHud=true;
            if(mission==8)
            {
                var network=boss.GetComponent<BossShieldNetwork>();var node=boss.GetComponentsInChildren<EnemyHealth>().First(e=>e!=boss);
                node.ReceivePlayerHit(10,rig.Player);await Task.Delay(80);
                var hud=UnityEngine.Object.FindAnyObjectByType<StarShift.UI.BossHud>();
                var fill=(UnityEngine.UI.Image)typeof(StarShift.UI.BossHud).GetField("fill",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
                shieldHud=Mathf.Abs(fill.fillAmount-network.CurrentShieldHp/(float)network.MaximumShieldHp)<.001f;Check(shieldHud,"HUD did not show shield damage");
            }
            foreach(var sp in boss.GetComponentsInChildren<EscortSpawner>())sp.enabled=false;
            foreach(var e in EnemyHealth.Active.ToArray()){e.SetDamageBlocked(true);var a=e.GetComponent<SingleFireAttack>();if(a!=null)a.enabled=false;}
            var fire=UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();fire.ClearProjectiles();
            var barrage=boss.GetComponentInChildren<BossBarrage>();Check(barrage!=null,"Boss has no new special");
            Vector3 At(float lateral){var p=PlayDirectionSpace.Point(new Vector2(lateral,.17f),Runner.Flow.Direction);return Camera.main.ViewportToWorldPoint(new Vector3(p.x,p.y,-Camera.main.transform.position.z));}
            rig.Unprotect();int lives=rig.Lives.CurrentLives;Check(barrage.BeginAttack(),"Special did not start");
            await rig.DragTo(At(barrage.SafeLane));await rig.MouseEvent(new Vector2(400,300),false);
            Session.Pause();var fixedTime=Time.fixedTime;var lane=barrage.SafeLane;await Task.Delay(180);Check(Time.fixedTime==fixedTime&&barrage.SafeLane==lane&&!barrage.IsActive,"Barrage advanced in Pause");Session.Resume();
            float lastLane=barrage.SafeLane;int relocations=1;var real=Time.realtimeSinceStartup;bool captured=false;
            while(barrage.IsBusy&&Time.realtimeSinceStartup-real<14)
            {
                if(!barrage.IsActive&&lastLane!=barrage.SafeLane){lastLane=barrage.SafeLane;await rig.DragTo(At(lastLane));await rig.MouseEvent(new Vector2(400,300),false);relocations++;}
                if(!captured&&barrage.IsActive)
                {
                    await Task.Delay(40);
                    var binding=BindingFlags.Instance|BindingFlags.NonPublic;
                    var lower=(AttackBeamPresentation)typeof(BossBarrage).GetField("lowerBandPresentation",binding).GetValue(barrage);
                    var upper=(AttackBeamPresentation)typeof(BossBarrage).GetField("upperBandPresentation",binding).GetValue(barrage);
                    var lowerLine=(LineRenderer)typeof(AttackBeamPresentation).GetField("core",binding).GetValue(lower);
                    var upperLine=(LineRenderer)typeof(AttackBeamPresentation).GetField("core",binding).GetValue(upper);
                    bool right=Runner.Flow.Direction==PlayDirection.Right;
                    float lowerEdge=right?lowerLine.GetPosition(0).y:lowerLine.GetPosition(0).x;
                    float upperEdge=right?upperLine.GetPosition(0).y:upperLine.GetPosition(0).x;
                    float expectedLower=right?barrage.LowerBounds.min.y:barrage.LowerBounds.max.x;
                    float expectedUpper=right?barrage.UpperBounds.max.y:barrage.UpperBounds.min.x;
                    Evidence("closure-barrage-geometry",new{mission,width,lowerEdge,upperEdge,expectedLower,expectedUpper,lower=lower.name,upper=upper.name});
                    Check(Mathf.Abs(lowerEdge-expectedLower)<.001f&&Mathf.Abs(upperEdge-expectedUpper)<.001f,"Visible shelter edges differ from damage gap");
                    await Capture($"closure-boss-{mission}-shelter-{width}");captured=true;
                }
                Check(rig.Lives.CurrentLives==lives,"Marked shelter killed player L"+mission);await Task.Delay(20);
            }
            Check(!barrage.IsBusy&&barrage.PulsesFired==3,"Barrage pulse count/termination");
            if(mission==8)Check(relocations==3,"Ion shelter failed to relocate");
            Check(barrage.BeginAttack(),"Second barrage did not begin");
            await Until(()=>rig.Lives.CurrentLives<lives,14000,"Standing outside shelter never dealt damage L"+mission);
            Check(rig.Lives.CurrentLives==lives-1,"Barrage damage resolved more than one life");
            barrage.CancelAttack();Check(!barrage.IsBusy&&!barrage.IsActive,"Barrage cancel retained state");
            var lines=barrage.GetComponentsInChildren<LineRenderer>();Check(lines.All(l=>!l.enabled),"Barrage cancel retained visuals");
            results.Add(new{mission,width,shieldHud,pulses=3,relocations,safeSurvival=true,dangerDamage=true,pause=true,cancel=true,shots=barrage.ShotsFired,scale=boss.transform.localScale.ToString()});
            }
            finally {for(int index=0;index<controls.Length;index++)if(controls[index]!=null)controls[index].enabled=enabledStates[index];}
        }
        return new{status="PASS",results};
    });

}
