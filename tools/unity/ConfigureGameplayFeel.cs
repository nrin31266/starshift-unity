using System;
using System.Linq;
using StarShift.Encounters;
using UnityEditor;
using UnityEngine;

public static class ConfigureGameplayFeel
{
    static readonly string Root = "Assets/StarShift/Data/";
    static FlightSequence Flight(string name, Vector2 entry, Vector2 exit, FlightPath path, bool hold = false)
    {
        var assetPath = Root + "Flights/" + name + ".asset";
        var sequence = AssetDatabase.LoadAssetAtPath<FlightSequence>(assetPath);
        if (sequence == null) { sequence = ScriptableObject.CreateInstance<FlightSequence>(); AssetDatabase.CreateAsset(sequence, assetPath); }
        var center = new Vector2(.5f, .72f);
        sequence.Phases = hold ? new[] {
            new FlightPhase { Kind = FlightPhaseKind.Entry, Path = path, From = entry, To = center, Duration = 1.3f, Curve = new Vector2(.15f,.12f) },
            new FlightPhase { Kind = FlightPhaseKind.Combat, From = center, To = center, Duration = 9, CanShoot = true, ClampToView = true, HoldUntilResolved = true }
        } : new[] {
            new FlightPhase { Kind = FlightPhaseKind.Entry, Path = path, From = entry, To = center, Duration = 1.3f, Curve = new Vector2(.15f,.12f) },
            new FlightPhase { Kind = FlightPhaseKind.Combat, From = center, To = center, Duration = 9, CanShoot = true, ClampToView = true },
            new FlightPhase { Kind = FlightPhaseKind.Exit, From = center, To = exit, Duration = 3f, CanShoot = true }
        };
        if (!sequence.IsValid(out var error)) throw new InvalidOperationException(error);
        EditorUtility.SetDirty(sequence);
        return sequence;
    }
    static object ConfigureFlights()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var retreat = Flight("M4TopRetreat", new Vector2(.5f,1.8f), new Vector2(.5f,1.8f), FlightPath.Straight);
        var dive = Flight("M4TopDive", new Vector2(.5f,1.8f), new Vector2(.5f,-.8f), FlightPath.Straight);
        var right = Flight("M4RightSweep", new Vector2(1.6f,.86f), new Vector2(-.8f,.45f), FlightPath.ArcLeft);
        var diagonal = Flight("M4DiagonalPass", new Vector2(-.6f,1.6f), new Vector2(1.8f,-.8f), FlightPath.ArcRight);
        var holdRight = Flight("M4HoldRight", new Vector2(1.6f,.86f), Vector2.zero, FlightPath.ArcLeft, true);
        for (int n = 2; n <= 4; n++)
        {
            var encounter = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Root + "Encounters/L1Encounter" + (char)('A'+n-1) + ".asset");
            for (int i = 0; i < encounter.Groups.Length; i++)
                encounter.Groups[i].Sequence = n == 2 ? (i%2==0 ? retreat : right) :
                    n == 3 ? (i%2==0 ? diagonal : retreat) : (i%2==0 ? dive : right);
            encounter.CompleteWhenScheduleExhausted = true;
            if (!encounter.IsValid(out var error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(encounter);
        }
        var final = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Root + "Encounters/L1EncounterE.asset");
        final.Groups[0].Sequence = holdRight;
        EditorUtility.SetDirty(final);
        AssetDatabase.SaveAssets();
        return new { pass = true, timed = 3, newSequences = 5 };
    }
    static T Ensure<T>(GameObject node) where T : Component
    {
        var component=node.GetComponent<T>();
        return component!=null ? component : node.AddComponent<T>();
    }
    static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Number(UnityEngine.Object target, string field, float value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }
    public static object Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        ConfigureFlights();
        string dataRoot = "Assets/StarShift/Data/";
        string rewards = dataRoot + "Rewards/";
        if (AssetDatabase.LoadAssetAtPath<StarShift.Rewards.PickupDefinition>(rewards+"MissileAmmo.asset") == null)
        {
            var error = AssetDatabase.MoveAsset(rewards+"EMP.asset",rewards+"MissileAmmo.asset");
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            error = AssetDatabase.MoveAsset(rewards+"TestEMP.asset",rewards+"TestMissileAmmo.asset");
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }
        var ammo = AssetDatabase.LoadAssetAtPath<StarShift.Rewards.PickupDefinition>(rewards+"MissileAmmo.asset");
        ammo.name="MissileAmmo";ammo.ShortCode="MSL+1";EditorUtility.SetDirty(ammo);
        var table = AssetDatabase.LoadAssetAtPath<StarShift.Rewards.RewardDropTable>(rewards+"TestMissileAmmo.asset");
        table.name="TestMissileAmmo";EditorUtility.SetDirty(table);
        var shard = AssetDatabase.LoadAssetAtPath<StarShift.Rewards.PickupDefinition>(rewards+"StellarShard.asset");
        shard.DriftSpeed=3.2f;shard.FallAcceleration=11;shard.MaximumFallSpeed=6.4f;shard.Lifetime=5;EditorUtility.SetDirty(shard);
        foreach (var id in new[]{"ShieldCell","Overdrive","SupportDrone","ExtraLife","PulseCore","PlasmaCore","HomingCore","MissileAmmo"})
        {
            var item=AssetDatabase.LoadAssetAtPath<StarShift.Rewards.PickupDefinition>(rewards+id+".asset");item.DriftSpeed=.8f;EditorUtility.SetDirty(item);
        }
        var thermalNames = new[]{"PulseLaserThermal","PlasmaThermal","HomingThermal"};
        for(int i=0;i<thermalNames.Length;i++)
        {
            var thermal=AssetDatabase.LoadAssetAtPath<StarShift.Combat.WeaponThermalProfile>(dataRoot+"Weapons/"+thermalNames[i]+".asset");
            Number(thermal,"heatPerSecond",i==0?9:i==1?11:8.5f);Number(thermal,"coolingDelay",.3f);
            Number(thermal,"coolRate",30);Number(thermal,"overheatCoolRate",50);Number(thermal,"recoveryFraction",.4f);
        }
        var weaponNames=new[]{"PulseLaserLv1","PlasmaSpread","HomingMissile"};
        for(int i=0;i<weaponNames.Length;i++)
        {
            var weapon=AssetDatabase.LoadAssetAtPath<StarShift.Combat.PulseLaserDefinition>(dataRoot+"Weapons/"+weaponNames[i]+".asset");
            var data=new SerializedObject(weapon);
            data.FindProperty("projectileSpeed").floatValue=i==0?15:i==1?10:8;
            if(i==1)for(int level=0;level<3;level++)data.FindProperty("levels").GetArrayElementAtIndex(level).FindPropertyRelative("SpreadDegrees").floatValue=level==0?32:level==1?48:58;
            if(i==2)for(int level=1;level<3;level++)data.FindProperty("levels").GetArrayElementAtIndex(level).FindPropertyRelative("SpreadDegrees").floatValue=level==1?28:44;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        for(int i=0;i<5;i++)
        {
            var encounter=AssetDatabase.LoadAssetAtPath<EncounterDefinition>(dataRoot+"Encounters/L1Encounter"+(char)('A'+i)+".asset");
            encounter.RewardGraceSeconds=1.5f;EditorUtility.SetDirty(encounter);
        }
        var player=GameObject.Find("Player");
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        var life=player.GetComponent<StarShift.Player.PlayerDamageLifecycle>();
        Number(life,"deathDuration",.6f);Number(life,"shieldBreakInvincibilityDuration",.7f);
        Number(player.GetComponent<StarShift.Player.PlayerRespawnEntrance>(),"duration",.55f);
        var reserve=Ensure<StarShift.Player.EmergencyMissileReserve>(player);
        Reference(reserve,"runner",runner);Reference(reserve,"lifecycle",life);
        var view=Camera.main.GetComponent<StarShift.Gameplay.GameplayViewController>();
        Number(view,"reserveImpactDuration",.14f);Number(view,"reserveImpactStrength",.004f);
        var burstNode=player.transform.Find("MissileBurst");
        if(burstNode==null){burstNode=new GameObject("MissileBurst").transform;burstNode.SetParent(player.transform,false);}
        var burst=ConfigureRing(burstNode);
        var presentation=Ensure<StarShift.Player.EmergencyMissilePresentation>(player);
        Reference(presentation,"reserve",reserve);Reference(presentation,"burst",burst);Reference(presentation,"view",view);
        Reference(presentation,"persistence",runner.GetComponent<StarShift.Persistence.CampaignLifecycle>());
        var hud=UnityEngine.Object.FindAnyObjectByType<StarShift.UI.GameplayHud>();var combat=hud.transform.Find("SafeArea/CombatHud");
        var existing=combat.Find("EmergencyMissile");
        var root=existing!=null?(RectTransform)existing:new GameObject("EmergencyMissile",typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(combat,false);root.anchorMin=root.anchorMax=root.pivot=new Vector2(1,0);root.anchoredPosition=new Vector2(-20,80);root.sizeDelta=new Vector2(176,56);
        var image=Ensure<UnityEngine.UI.Image>(root.gameObject);
        image.color=new Color(.09f,.17f,.21f,.9f);image.raycastTarget=true;
        var button=Ensure<UnityEngine.UI.Button>(root.gameObject);button.targetGraphic=image;button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
        var label=root.GetComponentInChildren<UnityEngine.UI.Text>();
        if(label==null)
        {
            var labelRoot=new GameObject("Label",typeof(RectTransform));labelRoot.transform.SetParent(root,false);
            var rect=labelRoot.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(4,0);rect.offsetMax=new Vector2(-4,0);
            label=labelRoot.AddComponent<UnityEngine.UI.Text>();
        }
        label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=20;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(1,.7f,.3f);label.raycastTarget=false;label.text="TÊN LỬA 1/3";
        Reference(hud,"missileReserve",reserve);Reference(hud,"missileButton",button);Reference(hud,"missileCount",label);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(player.scene);
        ConfigurePrefabDefaults();
        RefineWeaponsAndMissile();
        TuneFallExitAndCooling();
        AssetDatabase.SaveAssets();
        return new {pass=true,ammo=ammo.ShortCode,capacity=reserve.Capacity,deathSeconds=.6f,entranceSeconds=.55f,impactSeconds=.14f};
    }

    static void ConfigurePrefabDefaults()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var player=GameObject.Find("Player");
        var path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player);
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var life=new SerializedObject(root.GetComponent<StarShift.Player.PlayerDamageLifecycle>());
            life.FindProperty("deathDuration").floatValue=.6f;life.ApplyModifiedPropertiesWithoutUndo();
            var entrance=new SerializedObject(root.GetComponent<StarShift.Player.PlayerRespawnEntrance>());
            entrance.FindProperty("duration").floatValue=.55f;entrance.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    static LineRenderer ConfigureRing(Transform node)
    {
        var old=node.GetComponent<SpriteRenderer>();if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        var ring=node.GetComponent<LineRenderer>();if(ring==null)ring=node.gameObject.AddComponent<LineRenderer>();
        ring.useWorldSpace=false;ring.loop=true;ring.positionCount=48;ring.widthMultiplier=.08f;
        for(int i=0;i<48;i++){var angle=i*Mathf.PI*2/48;ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*.5f,Mathf.Sin(angle)*.5f,0));}
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/StarShift/VFX/ReserveMissileRing.mat");
        if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,"Assets/StarShift/VFX/ReserveMissileRing.mat");}
        ring.sharedMaterial=material;ring.sortingOrder=12;ring.enabled=false;return ring;
    }

    static Sprite ProjectileSprite(string name, Func<float,float,Color> pixel)
    {
        var path="Assets/StarShift/Art/Placeholders/"+name+".png";
        var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);
        var colors=new Color[128*128];
        for(int y=0;y<128;y++)for(int x=0;x<128;x++)colors[y*128+x]=pixel((x+.5f)/64-1,(y+.5f)/64-1);
        texture.SetPixels(colors);texture.Apply();
        System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=128;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Transform Child(Transform parent,string name)
    {
        var node=parent.Find(name);if(node!=null)return node;
        node=new GameObject(name).transform;node.SetParent(parent,false);return node;
    }
    static TrailRenderer ConfigureTrail(Transform node, Material material,float width)
    {
        var trail=Ensure<TrailRenderer>(node.gameObject);trail.sharedMaterial=material;
        trail.time=.13f;trail.startWidth=width;trail.endWidth=0;trail.minVertexDistance=.04f;
        trail.numCapVertices=2;trail.sortingOrder=9;trail.autodestruct=false;trail.emitting=false;
        trail.startColor=new Color(1,.65f,.2f,.75f);trail.endColor=new Color(1,.25f,.05f,0);
        return trail;
    }
    public static object RefineWeaponsAndMissile()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        var pulse=ProjectileSprite("pulse-bolt-placeholder",(x,y)=>
        {
            var end=Mathf.Max(0,Mathf.Abs(y)-.52f)/.35f;
            if(x*x/(.36f*.36f)+end*end>1)return Color.clear;
            return Mathf.Abs(x)<.16f?Color.white:new Color(.5f,.85f,1,.85f);
        });
        var plasma=ProjectileSprite("plasma-orb-placeholder",(x,y)=>
        {
            var r=Mathf.Sqrt(x*x+y*y);if(r>.9f)return Color.clear;
            var core=Mathf.Clamp01((.65f-r)/.45f);
            return new Color(1,Mathf.Lerp(.45f,1,core),1,Mathf.Clamp01((.9f-r)*7));
        });
        var rocket=ProjectileSprite("homing-rocket-placeholder",(x,y)=>
        {
            var ax=Mathf.Abs(x);
            if(y>.35f && y<.88f && ax<(.88f-y)*.52f)return Color.white;
            if(y>=-.5f && y<=.35f && ax<.275f)return ax<.14f?Color.white:new Color(.55f,.65f,.75f);
            if(y>=-.7f && y<-.2f && ax<.58f && ax<(.1f-y)*.9f)return new Color(.6f,.7f,.8f);
            if(y<-.5f && y>-.96f && ax<(.96f+y)*.5f)return new Color(1,.55f,.12f,1);
            return Color.clear;
        });
        var impact=ProjectileSprite("impact-glow-placeholder",(x,y)=>new Color(1,1,1,Mathf.Clamp01((.9f-Mathf.Sqrt(x*x+y*y))*2)));
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/StarShift/VFX/ReserveMissileRing.mat");
        var names=new[]{"PulseLaserLv1","PlasmaSpread","HomingMissile"};
        var sprites=new[]{pulse,plasma,rocket};
        for(int i=0;i<3;i++)
        {
            var weapon=AssetDatabase.LoadAssetAtPath<StarShift.Combat.PulseLaserDefinition>(Root+"Weapons/"+names[i]+".asset");
            Reference(weapon,"projectileSprite",sprites[i]);
            var data=new SerializedObject(weapon);
            data.FindProperty("projectileScale").vector2Value=i==0?new Vector2(.25f,.52f):i==1?new Vector2(.475f,.38f):new Vector2(.35f,.48f);
            data.FindProperty("trailSeconds").floatValue=i==2?.13f:0;
            if(i==2)data.FindProperty("projectileColor").colorValue=new Color(1,.8f,.55f);
            for(int level=0;level<3;level++)data.FindProperty("levels").GetArrayElementAtIndex(level).FindPropertyRelative("LateralSpacing").floatValue=i==1?0:.22f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        var prefabPath="Assets/StarShift/Prefabs/Projectiles/PlayerPulseProjectile.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try{ConfigureTrail(Child(prefab.transform,"Trail"),material,.1f);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var player=GameObject.Find("Player");
        var presentation=player.GetComponent<StarShift.Player.EmergencyMissilePresentation>();
        var flight=Ensure<SpriteRenderer>(Child(player.transform,"MissileFlight").gameObject);
        flight.sprite=rocket;flight.color=new Color(1,.9f,.7f);flight.sortingOrder=12;flight.enabled=false;
        flight.transform.localScale=new Vector3(.7f,.9f,1);
        var trailRenderer=ConfigureTrail(Child(flight.transform,"Trail"),material,.16f);
        var flash=Ensure<SpriteRenderer>(Child(player.transform,"MissileFlash").gameObject);
        flash.sprite=impact;flash.sortingOrder=11;flash.enabled=false;
        Reference(presentation,"rocket",flight);Reference(presentation,"trail",trailRenderer);Reference(presentation,"flash",flash);
        Number(presentation,"duration",.45f);Number(player.GetComponent<StarShift.Player.EmergencyMissileReserve>(),"flightSeconds",.32f);
        for(int i=0;i<4;i++)
        {
            var encounter=AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Root+"Encounters/L1Encounter"+(char)('A'+i)+".asset");
            encounter.HandoffDelay=1;EditorUtility.SetDirty(encounter);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(player.scene);AssetDatabase.SaveAssets();
        return new{pass=true,distinctSprites=3,flightSeconds=.32f,handoffSeconds=1};
    }
    public static object TuneFallExitAndCooling()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        var shard=AssetDatabase.LoadAssetAtPath<StarShift.Rewards.PickupDefinition>(Root+"Rewards/StellarShard.asset");
        shard.DriftSpeed=3.2f;shard.FallAcceleration=11;shard.MaximumFallSpeed=6.4f;EditorUtility.SetDirty(shard);
        foreach(var id in new[]{"M4TopRetreat","M4TopDive","M4RightSweep","M4DiagonalPass"})
        {
            var sequence=AssetDatabase.LoadAssetAtPath<FlightSequence>(Root+"Flights/"+id+".asset");
            foreach(var phase in sequence.Phases)if(phase.Kind==FlightPhaseKind.Exit)phase.Duration=3;
            EditorUtility.SetDirty(sequence);
        }
        foreach(var id in new[]{"PulseLaserThermal","PlasmaThermal","HomingThermal"})
        {
            var profile=AssetDatabase.LoadAssetAtPath<StarShift.Combat.WeaponThermalProfile>(Root+"Weapons/"+id+".asset");
            Number(profile,"coolingDelay",.3f);Number(profile,"coolRate",30);Number(profile,"overheatCoolRate",50);
        }
        var encounter=AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Root+"Encounters/L1EncounterD.asset");
        encounter.Duration=21;EditorUtility.SetDirty(encounter);
        AssetDatabase.SaveAssets();return new{pass=true,fallSpeed=3.2f,terminalSpeed=6.4f,exitSeconds=3,coolingDelay=.3f,coolRate=30,overheatCoolRate=50};
    }
}
