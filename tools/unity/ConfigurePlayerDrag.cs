using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarShift.Player;

public static class ConfigurePlayerDrag
{
    public static string Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before wiring Player.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity")
            throw new InvalidOperationException("Open 02_Gameplay first.");
        var root = PrefabUtility.LoadPrefabContents("Assets/StarShift/Prefabs/Player/Player.prefab");
        try
        {
            var input = root.GetComponent<InputSystemPointerInput>();
            if (input == null) input = root.AddComponent<InputSystemPointerInput>();
            var movement = root.GetComponent<RelativeDragMovement>();
            if (movement == null) movement = root.AddComponent<RelativeDragMovement>();
            var settings = new SerializedObject(movement);
            settings.FindProperty("pointerInput").objectReferenceValue = input;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/StarShift/Prefabs/Player/Player.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var player = GameObject.Find("Player");
        var instance = player.GetComponent<RelativeDragMovement>();
        Undo.RecordObject(instance, "Assign gameplay movement camera");
        var instanceSettings = new SerializedObject(instance);
        instanceSettings.FindProperty("movementCamera").objectReferenceValue = Camera.main;
        instanceSettings.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player;
        return "Player prefab wired with InputSystemPointerInput + RelativeDragMovement; scene camera assigned; existing transforms retained.";
    }
}
