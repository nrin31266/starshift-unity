using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarShift.Player;

public static class ConfigurePlayerBounds
{
    public static string Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before wiring bounds.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved gameplay scene first.");
        var root = PrefabUtility.LoadPrefabContents("Assets/StarShift/Prefabs/Player/Player.prefab");
        try
        {
            if (root.GetComponent<PlayerMovementBounds>() == null)
                root.AddComponent<PlayerMovementBounds>();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/StarShift/Prefabs/Player/Player.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = GameObject.Find("Player");
        return "PlayerMovementBounds added to Player prefab; footprint 0.8 x 1.0 local units; scene transforms and input wiring retained.";
    }
}
