using System;
using System.Linq;
using System.Reflection;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConfigureM4Testbed
{
    const string Data = "Assets/StarShift/Data/";
    const string Prefabs = "Assets/StarShift/Prefabs/";
    static readonly Color Cyan = new Color(.3f,.9f,1);
    static readonly Color Amber = new Color(1,.76f,.2f);
    static void Set(UnityEngine.Object obj, string field, object value)
    {
        obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj,value);
        EditorUtility.SetDirty(obj);
    }
    static T Add<T>(GameObject root) where T:Component => root.GetComponent<T>() ?? root.AddComponent<T>();
    static T Asset<T>(string path, Action<T> configure) where T:ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var asset = ScriptableObject.CreateInstance<T>(); configure(asset); AssetDatabase.CreateAsset(asset,path); return asset;
    }
    static void Folder(string parent,string child) { if (!AssetDatabase.IsValidFolder(parent+"/"+child)) AssetDatabase.CreateFolder(parent,child); }
    static PickupDefinition Item(string name,PickupKind kind,string code,Color color,WeaponBranch branch=WeaponBranch.Pulse)
        => Asset<PickupDefinition>(Data+"Rewards/"+name+".asset", x => {x.Kind=kind;x.ShortCode=code;x.Color=color;x.Branch=branch;});
    static FlightSequence Flight(string name,Vector2 from,Vector2 hold,float holdSeconds,bool terminal=false,FlightPath path=FlightPath.Straight,Vector2 curve=default)
        => Asset<FlightSequence>(Data+"Flights/"+name+".asset", x => {
            x.Phases = new[] {
                new FlightPhase { Kind=FlightPhaseKind.Entry,Path=path,From=from,To=hold,Duration=1.3f,Curve=curve },
                new FlightPhase { Kind=FlightPhaseKind.Combat,From=hold,To=hold,Duration=holdSeconds,CanShoot=true,ClampToView=true,HoldUntilResolved=terminal },
                new FlightPhase { Kind=FlightPhaseKind.Exit,From=hold,To=new Vector2(1.2f,hold.y-.12f),Duration=4,CanShoot=true }
            };
            if(terminal) x.Phases=x.Phases.Take(2).ToArray();
        });
    static SpawnAction Group(GameObject prefab, FlightSequence flight, int count, float start, float yOffset, RewardDropTable reward=null, FormationLayout formation=FormationLayout.Line)
        => new SpawnAction { EnemyPrefab=prefab,EnemyDefinition=prefab.GetComponent<EnemyHealth>().Definition,
            Sequence=flight,Count=count,StartTime=start,Stagger=.12f,Anchor=new Vector2(0,yOffset),
            Formation=formation,SlotStep=new Vector2(.1f,.08f),FormationRise=.025f,Required=true,RewardOverride=reward };
    static GameObject Target(string name,int hp,int score,bool damaging,Color color,Sprite sprite)
    {
        var path=Prefabs+"Enemies/"+name+".prefab";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null)return existing;
        var definition=Asset<EnemyDefinition>(Data+"Enemies/"+name+".asset",x=> {Set(x,"maxHp",hp);Set(x,"score",score);Set(x,"bodyDamagesPlayer",damaging);});
        var root=PrefabUtility.LoadPrefabContents(Prefabs+"Enemies/Scout.prefab");
        try
        {
            root.name=name;
            Set(root.GetComponent<EnemyHealth>(),"definition",definition);
            root.GetComponent<SingleFireAttack>().enabled=false;
            foreach(var visual in root.GetComponentsInChildren<SpriteRenderer>()) {visual.sprite=sprite;visual.color=color;visual.transform.localScale=new Vector3(.7f,.55f,1);}
            return PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
    public static object Configure()
    {
        var scene=SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.isDirty || scene.path!="Assets/StarShift/Scenes/02_Gameplay.unity") throw new InvalidOperationException("Saved gameplay scene/Edit Mode required.");
        Folder("Assets/StarShift/Data","Rewards");Folder("Assets/StarShift/Prefabs","Rewards");
        var pulse=AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>(Data+"Weapons/PulseLaserLv1.asset");
        if(pulse.Pattern(WeaponLoadout.MaximumLevel)==null) Set(pulse,"levels",new[]{
            new WeaponLevelPattern {ProjectileCount=1},
            new WeaponLevelPattern {ProjectileCount=2},
            new WeaponLevelPattern {ProjectileCount=3,CenterBonus=1},
            new WeaponLevelPattern {ProjectileCount=3,Damage=2},
            new WeaponLevelPattern {ProjectileCount=5,CenterBonus=3,FireInterval=.16f}
        });
        var plasma=Asset<PulseLaserDefinition>(Data+"Weapons/PlasmaSpread.asset",x=>{
            Set(x,"branch",WeaponBranch.Plasma);Set(x,"projectilePrefab",pulse.ProjectilePrefab);Set(x,"projectileColor",new Color(.8f,.4f,1));
            Set(x,"thermalProfile",Asset<WeaponThermalProfile>(Data+"Weapons/PlasmaThermal.asset",_=>{}));
            Set(x,"levels",new[]{new WeaponLevelPattern{ProjectileCount=3,SpreadDegrees=26,FireInterval=.25f},
                new WeaponLevelPattern{ProjectileCount=5,SpreadDegrees=40,FireInterval=.25f},
                new WeaponLevelPattern{ProjectileCount=5,SpreadDegrees=40,CenterBonus=2,FireInterval=.22f},
                new WeaponLevelPattern{ProjectileCount=7,SpreadDegrees=66,LateralSpacing=0,CenterBonus=3,FireInterval=.22f},
                new WeaponLevelPattern{ProjectileCount=7,SpreadDegrees=70,LateralSpacing=0,Damage=2,CenterBonus=1,FireInterval=.22f}});
        });
        var homing=Asset<PulseLaserDefinition>(Data+"Weapons/HomingMissile.asset",x=>{
            Set(x,"branch",WeaponBranch.Homing);Set(x,"projectilePrefab",pulse.ProjectilePrefab);Set(x,"projectileColor",new Color(1,.5f,.16f));
            Set(x,"projectileSpeed",8f);Set(x,"projectileLifetime",3f);Set(x,"trackingDegreesPerSecond",110f);Set(x,"trackingRange",18f);
            Set(x,"thermalProfile",Asset<WeaponThermalProfile>(Data+"Weapons/HomingThermal.asset",_=>{}));
            Set(x,"levels",new[]{new WeaponLevelPattern{ProjectileCount=1,Damage=2,FireInterval=.5f},
                new WeaponLevelPattern{ProjectileCount=2,Damage=2,FireInterval=.5f},
                new WeaponLevelPattern{ProjectileCount=3,Damage=2,FireInterval=.5f,SplashRadius=.85f},
                new WeaponLevelPattern{ProjectileCount=4,SpreadDegrees=50,Damage=2,FireInterval=.48f,SplashRadius=1},
                new WeaponLevelPattern{ProjectileCount=5,SpreadDegrees=56,Damage=2,FireInterval=.45f,SplashRadius=1.1f}});
        });
        var shard=Item("StellarShard",PickupKind.Shard,"SS",Amber);
        var pulseCore=Item("PulseCore",PickupKind.WeaponCore,"T1+",Cyan);
        var plasmaCore=Item("PlasmaCore",PickupKind.WeaponCore,"T2+",new Color(.8f,.4f,1),WeaponBranch.Plasma);
        var homingCore=Item("HomingCore",PickupKind.WeaponCore,"T3+",new Color(1,.5f,.16f),WeaponBranch.Homing);
        var shield=Item("ShieldCell",PickupKind.Shield,"SH",new Color(.2f,1,.7f));
        var od=Item("Overdrive",PickupKind.Overdrive,"OD",new Color(1,.45f,.8f));
        var drone=Item("SupportDrone",PickupKind.SupportDrone,"DR",Cyan); if(drone.Duration==8){drone.Duration=11;EditorUtility.SetDirty(drone);}
        var ammo=Item("MissileAmmo",PickupKind.MissileAmmo,"MSL+1",new Color(.7f,.8f,1));
        var life=Item("ExtraLife",PickupKind.ExtraLife,"+1",new Color(.4f,1,.5f));
        WeightedPickup[] weights={new WeightedPickup{Item=pulseCore,Weight=10},new WeightedPickup{Item=plasmaCore,Weight=10},new WeightedPickup{Item=homingCore,Weight=10},
            new WeightedPickup{Item=shield,Weight=25},new WeightedPickup{Item=od,Weight=18},new WeightedPickup{Item=drone,Weight=14},new WeightedPickup{Item=ammo,Weight=9},new WeightedPickup{Item=life,Weight=4}};
        var normal=Asset<RewardDropTable>(Data+"Rewards/NormalDrops.asset",x=>{x.Shard=shard;x.MajorItems=weights;});
        var major=Asset<RewardDropTable>(Data+"Rewards/SupplyCrateDrops.asset",x=>{x.Source=RewardSource.MajorCrate;x.MajorItems=weights;x.Shard=shard;});
        var resource=Asset<RewardDropTable>(Data+"Rewards/ResourceDrops.asset",x=>{x.Source=RewardSource.Resource;x.Shard=shard;});
        Func<PickupDefinition,RewardDropTable> forced=item=>Asset<RewardDropTable>(Data+"Rewards/Test"+item.name+".asset",x=>{x.GuaranteedPickup=item;x.Shard=shard;});
        var disc=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/Placeholders/planet-disc-placeholder.png");
        var cargo=Target("SupplyDrone",3,0,false,Amber,disc);
        var asteroid=Target("ResourceAsteroid",4,0,true,new Color(.65f,.48f,.2f),disc);
        var pickupPath=Prefabs+"Rewards/RewardPickup.prefab";
        var pickup=AssetDatabase.LoadAssetAtPath<RewardPickup>(pickupPath);
        if(pickup==null)
        {
            var root=new GameObject("RewardPickup");
            try
            {
                var runtime=root.AddComponent<RewardPickup>();
                var visual=new GameObject("Symbol").AddComponent<SpriteRenderer>();visual.transform.SetParent(root.transform,false);visual.sprite=disc;visual.sortingOrder=6;visual.transform.localScale=Vector3.one*.25f;
                var label=new GameObject("Code").AddComponent<TextMesh>();label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,-.45f,0);label.anchor=TextAnchor.MiddleCenter;label.characterSize=.065f;label.fontSize=56;label.color=Color.white;label.GetComponent<MeshRenderer>().sortingOrder=7;
                Set(runtime,"visual",visual);Set(runtime,"label",label);
                pickup=PrefabUtility.SaveAsPrefabAsset(root,pickupPath).GetComponent<RewardPickup>();
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        var playerPath=Prefabs+"Player/Player.prefab";
        var playerPrefab=PrefabUtility.LoadPrefabContents(playerPath);
        try
        {
            Set(Add<WeaponLoadout>(playerPrefab),"weapons",new[]{pulse,plasma,homing});
            var effects=Add<PlayerTemporaryEffects>(playerPrefab);
            Add<PlayerPickupReceiver>(playerPrefab);
            var shieldNode=playerPrefab.transform.Find("ShieldCue");
            if(shieldNode==null)
            {
                shieldNode=new GameObject("ShieldCue").transform;shieldNode.SetParent(playerPrefab.transform,false);
                var visual=shieldNode.gameObject.AddComponent<SpriteRenderer>();visual.sprite=disc;visual.color=new Color(.1f,1,.7f,.22f);visual.sortingOrder=-1;shieldNode.localScale=Vector3.one*.7f;visual.enabled=false;
            }
            var feedback=Add<PlayerEffectPresentation>(playerPrefab);Set(feedback,"effects",effects);Set(feedback,"shield",shieldNode.GetComponent<SpriteRenderer>());
            var droneNode=playerPrefab.transform.Find("SupportDrone");
            if(droneNode==null)
            {
                droneNode=new GameObject("SupportDrone").transform;droneNode.SetParent(playerPrefab.transform,false);droneNode.localPosition=new Vector3(-.85f,.15f,0);
                var visual=droneNode.gameObject.AddComponent<SpriteRenderer>();visual.sprite=disc;visual.color=Cyan;visual.sortingOrder=5;droneNode.localScale=Vector3.one*.18f;visual.enabled=false;
            }
            var assistant=Add<SupportDrone>(droneNode.gameObject);Set(assistant,"effects",effects);Set(assistant,"gate",playerPrefab.GetComponent<PlayerGameplayGate>());Set(assistant,"weapon",pulse);Set(assistant,"visual",droneNode.GetComponent<SpriteRenderer>());
            PrefabUtility.SaveAsPrefabAsset(playerPrefab,playerPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(playerPrefab);}
        var player=GameObject.Find("Player");
        var encounterRoot=GameObject.Find("L1Encounters");
        var rewards=Add<RunRewards>(encounterRoot);
        var receiver=player.GetComponent<PlayerPickupReceiver>();Set(receiver,"rewards",rewards);
        var director=Add<RewardDirector>(encounterRoot);Set(director,"defaultDrops",normal);Set(director,"pickupPrefab",pickup);Set(director,"receiver",receiver);
        ConfigureHud(player,rewards);
        var scout=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"Enemies/Scout.prefab");
        var hold=Flight("M4Hold",new Vector2(.5f,1.12f),new Vector2(.5f,.73f),1,true);
        var fly=Flight("M4Arc",new Vector2(-.2f,.86f),new Vector2(.5f,.72f),9,false,FlightPath.ArcRight,new Vector2(.15f,.12f));
        var supplyFlight=Flight("M4Supply",new Vector2(-.1f,.56f),new Vector2(.5f,.55f),6);
        var a=AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Data+"Fixtures/Encounters/L1EncounterA.asset");
        var b=AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Data+"Fixtures/Encounters/L1EncounterB.asset");
        var c=Asset<EncounterDefinition>(Data+"Fixtures/Encounters/L1EncounterC.asset",_=>{});
        var d=Asset<EncounterDefinition>(Data+"Fixtures/Encounters/L1EncounterD.asset",_=>{});
        var e=Asset<EncounterDefinition>(Data+"Fixtures/Encounters/L1EncounterE.asset",_=>{});
        a.Groups=new[]{Group(scout,hold,5,0,0),Group(scout,hold,1,1.8f,-.2f,forced(pulseCore))};
        b.Groups=new[]{Group(scout,fly,6,0,0,normal,FormationLayout.V),Group(scout,fly,6,.4f,-.17f,normal,FormationLayout.Staggered),Group(scout,fly,1,4,-.28f,forced(plasmaCore))};
        c.Groups=new[]{Group(scout,fly,4,0,0),Group(scout,fly,1,1,-.17f,forced(shield)),Group(scout,fly,1,3,-.28f,forced(od)),Group(scout,fly,1,5,-.39f,forced(drone)),Group(scout,fly,4,9,0)};
        d.Groups=new[]{Group(scout,fly,6,0,0,normal,FormationLayout.Wedge),Group(scout,fly,1,2,-.25f,forced(ammo)),Group(scout,fly,1,4,-.38f,forced(homingCore)),Group(scout,fly,4,7,-.12f)};
        e.Groups=new[]{Group(scout,hold,5,0,0),Group(scout,hold,1,0,-.2f,forced(life)),Group(scout,hold,1,0,-.35f,forced(pulseCore))};
        var all=new[]{a,b,c,d,e};
        for(int i=0;i<all.Length;i++)
        {
            var x=all[i];x.Id="ENCOUNTER_"+(i+1);x.ScoutPrefab=scout;x.Completion=(i==0||i==4)?EncounterCompletion.Eliminate:EncounterCompletion.Timeline;
            x.Duration=i==2?24:20;x.RewardGraceSeconds=4;x.HandoffDelay=1.7f;x.ActiveShooterLimit=2;x.ShotSpacing=.7f;x.ShotsPerVolley=2;x.VolleyPause=1.7f;
            x.TransitionTitle="Encounter "+(i+1);x.TransitionDescription="";x.OptionalEvents=Array.Empty<OptionalEvent>();EditorUtility.SetDirty(x);
        }
        c.OptionalEvents=new[]{new OptionalEvent{Id="M4_SUPPLY",Kind=OptionalEventKind.SupplyDrone,TimeOffset=7,Group=Group(cargo,supplyFlight,1,0,0,major)}};
        e.OptionalEvents=new[]{new OptionalEvent{Id="M4_RESOURCE",Kind=OptionalEventKind.GoldenMeteor,TimeOffset=1,Group=Group(asteroid,supplyFlight,1,0,-.18f,resource)}};
        var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>(Data+"Fixtures/Levels/M4SystemTestbed.asset");
        level.Encounters=all;
        level.AdditionalStarts=new[]{new LevelStartPoint{Id="SYSTEM_DENSITY",Encounter=b},new LevelStartPoint{Id="SYSTEM_EFFECTS",Encounter=c},new LevelStartPoint{Id="SYSTEM_SURVIVAL",Encounter=d},new LevelStartPoint{Id="SYSTEM_REWARDS",Encounter=e}};
        EditorUtility.SetDirty(level);
        if(!level.IsValid(out var error))throw new InvalidOperationException(error);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return new { configured=true,encounters=all.Select(x=>new{x.Id,x.EnemyCount,x.Completion}).ToArray(),preservedIntroDelay=level.Intro.ContinueDelaySeconds };
    }
    public static object Inspect()
    {
        var scene=SceneManager.GetActiveScene();
        var nodes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        int missingReferences=0;
        foreach(var component in nodes.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null))
        {
            var iterator=new SerializedObject(component).GetIterator();
            while(iterator.NextVisible(true))
                if(iterator.propertyType==SerializedPropertyType.ObjectReference && iterator.objectReferenceValue==null && iterator.objectReferenceEntityIdValue!=default)missingReferences++;
        }
        var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>(Data+"Fixtures/Levels/M4SystemTestbed.asset");
        var loadout=GameObject.Find("Player").GetComponent<WeaponLoadout>();
        foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})
        {
            var weapon=loadout.Definition(branch);
            if(weapon==null || weapon.ThermalProfile==null || weapon.ProjectilePrefab==null)throw new InvalidOperationException("Missing weapon config: "+branch);
            for(int i=1;i<=WeaponLoadout.MaximumLevel;i++)if(weapon.Pattern(i)==null || weapon.Pattern(i).ProjectileCount<1 || weapon.Pattern(i).FireInterval<=0)throw new InvalidOperationException("Invalid weapon pattern");
        }
        var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/StarShift/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        var missingScripts=nodes.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))+
            prefabs.Sum(p=>p.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
        return new{valid=level.IsValid(out var error),error,missingScripts,missingReferences,scene.isDirty,encounterCount=level.Encounters.Length,
            introDelay=level.Intro.ContinueDelaySeconds,runInBackground=PlayerSettings.runInBackground};
    }

    static void ConfigureHud(GameObject player,RunRewards rewards)
    {
        var hud=GameObject.Find("GameplayUI").GetComponent<GameplayHud>();var combat=hud.transform.Find("SafeArea/CombatHud");
        Set(hud,"rewards",rewards);Set(hud,"loadout",player.GetComponent<WeaponLoadout>());Set(hud,"effects",player.GetComponent<PlayerTemporaryEffects>());Set(hud,"pickupReceiver",player.GetComponent<PlayerPickupReceiver>());
        var weapon=combat.Find("Weapon");var button=Add<Button>(weapon.gameObject);var image=Add<Image>(weapon.gameObject);image.color=Color.clear;image.raycastTarget=true;button.targetGraphic=image;Set(hud,"weaponButton",button);
        var counterRoot=Child(combat,"Counters");counterRoot.anchorMin=counterRoot.anchorMax=new Vector2(.5f,1);counterRoot.pivot=new Vector2(.5f,1);counterRoot.anchoredPosition=new Vector2(0,-8);counterRoot.sizeDelta=new Vector2(160,44);
        var group=Add<HorizontalLayoutGroup>(counterRoot.gameObject);group.spacing=12;group.childControlWidth=true;group.childControlHeight=false;group.childForceExpandWidth=false;group.childForceExpandHeight=false;group.childAlignment=TextAnchor.MiddleCenter;
        Add<ContentSizeFitter>(counterRoot.gameObject).horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
        foreach(var name in new[]{"Score","Shard"})
        {
            var row=combat.Find(name)??counterRoot.Find(name);row.SetParent(counterRoot,false);
            var rt=(RectTransform)row;rt.sizeDelta=new Vector2(100,44);
            var layout=Add<HorizontalLayoutGroup>(row.gameObject);layout.padding=new RectOffset(4,4,0,0);layout.spacing=4;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;layout.childAlignment=TextAnchor.MiddleCenter;
            var plate=row.Find("Plate");if(plate!=null)Add<LayoutElement>(plate.gameObject).ignoreLayout=true;
            foreach(var text in row.GetComponentsInChildren<Text>()){text.resizeTextForBestFit=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;}
        }
        Set(hud,"effectLabel",Cue(combat,"Effects",new Vector2(0,32),22));Set(hud,"pickupLabel",Cue(combat,"PickupCue",new Vector2(0,65),24));
        foreach(var graphic in combat.GetComponentsInChildren<Graphic>())if(graphic.gameObject!=weapon.gameObject && graphic.transform.GetComponentInParent<Button>()==null)graphic.raycastTarget=false;
        foreach(var graphic in weapon.GetComponentsInChildren<Graphic>())if(graphic!=image)graphic.raycastTarget=false;
    }
    static RectTransform Child(Transform parent,string name)
    {
        var existing=parent.Find(name) as RectTransform;if(existing!=null)return existing;
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
    }
    static Text Cue(Transform parent,string name,Vector2 position,int size)
    {
        var rect=Child(parent,name);rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.anchoredPosition=position;rect.sizeDelta=new Vector2(300,32);
        var text=Add<Text>(rect.gameObject);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;text.color=Cyan;text.raycastTarget=false;text.text="";return text;
    }
}
