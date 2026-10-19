using System;
using System.Linq;
using System.Reflection;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class ConfigureRegionA
{
 const string D="Assets/StarShift/Data/", R=D+"RegionA/", P="Assets/StarShift/Prefabs/Enemies/";
 static T Load<T>(string p) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(p);
 public static void Set(UnityEngine.Object o,string f,object v){o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);EditorUtility.SetDirty(o);}
 static T Asset<T>(string n,Action<T> setup) where T:ScriptableObject
 {var a=Load<T>(R+n+".asset");if(a!=null)return a;a=ScriptableObject.CreateInstance<T>();setup(a);AssetDatabase.CreateAsset(a,R+n+".asset");return a;}
 static SingleFireDefinition Fire(string n,int count,float spread,float cooldown)
 =>Asset<SingleFireDefinition>(n,x=>{var old=Load<SingleFireDefinition>(D+"Enemies/ScoutSingleFire.asset");Set(x,"projectilePrefab",old.ProjectilePrefab);Set(x,"initialDelay",.8f);Set(x,"telegraphDuration",count>1?.7f:.4f);Set(x,"cooldown",cooldown);Set(x,"projectileCount",count);Set(x,"spreadDegrees",spread);});
 static GameObject Enemy(string n,int hp,int score,Color color,Vector2 size,SingleFireDefinition fire,bool large=false)
 {
  var saved=Load<GameObject>(P+n+".prefab");if(saved!=null)return saved;
  var def=Asset<EnemyDefinition>(n,x=>{Set(x,"maxHp",hp);Set(x,"score",score);Set(x,"missileTier",large?MissileDamageTier.Large:MissileDamageTier.Normal);});
  var root=PrefabUtility.LoadPrefabContents(P+"Scout.prefab");
  try{
   root.name=n;Set(root.GetComponent<EnemyHealth>(),"definition",def);var attack=root.GetComponent<SingleFireAttack>();attack.enabled=fire!=null;if(fire!=null)Set(attack,"definition",fire);
   var hull=root.GetComponentInChildren<SpriteRenderer>();hull.color=color;hull.transform.localScale=new Vector3(size.x,size.y,1);
   if(n.Contains("Asteroid"))hull.sprite=Load<Sprite>("Assets/StarShift/Art/Placeholders/asteroid-placeholder.png");
   if(n=="Bomber")for(int i=0;i<2;i++){var pod=new GameObject("EnginePod"+i).AddComponent<SpriteRenderer>();pod.transform.SetParent(hull.transform.parent,false);pod.sprite=hull.sprite;pod.color=new Color(.6f,.22f,.16f);pod.transform.localPosition=new Vector3(i==0?-.5f:.5f,-.1f,0);pod.transform.localScale=new Vector3(.24f,.6f,1);}
   var accent=new GameObject("EliteAccent").AddComponent<SpriteRenderer>();accent.transform.SetParent(root.transform,false);accent.sprite=Load<Sprite>("Assets/StarShift/Art/Placeholders/impact-glow-placeholder.png");accent.transform.localScale=new Vector3(size.x*1.5f,size.y*.22f,1);accent.transform.localPosition=new Vector3(0,.45f,0);accent.enabled=false;accent.sortingOrder=6;Set(root.AddComponent<EnemyModifierPresentation>(),"accent",accent);
   return PrefabUtility.SaveAsPrefabAsset(root,P+n+".prefab");
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 static FlightSequence Path(string n,Vector2 from,Vector2 hold,Vector2 exit,FlightPath entry=FlightPath.Straight,FlightPath combat=FlightPath.Straight,float seconds=10,bool terminal=false,Vector2 curve=default)
 =>Asset<FlightSequence>(n,x=>{x.Phases=new[]{new FlightPhase{Kind=FlightPhaseKind.Entry,Path=entry,From=from,To=hold,Curve=curve,Duration=2.2f},new FlightPhase{Kind=FlightPhaseKind.Combat,Path=combat,From=hold,To=hold,Curve=curve,Duration=seconds,CanShoot=true,ClampToView=true,HoldUntilResolved=terminal,Cycles=2},new FlightPhase{Kind=FlightPhaseKind.Exit,From=hold,To=exit,Duration=3.5f,CanShoot=true}};if(terminal)x.Phases=x.Phases.Take(2).ToArray();});
 static SpawnAction G(GameObject enemy,FlightSequence flight,int count,float start=0,FormationLayout form=FormationLayout.Line,Vector2 anchor=default,RewardDropTable reward=null,EnemyModifierDefinition elite=null)
 =>new SpawnAction{EnemyPrefab=enemy,EnemyDefinition=enemy.GetComponent<EnemyHealth>().Definition,Sequence=flight,Count=count,StartTime=start,Stagger=.24f,Formation=form,SlotStep=new Vector2(.115f,.075f),FormationRise=.035f,Anchor=anchor,RewardOverride=reward,Elite=elite};
 static EncounterDefinition E(string n,int index,EncounterCompletion mode,float seconds,SpawnAction[] groups,OptionalEvent[] events=null)
 =>Asset<EncounterDefinition>(n,x=>{x.Id="ENCOUNTER_"+index;x.Completion=mode;x.Duration=seconds;x.Groups=groups;x.OptionalEvents=events??Array.Empty<OptionalEvent>();x.ActiveShooterLimit=1;x.ShotSpacing=.85f;x.ShotsPerVolley=2;x.VolleyPause=2.2f;x.ClearPauseSeconds=1;x.HandoffTravelSeconds=1.5f;x.RewardGraceSeconds=2.5f;x.CompleteWhenScheduleExhausted=mode==EncounterCompletion.Timeline;});
 static OptionalEvent Event(string id,OptionalEventKind kind,float time,SpawnAction g)=>new OptionalEvent{Id=id,Kind=kind,TimeOffset=time,Group=g};
 static LevelDefinition M(int n,EncounterDefinition[] encounters)=>Asset<LevelDefinition>("Mission0"+n,x=>{x.Id="L"+n;x.MissionNumber=n;x.DisplayName="NHIỆM VỤ "+n.ToString("00");x.Camera=Load<CameraProfile>(D+"Cameras/Normal.asset");x.Encounters=encounters;x.Intro=new MissionIntroSettings{ArrivalTitle=x.DisplayName};});
 public static object BatchAB()
 {
  var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.isDirty)throw new InvalidOperationException("Saved scene/Edit Mode required.");
  if(!AssetDatabase.IsValidFolder(R.TrimEnd('/')))AssetDatabase.CreateFolder(D.TrimEnd('/'),"RegionA");
  var scout=Load<GameObject>(P+"Scout.prefab");var raider=Enemy("Raider",3,150,new Color(.85f,.3f,.5f),new Vector2(.45f,.8f),Fire("RaiderFire",1,0,2.2f));
  var bomber=Enemy("Bomber",6,250,new Color(.9f,.4f,.16f),new Vector2(.85f,.65f),Fire("BomberSpread",3,40,3.6f),true);
  var asteroid=Enemy("StormAsteroid",2,0,new Color(.5f,.52f,.57f),new Vector2(.6f,.6f),null);
  var resource=Load<GameObject>(P+"ResourceAsteroid.prefab");var supply=Load<GameObject>(P+"SupplyDrone.prefab");
  var normal=Load<RewardDropTable>(D+"Rewards/NormalDrops.asset");var gold=Load<RewardDropTable>(D+"Rewards/ResourceDrops.asset");var crate=Load<RewardDropTable>(D+"Rewards/SupplyCrateDrops.asset");
  var elite=Asset<EnemyModifierDefinition>("Elite",x=>{});var eliteDrops=Asset<RewardDropTable>("EliteDrops",x=>{x.Source=RewardSource.Elite;x.MajorChance=.065f;x.ShardChance=.25f;x.Shard=normal.Shard;x.MajorItems=normal.MajorItems;});
  var core=Asset<RewardDropTable>("OnboardingPulseCore",x=>x.GuaranteedPickup=Load<PickupDefinition>(D+"Rewards/PulseCore.asset"));
  var hold=Path("TopHold",new Vector2(.5f,1.2f),new Vector2(.5f,.76f),Vector2.zero,terminal:true);
  var left=Path("ArcLeft",new Vector2(.72f,1.22f),new Vector2(.43f,.73f),new Vector2(-.4f,.55f),FlightPath.ArcLeft,curve:new Vector2(.22f,.04f));
  var right=Path("ArcRight",new Vector2(.28f,1.22f),new Vector2(.57f,.76f),new Vector2(1.4f,.55f),FlightPath.ArcRight,curve:new Vector2(.22f,.04f));
  var sine=Path("SineFormation",new Vector2(.5f,1.22f),new Vector2(.5f,.72f),new Vector2(.5f,1.4f),combat:FlightPath.Sine,seconds:13,curve:new Vector2(.2f,.065f));
  var pincL=Path("PincerLeft",new Vector2(-.35f,.9f),new Vector2(.42f,.72f),new Vector2(.85f,1.35f),FlightPath.Pincer,FlightPath.Sine,11,false,new Vector2(.13f,.07f));
  var pincR=Path("PincerRight",new Vector2(1.35f,.85f),new Vector2(.58f,.62f),new Vector2(.15f,1.35f),FlightPath.Pincer,FlightPath.Sine,11,false,new Vector2(-.13f,.06f));
  var storm=Asset<FlightSequence>("DiagonalStorm",x=>x.Phases=new[]{new FlightPhase{Kind=FlightPhaseKind.Entry,From=new Vector2(.2f,1.3f),To=new Vector2(.28f,1.02f),Duration=1},new FlightPhase{Kind=FlightPhaseKind.Exit,Path=FlightPath.DiagonalPass,From=new Vector2(.28f,1.02f),To=new Vector2(.9f,-.3f),Duration=7,Smooth=false}});
  var supplyPath=Path("SupplyWindow",new Vector2(.5f,1.18f),new Vector2(.5f,.65f),new Vector2(1.3f,.65f),seconds:8);
  var a=E("M1Foundation",1,EncounterCompletion.Eliminate,30,new[]{G(scout,hold,3),G(scout,hold,1,3,anchor:new Vector2(0,-.15f),reward:core)});
  var b=E("M1Formation",2,EncounterCompletion.Timeline,32,new[]{G(scout,left,5,form:FormationLayout.V),G(scout,right,5,13,FormationLayout.Staggered)});
  var c=E("M2RaiderArcs",1,EncounterCompletion.Timeline,38,new[]{G(raider,left,5,form:FormationLayout.V),G(raider,right,5,15,FormationLayout.Staggered),G(scout,left,3,21)});
  var d=E("M2Storm",2,EncounterCompletion.Survive,14,new[]{G(asteroid,storm,3),G(asteroid,storm,3,5,anchor:new Vector2(.25f,0)),G(resource,storm,1,2,reward:gold),G(resource,storm,1,7,anchor:new Vector2(.35f,0),reward:gold)});
  var e=E("M2Bomber",3,EncounterCompletion.Eliminate,40,new[]{G(bomber,hold,2,anchor:new Vector2(0,.03f)),G(scout,hold,5,2,FormationLayout.Wedge,new Vector2(0,-.2f))});
  var f=E("M3Sine",1,EncounterCompletion.Timeline,42,new[]{G(raider,sine,6,form:FormationLayout.Staggered),G(raider,sine,6,18,FormationLayout.V)});
  var g=E("M3PincerSupply",2,EncounterCompletion.Timeline,42,new[]{G(scout,pincL,4,0,FormationLayout.Column),G(raider,pincR,4,3,FormationLayout.Staggered),G(scout,right,4,26)},new[]{Event("FIRST_SUPPLY",OptionalEventKind.SupplyDrone,17,G(supply,supplyPath,1,reward:crate))});
  var h=E("M3Elite",3,EncounterCompletion.Eliminate,45,new[]{G(bomber,hold,1,reward:eliteDrops,elite:elite),G(raider,hold,5,3,FormationLayout.V,new Vector2(0,-.2f))});
  E("M4ResourceStorm",1,EncounterCompletion.Survive,16,new[]{G(asteroid,storm,3),G(asteroid,storm,2,7,anchor:new Vector2(.25f,0)),G(resource,storm,1,2,reward:gold),G(resource,storm,1,8,anchor:new Vector2(.28f,0),reward:gold)},new[]{Event("PREBOSS_SUPPLY",OptionalEventKind.SupplyDrone,4,G(supply,supplyPath,1,reward:crate))});
  var missions=new[]{M(1,new[]{a,b}),M(2,new[]{c,d,e}),M(3,new[]{f,g,h})};foreach(var m in missions)if(!m.IsValid(out var error))throw new InvalidOperationException(error);
  Set(UnityEngine.Object.FindAnyObjectByType<LevelFlow>(),"level",missions[0]);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  return missions.Select(m=>new{m.Id,count=m.Encounters.Length}).ToArray();
 }
 public static object BatchC()
 {
  var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.isDirty)throw new InvalidOperationException("Saved scene/Edit Mode required.");
  var bossDef=Asset<EnemyDefinition>("VanguardHealth",x=>{Set(x,"maxHp",240);Set(x,"score",2500);Set(x,"missileTier",MissileDamageTier.Boss);});
  var phases=Asset<BossPhaseDefinition>("VanguardPhases",x=>{});
  var cannon=Fire("VanguardCannon",1,0,2);var missile=Fire("VanguardMissiles",3,28,3);var sweep=Fire("VanguardSweep",5,64,3);
  Set(cannon,"projectileSpeed",4f);Set(cannon,"telegraphDuration",.55f);Set(missile,"projectileSpeed",3f);Set(missile,"telegraphDuration",1f);Set(sweep,"projectileSpeed",3.6f);Set(sweep,"telegraphDuration",.85f);
  var boss=Load<GameObject>(P+"VanguardFrigate.prefab");
  if(boss==null){
   var root=PrefabUtility.LoadPrefabContents(P+"Bomber.prefab");
   try{
    root.name="VanguardFrigate";Set(root.GetComponent<EnemyHealth>(),"definition",bossDef);root.GetComponent<SingleFireAttack>().enabled=false;
    var oldModifier=root.GetComponent<EnemyModifierPresentation>();UnityEngine.Object.DestroyImmediate(oldModifier);UnityEngine.Object.DestroyImmediate(root.transform.Find("EliteAccent").gameObject);
    var hull=root.GetComponentInChildren<SpriteRenderer>();hull.transform.localScale=new Vector3(3.8f,2.2f,1);hull.color=new Color(.42f,.5f,.6f);
    var hit=root.GetComponentInChildren<BoxCollider2D>();hit.size=new Vector2(3.4f,1.5f);
    var modules=new BossAttackModule[3];
    var configs=new[]{cannon,missile,sweep};
    for(int i=0;i<3;i++){
     var node=new GameObject("WeaponModule"+i);node.transform.SetParent(root.transform,false);
     var cue=node.AddComponent<SpriteRenderer>();cue.sprite=hull.sprite;cue.color=i==1?new Color(1,.6f,.15f):new Color(1,.22f,.3f);cue.sortingOrder=5;node.transform.localPosition=new Vector3(0,-.35f-i*.1f,0);node.transform.localScale=new Vector3(.5f,.3f,1);
     var muzzles=new Transform[i==0?2:1];for(int j=0;j<muzzles.Length;j++){var muzzle=new GameObject("Muzzle"+j).transform;muzzle.SetParent(root.transform,false);muzzle.localPosition=new Vector3(i==0?(j==0?-1.1f:1.1f):0,-.7f,0);muzzles[j]=muzzle;}
     modules[i]=node.AddComponent<BossAttackModule>();Set(modules[i],"definition",configs[i]);Set(modules[i],"muzzles",muzzles);Set(modules[i],"telegraph",cue);
    }
    var phaseCue=new GameObject("PhaseCue").AddComponent<SpriteRenderer>();phaseCue.transform.SetParent(root.transform,false);phaseCue.sprite=Load<Sprite>("Assets/StarShift/Art/Placeholders/impact-glow-placeholder.png");phaseCue.color=new Color(1,.7f,.15f,.7f);phaseCue.transform.localScale=new Vector3(3,1,1);phaseCue.sortingOrder=6;phaseCue.enabled=false;
    var runner=root.AddComponent<BossPhaseRunner>();Set(runner,"definition",phases);Set(runner,"modules",modules);Set(runner,"phaseCue",phaseCue);
    var explosions=new SpriteRenderer[5];for(int i=0;i<5;i++){var e=new GameObject("DeathBeat"+i).AddComponent<SpriteRenderer>();e.transform.SetParent(root.transform,false);e.transform.localPosition=new Vector3(i==4?0:(i%2==0?-1:1)*.8f,i==4?0:.3f-i*.13f,0);e.sprite=phaseCue.sprite;e.color=new Color(1,.65f,.2f,.9f);e.enabled=false;e.sortingOrder=8;explosions[i]=e;}
    Set(root.AddComponent<BossDeathSequence>(),"explosions",explosions);
    boss=PrefabUtility.SaveAsPrefabAsset(root,P+"VanguardFrigate.prefab");
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  var flight=Path("VanguardFlight",new Vector2(.5f,1.3f),new Vector2(.5f,.77f),Vector2.zero,combat:FlightPath.Sine,seconds:7,terminal:true,curve:new Vector2(.16f,0));
  flight.Phases[1].RepeatMotion=true;EditorUtility.SetDirty(flight);
  var crate=Load<RewardDropTable>(D+"Rewards/SupplyCrateDrops.asset");
  var encounter=E("M4Vanguard",2,EncounterCompletion.Boss,120,new[]{G(boss,flight,1,reward:crate)});encounter.BossPostDeathSeconds=3;encounter.RewardGraceSeconds=5;encounter.CameraOverride=Asset<CameraProfile>("BossCamera",x=>x.View=new GameplayViewSettings(7.5f,new Rect(0,0,1,1)));EditorUtility.SetDirty(encounter);
  var m4=M(4,new[]{Load<EncounterDefinition>(R+"M4ResourceStorm.asset"),encounter});m4.AdditionalStarts=new[]{new LevelStartPoint{Id="PRE_BOSS",Encounter=m4.Encounters[0]},new LevelStartPoint{Id="BOSS",Encounter=encounter},new LevelStartPoint{Id="BOSS_PHASE_B",Encounter=encounter}};EditorUtility.SetDirty(m4);
  var missions=Enumerable.Range(1,4).Select(n=>Load<LevelDefinition>(R+"Mission0"+n+".asset")).ToArray();
  for(int i=0;i<4;i++){missions[i].ReplayPreset=new ReplayLoadoutPreset{WeaponLevels=i==0?new[]{1,0,0}:i==1?new[]{2,0,0}:new[]{2,1,1}};EditorUtility.SetDirty(missions[i]);}
  var rootScene=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>().gameObject;
  var flow=rootScene.GetComponent<RegionCampaignFlow>()??rootScene.AddComponent<RegionCampaignFlow>();Set(flow,"missions",missions);
  Set(rootScene.GetComponent<LevelFlow>(),"level",missions[0]);
  ConfigureBossHud(rootScene.GetComponent<EncounterRunner>());
  foreach(var m in missions)if(!m.IsValid(out var error))throw new InvalidOperationException(error);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return new{missions=missions.Length,encounters=missions.Sum(m=>m.Encounters.Length),bossHp=bossDef.MaxHp};
 }
 static void ConfigureBossHud(EncounterRunner runner)
 {
  var safe=GameObject.Find("GameplayUI").transform.Find("SafeArea");if(safe.Find("BossHud")!=null)return;
  var node=new GameObject("BossHud",typeof(RectTransform),typeof(CanvasGroup),typeof(StarShift.UI.BossHud));node.transform.SetParent(safe,false);
  var rect=node.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.27f,1);rect.anchorMax=new Vector2(.73f,1);rect.pivot=new Vector2(.5f,1);rect.offsetMin=new Vector2(0,-110);rect.offsetMax=new Vector2(0,-60);
  var group=node.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
  var labelNode=new GameObject("Name",typeof(RectTransform),typeof(UnityEngine.UI.Text));labelNode.transform.SetParent(node.transform,false);
  var text=labelNode.GetComponent<UnityEngine.UI.Text>();text.font=GameObject.Find("GameplayUI").GetComponentInChildren<UnityEngine.UI.Text>(true).font;text.fontSize=22;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;
  var tr=labelNode.GetComponent<RectTransform>();tr.anchorMin=new Vector2(0,.45f);tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
  var bg=new GameObject("Track",typeof(RectTransform),typeof(UnityEngine.UI.Image));bg.transform.SetParent(node.transform,false);bg.GetComponent<UnityEngine.UI.Image>().color=new Color(.2f,.12f,.15f);bg.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
  var br=bg.GetComponent<RectTransform>();br.anchorMin=Vector2.zero;br.anchorMax=new Vector2(1,.3f);br.offsetMin=br.offsetMax=Vector2.zero;
  var fill=new GameObject("Fill",typeof(RectTransform),typeof(UnityEngine.UI.Image));fill.transform.SetParent(bg.transform,false);var image=fill.GetComponent<UnityEngine.UI.Image>();image.color=new Color(1,.3f,.25f);image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;image.raycastTarget=false;
  var fr=fill.GetComponent<RectTransform>();fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=fr.offsetMax=Vector2.zero;
  var hud=node.GetComponent<StarShift.UI.BossHud>();Set(hud,"runner",runner);Set(hud,"group",group);Set(hud,"label",text);Set(hud,"fill",image);
 }
 public static object CuesAndBurst()
 {
  if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
  Set(Load<SingleFireDefinition>(R+"RaiderFire.asset"),"burstCount",2);
  foreach(var name in new[]{"StormAsteroid","ResourceAsteroid"}){
   var path=P+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
   try{
    if(root.GetComponent<EnemyEntryTelegraph>()==null){
     var marker=new GameObject("EntryWarning").AddComponent<SpriteRenderer>();marker.transform.SetParent(root.transform,false);
     marker.sprite=Load<Sprite>("Assets/StarShift/Art/Placeholders/player-placeholder.png");marker.color=new Color(1,.65f,.2f,.8f);marker.sortingOrder=8;
     marker.transform.localScale=Vector3.one*.3f;marker.transform.localRotation=Quaternion.Euler(0,0,180);
     Set(root.AddComponent<EnemyEntryTelegraph>(),"cue",marker);
     PrefabUtility.SaveAsPrefabAsset(root,path);
    }
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
  if(runner.GetComponent<StarShift.UI.EncounterCuePresentation>()==null)runner.gameObject.AddComponent<StarShift.UI.EncounterCuePresentation>();
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{cues=true,raiderBurst=2};
 }
}
