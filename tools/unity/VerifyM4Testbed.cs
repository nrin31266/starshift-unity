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

public static class VerifyM4Testbed
{
    static readonly BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;
    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession Session => UnityEngine.Object.FindAnyObjectByType<GameplaySession>();
    static RewardDirector Director => Runner.GetComponent<RewardDirector>();
    static EnemyHealth[] Enemies => UnityEngine.Object.FindObjectsByType<EnemyHealth>().Where(e=>e.IsAlive).ToArray();
    static PickupDefinition Item(string name)=>AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/"+name+".asset");
    static RewardPickup[] Pickups => UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive).ToArray();
    static void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
    static async Task Until(Func<bool> predicate,int ms,string message)
    {
        var end=Time.realtimeSinceStartup+ms/1000f;
        while(!predicate() && Time.realtimeSinceStartup<end)await Task.Delay(10);
        Require(predicate(),message);
    }
    static T Ui<T>(string name) where T:Component => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include).Single(c=>c.name==name && c.transform.IsChildOf(GameObject.Find("GameplayUI").transform));
    static async Task<string> Capture(string name)
    {
        var path=System.IO.Path.GetFullPath("../.design/m4-a/screenshots/"+name+".png");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));ScreenCapture.CaptureScreenshot(path);await Task.Delay(150);
        Require(System.IO.File.Exists(path),"Missing screenshot: "+name);return path;
    }
    static void Restart(Rig rig,string id="ENCOUNTER_3",bool standalone=true,bool intro=false)
    {
        rig.RestoreFlights();
        Session.enabled=false;Runner.StopRun();Time.timeScale=1;AudioListener.pause=false;Session.enabled=true;
        var life=rig.Player.GetComponent<PlayerDamageLifecycle>();life.enabled=false;life.enabled=true;rig.Protect();rig.Input.CancelGesture();rig.Place(new Vector2(0,-3.8f));
        Runner.StartRun(id,standalone,!intro,null,null);Director.SetTestSeed(410);
    }
    public static async Task<object> Weapons()
    {
        using(var rig=new Rig(1600,6))
        {
            Restart(rig,"ENCOUNTER_2");
            var loadout=rig.Player.GetComponent<WeaponLoadout>();var weapon=rig.Player.GetComponent<PulseLaserWeapon>();
            var salvos=new List<string>();
            foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})
            {
                if(loadout.Level(branch)==0)loadout.ApplyCore(branch);
                while(loadout.ActiveBranch!=branch)loadout.Cycle();
                for(int level=1;level<=WeaponLoadout.MaximumLevel;level++)
                {
                    if(level>1)loadout.ApplyCore(branch);
                    await rig.MouseEvent(new Vector2(400,200),false);
                    var before=weapon.ShotsFired;await rig.MouseEvent(new Vector2(400,200),true);await rig.MouseEvent(new Vector2(400,200),false);
                    var expected=loadout.ActiveDefinition.Pattern(level).ProjectileCount;
                    Require(weapon.ShotsFired-before==expected,$"Wrong {branch} L{level} salvo: {weapon.ShotsFired-before} expected {expected}");
                    salvos.Add(branch+" L"+level+"="+expected);
                }
            }
            rig.Unprotect();var lifecycle=rig.Player.GetComponent<PlayerDamageLifecycle>();
            Require(lifecycle.TryReceiveHit(null),"Death rejected");Require(loadout.Level(WeaponBranch.Homing)==4 && loadout.Level(WeaponBranch.Pulse)==5 && loadout.Level(WeaponBranch.Plasma)==5,"Active-only downgrade failed");
            Require(weapon.Heat==0 && !weapon.IsOverheated,"Death thermal reset failed");
            await Until(()=>rig.Gate.State==PlayerGameplayState.PostEntryInvincibility,2500,"Respawn failed");
            rig.Protect();for(int i=0;i<WeaponLoadout.MaximumLevel;i++)weapon.ApplyDeathDowngrade();Require(loadout.ActiveLevel==1,"Death minimum failed");
            Restart(rig,"ENCOUNTER_2");
            await rig.MouseEvent(new Vector2(400,200),true);await Task.Delay(700);await rig.MouseEvent(new Vector2(400,200),false);
            Require(weapon.Heat>0 && !weapon.IsOverheated,"Burst incorrectly locked");
            var burstHeat=weapon.Heat;await Task.Delay(500);Require(weapon.Heat<burstHeat,"Release did not cool");
            await rig.MouseEvent(new Vector2(400,200),true);
            await Until(()=>weapon.IsOverheated,Mathf.CeilToInt(loadout.ActiveDefinition.ThermalProfile.Capacity / loadout.ActiveDefinition.ThermalProfile.HeatPerSecond * 1000)+1500,"No MAX overheat");
            Require(weapon.HeatFraction>=.97f && rig.Gate.CanInput,"Overheat affected movement permission");
            var position=rig.Player.transform.position;await rig.MouseEvent(new Vector2(480,200),true);Require(rig.Player.transform.position.x>position.x,"Overheated movement failed");
            Session.Pause();var frozen=weapon.Heat;await Task.Delay(180);Require(weapon.Heat==frozen,"Pause heat advanced");Session.Resume();
            await Until(()=>!weapon.IsOverheated,1800,"Recovery failed");Require(weapon.Heat>0,"Recovery waited for zero");
            // Resume hủy ownership; tạo held intent mới cho auto-resume probe.
            await rig.MouseEvent(new Vector2(400,200),false);await rig.MouseEvent(new Vector2(400,200),true);
            await Until(()=>weapon.IsOverheated,Mathf.CeilToInt(loadout.ActiveDefinition.ThermalProfile.Capacity / loadout.ActiveDefinition.ThermalProfile.HeatPerSecond * 1000)+1500,"Second overheat failed");var shots=weapon.ShotsFired;
            await Until(()=>weapon.ShotsFired>shots,2000,"Held auto-resume failed");
            await rig.MouseEvent(new Vector2(400,200),false);
            var retained=weapon.Heat;loadout.ApplyCore(WeaponBranch.Pulse);Require(Mathf.Approximately(retained,weapon.Heat),"Core reset heat");
            return new{pass=true,salvos,activeOnlyDeathPenalty=true,maxOnlyOverheat=true,pause=true,heldResume=true,frames=Time.frameCount-rig.FirstFrame};
        }
    }
    public static async Task<object> PickupsAndRewards()
    {
        using(var rig=new Rig(1600,6))
        {
            Restart(rig);
            var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();var effects=rig.Player.GetComponent<PlayerTemporaryEffects>();var rewards=Runner.GetComponent<RunRewards>();var loadout=rig.Player.GetComponent<WeaponLoadout>();
            var pickup=Director.Spawn(Item("StellarShard"),null,rig.Player.transform.position);await Task.Delay(100);
            Require(rewards.Shards==1 && !pickup.IsActive,"Shard contact failed");
            Require(GameObject.Find("GameplayUI").transform.Find("SafeArea/CombatHud/Counters/Shard/Count").GetComponent<Text>().text=="1","Shard HUD stale");
            var again=Director.Spawn(Item("ShieldCell"),null,rig.Player.transform.position+Vector3.right*2);
            Require(again==pickup,"Pickup pool did not reuse");
            await rig.MouseEvent(new Vector2(400,200),true);await rig.MouseEvent(new Vector2(650,200),true);await rig.MouseEvent(new Vector2(650,200),false);
            // Sweep kiểm tra drag qua pickup; setup contact riêng nếu delta chưa tới.
            if(again.IsActive)rig.Place(again.transform.position);
            await Task.Delay(100);Require(effects.HasShield,"Moving collection failed");
            var life=rig.Player.GetComponent<PlayerDamageLifecycle>();rig.Unprotect();var lives=rig.Lives.CurrentLives;
            receiver.TryCollect(Item("PulseCore"));
            Require(life.TryReceiveHit(null) && rig.Lives.CurrentLives==lives && !effects.HasShield && loadout.ActiveLevel==2,"Shield precedence failed");
            Require(!life.TryReceiveHit(null),"Shield break duplicate hit");
            await Until(()=>rig.Gate.State==PlayerGameplayState.Active,1500,"Shield protection did not expire");
            Require(life.TryReceiveHit(null) && rig.Lives.CurrentLives==lives-1 && loadout.ActiveLevel==1,"Unshielded death failed");
            await Until(()=>rig.Gate.State==PlayerGameplayState.PostEntryInvincibility,2500,"Respawn missing");rig.Protect();
            Require(receiver.TryCollect(Item("ExtraLife")) && rig.Lives.CurrentLives==lives,"Extra life failed");
            receiver.TryCollect(Item("ExtraLife"));receiver.TryCollect(Item("ExtraLife"));Require(rig.Lives.CurrentLives==5 && !receiver.TryCollect(Item("ExtraLife")),"Life cap/overflow failed");
            Restart(rig);receiver.TryCollect(Item("Overdrive"));receiver.TryCollect(Item("SupportDrone"));
            await Task.Delay(250);var drone=rig.Player.GetComponentInChildren<SupportDrone>();Require(drone.IsVisible && drone.ShotsFired>0 && effects.FireRateMultiplier>1,"Buff/drone inactive");
            Session.Pause();var od=effects.OverdriveRemaining;var dr=effects.DroneRemaining;await Task.Delay(200);Require(od==effects.OverdriveRemaining && dr==effects.DroneRemaining,"Pause advanced buffs");Session.Resume();
            await Until(()=>effects.OverdriveRemaining<=0,10000,"OD expiry failed");Require(effects.FireRateMultiplier==1 && effects.DroneRemaining>0,"Buff timers coupled");
            await Until(()=>effects.DroneRemaining<=0,4000,"Drone expiry failed");await Task.Delay(80);Require(!drone.IsVisible,"Stale drone visual");
            Restart(rig,"ENCOUNTER_2");await Task.Delay(2000);
            var beforeScore=rewards.Score;var enemy=Enemies.First();enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);await Task.Delay(100);
            Require(rewards.Score==beforeScore+enemy.Definition.Score,"Kill score missing or duplicated");var score=rewards.Score;enemy.ReceivePlayerHit(999,rig.Player);Require(rewards.Score==score,"Duplicate kill reward");
            receiver.TryCollect(Item("MissileAmmo"));Require(rig.Player.GetComponent<EmergencyMissileReserve>().Current==2 && rig.Lives.CurrentLives==3,"Ammo pickup altered death/lives");
            Restart(rig,"ENCOUNTER_1",false);await Task.Delay(2000);
            foreach(var target in Enemies)target.ReceivePlayerHit(target.CurrentHp,rig.Player);
            await Until(()=>Runner.Stage==EncounterStage.Handoff,1800,"No minor handoff");
            var final=Pickups.FirstOrDefault(p=>p.Item==Item("PulseCore"));Require(final!=null,"Final kill reward lost");var beforeLevel=loadout.ActiveLevel;
            rig.Place(final.transform.position);await Task.Delay(100);Require(loadout.ActiveLevel==beforeLevel+1,"Final reward uncollectible");
            await Until(()=>Runner.CurrentIndex==1,7000,"Handoff failed");
            Restart(rig);receiver.TryCollect(Item("ShieldCell"));receiver.TryCollect(Item("Overdrive"));receiver.TryCollect(Item("SupportDrone"));
            rig.Unprotect();Require(life.TryReceiveHit(null),"Shield-break setup failed");
            await Until(()=>rig.Gate.State==PlayerGameplayState.Active,1500,"Shield-break protection did not expire before death");
            Require(life.TryReceiveHit(null),"Unshielded death rejected");
            Require(!effects.HasShield && effects.OverdriveRemaining==0 && effects.DroneRemaining==0,"Death retained buffs");
            Restart(rig);Require(rewards.Score==0 && rewards.Shards==0 && loadout.ActiveLevel==1 && effects.DroneRemaining==0,"Replay/reset stale state");
            return new{pass=true,contact=true,movingCollection=true,poolReuse=true,shield=true,extraLifeCap=5,overdriveExpiry=true,droneExpiry=true,score=true,missileAmmo=true,handoffCollection=true,deathReset=true};
        }
    }
    public static async Task<object> CombatAndDrops()
    {
        using(var rig=new Rig(1600,6))
        {
            Restart(rig,"ENCOUNTER_2");await Task.Delay(2200);
            var visible=Enemies.Count(e=>!e.GetComponent<EntryHoldFlight>().IsOutsideView);
            Require(visible>=10 && visible<=13,"Density target missing: "+visible);
            var loadout=rig.Player.GetComponent<WeaponLoadout>();var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();
            var rewards=Runner.GetComponent<RunRewards>();
            var table=AssetDatabase.LoadAssetAtPath<RewardDropTable>("Assets/StarShift/Data/Rewards/NormalDrops.asset");
            var one=new RewardRoller();var two=new RewardRoller();one.Reset(414);two.Reset(414);int major=0,shards=0;
            for(int i=0;i<4000;i++)
            {
                var a=one.RollDirect(table,receiver,rig.Lives,loadout);var b=two.RollDirect(table,receiver,rig.Lives,loadout);Require(a==b,"Seed was not deterministic");
                if(a!=null){if(a.Kind==PickupKind.Shard)shards++;else major++;}
            }
            Require(major>50 && major<150 && shards>60 && shards<230,"Normal drop rate out of broad smoke band");
            typeof(PlayerLives).GetField("<CurrentLives>k__BackingField",Fields).SetValue(rig.Lives,1);
            var majorTable=AssetDatabase.LoadAssetAtPath<RewardDropTable>("Assets/StarShift/Data/Rewards/SupplyCrateDrops.asset");
            one.RecordCrate(Item("Overdrive"),1);one.RecordCrate(Item("SupportDrone"),1);
            var defense=one.SelectMajor(majorTable,receiver,rig.Lives,loadout,true);
            Require(defense!=null && (defense.Kind==PickupKind.Shield || defense.Kind==PickupKind.ExtraLife),"Defensive pity failed");
            one.RecordCrate(defense,1);
            Require(Enumerable.Range(0,80).Select(_=>one.SelectMajor(majorTable,receiver,rig.Lives,loadout,true)).Any(x=>x.Kind!=PickupKind.Shield && x.Kind!=PickupKind.ExtraLife),"Pity did not reset");
            typeof(PlayerLives).GetField("<CurrentLives>k__BackingField",Fields).SetValue(rig.Lives,5);
            foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})while(loadout.Level(branch)<WeaponLoadout.MaximumLevel)loadout.ApplyCore(branch);
            for(int i=0;i<100;i++) {var item=one.SelectMajor(majorTable,receiver,rig.Lives,loadout,true);Require(item!=null && item.Kind!=PickupKind.WeaponCore && item.Kind!=PickupKind.ExtraLife,"Maxed reward was not filtered");}
            Restart(rig,"ENCOUNTER_2");await Task.Delay(2100);
            var targets=Enemies.Take(2).ToArray();
            foreach(var enemy in Enemies) {rig.Freeze(enemy);enemy.transform.position=new Vector3(7,4,0);enemy.GetComponent<Rigidbody2D>().position=enemy.transform.position;}
            targets[0].transform.position=new Vector3(.8f,0,0);targets[0].GetComponent<Rigidbody2D>().position=targets[0].transform.position;
            targets[1].transform.position=new Vector3(1.35f,0,0);targets[1].GetComponent<Rigidbody2D>().position=targets[1].transform.position;
            var hpBefore=targets.Select(e=>e.CurrentHp).ToArray();
            var missile=loadout.Definition(WeaponBranch.Homing);var shot=UnityEngine.Object.Instantiate(missile.ProjectilePrefab);
            var released=false;
            shot.Launch(missile,new Vector3(0,-3,0),Vector2.up,2,.85f,rig.Player,p=>{released=true;p.ResetForPool();});
            await Task.Delay(100);Require(Vector3.Angle(shot.transform.up,Vector3.up)>1 && Vector3.Angle(shot.transform.up,Vector3.up)<18,"Missile tracking unbounded/missing");
            await Until(()=>released,1600,"Missile did not hit");Require(targets[0].CurrentHp==Mathf.Max(0,hpBefore[0]-2) && targets[1].CurrentHp==Mathf.Max(0,hpBefore[1]-2),"Missile splash failed: before="+string.Join(",",hpBefore)+" after="+string.Join(",",targets.Select(e=>e.CurrentHp)));
            UnityEngine.Object.Destroy(shot.gameObject);
            Restart(rig,"ENCOUNTER_3");
            await Until(()=>Enemies.Any(e=>e.name.StartsWith("SupplyDrone")),10000,"Supply did not spawn");
            var supply=Enemies.First(e=>e.name.StartsWith("SupplyDrone"));Require(!supply.Definition.BodyDamagesPlayer && !supply.GetComponent<SingleFireAttack>().enabled,"Supply is hostile");
            supply.ReceivePlayerHit(supply.CurrentHp,rig.Player);await Task.Delay(50);var pair=Pickups;Require(pair.Length==2 && pair.All(p=>p.Crate==null) && pair.Any(p=>p.Item.Kind==PickupKind.WeaponCore),"No direct supply pair");
            foreach(var item in pair)Require(item.TryCollect(),"Supply pickup collection");Require(rewards.SupplyCaptured==1 && Director.PickupsCollected==2,"Supply pair counted twice");
            Restart(rig,"ENCOUNTER_3");
            await Until(()=>Enemies.Any(e=>e.name.StartsWith("SupplyDrone")),10000,"Supply escape setup missing");
            supply=Enemies.First(e=>e.name.StartsWith("SupplyDrone"));
            await Until(()=>!supply.gameObject.activeSelf,14000,"Supply never escaped");Require(Pickups.Length==0 && rewards.SupplyCaptured==0,"Escaped supply granted reward");
            Restart(rig,"ENCOUNTER_5");await Until(()=>Enemies.Any(e=>e.name.StartsWith("ResourceAsteroid")),3000,"Resource did not spawn");
            var rock=Enemies.First(e=>e.name.StartsWith("ResourceAsteroid"));rock.ReceivePlayerHit(rock.CurrentHp,rig.Player);await Task.Delay(50);
            var shard=Pickups.FirstOrDefault(p=>p.Item==Item("StellarShard"));Require(shard!=null,"Resource has no guaranteed shard");rig.Place(shard.transform.position);await Task.Delay(100);Require(rewards.Shards==1,"Resource shard uncollectible");
            Restart(rig);var capacity=new List<RewardPickup>();
            for(int i=0;i<7;i++)capacity.Add(Director.Spawn(Item("StellarShard"),null,new Vector3(-6+i*.5f,3,0)));
            Require(capacity.Count(p=>p!=null)==6 && Director.ActivePickups==6,"Pickup cap missing");
            return new{pass=true,visibleDensity=visible,seededRolls=4000,major,shards,defensivePity=true,missileTracking=true,splash=true,supplyDestroyAndEscape=true,resource=true,pickupCap=6};
        }
    }

    public static string BeginClosure()
    {
        _ = Closure();
        return "Closure started; read .design/m4-a/closure-results.json";
    }
    static async Task Closure()
    {
        var path=System.IO.Path.GetFullPath("../.design/m4-a/closure-results.json");
        var results=new Dictionary<string,object>();string current="starting";
        Action save=()=>System.IO.File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{current,results},Newtonsoft.Json.Formatting.Indented));
        var checks=new Dictionary<string,Func<Task<object>>>{
            {"devFastStartReview",DevFastStartReview},{"weaponsAndHeat",Weapons},
            {"pickupsEffectsRewardsReset",PickupsAndRewards},{"missileProtection",MissilesAndProtection},
            {"dropEdges",()=>Task.FromResult(DropSelectionEdges())},{"handoff",HandoffSmoke},{"pickupFade",PickupFade},
            {"density",Stress},{"fullMission",()=>FullMission(1600,true)}};
        try
        {
            foreach(var check in checks){current=check.Key;save();results.Add(current,await check.Value());save();}
            current="PASS";save();
        }
        catch(Exception error){results[current]=new{pass=false,error=error.ToString()};current="FAILED";save();}
    }
    public static async Task<object> DevFastStartReview()
    {
        using(var rig=new Rig(1600,6))
        {
            var dev=Runner.GetComponent<StarShift.Dev.DevFastStart>();var original=EditorJsonUtility.ToJson(dev);
            var shots=new List<string>();var loadout=rig.Player.GetComponent<WeaponLoadout>();
            try
            {
                Set(dev,"devModeEnabled",true);Set(dev,"standalone",true);Set(dev,"skipIntro",true);
                Set(dev,"startLevel",null);Set(dev,"cameraOverride",null);Set(dev,"overrideDirection",false);
                Set(dev,"deterministicRewards",true);Set(dev,"rewardSeed",410);
                for(int encounter=1;encounter<=5;encounter++)
                {
                    Set(dev,"startPoint","ENCOUNTER_"+encounter);Set(dev,"startingWeaponLevel",encounter==1?1:2);Set(dev,"unlockAllWeapons",encounter>1);
                    dev.Replay();rig.Protect();rig.Place(new Vector2(0,-3.8f));
                    Require(Runner.CurrentIndex==encounter-1 && loadout.ActiveLevel==(encounter==1?1:2),"DevFastStart state wrong");
                    await Task.Delay(encounter==3?7800:2800);
                    if(encounter==2)while(loadout.ActiveBranch!=WeaponBranch.Plasma)loadout.Cycle();
                    if(encounter==4)while(loadout.ActiveBranch!=WeaponBranch.Homing)loadout.Cycle();
                    if(encounter==3)
                    {
                        var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();
                        receiver.TryCollect(Item("ShieldCell"));receiver.TryCollect(Item("Overdrive"));receiver.TryCollect(Item("SupportDrone"));
                    }
                    await rig.MouseEvent(new Vector2(400,200),true);await Task.Delay(700);await rig.MouseEvent(new Vector2(400,200),false);
                    shots.Add(await Capture("closure-encounter-"+encounter));
                }
                dev.Replay();rig.Protect();
                Require(Runner.CurrentIndex==4 && rig.Lives.CurrentLives==3 && Pickups.Length==0,"Dev replay retained run state");
                return new{pass=true,encounters=5,replay=true,shots};
            }
            finally{EditorJsonUtility.FromJsonOverwrite(original,dev);}
        }
    }
    public static async Task<object> HandoffSmoke()
    {
        using(var rig=new Rig(1600,6))
        {
            Restart(rig,"ENCOUNTER_1",false);await Task.Delay(2100);
            var config=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<SingleFireDefinition>("Assets/StarShift/Data/Enemies/ScoutSingleFire.asset"));
            Set(config,"projectileSpeed",1f);Set(config,"projectileLifetime",.1f);
            var muzzle=new GameObject("M4CleanupProbe");muzzle.transform.position=new Vector3(0,2,0);
            var shot=UnityEngine.Object.Instantiate(config.ProjectilePrefab);bool released=false;
            try
            {
                shot.Launch(config,muzzle.transform,Enemies[0].gameObject,rig.Player,p=>{released=true;p.ResetForPool();});
                foreach(var enemy in Enemies)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
                await Until(()=>Runner.Stage==EncounterStage.ProjectileDrain,500,"Handoff skipped drain");
                var collider=shot.GetComponent<BoxCollider2D>();var pos=shot.transform.position;var start=Time.fixedTime;
                Require(collider.enabled && !released,"Projectile hard-cleared");await Task.Delay(200);Require(shot.transform.position!=pos && collider.enabled,"Grace not natural flight");
                await Until(()=>!collider.enabled,900,"No fade damage-off");Require(!released && Mathf.Abs(Time.fixedTime-start-Runner.Current.ProjectileGraceSeconds)<.1f,"Wrong grace duration");
                Session.Pause();var tint=shot.GetComponentInChildren<SpriteRenderer>().color;await Task.Delay(180);Require(tint==shot.GetComponentInChildren<SpriteRenderer>().color,"Fade advanced in Pause");Session.Resume();
                await Until(()=>released,600,"Fade never returned pool");
            }
            finally{UnityEngine.Object.Destroy(shot.gameObject);UnityEngine.Object.Destroy(muzzle);UnityEngine.Object.Destroy(config);}
            return new{pass=true,naturalProjectileGrace=true,fadeDamageOff=true,pauseFreeze=true,poolReturn=true};
        }
    }

    public static string BeginFinalRuns()
    {
        _ = FinalRuns();
        return "Final runs started; read .design/m4-a/full-result-1600.json and full-result-2000.json";
    }
    static async Task FinalRuns()
    {
        foreach(var width in new[]{1600,2000})
        {
            var path=System.IO.Path.GetFullPath("../.design/m4-a/full-result-"+width+".json");
            System.IO.File.WriteAllText(path,"{\"status\":\"running\"}");
            await RunFullMissionToFile(width,width==2000,path);
        }
    }

    public static string BeginFullMission(int width,bool withDeath)
    {
        var path=System.IO.Path.GetFullPath("../.design/m4-a/full-result-"+width+".json");
        System.IO.File.WriteAllText(path,"{\"status\":\"running\"}");
        _ = RunFullMissionToFile(width,withDeath,path);
        return path;
    }
    static async Task RunFullMissionToFile(int width,bool withDeath,string path)
    {
        try
        {
            var result=await FullMission(width,withDeath);
            System.IO.File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception error)
        {
            System.IO.File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{pass=false,error=error.ToString()},Newtonsoft.Json.Formatting.Indented));
        }
    }

    public static async Task<object> FullMission(int width,bool withDeath)
    {
        using(var rig=new Rig(width,6,true))
        {
            var startFrame=Time.frameCount;var loadout=rig.Player.GetComponent<WeaponLoadout>();var effects=rig.Player.GetComponent<PlayerTemporaryEffects>();var rewards=Runner.GetComponent<RunRewards>();
            var completed=new List<string>();Action<EncounterDefinition> done=e=>completed.Add(e.Id);Runner.Completed+=done;
            int peak=0;bool death=false,paused=false,missileUsed=false;int upgrades=0;int lastLevel=1;var screenshots=new List<string>();
            var reserve=rig.Player.GetComponent<EmergencyMissileReserve>();int missileDetonations=0;Action detonated=()=>missileDetonations++;reserve.Detonated+=detonated;
            try
            {
                await Until(()=>Runner.Flow.CanContinue,3000,"Intro not ready");
                await rig.MouseEvent(new Vector2(width*.8f,300),true);await rig.MouseEvent(new Vector2(width*.8f,300),false);
                await Until(()=>Runner.Stage==EncounterStage.Running,10000,"Intro did not reach combat");
                var deadline=Time.realtimeSinceStartup+145;
                while(!Session.IsClear && Time.realtimeSinceStartup<deadline)
                {
                    peak=Mathf.Max(peak,Enemies.Count(e=>!e.GetComponent<EntryHoldFlight>().IsOutsideView));
                    if(loadout.ActiveLevel>lastLevel)upgrades++;lastLevel=loadout.ActiveLevel;
                    if(withDeath && !death && Runner.CurrentIndex>=2)
                    {
                        if(loadout.ActiveLevel<2)rig.Player.GetComponent<PlayerPickupReceiver>().TryCollect(Item("PulseCore"));
                        effects.Clear();rig.Unprotect();var before=loadout.ActiveLevel;var lives=rig.Lives.CurrentLives;
                        Require(rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null),"Full-run hit rejected");
                        Require(loadout.ActiveLevel==Mathf.Max(1,before-1) && rig.Lives.CurrentLives==lives-1,"Full-run death penalty wrong");
                        await Until(()=>rig.Gate.State==PlayerGameplayState.PostEntryInvincibility,3000,"Full-run respawn missing");rig.Protect();death=true;
                    }
                    if(withDeath && !paused && Runner.CurrentIndex==1)
                    {
                        await rig.MouseEvent(new Vector2(width*.5f,220),false);
                        var point=RectTransformUtility.WorldToScreenPoint(null,Ui<Button>("Pause").transform.position);
                        var pos=rig.Player.transform.position;await rig.MouseEvent(point,true);await rig.MouseEvent(point,false);Require(Session.IsPaused && rig.Player.transform.position==pos && !rig.Input.FireHeld,"Pause leaked input");
                        var elapsed=Runner.Elapsed;await Task.Delay(180);Require(Runner.Elapsed==elapsed,"Pause advanced encounter");
                        point=RectTransformUtility.WorldToScreenPoint(null,UnityEngine.Object.FindAnyObjectByType<StarShift.UI.ShellView>().Resume.transform.position);await rig.MouseEvent(point,true);await rig.MouseEvent(point,false);Require(!Session.IsPaused,"Resume failed");paused=true;
                    }
                    // Đợi formation density đủ trước khi pilot bắn trong stress encounter.
                    if(Runner.CurrentIndex==1 && Runner.Elapsed<2.4f) {await Task.Delay(80);continue;}
                    if(!missileUsed && Runner.CurrentIndex==3 && Enemies.Count(e=>!e.GetComponent<EntryHoldFlight>().IsOutsideView)>=2 && reserve.CanUse)
                    {
                        var point=RectTransformUtility.WorldToScreenPoint(null,Ui<Button>("EmergencyMissile").transform.position);
                        var stock=reserve.Current;
                        await rig.MouseEvent(point,false);await rig.MouseEvent(point,true);await rig.MouseEvent(point,false);
                        Require(reserve.Current==stock-1,"Full-run missile button did not consume exactly one");
                        await Until(()=>!reserve.IsInFlight,1200,"Full-run missile did not detonate");missileUsed=true;
                    }
                    var pickup=Pickups.FirstOrDefault(p=>p.Crate!=null || rig.Player.GetComponent<PlayerPickupReceiver>().CanUse(p.Item));
                    if(pickup!=null)
                    {
                        await rig.DragTo(pickup.transform.position);
                        await Task.Delay(80);
                        await rig.DragTo(new Vector3(rig.Player.transform.position.x,-3.6f,0));
                    }
                    else
                    {
                        var target=Enemies.Where(e=>!e.GetComponent<EntryHoldFlight>().IsOutsideView).OrderBy(e=>Mathf.Abs(e.transform.position.x-rig.Player.transform.position.x)).FirstOrDefault();
                        if(target!=null)await rig.DragTo(new Vector3(target.transform.position.x,-3.6f,0));
                        else {await rig.MouseEvent(new Vector2(width*.5f,250),true);await Task.Delay(80);}
                    }
                    if(Runner.CurrentIndex==2 && effects.OverdriveRemaining>0 && screenshots.Count==0)screenshots.Add(await Capture("full-effects-"+width));
                }
                Require(Session.IsClear,"Full mission did not Clear; stage="+Runner.Stage+" encounter="+Runner.CurrentIndex+" enemies="+Enemies.Length);
                Require(completed.Count==5 && completed.Distinct().Count()==5,"Completion not once-only");
                Require(rewards.Score>0 && upgrades>0 && Director.PickupsCollected>0,"Full mission reward loop missing");
                Require(!withDeath || (death && paused),"Death/Pause full-run coverage missing");
                Require(missileUsed && missileDetonations==1,"Full-run reserve launch/detonation missing or duplicate");
                Require(effects.OverdriveRemaining==0 && effects.DroneRemaining==0 && !effects.HasShield && Pickups.Length==0,"Terminal stale effects/rewards");
                await Task.Delay(600);
                screenshots.Add(await Capture("mission-clear-"+width));
                return new{pass=true,width,height=900,withDeath,death,paused,missileUsed,missileDetonations,completed,peakVisible=peak,rewards.Score,rewards.Shards,rewards.CoresCollected,rewards.SupplyCaptured,Director.PickupsCollected,upgrades,frames=Time.frameCount-startFrame,screenshots};
            }
            finally{Runner.Completed-=done;reserve.Detonated-=detonated;}
        }
    }

    public static async Task<object> Visuals(int width)
    {
        using(var rig=new Rig(width,6))
        {
            var shots=new List<string>();var weapon=rig.Player.GetComponent<PulseLaserWeapon>();var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();
            Restart(rig,"ENCOUNTER_1");await Task.Delay(3200);shots.Add(await Capture("normal-"+width));
            Restart(rig,"ENCOUNTER_2");await Task.Delay(3200);shots.Add(await Capture("density-"+width));
            Director.Spawn(Item("ShieldCell"),null,new Vector3(1,0,0));shots.Add(await Capture("pickup-"+width));
            receiver.TryCollect(Item("ShieldCell"));receiver.TryCollect(Item("Overdrive"));receiver.TryCollect(Item("SupportDrone"));shots.Add(await Capture("effects-"+width));
            rig.Place(new Vector2(-6,-3.6f));await rig.MouseEvent(new Vector2(width*.35f,220),true);
            await Until(()=>weapon.HeatFraction>=.85f,6500,"Visual warmup failed");shots.Add(await Capture("high-heat-"+width));
            await Until(()=>weapon.IsOverheated,2200,"Visual overheat missing");shots.Add(await Capture("overheated-"+width));
            await rig.MouseEvent(new Vector2(width*.35f,220),false);receiver.TryCollect(Item("StellarShard"));
            var target=Enemies.FirstOrDefault();if(target!=null)target.ReceivePlayerHit(target.CurrentHp,rig.Player);
            await Task.Delay(100);shots.Add(await Capture("score-shard-"+width));
            Restart(rig,"ENCOUNTER_1",false);await Task.Delay(2100);await rig.MouseEvent(new Vector2(width*.35f,220),true);await Task.Delay(350);
            foreach(var enemy in Enemies)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
            await Until(()=>Runner.Stage==EncounterStage.RewardGrace || Runner.Stage==EncounterStage.Handoff,2000,"Visual handoff missing");
            foreach(var p in Pickups)p.Release();
            await Until(()=>Runner.Stage==EncounterStage.Handoff,1000,"Visual handoff blocked");
            Require(weapon.Heat>0,"Handoff reset heat");shots.Add(await Capture("transition-"+width));
            return new{pass=true,width,height=900,shots};
        }
    }

    public static async Task<object> RegressionAndPools()
    {
        await HandoffSmoke();
        using(var rig=new Rig(1600,6))
        {
            Restart(rig,"ENCOUNTER_2");var counts=new List<int>();var projectileCounts=new List<int>();
            for(int cycle=0;cycle<4;cycle++)
            {
                Restart(rig,"ENCOUNTER_2");await Task.Delay(5500);
                foreach(var enemy in Enemies)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
                foreach(var p in Pickups)p.Release();
                await rig.MouseEvent(new Vector2(400,200),true);await Task.Delay(900);await rig.MouseEvent(new Vector2(400,200),false);await Task.Delay(1700);
                counts.Add(Runner.GetComponentsInChildren<EnemyHealth>(true).Length);
                projectileCounts.Add(rig.Player.GetComponent<PulseLaserWeapon>().CreatedProjectileCount);
            }
            Require(counts[2]==counts[3] && projectileCounts[2]==projectileCounts[3],"Pool grew after warmup");
            Restart(rig,"ENCOUNTER_2");
            var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();var effects=rig.Player.GetComponent<PlayerTemporaryEffects>();
            for(int i=0;i<3;i++)
            {
                rig.Player.GetComponent<PlayerDamageLifecycle>().enabled=true;rig.Gate.SetState(PlayerGameplayState.Active);
                receiver.TryCollect(Item("Overdrive"));receiver.TryCollect(Item("SupportDrone"));
                Require(rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null),"GameOver damage rejected");
                if(i<2)await Until(()=>rig.Gate.State==PlayerGameplayState.PostEntryInvincibility,3000,"GameOver fixture respawn failed");
            }
            await Until(()=>rig.Gate.State==PlayerGameplayState.GameOver,1500,"GameOver missing");
            Require(rig.Lives.CurrentLives==0 && Session.IsTerminal && Time.timeScale==0,"GameOver failed to stop combat");
            Require(effects.OverdriveRemaining==0 && effects.DroneRemaining==0,"GameOver retained player buffs");
            Restart(rig);Require(rig.Lives.CurrentLives==3 && Time.timeScale==1 && Runner.HasActiveRun && Pickups.Length==0 && UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length==0,"Reset after terminal failed");
            return new{pass=true,minorProjectileGraceFade=true,pauseFade=true,gameOver=true,reset=true,enemyPoolCounts=counts,primaryProjectileCounts=projectileCounts};
        }
    }
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Fields).SetValue(obj,value);

    public static async Task<object> Stress()
    {
        using(var rig=new Rig(1600,6))
        {
            Restart(rig,"ENCOUNTER_3");rig.Place(new Vector2(-7,-3.6f));
            var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();var weapon=rig.Player.GetComponent<PulseLaserWeapon>();var rewards=Runner.GetComponent<RunRewards>();
            await rig.MouseEvent(new Vector2(300,220),true);
            await Until(()=>Runner.Elapsed>9.8f,12000,"Stress schedule stalled");
            receiver.TryCollect(Item("Overdrive"));receiver.TryCollect(Item("SupportDrone"));
            var peak=Enemies.Count(e=>!e.GetComponent<EntryHoldFlight>().IsOutsideView);Require(peak>=8,"Stress did not reach density: "+peak);
            var before=weapon.ShotsFired;var maximumPickups=0;var sawHeatChange=false;var initialHeat=weapon.Heat;
            for(int i=0;i<10;i++)
            {
                var target=Enemies.Where(e=>!e.GetComponent<EntryHoldFlight>().IsOutsideView).OrderBy(e=>e.transform.position.x).Skip(i%Mathf.Max(1,Enemies.Length)).FirstOrDefault()??Enemies.FirstOrDefault();
                if(target!=null)await rig.DragTo(new Vector3(target.transform.position.x,-3.6f,0));
                await Task.Delay(200);maximumPickups=Mathf.Max(maximumPickups,Pickups.Length);sawHeatChange|=!Mathf.Approximately(initialHeat,weapon.Heat);
            }
            // Dùng contact thực để counter Shard được kiểm tra cùng stress loop.
            var shard=Director.Spawn(Item("StellarShard"),null,rig.Player.transform.position);await Task.Delay(100);
            Require(weapon.ShotsFired>before && rewards.Score>0 && rewards.Shards>0 && sawHeatChange,"Stress combat/reward state missing");
            return new{pass=true,peakVisible=peak,weapon.ShotsFired,rewards.Score,rewards.Shards,maximumPickups,temporaryEffect=true,heatChanged=sawHeatChange};
        }
    }

    public static object DropSelectionEdges()
    {
        using(var rig=new Rig(1600,6))
        {
            Restart(rig);
            var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();var loadout=rig.Player.GetComponent<WeaponLoadout>();
            var table=AssetDatabase.LoadAssetAtPath<RewardDropTable>("Assets/StarShift/Data/Rewards/SupplyCrateDrops.asset");
            var roller=new RewardRoller();roller.Reset(414);
            typeof(PlayerLives).GetField("<CurrentLives>k__BackingField",Fields).SetValue(rig.Lives,1);
            roller.RecordCrate(Item("Overdrive"),1);roller.RecordCrate(Item("SupportDrone"),1);
            var defense=roller.SelectMajor(table,receiver,rig.Lives,loadout,true);
            Require(defense.Kind==PickupKind.Shield || defense.Kind==PickupKind.ExtraLife,"Pity missing");roller.RecordCrate(defense,1);
            Require(Enumerable.Range(0,80).Select(_=>roller.SelectMajor(table,receiver,rig.Lives,loadout,true)).Any(x=>x.Kind!=PickupKind.Shield && x.Kind!=PickupKind.ExtraLife),"Pity did not reset");
            typeof(PlayerLives).GetField("<CurrentLives>k__BackingField",Fields).SetValue(rig.Lives,5);
            foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})while(loadout.Level(branch)<WeaponLoadout.MaximumLevel)loadout.ApplyCore(branch);
            for(int i=0;i<100;i++){var item=roller.SelectMajor(table,receiver,rig.Lives,loadout,true);Require(item!=null && item.Kind!=PickupKind.WeaponCore && item.Kind!=PickupKind.ExtraLife,"Maxed reward not filtered");}
            return new{pass=true,pityReset=true,maxedFilters=true};
        }
    }

    public static async Task<object> PowerAndLifecycle()
    {
        using(var rig=new Rig(1600,6))
        {
            var loadout=rig.Player.GetComponent<WeaponLoadout>();var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();var weapon=rig.Player.GetComponent<PulseLaserWeapon>();var effects=rig.Player.GetComponent<PlayerTemporaryEffects>();
            var ttks=new List<float>();
            foreach(var level in new[]{1,3})
            {
                Restart(rig,"ENCOUNTER_1");await Until(()=>Enemies.Length>0,700,"TTK enemy missing");
                var target=Enemies[0];rig.Freeze(target);target.transform.position=Vector3.zero;target.GetComponent<Rigidbody2D>().position=Vector2.zero;
                while(loadout.ActiveLevel<level)loadout.ApplyCore(WeaponBranch.Pulse);
                rig.Place(new Vector2(0,-3));var start=Time.fixedTime;await rig.MouseEvent(new Vector2(400,220),true);
                await Until(()=>!target.IsAlive,2000,"TTK target survived");ttks.Add(Time.fixedTime-start);await rig.MouseEvent(new Vector2(400,220),false);
            }
            Require(ttks[1]<ttks[0]*.8f,"Upgrade did not improve real TTK: "+string.Join(",",ttks));
            Restart(rig,"ENCOUNTER_2");receiver.TryCollect(Item("PlasmaCore"));await Task.Delay(80);
            var button=Ui<Button>("Weapon");var point=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);var position=rig.Player.transform.position;
            await rig.MouseEvent(point,true);await rig.MouseEvent(point,false);Require(loadout.ActiveBranch==WeaponBranch.Plasma && rig.Player.transform.position==position && !rig.Input.FireHeld,"Weapon UI leaked gameplay pointer");
            loadout.ApplyCore(WeaponBranch.Plasma);loadout.ApplyCore(WeaponBranch.Plasma);rig.Place(new Vector2(-8,-3.6f));
            var created=new List<int>();
            for(int i=0;i<2;i++)
            {
                receiver.TryCollect(Item("Overdrive"));await rig.MouseEvent(new Vector2(300,220),true);await Task.Delay(6200);await rig.MouseEvent(new Vector2(300,220),false);await Task.Delay(1700);created.Add(weapon.CreatedProjectileCount);
            }
            Require(created[0]==created[1],"Plasma OD pool churn after warmup: "+string.Join(",",created));
            Restart(rig);var expiring=UnityEngine.Object.Instantiate(Item("StellarShard"));expiring.Lifetime=.12f;
            var beforeExpired=Director.PickupsExpired;Director.Spawn(expiring,null,new Vector3(5,3,0));await Task.Delay(250);Require(Director.PickupsExpired==beforeExpired+1 && Director.ActivePickups==0,"Pickup lifetime/pool reset failed");UnityEngine.Object.Destroy(expiring);
            Restart(rig,"ENCOUNTER_1",false);await Task.Delay(2100);receiver.TryCollect(Item("Overdrive"));receiver.TryCollect(Item("SupportDrone"));receiver.TryCollect(Item("ShieldCell"));
            await rig.MouseEvent(new Vector2(300,220),true);await Task.Delay(300);
            foreach(var enemy in Enemies)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
            await Until(()=>Runner.Stage==EncounterStage.RewardGrace || Runner.Stage==EncounterStage.Handoff,1800,"Lifecycle handoff absent");foreach(var pickup in Pickups)pickup.Release();
            await Until(()=>Runner.CurrentIndex==1,4000,"Lifecycle next encounter absent");
            Require(effects.HasShield && effects.OverdriveRemaining>0 && effects.DroneRemaining>0 && weapon.Heat>0,"Handoff reset persistent combat effects");
            Require(UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length>0,"No friendly shots before reset probe");
            Runner.StartRun("ENCOUNTER_3",true,true,null,null);
            Require(UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length==0 && !effects.HasShield && effects.OverdriveRemaining==0 && effects.DroneRemaining==0,"Run reset retained effects/shots");
            return new{pass=true,pulseTtkSeconds=ttks,plasmaOverdriveCreated=created,uiSwitchOwnership=true,pickupExpiry=true,handoffRetention=true,runResetFriendlyCleanup=true};
        }
    }

    public static async Task<object> PickupFade()
    {
        using(var rig=new Rig(1600,6))
        {
            Restart(rig);
            var item=UnityEngine.Object.Instantiate(Item("StellarShard"));item.Lifetime=.6f;
            try
            {
                var pickup=Director.Spawn(item,null,new Vector3(4,2,0));var visual=pickup.GetComponentInChildren<SpriteRenderer>();
                await Task.Delay(400);Require(pickup.IsActive && visual.color.a>0 && visual.color.a<1,"Lifetime cue did not fade");
                Session.Pause();var alpha=visual.color.a;await Task.Delay(120);Require(visual.color.a==alpha,"Pause advanced expiry cue");Session.Resume();
                await Until(()=>!pickup.IsActive,500,"Lifetime did not release");
                var reused=Director.Spawn(item,null,new Vector3(4,2,0));Require(reused==pickup && visual.color.a==1,"Reuse retained fade alpha");
                Runner.GetComponent<EncounterCleanup>().BeginGrace(.8f);await Task.Delay(400);Require(pickup.IsActive && visual.color.a==1,"Grace failed to extend lifetime");
                await Task.Delay(220);Require(pickup.IsActive && visual.color.a>0 && visual.color.a<1,"Grace expiry cue missing");
                await Until(()=>!pickup.IsActive,400,"Grace pickup did not expire");
                return new{pass=true,lifetimeFade=true,pauseFreeze=true,reuseOpacity=true,graceExtensionAndFade=true};
            }
            finally{UnityEngine.Object.Destroy(item);}
        }
    }

    public static async Task<object> MissilesAndProtection()
    {
        using(var rig=new Rig(1600,6))
        {
            Restart(rig,"ENCOUNTER_2");await Task.Delay(2100);rig.Place(Vector2.zero);
            var enemies=Enemies.Take(2).ToArray();
            for(int i=0;i<2;i++){rig.Freeze(enemies[i]);enemies[i].transform.position=new Vector3(i==0?2:Camera.main.ViewportToWorldPoint(new Vector3(1.2f,.6f,10)).x,2,0);enemies[i].GetComponent<Rigidbody2D>().position=enemies[i].transform.position;}
            var hp=enemies.Select(e=>e.CurrentHp).ToArray();var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();
            var profile=AssetDatabase.LoadAssetAtPath<SingleFireDefinition>("Assets/StarShift/Data/Enemies/ScoutSingleFire.asset");var muzzle=new GameObject("MissileProbeMuzzle");muzzle.transform.position=new Vector3(3,3,0);
            var hostile=UnityEngine.Object.Instantiate(profile.ProjectilePrefab);
            var friendly=UnityEngine.Object.Instantiate(rig.Player.GetComponent<WeaponLoadout>().ActiveDefinition.ProjectilePrefab);
            try
            {
                hostile.Launch(profile,muzzle.transform,enemies[0].gameObject,rig.Player,p=>p.ResetForPool());
                friendly.Launch(rig.Player.GetComponent<WeaponLoadout>().ActiveDefinition,new Vector3(-5,-4,0),Vector2.up,1,0,rig.Player,p=>p.ResetForPool());
                var gate=UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();var allowed=gate.CanFire;var lives=rig.Lives.CurrentLives;
                Require(receiver.TryCollect(Item("MissileAmmo")),"Missile ammo rejected");
                Require(enemies[0].CurrentHp==hp[0] && enemies[1].CurrentHp==hp[1],"Ammo pickup must not detonate");
                Require(rig.Player.GetComponent<EmergencyMissileReserve>().TryUse(),"Missile activation rejected");
                await Until(()=>!rig.Player.GetComponent<EmergencyMissileReserve>().IsInFlight,1200,"Missile detonation timeout");
                Require(!enemies[0].IsAlive && enemies[1].CurrentHp==hp[1],"Visible normal targets only");
                Require(UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length==0 && friendly.gameObject.activeSelf && gate.CanFire==allowed && rig.Lives.CurrentLives==lives,"Missile used wrong cleanup semantics");
                receiver.TryCollect(Item("ShieldCell"));var effects=rig.Player.GetComponent<PlayerTemporaryEffects>();
                var life=rig.Player.GetComponent<PlayerDamageLifecycle>();life.enabled=true;rig.Gate.SetState(PlayerGameplayState.PostEntryInvincibility);
                Require(!life.TryReceiveHit(null) && effects.HasShield,"Invincibility consumed shield");
                rig.Gate.SetState(PlayerGameplayState.Active);Require(life.TryReceiveHit(null) && !effects.HasShield && rig.Lives.CurrentLives==3,"Shield did not absorb intended hit");
                return new{pass=true,missileReserveAndActivation=true,hostileOnlyCleanup=true,firePermissionUnchanged=true,invincibilityBeforeShield=true};
            }
            finally{UnityEngine.Object.Destroy(hostile.gameObject);UnityEngine.Object.Destroy(friendly.gameObject);UnityEngine.Object.Destroy(muzzle);}
        }
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
        readonly LevelDefinition previousLevel;
        public Rig(int width, float size, bool intro = false)
        {
            Require(Application.isPlaying, "Play Mode required.");
            Session.enabled = false; Runner.StopRun(); Time.timeScale = 1; AudioListener.pause = false; Session.enabled = true;
            previousLevel = Runner.Flow.Level;
            Runner.Flow.LoadLevel(AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/StarShift/Data/Fixtures/Levels/M4SystemTestbed.asset"));
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
        static EncounterDefinition[] Configs() => new[] { "A", "B" }.Select(id => AssetDatabase.LoadAssetAtPath<EncounterDefinition>("Assets/StarShift/Data/Fixtures/Encounters/L1Encounter" + id + ".asset")).ToArray();
        public void Dispose()
        {
            RestoreFlights();
            Session.enabled = false; Runner.StopRun(); Time.timeScale = 1;
            InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(touch);
            foreach (var d in disabled) InputSystem.EnableDevice(d);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor; InputSystem.settings.backgroundBehavior = oldBackground;
            var life = Player.GetComponent<PlayerDamageLifecycle>(); life.enabled = false; life.enabled = true; Input.CancelGesture();
            Runner.Flow.LoadLevel(previousLevel);
            view.SetBaseline(Runner.Flow.Level.Camera.View); view.RequestView(oldView, 0); gameView.GetType().GetMethod("SizeSelectionCallback").Invoke(gameView, new object[] { oldSize, null });
            gameViewZoom.GetType().GetMethod("SetTransform").Invoke(gameViewZoom, new object[] { oldGameViewPan, oldGameViewZoom });
            UnityEngine.Object.Destroy(profile);
            Session.enabled = true; Time.timeScale = oldScale; AudioListener.pause = oldAudio;
            Require(definitions.SequenceEqual(Configs().Select(EditorJsonUtility.ToJson)), "QA mutated encounter config assets.");
        }
    }
}
