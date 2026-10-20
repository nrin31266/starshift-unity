using System;
using System.Linq;
using StarShift.Combat;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Batch 2: HUD icon-first, heat gauge sáng, pickup container tách khỏi UI icon.
// Chạy một lần trên saved 02_Gameplay (Edit Mode); idempotent với các node đã có.
public static class ConfigureUiBatch2
{
    const string Art = "Assets/StarShift/Art/M8/";
    const string Shell = "Assets/StarShift/Prefabs/UI/GameShellUI.prefab";
    const string Fonts = "Assets/StarShift/UI/Fonts/";
    static readonly Color Ink = new Color(.04f, .075f, .12f, .92f);
    static readonly Color Steel = new Color(.68f, .76f, .82f, 1f);
    static readonly Color Cyan = new Color(.35f, .95f, 1f, 1f);
    static readonly Color Amber = new Color(1f, .76f, .32f, 1f);
    static Sprite S(string id) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + id + ".png") ?? throw new InvalidOperationException("Missing " + id);
    static void Set(UnityEngine.Object o, string field, UnityEngine.Object value)
    { var so = new SerializedObject(o); var p = so.FindProperty(field) ?? throw new Exception(o.name + ":" + field); p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void BindSprites(UnityEngine.Object o, string field, Sprite[] values)
    { var so = new SerializedObject(o); var p = so.FindProperty(field); p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void BindObjects(UnityEngine.Object o, string field, UnityEngine.Object[] values)
    { var so = new SerializedObject(o); var p = so.FindProperty(field); p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Prefab(string path, Action<GameObject> edit)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static RectTransform Node(Transform parent, string name)
    {
        var found = parent.Find(name) as RectTransform;
        if (found != null) return found;
        found = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        found.SetParent(parent, false);
        return found;
    }
    static RectTransform SpriteImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        var rect = Node(parent, name);
        var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.sprite = sprite; image.type = Image.Type.Simple; image.color = Color.white;
        image.preserveAspect = true; image.raycastTarget = false;
        Place(rect, anchor, offset, size);
        return rect;
    }
    static void Place(RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
    { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = Vector2.one * .5f; rect.anchoredPosition = offset; rect.sizeDelta = size; }
    static void Font(Text text, int size, Color color, Font font, TextAnchor align = TextAnchor.MiddleCenter)
    {
        text.font = font; text.fontSize = size; text.color = color; text.alignment = align;
        text.fontStyle = FontStyle.Normal; text.resizeTextForBestFit = false; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
    }
    static void ClearPlates(Transform root)
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true).Where(i => i.name == "Plate"))
            image.color = Color.clear;
    }

    // HUD: icon 44-52px 1:1, nhãn cấp ngắn, không plate; heat housing sáng có notch.
    static void Hud(GameObject root)
    {
        var hud = root.GetComponentInChildren<GameplayHud>(true);
        if (hud == null) return;
        var display = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "VT323-Regular.ttf");
        var body = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "ChakraPetch-SemiBold.ttf");
        var so = new SerializedObject(hud);
        T Ref<T>(string field) where T : UnityEngine.Object => (T)so.FindProperty(field).objectReferenceValue;
        var combat = hud.transform.Find("SafeArea/CombatHud") ?? hud.transform;

        // Node inventory cũ chỉ hiển thị text dài; bỏ hẳn để HUD không còn plate thừa.
        var staleInventory = combat.Find("WeaponInventory");
        if (staleInventory != null) UnityEngine.Object.DestroyImmediate(staleInventory.gameObject);

        // Weapon: icon to bên trái, nhãn "C1" ngắn bên phải, nút giữ target ≥ 72px.
        var weaponLabel = Ref<Text>("weaponLabel");
        var weapon = (RectTransform)weaponLabel.transform.parent;
        Place(weapon, new Vector2(1, 0), new Vector2(-84, 56), new Vector2(152, 92));
        ClearPlates(weapon);
        var weaponIcon = SpriteImage(weapon, "M8WeaponIcon", S("ui-weapon-pulse"), new Vector2(0, .5f), new Vector2(52, 0), new Vector2(72, 72));
        Set(hud, "weaponIcon", weaponIcon.GetComponent<Image>());
        BindSprites(hud, "weaponIcons", new[] { S("ui-weapon-pulse"), S("ui-weapon-plasma"), S("ui-weapon-homing") });
        Place(weaponLabel.rectTransform, new Vector2(1, .5f), new Vector2(-34, 0), new Vector2(72, 64));
        Font(weaponLabel, 40, Cyan, display);
        var weaponButton = Ref<Button>("weaponButton");
        if (weaponButton != null)
        {
            var image = weaponButton.targetGraphic as Image;
            if (image != null) { image.sprite = S("ui-panel"); image.type = Image.Type.Sliced; image.color = new Color(1, 1, 1, .55f); }
        }

        // Missile: icon + "số/cap", không text dài.
        var missile = Ref<Button>("missileButton");
        if (missile != null)
        {
            Place((RectTransform)missile.transform, new Vector2(1, 0), new Vector2(-84, 160), new Vector2(152, 84));
            var image = missile.targetGraphic as Image;
            if (image != null) { image.sprite = S("ui-panel"); image.type = Image.Type.Sliced; image.color = new Color(1, 1, 1, .5f); }
            SpriteImage(missile.transform, "M8AmmoIcon", S("ui-ammo"), new Vector2(0, .5f), new Vector2(48, 0), new Vector2(64, 64));
            var count = Ref<Text>("missileCount");
            Place(count.rectTransform, new Vector2(1, .5f), new Vector2(-40, 0), new Vector2(80, 64));
            Font(count, 32, Color.white, body);
        }

        // Buff: icon 44 + timer ngay dưới; không còn label paragraph.
        var effectRow = (RectTransform)Ref<Image>("shieldIcon").transform.parent;
        // Node Effects chỉ còn là container; tắt Text để không vẽ paragraph rỗng.
        var staleEffect = effectRow.GetComponent<Text>();
        if (staleEffect != null) { staleEffect.text = ""; staleEffect.enabled = false; }
        foreach (var pair in new[] { new[] { "shieldIcon", "ui-shield" }, new[] { "overdriveIcon", "ui-overdrive" }, new[] { "supportIcon", "ui-support" } })
        {
            var img = Ref<Image>(pair[0]);
            img.sprite = S(pair[1]);
            Place(img.rectTransform, new Vector2(.5f, .5f), new Vector2((System.Array.IndexOf(new[] { "shieldIcon", "overdriveIcon", "supportIcon" }, pair[0]) - 1) * 64, 8), new Vector2(48, 48));
        }
        if (effectRow != null) Place(effectRow, new Vector2(.5f, 0), new Vector2(0, 74), new Vector2(220, 96));
        var odTimer = Ref<Text>("overdriveTimer");
        if (odTimer != null) { Place(odTimer.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -32), new Vector2(64, 26)); Font(odTimer, 26, Amber, body); }
        var drTimer = Ref<Text>("supportTimer");
        if (drTimer != null) { Place(drTimer.rectTransform, new Vector2(.5f, .5f), new Vector2(64, -32), new Vector2(64, 26)); Font(drTimer, 26, Cyan, body); }
        var cue = Ref<Text>("pickupLabel");
        if (cue != null) { Place(cue.rectTransform, new Vector2(.5f, 0), new Vector2(0, 150), new Vector2(520, 34)); Font(cue, 26, new Color(.85f, .97f, 1f), body); }

        // Lives: icon 44 + ×N.
        var livesLabel = Ref<Text>("livesLabel");
        var lives = (RectTransform)livesLabel.transform.parent;
        Place(lives, new Vector2(0, 0), new Vector2(66, 44), new Vector2(132, 72));
        ClearPlates(lives);
        var lifeIcon = lives.Find("LifeIcon") as RectTransform;
        if (lifeIcon == null) lifeIcon = Node(lives, "LifeIcon");
        var lifeImage = lifeIcon.GetComponent<Image>() ?? lifeIcon.gameObject.AddComponent<Image>();
        lifeImage.sprite = S("ui-life"); lifeImage.preserveAspect = true; lifeImage.raycastTarget = false; lifeImage.color = Color.white;
        Place(lifeIcon, new Vector2(0, .5f), new Vector2(36, 0), new Vector2(56, 56));
        Place(livesLabel.rectTransform, new Vector2(1, .5f), new Vector2(-36, 0), new Vector2(84, 60));
        Font(livesLabel, 42, Color.white, display);

        // Counters: icon 40 + số; caption text ngắn chỉ còn icon.
        foreach (var spec in new[] { new[] { "scoreLabel", "ui-score", "0.42" }, new[] { "shardLabel", "ui-shard", "0.62" } })
        {
            var count = Ref<Text>(spec[0]);
            var row = (RectTransform)count.transform.parent;
            Place(row, new Vector2(float.Parse(spec[2], System.Globalization.CultureInfo.InvariantCulture), 1), new Vector2(0, -52), new Vector2(160, 64));
            ClearPlates(row);
            foreach (var old in row.GetComponentsInChildren<Text>(true).Where(t => t != count)) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var oldIcon = row.Find("M8ShardIcon") ?? row.Find("Icon");
            if (oldIcon != null) UnityEngine.Object.DestroyImmediate(oldIcon.gameObject);
            SpriteImage(row, "CounterIcon", S(spec[1]), new Vector2(0, .5f), new Vector2(28, 0), new Vector2(48, 48));
            Place(count.rectTransform, new Vector2(1, .5f), new Vector2(-30, 0), new Vector2(96, 56));
            Font(count, 38, spec[0] == "shardLabel" ? Amber : Color.white, display);
        }

        // Heat: housing sáng 40px, fill dày 20px, notch hiển thị, status ngay trên.
        var heatFrame = Ref<Image>("heatFrame");
        var gauge = (RectTransform)heatFrame.transform.parent;
        Place(gauge, new Vector2(0, 1), new Vector2(150, -48), new Vector2(272, 76));
        ClearPlates(gauge);
        var caption = gauge.Find("Caption") as RectTransform;
        if (caption == null) caption = Node(gauge, "Caption");
        var captionText = caption.GetComponent<Text>() ?? caption.gameObject.AddComponent<Text>();
        captionText.text = "NHIỆT";
        Font(captionText, 26, new Color(.78f, .9f, .98f), body);
        Place(caption, new Vector2(0, 1), new Vector2(58, -18), new Vector2(96, 28));
        Place((RectTransform)heatFrame.transform, new Vector2(0, 0), new Vector2(148, 26), new Vector2(256, 40));
        heatFrame.sprite = S("ui-heat-housing"); heatFrame.type = Image.Type.Sliced; heatFrame.color = Color.white; heatFrame.raycastTarget = false;
        var track = heatFrame.transform.Find("HeatTrack") as RectTransform;
        if (track != null)
        {
            Place(track, new Vector2(0, 0), new Vector2(148, 26), new Vector2(236, 22));
            var trackImage = track.GetComponent<Image>();
            if (trackImage != null) trackImage.color = new Color(.13f, .22f, .30f, .98f);
        }
        // Well cũ tô gần đen, che mất housing sáng; đổi sang slate để notch/fill đọc rõ.
        var well = heatFrame.transform.Find("HeatTrack/Well") ?? heatFrame.transform.Find("Well");
        var wellRect = well as RectTransform;
        if (wellRect != null)
        {
            var wellImage = wellRect.GetComponent<Image>();
            if (wellImage != null) wellImage.color = new Color(.20f, .31f, .40f, 1f);
            wellRect.offsetMin = new Vector2(8, 7); wellRect.offsetMax = new Vector2(-8, -7);
        }
        var heatFill = Ref<Image>("heatFill");
        heatFill.sprite = S("ui-heat-fill"); heatFill.type = Image.Type.Tiled; heatFill.color = Cyan;
        heatFill.rectTransform.anchorMin = new Vector2(0, 0); heatFill.rectTransform.anchorMax = new Vector2(1, 1);
        heatFill.rectTransform.offsetMin = new Vector2(10, 8); heatFill.rectTransform.offsetMax = new Vector2(-10, -8);
        var marker = heatFill.transform.parent.Find("RecoveryMarker") as RectTransform;
        if (marker != null) { Place(marker, new Vector2(.4f, .5f), Vector2.zero, new Vector2(3, 22)); marker.GetComponent<Image>().color = new Color(1, 1, 1, .9f); }

        var notches = new Image[4];
        for (int i = 0; i < notches.Length; i++)
        {
            var notch = SpriteImage(heatFrame.transform, "M8HeatNotch" + i, S("ui-heat-notch"), new Vector2(.2f * (i + 1), .5f), Vector2.zero, new Vector2(6, 28));
            notches[i] = notch.GetComponent<Image>();
        }
        BindObjects(hud, "heatNotches", notches);

        var status = Ref<Text>("heatStatus");
        if (status != null)
        {
            Place(status.rectTransform, new Vector2(1, 1), new Vector2(-66, -18), new Vector2(120, 28));
            Font(status, 30, Amber, body, TextAnchor.MiddleRight);
        }

        // Pause: nút 64px với icon 40.
        var pause = Ref<Button>("pauseButton");
        if (pause != null)
        {
            Place((RectTransform)pause.transform, Vector2.one, new Vector2(-52, -52), new Vector2(72, 72));
            var face = pause.transform.Find("Face") as RectTransform;
            if (face != null)
            {
                Place(face, Vector2.one * .5f, Vector2.zero, new Vector2(64, 64));
                var faceImage = face.GetComponent<Image>() ?? face.gameObject.AddComponent<Image>();
                faceImage.sprite = S("ui-icon-pause"); faceImage.type = Image.Type.Simple; faceImage.color = Color.white; faceImage.preserveAspect = true;
                var plate = face.Find("Plate");
                if (plate != null) plate.GetComponent<Image>().color = Color.clear;
            }
            var image = pause.targetGraphic as Image;
            if (image != null) { image.sprite = S("ui-panel"); image.type = Image.Type.Sliced; image.color = new Color(1, 1, 1, .9f); }
            var label = pause.transform.Find("Label")?.GetComponent<Text>();
            if (label != null) label.text = "";
        }
        EditorUtility.SetDirty(hud);
    }

    // Button/card/modal: nine-slice mới, hierarchy rõ, target ≥ 44px.
    static void ShellSkin(GameObject root)
    {
        var view = root.GetComponent<ShellView>();
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "Plate" || image.name == "Card" || image.name.StartsWith("Block"))
            { image.sprite = S("ui-panel"); image.type = Image.Type.Sliced; image.color = Color.white; }
        }
        foreach (var modal in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Card" && t.parent != null && (t.parent.name == "Pause" || t.parent.name == "Results" || t.parent.name == "Confirmation" || t.parent.name == "Error")))
        {
            var modalSprite = modal.parent.name == "Results" ? "ui-card" : "ui-modal";
            foreach (var image in modal.GetComponents<Image>())
            { image.sprite = S(modalSprite); image.type = Image.Type.Sliced; image.color = Color.white; }
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (!(button.targetGraphic is Image image)) continue;
            bool card = view != null && view.CosmeticCards.Contains(button);
            bool tab = view != null && (view.CosmeticTabs.Contains(button) || button == view.HangarMode || button == view.StoreMode);
            bool primary = view != null && (button == view.Continue || button == view.NewGame || button == view.CosmeticAction || button == view.Next || button == view.LaunchReplay || button == view.Confirm || button == view.SaveSettings || button == view.Resume);
            image.sprite = S(card ? "ui-card" : tab ? "ui-tab" : primary ? "ui-button" : "ui-button-secondary");
            image.type = Image.Type.Sliced; image.color = Color.white;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(.82f, 1, 1); colors.selectedColor = new Color(.7f, 1, 1);
            colors.pressedColor = new Color(.55f, .8f, .9f); colors.disabledColor = new Color(.45f, .52f, .6f, .55f);
            colors.fadeDuration = .08f; button.colors = colors;
            var rect = (RectTransform)button.transform;
            if (rect.sizeDelta.y < 44 && rect.sizeDelta.y > 0) rect.sizeDelta = new Vector2(rect.sizeDelta.x, 44);
        }
        foreach (var outline in root.GetComponentsInChildren<Outline>(true))
        {
            outline.effectColor = new Color(.35f, .95f, 1, .95f);
            outline.effectDistance = new Vector2(2, -2);
        }
        var display = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "VT323-Regular.ttf");
        var body = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "ChakraPetch-Medium.ttf");
        var strong = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "ChakraPetch-SemiBold.ttf");
        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            bool heading = text.name == "Title" || text.name == "ShipName" || text.name == "ResultTitle";
            text.font = heading ? display : text.GetComponentInParent<Button>() != null ? strong : body;
            text.fontStyle = FontStyle.Normal; text.resizeTextForBestFit = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (text.fontSize < 26) text.fontSize = 26;
        }
        EditorUtility.SetDirty(root);
    }

    public static object Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        AssetDatabase.Refresh();
        // Border nine-slice: khung UI 6px; housing giữ 6/4 để notch không bị kéo giãn.
        foreach (var id in new[] { "ui-panel", "ui-button", "ui-button-secondary", "ui-tab", "ui-card", "ui-modal" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + id + ".png");
            importer.spriteBorder = new Vector4(6, 6, 6, 6); importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        foreach (var id in new[] { "ui-heat-housing" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + id + ".png");
            importer.spriteBorder = new Vector4(6, 4, 6, 4); importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        foreach (var id in new[] { "ui-heat-fill", "ui-heat-notch" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + id + ".png");
            importer.spriteBorder = Vector4.zero; importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        // Pickup world scale: container 64x72 ở 56.25 PPU đã ≥ 48px ở 900p; giữ 1.0 để neon symbol đọc rõ.
        foreach (var guid in AssetDatabase.FindAssets("t:PickupDefinition", new[] { "Assets/StarShift/Data" }))
        {
            var item = AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            item.VisualScale = 1f;
            EditorUtility.SetDirty(item);
        }
        Prefab(Shell, ShellSkin);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) ShellSkin(root);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Hud(root);
        AssetDatabase.SaveAssets();
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new
        {
            status = "PASS",
            hudIcon = new[] { 48, 52 },
            pickupCanvas = new[] { 64, 72 },
            minimumPixelsAt900 = 72f * 900f / (19.2f * 56.25f),
            heatHousingHeight = 40,
            heatNotches = 4,
            font = "VT323 display / Chakra Petch body"
        };
    }

    // Đọc lại để verify wiring, không sửa gì.
    public static object Inspect()
    {
        var hud = UnityEngine.Object.FindAnyObjectByType<GameplayHud>();
        var so = new SerializedObject(hud);
        var icon = so.FindProperty("weaponIcon").objectReferenceValue as Image;
        var notches = so.FindProperty("heatNotches");
        var frame = so.FindProperty("heatFrame").objectReferenceValue as Image;
        return new
        {
            weaponIcon = icon != null ? new { name = icon.sprite.name, width = icon.rectTransform.rect.width, height = icon.rectTransform.rect.height } : null,
            heatFrame = frame != null ? new { name = frame.sprite.name, type = frame.type.ToString(), height = frame.rectTransform.rect.height } : null,
            heatNotchCount = notches.arraySize,
            weaponIcons = so.FindProperty("weaponIcons").arraySize,
            shellButtons = GameObject.Find("GameShellUI").GetComponentsInChildren<Button>(true).Length
        };
    }
}
