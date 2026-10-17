using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarShift.Enemies;

public static class ConfigureScout
{
    public const string PrefabPath = "Assets/StarShift/Prefabs/Enemies/Scout.prefab";
    public const string DefinitionPath = "Assets/StarShift/Data/Enemies/Scout.asset";

    public static object Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty ||
            scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity")
            throw new InvalidOperationException("Open saved gameplay scene in Edit Mode first.");
        Folder("Assets/StarShift/Prefabs/Enemies");
        Folder("Assets/StarShift/Data/Enemies");
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var seed = new SerializedObject(definition);
            seed.FindProperty("maxHp").intValue = 4;
            seed.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            var root = new GameObject("Scout");
            try
            {
                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                var visual = Child(root, "Visual");
                var hull = Child(visual, "HullPlaceholder").AddComponent<SpriteRenderer>();
                hull.sprite = PlaceholderSprite();
                hull.color = new Color(.5f, .55f, .62f, 1);
                hull.sortingOrder = 5;
                var core = Child(visual, "CorePlaceholder").AddComponent<SpriteRenderer>();
                core.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/Placeholders/pulse-projectile-placeholder.png");
                if (core.sprite == null) throw new InvalidOperationException("Expected existing white projectile placeholder sprite.");
                core.color = new Color(.95f, .12f, .18f, 1);
                core.sortingOrder = 6;
                core.transform.localScale = new Vector3(2.4f, .3f, 1);
                core.transform.localPosition = new Vector3(0, -.05f, 0);
                var collision = Child(root, "Collision").AddComponent<BoxCollider2D>();
                collision.isTrigger = true;
                collision.size = new Vector2(1.28f, .8f);
                var health = root.AddComponent<EnemyHealth>();
                var healthSettings = new SerializedObject(health);
                healthSettings.FindProperty("definition").objectReferenceValue = definition;
                healthSettings.ApplyModifiedPropertiesWithoutUndo();
                var presentation = root.AddComponent<EnemyDamagePresentation>();
                var presentationSettings = new SerializedObject(presentation);
                presentationSettings.FindProperty("hull").objectReferenceValue = hull;
                presentationSettings.ApplyModifiedPropertiesWithoutUndo();
                var flight = root.AddComponent<EntryHoldFlight>();
                var flightSettings = new SerializedObject(flight);
                flightSettings.FindProperty("collisionShape").objectReferenceValue = collision;
                flightSettings.ApplyModifiedPropertiesWithoutUndo();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        var scout = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyHealth>(true))
            .FirstOrDefault(health => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(health.gameObject) == PrefabPath);
        if (scout == null)
        {
            var group = GameObject.Find("Enemies");
            if (group == null) group = new GameObject("Enemies");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "Scout_EntryHold";
            instance.transform.SetParent(group.transform, false);
            instance.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(.5f, 1.12f, 10));
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            scout = instance.GetComponent<EnemyHealth>();
        }
        var sceneFlight = new SerializedObject(scout.GetComponent<EntryHoldFlight>());
        sceneFlight.FindProperty("flightCamera").objectReferenceValue = Camera.main;
        sceneFlight.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = scout.gameObject;
        return Inspect();
    }

    public static object Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        var enemies = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyHealth>(true));
        return new { scene = scene.path, dirty = scene.isDirty, playing = Application.isPlaying,
            scouts = enemies.Select(health => new { name = health.name, active = health.gameObject.activeSelf,
                hp = health.CurrentHp, maxHp = health.MaxHp, alive = health.IsAlive,
                holding = health.GetComponent<EntryHoldFlight>().IsHolding,
                body = health.GetComponent<Rigidbody2D>().bodyType.ToString(),
                collisionBoundToRoot = health.GetComponentInChildren<BoxCollider2D>(true).attachedRigidbody == health.GetComponent<Rigidbody2D>(),
                definitionAssigned = new SerializedObject(health).FindProperty("definition").objectReferenceValue != null,
                presentationBound = new SerializedObject(health.GetComponent<EnemyDamagePresentation>()).FindProperty("hull").objectReferenceValue != null,
                flightEnvelopeBound = new SerializedObject(health.GetComponent<EntryHoldFlight>()).FindProperty("collisionShape").objectReferenceValue != null,
                spritesAssigned = health.GetComponentsInChildren<SpriteRenderer>(true).All(renderer => renderer.sprite != null),
                missingScripts = health.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
                components = health.GetComponents<Component>().Select(c => c.GetType().Name).ToArray(),
                hierarchy = health.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray(),
                prefab = Application.isPlaying ? "runtime instance" : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(health.gameObject) }).ToArray() };
    }

    static Sprite PlaceholderSprite()
    {
        const string path = "Assets/StarShift/Art/Placeholders/scout-placeholder.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;
        var vertices = new[] { new Vector2(.18f, .88f), new Vector2(.38f, .74f), new Vector2(.62f, .74f),
            new Vector2(.82f, .88f), new Vector2(.98f, .55f), new Vector2(.76f, .24f), new Vector2(.6f, .24f),
            new Vector2(.5f, .08f), new Vector2(.4f, .24f), new Vector2(.24f, .24f), new Vector2(.02f, .55f) };
        var texture = new Texture2D(128, 80, TextureFormat.RGBA32, false);
        var pixels = new Color32[128 * 80];
        for (int y = 0; y < 80; y++)
        for (int x = 0; x < 128; x++)
            if (Inside(new Vector2((x + .5f) / 128, (y + .5f) / 80), vertices)) pixels[y * 128 + x] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static bool Inside(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                inside = !inside;
        return inside;
    }

    static GameObject Child(GameObject parent, string name)
    {
        var value = new GameObject(name);
        value.transform.SetParent(parent.transform, false);
        return value;
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = path.Substring(0, path.LastIndexOf('/'));
        Folder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
}
