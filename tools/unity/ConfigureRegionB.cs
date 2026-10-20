using System;
using System.Collections.Generic;
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

public static class ConfigureRegionB
{
    const string DataFolder = "Assets/StarShift/Data/", RegionBFolder = DataFolder + "RegionB/", EnemyPrefabFolder = "Assets/StarShift/Prefabs/Enemies/";
    static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    static T AuthorAsset<T>(string path, Action<T> configure) where T : ScriptableObject
    {
        var asset = Load<T>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
        configure(asset); EditorUtility.SetDirty(asset); return asset;
    }
    static void SetSerializedField(UnityEngine.Object o, string field, object value)
    { o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, value); EditorUtility.SetDirty(o); }
    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent); AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
    static GameObject Enemy(string name) => Load<GameObject>(EnemyPrefabFolder + name + ".prefab");
    public static object OrganizeAssetNames()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Asset naming cleanup requires Edit Mode.");
        var moves=new List<(string source,string destination,string guid)>();
        foreach(var guid in AssetDatabase.FindAssets("t:ScriptableObject",new[]{DataFolder+"RegionA",RegionBFolder.TrimEnd('/')}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var name=System.IO.Path.GetFileNameWithoutExtension(path);var renamed=name;
            if(path.Contains("/Cameras/")&&name.StartsWith("Feedback"))renamed=name.Substring(8)+"CombatView";
            else if(path.Contains("/Formations/"))
            {
                if(name.StartsWith("Feedback"))renamed=name.Substring(8)+"Formation";
                else if(name.StartsWith("M6M"))renamed=name.Substring(2)+"Formation";
            }
            else if(path.Contains("/Flights/"))
            {
                var required=System.Text.RegularExpressions.Regex.Match(name,@"^FeedbackRequired(M\d+[A-Za-z]+)(\d+)$");
                var shared=System.Text.RegularExpressions.Regex.Match(name,@"^(?:M6)?(Hold|Timed)(Wave)?(-1|0|1)$");
                if(required.Success)renamed=required.Groups[1].Value+"Group"+(int.Parse(required.Groups[2].Value)+1)+"RequiredHold";
                else if(shared.Success)renamed="Formation"+(shared.Groups[1].Value=="Hold"?"Hold":"Transit")+"From"+
                    (shared.Groups[3].Value=="-1"?"Left":shared.Groups[3].Value=="1"?"Right":"Center")+shared.Groups[2].Value;
            }
            if(renamed!=name)moves.Add((path,path.Substring(0,path.LastIndexOf('/')+1)+renamed+".asset",guid));
        }
        foreach(var move in moves)
            if(AssetDatabase.LoadMainAssetAtPath(move.destination)!=null)throw new InvalidOperationException("Naming collision: "+move.destination);
        foreach(var move in moves)
        {
            var error=AssetDatabase.MoveAsset(move.source,move.destination);if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            var asset=AssetDatabase.LoadMainAssetAtPath(move.destination);asset.name=System.IO.Path.GetFileNameWithoutExtension(move.destination);EditorUtility.SetDirty(asset);
            if(AssetDatabase.AssetPathToGUID(move.destination)!=move.guid)throw new InvalidOperationException("Asset GUID changed: "+move.destination);
        }
        AssetDatabase.SaveAssets();
        var assets=AssetDatabase.FindAssets("",new[]{"Assets/StarShift"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!AssetDatabase.IsValidFolder(p)).ToArray();
        var referenced=new HashSet<string>(assets.SelectMany(p=>AssetDatabase.GetDependencies(p,false).Where(d=>d!=p)));
        var unused=assets.Where(p=>p.StartsWith(RegionBFolder+"Cameras/")&&p.EndsWith(".asset")&&!referenced.Contains(p)).ToArray();
        foreach(var path in unused)if(!AssetDatabase.DeleteAsset(path))throw new InvalidOperationException("Could not remove unused camera: "+path);
        return new{renamed=moves.Count,removedUnusedCameras=unused.Length,guidsPreserved=true};
    }
    static T RegionAAsset<T>(string name) where T : UnityEngine.Object => StarShift.Editor.RegionAAssetPaths.Load<T>(DataFolder + "RegionA/" + name + ".asset");
    static SingleFireDefinition Fire(string name, int shots, float fan, float charge, float speed = 4)
    => AuthorAsset<SingleFireDefinition>(RegionBFolder + "Weapons/" + name + ".asset", f => {
        EditorUtility.CopySerialized(Load<SingleFireDefinition>(DataFolder + "Enemies/ScoutSingleFire.asset"), f); f.name=name;
        SetSerializedField(f,"projectileCount",shots); SetSerializedField(f,"spreadDegrees",fan); SetSerializedField(f,"telegraphDuration",charge); SetSerializedField(f,"cooldown",3.5f); SetSerializedField(f,"projectileSpeed",speed);
    });
    static AttackBeamPresentation Beam(GameObject parent)
    {
        var go = new GameObject("AimAndBeam"); go.transform.SetParent(parent.transform,false);
        var beam = go.AddComponent<AttackBeamPresentation>();
        var lines = new LineRenderer[2];
        for (int i=0;i<2;i++)
        {
            var node = new GameObject(i==0?"Telegraph":"BeamCore"); node.transform.SetParent(go.transform,false);
            var line = node.AddComponent<LineRenderer>(); line.sharedMaterial = Load<Material>(DataFolder+"RegionA/Boss/BeamMaterial.mat"); line.useWorldSpace = true; line.positionCount = 2; line.sortingOrder = 12+i; line.enabled = false;
            line.startColor = line.endColor = Color.white; lines[i] = line;
        }
        SetSerializedField(beam,"column",lines[0]); SetSerializedField(beam,"core",lines[1]); return beam;
    }
    static DirectedAttack Directed(GameObject go, string name, bool sweep=false, bool strike=false)
    {
        var config = AuthorAsset<DirectedAttackDefinition>(RegionBFolder+"Weapons/"+name+".asset", x => {
            x.Kind = strike ? DirectedAttackKind.Strike : DirectedAttackKind.Beam;
            x.ChargeSeconds = strike ? .85f : sweep ? 1.5f : 1.15f;
            x.TrackingSeconds = strike ? .2f : .3f; x.ActiveSeconds = strike ? .65f : sweep ? 2.4f : .45f;
            x.CooldownSeconds = strike ? 4.5f : 3.6f; x.BeamWidth = sweep ? .36f : .20f; x.Sweep = sweep;
        });
        var attack = go.AddComponent<DirectedAttack>(); SetSerializedField(attack,"definition",config); SetSerializedField(attack,"muzzle",go.transform); SetSerializedField(attack,"presentation",Beam(go)); return attack;
    }
    static GameObject NewEnemy(string name, int hp, int score, Vector2 size, Color tint, Action<GameObject> extra)
    {
        var path = EnemyPrefabFolder+name+".prefab"; var existing = Enemy(name); if(existing!=null)return existing;
        var definition = AuthorAsset<EnemyDefinition>(RegionBFolder+"Enemies/"+name+".asset",d=>{SetSerializedField(d,"maxHp",hp);SetSerializedField(d,"score",score);SetSerializedField(d,"missileTier",name=="Carrier"?MissileDamageTier.Large:MissileDamageTier.Normal);});
        var root = PrefabUtility.LoadPrefabContents(EnemyPrefabFolder+"Scout.prefab");
        try
        {
            root.name=name;SetSerializedField(root.GetComponent<EnemyHealth>(),"definition",definition);
            var hull=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="HullPlaceholder");
            hull.transform.localScale=new Vector3(size.x,size.y,1);hull.color=tint;
            var collider=root.GetComponentInChildren<BoxCollider2D>();collider.size=size*.82f;collider.offset=Vector2.zero;
            extra(root);return PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
    static FlightSequence Flight(string name, int side, bool terminal, float hold=18, bool wave=false, Vector2? anchor=null,bool regionA=false)
    => AuthorAsset<FlightSequence>((regionA?DataFolder+"RegionA/Flights/":RegionBFolder+"Flights/")+name+".asset", p=>{
        var end=anchor??new Vector2(.5f,.73f);
        var start=new Vector2(side<0?-.12f:side>0?1.12f:.5f,1.25f);
        var phases=new List<FlightPhase>{
            new FlightPhase{Kind=FlightPhaseKind.Entry,Path=side==0?FlightPath.Straight:side<0?FlightPath.ArcRight:FlightPath.ArcLeft,From=start,To=end,Curve=new Vector2(.10f*side,0),Duration=2.6f},
            new FlightPhase{Kind=FlightPhaseKind.Combat,Path=wave?FlightPath.Sine:FlightPath.Straight,From=end,To=end,Duration=hold,CanShoot=true,ClampToView=true,HoldUntilResolved=terminal,RepeatMotion=wave,Curve=new Vector2(.035f,.025f),Cycles=1}};
        if(!terminal)phases.Add(new FlightPhase{Kind=FlightPhaseKind.Exit,From=end,To=new Vector2(side<0?-.4f:1.4f,1.3f),Duration=2.8f,CanShoot=false});
        p.Phases=phases.ToArray();
    });
    struct Placement
    {
        public string Enemy;public Vector2 Point;public bool Elite;
        public Placement(string enemy,float x,float y,bool elite=false){Enemy=enemy;Point=new Vector2(x,y);Elite=elite;}
    }
    static Placement Place(string name,float x,float y,bool elite=false)=>new Placement(name,x,y,elite);
    static SpawnAction[] AuthorFormation(string name, IEnumerable<Placement> placements, bool terminal, bool wave=false, bool regionA=false)
    {
        var groups=new List<SpawnAction>();var ordered=placements.GroupBy(p=>(p.Enemy,p.Elite,side:p.Point.x<-.01f?-1:p.Point.x>.01f?1:0)).ToArray();
        var slots=new List<Vector2>();
        foreach(var group in ordered)slots.AddRange(group.Select(p=>p.Point));
        var path=regionA?DataFolder+"RegionA/Formations/"+name+".asset":RegionBFolder+"Formations/"+name+".asset";
        var plan=AuthorAsset<FormationPlan>(path,p=>p.Slots=slots.ToArray());
        int first=0;
        foreach(var group in ordered)
        {
            var flightName="Formation"+(terminal?"Hold":"Transit")+"From"+(group.Key.side<0?"Left":group.Key.side>0?"Right":"Center")+(wave?"Wave":"");
            var prefab=Enemy(group.Key.Enemy);var flight=Flight(flightName,group.Key.side,terminal,18,wave,regionA?new Vector2(.5f,.61f):(Vector2?)null,regionA);
            groups.Add(new SpawnAction{EnemyPrefab=prefab,EnemyDefinition=prefab.GetComponent<EnemyHealth>().Definition,Count=group.Count(),Plan=plan,FirstSlot=first,Stagger=.06f,Sequence=flight,
                Elite=group.Key.Elite?RegionAAsset<EnemyModifierDefinition>("Elite"):null,RewardOverride=group.Key.Elite?RegionAAsset<RewardDropTable>("EliteDrops"):null});
            first+=group.Count();
        }
        return groups.ToArray();
    }
    static CameraProfile Camera(string name,float size,Rect? region=null)=>AuthorAsset<CameraProfile>(RegionBFolder+"Cameras/"+name+".asset",c=>{c.View=new GameplayViewSettings(size,region??new Rect(0,0,1,1));c.TransitionSeconds=1.15f;});
    static EncounterDefinition Encounter(string name,int index,EncounterCompletion mode,float size,SpawnAction[] groups,int shooters=2,float duration=32)
    => AuthorAsset<EncounterDefinition>(RegionBFolder+"Encounters/"+name+".asset",e=>{
        e.Id="ENCOUNTER_"+index;e.Completion=mode;e.Duration=duration;e.Groups=groups;e.CameraOverride=Camera(name,size);
        e.CompleteWhenScheduleExhausted=mode==EncounterCompletion.Timeline;e.ActiveShooterLimit=shooters;e.ShotSpacing=shooters==1?.85f:.30f;e.ShotsPerVolley=shooters*2;e.VolleyPause=2.0f;
        e.ClearPauseSeconds=.7f;e.HandoffTravelSeconds=.85f;e.ProjectileGraceSeconds=.45f;e.ProjectileFadeSeconds=.25f;e.EntryLeadSeconds=.5f;e.RewardGraceSeconds=mode==EncounterCompletion.Boss?12:2.5f;
        e.GuaranteedMajorCount=mode==EncounterCompletion.Survive||mode==EncounterCompletion.Boss?0:1;e.GuaranteeTable=RegionAAsset<RewardDropTable>("ProgressionDrops");
    });
    static SpawnAction Supply(float time=0) => new SpawnAction{EnemyPrefab=Enemy("SupplyDrone"),Count=1,Required=false,StartTime=time,Stagger=0,Sequence=Flight("SupplyWindow",0,false,5,false,new Vector2(.5f,.56f)),RewardOverride=Load<RewardDropTable>(DataFolder+"Rewards/SupplyCrateDrops.asset")};
    static OptionalEvent SupplyEvent(float time)=>new OptionalEvent{Id="SUPPLY",TimeOffset=time,Kind=OptionalEventKind.SupplyDrone,Group=Supply()};
    static SpawnAction[] Corridor()
    {
        var result=new List<SpawnAction>();
        var openings=new[]{.23f,.50f,.77f,.50f,.23f};
        for(int gate=0;gate<openings.Length;gate++)
        for(int row=0;row<6;row++)
        {
            float localX=.08f+row*.168f;
            // Giữ khe hở đủ rộng; đá xuyên màn hình tự nhiên, không clamp ở biên.
            if(Mathf.Abs(1-localX-openings[gate])<.19f)continue;
            bool edge=row==0||row==5;
            result.Add(new SpawnAction{EnemyPrefab=Enemy("StormAsteroid"),Count=1,Required=false,ContinuousTransit=true,StartTime=gate*3.6f,Stagger=0,
                SpawnScale=edge?2.1f:1.5f,HpOverride=edge?6:4,VisualSpin=gate%2==0?18:-18,EntryAnchor=new Vector2(localX,1.15f),HoldAnchor=new Vector2(localX,1.15f),ExitAnchor=new Vector2(localX,-.20f),EntryDuration=1.2f,HoldDuration=0,ExitDuration=4.9f});
        }
        for(int gate=0;gate<openings.Length;gate++)
        for(int side=0;side<2;side++)
        {
            float lane=side==0?.08f:.92f;
            if(Mathf.Abs(1-lane-openings[gate])<.19f)continue;
            result.Add(new SpawnAction{EnemyPrefab=Enemy("StormAsteroid"),Count=1,Required=false,ContinuousTransit=true,StartTime=gate*3.6f+1.3f,Stagger=0,
                SpawnScale=side==0?.7f:1.05f,HpOverride=side==0?1:2,VisualSpin=35,EntryAnchor=new Vector2(lane,1.15f),HoldAnchor=new Vector2(lane,1.15f),ExitAnchor=new Vector2(lane,-.20f),EntryDuration=1.0f,HoldDuration=0,ExitDuration=3.8f});
        }
        result.Add(new SpawnAction{EnemyPrefab=Enemy("ResourceAsteroid"),Count=1,Required=false,ContinuousTransit=true,StartTime=7.6f,SpawnScale=.85f,EntryAnchor=new Vector2(.14f,1.16f),HoldAnchor=new Vector2(.14f,1.16f),ExitAnchor=new Vector2(.14f,-.2f),EntryDuration=1.2f,HoldDuration=0,ExitDuration=4.9f,RewardOverride=Load<RewardDropTable>(DataFolder+"Rewards/ResourceDrops.asset")});
        return result.ToArray();
    }
    static BossAttackModule Module(GameObject parent,string name,SingleFireDefinition fire,DirectedAttack directed=null,EscortSpawner escorts=null)
    {
        var go=directed!=null?directed.gameObject:new GameObject(name);
        if(directed==null)go.transform.SetParent(parent.transform,false);
        go.name=name;
        var cue=go.GetComponent<SpriteRenderer>();if(cue==null){cue=go.AddComponent<SpriteRenderer>();cue.sprite=Load<Sprite>("Assets/StarShift/Art/Placeholders/impact-glow-placeholder.png");cue.color=new Color(1,.35f,.15f);go.transform.localScale=Vector3.one*.35f;}cue.sortingOrder=8;
        var attack=go.AddComponent<BossAttackModule>();SetSerializedField(attack,"definition",fire);SetSerializedField(attack,"muzzles",new[]{go.transform});SetSerializedField(attack,"telegraph",cue);SetSerializedField(attack,"directed",directed);SetSerializedField(attack,"escorts",escorts);return attack;
    }
    static EscortSpawner Spawner(GameObject go,bool automatic)
    {
        var component=go.AddComponent<EscortSpawner>();SetSerializedField(component,"escortPrefab",Enemy("WardenDrone"));SetSerializedField(component,"escortFlight",Flight("DronePressure",0,false,6,false,new Vector2(.5f,.55f)));SetSerializedField(component,"capacity",3);SetSerializedField(component,"intervalSeconds",5.0f);SetSerializedField(component,"automatic",automatic);return component;
    }
    static GameObject Boss()
    {
        var existing=Enemy("NebulaWarden");if(existing!=null)return existing;
        var health=AuthorAsset<EnemyDefinition>(RegionBFolder+"Boss/WardenHealth.asset",h=>{SetSerializedField(h,"maxHp",360);SetSerializedField(h,"score",4000);SetSerializedField(h,"missileTier",MissileDamageTier.Boss);});
        var nodeDef=AuthorAsset<EnemyDefinition>(RegionBFolder+"Boss/ShieldNodeHealth.asset",h=>{SetSerializedField(h,"maxHp",90);SetSerializedField(h,"score",0);SetSerializedField(h,"missileTier",MissileDamageTier.Large);});
        var phases=AuthorAsset<BossPhaseDefinition>(RegionBFolder+"Boss/WardenPhases.asset",p=>{p.DisplayName="Nebula Warden";p.PhaseAOrder=new[]{0,2,1,2};p.PhaseBOrder=new[]{3,4,3,5};p.PhaseARestSeconds=1.25f;p.PhaseBRestSeconds=.9f;p.TransitionSeconds=1.2f;p.PhaseBOffset=Vector2.zero;p.PhaseBMotionMultiplier=1.1f;});
        var root=PrefabUtility.LoadPrefabContents(EnemyPrefabFolder+"VanguardFrigate.prefab");
        try
        {
            root.name="NebulaWarden";
            foreach(var component in root.GetComponentsInChildren<BossAttackModule>(true))UnityEngine.Object.DestroyImmediate(component.gameObject);
            foreach(var line in root.GetComponentsInChildren<LineRenderer>(true))UnityEngine.Object.DestroyImmediate(line.gameObject);
            foreach(var child in root.transform.Cast<Transform>().ToArray())
                if(child.name.StartsWith("CapitalWing") || child.name.StartsWith("Engine") || child.name.StartsWith("CannonPod") || new[]{"Reactor","BeamPod","MissilePod"}.Contains(child.name))UnityEngine.Object.DestroyImmediate(child.gameObject);
            var envelope=root.transform.Find("MovementEnvelope").GetComponent<BoxCollider2D>();envelope.size=new Vector2(6.4f,4.0f);
            var hp=root.GetComponent<EnemyHealth>();SetSerializedField(hp,"definition",health);
            var hull=root.GetComponentsInChildren<SpriteRenderer>(true).First(x=>x.name=="HullPlaceholder");hull.transform.localScale=new Vector3(5.4f,2.4f,1);hull.color=new Color(.45f,.18f,.55f);
            var box=root.transform.Find("Collision").GetComponent<BoxCollider2D>();box.size=new Vector2(4.9f,1.9f);box.offset=Vector2.zero;
            var shield=new GameObject("ShieldNetwork").AddComponent<SpriteRenderer>();shield.transform.SetParent(root.transform,false);shield.sprite=Load<Sprite>("Assets/StarShift/Art/Placeholders/impact-glow-placeholder.png");shield.transform.localScale=new Vector3(6,3.0f,1);shield.sortingOrder=7;
            var nodes=new EnemyHealth[3];var modules=new BossAttackModule[6];var plasma=Fire("WardenPlasma",5,48,.9f,4.8f);var droneCue=Fire("WardenDroneCharge",1,0,.9f);
            for(int i=0;i<3;i++)
            {
                var go=new GameObject(new[]{"BeamNode","DroneNode","PlasmaNode"}[i]);go.layer=box.gameObject.layer;go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3((i-1)*2.0f,-1.6f,0);
                var visual=go.AddComponent<SpriteRenderer>();visual.sprite=hull.sprite;visual.color=new[]{new Color(1,.3f,.5f),new Color(.2f,1,.8f),new Color(1,.65f,.15f)}[i];visual.sortingOrder=9;go.transform.localScale=new Vector3(.95f,.9f,1);
                go.AddComponent<BoxCollider2D>().size=new Vector2(1.15f,1f);go.GetComponent<BoxCollider2D>().isTrigger=true;
                nodes[i]=go.AddComponent<EnemyHealth>();SetSerializedField(nodes[i],"definition",nodeDef);SetSerializedField(go.AddComponent<EnemyDamagePresentation>(),"hull",visual);
                if(i==0)modules[i]=Module(go,"BeamNodeAttack",droneCue,Directed(go,"WardenSweep",true));
                else if(i==1)modules[i]=Module(go,"DroneNodeAttack",droneCue,escorts:Spawner(go,false));
                else modules[i]=Module(go,"PlasmaNodeAttack",plasma);
            }
            modules[3]=Module(root,"ExposedPlasma",plasma);modules[3].transform.localPosition=new Vector3(0,-.95f,0);
            var coreBeam=new GameObject("ExposedBeam");coreBeam.transform.SetParent(root.transform,false);coreBeam.transform.localPosition=new Vector3(0,-.95f,0);
            modules[4]=Module(root,"ExposedSweep",droneCue,Directed(coreBeam,"WardenSweep",true));
            modules[5]=Module(root,"ExposedDrones",droneCue,escorts:Spawner(root,false));
            SetSerializedField(root.GetComponent<BossPhaseRunner>(),"definition",phases);SetSerializedField(root.GetComponent<BossPhaseRunner>(),"modules",modules);
            var network=root.AddComponent<BossShieldNetwork>();SetSerializedField(network,"nodes",nodes);SetSerializedField(network,"shield",shield);
            var breaks=new SpriteRenderer[3];for(int i=0;i<3;i++){var cue=new GameObject("ShieldBreak"+i).AddComponent<SpriteRenderer>();cue.transform.SetParent(root.transform,false);cue.transform.localPosition=nodes[i].transform.localPosition;cue.sprite=shield.sprite;cue.color=new Color(1,.7f,.2f);cue.sortingOrder=15;cue.enabled=false;breaks[i]=cue;}SetSerializedField(network,"breaks",breaks);
            var beats=root.GetComponentsInChildren<SpriteRenderer>(true).Where(x=>x.name.StartsWith("DeathBeat")).OrderBy(x=>x.name).ToArray();
            for(int i=0;i<beats.Length;i++){beats[i].transform.localPosition=new Vector3(i==4?0:(i%2==0?-1:1)*2,.4f-i*.18f,0);beats[i].transform.localScale=Vector3.one*(i==4?3:1.4f);}
            return PrefabUtility.SaveAsPrefabAsset(root,EnemyPrefabFolder+"NebulaWarden.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static object Configure()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode required.");
        foreach(var dir in new[]{"Missions","Encounters","Formations","Flights","Cameras","Enemies","Weapons","Boss","Rewards"})Folder(RegionBFolder+dir);
        NewEnemy("WardenDrone",2,0,new Vector2(.55f,.65f),new Color(.45f,.18f,.28f),g=>SetSerializedField(g.GetComponent<SingleFireAttack>(),"definition",Fire("DroneSingle",1,0,.65f,4)));
        NewEnemy("Sniper",7,250,new Vector2(.95f,1.1f),new Color(1,.25f,.55f),g=>Directed(g,"SniperBeam"));
        NewEnemy("Interceptor",6,250,new Vector2(.85f,1.2f),new Color(.95f,.85f,.2f),g=>Directed(g,"InterceptorStrike",strike:true));
        NewEnemy("Carrier",34,500,new Vector2(2.0f,1.65f),new Color(.45f,.25f,.85f),g=>{SetSerializedField(g.GetComponent<SingleFireAttack>(),"definition",Fire("CarrierSingle",1,0,.8f,3.6f));Spawner(g,true);});
        var boss=Boss();
        var arrow=new List<Placement>{Place("Raider",0,-.17f)};
        for(int i=1;i<=4;i++){arrow.Add(Place(i%2==0?"Raider":"Scout",-i*.08f,-.17f+i*.08f));arrow.Add(Place(i%2==0?"Raider":"Scout",i*.08f,-.17f+i*.08f));}
        for(int i=-2;i<=2;i++)arrow.Add(Place("Scout",i*.11f,.15f));
        var m5a=Encounter("M5Arrowhead",1,EncounterCompletion.Eliminate,6.6f,AuthorFormation("Arrowhead",arrow,true));
        var ribbons=new List<Placement>();for(int i=0;i<8;i++){float x=-.3675f+i*.105f;ribbons.Add(Place(i%2==0?"Raider":"Scout",x,.10f+Mathf.Sin(i*Mathf.PI/3)*.035f));ribbons.Add(Place(i%2==0?"Scout":"Raider",x,-.12f+Mathf.Sin(i*Mathf.PI/3)*.035f));}
        var m5b=Encounter("M5Ribbons",2,EncounterCompletion.Timeline,6.5f,AuthorFormation("MirroredRibbons",ribbons,false,true),3);
        var escort=new List<Placement>{Place("Bomber",-.16f,.05f),Place("Bomber",.16f,.05f)};
        for(int i=-3;i<=3;i++){escort.Add(Place(i%2==0?"Raider":"Scout",i*.105f,-.14f));if(i!=0)escort.Add(Place("Scout",i*.105f,.20f));}
        var m5c=Encounter("M5BomberLanes",3,EncounterCompletion.Eliminate,6.6f,AuthorFormation("HeavyLanes",escort,true),3);
        var m6a=Encounter("M6Sniper",1,EncounterCompletion.Eliminate,5.25f,AuthorFormation("M6SniperFormation",new[]{Place("Sniper",0,.08f),Place("Scout",-.29f,-.08f),Place("Scout",.29f,-.08f),Place("Raider",-.11f,-.12f),Place("Raider",.11f,-.12f)},true),1,28);
        var m6b=Encounter("M6Corridor",2,EncounterCompletion.Survive,5.5f,Corridor(),1,19);m6b.OptionalEvents=new[]{SupplyEvent(9)};
        var m6c=Encounter("M6LaneLock",3,EncounterCompletion.Eliminate,5.5f,AuthorFormation("M6LaneLockFormation",new[]{Place("Sniper",-.24f,.1f),Place("Bomber",.16f,.06f),Place("Scout",-.3f,-.12f),Place("Raider",-.12f,-.16f),Place("Scout",.08f,-.16f),Place("Raider",.30f,-.12f)},true),1);
        var scissors=new List<Placement>();for(int i=-4;i<=4;i++){float x=i*.077f;scissors.Add(Place(i%2==0?"Raider":"Scout",x,x*.55f));if(i!=0)scissors.Add(Place(i%2==0?"Scout":"Raider",x,-x*.55f));}
        var m7a=Encounter("M7Crossfire",1,EncounterCompletion.Timeline,7.2f,AuthorFormation("Scissors",scissors,false,true),4,34);
        var wings=new List<Placement>{Place("Carrier",0,.10f)};for(int side=-1;side<=1;side+=2)for(int i=1;i<=4;i++){wings.Add(Place(i==2?"Interceptor":"Raider",side*i*.095f,.10f-i*.055f));if(i>1)wings.Add(Place("Scout",side*i*.095f,.20f));}
        var m7b=Encounter("M7Carrier",2,EncounterCompletion.Eliminate,7.0f,AuthorFormation("M7CarrierFormation",wings,true),3,40);m7b.OptionalEvents=new[]{SupplyEvent(10)};
        var towers=new List<Placement>{Place("Sniper",-.25f,.08f),Place("Sniper",.25f,.08f)};for(int i=-1;i<=1;i++){towers.Add(Place("Raider",-.25f+i*.11f,-.12f));towers.Add(Place("Scout",.25f+i*.11f,-.12f));}
        var m7c=Encounter("M7DualSniper",3,EncounterCompletion.Timeline,6.5f,AuthorFormation("M7DualSniperFormation",towers,false),1,30);m7c.ShotSpacing=.9f;m7c.VolleyPause=1.9f;
        var fortress=new List<Placement>{Place("Carrier",0,.14f),Place("Bomber",-.22f,.08f),Place("Bomber",.22f,.08f)};
        for(int i=-3;i<=3;i++)fortress.Add(Place(i%2==0?"Interceptor":"Scout",i*.105f,-.14f));
        var m8a=Encounter("M8Fortress",1,EncounterCompletion.Eliminate,7.1f,AuthorFormation("M8FortressFormation",fortress,true),3,29);m8a.OptionalEvents=new[]{SupplyEvent(16)};
        var bossFlight=Flight("WardenEntry",0,true,12,false,new Vector2(.5f,.82f));
        var m8b=Encounter("M8Warden",2,EncounterCompletion.Boss,7.4f,new[]{new SpawnAction{EnemyPrefab=boss,EnemyDefinition=boss.GetComponent<EnemyHealth>().Definition,Count=1,Stagger=0,Sequence=bossFlight,RewardOverride=Load<RewardDropTable>(DataFolder+"Rewards/SupplyCrateDrops.asset")}},1,180);
        m8b.CameraOverride=Camera("WardenBattlefield",7.4f,new Rect(.02f,.04f,.57f,.92f));m8b.BossPostDeathSeconds=3;
        var content=new[]{new[]{m5a,m5b,m5c},new[]{m6a,m6b,m6c},new[]{m7a,m7b,m7c},new[]{m8a,m8b}};
        var titles=new[]{"Tấn công từ bên phải. Giữ khoảng trống để di chuyển.","Đọc đường ngắm. Tìm khe hở giữa các cổng mảnh vỡ.","Ưu tiên tàu mẹ và tránh đường lao đã khóa.","Phá tuyến phòng thủ. Hạ các nút khiên trước khi đánh lõi."};
        var missions=new List<LevelDefinition>();for(int i=1;i<=4;i++)missions.Add(RegionAAsset<LevelDefinition>("Mission0"+i));
        for(int i=0;i<4;i++)
        {
            int n=i+5;var level=AuthorAsset<LevelDefinition>(RegionBFolder+"Missions/Mission0"+n+".asset",l=>{
                l.Id="L"+n;l.MissionNumber=n;l.DisplayName="NHIỆM VỤ "+n.ToString("00");l.RegionId="crimson-nebula";l.Direction=PlayDirection.Right;l.Camera=content[n-5][0].CameraOverride;l.Encounters=content[n-5];l.EnemyHealthMultiplier=1.25f+(n-5)*.05f;l.MissionBriefing=titles[n-5];
                l.Intro=new MissionIntroSettings{ArrivalTitle=l.DisplayName,Enabled=false};l.ReplayPreset=new ReplayLoadoutPreset{WeaponLevels=new[]{3+(n>=7?1:0),2,2}};
                l.AdditionalStarts=n==6?new[]{new LevelStartPoint{Id="SNIPER",Encounter=l.Encounters[0]},new LevelStartPoint{Id="CORRIDOR",Encounter=l.Encounters[1]}}:n==7?new[]{new LevelStartPoint{Id="CARRIER",Encounter=l.Encounters[1]},new LevelStartPoint{Id="DUAL_SNIPER",Encounter=l.Encounters[2]}}:n==8?new[]{new LevelStartPoint{Id="PRE_BOSS",Encounter=l.Encounters[0]},new LevelStartPoint{Id="BOSS",Encounter=l.Encounters[1]},new LevelStartPoint{Id="BOSS_PHASE_B",Encounter=l.Encounters[1]}}:Array.Empty<LevelStartPoint>();
            });missions.Add(level);
        }
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();SetSerializedField(runner.GetComponent<RegionCampaignFlow>(),"missions",missions.ToArray());
        SetSerializedField(runner.GetComponent<StarShift.Dev.DevFastStart>(),"devModeEnabled",false);
        RewardFloors();
        foreach(var level in missions)if(!level.IsValid(out var error))throw new InvalidOperationException(level.name+": "+error);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        RefreshPresentation();CombatFeedback();OrganizeAssetNames();return new{missions=missions.Count,regionBEncounters=content.Sum(a=>a.Length),bossHp=520,nodeHp=110};
    }
    public static object RefreshHoming()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
        var weapon=Load<StarShift.Combat.PulseLaserDefinition>(DataFolder+"Weapons/HomingMissile.asset");
        SetSerializedField(weapon,"trackingLaunchSeconds",.16f);SetSerializedField(weapon,"trackingLaunchArcDegrees",9f);
        for(int i=0;i<5;i++){weapon.Pattern(i+1).SpreadDegrees=i==0?0:2+4*i;weapon.Pattern(i+1).LateralSpacing=.09f;}
        AssetDatabase.SaveAssets();
        return new{launchSeconds=weapon.TrackingLaunchSeconds,arcDegrees=weapon.TrackingLaunchArcDegrees};
    }
    public static object RewardFloors()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
        var guaranteed=new[]{"M5Arrowhead","M5BomberLanes","M6Sniper","M7Carrier","M7DualSniper"};
        foreach(var guid in AssetDatabase.FindAssets("t:EncounterDefinition",new[]{RegionBFolder+"Encounters"}))
        {
            var encounter=Load<EncounterDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            encounter.GuaranteedMajorCount=guaranteed.Contains(encounter.name)?1:0;EditorUtility.SetDirty(encounter);
        }
        AssetDatabase.SaveAssets();return new{regionBFloor=guaranteed.Length};
    }
    public static object RefreshPresentation()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
        var path=EnemyPrefabFolder+"NebulaWarden.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var health=root.GetComponent<EnemyHealth>();var nodes=root.GetComponentsInChildren<EnemyHealth>(true).Where(n=>n!=health).ToArray();
            foreach(var node in nodes)node.GetComponent<BoxCollider2D>().size=new Vector2(1.15f,1f);
            var shield=root.transform.Find("ShieldNetwork").GetComponent<SpriteRenderer>();var cues=new SpriteRenderer[nodes.Length];
            for(int i=0;i<nodes.Length;i++)
            {
                var child=root.transform.Find("ShieldBreak"+i);
                if(child==null){child=new GameObject("ShieldBreak"+i).transform;child.SetParent(root.transform,false);child.gameObject.AddComponent<SpriteRenderer>();}
                var cue=child.GetComponent<SpriteRenderer>();cue.transform.localPosition=nodes[i].transform.localPosition;cue.sprite=shield.sprite;cue.color=new Color(1,.7f,.2f);cue.sortingOrder=15;cue.enabled=false;cues[i]=cue;
            }
            SetSerializedField(root.GetComponent<BossShieldNetwork>(),"breaks",cues);PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        path=EnemyPrefabFolder+"Raider.prefab";root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hull=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="HullPlaceholder");hull.transform.localScale=new Vector3(.8f,1.05f,1);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        foreach(var guid in AssetDatabase.FindAssets("t:ScriptableObject",new[]{RegionBFolder+"Weapons"}))
        {var assetPath=AssetDatabase.GUIDToAssetPath(guid);var item=Load<ScriptableObject>(assetPath);item.name=System.IO.Path.GetFileNameWithoutExtension(assetPath);EditorUtility.SetDirty(item);}
        foreach(var enemy in new[]{"Interceptor","Sniper","Carrier","WardenDrone"})
        {
            var enemyPath=EnemyPrefabFolder+enemy+".prefab";var model=PrefabUtility.LoadPrefabContents(enemyPath);
            try
            {
                var hull=model.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="HullPlaceholder");
                SetSerializedField(model.GetComponent<EnemyHealth>().Definition,"score",enemy=="Carrier"?500:enemy=="WardenDrone"?0:250);
                hull.color=enemy=="Sniper"?new Color(.65f,.18f,.40f):enemy=="Carrier"?new Color(.35f,.25f,.42f):new Color(.85f,.18f,.32f);
                if(enemy=="Interceptor") {hull.sprite=Load<Sprite>("Assets/StarShift/Art/Placeholders/player-placeholder.png");hull.transform.localRotation=Quaternion.Euler(0,0,180);}
                if(enemy=="WardenDrone"){PrefabUtility.SaveAsPrefabAsset(model,enemyPath);continue;}
                var name=enemy=="Sniper"?"LongBarrel":enemy=="Carrier"?"HangarRail":"EnginePlume";
                var child=model.transform.Find(name);if(child==null){child=new GameObject(name).transform;child.SetParent(model.transform,false);child.gameObject.AddComponent<SpriteRenderer>();}
                var renderer=child.GetComponent<SpriteRenderer>();renderer.sprite=Load<Sprite>("Assets/StarShift/Art/Placeholders/"+(enemy=="Interceptor"?"impact-glow-placeholder.png":"pulse-bolt-placeholder.png"));renderer.sortingOrder=5;renderer.color=enemy=="Sniper"?new Color(1,.3f,.55f):new Color(1,.15f,.4f);
                child.localPosition=new Vector3(0,enemy=="Sniper"?-.6f:enemy=="Carrier"?-.2f:.55f,0);child.localScale=enemy=="Carrier"?new Vector3(1.4f,.25f,1):new Vector3(.18f,.8f,1);
                PrefabUtility.SaveAsPrefabAsset(model,enemyPath);
            }finally{PrefabUtility.UnloadPrefabContents(model);}
        }
        var warden=PrefabUtility.LoadPrefabContents(EnemyPrefabFolder+"NebulaWarden.prefab");
        try
        {
            foreach(var beam in warden.GetComponentsInChildren<AttackBeamPresentation>(true))
            {
                var child=beam.transform.Find("SweepDirection");if(child==null){child=new GameObject("SweepDirection").transform;child.SetParent(beam.transform,false);child.gameObject.AddComponent<LineRenderer>();}
                var line=child.GetComponent<LineRenderer>();line.sharedMaterial=Load<Material>(DataFolder+"RegionA/Boss/BeamMaterial.mat");line.useWorldSpace=true;line.positionCount=5;line.startWidth=line.endWidth=.045f;line.startColor=line.endColor=new Color(1,.12f,.45f,.65f);line.sortingOrder=11;line.enabled=false;SetSerializedField(beam,"sweepGuide",line);
            }
            PrefabUtility.SaveAsPrefabAsset(warden,EnemyPrefabFolder+"NebulaWarden.prefab");
        }finally{PrefabUtility.UnloadPrefabContents(warden);}
        var player=GameObject.Find("Player");
        if(player!=null)
        {
            var hull=player.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="HullPlaceholder");
            // Bỏ tint đỏ legacy để tàu người chơi phân biệt với hostile placeholder.
            PrefabUtility.RevertPropertyOverride(new SerializedObject(hull).FindProperty("m_Color"),InteractionMode.AutomatedAction);
        }
        var decor=GameObject.Find("EnvironmentPlaceholders");if(decor!=null)foreach(var renderer in decor.GetComponentsInChildren<SpriteRenderer>())renderer.color=new Color(.08f,.1f,.12f,.35f);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();return new{shieldBreakFeedback=true,raiderReadableScale=true};
    }
    public static object RegionAFormations()
    {
        var names=new[]{"M1Formation","M2RaiderArcs","M2Bomber","M3Sine","M3PincerSupply","M3Elite"};
        var diamonds=new List<Placement>();foreach(float center in new[]{-.20f,.20f})for(int row=-2;row<=2;row++)for(int col=-(2-Math.Abs(row));col<=2-Math.Abs(row);col+=2)
            diamonds.Add(Place("Scout",center+col*.065f,row*.09f));
        var spear=new List<Placement>{Place("Bomber",0,.17f)};for(int row=0;row<4;row++)for(int col=-row;col<=row;col++)spear.Add(Place((row+col)%2==0?"Raider":"Scout",col*.085f,.08f-row*.095f));
        var guard=new List<Placement>{Place("Bomber",-.10f,.08f),Place("Bomber",.10f,.08f)};for(int i=-3;i<=3;i++){guard.Add(Place("Scout",i*.09f,-.17f));if(Math.Abs(i)>1)guard.Add(Place("Raider",i*.09f,.10f));}
        var waves=new List<Placement>();for(int side=-1;side<=1;side+=2)for(int i=0;i<8;i++)waves.Add(Place(i%2==0?"Raider":"Scout",side*(.14f+.07f*Mathf.Sin(i*.75f)),.28f-i*.08f));
        var cross=new List<Placement>{Place("Bomber",0,0)};for(int i=1;i<=4;i++)for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)cross.Add(Place(i%2==0?"Scout":"Raider",x*i*.073f,y*i*.068f));
        var crown=new List<Placement>{Place("Bomber",0,-.02f,true)};for(int i=-3;i<=3;i++){if(i!=0)crown.Add(Place(i%2==0?"Scout":"Raider",i*.09f,-.17f));crown.Add(Place("Raider",i*.09f,.10f+(Math.Abs(i)%2==1?.12f:0)));}
        var shapes=new[]{diamonds,spear,guard,waves,cross,crown};var results=new List<object>();
        for(int i=0;i<names.Length;i++)
        {
            var e=RegionAAsset<EncounterDefinition>(names[i]);bool terminal=e.Completion==EncounterCompletion.Eliminate;
            e.Groups=AuthorFormation(names[i]+"Formation",shapes[i],terminal,i==3,true);e.EntryBeats=Array.Empty<EntryBeatRule>();EditorUtility.SetDirty(e);results.Add(new{e.name,count=e.EnemyCount});
            if(!e.IsValid(out var error))throw new InvalidOperationException(error);
        }
        // Giữ floor đã author; giảm odds theo population để không tự tăng progression.
        var normal=Load<RewardDropTable>(DataFolder+"Rewards/NormalDrops.asset");normal.MajorChance=.018f;normal.ShardChance=.18f;EditorUtility.SetDirty(normal);
        AssetDatabase.SaveAssets();return results;
    }
    public static object CombatFeedback()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
        var tank=NewEnemy("Bulwark",28,180,new Vector2(1.65f,1.45f),new Color(.45f,.32f,.34f),g=>{
            g.GetComponent<SingleFireAttack>().enabled=false;
            SetSerializedField(g.GetComponent<EnemyHealth>().Definition,"missileTier",MissileDamageTier.Large);
        });
        var forms=new Dictionary<EncounterDefinition,Placement[]>();
        forms[RegionAAsset<EncounterDefinition>("M2RaiderArcs")]=new[]{Place("Bulwark",-.16f,-.17f),Place("Bulwark",.16f,-.17f),Place("Bomber",0,.19f),Place("Interceptor",-.16f,.04f),Place("Interceptor",.16f,.04f)}
            .Concat(Enumerable.Range(-3,7).Select(n=>Place("Scout",n*.10f,.28f)))
            .Concat(new[]{Place("Raider",-.34f,-.05f),Place("Raider",.34f,-.05f),Place("Raider",-.34f,.16f),Place("Raider",.34f,.16f),Place("Scout",0,-.18f)}).ToArray();
        forms[RegionAAsset<EncounterDefinition>("M2Bomber")]=new[]{Place("Bulwark",-.14f,-.15f),Place("Bulwark",.14f,-.15f),Place("Bomber",-.14f,.11f),Place("Bomber",.14f,.11f)}
            .Concat(Enumerable.Range(-3,7).Select(n=>Place("Scout",n*.10f,.30f)))
            .Concat(new[]{Place("Raider",-.34f,-.07f),Place("Raider",.34f,-.07f)}).ToArray();
        forms[RegionAAsset<EncounterDefinition>("M3Elite")]=new[]{Place("Bulwark",-.22f,-.17f),Place("Bulwark",.22f,-.17f),Place("Bomber",0,-.17f,true),Place("Sniper",-.22f,.25f),Place("Sniper",.22f,.25f),Place("Raider",0,.25f),Place("Interceptor",-.22f,.04f),Place("Interceptor",.22f,.04f)}
            .Concat(new[]{-.38f,.38f}.SelectMany(x=>new[]{Place("Scout",x,-.06f),Place("Scout",x,.16f)}))
            .Concat(new[]{Place("Scout",-.10f,.29f),Place("Scout",.10f,.29f)}).ToArray();
        forms[Load<EncounterDefinition>(RegionBFolder+"Encounters/M6Sniper.asset")]=new[]{Place("Sniper",0,.19f),Place("Bulwark",0,-.20f),Place("Bulwark",-.19f,-.06f),Place("Bulwark",.19f,-.06f),Place("Scout",-.36f,.15f),Place("Scout",.36f,.15f)};
        forms[Load<EncounterDefinition>(RegionBFolder+"Encounters/M6LaneLock.asset")]=new[]{Place("Sniper",-.23f,.13f),Place("Bomber",.17f,.13f),Place("Bulwark",-.23f,-.15f),Place("Bulwark",.17f,-.15f),Place("Scout",-.40f,.03f),Place("Raider",.36f,-.10f)};
        forms[Load<EncounterDefinition>(RegionBFolder+"Encounters/M7DualSniper.asset")]=new[]{Place("Sniper",-.23f,.13f),Place("Sniper",.23f,.13f),Place("Bulwark",-.23f,-.15f),Place("Bulwark",.23f,-.15f),Place("Raider",-.36f,.04f),Place("Raider",.36f,.04f),Place("Scout",-.10f,-.06f),Place("Scout",.10f,-.06f)};
        forms[Load<EncounterDefinition>(RegionBFolder+"Encounters/M7Carrier.asset")]=new[]{Place("Carrier",0,.18f),Place("Bulwark",-.20f,-.18f),Place("Bulwark",.20f,-.18f),Place("Interceptor",-.20f,.04f),Place("Interceptor",.20f,.04f),Place("Raider",0,-.18f)}
            .Concat(new[]{-.38f,.38f}.SelectMany(x=>new[]{Place("Raider",x,-.03f),Place("Scout",x,.14f)})).ToArray();
        forms[Load<EncounterDefinition>(RegionBFolder+"Encounters/M8Fortress.asset")]=new[]{Place("Carrier",0,.18f),Place("Bomber",-.35f,.13f),Place("Bomber",.35f,.13f),Place("Bulwark",-.23f,-.17f),Place("Bulwark",0,-.17f),Place("Bulwark",.23f,-.17f),Place("Interceptor",-.23f,.05f),Place("Interceptor",0,.05f),Place("Interceptor",.23f,.05f),Place("Scout",-.40f,-.02f),Place("Scout",.40f,-.02f)};
        foreach(var pair in forms)
        {
            var e=pair.Key;e.Groups=AuthorFormation(e.name+"Formation",pair.Value,e.Completion==EncounterCompletion.Eliminate,false,AssetDatabase.GetAssetPath(e).Contains("RegionA"));
            EditorUtility.SetDirty(e);
        }
        foreach(var e in new[]{RegionAAsset<EncounterDefinition>("M2RaiderArcs"),RegionAAsset<EncounterDefinition>("M3Sine"),Load<EncounterDefinition>(RegionBFolder+"Encounters/M6Sniper.asset"),Load<EncounterDefinition>(RegionBFolder+"Encounters/M8Fortress.asset")})
        {
            e.Completion=EncounterCompletion.Eliminate;e.CompleteWhenScheduleExhausted=false;
            for(int index=0;index<e.Groups.Length;index++)
            {
                var group=e.Groups[index];var source=group.Sequence;if(source==null||!group.Required)continue;
                var flightName=e.name+"Group"+(index+1)+"RequiredHold";
                var path=(AssetDatabase.GetAssetPath(e).Contains("RegionA")?DataFolder+"RegionA/":RegionBFolder)+"Flights/"+flightName+".asset";
                group.Sequence=AuthorAsset<FlightSequence>(path,f=>{
                    if(source!=f)EditorUtility.CopySerialized(source,f);f.name=flightName;
                    f.Phases=f.Phases.TakeWhile(p=>p.Kind!=FlightPhaseKind.Exit).ToArray();f.Phases[f.Phases.Length-1].HoldUntilResolved=true;
                });
            }
            EditorUtility.SetDirty(e);
        }
        var preboss=Load<EncounterDefinition>(RegionBFolder+"Encounters/M8Fortress.asset");
        foreach(var supply in preboss.OptionalEvents){supply.TimeOffset=3;supply.UsePressureWindow=true;supply.MaximumActiveEnemies=3;supply.FallbackTime=12;}
        SetSerializedField(RegionAAsset<EnemyDefinition>("Bomber"),"maxHp",16);
        var missions=UnityEngine.Object.FindAnyObjectByType<RegionCampaignFlow>().Missions;
        var flights=new HashSet<FlightSequence>();
        foreach(var level in missions)
        {
            level.RegionEnemyHealthMultiplier=level.MissionNumber>=5?1.30f:1;EditorUtility.SetDirty(level);
            for(int index=0;index<level.Encounters.Length;index++)
            {
                var e=level.Encounters[index];bool boss=e.Completion==EncounterCompletion.Boss;
                e.ActiveShooterLimit=boss?4:e.name=="M3Elite"?2:Mathf.Clamp(e.ActiveShooterLimit,1,4);
                e.ShotSpacing=level.MissionNumber<=2?.65f:.50f;e.ShotsPerVolley=boss?4:Mathf.Min(4,e.ActiveShooterLimit+1);e.VolleyPause=boss?3.2f:3.0f;
                if(level.MissionNumber!=1||index>0)
                {
                    var source=e.CameraOverride!=null?e.CameraOverride:level.Camera;
                    var path=(level.MissionNumber>=5?RegionBFolder:DataFolder+"RegionA/")+"Cameras/"+e.name+"CombatView.asset";
                    e.CameraOverride=AuthorAsset<CameraProfile>(path,c=>{
                        if(source!=c)EditorUtility.CopySerialized(source,c);c.name=e.name+"CombatView";
                        c.View=new GameplayViewSettings(boss?(level.MissionNumber==8?9.2f:9.6f):source.View.OrthographicSize,
                            level.Direction==PlayDirection.Up?new Rect(0,0,1,.55f):new Rect(0,0,.59f,1));
                    });
                }
                foreach(var group in e.Groups)if(group.Sequence!=null&&!boss&&group.EnemyPrefab!=null&&group.EnemyPrefab.GetComponent<AsteroidVisualMotion>()==null&&group.EnemyPrefab.GetComponent<EnemyHealth>().Definition.BodyDamagesPlayer)flights.Add(group.Sequence);
                EditorUtility.SetDirty(e);
            }
        }
        foreach(var flight in flights)
        {
            foreach(var phase in flight.Phases)if(phase.Kind==FlightPhaseKind.Combat||phase.Kind==FlightPhaseKind.Hold)
            {phase.Path=FlightPath.Sine;phase.Curve=new Vector2(.025f,.008f);phase.RepeatMotion=true;phase.MotionPeriod=7;}
            EditorUtility.SetDirty(flight);
        }
        SetSerializedField(RegionAAsset<EnemyDefinition>("Boss/VanguardHealth"),"maxHp",680);
        SetSerializedField(Load<EnemyDefinition>(RegionBFolder+"Boss/WardenHealth.asset"),"maxHp",520);SetSerializedField(Load<EnemyDefinition>(RegionBFolder+"Boss/ShieldNodeHealth.asset"),"maxHp",110);
        var gold=AuthorAsset<PickupDefinition>(RegionBFolder+"Rewards/WardenGold.asset",g=>{EditorUtility.CopySerialized(RegionAAsset<PickupDefinition>("Boss/BossGold"),g);g.name="WardenGold";g.VisualScale=.75f;g.Lifetime=14;});
        var drops=AuthorAsset<RewardDropTable>(RegionBFolder+"Rewards/WardenDefeatDrops.asset",d=>{EditorUtility.CopySerialized(RegionAAsset<RewardDropTable>("Boss/VanguardDefeatDrops"),d);d.name="WardenDefeatDrops";d.Shard=gold;d.BossShardCount=36;});
        var warden=Load<EncounterDefinition>(RegionBFolder+"Encounters/M8Warden.asset");warden.Groups[0].RewardOverride=drops;warden.RewardGraceSeconds=14;EditorUtility.SetDirty(warden);
        foreach(var name in new[]{"VanguardFrigate","NebulaWarden"})
        {
            var path=EnemyPrefabFolder+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(name=="VanguardFrigate")
                {
                    var sp=root.GetComponent<EscortSpawner>()??root.AddComponent<EscortSpawner>();
                    SetSerializedField(sp,"escortPrefab",Enemy("WardenDrone"));SetSerializedField(sp,"escortFlight",Load<FlightSequence>(RegionBFolder+"Flights/DronePressure.asset"));SetSerializedField(sp,"capacity",3);SetSerializedField(sp,"intervalSeconds",4.5f);SetSerializedField(sp,"automatic",true);
                }
                else foreach(var module in root.GetComponentsInChildren<BossAttackModule>(true))SetSerializedField(module,"escortVolleyCount",2);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var drone=Load<FlightSequence>(RegionBFolder+"Flights/DronePressure.asset");
        drone.Phases[0].Duration=1.1f;drone.Phases[1].Duration=9;drone.Phases[1].Path=FlightPath.Sine;drone.Phases[1].Curve=new Vector2(.035f,.01f);drone.Phases[1].RepeatMotion=true;drone.Phases[1].MotionPeriod=5;EditorUtility.SetDirty(drone);
        RefreshHoming();ThreatClosure();
        var camera=UnityEngine.Camera.main; var hint=camera.GetComponent<PlayableAreaPresentation>()??camera.gameObject.AddComponent<PlayableAreaPresentation>();SetSerializedField(hint,"movement",UnityEngine.Object.FindAnyObjectByType<StarShift.Player.RelativeDragMovement>());
        SetSerializedField(UnityEngine.Object.FindAnyObjectByType<StarShift.Dev.DevFastStart>(),"devModeEnabled",false);
        foreach(var level in missions)if(!level.IsValid(out var error))throw new InvalidOperationException(level.name+": "+error);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        return new{tank=tank.name,forms=forms.Count,boss1Hp=680,boss2Hp=520,nodeHp=110,gold=36,regionBHpMultiplier=1.3f};
    }

    public static object ThreatClosure()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
        SetSerializedField(Load<EnemyDefinition>(RegionBFolder+"Enemies/Sniper.asset"),"maxHp",18);
        SetSerializedField(Load<EnemyDefinition>(RegionBFolder+"Enemies/Interceptor.asset"),"maxHp",16);
        var sweep=Load<DirectedAttackDefinition>(RegionBFolder+"Weapons/WardenSweep.asset");
        sweep.AlternateSweep=true;sweep.SweepLanes=new Vector2(.07f,.93f);sweep.ChargeSeconds=1.15f;sweep.ActiveSeconds=2.2f;EditorUtility.SetDirty(sweep);
        var curtainProjectilePath="Assets/StarShift/Prefabs/Projectiles/VanguardCurtainProjectile.prefab";
        var curtainProjectile=PrefabUtility.LoadPrefabContents(Load<GameObject>(curtainProjectilePath)!=null?curtainProjectilePath:"Assets/StarShift/Prefabs/Projectiles/EnemySingleProjectile.prefab");
        try
        {
            curtainProjectile.name="VanguardCurtainProjectile";
            curtainProjectile.GetComponent<BoxCollider2D>().size=new Vector2(2.1f,.24f);
            var visual=curtainProjectile.GetComponentInChildren<SpriteRenderer>();
            visual.transform.localScale=new Vector3(2.1f/visual.sprite.bounds.size.x,.28f/visual.sprite.bounds.size.y,1);visual.color=new Color(1,.6f,.12f);
            PrefabUtility.SaveAsPrefabAsset(curtainProjectile,curtainProjectilePath);
        }
        finally{PrefabUtility.UnloadPrefabContents(curtainProjectile);}
        foreach(var name in new[]{"VanguardFrigate","NebulaWarden"})
        {
            bool warden=name=="NebulaWarden";var path=EnemyPrefabFolder+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(warden)root.transform.localScale=Vector3.one*1.55f;
                var config=AuthorAsset<BossBarrageDefinition>((warden?RegionBFolder+"Boss/":DataFolder+"RegionA/Boss/")+(warden?"WardenIonShelter":"VanguardCurtain")+".asset",c=>{
                    c.Kind=warden?BossBarrageKind.IonShelter:BossBarrageKind.ProjectileCurtain;
                    c.OpeningWidth=.24f;c.ChargeSeconds=warden?1.15f:1.1f;c.PulseCount=3;c.PulseInterval=warden?.25f:.55f;c.ActiveSeconds=.55f;c.ProjectilesPerRow=21;
                    if(!warden)c.Projectile=AuthorAsset<SingleFireDefinition>(DataFolder+"RegionA/Boss/VanguardCurtainProjectile.asset",f=>{
                        EditorUtility.CopySerialized(Load<SingleFireDefinition>(DataFolder+"Enemies/ScoutSingleFire.asset"),f);
                        SetSerializedField(f,"projectilePrefab",Load<GameObject>(curtainProjectilePath).GetComponent<StarShift.Combat.EnemyProjectile>());
                        SetSerializedField(f,"projectileCount",1);SetSerializedField(f,"telegraphDuration",1.1f);SetSerializedField(f,"projectileSpeed",8.5f);
                    });
                });
                var node=root.transform.Find("ShelterBarrage");BossAttackModule module;
                if(node==null)
                {
                    module=Module(root,"ShelterBarrage",Load<SingleFireDefinition>(RegionBFolder+"Weapons/WardenDroneCharge.asset"));
                    var barrage=module.gameObject.AddComponent<BossBarrage>();SetSerializedField(barrage,"definition",config);
                    SetSerializedField(barrage,"lowerBandPresentation",Beam(module.gameObject));SetSerializedField(barrage,"upperBandPresentation",Beam(module.gameObject));SetSerializedField(module,"barrage",barrage);
                }
                else {module=node.GetComponent<BossAttackModule>();SetSerializedField(node.GetComponent<BossBarrage>(),"definition",config);}
                var runner=root.GetComponent<BossPhaseRunner>();
                var modules=(BossAttackModule[])runner.GetType().GetField("modules",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runner);
                if(!modules.Contains(module))modules=modules.Concat(new[]{module}).ToArray();SetSerializedField(runner,"modules",modules);
                var phase=runner.Definition;int special=Array.IndexOf(modules,module);
                phase.PhaseAOrder=warden?new[]{special,0,2,1,2}:new[]{special,0,1,0};
                phase.PhaseBOrder=warden?new[]{special,3,4,5}:new[]{special,2,0,1,2};EditorUtility.SetDirty(phase);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var beamMaterial=Load<Material>(DataFolder+"RegionA/Boss/BeamMaterial.mat");
        if(beamMaterial==null)throw new InvalidOperationException("Missing attack beam material");
        foreach(var prefabName in new[]{"Sniper","Interceptor","VanguardFrigate","NebulaWarden"})
        {
            var prefabPath=EnemyPrefabFolder+prefabName+".prefab";var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try {foreach(var line in prefab.GetComponentsInChildren<LineRenderer>(true))line.sharedMaterial=beamMaterial;PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        AssetDatabase.SaveAssets();return new{sniperHp=18,interceptorHp=16,wardenScale=1.55f,newBossSkills=2};
    }

}
