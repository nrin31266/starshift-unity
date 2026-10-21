using System;
using System.IO;
using System.Linq;
using StarShift.Enemies;
using StarShift.Combat;
using UnityEditor;
using UnityEngine;

public static class ConfigurePolishVfx
{
    static Sprite[] Frames(string id)=>AssetDatabase.LoadAllAssetsAtPath("Assets/StarShift/Art/VFX/"+id+"-sheet.png").OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
    static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Array(UnityEngine.Object target,string field,Sprite[] values)
    {if(values.Length==0)throw new Exception("Missing "+field);var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static void Number(UnityEngine.Object target,string field,float value){var so=new SerializedObject(target);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void ColorField(UnityEngine.Object target,string field,Color value){var so=new SerializedObject(target);so.FindProperty(field).colorValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Objects(UnityEngine.Object target,string field,UnityEngine.Object[] values)
    {var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static SpriteRenderer Visual(Transform parent,string name,Sprite sprite)
    {var t=parent.Find(name);if(t==null){t=new GameObject(name).transform;t.SetParent(parent,false);}var r=t.GetComponent<SpriteRenderer>();if(r==null)r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=31;r.enabled=false;return r;}
    public static object BulwarkFire()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        const string profilePath="Assets/StarShift/Data/Enemies/BulwarkSingleFire.asset";
        var profile=AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(profilePath);
        if(profile==null)
        {
            profile=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<SingleFireDefinition>("Assets/StarShift/Data/Enemies/ScoutSingleFire.asset"));
            profile.name="BulwarkSingleFire";AssetDatabase.CreateAsset(profile,profilePath);
        }
        Number(profile,"initialDelay",1.2f);Number(profile,"telegraphDuration",.9f);Number(profile,"cooldown",5);Number(profile,"projectileSpeed",3.6f);
        var so=new SerializedObject(profile);so.FindProperty("projectileCount").intValue=1;so.FindProperty("burstCount").intValue=1;so.FindProperty("projectileScale").vector2Value=Vector2.one;so.FindProperty("projectileColor").colorValue=Color.white;
        so.FindProperty("projectileSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/Projectiles/hostile-lemon.png");so.ApplyModifiedPropertiesWithoutUndo();
        const string path="Assets/StarShift/Prefabs/Enemies/Bulwark.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var attack=root.GetComponent<SingleFireAttack>();attack.enabled=true;Ref(attack,"definition",profile);
            var muzzle=(Transform)new SerializedObject(attack).FindProperty("muzzle").objectReferenceValue;
            var cue=(SpriteRenderer)new SerializedObject(root.GetComponent<EnemyFirePresentation>()).FindProperty("chargeCue").objectReferenceValue;
            cue.transform.position=muzzle.position+root.transform.up*.08f;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();return new{singleShot=true,telegraph=.9f,cooldown=5,speed=3.6f,sharedCoordinator=true};
    }
    public static object Bind()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        const string beamPath="Assets/StarShift/Art/VFX/beam-energy.png";
        var texture=(TextureImporter)AssetImporter.GetAtPath(beamPath);texture.wrapMode=TextureWrapMode.Repeat;texture.SaveAndReimport();
        var beamMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/StarShift/Data/RegionA/Boss/BeamMaterial.mat");
        beamMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(beamPath);EditorUtility.SetDirty(beamMaterial);
        const string fieldPath="Assets/StarShift/Art/VFX/ion-curtain.png";
        var fieldTexture=(TextureImporter)AssetImporter.GetAtPath(fieldPath);fieldTexture.wrapMode=TextureWrapMode.Repeat;fieldTexture.SaveAndReimport();
        const string fieldMaterialPath="Assets/StarShift/Data/RegionB/Boss/IonFieldMaterial.mat";
        var fieldMaterial=AssetDatabase.LoadAssetAtPath<Material>(fieldMaterialPath);
        if(fieldMaterial==null){fieldMaterial=new Material(beamMaterial);AssetDatabase.CreateAsset(fieldMaterial,fieldMaterialPath);}
        fieldMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(fieldPath);EditorUtility.SetDirty(fieldMaterial);
        var curtain=AssetDatabase.LoadAssetAtPath<SingleFireDefinition>("Assets/StarShift/Data/RegionA/Boss/VanguardCurtainProjectile.asset");
        var curtainSo=new SerializedObject(curtain);curtainSo.FindProperty("projectileSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/Projectiles/vanguard-curtain-bolt.png");curtainSo.FindProperty("projectileScale").vector2Value=Vector2.one;curtainSo.FindProperty("projectileColor").colorValue=Color.white;curtainSo.ApplyModifiedPropertiesWithoutUndo();
        const string projectilePath="Assets/StarShift/Prefabs/Projectiles/VanguardCurtainProjectile.prefab";
        var projectileRoot=PrefabUtility.LoadPrefabContents(projectilePath);
        try{var visual=projectileRoot.GetComponentInChildren<SpriteRenderer>();visual.sprite=curtain.ProjectileSprite;var size=visual.sprite.bounds.size;var box=projectileRoot.GetComponent<BoxCollider2D>().size;visual.transform.localScale=new Vector3(box.x/size.x,box.y/size.y,1);PrefabUtility.SaveAsPrefabAsset(projectileRoot,projectilePath);}finally{PrefabUtility.UnloadPrefabContents(projectileRoot);}
        int prefabs=0,healths=0,focus=0,skills=0;
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/StarShift/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset.GetComponentInChildren<EnemyHealth>(true)==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(root.GetComponent<AsteroidVisualMotion>()!=null)
                {
                    var family=root.name=="ResourceAsteroid"?"resource-asteroid":"asteroid-medium";
                    var damage=root.GetComponent<EnemyDamagePresentation>();
                    Ref(damage,"damagedSprite",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/Environments/"+family+"-damaged.png"));
                    Ref(damage,"criticalSprite",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/Environments/"+family+"-critical.png"));
                }
                foreach(var health in root.GetComponentsInChildren<EnemyHealth>(true))
                {
                    var hit=health.GetComponent<EnemyHitPresentation>();if(hit==null)hit=health.gameObject.AddComponent<EnemyHitPresentation>();
                    var so=new SerializedObject(hit);var impact=(SpriteRenderer)so.FindProperty("impact").objectReferenceValue;
                    if(impact==null)
                    {
                        var hull=health.GetComponentInChildren<SpriteRenderer>(true);
                        impact=new GameObject("ProjectileImpact").AddComponent<SpriteRenderer>();impact.transform.SetParent(health.transform,false);
                        impact.sharedMaterial=hull.sharedMaterial;impact.sortingLayerID=hull.sortingLayerID;impact.sortingOrder=30;impact.enabled=false;Ref(hit,"impact",impact);
                    }
                    Array(hit,"pulseImpactFrames",Frames("hit-pulse"));Array(hit,"plasmaImpactFrames",Frames("hit-plasma"));Array(hit,"homingImpactFrames",Frames("hit-homing"));Array(hit,"rockImpactFrames",Frames("hit-rock"));
                    Array(hit,"shieldImpactFrames",Frames("hit-shield"));
                    Array(hit,"hullImpactFrames",Frames("hit-pulse"));Array(hit,"moduleImpactFrames",Frames("hit-homing"));Array(hit,"hitFrames",Frames("hit-pulse"));
                    impact.sprite=Frames("hit-pulse")[0];
                    var sparks=(SpriteRenderer)new SerializedObject(hit).FindProperty("sparks").objectReferenceValue;if(sparks!=null)sparks.sprite=Frames("hit-pulse")[0];healths++;
                }
                foreach(var fire in root.GetComponentsInChildren<EnemyFirePresentation>(true))
                {var fireSo=new SerializedObject(fire);var charge=Frames(fireSo.FindProperty("specialDanger").boolValue?"focus-sniper":"focus-gun");Array(fire,"chargeFrames",charge);var renderer=(SpriteRenderer)fireSo.FindProperty("chargeCue").objectReferenceValue;if(renderer!=null)renderer.sprite=charge[0];Number(fire,"cueSizeAt900",44);focus++;}
                foreach(var directed in root.GetComponentsInChildren<DirectedAttackTelegraph>(true))
                {
                    var so=new SerializedObject(directed);bool strike=so.FindProperty("strike").boolValue;
                    var warning=Frames(strike?"focus-strike":"focus-sniper");Array(directed,"warningFrames",warning);var renderer=(SpriteRenderer)so.FindProperty("warning").objectReferenceValue;if(renderer!=null)renderer.sprite=warning[0];focus++;
                }
                bool warden=path.Contains("Warden");
                foreach(var beam in root.GetComponentsInChildren<AttackBeamPresentation>(true))
                {
                    bool curtainField=beam.GetComponentInParent<BossBarrage>()!=null;
                    foreach(var line in beam.GetComponentsInChildren<LineRenderer>(true))
                    {line.sharedMaterial=beamMaterial;line.textureMode=LineTextureMode.Tile;line.numCapVertices=curtainField?0:3;line.numCornerVertices=3;}
                    if(curtainField)
                    {
                        var beamSo=new SerializedObject(beam);var column=(LineRenderer)beamSo.FindProperty("column").objectReferenceValue;
                        column.sharedMaterial=fieldMaterial;column.sortingOrder=-1;
                    }
                }
                foreach(var cue in root.GetComponentsInChildren<BossSkillPhasePresentation>(true))
                {
                    var so=new SerializedObject(cue);var barrage=(BossBarrage)so.FindProperty("barrage").objectReferenceValue;
                    var module=(BossAttackModule)so.FindProperty("module").objectReferenceValue;
                    string family;
                    if(barrage!=null)family=warden?"warden-ion-shelter-cue":"vanguard-curtain-cue";
                    else if(module!=null)
                    {
                        var moduleSo=new SerializedObject(module);
                        family=moduleSo.FindProperty("directed").objectReferenceValue!=null?"warden-beam-sweep-cue"
                            :moduleSo.FindProperty("beam").objectReferenceValue!=null?"vanguard-lane-beam-cue":"focus-missile";
                    }
                    else continue;
                    var frames=Frames(family);var renderer=(SpriteRenderer)so.FindProperty("cue").objectReferenceValue;if(renderer!=null)renderer.sprite=frames[0];Array(cue,"prepareFrames",frames.Take(frames.Length/2).ToArray());
                    Array(cue,"lockFrames",frames.Skip(frames.Length/2).ToArray());Array(cue,"attackFrames",frames.Reverse().ToArray());
                    if(family=="vanguard-curtain-cue")Array(cue,"attackFrames",Frames("vanguard-curtain-pulse"));
                    if(family=="warden-ion-shelter-cue")Array(cue,"attackFrames",Frames("warden-shelter-pulse"));
                    if(family=="warden-beam-sweep-cue")Array(cue,"attackFrames",Frames("warden-sweep-pulse"));
                    Number(cue,"frameSeconds",family.StartsWith("warden-",StringComparison.Ordinal)?.035f:.055f);
                    ColorField(cue,"prepareTint",new Color(1,1,1,.9f));ColorField(cue,"lockTint",Color.white);ColorField(cue,"attackTint",Color.white);
                    Number(cue,"cueScale",1.4f);skills++;
                }
                if(path.EndsWith("/VanguardFrigate.prefab",StringComparison.Ordinal))
                {
                    var armor=root.GetComponent<BossArmorBreakPresentation>();if(armor==null)armor=root.AddComponent<BossArmorBreakPresentation>();
                    var chunks=Enumerable.Range(0,2).Select(i=>Visual(root.transform,"ArmorBreakFragment"+i,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/VFX/boss-armor-fragment-"+i+".png"))).ToArray();
                    var blasts=Enumerable.Range(0,2).Select(i=>Visual(root.transform,"ArmorBreakBurst"+i,Frames("boss-armor-rupture")[0])).ToArray();
                    Objects(armor,"fragments",chunks);Objects(armor,"bursts",blasts);Array(armor,"burstFrames",Frames("boss-armor-rupture"));
                    var armorSo=new SerializedObject(armor);var fractions=armorSo.FindProperty("healthFractions");fractions.arraySize=2;
                    var damageSo=new SerializedObject(root.GetComponent<EnemyDamagePresentation>());fractions.GetArrayElementAtIndex(0).floatValue=damageSo.FindProperty("damagedHealthFraction").floatValue;fractions.GetArrayElementAtIndex(1).floatValue=damageSo.FindProperty("criticalHealthFraction").floatValue;armorSo.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);prefabs++;
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return new{prefabs,healths,focus,skills};
    }
}
