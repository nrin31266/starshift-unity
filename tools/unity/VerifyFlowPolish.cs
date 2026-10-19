using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Dev;
using StarShift.Persistence;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.Rewards;
using UnityEditor;
using UnityEngine;
using Rig = VerifyPacingRefinement.Rig;
public static class VerifyFlowPolish
{
    const string O="../.design/m5-flow-polish/";
    static EncounterRunner R=>UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession S=>R.GetComponent<GameplaySession>();
    static LevelDefinition L(int n)=>StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission0"+n+".asset");
    static void Check(bool c,string m){if(!c)throw new InvalidOperationException(m);}
    static void Evidence(string n,object o)=>File.WriteAllText(O+n+".json",Newtonsoft.Json.JsonConvert.SerializeObject(o,Newtonsoft.Json.Formatting.Indented));
    static async Task Until(Func<bool> f,int ms,string m){var end=Time.realtimeSinceStartup+ms/1000f;while(!f()&&Time.realtimeSinceStartup<end)await Task.Delay(15);Check(f(),m);}
    static async Task Capture(string n){ScreenCapture.CaptureScreenshot(Path.GetFullPath(O+"screenshots/"+n+".png"));await Task.Delay(180);}
    static void Start(Rig rig,int mission,int encounter=0,bool standalone=true){R.StopRun();R.Flow.LoadLevel(L(mission));R.StartRun("ENCOUNTER_"+(encounter+1),standalone,true,null,null);rig.Protect();}
 public static object BeginDevRoute(){_=DevRoute();return new{started=true};}
 static async Task DevRoute()
 {
  var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();var dev=runner.GetComponent<DevFastStart>();var config=EditorJsonUtility.ToJson(dev);var owner=runner.GetComponent<CampaignLifecycle>();var path=owner.SavePath;
  string Bytes()=>File.Exists(path)?Convert.ToBase64String(File.ReadAllBytes(path)):null;var before=Bytes();
  void Set(string n,object value)=>typeof(DevFastStart).GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(dev,value);
  object result;
  try{
   Set("devModeEnabled",true);Set("startLevel",StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission04.asset"));Set("startPoint","BOSS_PHASE_B");Set("skipIntro",true);Set("standalone",true);dev.Replay();
   var end=Time.realtimeSinceStartup+7;BossPhaseRunner boss=null;
   while(Time.realtimeSinceStartup<end){boss=UnityEngine.Object.FindAnyObjectByType<BossPhaseRunner>();if(boss!=null&&boss.IsPhaseB)break;await Task.Delay(30);}
   if(boss==null||!boss.IsPhaseB||!boss.GetComponent<EnemyHealth>().CanReceiveDamage)throw new InvalidOperationException("Phase B shortcut blocked by entry protection");
   if(before!=Bytes())throw new InvalidOperationException("Dev route wrote production save");
   result=new{status="PASS",bossPhaseB=true,hp=boss.GetComponent<EnemyHealth>().CurrentHp,entryComplete=boss.GetComponent<EntryHoldFlight>().IsHolding,productionUnchanged=true};
  }catch(Exception ex){result=new{status="FAILED",error=ex.ToString()};}
  finally{runner.StopRun();EditorJsonUtility.FromJsonOverwrite(config,dev);}
  File.WriteAllText("../.design/m5-flow-polish/dev-route.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 }
    public static async Task<object> RespawnContinuity()
    {
        using (var rig = new Rig(1600, 6))
        {
            var life = rig.Player.GetComponent<PlayerDamageLifecycle>();
            var weapon = rig.Player.GetComponent<PulseLaserWeapon>();
            var enemyGate = UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();
            var profile = AssetDatabase.LoadAssetAtPath<SingleFireDefinition>("Assets/StarShift/Data/Enemies/ScoutSingleFire.asset");
            var owner = new GameObject("RespawnContinuityOwner");
            var shots = new List<EnemyProjectile>();
            var results = new List<object>();
            try
            {
                foreach (bool touch in new[] { false, true })
                {
                    async Task Pointer(Vector2 position, bool held, bool begin = false)
                    {
                        if (touch) await rig.TouchEvent(31, held ? (begin ? UnityEngine.InputSystem.TouchPhase.Began : UnityEngine.InputSystem.TouchPhase.Moved) : UnityEngine.InputSystem.TouchPhase.Ended, position);
                        else await rig.MouseEvent(position, held);
                    }
                    rig.Unprotect();
                    await Pointer(new Vector2(400, 300), true, true);
                    await Task.Delay(100);
                    var friendly = UnityEngine.Object.FindObjectsByType<PlayerProjectile>().ToArray();
                    Check(friendly.Length > 0 && rig.Input.FireHeld, "Held fire fixture unavailable");
                    var shot = UnityEngine.Object.Instantiate(profile.ProjectilePrefab); shots.Add(shot);
                    owner.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(.1f, .9f, -Camera.main.transform.position.z));
                    shot.Launch(profile, owner.transform, owner, rig.Player, p => p.ResetForPool());
                    var beforeShot = shot.transform.position;
                    Check(life.TryReceiveHit(owner), "Death rejected");
                    Check(shot.gameObject.activeSelf && friendly.All(p => p.gameObject.activeSelf), "Death cleared flying projectiles");
                    Check(!rig.Gate.CanInput && !rig.Gate.CanFire && !enemyGate.CanFire, "Death permissions stayed open");
                    var lives = rig.Lives.CurrentLives;
                    Check(!life.TryReceiveHit(owner) && rig.Lives.CurrentLives == lives, "Duplicate death consumed life");
                    await Pointer(new Vector2(900, 600), true);
                    Check(shot.gameObject.activeSelf && Vector3.Distance(shot.transform.position, beforeShot) > .01f, "Surviving enemy projectile froze");
                    await Until(() => rig.Gate.State == PlayerGameplayState.PostEntryInvincibility, 2200, "Respawn did not return control");
                    var safePose = rig.Player.transform.position;
                    await Pointer(new Vector2(900, 600), true);
                    Check(Vector3.Distance(rig.Player.transform.position, safePose) < .01f, "Blocked drag teleported respawn");
                    var before = Camera.main.WorldToScreenPoint(rig.Player.transform.position);
                    var fired = weapon.ShotsFired;
                    await Pointer(new Vector2(930, 620), true);
                    var delta = (Vector2)(Camera.main.WorldToScreenPoint(rig.Player.transform.position) - before);
                    Check((delta - new Vector2(30, 20)).sqrMagnitude < 2, "Held drag did not resume: " + delta);
                    await Task.Delay(220);
                    Check(rig.Input.FireHeld && weapon.ShotsFired > fired, "Held fire required a fresh press");
                    Check(!rig.Gate.CanTakeDamage && !life.TryReceiveHit(owner) && rig.Lives.CurrentLives == lives, "Respawn protection missing");
                    await Pointer(new Vector2(930, 620), false);
                    results.Add(new { input = touch ? "touch" : "mouse", heldDragDelta = delta.ToString(), heldFire = true, projectilesPreserved = true, protectedRespawn = true });
                }
                // Pause vẫn hủy gesture; chỉ death/respawn nối lại pointer còn hợp lệ.
                await rig.MouseEvent(new Vector2(400, 300), true);
                S.Pause(); S.Resume();
                await rig.MouseEvent(new Vector2(450, 320), true);
                Check(!rig.Input.FireHeld, "Pause accepted an old gesture");
                await rig.MouseEvent(new Vector2(450, 320), false);
                rig.Unprotect(); rig.Lives.RestoreCheckpoint(1);
                var terminalShot = UnityEngine.Object.Instantiate(profile.ProjectilePrefab); shots.Add(terminalShot);
                terminalShot.Launch(profile, owner.transform, owner, rig.Player, p => p.ResetForPool());
                Check(life.TryReceiveHit(owner) && terminalShot.gameObject.activeSelf, "Last death prematurely cleared shots");
                await Until(() => rig.Gate.State == PlayerGameplayState.GameOver, 1500, "Game Over did not finish");
                Check(!terminalShot.gameObject.activeSelf && UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length == 0, "Terminal cleanup regressed");
                var result = new { status = "PASS", results, pauseRequiresFreshPress = true, gameOverCleanup = true };
                Directory.CreateDirectory("../.design/m5-respawn-continuity");
                File.WriteAllText("../.design/m5-respawn-continuity/result.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
                return result;
            }
            finally
            {
                foreach (var shot in shots) if (shot != null) UnityEngine.Object.DestroyImmediate(shot.gameObject);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
    public static object BeginFullRegion()
    {
        Check(Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(O+"targeted.json"))["status"].ToString()=="PASS","Targeted prerequisites");
        Check(!File.Exists(O+"human-regression/full-region.json"),"Full run already attempted; inspect before repeating");
        typeof(VerifyHumanFeel).GetMethod("FullRegion",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,null);
        return new{started=true};
    }
    public static object Begin(){_=Run();return new{started=true};}
    static async Task Run()
    {
        Evidence("targeted",new{status="RUNNING"});var results=new List<object>();
        try{
            foreach(var width in new[]{1600,2000})using(var rig=new Rig(width,6)){
                R.StopRun();var introLevel=UnityEngine.Object.Instantiate(L(1));introLevel.Intro=JsonUtility.FromJson<MissionIntroSettings>(JsonUtility.ToJson(L(1).Intro));introLevel.Intro.Enabled=true;R.Flow.LoadLevel(introLevel);R.StartRun("LEVEL_START",true,false,null,null);
                await Until(()=>R.Flow.CanContinue,3000,"Intro ready");S.BeginMission();
                Check(rig.Gate.CanFire&&!R.Flow.IsCinematic,"Approach still locks fire");
                var weapon=rig.Player.GetComponent<PulseLaserWeapon>();var before=weapon.ShotsFired;
                await rig.MouseEvent(new Vector2(500,350),false);await rig.MouseEvent(new Vector2(500,350),true);
                await Task.Delay(400);Check(weapon.ShotsFired>before,"No shots during zoom");await Capture("intro-fire-"+width);
                await Until(()=>R.Flow.IntroPhase==MissionIntroPhase.Brake,3500,"First brake");
                var camera=Camera.main;float prior=camera.orthographicSize,maxReverse=0,maxStep=0;var last=rig.Player.transform.position;
                while(R.Stage==EncounterStage.Intro){maxReverse=Math.Max(maxReverse,camera.orthographicSize-prior);maxStep=Math.Max(maxStep,Vector3.Distance(last,rig.Player.transform.position));prior=camera.orthographicSize;last=rig.Player.transform.position;await Task.Delay(12);}
                Check(maxReverse<.002f&&maxStep<.12f&&rig.Input.FireHeld,"First intro pose/input discontinuity: reverse="+maxReverse+" move="+maxStep+" held="+rig.Input.FireHeld);
                await rig.MouseEvent(new Vector2(500,350),false);rig.Protect();
                var loadout=rig.Player.GetComponent<WeaponLoadout>();
                foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})for(int level=1;level<=5;level++){
                    var levels=new[]{1,0,0};levels[(int)branch]=level;weapon.ResetForSession();loadout.Restore(branch,levels);var pattern=loadout.ActiveDefinition.Pattern(level);
                    Check(pattern.ProjectileCount==(branch==WeaponBranch.Plasma?level+2:level),"Pattern not visibly distinct");before=weapon.ShotsFired;
                    await rig.MouseEvent(new Vector2(500,350),true);await Task.Delay(650);await rig.MouseEvent(new Vector2(500,350),false);
                    Check(weapon.ShotsFired-before>=pattern.ProjectileCount,"Pattern failed "+branch+level);
                }
                await Capture("hud-inventory-"+width);results.Add(new{width,firstIntroReverse=maxReverse,firstIntroMaxMove=maxStep,patterns=15});
                Start(rig,4);UnityEngine.Object.Destroy(introLevel);Check(R.GetComponent<RegionCampaignFlow>().QueueAdvance(),"Journey queue");
                var journey=R.GetComponent<PlanetJourney>();await Until(()=>journey.IsAwaitingTap,9500,"Planet reveal");S.BeginMission();
                before=weapon.ShotsFired;await rig.MouseEvent(new Vector2(500,350),true);await Task.Delay(400);Check(weapon.ShotsFired>before,"Second approach fire locked");
                await Until(()=>journey.Phase==PlanetJourneyPhase.Brake,3500,"Second brake");prior=camera.orthographicSize;maxStep=0;last=rig.Player.transform.position;
                while(journey.IsActive){maxStep=Math.Max(maxStep,Vector3.Distance(last,rig.Player.transform.position));last=rig.Player.transform.position;await Task.Delay(12);}
                Check(maxStep<.12f&&rig.Input.FireHeld&&rig.Gate.CanFire,"Second intro pose/input discontinuity: move="+maxStep+" held="+rig.Input.FireHeld);await Capture("second-ready-"+width);results.Add(new{width,secondIntroMaxMove=maxStep,heldAcrossFinish=rig.Input.FireHeld});
                await rig.MouseEvent(new Vector2(500,350),false);
            }
            using(var rig=new Rig(1600,6)){
                Start(rig,1);await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive),2000,"Scout spawn");var scout=EnemyHealth.Active.First(e=>e.IsAlive);Check(scout.MaxHp==4,"Scout is not 4 HP");
                await Until(()=>scout.CanReceiveDamage,2500,"Scout entry protection stuck");rig.Freeze(scout);var t=Time.fixedTime;await rig.KillWithPulse(scout);results.Add(new{scoutHp=4,pulseL1Seconds=Time.fixedTime-t});rig.RestoreFlights();
                Start(rig,2,1,false);await Until(()=>R.Stage==EncounterStage.ExitDrain,17000,"Storm did not wait");
                var survivors=EnemyHealth.Active.Where(e=>e.IsAlive&&!e.GetComponent<EntryHoldFlight>().IsOutsideView).ToArray();Check(survivors.Length>0,"No live asteroids at deadline");
                var exitStart=Time.fixedTime;int tracked=survivors.Length;
                while(R.CurrentIndex==1&&R.HasActiveRun){foreach(var e in survivors)if(!e.IsAlive)Check(e.GetComponent<EntryHoldFlight>().HasExited||e.GetComponent<EntryHoldFlight>().IsOutsideView,"Asteroid popped inside viewport");await Task.Delay(20);}
                Check(R.CurrentIndex==2,"Storm failed to advance");results.Add(new{naturalExitAsteroids=tracked,extraExitSeconds=Time.fixedTime-exitStart});
                Start(rig,4,1);await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&e.Definition.MissileTier==MissileDamageTier.Boss),2500,"Boss spawn");var boss=EnemyHealth.Active.First(e=>e.IsAlive&&e.Definition.MissileTier==MissileDamageTier.Boss);
                var hp=boss.CurrentHp;boss.ReceivePlayerHit(hp,rig.Player);Check(boss.CurrentHp==hp,"Boss hit before entry");
                await Until(()=>boss.CanReceiveDamage,5000,"Boss never vulnerable");await Task.Delay(300);Check(boss.GetComponent<EntryHoldFlight>().IsHolding,"Boss vulnerable before hold");boss.ReceivePlayerHit(boss.CurrentHp,rig.Player);
                await Until(()=>R.Stage==EncounterStage.RewardGrace,5000,"Boss rewards");var pickups=UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive).ToArray();
                Check(pickups.Count(p=>p.Item?.Kind==PickupKind.Shard)==28&&pickups.Length==30&&pickups.All(p=>p.Crate==null),"Boss gold/pair count");
                Check(pickups.All(p=>p.GetComponentInChildren<SpriteRenderer>().transform.lossyScale.x>0),"Pickup sprite has zero scale");
                await Capture("boss-gold-rain");S.Pause();var positions=pickups.Select(p=>p.transform.position).ToArray();await Task.Delay(200);Check(pickups.Select((p,i)=>Vector3.Distance(p.transform.position,positions[i])<.001f).All(x=>x),"Reward Pause");S.Resume();rig.Protect();
                Check(pickups.First(p=>p.Item?.Kind==PickupKind.Shard).TryCollect(),"Gold collection");
                await Task.Delay(6500);Check(R.Stage==EncounterStage.RewardGrace&&R.GetComponent<RewardDirector>().PickupsExpired==0,"Reward expired too soon");Check(pickups.Where(p=>p.IsActive&&p.Item.Kind!=PickupKind.Shard).All(p=>Camera.main.WorldToViewportPoint(p.transform.position).y>=.085f),"Reward fell out of collection window");await Capture("boss-reward-floor");
                await Until(()=>pickups.All(p=>!p.IsActive||p.Item.Kind!=PickupKind.Shard),3500,"Gold stayed at reward floor");
                await Until(()=>!R.HasActiveRun,6500,"Reward phase did not finish");results.Add(new{bossEntryProtected=true,bossGold=28,pair=2,rewardPause=true,rewardRetained=true});
            }
            Evidence("targeted",new{status="PASS",results});
        }catch(Exception e){Evidence("targeted",new{status="FAILED",error=e.ToString(),results});}
    }
}
