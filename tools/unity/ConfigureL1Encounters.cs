using System;
using System.Linq;
using StarShift.Dev;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigureL1Encounters
{
    const string Folder = "Assets/StarShift/Data/Fixtures/Encounters";
    public static object Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty ||
            scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity")
            throw new InvalidOperationException("Open saved gameplay scene in Edit Mode first.");
        if (!AssetDatabase.IsValidFolder("Assets/StarShift/Data/Fixtures")) AssetDatabase.CreateFolder("Assets/StarShift/Data", "Fixtures");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/StarShift/Data/Fixtures", "Encounters");
        var a = LoadOrCreate("L1EncounterA", definition =>
        {
            definition.Id = "ENCOUNTER_1";
            definition.Completion = EncounterCompletion.Eliminate;
            definition.Groups = new[] { new ScoutSpawnGroup { EntryDuration = .9f } };
            definition.ShotSpacing = 1f;
            definition.VolleyPause = 1f;
        });
        var b = LoadOrCreate("L1EncounterB", ConfigureEncounterB);
        var enemies = GameObject.Find("Enemies");
        var sandbox = enemies.transform.Find("Scout_EntryHold");
        if (sandbox != null) sandbox.gameObject.SetActive(false);
        var root = GameObject.Find("L1Encounters") ?? new GameObject("L1Encounters");
        var runner = root.GetComponent<EncounterRunner>() ?? root.AddComponent<EncounterRunner>();
        if (root.GetComponent<DevFastStart>() == null) root.AddComponent<DevFastStart>();
        var settings = new SerializedObject(runner);
        var sequence = settings.FindProperty("encounters");
        sequence.arraySize = 2;
        sequence.GetArrayElementAtIndex(0).objectReferenceValue = a;
        sequence.GetArrayElementAtIndex(1).objectReferenceValue = b;
        settings.FindProperty("safetyGate").objectReferenceValue = enemies.GetComponent<EnemyFireGate>();
        settings.FindProperty("playerGate").objectReferenceValue = GameObject.Find("Player").GetComponent<PlayerGameplayGate>();
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return Inspect();
    }

    static void ConfigureEncounterB(EncounterDefinition definition)
    {
        definition.Id = "ENCOUNTER_2";
        definition.Completion = EncounterCompletion.Timeline;
        definition.Duration = 14f;
        definition.Groups = new[]
        {
            new ScoutSpawnGroup { Count = 3, Stagger = .25f, EntryAnchor = new Vector2(-.24f, .78f),
                HoldAnchor = new Vector2(.32f, .72f), SlotStep = new Vector2(.13f, .065f),
                EntryCurve = new Vector2(.1f, .12f), EntryDuration = 3.2f, HoldDuration = 3f,
                ExitAnchor = new Vector2(-.28f, .42f), ExitDuration = 7.5f },
            new ScoutSpawnGroup { Count = 3, StartTime = 0, Stagger = .25f,
                EntryAnchor = new Vector2(1.24f, .78f), HoldAnchor = new Vector2(.68f, .72f),
                SlotStep = new Vector2(-.13f, .065f), EntryCurve = new Vector2(-.1f, .12f),
                EntryDuration = 3.2f, HoldDuration = 3f, ExitAnchor = new Vector2(1.28f, .42f), ExitDuration = 7.5f }
        };
        definition.ShotsPerVolley = 2;
        definition.ShotSpacing = .55f;
        definition.VolleyPause = 1.8f;
    }

    public static object ApplyPlaytestFeedback()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before applying playtest tuning.");
        var a = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Folder + "/L1EncounterA.asset");
        var b = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Folder + "/L1EncounterB.asset");
        Undo.RecordObjects(new UnityEngine.Object[] { a, b }, "Synchronize encounter entry");
        a.Groups[0].EntryDuration = .9f;
        b.Duration = 14f;
        foreach (var group in b.Groups)
        {
            group.StartTime = 0;
            group.Stagger = .25f;
            group.EntryDuration = 3.2f;
            group.HoldDuration = 3f;
        }
        b.Groups[1].SlotStep = new Vector2(-.13f, .065f);
        EditorUtility.SetDirty(a); EditorUtility.SetDirty(b);
        const string path = "Assets/StarShift/Prefabs/Enemies/Scout.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var presentation = new SerializedObject(root.GetComponent<EnemyDamagePresentation>());
            presentation.FindProperty("damagedHealthFraction").floatValue = .5f;
            presentation.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        return Inspect();
    }

    static EncounterDefinition LoadOrCreate(string name, Action<EncounterDefinition> configure)
    {
        var path = Folder + "/" + name + ".asset";
        var definition = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(path);
        if (definition != null) return definition;
        definition = ScriptableObject.CreateInstance<EncounterDefinition>();
        definition.ScoutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/Enemies/Scout.prefab");
        configure(definition);
        if (!definition.IsValid(out var error)) throw new InvalidOperationException(error);
        AssetDatabase.CreateAsset(definition, path);
        return definition;
    }

    public static object Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        var nodes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var runner = UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        var settings = new SerializedObject(runner);
        int missingReferences = 0;
        foreach (var component in nodes.SelectMany(node => node.GetComponents<Component>()).Where(c => c != null))
        {
            var iterator = new SerializedObject(component).GetIterator();
            while (iterator.NextVisible(true))
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == null &&
                    iterator.objectReferenceEntityIdValue != default) missingReferences++;
        }
        return new { scene = scene.path, dirty = scene.isDirty,
            definitions = new[] { "L1EncounterA", "L1EncounterB" }.Select(name =>
            {
                var d = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(Folder + "/" + name + ".asset");
                return new { d.Id, completion = d.Completion.ToString(), count = d.EnemyCount,
                    d.Duration, d.ShotSpacing, d.ShotsPerVolley, d.VolleyPause, d.HandoffDelay,
                    valid = d.IsValid(out _) };
            }).ToArray(),
            runnerBound = settings.FindProperty("safetyGate").objectReferenceValue != null &&
                settings.FindProperty("playerGate").objectReferenceValue != null,
            sandboxActive = GameObject.Find("Enemies").transform.Find("Scout_EntryHold").gameObject.activeSelf,
            missingScripts = nodes.Sum(node => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject)),
            missingReferences };
    }
}
