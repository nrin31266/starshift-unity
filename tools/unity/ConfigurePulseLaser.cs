using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarShift.Combat;
using StarShift.Player;

public static class ConfigurePulseLaser
{
    const string ProjectilePath = "Assets/StarShift/Prefabs/Projectiles/PlayerPulseProjectile.prefab";
    const string DefinitionPath = "Assets/StarShift/Data/Weapons/PulseLaserLv1.asset";

    public static string Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before authoring.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity")
            throw new InvalidOperationException("Open 02_Gameplay first.");
        Folder("Assets/StarShift/Prefabs/Projectiles");
        Folder("Assets/StarShift/Data/Weapons");
        var projectile = AssetDatabase.LoadAssetAtPath<PlayerProjectile>(ProjectilePath);
        if (projectile == null)
        {
            var root = new GameObject("PlayerPulseProjectile");
            try
            {
                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                var collider = root.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(.1f, .4f);
                root.AddComponent<PlayerProjectile>();
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = PlaceholderSprite();
                renderer.color = new Color(.1f, .85f, 1f, 1f);
                renderer.sortingOrder = 20;
                var saved = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePath);
                projectile = saved.GetComponent<PlayerProjectile>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        var definition = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<PulseLaserDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }
        var definitionSettings = new SerializedObject(definition);
        definitionSettings.FindProperty("projectilePrefab").objectReferenceValue = projectile;
        definitionSettings.ApplyModifiedPropertiesWithoutUndo();

        var player = PrefabUtility.LoadPrefabContents("Assets/StarShift/Prefabs/Player/Player.prefab");
        try
        {
            if (player.GetComponent<PlayerGameplayGate>() == null) player.AddComponent<PlayerGameplayGate>();
            var muzzle = player.transform.Find("Muzzle");
            if (muzzle == null)
            {
                muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(player.transform, false);
                muzzle.localPosition = new Vector3(0, .75f, 0);
            }
            var weapon = player.GetComponent<PulseLaserWeapon>();
            if (weapon == null) weapon = player.AddComponent<PulseLaserWeapon>();
            var weaponSettings = new SerializedObject(weapon);
            weaponSettings.FindProperty("definition").objectReferenceValue = definition;
            weaponSettings.FindProperty("pointerInput").objectReferenceValue = player.GetComponent<InputSystemPointerInput>();
            weaponSettings.FindProperty("muzzle").objectReferenceValue = muzzle;
            weaponSettings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, "Assets/StarShift/Prefabs/Player/Player.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = GameObject.Find("Player");
        return "Pulse Laser definition, cyan projectile prefab and Player gate/weapon/Muzzle saved; balance defaults read from definition, no gameplay probe saved.";
    }

    static Sprite PlaceholderSprite()
    {
        const string path = "Assets/StarShift/Art/Placeholders/pulse-projectile-placeholder.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        var texture = new Texture2D(16, 64, TextureFormat.RGBA32, false);
        var pixels = new Color32[16 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = path.Substring(0, path.LastIndexOf('/'));
        Folder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
}
