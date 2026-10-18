using System;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigurePersistence
{
    public static object Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var runner = UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        var scene = runner.gameObject.scene;
        if (scene.isDirty) throw new InvalidOperationException("Save/reconcile existing scene edits first.");
        var owner = runner.GetComponent<CampaignLifecycle>() ?? runner.gameObject.AddComponent<CampaignLifecycle>();
        var data = new SerializedObject(owner);
        data.FindProperty("runner").objectReferenceValue = runner;
        data.FindProperty("session").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<GameplaySession>();
        data.FindProperty("player").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<PlayerDamageLifecycle>();
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return Inspect();
    }
    public static object Inspect()
    {
        int missingScripts = 0, missingReferences = 0;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            foreach (var component in t.GetComponents<Component>())
            {
                if (component == null) continue;
                var p = new SerializedObject(component).GetIterator();
                while (p.NextVisible(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && p.objectReferenceEntityIdValue != default) missingReferences++;
            }
        }
        return new { missingScripts, missingReferences, sceneDirty = scene.isDirty,
            lifecycle = UnityEngine.Object.FindAnyObjectByType<CampaignLifecycle>() != null };
    }
}
