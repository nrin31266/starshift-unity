using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Rewards;
using UnityEditor;
using UnityEngine;
public static class VerifyFlowPolishClosure
{
    public static object Inspect()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var missing=new List<string>();
        void InspectObject(UnityEngine.Object o)
        {
            if(o==null)return;
            var so=new SerializedObject(o);var it=so.GetIterator();
            while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue==null&&!it.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))missing.Add(o.name+"/"+it.propertyPath);
        }
        void InspectTree(GameObject root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)missing.Add(t.name+"/MissingScript");
                foreach(var c in t.GetComponents<Component>())InspectObject(c);
            }
        }
        var path="Assets/StarShift/";
        foreach(var guid in AssetDatabase.FindAssets("t:ScriptableObject",new[]{path+"Data"}))InspectObject(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{path+"Prefabs"}))InspectTree(StarShift.Editor.RegionAAssetPaths.Load<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())InspectTree(root);
        var levels=Enumerable.Range(1,4).Select(i=>StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>(path+"Data/RegionA/Mission0"+i+".asset")).ToArray();
        foreach(var level in levels)if(!level.IsValid(out var e))throw new InvalidOperationException(e);
        if(levels.SelectMany(l=>l.Encounters).Any(e=>e.ActiveShooterLimit>3))throw new InvalidOperationException("Planet 1 shooter cap");
        var dev=UnityEngine.Object.FindAnyObjectByType<StarShift.Dev.DevFastStart>();var state=new SerializedObject(dev);
        if(state.FindProperty("devModeEnabled").boolValue)throw new InvalidOperationException("Dev mode saved enabled");
        if(missing.Count>0)throw new InvalidOperationException(string.Join(",",missing));
        var result=new{status="PASS",missingReferences=missing.Count,productionLevels=levels.Length,hpMultipliers=levels.Select(l=>l.EnemyHealthMultiplier).ToArray(),introEnabled=levels[0].Intro.Enabled,devModeEnabled=false,fixtureGuid=AssetDatabase.AssetPathToGUID(path+"Data/Fixtures/Levels/M4SystemTestbed.asset")};
        File.WriteAllText("../.design/m5-flow-polish/closure.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
