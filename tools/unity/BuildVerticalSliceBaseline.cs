using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildVerticalSliceBaseline
{
    const string ScenePath = "Assets/StarShift/Scenes/02_Gameplay.unity";
    const string ArtPath = "Assets/StarShift/Art/Placeholders";
    const string PrefabPath = "Assets/StarShift/Prefabs/Player/Player.prefab";

    public static string CheckpointA()
    {
        RequireEditMode();
        var source = SceneManager.GetActiveScene();
        if (source.path != "Assets/Scenes/SampleScene.unity" || source.isDirty)
            throw new InvalidOperationException("Open the clean SampleScene before creating the foundation.");
        if (File.Exists(ScenePath))
            throw new InvalidOperationException("Gameplay scene already exists; inspect it instead of rebuilding.");
        Folder(ArtPath);
        var hull = SpriteAsset("player-placeholder", 64, 80, 80,
            new[] { new Vector2(.5f,.97f), new Vector2(.93f,.15f),
                new Vector2(.5f,.31f), new Vector2(.07f,.15f) });
        var rock = SpriteAsset("asteroid-placeholder", 64, 64, 64,
            new[] { new Vector2(.26f,.95f), new Vector2(.75f,.86f),
                new Vector2(.97f,.52f), new Vector2(.72f,.08f),
                new Vector2(.23f,.04f), new Vector2(.03f,.43f) });
        if (!EditorSceneManager.SaveScene(source, ScenePath, true))
            throw new InvalidOperationException("Could not save the scene copy.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var camera = Camera.main;
        camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.018f, .028f, .065f, 1);
        camera.nearClipPlane = .3f;
        camera.farClipPlane = 100;
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, -3.2f, 0);
        var visual = Child(player, "Visual");
        Render(Child(visual, "HullPlaceholder"), hull, new Color(.12f,.8f,1), 10);
        var environment = new GameObject("EnvironmentPlaceholders");
        var left = Child(environment, "AsteroidLeft");
        left.transform.localPosition = new Vector3(-5, 1.4f, 0);
        left.transform.localScale = Vector3.one * 1.15f;
        left.transform.localRotation = Quaternion.Euler(0, 0, 18);
        Render(left, rock, new Color(.24f,.29f,.34f), -10);
        var right = Child(environment, "AsteroidRight");
        right.transform.localPosition = new Vector3(5.5f, 2.7f, 0);
        right.transform.localScale = Vector3.one * .75f;
        right.transform.localRotation = Quaternion.Euler(0, 0, -27);
        Render(right, rock, new Color(.3f,.33f,.37f), -10);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.identity, 7, true, true);
        return "Checkpoint A saved: camera, Global Light 2D, Player/Visual/HullPlaceholder, EnvironmentPlaceholders with two decorative asteroids.";
    }

    public static string CheckpointB()
    {
        RequireEditMode();
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open the saved gameplay foundation first.");
        if (File.Exists(PrefabPath))
            throw new InvalidOperationException("Player prefab already exists; inspect it instead of overwriting.");
        var player = scene.GetRootGameObjects().Single(x => x.name == "Player");
        if (player.GetComponent<Rigidbody2D>() != null || player.transform.Find("Collision") != null)
            throw new InvalidOperationException("Player already has physics; inspect before changing.");
        Folder("Assets/StarShift/Prefabs/Player");
        var body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var collision = Child(player, "Collision").AddComponent<CircleCollider2D>();
        collision.isTrigger = true;
        collision.radius = .2f;
        collision.offset = new Vector2(0, -.04f);
        var spawn = player.transform.position;
        // Prefab ở origin; vị trí xuất hiện thuộc scene instance.
        player.transform.position = Vector3.zero;
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(player, PrefabPath, InteractionMode.AutomatedAction);
        if (prefab == null) throw new InvalidOperationException("Player prefab save failed.");
        player.transform.position = spawn;
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player;
        return "Checkpoint B saved: connected Player prefab; Kinematic Rigidbody2D on root; separate Visual and Collision trigger; no gameplay scripts.";
    }

    public static object Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        var player = scene.GetRootGameObjects().Single(x => x.name == "Player");
        var renderer = player.GetComponentInChildren<SpriteRenderer>();
        var collider = player.GetComponentInChildren<CircleCollider2D>();
        var body = player.GetComponent<Rigidbody2D>();
        return new {
            scene = scene.path, dirty = scene.isDirty,
            playing = Application.isPlaying, frame = Time.frameCount,
            cameraSize = Camera.main.orthographicSize, aspect = Camera.main.aspect,
            playerPosition = player.transform.position.ToString("F3"),
            visualBounds = renderer.bounds.ToString(), sprite = AssetDatabase.GetAssetPath(renderer.sprite),
            prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player),
            prefabStatus = PrefabUtility.GetPrefabInstanceStatus(player).ToString(),
            rigidbody = body == null ? "none" : body.bodyType.ToString(),
            trigger = collider != null && collider.isTrigger,
            colliderBodyIsRoot = collider != null && collider.attachedRigidbody == body,
            missingScripts = player.GetComponentsInChildren<Transform>(true)
                .Sum(x => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)),
            hierarchy = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .Select(x => new { path = PathOf(x), components = x.GetComponents<Component>()
                    .Select(c => c == null ? "MISSING" : c.GetType().Name).ToArray() }).ToArray()
        };
    }

    static string PathOf(Transform value)
    {
        return value.parent == null ? value.name : PathOf(value.parent) + "/" + value.name;
    }

    static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before authoring assets.");
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = path.Substring(0, path.LastIndexOf('/'));
        Folder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    static GameObject Child(GameObject parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    static void Render(GameObject target, Sprite sprite, Color color, int order)
    {
        var renderer = target.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
    }

    static Sprite SpriteAsset(string name, int width, int height, float pixelsPerUnit, Vector2[] polygon)
    {
        var path = ArtPath + "/" + name + ".png";
        if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite " + path);
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var point = new Vector2((x + .5f) / width, (y + .5f) / height);
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i]; var b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x-a.x) * (point.y-a.y) / (b.y-a.y) + a.x)
                    inside = !inside;
            }
            pixels[y * width + x] = inside ? new Color32(255,255,255,255) : new Color32(0,0,0,0);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
