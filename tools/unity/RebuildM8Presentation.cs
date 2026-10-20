using System;
using System.Linq;
using StarShift.Player;
using StarShift.Enemies;
using StarShift.Rewards;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RebuildM8Presentation
{
    const string Art = "Assets/StarShift/Art/M8/";
    static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(name).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void SetArray(UnityEngine.Object target, string name, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(name);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Sprite[] Frames(string id) => AssetDatabase.LoadAllAssetsAtPath(Art + id + "-sheet.png").OfType<Sprite>().OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
    static SpriteRenderer Visual(GameObject root, string name, Sprite sprite, int order)
    {
        var child = root.transform.Find(name);
        if (child == null) { child = new GameObject(name).transform; child.SetParent(root.transform, false); }
        var renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.sortingOrder = order; renderer.enabled = false;
        return renderer;
    }
    static void Player(GameObject root)
    {
        var appearance = root.GetComponent<ShipAppearance>();
        if (appearance == null) return;
        var so = new SerializedObject(appearance);
        var primary = (TrailRenderer)so.FindProperty("movementTrail").objectReferenceValue;
        foreach (var trail in root.GetComponentsInChildren<TrailRenderer>(true))
            if (trail.name.StartsWith("CosmeticEngineTrail") && trail != primary) UnityEngine.Object.DestroyImmediate(trail.gameObject);
        if (primary != null)
        {
            primary.name = "MovementTrail"; primary.startWidth = .07f; primary.endWidth = 0; primary.time = .12f;
            primary.emitting = false; primary.Clear();
        }
        var old = root.transform.Find("M8Debris");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var death = root.GetComponent<PlayerDeathPresentation>();
        if (death == null) death = root.AddComponent<PlayerDeathPresentation>();
        var frames = Frames("explosion");
        if (frames.Length != 8) throw new Exception("Expected 8 common explosion frames");
        Set(death, "explosion", Visual(root, "DeathExplosion", frames[0], 18));
        SetArray(death, "explosionFrames", frames);
        var debris = Enumerable.Range(0, 4).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(Art + "player-fragment-" + i + ".png")).ToArray();
        if (debris.Any(sprite => sprite == null)) throw new Exception("Missing armor fragments");
        SetArray(death, "fragments", Enumerable.Range(0, 4).Select(i => (UnityEngine.Object)Visual(root, "DeathFragment" + i, debris[i], 19)).ToArray());
        EditorUtility.SetDirty(appearance); EditorUtility.SetDirty(death);
    }
    public static object BindPlayer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var path = "Assets/StarShift/Prefabs/Player/Player.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try { Player(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        var root = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().First(x => x.name == "Player");
        Player(root);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene); EditorSceneManager.SaveOpenScenes();
        return "One small movement trail; independent death explosion/fragments; hull does not blink during protection";
    }
    public static object BindFleetAndHeat()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/StarShift/Prefabs/Enemies"}))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer == null) continue;
                    if (!renderer.name.StartsWith("M8Engine") && !renderer.name.StartsWith("EngineGlow") && renderer.name != "EnginePlume") continue;
                    if (renderer.GetComponentsInChildren<Collider2D>(true).Length > 0) throw new Exception("Collider on plume " + path);
                    UnityEngine.Object.DestroyImmediate(renderer.gameObject);
                }
                var asteroid = root.GetComponent<AsteroidVisualMotion>();
                if (root.name == "VanguardFrigate")
                    foreach (var cannon in root.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name.StartsWith("CannonPod")))
                        cannon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "vanguard-beam-pod.png");
                if (asteroid != null)
                {
                    foreach (var pair in new[]{new[]{"sizeSprites",""},new[]{"damagedSizeSprites","-damaged"},new[]{"criticalSizeSprites","-critical"}})
                        SetArray(asteroid, pair[0], new[]{"small","medium","large","huge"}.Select(size => (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Sprite>(Art+"asteroid-"+size+pair[1]+".png")).ToArray());
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var guid in AssetDatabase.FindAssets("t:PickupDefinition", new[]{"Assets/StarShift/Data"}))
        {
            var item = AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            var id = item.Kind == PickupKind.Shard ? "stellar-shard" : item.Kind == PickupKind.ExtraLife ? "extra-life" : item.Kind == PickupKind.Shield ? "shield-cell" : item.Kind == PickupKind.Overdrive ? "overdrive" : item.Kind == PickupKind.SupportDrone ? "support-drone" : item.Kind == PickupKind.MissileAmmo ? "missile-ammo" : item.Branch == StarShift.Combat.WeaponBranch.Pulse ? "pulse-core" : item.Branch == StarShift.Combat.WeaponBranch.Plasma ? "plasma-core" : "homing-core";
            item.Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + id + ".png");
            item.AnimationFrames = Frames(id); item.Color = Color.white;
            EditorUtility.SetDirty(item);
        }
        var hud = UnityEngine.Object.FindAnyObjectByType<StarShift.UI.GameplayHud>(FindObjectsInactive.Include);
        var so = new SerializedObject(hud);
        var frame = (Image)so.FindProperty("heatFrame").objectReferenceValue;
        var fill = (Image)so.FindProperty("heatFill").objectReferenceValue;
        frame.rectTransform.sizeDelta = new Vector2(256, 18);
        fill.transform.SetParent(frame.transform, false);
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = new Vector2(4, 4); fill.rectTransform.offsetMax = new Vector2(-4, -4);
        fill.type = Image.Type.Simple;
        foreach (var image in frame.GetComponentsInChildren<Image>(true))
            if (image != frame && image != fill && !image.name.Contains("Notch")) image.enabled = false;
        var notches = so.FindProperty("heatNotches");
        for (int i = 0; i < notches.arraySize; i++)
        {
            var notch = (Image)notches.GetArrayElementAtIndex(i).objectReferenceValue;
            notch.rectTransform.sizeDelta = new Vector2(2, 9);
        }
        var status = (Text)so.FindProperty("heatStatus").objectReferenceValue;
        status.rectTransform.anchoredPosition = new Vector2(184, -14); status.fontSize = 22;
        EditorUtility.SetDirty(hud);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(hud.gameObject.scene); EditorSceneManager.SaveOpenScenes();
        return "Enemy plumes retired; size-specific asteroid states, world capsules and slim heat rail bound";
    }

    public static object BindBursts()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var burst = UnityEngine.Object.FindAnyObjectByType<AsteroidDestructionPresentation>(FindObjectsInactive.Include);
        if (burst == null) throw new Exception("Missing asteroid presentation");
        var rock = Frames("rock-shatter");
        var cargo = Frames("supply-burst");
        if (rock.Length != 12 || cargo.Length != 4) throw new Exception("Invalid burst frame count");
        SetArray(burst, "frames", rock); SetArray(burst, "supplyFrames", cargo);
        Set(burst, "burstSprite", rock[1]);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(burst.gameObject.scene); EditorSceneManager.SaveOpenScenes();
        return "Rock and cargo bursts bound";
    }

}
