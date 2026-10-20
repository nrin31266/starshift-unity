using System;
using System.Collections.Generic;
using System.Linq;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConfigureM8Art
{
    const string Art = "Assets/StarShift/Art/M8/";
    const string Fonts = "Assets/StarShift/UI/Fonts/";
    public const float Ppu = 56.25f;
    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png") ?? throw new InvalidOperationException("Missing art: " + name);
    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); var property = so.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.name + ": missing " + field);
        property.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Import()
    {
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Art.TrimEnd('/') }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = path.EndsWith("explosion-sheet.png") ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = path.Contains("ui-") || path.Contains("hud-") ? 100 : path.EndsWith("background.png") ? 21 : Ppu;
            importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = !path.EndsWith("background.png");
            importer.wrapMode = TextureWrapMode.Clamp; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            if (path.Contains("ui-")) Border(importer);
        }
        Slice();
    }
    static ISpriteEditorDataProvider Provider(TextureImporter importer, params EEditCapability[] required)
    {
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new InvalidOperationException("No Sprite Editor data provider");
        provider.InitSpriteEditorDataProvider();
        var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (edit == null || required.Any(c => !edit.GetEditCapability().HasCapability(c)))
            throw new InvalidOperationException("Unsupported sprite edit capability; no mutation applied");
        return provider;
    }
    static void Border(TextureImporter importer)
    {
        var provider = Provider(importer, EEditCapability.EditBorder);
        var rects = provider.GetSpriteRects();
        foreach (var rect in rects) rect.border = new Vector4(6, 6, 6, 6);
        provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
    }
    static void Slice()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "explosion-sheet.png");
        var provider = Provider(importer, EEditCapability.CreateAndDeleteSprite, EEditCapability.EditSpriteRect, EEditCapability.EditPivot, EEditCapability.EditSpriteName);
        var existing = provider.GetSpriteRects().ToDictionary(r => r.name);
        var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (ids == null) throw new InvalidOperationException("No name/file-ID provider");
        var rects = new SpriteRect[8];
        for (int i = 0; i < rects.Length; i++)
        {
            var name = "explosion_" + i.ToString("D2");
            rects[i] = new SpriteRect { name = name, rect = new Rect((i % 4) * 64, 128 - (i / 4 + 1) * 64, 64, 64), alignment = SpriteAlignment.Center, pivot = Vector2.one * .5f, spriteID = existing.TryGetValue(name, out var old) ? old.spriteID : UnityEngine.GUID.Generate() };
        }
        provider.SetSpriteRects(rects); ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
    }
    static void Prefab(string name, Action<GameObject> edit)
    {
        var path = "Assets/StarShift/Prefabs/" + name + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void Hull(GameObject root, string art, bool heavy)
    {
        var hull = root.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "HullPlaceholder");
        hull.sprite = Sprite(art); hull.color = new Color(.9f, .9f, .9f, 1);
        // Giữ scale/collider gameplay của root, chỉ đưa layer hình về đúng mật độ pixel.
        hull.transform.localScale = Vector3.one / root.transform.localScale.x;
        var fire = root.GetComponent<EnemyFirePresentation>();
        foreach (var r in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (heavy && r.name.StartsWith("EnginePod")) r.enabled = false;
            if (r.name != "CorePlaceholder") continue;
            r.enabled = fire != null;
            if (fire == null) continue;
            r.sprite = Sprite("enemy-telegraph"); r.transform.localScale = Vector3.one;
            r.color = new Color(1, 1, 1, 0); r.sortingOrder = hull.sortingOrder + 2;
            var so = new SerializedObject(fire); so.FindProperty("chargedTint").colorValue = Color.white; so.ApplyModifiedPropertiesWithoutUndo();
        }
        var damage = root.GetComponent<EnemyDamagePresentation>();
        if (damage != null) { Set(damage, "damagedSprite", null); Set(damage, "criticalSprite", null); }
    }
    static void Style(GameObject root)
    {
        var display = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "VT323-Regular.ttf");
        var body = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "ChakraPetch-Medium.ttf");
        var strong = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "ChakraPetch-SemiBold.ttf");
        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            bool heading = text.name == "Title" || text.name == "ShipName" || text.name == "ResultTitle" || text.name == "RegionA" || text.name == "RegionB";
            text.font = heading ? display : text.GetComponentInParent<Button>() != null ? strong : body;
            text.fontStyle = FontStyle.Normal; text.resizeTextForBestFit = false;
            if (heading) text.fontSize = text.text == "STARSHIFT" ? 100 : text.fontSize >= 46 ? 60 : 40;
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            var image = button.targetGraphic as Image;
            if (image != null) { image.sprite = Sprite("ui-button"); image.type = Image.Type.Sliced; image.color = Color.white; }
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.85f, 1, 1);
            colors.selectedColor = new Color(.8f, 1, 1); colors.pressedColor = new Color(.55f, .75f, .85f);
            colors.disabledColor = new Color(.35f, .4f, .45f, .55f); colors.colorMultiplier = 1; colors.fadeDuration = .08f; button.colors = colors;
        }
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "Card" || image.name.StartsWith("Block") || image.name == "Plate")
            { image.sprite = Sprite("ui-panel"); image.type = Image.Type.Sliced; image.color = Color.white; }
            if (image.name == "LifeIcon") { image.sprite = Sprite("ui-life"); image.color = Color.white; image.preserveAspect = true; image.rectTransform.sizeDelta = new Vector2(44, 44); }
            if (image.name == "Ship" && image.transform.parent.name == "ShipPreview")
            { image.sprite = Sprite("player"); image.color = Color.white; image.rectTransform.sizeDelta = new Vector2(256, 320); image.preserveAspect = true; }
        }
        foreach (var row in root.GetComponentsInChildren<RectTransform>(true))
            if ((row.name == "Score" || row.name == "Shard") && row.parent != null && row.parent.name == "Counters")
                row.anchoredPosition = new Vector2(row.anchoredPosition.x, -50);
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.pixelPerfect = true;
    }
    static void Environment(GameObject root)
    {
        var view = root.GetComponent<ShellView>(); if (view == null) return;
        foreach (var index in new[] { ShellScreen.Menu, ShellScreen.Hangar })
        {
            var screen = view.Screens[(int)index].transform;
            var child = screen.Find("M8Environment");
            if (child == null) child = new GameObject("M8Environment", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).transform;
            child.SetParent(screen, false); child.SetAsFirstSibling();
            var rect = (RectTransform)child; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = child.GetComponent<Image>(); image.sprite = Sprite("background"); image.color = Color.white; image.raycastTarget = false; image.preserveAspect = true;
            var aspect = child.GetComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; aspect.aspectRatio = 768f / 336;
        }
    }
    public static string Contracts()
    {
        var rows = new List<object>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/StarShift/Prefabs" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var c in root.GetComponentsInChildren<Collider2D>(true))
                rows.Add(new { path, node = AnimationUtility.CalculateTransformPath(c.transform, root.transform), trigger = c.isTrigger, offset = c.offset.ToString("R"), scale = c.transform.lossyScale.ToString("R"), shape = c is BoxCollider2D b ? b.size.ToString("R") : c is CircleCollider2D circle ? circle.radius.ToString("R") : c.GetType().Name });
        }
        foreach (var guid in AssetDatabase.FindAssets("t:PulseLaserDefinition", new[] { "Assets/StarShift/Data" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var d = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>(path);
            rows.Add(new { path, d.Branch, d.Damage, d.FireInterval, d.ProjectileSpeed, d.ProjectileLifetime, d.TrackingDegreesPerSecond, d.TrackingRange, d.TrackingLaunchSeconds, d.TrackingLaunchArcDegrees, levels = Enumerable.Range(1, 5).Select(d.Pattern).ToArray() });
        }
        foreach (var guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/StarShift/Data" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var d = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            rows.Add(new { path, d.MaxHp, d.Score, d.BodyDamagesPlayer, d.MissileTier });
        }
        foreach (var guid in AssetDatabase.FindAssets("t:SingleFireDefinition", new[] { "Assets/StarShift/Data" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var d = AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(path);
            rows.Add(new { path, d.ProjectileSpeed, d.ProjectileLifetime, d.InitialDelay, d.TelegraphDuration, d.Cooldown, d.ProjectileCount, d.SpreadDegrees, d.BurstCount, d.BurstInterval });
        }
        foreach (var guid in AssetDatabase.FindAssets("t:PickupDefinition", new[] { "Assets/StarShift/Data" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var d = AssetDatabase.LoadAssetAtPath<PickupDefinition>(path);
            rows.Add(new { path, d.Kind, d.Amount, d.VisualScale, d.Lifetime, d.DriftSpeed, d.Duration, d.FireRateMultiplier, d.FallAcceleration, d.MaximumFallSpeed, d.ShardBounceHeight });
        }
        return Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented);
    }
    public static object Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        const string baseline = "../.design/m8/contracts-before.json";
        System.IO.Directory.CreateDirectory("../.design/m8");
        var before = Contracts();
        if (!System.IO.File.Exists(baseline)) System.IO.File.WriteAllText(baseline, before);
        else if (System.IO.File.ReadAllText(baseline) != before) throw new InvalidOperationException("Gameplay contract baseline changed before authoring");
        Import();
        Prefab("Enemies/Scout", r => Hull(r, "scout", false));
        Prefab("Enemies/Raider", r => Hull(r, "raider", false));
        Prefab("Enemies/Bomber", r => Hull(r, "heavy", true));
        Prefab("Enemies/VanguardFrigate", root => {
            foreach (var r in root.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name == "CannonPod-1" || r.name == "CannonPod1"))
            { r.sprite = Sprite("boss-module"); r.color = Color.white; r.transform.localScale = Vector3.one; }
        });
        Prefab("Projectiles/EnemySingleProjectile", root => {
            var visual = root.GetComponentInChildren<SpriteRenderer>(true); visual.sprite = Sprite("enemy-bolt"); visual.color = Color.white; visual.transform.localScale = Vector3.one;
        });
        Prefab("Projectiles/PlayerPulseProjectile", root => {
            var visual = root.GetComponentInChildren<SpriteRenderer>(true); visual.sprite = Sprite("player-pulse"); visual.color = Color.white;
        });
        foreach (var guid in AssetDatabase.FindAssets("t:PulseLaserDefinition", new[] { "Assets/StarShift/Data" }))
        {
            var definition = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition.Branch != WeaponBranch.Pulse) continue;
            var so = new SerializedObject(definition); so.FindProperty("projectileSprite").objectReferenceValue = Sprite("player-pulse");
            so.FindProperty("projectileColor").colorValue = Color.white; so.FindProperty("projectileScale").vector2Value = new Vector2(1.25f, 1);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var guid in AssetDatabase.FindAssets("t:PickupDefinition", new[] { "Assets/StarShift/Data" }))
        {
            var item = AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (item.Kind != PickupKind.Shard) continue;
            item.Sprite = Sprite("stellar-shard"); EditorUtility.SetDirty(item);
        }
        Prefab("UI/GameShellUI", root => { Style(root); Environment(root); });
        var player = GameObject.Find("Player"); var appearance = player.GetComponent<ShipAppearance>();
        var appearanceSo = new SerializedObject(appearance);
        foreach (var name in new[] { "hull", "approachHull" })
        { var r = (SpriteRenderer)appearanceSo.FindProperty(name).objectReferenceValue; r.sprite = Sprite("player"); r.color = Color.white; r.transform.localScale = Vector3.one; EditorUtility.SetDirty(r); }
        var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/StarShift/Data/Cosmetics/ShipCosmetics.asset");
        catalog.Default(CosmeticSlot.Chassis).Sprite = Sprite("player"); catalog.Default(CosmeticSlot.Chassis).FlipY = false;
        catalog.Default(CosmeticSlot.Skin).Tint = Color.white; EditorUtility.SetDirty(catalog);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Style(root);
        var shell = UnityEngine.Object.FindAnyObjectByType<ShellView>(); Environment(shell.gameObject);
        var camera = Camera.main; var background = camera.transform.Find("M8Environment");
        if (background == null) { background = new GameObject("M8Environment").transform; background.SetParent(camera.transform, false); }
        background.localPosition = new Vector3(0, 0, 20); background.localScale = Vector3.one;
        var renderer = background.GetComponent<SpriteRenderer>(); if (renderer == null) renderer = background.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = Sprite("background"); renderer.color = Color.white; renderer.sortingOrder = -1000;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(8, 15, 32, 255);
        var effect = UnityEngine.Object.FindAnyObjectByType<AsteroidDestructionPresentation>();
        var frames = AssetDatabase.LoadAllAssetsAtPath(Art + "explosion-sheet.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length != 8) throw new InvalidOperationException("Explosion needs eight frames");
        var effectSo = new SerializedObject(effect); var property = effectSo.FindProperty("frames"); property.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        effectSo.FindProperty("seconds").floatValue = .5f; effectSo.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        var after = Contracts(); System.IO.File.WriteAllText("../.design/m8/contracts-after.json", after);
        if (after != before) throw new InvalidOperationException("Art authoring changed gameplay/collider contracts");
        return new { status = "AUTHORED", ppu = Ppu, nativePixelsAt900 = 900f / (camera.orthographicSize * 2) / Ppu, frames = frames.Length, gameplayContractsChanged = false };
    }
}
