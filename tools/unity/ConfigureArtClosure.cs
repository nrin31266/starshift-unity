using System;
using System.Linq;
using StarShift.Enemies;
using StarShift.Combat;
using StarShift.Player;
using StarShift.Gameplay;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

public static class ConfigureArtClosure
{
    public static object ProductionDefaults()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var dev=UnityEngine.Object.FindAnyObjectByType<StarShift.Dev.DevFastStart>();var config=new SerializedObject(dev);
        config.FindProperty("devModeEnabled").boolValue=false;config.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(dev);
        var component=dev.GetComponent<StarShift.Dev.EncounterDebugView>();var debug=new SerializedObject(component);
        foreach(var flag in new[]{"showBounds","showPaths","showSpawnPoints","showHitboxes","showFps","showContentLabels"})debug.FindProperty(flag).boolValue=false;
        debug.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(component);
        EditorSceneManager.MarkSceneDirty(dev.gameObject.scene);EditorSceneManager.SaveScene(dev.gameObject.scene);
        return new{devModeOff=true,debugFlagsOff=true,startPoint=config.FindProperty("startPoint").stringValue};
    }
    static Sprite[] Frames(string id)=>AssetDatabase.LoadAllAssetsAtPath("Assets/StarShift/Art/VFX/"+id+"-sheet.png").OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
    static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Array(UnityEngine.Object target,string field,Sprite[] frames)
    {
        var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=frames.Length;for(int i=0;i<frames.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=frames[i];so.ApplyModifiedPropertiesWithoutUndo();
    }
    static SpriteRenderer Visual(Transform parent,string name,Sprite sprite,int order)
    {
        var child=parent.Find(name);if(child==null){child=new GameObject(name).transform;child.SetParent(parent,false);}
        var r=child.GetComponent<SpriteRenderer>();if(r==null)r=child.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;r.enabled=false;return r;
    }
    static void Player(GameObject root)
    {
        var feedback=root.GetComponent<PlayerCombatPresentation>();
        Array(feedback,"rewardFrames",Frames("pickup-collect-upgrade"));Array(feedback,"upgradeRewardFrames",Frames("pickup-collect-upgrade"));
        Array(feedback,"coinRewardFrames",Frames("pickup-coin"));Array(feedback,"shieldRewardFrames",Frames("pickup-shield"));
        var reward=(SpriteRenderer)new SerializedObject(feedback).FindProperty("reward").objectReferenceValue;reward.sprite=Frames("pickup-collect-upgrade")[0];
        var death=root.GetComponent<PlayerDeathPresentation>();Array(death,"explosionFrames",Frames("explosion"));
        var explosion=(SpriteRenderer)new SerializedObject(death).FindProperty("explosion").objectReferenceValue;explosion.sprite=Frames("explosion")[0];
    }
    public static object Art()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        int prefabs=0;
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/StarShift/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.GetComponentInChildren<EnemyHealth>(true)==null&&asset.GetComponent<PlayerCombatPresentation>()==null&&asset.GetComponent<EnemyProjectile>()==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(root.GetComponent<PlayerCombatPresentation>()!=null)Player(root);
                foreach(var hit in root.GetComponentsInChildren<EnemyHitPresentation>(true))
                {
                    foreach(var family in new[]{"pulse","plasma","homing","rock"})Array(hit,family+"ImpactFrames",Frames("hit-"+family));
                    Array(hit,"hullImpactFrames",Frames("hit-pulse"));Array(hit,"moduleImpactFrames",Frames("hit-homing"));Array(hit,"hitFrames",Frames("hit-pulse"));
                    var visual=(SpriteRenderer)new SerializedObject(hit).FindProperty("impact").objectReferenceValue;if(visual!=null)visual.sprite=Frames("hit-pulse")[0];
                }
                foreach(var cue in root.GetComponentsInChildren<DirectedAttackTelegraph>(true))
                {
                    var so=new SerializedObject(cue);if(!so.FindProperty("strike").boolValue)continue;
                    Array(cue,"warningFrames",Frames("focus-strike"));Array(cue,"recoverFrames",System.Array.Empty<Sprite>());
                    var warning=(SpriteRenderer)so.FindProperty("warning").objectReferenceValue;warning.sprite=Frames("focus-strike")[0];
                    var launch=Visual(cue.transform,"StrikeLaunch",Frames("strike-launch")[0],warning.sortingOrder+1);launch.sharedMaterial=warning.sharedMaterial;launch.sortingLayerID=warning.sortingLayerID;
                    Ref(cue,"launch",launch);Array(cue,"launchFrames",Frames("strike-launch"));
                }
                foreach(var beam in root.GetComponentsInChildren<AttackBeamPresentation>(true))
                {
                    var directed=beam.GetComponentInParent<DirectedAttack>();if(directed==null||!directed.Definition.Sweep)continue;
                    var so=new SerializedObject(beam);var guide=(LineRenderer)so.FindProperty("sweepGuide").objectReferenceValue;
                    Ref(beam,"sweepGuide",null);if(guide!=null&&guide.gameObject!=beam.gameObject)UnityEngine.Object.DestroyImmediate(guide.gameObject);
                    var arrow=Visual(beam.transform,"SweepArrow",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/VFX/warden-sweep-arrow.png"),30);
                    Ref(beam,"sweepArrow",arrow);arrow.gameObject.SetActive(false);
                }
                foreach(var staged in root.GetComponentsInChildren<StagedDestructionPresentation>(true))
                {
                    var so=new SerializedObject(staged);var stages=so.FindProperty("stages");var frames=Frames("boss-blast");
                    for(int i=0;i<stages.arraySize;i++)
                    {
                        var stage=stages.GetArrayElementAtIndex(i);var bank=stage.FindPropertyRelative("blastFrames");bank.arraySize=frames.Length;
                        for(int f=0;f<frames.Length;f++)bank.GetArrayElementAtIndex(f).objectReferenceValue=frames[f];
                        var blast=(SpriteRenderer)stage.FindPropertyRelative("blast").objectReferenceValue;
                        if(blast!=null){blast.sprite=frames[0];var loop=blast.GetComponent<SpriteFrameLoop>();if(loop!=null)UnityEngine.Object.DestroyImmediate(loop);}
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach(var death in root.GetComponentsInChildren<BossDeathSequence>(true))
                {
                    Array(death,"explosionFrames",Frames("boss-blast"));var so=new SerializedObject(death);var bursts=so.FindProperty("explosions");
                    for(int i=0;i<bursts.arraySize;i++)
                    {
                        var burst=(SpriteRenderer)bursts.GetArrayElementAtIndex(i).objectReferenceValue;burst.sprite=Frames("boss-blast")[0];
                        var loop=burst.GetComponent<SpriteFrameLoop>();if(loop!=null)UnityEngine.Object.DestroyImmediate(loop);
                    }
                }
                if(root.name=="VanguardCurtainProjectile")
                {
                    var frames=AssetDatabase.LoadAllAssetsAtPath("Assets/StarShift/Art/Projectiles/vanguard-curtain-bolt-sheet.png").OfType<Sprite>().OrderBy(f=>f.name,StringComparer.Ordinal).ToArray();
                    var visual=root.GetComponentInChildren<SpriteRenderer>();var size=root.GetComponent<BoxCollider2D>().size;visual.sprite=frames[0];visual.transform.localScale=new Vector3(size.x/frames[0].bounds.size.x,size.y/frames[0].bounds.size.y,1);
                    var loop=visual.GetComponent<SpriteFrameLoop>();if(loop==null)loop=visual.gameObject.AddComponent<SpriteFrameLoop>();Array(loop,"frames",frames);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);prefabs++;
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var missile=UnityEngine.Object.FindAnyObjectByType<EmergencyMissilePresentation>();Array(missile,"flashFrames",Frames("missile-splash"));
        var flash=(SpriteRenderer)new SerializedObject(missile).FindProperty("flash").objectReferenceValue;flash.sprite=Frames("missile-splash")[0];EditorUtility.SetDirty(missile);EditorUtility.SetDirty(flash);
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerCombatPresentation>();Player(player.gameObject);
        foreach(var component in player.GetComponentsInChildren<Component>(true))EditorUtility.SetDirty(component);
        var destruction=UnityEngine.Object.FindAnyObjectByType<AsteroidDestructionPresentation>();Array(destruction,"enemyFrames",Frames("explosion"));Array(destruction,"heavyFrames",Frames("heavy-burst"));Array(destruction,"supplyFrames",Frames("supply-burst-polished"));EditorUtility.SetDirty(destruction);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();return new{prefabs,selectedArtBound=true};
    }
    public static object Gameplay()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var curtain=AssetDatabase.LoadAssetAtPath<BossBarrageDefinition>("Assets/StarShift/Data/RegionA/Boss/VanguardCurtain.asset");
        curtain.OpeningWidth=.18f;curtain.PulseInterval=.95f;curtain.ProjectilesPerRow=25;EditorUtility.SetDirty(curtain);
        const string projectilePath="Assets/StarShift/Prefabs/Projectiles/VanguardCurtainProjectile.prefab";
        var projectile=PrefabUtility.LoadPrefabContents(projectilePath);
        try{projectile.GetComponent<BoxCollider2D>().size=new Vector2(1.9f,.45f);PrefabUtility.SaveAsPrefabAsset(projectile,projectilePath);}finally{PrefabUtility.UnloadPrefabContents(projectile);}
        int duplicateCues=0;
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/StarShift/Prefabs/Enemies"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var directed in root.GetComponentsInChildren<DirectedAttack>(true))
                {
                    var fire=directed.GetComponent<EnemyFirePresentation>();if(fire==null)continue;
                    var cue=(SpriteRenderer)new SerializedObject(fire).FindProperty("chargeCue").objectReferenceValue;
                    UnityEngine.Object.DestroyImmediate(fire);
                    if(cue!=null)UnityEngine.Object.DestroyImmediate(cue.gameObject);
                    duplicateCues++;
                }
                if(root.name=="VanguardFrigate")
                    foreach(var module in root.GetComponentsInChildren<BossAttackModule>(true))
                    {
                        var so=new SerializedObject(module);if(so.FindProperty("beam").objectReferenceValue==null)continue;
                        so.FindProperty("beamTrackingSpeed").floatValue=.22f;so.FindProperty("beamSeconds").floatValue=2;so.ApplyModifiedPropertiesWithoutUndo();
                    }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return new{duplicateCuesRemoved=duplicateCues,curtainGap=.18f,rowInterval=.95f,beamTracking=.22f,beamSeconds=2};
    }
}
