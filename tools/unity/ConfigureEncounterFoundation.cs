using System;
using System.Linq;
using System.IO;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.UI;
using StarShift.Dev;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ConfigureEncounterFoundation
{
    const string Data = "Assets/StarShift/Data";
    static void Bind(UnityEngine.Object target, string field, UnityEngine.Object reference)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = reference; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static T Asset<T>(string path, Action<T> create) where T : ScriptableObject
    {
        var item = AssetDatabase.LoadAssetAtPath<T>(path);
        if (item != null) return item;
        item = ScriptableObject.CreateInstance<T>(); create(item); AssetDatabase.CreateAsset(item,path); return item;
    }
    static T Add<T>(GameObject root) where T : Component => root.GetComponent<T>() ?? root.AddComponent<T>();
    public static object Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity") throw new InvalidOperationException("Saved gameplay scene required.");
        foreach (var folder in new[] { "Levels", "Flights", "Cameras" })
            if (!AssetDatabase.IsValidFolder(Data+"/"+folder)) AssetDatabase.CreateFolder(Data,folder);
        var baseline = Asset<CameraProfile>(Data+"/Cameras/Normal.asset", x => x.View=GameplayViewSettings.Baseline);
        Asset<CameraProfile>(Data+"/Cameras/Wide.asset", x => x.View=new GameplayViewSettings(7.2f,new Rect(0,0,1,1)));
        Asset<CameraProfile>(Data+"/Cameras/LowerRegion.asset", x => x.View=new GameplayViewSettings(6,new Rect(0,0,1,.7f)));
        var a = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Data+"/Encounters/L1EncounterA.asset");
        var b = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Data+"/Encounters/L1EncounterB.asset");
        foreach (var encounter in new[] { a,b })
        {
            for (int i=0;i<encounter.Groups.Length;i++)
            {
                var g=encounter.Groups[i];
                if (g.Sequence!=null) continue;
                var sequence = Asset<FlightSequence>(Data+"/Flights/"+(encounter==a?"L1A":"L1B")+i+".asset", x =>
                {
                    var entry = new FlightPhase { Kind=FlightPhaseKind.Entry, Path=g.EntryCurve.x<0?FlightPath.ArcLeft:FlightPath.ArcRight,
                        From=g.EntryAnchor,To=g.HoldAnchor,Curve=g.EntryCurve,Duration=g.EntryDuration };
                    var hold = new FlightPhase { Kind=FlightPhaseKind.Hold,From=g.HoldAnchor,To=g.HoldAnchor,
                        Duration=encounter==a?1:g.HoldDuration,CanShoot=true,ClampToView=true,HoldUntilResolved=encounter==a };
                    var exit = new FlightPhase { Kind=FlightPhaseKind.Exit,From=g.HoldAnchor,To=g.ExitAnchor,
                        Duration=g.ExitDuration,CanShoot=true };
                    x.Phases=encounter==a?new[] { entry,hold }:new[] { entry,hold,exit };
                });
                g.Sequence=sequence;
                g.Required=true;
                g.EnemyDefinition=encounter.ScoutPrefab.GetComponent<EnemyHealth>().Definition;
            }
            EditorUtility.SetDirty(encounter);
        }
        var level = Asset<LevelDefinition>(Data+"/Levels/L1.asset", x =>
        { x.Camera=baseline; x.Encounters=new[] { a,b }; });
        var root=GameObject.Find("L1Encounters");
        Add<ScenePrefabPool>(root); Add<EncounterCleanup>(root);
        var flow=Add<LevelFlow>(root);
        Add<EncounterDebugView>(root);
        var camera=Camera.main;
        var player=GameObject.Find("Player").GetComponent<PlayerGameplayGate>();
        var gate=UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();
        var presentationRoot=GameObject.Find("MissionApproach") ?? new GameObject("MissionApproach");
        var presentation=Add<MissionIntroPresentation>(presentationRoot);
        var sprite=CreateDisc();
        var planet=presentationRoot.transform.Find("Planet");
        if (planet==null)
        {
            planet=new GameObject("Planet").transform; planet.SetParent(presentationRoot.transform,false);
            var renderer=planet.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite=sprite;
            renderer.color=new Color(.12f,.46f,.62f,1); renderer.sortingOrder=-5;
        }
        var stars=presentationRoot.transform.Find("ApproachStars");
        if (stars==null)
        {
            stars=new GameObject("ApproachStars").transform; stars.SetParent(presentationRoot.transform,false);
            var random=new System.Random(713);
            for (int i=0;i<48;i++)
            {
                var star=new GameObject("Star").AddComponent<SpriteRenderer>(); star.transform.SetParent(stars,false);
                star.sprite=sprite; star.color=new Color(.5f,.75f,.9f,.7f); star.sortingOrder=-6;
                star.transform.localPosition=new Vector3((float)random.NextDouble()*32-16,(float)random.NextDouble()*24-8,0);
                star.transform.localScale=new Vector3(.025f,.04f+(float)random.NextDouble()*.1f,1);
            }
        }
        Bind(presentation,"planet",planet.GetComponent<SpriteRenderer>()); Bind(presentation,"stars",stars); Bind(presentation,"gameplayCamera",camera);
        presentation.Hide();
        Bind(flow,"level",level); Bind(flow,"view",camera.GetComponent<GameplayViewController>()); Bind(flow,"player",player);
        Bind(flow,"enemyFireGate",gate); Bind(flow,"presentation",presentation);
        var runner=root.GetComponent<EncounterRunner>();
        Bind(runner,"levelFlow",flow);
        var runnerSettings=new SerializedObject(runner);
        runnerSettings.FindProperty("encounters").arraySize=0;
        runnerSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return Inspect();
    }
    static Sprite CreateDisc()
    {
        const string path="Assets/StarShift/Art/Placeholders/planet-disc-placeholder.png";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite!=null) return sprite;
        var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);
        for(int y=0;y<128;y++) for(int x=0;x<128;x++)
        {
            var p=new Vector2((x-63.5f)/62,(y-63.5f)/62); var radius=p.magnitude;
            var light=Mathf.Clamp01(.75f-.3f*p.x+.2f*p.y);
            texture.SetPixel(x,y,new Color(light,light,light,Mathf.Clamp01((1-radius)*64)));
        }
        File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;
        importer.spritePixelsPerUnit=64; importer.alphaIsTransparency=true; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static object Inspect()
    {
        var scene=SceneManager.GetActiveScene();
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        int missing=0;
        foreach(var c in all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null))
        {
            var p=new SerializedObject(c).GetIterator();
            while(p.NextVisible(true)) if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue==null && p.objectReferenceEntityIdValue!=default) missing++;
        }
        var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>(Data+"/Levels/L1.asset");
        return new { valid=level.IsValid(out var error), error, level.Id, level.LocationName,
            encounters=level.Encounters.Select(e=>new {e.Id,e.EnemyCount, sequences=e.Groups.All(g=>g.Sequence!=null)}).ToArray(),
            missingReferences=missing,missingScripts=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),scene.isDirty };
    }
}
