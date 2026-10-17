using System;
using StarShift.Combat;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConfigureThermalHud
{
    const float Unit = 8;
    const float Margin = Unit;
    const float Padding = Unit * .5f;
    const float RowHeight = Unit * 5.5f;
    static readonly Color Cyan = new Color(.3f, .9f, 1);
    static readonly Color Ink = new Color(.025f, .045f, .065f, .55f);

    public static object Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.isDirty || scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity")
            throw new InvalidOperationException("Open saved gameplay scene in Edit Mode first.");
        const string path = "Assets/StarShift/Data/Weapons/PulseLaserThermal.asset";
        var profile = AssetDatabase.LoadAssetAtPath<WeaponThermalProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<WeaponThermalProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        var weapon = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>("Assets/StarShift/Data/Weapons/PulseLaserLv1.asset");
        Bind(weapon, "thermalProfile", profile);
        var ui = GameObject.Find("GameplayUI");
        var hud = ui.GetComponent<GameplayHud>();
        var combat = ui.transform.Find("SafeArea/CombatHud");
        var lives = (RectTransform)combat.Find("Lives");
        Place(lives, Vector2.zero, new Vector2(Margin + 46, Margin + RowHeight / 2), new Vector2(92, RowHeight));
        Plate(lives);
        Place((RectTransform)lives.Find("LifeIcon"), new Vector2(0, .5f), new Vector2(Padding + 14, 0), new Vector2(28, 34));
        Style(lives.Find("Count").GetComponent<Text>(), 32, Color.white);
        Place((RectTransform)lives.Find("Count"), new Vector2(1, .5f), new Vector2(-Padding - 25, 0), new Vector2(50, RowHeight));
        Counter((RectTransform)combat.Find("Score"), -48, 72, "S");
        Counter((RectTransform)combat.Find("Shard"), 40, 88, "SS");
        var pause = (RectTransform)combat.Find("Pause");
        Place(pause, Vector2.one, new Vector2(-Margin - RowHeight / 2, -Margin - RowHeight / 2), new Vector2(56, 56));
        pause.GetComponent<Image>().color = Color.clear;
        var face = Child(pause, "Face");
        Place(face, new Vector2(.5f, .5f), Vector2.zero, new Vector2(RowHeight, RowHeight));
        var oldFace = face.GetComponent<Image>(); if (oldFace != null) UnityEngine.Object.DestroyImmediate(oldFace);
        Plate(face);
        face.SetAsFirstSibling();
        var pauseText = pause.Find("Label").GetComponent<Text>();
        Style(pauseText, 30, Color.white);

        var status = (RectTransform)combat.Find("Weapon");
        Place(status, new Vector2(1, 0), new Vector2(-Margin - 44, Margin + RowHeight / 2), new Vector2(88, RowHeight));
        Plate(status);
        var level = status.Find("WeaponLabel").GetComponent<Text>();
        level.text = GameplayCopy.WeaponStatus(1); Style(level, 28, Color.white);
        Stretch(level.rectTransform); level.rectTransform.offsetMin = new Vector2(Padding, 0); level.rectTransform.offsetMax = new Vector2(-Padding, 0);
        var oldIcon = status.Find("WeaponIcon"); if (oldIcon != null) UnityEngine.Object.DestroyImmediate(oldIcon.gameObject);
        var gauge = Child(combat, "HeatGauge");
        Place(gauge, new Vector2(0, 1), new Vector2(Margin + 132, -Margin - 28), new Vector2(264, 56));
        Plate(gauge);
        var caption = Child(gauge, "Caption");
        var captionText = caption.GetComponent<Text>() ?? caption.gameObject.AddComponent<Text>();
        captionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        captionText.text = "H"; Style(captionText, 24, new Color(.73f, .86f, .94f));
        captionText.alignment = TextAnchor.MiddleLeft;
        Place(caption, new Vector2(0, 1), new Vector2(Padding + 12, -Padding - 13), new Vector2(24, 26));
        var thermometer = gauge.Find("Thermometer"); if (thermometer != null) UnityEngine.Object.DestroyImmediate(thermometer.gameObject);
        var track = status.Find("HeatTrack") as RectTransform ?? Child(gauge, "HeatTrack");
        track.SetParent(gauge, false);
        Place(track, new Vector2(0, 0), new Vector2(132, Padding + 9), new Vector2(256, 18));
        Fill(track, new Color(.09f, .17f, .21f));
        var well = Child(track, "Well"); Stretch(well); well.offsetMin = Vector2.one * 2; well.offsetMax = -Vector2.one * 2;
        Fill(well, new Color(.015f, .025f, .035f)); well.SetAsFirstSibling();
        var fill = Child(well, "HeatFill");
        var oldFill = track.Find("HeatFill"); if (oldFill != null) UnityEngine.Object.DestroyImmediate(oldFill.gameObject);
        var image = Fill(fill, Cyan);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
        var recovery = Child(track, "RecoveryMarker");
        recovery.anchorMin = recovery.anchorMax = new Vector2(profile.RecoveryFraction, .5f);
        recovery.anchoredPosition = Vector2.zero; recovery.sizeDelta = new Vector2(2, 20);
        Fill(recovery, new Color(.75f, .88f, .96f, .7f));
        var state = status.Find("HeatStatus") as RectTransform ?? Child(gauge, "HeatStatus");
        state.SetParent(gauge, false);
        var label = state.GetComponent<Text>() ?? state.gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Style(label, 22, Cyan); label.text = ""; label.alignment = TextAnchor.MiddleRight;
        Place(state, new Vector2(1, 1), new Vector2(-Padding - 48, -Padding - 13), new Vector2(96, 26));
        Bind(hud, "heatFill", image); Bind(hud, "heatFrame", track.GetComponent<Image>()); Bind(hud, "heatStatus", label);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return new { configured = true, profile = path, rowHeight = RowHeight, margin = Margin, padding = Padding, weaponWidth = 88, heatCorner = "top-left", heatGaugeWidth = 264, scene.isDirty };
    }

    static void Counter(RectTransform row, float offset, float width, string abbreviation)
    {
        Place(row, new Vector2(.5f, 1), new Vector2(offset, -Margin - RowHeight / 2), new Vector2(width, RowHeight));
        Plate(row);
        var icon = row.Find("Icon").GetComponent<Text>(); icon.text = abbreviation; Style(icon, 28, icon.color);
        icon.alignment = TextAnchor.MiddleLeft;
        var symbolWidth = abbreviation.Length == 1 ? 20 : 38;
        Place(icon.rectTransform, new Vector2(0, .5f), new Vector2(Padding + symbolWidth / 2, 0), new Vector2(symbolWidth, RowHeight));
        var count = row.Find("Count").GetComponent<Text>(); Style(count, 32, Color.white);
        Place(count.rectTransform, new Vector2(1, .5f), new Vector2(-Padding - 18, 0), new Vector2(36, RowHeight));
    }
    static void Plate(RectTransform parent)
    {
        var plate = Child(parent, "Plate"); Stretch(plate);
        var old = plate.GetComponent<Graphic>(); if (old != null && !(old is Image)) UnityEngine.Object.DestroyImmediate(old);
        Fill(plate, Ink); plate.SetAsFirstSibling();
        var top = parent.Find("TopEdge"); if (top != null) UnityEngine.Object.DestroyImmediate(top.gameObject);
    }
    static RectTransform Child(Transform parent, string name)
    {
        var found = parent.Find(name) as RectTransform;
        if (found != null) return found;
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
    }
    static Image Fill(RectTransform rect, Color color)
    {
        var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false; return image;
    }
    static void Style(Text label, int size, Color color)
    {
        label.fontSize = size; label.color = color; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
    }
    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static void Bind(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
