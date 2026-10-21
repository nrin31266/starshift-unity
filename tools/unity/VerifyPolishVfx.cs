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
using StarShift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyPolishVfx
{
    const string Output="../.design/presentation-polish/";
    public static Task CurrentTask {get;private set;}
    public static object Begin(string scope="vfx",int targetWidth=1600)
    {
        Check(CurrentTask==null||CurrentTask.IsCompleted,"Probe already running");
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"current.json","{\"status\":\"running\"}");
        CurrentTask=RunSelected(scope,targetWidth);return new{started=true,scope,targetWidth};
    }
    static async Task RunSelected(string scope,int targetWidth)
    {
        try
        {
            var result=scope=="deaths"?await Deaths():await Run(targetWidth);
            File.WriteAllText(Output+"current.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="PASS",scope,targetWidth,result},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception error){File.WriteAllText(Output+"current.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="FAIL",scope,error=error.ToString()},Newtonsoft.Json.Formatting.Indented));}
    }
    static int width;
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static async Task Until(Func<bool> predicate,int ms,string message)
    {float end=Time.realtimeSinceStartup+ms/1000f;while(!predicate()&&Time.realtimeSinceStartup<end)await Task.Delay(15);Check(predicate(),message);}
    static async Task Capture(string id)
    {var path=Path.GetFullPath(Output+"screenshots/"+id+"-"+width+".png");if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);await Until(()=>File.Exists(path),2000,"Screenshot "+id);}
    static T Field<T>(UnityEngine.Object target,string name) where T:UnityEngine.Object => (T)new SerializedObject(target).FindProperty(name).objectReferenceValue;
    static async Task Impact(EnemyHealth target,PlayerDamageLifecycle player,WeaponBranch branch,string expected)
    {
        var definition=player.GetComponent<WeaponLoadout>().Definition(branch);EnemyHitInfo? observed=null;
        Action<EnemyHitInfo> record=hit=>observed=hit;target.HitPresented+=record;
        try
        {
            Time.timeScale=.25f;
            var projectile=UnityEngine.Object.Instantiate(definition.ProjectilePrefab);
            projectile.Launch(definition,target.transform.position-Vector3.up*.6f,Vector2.up,1,0,player.gameObject,p=>UnityEngine.Object.Destroy(p.gameObject),target);
            await Until(()=>observed.HasValue,1500,"Actual projectile missed "+expected);
            Check(observed.Value.Branch==branch,"Projectile family lost before hit presentation");
            var renderer=Field<SpriteRenderer>(target.GetComponent<EnemyHitPresentation>(),"impact");
            await Until(()=>renderer.enabled&&renderer.sprite!=null&&int.TryParse(renderer.sprite.name.Substring(renderer.sprite.name.Length-2),out var frame)&&frame>=2&&frame<=4,1500,"Peak impact frame "+expected);
            Time.timeScale=0;
            Check(renderer.enabled&&renderer.sprite.name.StartsWith(expected+"_",StringComparison.Ordinal),"Wrong impact "+expected+": "+renderer.sprite?.name);
            Check(renderer.bounds.size.x>.25f&&renderer.bounds.size.x<1.3f,"Impact scale out of gameplay range "+expected);
            await Capture(expected);
            Time.timeScale=1;await Task.Delay(250);
        }
        finally{Time.timeScale=1;target.HitPresented-=record;}
    }
    static async Task Skill(BossAttackModule module,EnemyHealth boss,string id)
    {
        var cue=module.GetComponent<BossSkillPhasePresentation>();Check(cue!=null,"Skill cue missing "+id);
        var sprite=Field<SpriteRenderer>(cue,"cue");
        var barrage=module.GetComponent<BossBarrage>();
        var phases=new HashSet<AttackPhase>();var rows=new List<float[]>();var safeLanes=new List<float>();int previousPulse=barrage!=null?barrage.PulsesFired:0;
        Check(module.BeginAttack(),"Skill rejected "+id);
        float end=Time.realtimeSinceStartup+16;bool capturedPrepare=false,capturedAttack=false,rearmed=false;
        do
        {
            var phase=module.CurrentPhase;phases.Add(phase);
            if(barrage!=null&&barrage.PulsesFired>0&&!barrage.IsActive&&phase==AttackPhase.Prepare)rearmed=true;
            if(phase==AttackPhase.Prepare&&!capturedPrepare)
            {
                await Task.Delay(35);Check(sprite.enabled&&sprite.color.a>=.8f,"Prepare invisible "+id);
                Check(sprite.bounds.size.x/(Camera.main.orthographicSize*2)*900>=32,"Skill cue too small "+id);
                if(id=="warden-beam-sweep")
                {
                    var beam=Field<AttackBeamPresentation>(Field<DirectedAttack>(module,"directed"),"presentation");
                    var arrow=Field<SpriteRenderer>(beam,"sweepArrow");Check(arrow.enabled&&arrow.gameObject.activeInHierarchy,"Sweep arrow invisible");
                }
                await Capture(id+"-prepare");capturedPrepare=true;
            }
            if(phase==AttackPhase.Attack&&!capturedAttack)
            {await Task.Delay(65);Check(sprite.enabled,"Delegated attack cue missing "+id);await Capture(id+"-attack");capturedAttack=true;}
            if(barrage!=null&&barrage.PulsesFired!=previousPulse)
            {
                previousPulse=barrage.PulsesFired;
                if(barrage.Definition.Kind==BossBarrageKind.ProjectileCurtain)
                {
                    var direction=boss.GetComponent<EntryHoldFlight>().Direction;
                    var points=UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Where(p=>p.Owner==boss.gameObject)
                        .Select(p=>PlayDirectionSpace.LocalPoint(Camera.main.WorldToViewportPoint(p.transform.position),direction)).ToArray();
                    Check(points.Length>0,"Curtain pulse has no actual shots");var newest=points.Max(p=>p.y);
                    rows.Add(points.Where(p=>p.y>newest-.008f).Select(p=>p.x).OrderBy(x=>x).ToArray());safeLanes.Add(barrage.SafeLane);
                }
            }
            await Task.Delay(10);
        }while(module.IsBusy&&Time.realtimeSinceStartup<end);
        Check(!module.IsBusy&&phases.Contains(AttackPhase.Prepare)&&phases.Contains(AttackPhase.Lock)&&phases.Contains(AttackPhase.Attack),"Skill phase coverage "+id);
        if(barrage!=null&&barrage.Definition.Kind==BossBarrageKind.IonShelter&&barrage.Definition.PulseCount>1)Check(rearmed,"Ion next pulse did not return to Prepare");
        if(rows.Count>0)
        {
            Check(rows.Count==barrage.Definition.PulseCount,"Missing barrage rows");
            for(int row=0;row<rows.Count;row++)foreach(float lane in rows[row])
            {
                float slot=(lane-.02f)/.96f*(barrage.Definition.ProjectilesPerRow-1);
                float offset=row%2==0?0:.5f;
                Check(Mathf.Abs(slot-offset-Mathf.Round(slot-offset))<.12f,"Rows did not alternate half a slot");
                Check(Mathf.Abs(lane-safeLanes[row])>barrage.Definition.OpeningWidth*.5f,"A shot entered the announced gap");
            }
        }
        module.CancelAttack();await Task.Delay(550);
    }
    public static async Task<object> Deaths()
    {
        Check(Application.isPlaying,"Play Mode required");width=Screen.width;
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();Check(!runner.HasActiveRun,"Idle Menu required");
        var save=runner.GetComponent<CampaignLifecycle>();var shell=runner.GetComponent<GameShell>();
        var oldLevel=runner.Flow.Level;var original=save.SavePath;
        var bytes=new[]{"",".bak",".invalid"}.ToDictionary(s=>s,s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null);
        var temp=Path.Combine(Path.GetTempPath(),"starshift-death-"+Guid.NewGuid().ToString("N"),"save.json");
        var level=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/StarShift/Data/Fixtures/Levels/M4SystemTestbed.asset"));
        var encounter=UnityEngine.Object.Instantiate(level.Encounters[0]);level.Encounters=new[]{encounter};level.AdditionalStarts=Array.Empty<LevelStartPoint>();
        encounter.Completion=EncounterCompletion.Eliminate;encounter.EntryLeadSeconds=0;encounter.OptionalEvents=Array.Empty<OptionalEvent>();encounter.EntryBeats=Array.Empty<EntryBeatRule>();
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerDamageLifecycle>();var pointer=player.GetComponent<InputSystemPointerInput>();bool oldPointer=pointer.enabled,oldDamage=player.enabled;
        int rewards=0;Action<EnemyHealth,RewardDropTable,Vector3> reward=(e,t,p)=>rewards++;
        var bursts=runner.GetComponent<AsteroidDestructionPresentation>();SpriteRenderer previous=null;bool reused=false;
        try
        {
            save.UseTestStorage(temp);shell.Boot();pointer.enabled=false;player.enabled=false;runner.EnemyDefeated+=reward;
            foreach(var name in new[]{"Scout","Scout","Bulwark","SupplyDrone","StormAsteroid","ResourceAsteroid"})
            {
                runner.StopRun();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/Enemies/"+name+".prefab");
                encounter.Groups=new[]{new SpawnAction{Count=1,EnemyPrefab=prefab,HpOverride=name.EndsWith("Asteroid",StringComparison.Ordinal)?100:0,Stagger=0,EntryDuration=.05f,HoldDuration=30,
                    EntryAnchor=new Vector2(.5f,.6f),HoldAnchor=new Vector2(.5f,.6f),GrantsRewards=name=="SupplyDrone",
                    RewardOverride=name=="SupplyDrone"?AssetDatabase.LoadAssetAtPath<RewardDropTable>("Assets/StarShift/Data/Rewards/SupplyCrateDrops.asset"):null}};
                runner.Flow.LoadLevel(level);runner.StartRun("LEVEL_START",true,true,null,null);
                UnityEngine.Object.FindAnyObjectByType<ShellView>().Show(ShellScreen.Playing);Time.timeScale=1;
                var gate=player.GetComponent<PlayerGameplayGate>();gate.SetCinematicLock(false);gate.SetState(PlayerGameplayState.PostEntryInvincibility);
                await Until(()=>runner.GetComponentsInChildren<EnemyHealth>().Any(e=>e.CanReceiveDamage),2500,"Death target entry "+name);
                var enemy=runner.GetComponentsInChildren<EnemyHealth>().First(e=>e.CanReceiveDamage);
                if(name=="Bulwark")
                {
                    var attack=enemy.GetComponent<SingleFireAttack>();Check(attack.enabled,"Bulwark gun disabled");
                    UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
                    await Until(()=>attack.IsTelegraphing,4000,"Bulwark did not request shared firing slot");
                    await Task.Delay(40);var cue=Field<SpriteRenderer>(enemy.GetComponent<EnemyFirePresentation>(),"chargeCue");Check(cue.enabled,"Bulwark warning invisible");
                    float chargeStart=Time.time;Time.timeScale=0;await Task.Delay(40);var frozenCharge=cue.sprite;await Task.Delay(150);Check(attack.IsTelegraphing&&cue.sprite==frozenCharge,"Bulwark charge ignored Pause: telegraph="+attack.IsTelegraphing+" frame="+frozenCharge?.name+" -> "+cue.sprite?.name);Time.timeScale=1;
                    await Capture("bulwark-charge");
                    await Until(()=>UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Any(p=>p.Owner==enemy.gameObject),2000,"Bulwark never fired");
                    float fired=Time.time;Check(fired-chargeStart>.7f,"Bulwark shot before warning");
                    var shot=UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Single(p=>p.Owner==enemy.gameObject);
                    Check(shot.GetComponentInChildren<SpriteRenderer>().sprite.name.StartsWith("hostile-lemon",StringComparison.Ordinal),"Bulwark projectile family wrong");
                    Check(runner.GetComponent<EncounterFireCoordinator>().BusyCount<=encounter.ActiveShooterLimit,"Bulwark bypassed firing cap");
                    await Capture("bulwark-shot");await Until(()=>attack.IsTelegraphing,6500,"Bulwark cooldown never ended");Check(Time.time-fired>=4.95f,"Bulwark fired too rapidly");
                }
                UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().Block();
                if(name=="SupplyDrone")
                {
                    Check(enemy.MaxHp==6,"Supply durability not six HP");
                    for(int hit=0;hit<5;hit++){enemy.ReceivePlayerHit(1,player.gameObject);Check(enemy.IsAlive,"Supply died before sixth hit");}
                }
                if(name.EndsWith("Asteroid",StringComparison.Ordinal))
                {
                    enemy.ReceivePlayerHit(Mathf.CeilToInt(enemy.MaxHp*.8f),player.gameObject);await Task.Delay(40);
                    var damage=enemy.GetComponent<EnemyDamagePresentation>();var hull=Field<SpriteRenderer>(damage,"hull");
                    Check(enemy.IsAlive&&damage.DamageStage==2&&hull.sprite!=null&&hull.sprite.name.Contains("critical"),"Asteroid critical fallback missing "+name);
                    await Capture("critical-"+name.ToLowerInvariant());
                }
                enemy.ReceivePlayerHit(enemy.CurrentHp,player.gameObject);
                Check(!enemy.gameObject.activeSelf,"One-shot did not return hull to pool");
                var visual=bursts.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="DeathBurst"&&r.gameObject.activeSelf);
                Check(visual.enabled&&visual.sprite!=null,"Pooled enemy lost death explosion");
                var first=visual.sprite;await Task.Delay(110);Check(visual.gameObject.activeSelf&&visual.sprite!=first,"Explosion did not animate after enemy disable");
                if(name=="Scout"&&previous!=null){Check(visual==previous,"Death burst did not reuse pool");reused=true;}
                if(name=="Scout")previous=visual;
                Time.timeScale=0;var frozen=visual.sprite;await Task.Delay(100);Check(visual.sprite==frozen,"Paused explosion advanced");
                await Capture("death-"+name.ToLowerInvariant());Time.timeScale=1;
                await Until(()=>!visual.gameObject.activeSelf,1000,"Death burst not recycled");
            }
            Check(rewards==1,"Presentation changed reward eligibility");Check(reused,"Pool reuse not exercised");
        }
        finally
        {
            runner.EnemyDefeated-=reward;Time.timeScale=1;runner.StopRun();save.ExitIsolated();runner.Flow.LoadLevel(oldLevel);save.UseTestStorage(original);shell.Boot();pointer.enabled=oldPointer;player.enabled=oldDamage;
            UnityEngine.Object.Destroy(level);UnityEngine.Object.Destroy(encounter);
            foreach(var p in bytes){var after=File.Exists(original+p.Key)?File.ReadAllBytes(original+p.Key):null;Check(p.Value==null?after==null:after!=null&&p.Value.SequenceEqual(after),"Production save changed");}
            if(Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
        }
        var result=new{status="PASS",oneShotSurvivesHullDisable=true,noRewardEnemyExplodes=true,poolReuse=true,pause=true,supplySixHits=true,asteroidBurst=true,bothAsteroidCriticalSprites=true,rewardEligibilityPreserved=true,productionSaveGuard=true};
        File.WriteAllText(Output+"bulwark.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="PASS",singleShot=true,warningBeforeShot=true,pause=true,cooldownFiveSeconds=true,sharedFiringCap=true},Newtonsoft.Json.Formatting.Indented));
        File.WriteAllText(Output+"deaths.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static async Task<object> Run(int targetWidth=1600)
    {
        Check(Application.isPlaying,"Play Mode required");width=targetWidth;
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();Check(!runner.HasActiveRun,"Idle Menu required");
        var save=runner.GetComponent<CampaignLifecycle>();var shell=runner.GetComponent<GameShell>();var session=runner.GetComponent<GameplaySession>();
        var oldLevel=runner.Flow.Level;var original=save.SavePath;var bytes=new[]{"",".bak",".invalid"}.ToDictionary(s=>s,s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null);
        var temp=Path.Combine(Path.GetTempPath(),"starshift-vfx-polish-"+Guid.NewGuid().ToString("N"),"save.json");
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerDamageLifecycle>();var pointer=player.GetComponent<InputSystemPointerInput>();var gate=player.GetComponent<PlayerGameplayGate>();
        bool oldPointer=pointer.enabled,oldDamage=player.enabled;
        var gameType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");var window=Resources.FindObjectsOfTypeAll(gameType).First();var size=gameType.GetProperty("selectedSizeIndex");var oldSize=size.GetValue(window);
        var created=new List<GameObject>();var view=Camera.main.GetComponent<GameplayViewController>();var oldView=view.TargetView;
        try
        {
            save.UseTestStorage(temp);shell.Boot();pointer.enabled=false;player.enabled=false;
            gameType.GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(width,900),"StarShift Polish "+width+"x900"});
            await Until(()=>Screen.width==width&&Screen.height==900,2500,"Resolution");
            UnityEngine.Object.FindAnyObjectByType<ShellView>().Show(ShellScreen.Playing);Time.timeScale=1;
            view.RequestView(new GameplayViewSettings(9,new Rect(0,0,1,1)),0);gate.SetCinematicLock(false);gate.SetState(PlayerGameplayState.PostEntryInvincibility);
            foreach(var name in new[]{"Scout","StormAsteroid"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/Enemies/"+name+".prefab");var root=UnityEngine.Object.Instantiate(prefab);created.Add(root);root.SetActive(false);
                var health=root.GetComponent<EnemyHealth>();health.ConfigureModifier(null,100);
                foreach(var attack in root.GetComponentsInChildren<SingleFireAttack>(true))attack.enabled=false;
                var flight=root.GetComponent<EntryHoldFlight>();flight.Configure(new Vector2(.5f,.55f),new Vector2(.5f,.55f),Vector2.zero,.01f,new Vector2(.5f,1.3f),60,0);
                root.SetActive(true);await Until(()=>health.CanReceiveDamage,2000,"Target entry "+name);
                if(name=="Scout")foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})await Impact(health,player,branch,"hit-"+branch.ToString().ToLowerInvariant());
                else
                {
                    root.GetComponent<AsteroidVisualMotion>().Configure(30);await Impact(health,player,WeaponBranch.Pulse,"hit-rock");
                    root.SetActive(false);root.SetActive(true);Check(!Field<SpriteRenderer>(root.GetComponent<EnemyHitPresentation>(),"impact").enabled,"Pool reuse kept impact");
                }
                UnityEngine.Object.Destroy(root);created.Remove(root);await Task.Delay(50);
            }
            foreach(var mission in new[]{"L4","L8"})
            {
                runner.StopRun();runner.Flow.LoadLevel(runner.GetComponent<RegionCampaignFlow>().FindMission(mission));runner.StartRun("BOSS",true,true,null,null);
                gate.SetCinematicLock(false);gate.SetState(PlayerGameplayState.PostEntryInvincibility);UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
                await Until(()=>runner.GetComponentInChildren<BossPhaseRunner>()?.GetComponent<EntryHoldFlight>().IsHolding==true,12000,"Boss entry "+mission);
                var driver=runner.GetComponentInChildren<BossPhaseRunner>();driver.enabled=false;var boss=driver.Health;
                UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ClearProjectiles();
                var modules=boss.GetComponentsInChildren<BossAttackModule>();
                var barrageModule=modules.Single(m=>m.GetComponent<BossBarrage>()!=null);
                var beamModule=modules.First(m=>Field<AttackBeamPresentation>(m,"beam")!=null||Field<DirectedAttack>(m,"directed")!=null);
                await Skill(barrageModule,boss,mission=="L4"?"vanguard-curtain":"warden-ion-shelter");
                await Skill(beamModule,boss,mission=="L4"?"vanguard-lane-beam":"warden-beam-sweep");
                if(mission=="L4")
                {
                    boss.ReceivePlayerHit(Mathf.CeilToInt(boss.MaxHp*.52f),player.gameObject);await Task.Delay(90);
                    Check(boss.GetComponent<BossArmorBreakPresentation>()!=null&&boss.transform.Find("ArmorBreakFragment0").GetComponent<SpriteRenderer>().enabled,"First boss armor piece missing");await Capture("vanguard-armor-break-1");
                    boss.ReceivePlayerHit(Mathf.CeilToInt(boss.MaxHp*.24f),player.gameObject);await Task.Delay(90);
                    Check(boss.transform.Find("ArmorBreakFragment1").GetComponent<SpriteRenderer>().enabled,"Second boss armor piece missing");await Capture("vanguard-armor-break-2");
                }
                else
                {
                    var shields=boss.GetComponent<BossShieldNetwork>();var node=boss.GetComponentsInChildren<EnemyHealth>().First(n=>n!=boss);
                    node.ReceivePlayerHit(node.MaxHp/2,player.gameObject);await Task.Delay(60);
                    var percentage=Field<Text>(UnityEngine.Object.FindAnyObjectByType<BossHud>(),"percentage");
                    Check(percentage.text==Mathf.CeilToInt(100f*shields.CurrentShieldHp/shields.MaximumShieldHp)+"%"&&percentage.cachedTextGenerator.vertexCount>=12,"Shield percentage not rendered/mismatched");await Capture("warden-shield-percentage");
                }
            }
        }
        finally
        {
            foreach(var root in created)if(root!=null)UnityEngine.Object.Destroy(root);
            foreach(var driver in runner.GetComponentsInChildren<BossPhaseRunner>(true))driver.enabled=true;
            runner.StopRun();save.ExitIsolated();runner.Flow.LoadLevel(oldLevel);save.UseTestStorage(original);shell.Boot();pointer.enabled=oldPointer;player.enabled=oldDamage;size.SetValue(window,oldSize);view.RequestView(oldView,0);
            foreach(var p in bytes){var after=File.Exists(original+p.Key)?File.ReadAllBytes(original+p.Key):null;Check(p.Value==null?after==null:after!=null&&p.Value.SequenceEqual(after),"Production save changed");}
            if(Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
        }
        var result=new{status="PASS",width,actualProjectileFamilies=3,asteroidImpactAndPool=true,bossSpecialSkills=4,alternatingCurtainRows=true,twoVanguardArmorBreaks=true,wardenShieldPercentage=true,productionSaveGuard=true};
        File.WriteAllText(Output+"vfx-"+width+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
