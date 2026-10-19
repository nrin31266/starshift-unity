using System;
using System.Linq;
using System.Reflection;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class ConfigurePacingRefinement
{
 const string R="Assets/StarShift/Data/RegionA/",P="Assets/StarShift/Prefabs/Enemies/";
 static T L<T>(string name) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(R+name+".asset");
 static GameObject Enemy(string n)=>AssetDatabase.LoadAssetAtPath<GameObject>(P+n+".prefab");
 static void Set(UnityEngine.Object o,string f,object v){o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);EditorUtility.SetDirty(o);}
 static T A<T>(string name,Action<T> update) where T:ScriptableObject{
  var x=L<T>(name);if(x==null){x=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(x,R+name+".asset");}update(x);EditorUtility.SetDirty(x);return x;
 }
 static FlightSequence Wing(string n,bool right,bool terminal=false,bool sine=false,float entry=2.6f,float hold=7)
 =>A<FlightSequence>(n,x=>{
  var from=new Vector2(right?1.12f:-.12f,1.05f);var to=new Vector2(right?.65f:.35f,.75f);
  x.Phases=new[]{new FlightPhase{Kind=FlightPhaseKind.Entry,Path=right?FlightPath.ArcLeft:FlightPath.ArcRight,From=from,To=to,Curve=new Vector2(.18f,.06f),Duration=entry},new FlightPhase{Kind=FlightPhaseKind.Combat,Path=sine?FlightPath.Sine:FlightPath.Straight,From=to,To=to,Curve=new Vector2(right?-.12f:.12f,.045f),Cycles=1,Duration=hold,CanShoot=true,ClampToView=true,HoldUntilResolved=terminal},new FlightPhase{Kind=FlightPhaseKind.Exit,From=to,To=new Vector2(right?1.3f:-.3f,1.15f),Duration=2.4f,CanShoot=true}};
  if(terminal)x.Phases=x.Phases.Take(2).ToArray();
 });
 static SpawnAction G(string enemy,FlightSequence path,int count,int beat=0,float offset=0,bool right=false)
 =>new SpawnAction{EnemyPrefab=Enemy(enemy),EnemyDefinition=Enemy(enemy).GetComponent<EnemyHealth>().Definition,Count=count,Sequence=path,EntryBeat=beat,StartTime=offset,Stagger=.18f,Formation=FormationLayout.Custom,SlotStep=new Vector2(right?-.085f:.085f,.045f)};
 static void E(string n,string title,string detail,SpawnAction[] groups,bool adaptive=false){
  var e=L<EncounterDefinition>(n);e.Groups=groups;e.EntryBeats=adaptive?new[]{new EntryBeatRule{MinimumGap=2.2f,RemainingThreshold=2,FallbackDelay=7}}:Array.Empty<EntryBeatRule>();
  e.EntryLeadSeconds=.5f;e.TitleSeconds=1.3f;e.TransitionTitle=title;e.TransitionDescription=detail;e.ClearPauseSeconds=.7f;e.HandoffTravelSeconds=.85f;e.ProjectileGraceSeconds=.45f;e.ProjectileFadeSeconds=.25f;
  EditorUtility.SetDirty(e);
 }
 public static object Configure(){
  var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.isDirty)throw new InvalidOperationException("Saved scene/Edit Mode required");
  var left=Wing("FormationLeft",false);var right=Wing("FormationRight",true);var holdLeft=Wing("FormationHoldLeft",false,true);var holdRight=Wing("FormationHoldRight",true,true);
  var sineLeft=Wing("SineWingLeft",false,false,true);var sineRight=Wing("SineWingRight",true,false,true);
  var core=L<EncounterDefinition>("M1Foundation").Groups.Last().RewardOverride;
  var carrier=G("Scout",L<FlightSequence>("TopHold"),1,offset:.2f);carrier.Anchor=new Vector2(0,-.12f);carrier.SpawnScale=1.2f;carrier.RewardOverride=core;
  E("M1Foundation","ĐỢT 1 · ĐỘI HÌNH TIÊN PHONG","Quét sạch nhóm Scout đang áp sát.",new[]{G("Scout",holdLeft,2),G("Scout",holdRight,2,offset:.12f,right:true),carrier});
  E("M1Formation","ĐỢT 2 · VÒNG CUNG TẤN CÔNG","Phá đội hình trước khi chúng rời khu vực.",new[]{G("Scout",left,3),G("Scout",right,3,offset:.12f,right:true),G("Scout",left,2,1),G("Scout",right,2,1,.12f,true)},true);
  E("M2RaiderArcs","ĐỢT 1 · ĐỘT KÍCH VÒNG CUNG","Raider đang áp sát từ hai cánh.",new[]{G("Raider",left,3),G("Raider",right,3,offset:.12f,right:true),G("Raider",right,4,1,0,true),G("Scout",left,2,1,.3f)},true);
  var heavy=A<FlightSequence>("HeavyEscort",x=>{x.Phases=new[]{new FlightPhase{Kind=FlightPhaseKind.Entry,From=new Vector2(.5f,1.2f),To=new Vector2(.5f,.79f),Duration=3.2f},new FlightPhase{Kind=FlightPhaseKind.Combat,From=new Vector2(.5f,.79f),To=new Vector2(.5f,.79f),CanShoot=true,ClampToView=true,HoldUntilResolved=true}};});
  var escortLeft=Wing("EscortLeft",false,true,entry:3);var escortRight=Wing("EscortRight",true,true,entry:3);
  foreach(var side in new[]{escortLeft,escortRight}){var target=new Vector2(side==escortLeft?.25f:.75f,.72f);side.Phases[0].To=target;side.Phases[1].From=side.Phases[1].To=target;EditorUtility.SetDirty(side);}
  var bombers=G("Bomber",heavy,2);bombers.Formation=FormationLayout.Line;bombers.SlotStep=new Vector2(.13f,0);
  var tail=G("Scout",L<FlightSequence>("TopHold"),1,offset:.3f);tail.Anchor=new Vector2(0,-.12f);
  E("M2Bomber","ĐỢT 3 · HỎA LỰC HẠNG NẶNG","Bomber đang dẫn đầu đội hình hộ tống.",new[]{bombers,G("Scout",escortLeft,2,offset:.15f),G("Scout",escortRight,2,offset:.3f,right:true),tail});
  E("M3Sine","ĐỢT 1 · ĐỘI HÌNH LƯỢN SÓNG","Giữ đường bắn khi Raider đổi hướng.",new[]{G("Raider",sineLeft,3),G("Raider",sineRight,3,offset:.15f,right:true),G("Raider",sineLeft,3,1),G("Raider",sineRight,3,1,.15f,true)},true);
  var supply=L<EncounterDefinition>("M3PincerSupply").OptionalEvents[0];supply.TimeOffset=2.5f;supply.UsePressureWindow=true;supply.MaximumActiveEnemies=2;supply.FallbackTime=7;
  E("M3PincerSupply","ĐỢT 2 · GỌNG KÌM TIẾP TẾ","Phá vòng vây và chặn mục tiêu tiếp tế.",new[]{G("Scout",left,4),G("Raider",right,4,offset:.15f,right:true),G("Scout",left,2,1),G("Scout",right,2,1,.15f,true)},true);
  var pincer=L<EncounterDefinition>("M3PincerSupply");pincer.EntryBeats[0].WaitForOptionalResolution=true;pincer.EntryBeats[0].MinimumGap=1.2f;EditorUtility.SetDirty(pincer);
  var elite=L<EncounterDefinition>("M3Elite").Groups[0];
  E("M3Elite","ĐỢT 3 · MỤC TIÊU TINH NHUỆ","Một Elite Bomber dẫn đầu đội hình.",new[]{elite,G("Raider",escortLeft,3,offset:.15f),G("Raider",escortRight,2,offset:.3f,right:true)});
  foreach(var pair in new[]{new{n="M2Storm",count=20,seed=522,title="ĐỢT 2 · BÃO THIÊN THẠCH",detail="Giữ khoảng trống và sống sót."},new{n="M4ResourceStorm",count=24,seed=544,title="ĐỢT 1 · VÀNH ĐAI HỖN LOẠN",detail="Thu thập tài nguyên trước trận đánh lớn."}}){
   var old=L<EncounterDefinition>(pair.n);var resources=old.Groups.Where(g=>g.EnemyPrefab==Enemy("ResourceAsteroid")).ToArray();
   E(pair.n,pair.title,pair.detail,resources);var rightFlow=pair.n=="M2Storm";
   var resourceFlight=A<FlightSequence>(pair.n+"ResourceFlow",x=>{
     var from=new Vector2(rightFlow?.2f:.8f,1.15f);var vector=new Vector2(rightFlow?.42f:-.42f,-1);
     var entry=from+vector*.17f;var exit=entry+vector*1.18f;
     x.Phases=new[]{new FlightPhase{Kind=FlightPhaseKind.Entry,From=from,To=entry,Duration=.65f,Smooth=false},new FlightPhase{Kind=FlightPhaseKind.Exit,From=entry,To=exit,Duration=4.1f,Smooth=false}};
   });
   foreach(var resourceGroup in resources){resourceGroup.Sequence=resourceFlight;resourceGroup.Anchor=new Vector2((rightFlow?1:-1)*(resourceGroup.StartTime>5?.25f:0),0);}
   old.HazardStream=A<HazardStreamDefinition>(pair.n+"Stream",x=>{x.Prefab=Enemy("StormAsteroid");x.Count=pair.count;x.Seed=pair.seed;x.DominantDirection=new Vector2(rightFlow?.42f:-.42f,-1);x.AngularVariance=3;x.Interval=new Vector2(.7f,1.05f);x.TransitSeconds=new Vector2(2.2f,3.2f);});EditorUtility.SetDirty(old);
  }
  var boss=L<EncounterDefinition>("M4Vanguard");boss.EntryLeadSeconds=.5f;boss.TitleSeconds=1.4f;EditorUtility.SetDirty(boss);
  var copy=new[]{"Giữ đường bắn và phá đội hình đang áp sát.","Địch cơ động và trường thiên thạch phía trước.","Phá vòng vây và chặn mục tiêu tiếp tế.","Chuẩn bị hỏa lực trước trận đánh lớn."};
  for(int i=1;i<=4;i++){var m=L<LevelDefinition>("Mission0"+i);m.Intro.MissionTitleSeconds=2.1f;m.Intro.MissionEntryLeadSeconds=.75f;m.MissionBriefing=copy[i-1];EditorUtility.SetDirty(m);}
  var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();var existing=UnityEngine.Object.FindAnyObjectByType<GameplayAnnouncement>();
  var wave=existing.transform.parent.Find("EncounterAnnouncement");if(wave==null){wave=UnityEngine.Object.Instantiate(existing.gameObject,existing.transform.parent).transform;wave.name="EncounterAnnouncement";}
  Set(runner.GetComponent<EncounterCuePresentation>(),"encounterAnnouncement",wave.GetComponent<GameplayAnnouncement>());
  foreach(var cue in UnityEngine.Object.FindObjectsByType<GameplayAnnouncement>()){Set(cue,"minorAnchor",new Vector2(.5f,.87f));Set(cue,"minorTitleSize",25);}
  var asteroidPath=P+"StormAsteroid.prefab";var root=PrefabUtility.LoadPrefabContents(asteroidPath);try{foreach(var v in root.GetComponentsInChildren<SpriteRenderer>())if(v.gameObject.name!="HullPlaceholder"&&v.gameObject.name!="EntryWarning")v.gameObject.SetActive(false);var motion=root.GetComponent<AsteroidVisualMotion>()??root.AddComponent<AsteroidVisualMotion>();Set(motion,"visual",root.GetComponentInChildren<SpriteRenderer>().transform);PrefabUtility.SaveAsPrefabAsset(root,asteroidPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
  var resourcePath=P+"ResourceAsteroid.prefab";var resource=PrefabUtility.LoadPrefabContents(resourcePath);try{foreach(var v in resource.GetComponentsInChildren<SpriteRenderer>()){if(v.gameObject.name=="HullPlaceholder"){v.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/Placeholders/asteroid-placeholder.png");v.color=new Color(.9f,.65f,.2f);}else if(v.gameObject.name!="EntryWarning")v.gameObject.SetActive(false);}PrefabUtility.SaveAsPrefabAsset(resource,resourcePath);}finally{PrefabUtility.UnloadPrefabContents(resource);}
  foreach(var m in Enumerable.Range(1,4).Select(i=>L<LevelDefinition>("Mission0"+i)))if(!m.IsValid(out var error))throw new InvalidOperationException(error);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return new{configured=true};
 }
 public static object Report()
 {
  var report=Enumerable.Range(1,4).Select(i=>{var mission=L<LevelDefinition>("Mission0"+i);return new{mission=i,intro=mission.Intro,encounters=mission.Encounters.Select(e=>new{name=e.name,mode=e.Completion.ToString(),duration=e.Duration,entryLead=e.EntryLeadSeconds,title=e.TitleSeconds,beats=e.EntryBeats,groups=e.Groups.Select(g=>new{enemy=g.EnemyPrefab.name,count=g.Count,beat=g.EntryBeat,offset=g.StartTime,stagger=g.Stagger,phases=g.Sequence?.Phases.Select(p=>new{kind=p.Kind.ToString(),seconds=p.Duration}).ToArray()}),stream=e.HazardStream==null?null:new{count=HazardStreamSchedule.Build(e.HazardStream,e.Duration).Count,interval=e.HazardStream.Interval,direction=e.HazardStream.DominantDirection,variance=e.HazardStream.AngularVariance},optional=e.OptionalEvents.Select(o=>new{o.Id,o.TimeOffset,o.UsePressureWindow,o.MaximumActiveEnemies,o.FallbackTime,count=o.Group.Count}),clear=e.ClearPauseSeconds,travel=e.HandoffTravelSeconds,grace=e.ProjectileGraceSeconds,fade=e.ProjectileFadeSeconds,rewardGrace=e.RewardGraceSeconds})};}).ToArray();
  System.IO.File.WriteAllText("../.design/m5-refinement/config-snapshot.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore}));return new{written=true};
 }
 public static object ConfigureTravel()
 {
  if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
  var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
  var journey=runner.GetComponent<PlanetJourney>()??runner.gameObject.AddComponent<PlanetJourney>();
  Set(journey,"presentation",UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>());
  Set(journey,"player",UnityEngine.Object.FindAnyObjectByType<StarShift.Player.PlayerGameplayGate>());
  Set(journey,"view",Camera.main.GetComponent<GameplayViewController>());
  Set(runner.GetComponent<RegionCampaignFlow>(),"missionClearSeconds",2.3f);
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{configured=true};
 }
}
