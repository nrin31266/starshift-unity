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
    const string Art = "Assets/StarShift/Art/";
    static string ArtPath(string id) => AssetDatabase.FindAssets(id + " t:Texture2D", new[]{Art.TrimEnd('/')})
        .Select(AssetDatabase.GUIDToAssetPath).Single(path => System.IO.Path.GetFileNameWithoutExtension(path) == id);
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
    static Sprite[] Frames(string id) => AssetDatabase.LoadAllAssetsAtPath(ArtPath(id + "-sheet")).OfType<Sprite>().OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
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
        if (appearance != null)
        {
            var so = new SerializedObject(appearance);
            var primary = (TrailRenderer)so.FindProperty("movementTrail").objectReferenceValue;
            foreach (var trail in root.GetComponentsInChildren<TrailRenderer>(true))
                if (trail.name.StartsWith("CosmeticEngineTrail") && trail != primary) UnityEngine.Object.DestroyImmediate(trail.gameObject);
            if (primary != null)
            {
                primary.name = "MovementTrail"; primary.startWidth = .07f; primary.endWidth = 0; primary.time = .12f;
                primary.emitting = false; primary.Clear();
            }
            EditorUtility.SetDirty(appearance);
        }
        var old = root.transform.Find("M8Debris");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var death = root.GetComponent<PlayerDeathPresentation>();
        if (death == null) death = root.AddComponent<PlayerDeathPresentation>();
        var frames = Frames("explosion");
        if (frames.Length != 8) throw new Exception("Expected 8 common explosion frames");
        Set(death, "explosion", Visual(root, "DeathExplosion", frames[0], 18));
        SetArray(death, "explosionFrames", frames);
        var debris = Enumerable.Range(0, 4).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath("player-fragment-" + i))).ToArray();
        if (debris.Any(sprite => sprite == null)) throw new Exception("Missing armor fragments");
        SetArray(death, "fragments", Enumerable.Range(0, 4).Select(i => (UnityEngine.Object)Visual(root, "DeathFragment" + i, debris[i], 19)).ToArray());
        var combat = root.GetComponent<PlayerCombatPresentation>();
        if (combat != null)
        {
            SetArray(combat,"muzzleFrames",Frames("muzzle-pulse"));
            SetArray(combat,"pulseMuzzleFrames",Frames("muzzle-pulse"));
            SetArray(combat,"plasmaMuzzleFrames",Frames("muzzle-plasma"));
            SetArray(combat,"missileMuzzleFrames",Frames("muzzle-missile"));
            var muzzle=(SpriteRenderer)new SerializedObject(combat).FindProperty("muzzle").objectReferenceValue;
            muzzle.sprite=Frames("muzzle-pulse")[0];
        }
        EditorUtility.SetDirty(death);
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
                if (asteroid != null)
                {
                    var tint = new SerializedObject(root.GetComponent<EnemyDamagePresentation>());
                    tint.FindProperty("damagedTint").colorValue = Color.white;
                    tint.FindProperty("criticalTint").colorValue = Color.white;
                    tint.ApplyModifiedPropertiesWithoutUndo();
                }
                if (root.name == "VanguardFrigate")
                    foreach (var cannon in root.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name.StartsWith("CannonPod")))
                        cannon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath("vanguard-beam-pod"));
                if (asteroid != null && root.name == "StormAsteroid")
                {
                    foreach (var pair in new[]{new[]{"sizeSprites",""},new[]{"damagedSizeSprites","-damaged"},new[]{"criticalSizeSprites","-critical"}})
                        SetArray(asteroid, pair[0], new[]{"small","medium","large","huge"}.Select(size => (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath("asteroid-"+size+pair[1]))).ToArray());
                }
                else if (asteroid != null)
                {
                    foreach (var field in new[]{"sizeSprites", "damagedSizeSprites", "criticalSizeSprites"})
                        SetArray(asteroid, field, System.Array.Empty<UnityEngine.Object>());
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var guid in AssetDatabase.FindAssets("t:PickupDefinition", new[]{"Assets/StarShift/Data"}))
        {
            var item = AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            var id = item.Kind == PickupKind.Shard ? "stellar-shard" : item.Kind == PickupKind.ExtraLife ? "extra-life" : item.Kind == PickupKind.Shield ? "shield-cell" : item.Kind == PickupKind.Overdrive ? "overdrive" : item.Kind == PickupKind.SupportDrone ? "support-drone" : item.Kind == PickupKind.MissileAmmo ? "missile-ammo" : item.Branch == StarShift.Combat.WeaponBranch.Pulse ? "pulse-core" : item.Branch == StarShift.Combat.WeaponBranch.Plasma ? "plasma-core" : "homing-core";
            item.Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath(id));
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
        var cargo = Frames("supply-burst-polished");
        if (rock.Length != 12 || cargo.Length != 8) throw new Exception("Invalid burst frame count");
        SetArray(burst, "frames", rock); SetArray(burst, "supplyFrames", cargo);
        Set(burst, "burstSprite", rock[1]);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(burst.gameObject.scene); EditorSceneManager.SaveOpenScenes();
        return "Rock and cargo bursts bound";
    }
    public static object BindSpeedStreaks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var intro = UnityEngine.Object.FindAnyObjectByType<StarShift.UI.MissionIntroPresentation>(FindObjectsInactive.Include);
        var so = new SerializedObject(intro);
        var stars = (Transform)so.FindProperty("stars").objectReferenceValue;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath("speed-streak"));
        foreach (var renderer in stars.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sprite = sprite; renderer.color = Color.white; EditorUtility.SetDirty(renderer);
        }
        so.FindProperty("streakWidthAt900").floatValue = 3;
        so.FindProperty("streakLengthPerSpeedAt900").floatValue = 70;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(intro.gameObject.scene); EditorSceneManager.SaveOpenScenes();
        return new { streaks = stars.GetComponentsInChildren<SpriteRenderer>(true).Length };
    }
    public static object UnusedExports(bool remove = false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        var owners=AssetDatabase.GetAllAssetPaths().Where(path=>path.StartsWith("Assets/")&&new[]{".unity",".prefab",".asset",".anim",".controller",".mat"}.Contains(System.IO.Path.GetExtension(path))).ToArray();
        var used=new System.Collections.Generic.HashSet<string>(AssetDatabase.GetDependencies(owners,true));
        var files=new[]{"Ships","Bosses","Projectiles","Pickups","UI","VFX","Environments"}
            .SelectMany(folder=>System.IO.Directory.GetFiles(Art+folder,"*.png",System.IO.SearchOption.AllDirectories))
            .Select(path=>path.Replace('\\','/')).Where(path=>!used.Contains(path)).OrderBy(path=>path).ToArray();
        System.IO.File.WriteAllText("../.design/m8-rebuild/unused-exports.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{remove,files},Newtonsoft.Json.Formatting.Indented));
        if(remove)
        {
            foreach(var path in files)if(!AssetDatabase.DeleteAsset(path))throw new Exception("Delete failed: "+path);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        return new{remove,files};
    }
    public static object BindWardenDamage()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        const string path="Assets/StarShift/Prefabs/Enemies/NebulaWarden.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var pair in new[]{new[]{"BeamNodeAttack","warden-beam-node"},new[]{"DroneNode","warden-drone-node"},new[]{"PlasmaNode","warden-plasma-node"}})
            {
                var node=root.transform.Find(pair[0]).gameObject;
                if(node.GetComponent<EnemyHealth>()==null)throw new Exception("Missing node HP: "+pair[0]);
                var damage=node.GetComponent<EnemyDamagePresentation>();
                if(damage==null)damage=node.AddComponent<EnemyDamagePresentation>();
                Set(damage,"hull",node.transform.Find("M8Art").GetComponent<SpriteRenderer>());
                Set(damage,"damagedSprite",AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath(pair[1]+"-damaged")));
                Set(damage,"criticalSprite",AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath(pair[1]+"-critical")));
                var so=new SerializedObject(damage);
                so.FindProperty("damagedTint").colorValue=Color.white;so.FindProperty("criticalTint").colorValue=Color.white;
                so.FindProperty("damagedHealthFraction").floatValue=.5f;so.FindProperty("criticalHealthFraction").floatValue=.25f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();return "Three Warden node damage-state families bound to existing node HP";
    }

}
