using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarShift.Gameplay;
using StarShift.Player;

public static class ConfigureGameplayView
{
    public static object Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        int missingScripts = 0;
        int missingReferences = 0;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            foreach (var component in child.GetComponents<MonoBehaviour>())
            {
                if (component == null) continue;
                var properties = new SerializedObject(component).GetIterator();
                while (properties.Next(true))
                    if (properties.propertyType == SerializedPropertyType.ObjectReference &&
                        properties.objectReferenceValue == null && properties.objectReferenceEntityIdValue != default)
                        missingReferences++;
            }
        }
        var player = GameObject.Find("Player");
        var movement = new SerializedObject(player.GetComponent<RelativeDragMovement>());
        var weapon = new SerializedObject(player.GetComponent<PulseLaserWeapon>());
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/Player/Player.prefab");
        var prefabWeapon = new SerializedObject(prefab.GetComponent<PulseLaserWeapon>());
        var camera = Camera.main;
        Selection.activeGameObject = camera.gameObject;
        var sceneView = EditorWindow.GetWindow<SceneView>();
        sceneView.drawGizmos = true;
        sceneView.LookAt(Vector3.zero, Quaternion.identity, 12, true, true);
        sceneView.Repaint();
        return new { scene = scene.path, dirty = scene.isDirty, playing = Application.isPlaying,
            missingScripts, missingReferences,
            movementCameraMatches = movement.FindProperty("movementCamera").objectReferenceValue == camera,
            sharedInput = movement.FindProperty("pointerInput").objectReferenceValue == weapon.FindProperty("pointerInput").objectReferenceValue,
            prefabInputValid = prefabWeapon.FindProperty("pointerInput").objectReferenceValue == prefab.GetComponent<PlayerPointerInput>(),
            cameraSize = camera.orthographicSize, target = camera.GetComponent<GameplayViewController>().TargetView,
            prefabConnected = Application.isPlaying ? "runtime instance" : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player) };
    }

    public static object Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty ||
            scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity")
            throw new InvalidOperationException("Open the saved gameplay scene in Edit Mode first.");
        var camera = Camera.main;
        if (camera == null || !camera.orthographic) throw new InvalidOperationException("Expected orthographic Main Camera.");
        var controller = camera.GetComponent<GameplayViewController>();
        if (controller == null) controller = Undo.AddComponent<GameplayViewController>(camera.gameObject);
        var serialized = new SerializedObject(controller);
        var target = serialized.FindProperty("targetView");
        target.FindPropertyRelative("orthographicSize").floatValue = GameplayViewSettings.BaselineSize;
        target.FindPropertyRelative("playableViewport").rectValue = new Rect(0, 0, 1, 1);
        serialized.FindProperty("transitionDuration").floatValue = .6f;
        serialized.ApplyModifiedProperties();
        Undo.RecordObject(camera, "Set gameplay view baseline");
        camera.orthographicSize = GameplayViewSettings.BaselineSize;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = camera.gameObject;
        return new { scene = scene.path, controller = controller.GetType().Name,
            size = camera.orthographicSize, region = "(0, 0, 1, 1)", transitionSeconds = .6f };
    }
}
