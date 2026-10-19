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

public static class ConfigureHumanFeel
{
    const string R = "Assets/StarShift/Data/RegionA/", P = "Assets/StarShift/Prefabs/Enemies/";
    static T Load<T>(string name) where T : UnityEngine.Object => StarShift.Editor.RegionAAssetPaths.Load<T>(R + name + ".asset");
    static GameObject Enemy(string name) => StarShift.Editor.RegionAAssetPaths.Load<GameObject>(P + name + ".prefab");
    static void Set(UnityEngine.Object target, string field, object value)
    { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); EditorUtility.SetDirty(target); }
    static T Asset<T>(string name, Action<T> configure) where T : ScriptableObject
    {
        var asset = Load<T>(name);
        if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, StarShift.Editor.RegionAAssetPaths.Resolve<T>(R + name + ".asset")); }
        configure(asset); EditorUtility.SetDirty(asset); return asset;
    }
    static FormationPlan Plan(string name, Vector2[] slots) => Asset<FormationPlan>(name, p => p.Slots = slots);
    static Vector2[] Mirror(Vector2[] left, params Vector2[] center) => left.Concat(left.Select(p => new Vector2(-p.x, p.y))).Concat(center).ToArray();
    static FlightSequence Flight(int side, bool terminal, float hold, bool sine = false)
    => Asset<FlightSequence>((terminal ? (side==0?"HeavyEscort":"FormationHold") : sine ? "SineWing" : hold==18 ? "M1FormationEntry" : "Formation") +
        (terminal && side==0 ? "" : side<0 ? "Left" : side>0 ? "Right" : "Center"), path =>
    {
        var start = new Vector2(side < 0 ? -.15f : side > 0 ? 1.15f : .5f, 1.16f);
        var target = new Vector2(.5f, .62f);
        path.Phases = new[] {
            new FlightPhase { Kind = FlightPhaseKind.Entry, Path = side < 0 ? FlightPath.ArcRight : side > 0 ? FlightPath.ArcLeft : FlightPath.Straight,
                From = start, To = target, Curve = new Vector2(.13f,.06f), Duration = 3.1f },
            new FlightPhase { Kind = FlightPhaseKind.Combat, Path = sine ? FlightPath.Sine : FlightPath.Straight, From = target, To = target,
                Curve = new Vector2(.065f,.025f), Duration = hold, CanShoot = true, ClampToView = true, HoldUntilResolved = terminal, RepeatMotion = sine },
            new FlightPhase { Kind = FlightPhaseKind.Exit, From = target, To = new Vector2(side < 0 ? -.3f : 1.3f,1.35f), Duration = 3.2f, CanShoot = true }
        };
        if (terminal) path.Phases = path.Phases.Take(2).ToArray();
    });
    static SpawnAction Group(string enemy, FormationPlan plan, int first, int count, FlightSequence path)
    => new SpawnAction { EnemyPrefab = Enemy(enemy), EnemyDefinition = Enemy(enemy).GetComponent<EnemyHealth>().Definition,
        Plan = plan, FirstSlot = first, Count = count, Sequence = path, Stagger = .09f, HpOverride = enemy == "Scout" ? 8 : 0 };
    static void Encounter(string name, FormationPlan plan, int wing, string enemy, bool terminal, float hold, int center = 0, bool sine = false)
    {
        var left = Flight(-1, terminal, hold, sine);
        var right = Flight(1, terminal, hold, sine);
        var groups = new[] { Group(enemy,plan,0,wing,left), Group(enemy,plan,wing,wing,right) }.ToList();
        if (center > 0) groups.Add(Group(enemy,plan,wing*2,center,Flight(0,terminal,hold,sine)));
        var encounter = Load<EncounterDefinition>(name);
        encounter.Groups = groups.ToArray(); encounter.EntryBeats = Array.Empty<EntryBeatRule>();
        EditorUtility.SetDirty(encounter);
    }
    static void Camera(string encounter, float size, Rect region)
    {
        var e = Load<EncounterDefinition>(encounter);
        e.CameraOverride = Asset<CameraProfile>(encounter == "M4Vanguard" ? "BossCamera" : encounter + "Camera", c => { c.View = new GameplayViewSettings(size,region); c.TransitionSeconds = 1.15f; });
        EditorUtility.SetDirty(e);
    }
    public static object Formations()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var arrow = Plan("FoundationChevron", Mirror(new[] {new Vector2(-.09f,.05f),new Vector2(-.18f,.10f),new Vector2(-.27f,.15f)},Vector2.zero));
        Encounter("M1Foundation",arrow,3,"Scout",true,20,1);
        var diamond = Plan("DoubleDiamond",Mirror(new[] {new Vector2(-.30f,.04f),new Vector2(-.24f,.13f),new Vector2(-.14f,.22f),new Vector2(-.23f,-.06f),new Vector2(-.12f,-.15f)},new Vector2(0,.28f),new Vector2(0,-.20f)));
        Encounter("M1Formation",diamond,5,"Scout",false,18,2);
        var spear = Plan("RaiderSpearhead",Mirror(new[] {new Vector2(-.10f,.03f),new Vector2(-.19f,.09f),new Vector2(-.28f,.15f),new Vector2(-.16f,.20f),new Vector2(-.27f,.25f)},new Vector2(0,-.06f),new Vector2(0,.12f)));
        Encounter("M2RaiderArcs",spear,5,"Raider",false,20,2);
        var escort = Plan("BomberGuard",Mirror(new[] {new Vector2(-.20f,.10f),new Vector2(-.29f,.01f),new Vector2(-.20f,-.10f),new Vector2(-.10f,-.18f)},new Vector2(-.075f,.13f),new Vector2(.075f,.13f)));
        Encounter("M2Bomber",escort,4,"Scout",true,20);
        var bomber = Load<EncounterDefinition>("M2Bomber");
        bomber.Groups = bomber.Groups.Concat(new[] { Group("Bomber",escort,8,2,Flight(0,true,20)) }).ToArray();
        var snake = Plan("MirroredSnakes",Mirror(new[] {new Vector2(-.30f,.22f),new Vector2(-.27f,.13f),new Vector2(-.19f,.04f),new Vector2(-.17f,-.05f),new Vector2(-.24f,-.14f),new Vector2(-.31f,-.23f)}));
        Encounter("M3Sine",snake,6,"Raider",false,20,sine:true);
        var cross = Plan("PincerCross",Mirror(new[] {new Vector2(-.08f,.06f),new Vector2(-.17f,.128f),new Vector2(-.26f,.195f),new Vector2(-.08f,-.06f),new Vector2(-.17f,-.128f),new Vector2(-.26f,-.195f)}));
        Encounter("M3PincerSupply",cross,6,"Scout",false,20);
        var pincer = Load<EncounterDefinition>("M3PincerSupply");
        pincer.Groups[1].HpOverride = 0; pincer.Groups[1].EnemyPrefab = Enemy("Raider"); pincer.Groups[1].EnemyDefinition = Enemy("Raider").GetComponent<EnemyHealth>().Definition;
        pincer.OptionalEvents[0].TimeOffset = 3.5f; pincer.OptionalEvents[0].FallbackTime = 16;
        var halo = Plan("EliteHalo",new[] {Vector2.zero}.Concat(Mirror(Enumerable.Range(0,5).Select(i=>new Vector2(Mathf.Cos((i+3)*Mathf.PI/5)*.26f,Mathf.Sin((i+3)*Mathf.PI/5)*.22f)).ToArray())).ToArray());
        var elite = Load<EncounterDefinition>("M3Elite");
        var anchor = Group("Bomber",halo,0,1,Flight(0,true,20));
        anchor.Elite = Load<EnemyModifierDefinition>("Elite"); anchor.RewardOverride = Load<RewardDropTable>("EliteDrops");
        elite.Groups = new[] {anchor,Group("Raider",halo,1,5,Flight(-1,true,20)),Group("Raider",halo,6,5,Flight(1,true,20))};
        elite.EntryBeats = Array.Empty<EntryBeatRule>(); EditorUtility.SetDirty(elite);
        var full = new Rect(0,0,1,1);
        var names = new[] {"M1Foundation","M1Formation","M2RaiderArcs","M2Storm","M2Bomber","M3Sine","M3PincerSupply","M3Elite","M4ResourceStorm","M4Vanguard"};
        var sizes = new[] {5.8f,6.2f,6.6f,7.3f,6.4f,6.9f,7.3f,6.6f,7.8f,9f};
        for (int i=0;i<names.Length;i++) Camera(names[i],sizes[i],i==9 ? new Rect(.02f,.02f,.96f,.59f) : full);
        foreach (var name in names) if (!Load<EncounterDefinition>(name).IsValid(out var error)) throw new InvalidOperationException(name+": "+error);
        AssetDatabase.SaveAssets(); return new {formations=7,cameras=10};
    }
    public static object DurabilityAndRewards()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        Set(Enemy("Raider").GetComponent<EnemyHealth>().Definition,"maxHp",11);
        Set(Enemy("Bomber").GetComponent<EnemyHealth>().Definition,"maxHp",22);
        var normal = StarShift.Editor.RegionAAssetPaths.Load<RewardDropTable>("Assets/StarShift/Data/Rewards/NormalDrops.asset");
        var progression = Asset<RewardDropTable>("ProgressionDrops", t => {
            t.Source=RewardSource.Normal; t.MajorItems=normal.MajorItems.Select(w=>new WeightedPickup { Item=w.Item,Weight=w.Item.Kind==PickupKind.WeaponCore?w.Weight*3:w.Weight }).ToArray();
            t.Shard=normal.Shard;
        });
        foreach (var name in new[] {"M1Foundation","M1Formation","M2RaiderArcs","M2Bomber","M3Sine","M3PincerSupply","M3Elite"})
        {
            var e=Load<EncounterDefinition>(name);e.GuaranteedMajorCount=1;e.GuaranteeTable=name=="M1Foundation"?Load<RewardDropTable>("OnboardingPulseCore"):progression;
            EditorUtility.SetDirty(e);
        }
        AssetDatabase.SaveAssets(); return new {scout=8,raider=11,bomber=22,elite=39,guarantees=7};
    }
    public static object Asteroids()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        foreach (var name in new[] {"M2Storm","M4ResourceStorm"})
        {
            var e=Load<EncounterDefinition>(name);var stream=e.HazardStream;
            stream.Count=name=="M2Storm"?30:36;stream.Interval=new Vector2(.46f,.65f);stream.TransitSeconds=new Vector2(4.8f,6.8f);
            stream.Variants=new[] {
                new AsteroidVariant{Size=AsteroidSize.Small,Scale=.7f,Hp=3,TransitMultiplier=.86f},
                new AsteroidVariant{Size=AsteroidSize.Medium,Scale=1.05f,Hp=7,TransitMultiplier=1},
                new AsteroidVariant{Size=AsteroidSize.Large,Scale=1.5f,Hp=13,TransitMultiplier=1.14f},
                new AsteroidVariant{Size=AsteroidSize.Huge,Scale=2.1f,Hp=22,TransitMultiplier=1.3f}};
            EditorUtility.SetDirty(stream);
            var resource=e.Groups[0].Sequence;resource.Phases[1].Duration=6.3f;EditorUtility.SetDirty(resource);
        }
        foreach (var name in new[] {"StormAsteroid","ResourceAsteroid"})
        {
            var path=P+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try {
                var hull=root.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="HullPlaceholder");
                var visual=root.GetComponent<AsteroidVisualMotion>()??root.AddComponent<AsteroidVisualMotion>(); Set(visual,"visual",hull.transform);
                var collider=root.GetComponentInChildren<BoxCollider2D>();collider.size=new Vector2(.66f,.66f);collider.offset=Vector2.zero;
                var damage=root.GetComponent<EnemyDamagePresentation>();Set(damage,"damagedHealthFraction",.66f);Set(damage,"criticalHealthFraction",.33f);
                Set(damage,"damagedTint",new Color(.7f,.4f,.24f));Set(damage,"criticalTint",new Color(1,.25f,.08f));
                PrefabUtility.SaveAsPrefabAsset(root,path);
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        var effect=runner.GetComponent<AsteroidDestructionPresentation>()??runner.gameObject.AddComponent<AsteroidDestructionPresentation>();
        Set(effect,"burstSprite",StarShift.Editor.RegionAAssetPaths.Load<Sprite>("Assets/StarShift/Art/Placeholders/impact-glow-placeholder.png"));
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        return new {m2=30,m4=36,variants=4};
    }

    static SpriteRenderer Part(Transform root, string name, Sprite sprite, Vector2 position, Vector2 size, Color color, int order=3)
    {
        var found=root.Find(name);var visual=found!=null?found.GetComponent<SpriteRenderer>():new GameObject(name).AddComponent<SpriteRenderer>();
        visual.transform.SetParent(root,false);visual.gameObject.layer=root.gameObject.layer;visual.sprite=sprite;visual.color=color;visual.sortingOrder=order;
        visual.transform.localPosition=position;visual.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
        return visual;
    }
    public static object Vanguard()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        var path=P+"VanguardFrigate.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var ship=StarShift.Editor.RegionAAssetPaths.Load<Sprite>("Assets/StarShift/Art/Placeholders/scout-placeholder.png");
            var bolt=StarShift.Editor.RegionAAssetPaths.Load<Sprite>("Assets/StarShift/Art/Placeholders/pulse-projectile-placeholder.png");
            var glow=StarShift.Editor.RegionAAssetPaths.Load<Sprite>("Assets/StarShift/Art/Placeholders/impact-glow-placeholder.png");
            var oldFire=root.GetComponent<EnemyFirePresentation>();if(oldFire!=null)UnityEngine.Object.DestroyImmediate(oldFire);
            foreach(var old in root.GetComponentsInChildren<SpriteRenderer>(true)) if(old.name.StartsWith("EnginePod")||old.name=="CorePlaceholder")UnityEngine.Object.DestroyImmediate(old.gameObject);
            var hull=root.GetComponentsInChildren<SpriteRenderer>(true).Single(x=>x.name=="HullPlaceholder");
            hull.transform.localScale=new Vector3(3.4f,3.9f,1);hull.color=new Color(.32f,.43f,.56f);
            for(int side=-1;side<=1;side+=2) {
                Part(root.transform,"CapitalWing"+side,ship,new Vector2(side*4.2f,.25f),new Vector2(5.6f,1.65f),new Color(.24f,.32f,.45f));
                Part(root.transform,"EngineMass"+side,bolt,new Vector2(side*5.8f,.75f),new Vector2(.9f,2.6f),new Color(.14f,.21f,.32f),4);
                Part(root.transform,"EngineGlow"+side,glow,new Vector2(side*5.8f,1.7f),new Vector2(.55f,1.1f),new Color(.15f,.85f,1,.8f),5);
                Part(root.transform,"CannonPod"+side,bolt,new Vector2(side*4.2f,-.9f),new Vector2(1.3f,1.6f),new Color(.48f,.53f,.6f),5);
                var hit=root.transform.Find("CapitalWing"+side).GetComponent<BoxCollider2D>(); if(hit==null)hit=root.transform.Find("CapitalWing"+side).gameObject.AddComponent<BoxCollider2D>();
                hit.isTrigger=true;hit.size=new Vector2(ship.bounds.size.x*.82f,ship.bounds.size.y*.65f);
            }
            Part(root.transform,"Reactor",glow,new Vector2(0,-.15f),new Vector2(1.65f,1.65f),new Color(.1f,.85f,1),6);
            Part(root.transform,"BeamPod",bolt,new Vector2(0,-1.55f),new Vector2(1.1f,1.4f),new Color(.7f,.16f,.35f),5);
            var body=root.transform.Find("Collision").GetComponent<BoxCollider2D>();body.size=new Vector2(3.7f,2.5f);
            var envelope=root.transform.Find("MovementEnvelope");if(envelope==null){envelope=new GameObject("MovementEnvelope").transform;envelope.SetParent(root.transform,false);}
            var box=envelope.GetComponent<BoxCollider2D>();if(box==null)box=envelope.gameObject.AddComponent<BoxCollider2D>();box.size=new Vector2(14,4.1f);box.enabled=false;
            Set(root.GetComponent<EntryHoldFlight>(),"collisionShape",box);
            var modules=root.GetComponentsInChildren<BossAttackModule>();
            for(int i=0;i<modules.Length;i++) {
                var muzzles=(Transform[])typeof(BossAttackModule).GetField("muzzles",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(modules[i]);
                for(int j=0;j<muzzles.Length;j++)muzzles[j].localPosition=new Vector3(i==0?(j==0?-4.2f:4.2f):i==1?5.8f:0,i==2?-2.25f:-1.65f,0);
                modules[i].transform.localPosition=new Vector3(i==1?5.8f:0,i==2?-1.55f:-1.05f,0);
                modules[i].transform.localScale=i==0?new Vector3(1.8f,.8f,1):new Vector3(.8f,.8f,1);
            }
            foreach(var module in modules){var oldCue=module.GetComponent<SpriteRenderer>();if(oldCue!=null)UnityEngine.Object.DestroyImmediate(oldCue);}
            Set(modules[0],"telegraph",root.transform.Find("CannonPod-1").GetComponent<SpriteRenderer>());
            Set(modules[0],"extraTelegraphs",new[]{root.transform.Find("CannonPod1").GetComponent<SpriteRenderer>()});
            var missilePod=Part(root.transform,"MissilePod",bolt,new Vector2(5.8f,-.9f),new Vector2(.65f,1.2f),new Color(1,.55f,.12f),6);
            Set(modules[1],"telegraph",missilePod);
            Set(modules[2],"telegraph",root.transform.Find("BeamPod").GetComponent<SpriteRenderer>());
            var beam=modules[2].GetComponent<BossBeamPresentation>();if(beam==null)beam=modules[2].gameObject.AddComponent<BossBeamPresentation>();
            var material=StarShift.Editor.RegionAAssetPaths.Load<Material>(R+"BeamMaterial.mat");
            if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,StarShift.Editor.RegionAAssetPaths.Resolve<Material>(R+"BeamMaterial.mat"));}
            for(int i=0;i<2;i++) {
                string name=i==0?"BeamColumn":"BeamCore";var node=root.transform.Find(name);
                if(node==null){node=new GameObject(name).transform;node.SetParent(root.transform,false);}
                var line=node.GetComponent<LineRenderer>();if(line==null)line=node.gameObject.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=2;
                line.sortingOrder=i==0?7:8;line.startColor=line.endColor=Color.white;line.enabled=false;
                Set(beam,i==0?"column":"core",line);
            }
            Set(modules[2],"beam",beam);Set(modules[2],"beamSeconds",1.5f);Set(modules[2],"beamViewportWidth",.10f);
            var cue=root.transform.Find("PhaseCue");cue.localScale=new Vector3(4,2.5f,1);
            var explosions=root.GetComponentsInChildren<SpriteRenderer>(true).Where(x=>x.name.StartsWith("DeathBeat")).OrderBy(x=>x.name).ToArray();
            for(int i=0;i<explosions.Length;i++){explosions[i].transform.localPosition=new Vector3(i==4?0:(i%2==0?-1:1)*4,.7f-i*.4f,0);explosions[i].transform.localScale=Vector3.one*(i==4?4:2);}
            PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        Set(Load<EnemyDefinition>("VanguardHealth"),"maxHp",520);
        Set(Load<SingleFireDefinition>("VanguardSweep"),"telegraphDuration",1.05f);
        var phases=Load<BossPhaseDefinition>("VanguardPhases");phases.PhaseAOrder=new[]{0,1,0};phases.PhaseBOrder=new[]{2,0,1,2};phases.TransitionSeconds=1.5f;
        phases.PhaseBRestSeconds=1.7f;phases.PhaseBOffset=new Vector2(-.06f,.015f);phases.PhaseBMotionMultiplier=1.2f;EditorUtility.SetDirty(phases);
        var flight=Load<FlightSequence>("VanguardFlight");flight.Phases[0].Duration=3.5f;flight.Phases[0].To=new Vector2(.5f,.73f);
        var combat=flight.Phases[1];combat.From=combat.To=flight.Phases[0].To;combat.Curve=new Vector2(.12f,.012f);combat.Cycles=1;combat.Duration=8;EditorUtility.SetDirty(flight);
        var hud=UnityEngine.Object.FindAnyObjectByType<StarShift.UI.BossHud>().GetComponent<RectTransform>();
        hud.offsetMin=new Vector2(0,-105);hud.offsetMax=new Vector2(0,-55);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();return new{hp=520,width=14,camera=9,signature="Lane Lock",phaseB=50};
    }
    public static object VisualRefinements()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        var path=P+"Bomber.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try {root.transform.localScale=Vector3.one*1.45f;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        foreach(var name in new[]{"StormAsteroid","ResourceAsteroid"}) {
            path=P+name+".prefab";root=PrefabUtility.LoadPrefabContents(path);
            try {var hull=root.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="HullPlaceholder");var size=Vector2.Scale(hull.sprite.bounds.size,hull.transform.localScale)*.72f;root.GetComponentInChildren<BoxCollider2D>().size=size;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        return new{bomberScale=1.45f,asteroidCollider="inset visual bounds"};
    }

    public static object PrepareVerification()
    {
        var files=new[]{"VerifyPacingRefinement.cs","VerifyHumanFeel.cs"};
        var sources=files.Select(n=>System.IO.File.ReadAllText("../tools/unity/"+n)).ToArray();
        var lines=sources.Select(s=>s.Split('\n')).ToArray();
        var imports=lines.SelectMany(s=>s).Where(s=>s.StartsWith("using ")).Distinct();
        var bodies=lines.Select(s=>string.Join("\n",s.Where(line=>!line.StartsWith("using "))));
        var folder="../.design/m5-human-feel/";System.IO.Directory.CreateDirectory(folder+"screenshots");
        var path=folder+"Verification.cs";System.IO.File.WriteAllText(path,string.Join("\n",imports)+"\n"+string.Join("\n",bodies));
        return new{path=System.IO.Path.GetFullPath(path)};
    }

    public static object ConsolidateFlights()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        var encounters=Enumerable.Range(1,3).SelectMany(n=>Load<LevelDefinition>("Mission0"+n).Encounters).ToArray();
        var previous=encounters.SelectMany(e=>e.Groups).Select(g=>JsonUtility.ToJson(g.Sequence)).ToArray();
        Formations();
        var next=encounters.SelectMany(e=>e.Groups).Select(g=>JsonUtility.ToJson(g.Sequence)).ToArray();
        // Tên asset khác; dữ liệu flight phải giữ nguyên sau gộp.
        string Normalize(string json)=>System.Text.RegularExpressions.Regex.Replace(json,"\"m_Name\":\"[^\"]*\"", "\"m_Name\":\"shared\"");
        if(!previous.Select(Normalize).SequenceEqual(next.Select(Normalize)))throw new InvalidOperationException("Consolidation changed flight data");
        var used=encounters.SelectMany(e=>e.Groups).Select(g=>AssetDatabase.GetAssetPath(g.Sequence)).ToHashSet();
        var candidates=AssetDatabase.FindAssets("t:FlightSequence",new[]{R.TrimEnd('/')}).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>System.Text.RegularExpressions.Regex.IsMatch(System.IO.Path.GetFileName(p),"^M[123].*Entry(Left|Right|Center)\\.asset$")&&!used.Contains(p)).ToArray();
        var all=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/StarShift/")&&(p.EndsWith(".asset")||p.EndsWith(".prefab")||p.EndsWith(".unity"))).ToArray();
        foreach(var candidate in candidates) {
            if(all.Any(p=>p!=candidate&&AssetDatabase.GetDependencies(p,false).Contains(candidate)))throw new InvalidOperationException("Still referenced: "+candidate);
            if(!AssetDatabase.DeleteAsset(candidate))throw new InvalidOperationException("Delete failed: "+candidate);
        }
        AssetDatabase.SaveAssets();return new{sameFlightData=true,removedDuplicates=candidates};
    }

    public static object ConsolidateBossCamera()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        var encounter=Load<EncounterDefinition>("M4Vanguard");var current=encounter.CameraOverride;var shared=Load<CameraProfile>("BossCamera");
        shared.View=current.View;shared.TransitionSeconds=current.TransitionSeconds;EditorUtility.SetDirty(shared);
        encounter.CameraOverride=shared;EditorUtility.SetDirty(encounter);AssetDatabase.SaveAssets();
        var sameView=shared.View.PlayableViewport==current.View.PlayableViewport;
        var path=AssetDatabase.GetAssetPath(current);
        if(current!=shared) {
            var refs=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/StarShift/")&&p!=path).Where(p=>AssetDatabase.GetDependencies(p,false).Contains(path)).ToArray();
            if(refs.Length>0)throw new InvalidOperationException("Camera still referenced: "+string.Join(",",refs));
            AssetDatabase.DeleteAsset(path);
        }
        return new{size=shared.View.OrthographicSize,sameView,existingGuidPreserved=true};
    }
}
