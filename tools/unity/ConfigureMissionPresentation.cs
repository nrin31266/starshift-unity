using System;
using System.Linq;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ConfigureMissionPresentation
{
    public static object TuneMinorHandoff()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/StarShift/Data/Levels/L1.asset");
        var outgoing = level.Encounters[0];
        outgoing.HandoffDelay = 1.7f;
        outgoing.ProjectileGraceSeconds = .65f;
        outgoing.ProjectileFadeSeconds = .3f;
        var incoming = level.Encounters[1];
        incoming.TransitionTitle = GameplayCopy.Encounter(2);
        incoming.TransitionDescription = "Tín hiệu địch đang tiến vào khu vực.";
        foreach (var encounter in level.Encounters)
        {
            EditorUtility.SetDirty(encounter);
            AssetDatabase.SaveAssetIfDirty(encounter);
        }
        return new { outgoing.HandoffDelay, outgoing.ProjectileGraceSeconds, outgoing.ProjectileFadeSeconds,
            incoming.TransitionTitle, incoming.TransitionDescription, level.Intro.ContinueDelaySeconds };
    }

    static void Bind(UnityEngine.Object target,string field,UnityEngine.Object value)
    {
        var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static RectTransform Rect(string name,Transform parent)
    {
        var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);return rt;
    }
    static void Place(RectTransform rt,Vector2 anchor,Vector2 size)
    {rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=size;}
    static Text Label(string name,Transform parent,string copy,int size,Color color)
    {
        var text=Rect(name,parent).gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text=copy;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        return text;
    }
    public static object Configure()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/StarShift/Scenes/02_Gameplay.unity")throw new InvalidOperationException("Gameplay scene required.");
        var ui=GameObject.Find("GameplayUI");var safe=ui.transform.Find("SafeArea");var hud=ui.GetComponent<GameplayHud>();
        bool migrate=safe.Find("ContinueHint")==null;
        var group=safe.Find("CombatHud") as RectTransform;
        if(group==null)
        {
            group=Rect("CombatHud",safe);group.anchorMin=Vector2.zero;group.anchorMax=Vector2.one;group.offsetMin=group.offsetMax=Vector2.zero;
            group.gameObject.AddComponent<CanvasGroup>();
            foreach(var name in new[] {"Lives","Score","Shard","Pause","Weapon"})safe.Find(name).SetParent(group,false);
        }
        Bind(hud,"gameplayHud",group.GetComponent<CanvasGroup>());
        var tap=ui.GetComponent<Image>();if(tap==null)tap=ui.AddComponent<Image>();
        tap.color=Color.clear;tap.raycastTarget=false;Bind(hud,"establishingTapArea",tap);
        var hint=safe.Find("ContinueHint");
        if(hint==null)
        {
            hint=safe.Find("BeginMission/Label");
            if(hint==null)hint=Label("ContinueHint",safe,GameplayCopy.TapToContinue,23,new Color(.55f,.68f,.74f,.7f)).transform;
            hint.SetParent(safe,false);hint.name="ContinueHint";
        }
        var oldButton=safe.Find("BeginMission");if(oldButton!=null)UnityEngine.Object.DestroyImmediate(oldButton.gameObject);
        var hintText=hint.GetComponent<Text>();hintText.text=GameplayCopy.TapToContinue;hintText.fontSize=23;
        hintText.color=new Color(.55f,.68f,.74f,.7f);hintText.raycastTarget=false;
        Place(hint as RectTransform,new Vector2(.5f,.08f),new Vector2(600,48));
        Bind(hud,"continueHint",hintText);hint.gameObject.SetActive(false);
        var announcement=safe.Find("Announcement");
        var announcementRect=announcement as RectTransform;
        Place(announcementRect,new Vector2(.065f,.4f),new Vector2(1000,160));announcementRect.pivot=new Vector2(0,.5f);
        var headline=announcement.Find("Title").GetComponent<Text>();headline.alignment=TextAnchor.MiddleLeft;headline.fontSize=42;
        Place(headline.rectTransform,new Vector2(0,1),new Vector2(1000,70));headline.rectTransform.pivot=new Vector2(0,1);
        var detail=announcement.Find("Description") ?? safe.Find("MissionBriefing");
        if(detail==null)detail=Label("Description",announcement,"",25,new Color(.6f,.72f,.79f)).transform;
        detail.SetParent(announcement,false);detail.name="Description";
        detail.gameObject.SetActive(true);
        var detailText=detail.GetComponent<Text>();detailText.alignment=TextAnchor.MiddleLeft;detailText.raycastTarget=false;
        Place(detail as RectTransform,Vector2.zero,new Vector2(1000,70));(detail as RectTransform).pivot=Vector2.zero;
        Bind(announcement.GetComponent<GameplayAnnouncement>(),"description",detailText);
        var presentation=UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>();
        var player=GameObject.Find("Player");var source=player.GetComponentInChildren<SpriteRenderer>(true);
        var ship=presentation.transform.Find("ApproachShip");
        if(ship==null)
        {
            ship=new GameObject("ApproachShip").transform;ship.SetParent(presentation.transform,false);
            var renderer=ship.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=source.sprite;renderer.color=source.color;renderer.sortingOrder=2;
            ship.localScale=source.transform.lossyScale;
        }
        Bind(presentation,"ship",ship.GetComponent<SpriteRenderer>());Bind(presentation,"playerVisual",player.transform.Find("Visual").gameObject);
        Bind(presentation,"playerRoot",player.transform);Bind(presentation,"gameplayBackdrop",GameObject.Find("EnvironmentPlaceholders"));
        if(migrate)
        {
            var visualSettings=new SerializedObject(presentation);
            visualSettings.FindProperty("planetStartScale").floatValue=3.2f;
            visualSettings.FindProperty("planetEndScale").floatValue=4.1f;visualSettings.ApplyModifiedPropertiesWithoutUndo();
            var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/StarShift/Data/Levels/L1.asset");
            level.Intro.ArrivalTitle=$"Mission {level.MissionNumber:00}";
            EditorUtility.SetDirty(level);
            level.Encounters[0].HandoffDelay=1.7f;EditorUtility.SetDirty(level.Encounters[0]);
            var next=level.Encounters[1];next.TransitionTitle=GameplayCopy.Encounter(2);
            next.TransitionDescription="Tín hiệu địch đang tiến vào khu vực.";EditorUtility.SetDirty(next);
        }
        var currentLevel=AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/StarShift/Data/Levels/L1.asset");
        if(Mathf.Approximately(currentLevel.Intro.TravelSeconds,1.1f))
        {currentLevel.Intro.TravelSeconds=1.8f;EditorUtility.SetDirty(currentLevel);}
        // Chỉ migrate seed cũ; giữ nguyên copy đã được author riêng.
        if(currentLevel.Intro.ArrivalTitle=="VÙNG XUNG ĐỘT")
        {currentLevel.Intro.ArrivalTitle=$"Mission {currentLevel.MissionNumber:00}";EditorUtility.SetDirty(currentLevel);}
        if(currentLevel.Encounters[1].TransitionTitle=="TĂNG VIỆN ĐỊCH")
        {currentLevel.Encounters[1].TransitionTitle=GameplayCopy.Encounter(2);EditorUtility.SetDirty(currentLevel.Encounters[1]);}
        presentation.Hide();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return new {configured=true,migrate,scene.isDirty,hint=hint.name,locationRemainsTbd=string.IsNullOrWhiteSpace(AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/StarShift/Data/Levels/L1.asset").LocationName)};
    }
}
