using System;
using System.Linq;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigurePlayerDamageLifecycle
{
    static readonly string[] LayerNames = { "Player", "Enemy", "PlayerProjectile", "EnemyProjectile" };
    public static object Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.isDirty || scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity")
            throw new InvalidOperationException("Open saved gameplay scene in Edit Mode first.");
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        for (int i = 0; i < LayerNames.Length; i++)
        {
            var slot = layers.GetArrayElementAtIndex(8 + i);
            if (slot.stringValue != "" && slot.stringValue != LayerNames[i])
                throw new InvalidOperationException("Refusing to replace occupied layer " + (8 + i));
            slot.stringValue = LayerNames[i];
        }
        tags.ApplyModifiedPropertiesWithoutUndo();
        for (int layer = 8; layer <= 11; layer++)
        for (int other = 0; other < 32; other++)
            Physics2D.IgnoreLayerCollision(layer, other, !Allowed(layer, other));
        EditorUtility.SetDirty(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]);
        var group = GameObject.Find("Enemies");
        if (group == null) throw new InvalidOperationException("Expected Enemies scene group.");
        var fireGate = group.GetComponent<EnemyFireGate>();
        if (fireGate == null) fireGate = group.AddComponent<EnemyFireGate>();
        ConfigurePrefab("Assets/StarShift/Prefabs/Player/Player.prefab", 8, root =>
        {
            Add<PlayerLives>(root);
            var entrance = Add<PlayerRespawnEntrance>(root);
            var settings = new SerializedObject(entrance);
            settings.FindProperty("collisionShape").objectReferenceValue = root.GetComponentInChildren<CircleCollider2D>(true);
            settings.FindProperty("blockingLayers").intValue = 1 << 9;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Add<PlayerDamageLifecycle>(root);
            Add<PlayerContactReceiver>(root);
            var presentation = Add<PlayerDamagePresentation>(root);
            settings = new SerializedObject(presentation);
            settings.FindProperty("hull").objectReferenceValue = root.GetComponentInChildren<SpriteRenderer>(true);
            settings.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<Rigidbody2D>().useFullKinematicContacts = true;
        });
        ConfigurePrefab("Assets/StarShift/Prefabs/Enemies/Scout.prefab", 9, root =>
            root.GetComponent<Rigidbody2D>().useFullKinematicContacts = true);
        ConfigurePrefab("Assets/StarShift/Prefabs/Projectiles/PlayerPulseProjectile.prefab", 10, root => {});
        ConfigurePrefab("Assets/StarShift/Prefabs/Projectiles/EnemySingleProjectile.prefab", 11, root => {});
        foreach (var lifecycle in UnityEngine.Object.FindObjectsByType<PlayerDamageLifecycle>())
        {
            var settings = new SerializedObject(lifecycle);
            settings.FindProperty("enemyFireGate").objectReferenceValue = fireGate;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(lifecycle);
        }
        foreach (var attack in UnityEngine.Object.FindObjectsByType<SingleFireAttack>())
        {
            var settings = new SerializedObject(attack);
            settings.FindProperty("fireGate").objectReferenceValue = fireGate;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(attack);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return Inspect();
    }

    static bool Allowed(int a, int b)
        => (a == 8 && (b == 9 || b == 11)) || (b == 8 && (a == 9 || a == 11)) ||
           (a == 9 && b == 10) || (a == 10 && b == 9);

    static T Add<T>(GameObject root) where T : Component
        => root.GetComponent<T>() ?? root.AddComponent<T>();

    static void ConfigurePrefab(string path, int layer, Action<GameObject> configure)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var node in root.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = layer;
            configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static object Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        var nodes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var gate = UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();
        var player = GameObject.Find("Player");
        var lifecycle = player.GetComponent<PlayerDamageLifecycle>();
        var bindings = new SerializedObject(lifecycle);
        return new { scene = scene.path, dirty = scene.isDirty, playing = Application.isPlaying,
            layers = Enumerable.Range(8, 4).Select(i => new { index = i, name = LayerMask.LayerToName(i) }).ToArray(),
            matrixMatches = Enumerable.Range(8, 4).All(a => Enumerable.Range(0, 32).All(b => Physics2D.GetIgnoreLayerCollision(a, b) != Allowed(a,b))),
            playerComponents = player.GetComponents<Component>().Select(c => c.GetType().Name).ToArray(),
            playerLayers = player.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject.layer).Distinct().ToArray(),
            scoutLayers = nodes.Select(t => t.GetComponent<EnemyHealth>()).Where(h => h != null)
                .SelectMany(h => h.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject.layer).Distinct().ToArray(),
            fireAuthorityBound = bindings.FindProperty("enemyFireGate").objectReferenceValue == gate,
            deathDuration = bindings.FindProperty("deathDuration").floatValue,
            invincibility = bindings.FindProperty("invincibilityDuration").floatValue,
            grace = bindings.FindProperty("enemyFireGrace").floatValue,
            missingScripts = nodes.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
            projectileLayers = new[] { "PlayerPulseProjectile", "EnemySingleProjectile" }.Select(name =>
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/Projectiles/" + name + ".prefab").layer).ToArray() };
    }
}
