using System;
using System.Linq;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigureScoutFire
{
    const string ScoutPath = "Assets/StarShift/Prefabs/Enemies/Scout.prefab";
    const string ProjectilePath = "Assets/StarShift/Prefabs/Projectiles/EnemySingleProjectile.prefab";
    const string DefinitionPath = "Assets/StarShift/Data/Enemies/ScoutSingleFire.asset";

    public static object Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty ||
            scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity")
            throw new InvalidOperationException("Open saved gameplay scene in Edit Mode first.");
        var projectile = AssetDatabase.LoadAssetAtPath<EnemyProjectile>(ProjectilePath);
        if (projectile == null)
        {
            var root = new GameObject("EnemySingleProjectile");
            try
            {
                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                var hitbox = root.AddComponent<BoxCollider2D>();
                hitbox.isTrigger = true;
                hitbox.size = new Vector2(.16f, .24f);
                root.AddComponent<EnemyProjectile>();
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(1.3f, .5f, 1);
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/Placeholders/pulse-projectile-placeholder.png");
                if (renderer.sprite == null) throw new InvalidOperationException("Expected existing white projectile placeholder sprite.");
                renderer.color = new Color(1, .2f, .3f, 1);
                renderer.sortingOrder = 21;
                projectile = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePath).GetComponent<EnemyProjectile>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        var definition = AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<SingleFireDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            var settings = new SerializedObject(definition);
            settings.FindProperty("projectilePrefab").objectReferenceValue = projectile;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        var scout = PrefabUtility.LoadPrefabContents(ScoutPath);
        try
        {
            var muzzle = scout.transform.Find("Muzzle");
            if (muzzle == null)
            {
                muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(scout.transform, false);
                muzzle.localPosition = new Vector3(0, -.5f, 0);
                muzzle.localRotation = Quaternion.Euler(0, 0, 180);
            }
            var attack = scout.GetComponent<SingleFireAttack>();
            if (attack == null)
            {
                attack = scout.AddComponent<SingleFireAttack>();
                var settings = new SerializedObject(attack);
                settings.FindProperty("definition").objectReferenceValue = definition;
                settings.FindProperty("muzzle").objectReferenceValue = muzzle;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            if (scout.GetComponent<EnemyFirePresentation>() == null)
            {
                var presentation = scout.AddComponent<EnemyFirePresentation>();
                var settings = new SerializedObject(presentation);
                settings.FindProperty("core").objectReferenceValue = scout.transform.Find("Visual/CorePlaceholder").GetComponent<SpriteRenderer>();
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(scout, ScoutPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(scout); }
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerGameplayGate>();
        if (player == null) throw new InvalidOperationException("Expected scene Player.");
        foreach (var attack in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SingleFireAttack>(true)))
        {
            var settings = new SerializedObject(attack);
            settings.FindProperty("target").objectReferenceValue = player.transform;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(attack);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return Inspect();
    }

    public static object Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        var definition = AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(DefinitionPath);
        var projectile = AssetDatabase.LoadAssetAtPath<EnemyProjectile>(ProjectilePath);
        return new { scene = scene.path, dirty = scene.isDirty,
            definition = new { definition.InitialDelay, definition.TelegraphDuration, definition.Cooldown,
                definition.ProjectileSpeed, definition.ProjectileLifetime },
            projectile = new { body = projectile.GetComponent<Rigidbody2D>().bodyType.ToString(),
                trigger = projectile.GetComponent<BoxCollider2D>().isTrigger,
                sprite = projectile.GetComponentInChildren<SpriteRenderer>().sprite != null },
            scouts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SingleFireAttack>(true))
                .Select(attack => new { attack.name,
                    definitionBound = new SerializedObject(attack).FindProperty("definition").objectReferenceValue == definition,
                    muzzleBound = new SerializedObject(attack).FindProperty("muzzle").objectReferenceValue != null,
                    targetBound = new SerializedObject(attack).FindProperty("target").objectReferenceValue != null,
                    coreBound = new SerializedObject(attack.GetComponent<EnemyFirePresentation>()).FindProperty("core").objectReferenceValue != null,
                    components = attack.GetComponents<Component>().Select(c => c.GetType().Name).ToArray() }).ToArray(),
            missingScripts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)) };
    }
}
