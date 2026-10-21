using System;
using System.Linq;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class InspectRegionA
{
 public static object Inspect()
 {
  var levels=Enumerable.Range(1,4).Select(n=>StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission0"+n+".asset")).ToArray();
  foreach(var level in levels)if(level==null||!level.IsValid(out _))throw new InvalidOperationException("Invalid production level");
  var scene=SceneManager.GetActiveScene();var nodes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/StarShift/Prefabs/Enemies"}).Select(AssetDatabase.GUIDToAssetPath).Select(StarShift.Editor.RegionAAssetPaths.Load<GameObject>).ToArray();
  var all=nodes.Concat(prefabs.SelectMany(p=>p.GetComponentsInChildren<Transform>(true))).ToArray();
  int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),references=0;
  foreach(var c in all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null)){
   var it=new SerializedObject(c).GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue==null&&it.objectReferenceEntityIdValue!=default)references++;
  }
  if(missing!=0||references!=0)throw new InvalidOperationException("Missing scripts/references: "+missing+"/"+references);
  var fixture=StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/Fixtures/Levels/EncounterSystemsFixture.asset");
  return new{pass=true,missions=levels.Length,encounters=levels.Sum(l=>l.Encounters.Length),missingScripts=missing,missingReferences=references,fixtureId=fixture.Id,fixtureEncounters=fixture.Encounters.Length,devMode=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<StarShift.Dev.DevFastStart>()).FindProperty("devModeEnabled").boolValue};
 }
}
