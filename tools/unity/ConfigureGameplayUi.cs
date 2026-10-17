using System;
using System.Linq;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConfigureGameplayUi
{
    static readonly Color Ink = new Color(.035f, .065f, .11f, .97f);
    static readonly Color Cyan = new Color(.3f, .9f, 1f);
    static readonly Color Muted = new Color(.5f, .6f, .67f);
    static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static object Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.isDirty || scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity")
            throw new InvalidOperationException("Open saved gameplay scene in Edit Mode first.");
        if (GameObject.Find("GameplayUI") != null)
        {
            BindInputActions();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return Inspect();
        }
        var player = GameObject.Find("Player");
        var runner = UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        var session = runner.gameObject.AddComponent<GameplaySession>();
        Bind(session, "runner", runner);
        Bind(session, "lifecycle", player.GetComponent<PlayerDamageLifecycle>());
        Bind(session, "enemyFireGate", UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>());
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        BindInputActions();
        var root = new GameObject("GameplayUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1;
        var safe = Rect("SafeArea", root.transform); Stretch(safe); safe.gameObject.AddComponent<SafeAreaLayout>();
        var lives = Rect("Lives", safe); Place(lives, new Vector2(0, 1), new Vector2(94, -58), new Vector2(148, 68));
        Image("Plate", lives, Ink);
        var lifeIcon = Rect("LifeIcon", lives); Place(lifeIcon, new Vector2(0, .5f), new Vector2(34, 0), new Vector2(34, 40));
        var icon = lifeIcon.gameObject.AddComponent<Image>();
        icon.sprite = player.GetComponentInChildren<SpriteRenderer>(true).sprite; icon.color = Cyan; icon.preserveAspect = true; icon.raycastTarget = false;
        var livesLabel = Text("Count", lives, GameplayCopy.Lives(3), 34, Color.white);
        Place(livesLabel.rectTransform, new Vector2(1, .5f), new Vector2(-46, 0), new Vector2(74, 60));
        var score = Counter("Score", safe, new Vector2(-110, -58), "★", Color.white);
        var shard = Counter("Shard", safe, new Vector2(110, -58), "◆", new Color(1, .72f, .25f));
        var pause = Button("Pause", safe, "Ⅱ");
        Place((RectTransform)pause.transform, Vector2.one, new Vector2(-60, -58), new Vector2(76, 68));
        var weapon = Rect("Weapon", safe);
        Place(weapon, new Vector2(1, 0), new Vector2(-184, 52), new Vector2(324, 60));
        Image("Plate", weapon, Ink);
        var weaponLabel = Text("WeaponLabel", weapon, GameplayCopy.Weapon(1), 28, Cyan);
        Stretch(weaponLabel.rectTransform, 12);

        var announcementRoot = Rect("Announcement", safe);
        Place(announcementRoot, new Vector2(.5f, .58f), Vector2.zero, new Vector2(760, 104));
        announcementRoot.gameObject.AddComponent<CanvasGroup>();
        var announcementLabel = Text("Title", announcementRoot, "", 46, Cyan);
        Stretch(announcementLabel.rectTransform);
        var announcement = announcementRoot.gameObject.AddComponent<GameplayAnnouncement>();
        Bind(announcement, "label", announcementLabel);

        var overlay = Rect("Overlay", root.transform); Stretch(overlay);
        var backdrop = overlay.gameObject.AddComponent<Image>(); backdrop.color = new Color(.01f, .025f, .045f, .8f);
        var card = Rect("Card", overlay);
        Place(card, new Vector2(.5f, .5f), Vector2.zero, new Vector2(720, 600));
        card.gameObject.AddComponent<Image>().color = Ink;
        var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 28, 28); layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        var fitter = card.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var title = Text("Title", card, GameplayCopy.Pause, 40, Cyan); Height(title.gameObject, 96);
        var resume = Button("Resume", card, GameplayCopy.Resume); Height(resume.gameObject, 66);
        var restart = Button("RestartEncounter", card, GameplayCopy.RestartEncounter); Height(restart.gameObject, 66);
        var settings = Button("Settings", card, GameplayCopy.Settings); Height(settings.gameObject, 66);
        var menu = Button("MainMenu", card, GameplayCopy.MainMenu); Height(menu.gameObject, 66);
        var next = Button("Continue", card, GameplayCopy.Continue); Height(next.gameObject, 66);
        var note = Text("Unavailable", card, GameplayCopy.Unavailable, 25, Muted); Height(note.gameObject, 50);
        restart.interactable = settings.interactable = menu.interactable = next.interactable = false;
        var hud = root.AddComponent<GameplayHud>();
        Bind(hud, "session", session); Bind(hud, "lives", player.GetComponent<PlayerLives>());
        Bind(hud, "weapon", player.GetComponent<PulseLaserWeapon>());
        Bind(hud, "livesLabel", livesLabel); Bind(hud, "weaponLabel", weaponLabel);
        Bind(hud, "scoreLabel", score); Bind(hud, "shardLabel", shard); Bind(hud, "pauseButton", pause);
        Bind(hud, "overlay", overlay.gameObject); Bind(hud, "title", title);
        Bind(hud, "resumeButton", resume); Bind(hud, "restartButton", restart); Bind(hud, "settingsButton", settings);
        Bind(hud, "menuButton", menu); Bind(hud, "continueButton", next); Bind(hud, "unavailableLabel", note);
        Bind(hud, "announcement", announcement);
        overlay.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return Inspect();
    }

    static void BindInputActions()
    {
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
        if (actions == null) throw new InvalidOperationException("Missing project input actions.");
        UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>().actionsAsset = actions;
    }
    static Text Counter(string name, Transform parent, Vector2 offset, string symbol, Color color)
    {
        var row = Rect(name, parent); Place(row, new Vector2(.5f, 1), offset, new Vector2(180, 68));
        Image("Plate", row, Ink);
        var icon = Text("Icon", row, symbol, 34, color);
        Place(icon.rectTransform, new Vector2(0, .5f), new Vector2(40, 0), new Vector2(60, 60));
        var count = Text("Count", row, GameplayCopy.Placeholder, 32, Muted);
        Place(count.rectTransform, new Vector2(1, .5f), new Vector2(-54, 0), new Vector2(80, 60));
        return count;
    }
    static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    static void Stretch(RectTransform rect, float inset = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset; rect.offsetMax = -Vector2.one * inset;
    }
    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    static void Image(string name, Transform parent, Color color)
    {
        var rect = Rect(name, parent); Stretch(rect);
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
    }
    static Text Text(string name, Transform parent, string copy, int size, Color color)
    {
        var label = Rect(name, parent).gameObject.AddComponent<Text>(); label.font = Font;
        label.text = copy; label.fontSize = size; label.color = color; label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }
    static Button Button(string name, Transform parent, string copy)
    {
        var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.12f, .24f, .31f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(.65f, 1, 1);
        colors.disabledColor = new Color(.35f, .42f, .46f, .65f); button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var label = Text("Label", rect, copy, 30, Color.white); Stretch(label.rectTransform, 6);
        return button;
    }
    static void Height(GameObject root, float height) => root.AddComponent<LayoutElement>().preferredHeight = height;
    static void Bind(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    public static object Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        var nodes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        int missingReferences = 0;
        foreach (var component in nodes.SelectMany(node => node.GetComponents<Component>()).Where(c => c != null))
        {
            var iterator = new SerializedObject(component).GetIterator();
            while (iterator.NextVisible(true))
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == null &&
                    iterator.objectReferenceEntityIdValue != default) missingReferences++;
        }
        return new { missingReferences, canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>() != null,
            hud = UnityEngine.Object.FindAnyObjectByType<GameplayHud>() != null,
            events = UnityEngine.Object.FindObjectsByType<EventSystem>().Length,
            missingScripts = nodes.Sum(node => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject)),
            vietnameseGlyphs = GameplayCopy.MissionClear.All(c => Font.HasCharacter(c)), scene.isDirty };
    }
}
