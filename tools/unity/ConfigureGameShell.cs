using System;
using System.Collections.Generic;
using System.Linq;
using StarShift.Dev;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConfigureGameShell
{
    static readonly Color Ink = new Color(.025f, .045f, .085f, .98f);
    static readonly Color Panel = new Color(.055f, .095f, .15f, .98f);
    static readonly Color Cyan = new Color(.36f, .9f, 1f);
    static readonly Color Muted = new Color(.68f, .77f, .84f);
    static readonly List<Button> Backs = new List<Button>();
    static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    public static object Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.isDirty || scene.path != "Assets/StarShift/Scenes/02_Gameplay.unity") throw new InvalidOperationException("Open saved Gameplay scene in Edit Mode.");
        var runner = UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        if (runner.GetComponent<GameShell>() != null) { EnsureRecoveryAction(); EnsureHangar(); PolishPreview(); PolishProduct(); RemoveLegacyOverlay(); return Inspect(); }
        Backs.Clear();
        var canvas = new GameObject("GameShellUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 30;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 1;
        var view = canvas.AddComponent<ShellView>();
        var safe = Rect("SafeArea", canvas.transform); Stretch(safe); safe.gameObject.AddComponent<SafeAreaLayout>();
        view.Screens = new GameObject[8];
        for (int i = 1; i < view.Screens.Length; i++) view.Screens[i] = Screen(((ShellScreen)i).ToString(), safe).gameObject;
        var menu = view.Screens[(int)ShellScreen.Menu].transform;
        var title = Text("Title", menu, "STARSHIFT", 84, Color.white); Place(title.rectTransform, .12f, .69f, 0, 30, 650, 110, new Vector2(0,.5f)); title.alignment = TextAnchor.MiddleLeft;
        view.MenuStatus = Text("CampaignStatus", menu, "", 25, Muted); Place(view.MenuStatus.rectTransform,.12f,.48f,0,0,590,120,new Vector2(0,.5f)); view.MenuStatus.alignment=TextAnchor.MiddleLeft;
        view.Wallet = Text("Wallet", menu, "", 24, new Color(1,.78f,.4f)); Place(view.Wallet.rectTransform,.12f,.28f,0,0,590,60,new Vector2(0,.5f)); view.Wallet.alignment=TextAnchor.MiddleLeft;
        var menuButtons = Rect("Actions", menu); Place(menuButtons,.76f,.5f,0,0,490,544); Stack(menuButtons, 16, 12);
        view.Continue = Button("Continue",menuButtons,ShellCopy.Continue,72);
        view.NewGame = Button("NewGame",menuButtons,ShellCopy.NewGame,72);
        view.Missions = Button("Missions",menuButtons,ShellCopy.Missions,64);
        view.Help = Button("Help",menuButtons,ShellCopy.Help,64);
        view.Settings = Button("Settings",menuButtons,GameplayCopy.Settings,64);
        view.Credits = Button("Credits",menuButtons,ShellCopy.Credits,64);
        // Các điểm sáng là hình tạm độc lập với artwork, chuyển động dùng unscaled time.
        var stars = Rect("Stars",menu); Stretch(stars); stars.SetAsFirstSibling();
        for(int i=0;i<38;i++) {var star=Rect("Star"+i,stars);Place(star,Mathf.Repeat(i*.618034f,1),Mathf.Repeat(i*.414214f,1),0,0,i%3==0?3:2,i%3==0?3:2);star.gameObject.AddComponent<Image>().color=new Color(.4f,.7f,.85f,.26f);star.GetComponent<Image>().raycastTarget=false;}
        stars.gameObject.AddComponent<ShellStarfield>();

        var hub = view.Screens[(int)ShellScreen.Missions].transform;
        Header(hub,ShellCopy.Missions);
        var a=Text("RegionA",hub,"REGION A",25,Cyan);Place(a.rectTransform,.06f,.77f,0,0,400,50,new Vector2(0,.5f));a.alignment=TextAnchor.MiddleLeft;
        var b=Text("RegionB",hub,"REGION B",25,Cyan);Place(b.rectTransform,.06f,.43f,0,0,400,50,new Vector2(0,.5f));b.alignment=TextAnchor.MiddleLeft;
        view.MissionCards=new Button[8];view.MissionLabels=new Text[8];
        for(int i=0;i<8;i++)
        {
            var button=Button("Mission"+(i+1).ToString("00"),hub,ShellCopy.Mission(i),0);
            Place((RectTransform)button.transform,.17f+.22f*(i%4),i<4?.64f:.30f,0,0,300,180);
            view.MissionCards[i]=button;view.MissionLabels[i]=button.GetComponentInChildren<Text>();view.MissionLabels[i].fontSize=25;
        }
        view.MissionDetail=Text("Selection",hub,"",23,Muted);Place(view.MissionDetail.rectTransform,.06f,.10f,0,0,820,65,new Vector2(0,.5f));view.MissionDetail.alignment=TextAnchor.MiddleLeft;
        view.LaunchReplay=Button("LaunchReplay",hub,ShellCopy.Replay,0);Place((RectTransform)view.LaunchReplay.transform,.94f,.10f,0,0,510,72,new Vector2(1,.5f));
        Back(hub);

        var settings = view.Screens[(int)ShellScreen.Settings].transform; Header(settings,GameplayCopy.Settings); Back(settings);
        view.Music=Slider(settings,ShellCopy.Music,.68f);
        view.Sfx=Slider(settings,ShellCopy.Sfx,.51f);
        view.Vibration=Toggle(settings,ShellCopy.Vibration,.35f);
        view.Shake=Toggle(settings,ShellCopy.Shake,.23f);
        view.SaveSettings=Button("SaveSettings",settings,ShellCopy.SaveSettings,0);Place((RectTransform)view.SaveSettings.transform,.5f,.10f,0,0,590,72);
        Info(view.Screens[(int)ShellScreen.Help].transform,ShellCopy.Help,string.Join("\n\n", ShellCopy.HelpBlocks),27);
        Info(view.Screens[(int)ShellScreen.Credits].transform,ShellCopy.Credits,ShellCopy.CreditsText,30);

        var pause=view.Screens[(int)ShellScreen.Pause].transform;
        var pauseCard=Card(pause,700,650); Stack(pauseCard,36,16);
        Height(Text("Title",pauseCard,GameplayCopy.Pause,44,Cyan).gameObject,76);
        view.PauseDetail=Text("Detail",pauseCard,"",25,Muted);Height(view.PauseDetail.gameObject,92);
        view.Resume=Button("Resume",pauseCard,GameplayCopy.Resume,72);
        view.Retry=Button("Retry",pauseCard,GameplayCopy.RestartEncounter,72);
        view.PauseSettings=Button("Settings",pauseCard,GameplayCopy.Settings,72);
        view.PauseMenu=Button("MainMenu",pauseCard,GameplayCopy.MainMenu,72);

        var result=view.Screens[(int)ShellScreen.Results].transform;
        var resultCard=Card(result,1120,760);Stack(resultCard,36,12);
        view.ResultTitle=Text("Title",resultCard,"",44,Cyan);Height(view.ResultTitle.gameObject,66);
        view.ResultMission=Text("Mission",resultCard,"",26,Muted);Height(view.ResultMission.gameObject,72);
        view.ResultStats=Text("Stats",resultCard,"",32,Color.white);Height(view.ResultStats.gameObject,165);
        view.ResultRecord=Text("Record",resultCard,"",25,new Color(1,.78f,.4f));Height(view.ResultRecord.gameObject,72);
        view.ResultStatus=Text("SaveStatus",resultCard,"",24,Muted);Height(view.ResultStatus.gameObject,64);
        var row=Rect("PrimaryActions",resultCard);Height(row.gameObject,72);Row(row);
        view.Next=Button("NextMission",row,ShellCopy.Next,72);
        view.ResultReplay=Button("Replay",row,ShellCopy.Replay,72);
        view.GameOverRetry=Button("Retry",row,GameplayCopy.RestartEncounter,72);
        view.ResultRetrySave=Button("RetrySave",row,ShellCopy.RetrySave,72);
        view.ResultMissions=Button("Missions",row,ShellCopy.Missions,72);
        view.ResultMenu=Button("MainMenu",resultCard,GameplayCopy.MainMenu,72);

        view.Confirmation=Screen("Confirmation",canvas.transform).gameObject;
        var confirm=Card(view.Confirmation.transform,880,440);Stack(confirm,36,24);
        view.ConfirmationText=Text("Message",confirm,"",31,Color.white);Height(view.ConfirmationText.gameObject,210);
        var confirmRow=Rect("Actions",confirm);Height(confirmRow.gameObject,72);Row(confirmRow);
        view.Cancel=Button("Cancel",confirmRow,ShellCopy.Cancel,72);view.Confirm=Button("Confirm",confirmRow,ShellCopy.Confirm,72);
        view.Error=Screen("Error",canvas.transform).gameObject;
        var error=Card(view.Error.transform,900,420);Stack(error,36,24);
        view.ErrorText=Text("Message",error,"",31,Color.white);Height(view.ErrorText.gameObject,190);
        var errorRow=Rect("Actions",error);Height(errorRow.gameObject,72);Row(errorRow);
        view.ErrorBack=Button("Back",errorRow,ShellCopy.Back,72);view.ErrorRetry=Button("Retry",errorRow,"THỬ LẠI",72);
        view.Loading=Screen("Loading",canvas.transform).gameObject;view.Fade=view.Loading.AddComponent<CanvasGroup>();
        var loading=Text("Message",view.Loading.transform,ShellCopy.Loading,36,Cyan);Place(loading.rectTransform,.5f,.5f,0,0,900,140);
        view.BackButtons=Backs.ToArray();
        var music= new GameObject("MusicChannel",typeof(AudioSource));music.transform.SetParent(runner.transform,false);
        music.GetComponent<AudioSource>().playOnAwake=false;music.GetComponent<AudioSource>().loop=true;
        var sfx=new GameObject("SfxChannel",typeof(AudioSource));sfx.transform.SetParent(runner.transform,false);sfx.GetComponent<AudioSource>().playOnAwake=false;
        var feedback=runner.gameObject.AddComponent<SettingsFeedback>();Bind(feedback,"music",music.GetComponent<AudioSource>());Bind(feedback,"sfx",sfx.GetComponent<AudioSource>());
        var shell=runner.gameObject.AddComponent<GameShell>();Bind(shell,"view",view);Bind(shell,"session",runner.GetComponent<GameplaySession>());Bind(shell,"pointer",UnityEngine.Object.FindAnyObjectByType<InputSystemPointerInput>());Bind(shell,"feedback",feedback);
        var dev=new SerializedObject(runner.GetComponent<DevFastStart>());dev.FindProperty("devModeEnabled").boolValue=false;dev.ApplyModifiedPropertiesWithoutUndo();
        var debug=runner.GetComponent<EncounterDebugView>();if(debug!=null)debug.enabled=false;
        foreach(var screen in view.Screens.Skip(1))screen.SetActive(false);
        view.Confirmation.SetActive(false);view.Error.SetActive(false);view.Loading.SetActive(false);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene.path,true)};
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        EnsureRecoveryAction();
        EnsureHangar();
        PolishPreview();
        PolishProduct();
        RemoveLegacyOverlay();
        return Inspect();
    }
    static void EnsureHangar()
    {
        var view=UnityEngine.Object.FindAnyObjectByType<ShellView>();
        if(view.Hangar!=null)return;
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerDamagePresentation>();
        var hull=player.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="HullPlaceholder");
        if(!AssetDatabase.IsValidFolder("Assets/StarShift/Data/Cosmetics"))AssetDatabase.CreateFolder("Assets/StarShift/Data","Cosmetics");
        var catalog=ScriptableObject.CreateInstance<CosmeticCatalog>();
        var scout=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/Placeholders/scout-placeholder.png");
        catalog.Options=new[]{
            new CosmeticOption{Id="chassis-arrow",DisplayName="MŨI TÊN",Slot=CosmeticSlot.Chassis,IsDefault=true,Sprite=hull.sprite},
            new CosmeticOption{Id="chassis-swept",DisplayName="CÁNH QUÉT",Slot=CosmeticSlot.Chassis,ShardCost=12,Sprite=scout,FlipY=true},
            new CosmeticOption{Id="skin-cyan",DisplayName="LAM BĂNG",Slot=CosmeticSlot.Skin,IsDefault=true,Tint=hull.color},
            new CosmeticOption{Id="skin-amber",DisplayName="HỔ PHÁCH",Slot=CosmeticSlot.Skin,ShardCost=6,Tint=new Color(1,.58f,.22f)},
            new CosmeticOption{Id="trail-none",DisplayName="ĐỘNG CƠ TIÊU CHUẨN",Slot=CosmeticSlot.Trail,IsDefault=true,Tint=Color.clear},
            new CosmeticOption{Id="trail-ion",DisplayName="VỆT ION",Slot=CosmeticSlot.Trail,ShardCost=8,Tint=new Color(.4f,.9f,1,.8f)}
        };
        AssetDatabase.CreateAsset(catalog,"Assets/StarShift/Data/Cosmetics/ShipCosmetics.asset");
        var trailObject=new GameObject("CosmeticEngineTrail",typeof(TrailRenderer));trailObject.transform.SetParent(player.transform,false);trailObject.transform.localPosition=new Vector3(0,-.35f,0);
        var trail=trailObject.GetComponent<TrailRenderer>();trail.sharedMaterial=player.GetComponentsInChildren<TrailRenderer>(true).First(t=>t!=trail).sharedMaterial;
        trail.time=.25f;trail.startWidth=.14f;trail.endWidth=0;trail.minVertexDistance=.04f;trail.emitting=false;trail.sortingOrder=hull.sortingOrder-1;
        var appearance=player.gameObject.AddComponent<ShipAppearance>();Bind(appearance,"catalog",catalog);Bind(appearance,"persistence",runner.GetComponent<StarShift.Persistence.CampaignLifecycle>());Bind(appearance,"hull",hull);Bind(appearance,"engineTrail",trail);
        Bind(runner.GetComponent<GameShell>(),"cosmetics",catalog);Bind(runner.GetComponent<GameShell>(),"appearance",appearance);
        var menu=view.Screens[(int)ShellScreen.Menu].transform;
        foreach(var name in new[]{"Subtitle","Release"}){var old=menu.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
        var title=menu.Find("Title").GetComponent<Text>();title.fontSize=70;Place(title.rectTransform,.08f,.84f,0,0,730,100,new Vector2(0,.5f));
        var initialPreview=Rect("ShipPreview",menu);Place(initialPreview,.28f,.53f,0,0,430,350);
        view.MenuTrail=PreviewTrail(initialPreview);
        view.MenuShip=PreviewShip(initialPreview,hull.sprite);
        view.MenuShipName=Text("ShipName",menu,"",34,Color.white);Place(view.MenuShipName.rectTransform,.28f,.31f,0,0,650,55);
        view.MenuEquipped=Text("Equipped",menu,ShellCopy.Equipped,22,Cyan);Place(view.MenuEquipped.rectTransform,.28f,.265f,0,0,650,40);
        view.MenuStatus.alignment=TextAnchor.MiddleCenter;view.MenuStatus.fontSize=23;Place(view.MenuStatus.rectTransform,.28f,.20f,0,0,660,70);
        view.MenuNotice=Text("RecoveryNotice",menu,"",20,Muted);Place(view.MenuNotice.rectTransform,.28f,.75f,0,0,660,62);
        Place(view.Wallet.rectTransform,.28f,.125f,0,0,650,44);view.Wallet.alignment=TextAnchor.MiddleCenter;
        view.Hangar=Button("Hangar",menu,ShellCopy.Hangar,0);Place((RectTransform)view.Hangar.transform,.28f,.055f,0,0,480,60);
        var screens=view.Screens;Array.Resize(ref screens,9);view.Screens=screens;
        var hangar=Screen("Hangar",menu.parent);view.Screens[(int)ShellScreen.Hangar]=hangar.gameObject;
        Header(hangar,ShellCopy.Hangar);
        var back=Button("Back",hangar,ShellCopy.Back,0);Place((RectTransform)back.transform,.94f,.88f,0,0,250,64,new Vector2(1,.5f));view.BackButtons=view.BackButtons.Concat(new[]{back}).ToArray();
        var preview=Rect("ShipPreview",hangar);Place(preview,.25f,.50f,0,0,470,420);
        PreviewTrail(preview);PreviewShip(preview,hull.sprite);
        var initialName=Text("ShipName",hangar,"",34,Color.white);Place(initialName.rectTransform,.25f,.23f,0,0,600,65);
        var initialWallet=Text("Wallet",hangar,"",26,new Color(1,.78f,.4f));Place(initialWallet.rectTransform,.25f,.14f,0,0,600,60);
        view.CosmeticTabs=new Button[3];
        for(int i=0;i<3;i++){view.CosmeticTabs[i]=Button("Tab"+i,hangar,ShellCopy.CosmeticTabs[i],0);Place((RectTransform)view.CosmeticTabs[i].transform,.51f+i*.158f,.74f,0,0,245,78);view.CosmeticTabs[i].GetComponentInChildren<Text>().fontSize=23;}
        view.CosmeticCards=new Button[2];view.CosmeticLabels=new Text[2];
        for(int i=0;i<2;i++){view.CosmeticCards[i]=Button("Cosmetic"+i,hangar,"",0);Place((RectTransform)view.CosmeticCards[i].transform,.59f+i*.23f,.51f,0,0,330,210);view.CosmeticLabels[i]=view.CosmeticCards[i].GetComponentInChildren<Text>();}
        view.CosmeticDetail=Text("Detail",hangar,"",26,Muted);Place(view.CosmeticDetail.rectTransform,.71f,.29f,0,0,750,100);
        view.CosmeticAction=Button("Equip",hangar,"",0);Place((RectTransform)view.CosmeticAction.transform,.71f,.15f,0,0,750,80);
        hangar.gameObject.SetActive(false);EditorUtility.SetDirty(view);
    }
    static Image PreviewShip(Transform parent,Sprite sprite)
    {var rect=Rect("Ship",parent);Place(rect,.5f,.52f,0,0,250,280);var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;}
    static Image PreviewTrail(Transform parent)
    {var rect=Rect("Trail",parent);Place(rect,.5f,.13f,0,0,22,150);var image=rect.gameObject.AddComponent<Image>();image.color=Color.clear;image.raycastTarget=false;return image;}
    static void PolishPreview()
    {
        var view=UnityEngine.Object.FindAnyObjectByType<ShellView>();
        var presentation=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>());
        Bind(UnityEngine.Object.FindAnyObjectByType<ShipAppearance>(),"approachHull",presentation.FindProperty("ship").objectReferenceValue);
        foreach(var screen in new[]{ShellScreen.Menu,ShellScreen.Missions,ShellScreen.Settings,ShellScreen.Help,ShellScreen.Credits,ShellScreen.Hangar}) { var image=view.Screens[(int)screen].GetComponent<Image>();var color=image.color;color.a=1;image.color=color; }
        foreach(var trail in new[]{view.MenuTrail}) { var rect=trail.rectTransform;Place(rect,.5f,.25f,0,0,22,90); }
        var catalog=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/StarShift/Data/Cosmetics/ShipCosmetics.asset");catalog.Find("chassis-swept").FlipY=true;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        if(PrefabUtility.IsPartOfPrefabInstance(view.gameObject))PrefabUtility.ApplyPrefabInstance(view.gameObject,InteractionMode.AutomatedAction);
    }
    public static object FinishProduct()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        PolishProduct();RemoveLegacyOverlay();return Inspect();
    }
    static void PolishProduct()
    {
        const string path="Assets/StarShift/Prefabs/UI/GameShellUI.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try { PolishProductView(root.GetComponent<ShellView>());PrefabUtility.SaveAsPrefabAsset(root,path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        PolishHud();
    }
    static void PolishProductView(ShellView view)
    {
        var menu=view.Screens[(int)ShellScreen.Menu].transform;
        var hangar=view.Screens[(int)ShellScreen.Hangar].transform;
        if(view.ShipStage==null)
        {
            var stage=Rect("ShipStage",menu.parent);Stretch(stage);view.ShipStage=stage.gameObject;
            var preview=view.MenuShip.transform.parent;
            preview.SetParent(stage,false);Place((RectTransform)preview,.28f,.54f,0,0,580,420);
            var previewButton=preview.GetComponent<Button>();if(previewButton!=null)UnityEngine.Object.DestroyImmediate(previewButton);
            var backdrop=preview.GetComponent<Image>();if(backdrop!=null)UnityEngine.Object.DestroyImmediate(backdrop);
            var label=preview.Find("Label");if(label!=null)UnityEngine.Object.DestroyImmediate(label.gameObject);
            foreach(var item in new[]{view.MenuShipName,view.MenuEquipped,view.MenuStatus,view.Wallet})item.transform.SetParent(stage,false);
            foreach(var name in new[]{"ShipPreview","ShipName","Wallet"}) {var old=hangar.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            view.Store=Button("Store",view.Continue.transform.parent,ShellCopy.Store,64);
            view.Hangar.transform.SetParent(view.Continue.transform.parent,false);
            view.Hangar.gameObject.AddComponent<LayoutElement>().preferredHeight=64;
            view.Hangar.transform.SetSiblingIndex(2);view.Store.transform.SetSiblingIndex(3);
            view.Hangar.GetComponentInChildren<Text>().text=ShellCopy.Hangar;
            view.HangarMode=Button("HangarMode",hangar,ShellCopy.Hangar,0);Place((RectTransform)view.HangarMode.transform,.64f,.87f,0,0,230,64);
            view.StoreMode=Button("StoreMode",hangar,ShellCopy.Store,0);Place((RectTransform)view.StoreMode.transform,.81f,.87f,0,0,270,64);
            view.CosmeticContext=Text("ShipContext",hangar,"",25,Muted);Place(view.CosmeticContext.rectTransform,.73f,.71f,0,0,720,48);
            view.CosmeticSwatches=new Image[view.CosmeticCards.Length];
            for(int i=0;i<view.CosmeticCards.Length;i++)
            {
                var card=view.CosmeticCards[i];var swatch=Rect("Swatch",card.transform);Place(swatch,.5f,.72f,0,0,70,80);
                view.CosmeticSwatches[i]=swatch.gameObject.AddComponent<Image>();view.CosmeticSwatches[i].preserveAspect=true;view.CosmeticSwatches[i].raycastTarget=false;
            }
        }
        Place((RectTransform)view.MenuShip.transform.parent,.28f,.54f,0,0,580,420);
        Place(view.MenuShipName.rectTransform,.28f,.31f,0,0,650,52);
        Place(view.MenuEquipped.rectTransform,.28f,.265f,0,0,650,32);view.MenuEquipped.fontSize=20;
        Place(view.MenuStatus.rectTransform,.28f,.20f,0,0,660,70);
        Place(view.Wallet.rectTransform,.28f,.12f,0,0,650,48);
        var actions=(RectTransform)view.Continue.transform.parent;
        var utility=menu.Find("UtilityActions") as RectTransform;
        if(utility==null){utility=Rect("UtilityActions",menu);Row(utility);}
        foreach(var button in new[]{view.Help,view.Settings,view.Credits})
        {button.transform.SetParent(utility,false);button.GetComponentInChildren<Text>().fontSize=22;}
        Place(utility,.75f,.17f,0,0,560,68);
        var destinations=actions.Find("CosmeticDestinations") as RectTransform;
        if(destinations==null){destinations=Rect("CosmeticDestinations",actions);Row(destinations);Height(destinations.gameObject,76);}
        view.Hangar.transform.SetParent(destinations,false);view.Store.transform.SetParent(destinations,false);destinations.SetSiblingIndex(2);
        Place(actions,.75f,.54f,0,0,560,420);
        actions.GetComponent<VerticalLayoutGroup>().spacing=22;
        view.Continue.GetComponent<LayoutElement>().preferredHeight=84;
        view.Continue.GetComponent<Image>().color=new Color(.10f,.38f,.46f);
        view.Continue.GetComponentInChildren<Text>().fontSize=34;
        Place((RectTransform)menu.Find("Title"),.08f,.91f,0,0,730,100,new Vector2(0,.5f));
        Place(view.MenuNotice.rectTransform,.28f,.80f,0,0,660,64);
        var header=hangar.Find("Title");if(header!=null)header.gameObject.SetActive(false);
        // Tàu là ngữ cảnh chính; sơn và vệt động cơ nằm trong phần ngoại hình của tàu.
        Place((RectTransform)view.CosmeticTabs[0].transform,.73f,.78f,0,0,720,60);
        Place((RectTransform)view.CosmeticTabs[1].transform,.61f,.64f,0,0,330,60);
        Place((RectTransform)view.CosmeticTabs[2].transform,.85f,.64f,0,0,350,60);
        for(int i=0;i<view.CosmeticTabs.Length;i++)view.CosmeticTabs[i].GetComponentInChildren<Text>().text=ShellCopy.CosmeticTabs[i];
        for(int i=0;i<view.CosmeticCards.Length;i++)
        {
            Place((RectTransform)view.CosmeticCards[i].transform,.61f+i*.24f,.44f,0,0,340,220);
            Place(view.CosmeticLabels[i].rectTransform,.5f,.29f,0,0,316,90);view.CosmeticLabels[i].fontSize=22;
        }
        Place(view.CosmeticDetail.rectTransform,.73f,.24f,0,0,720,100);view.CosmeticDetail.fontSize=23;
        Place((RectTransform)view.CosmeticAction.transform,.73f,.11f,0,0,720,76);view.CosmeticAction.GetComponentInChildren<Text>().fontSize=25;
        foreach(var button in view.CosmeticTabs.Concat(view.CosmeticCards).Concat(view.MissionCards))
        {var outline=button.GetComponent<Outline>()??button.gameObject.AddComponent<Outline>();outline.effectColor=Cyan;outline.effectDistance=new Vector2(3,-3);outline.enabled=false;}
        // Back ở góc trái, tách hẳn hai điểm đến Xưởng và Cửa hàng.
        Place((RectTransform)view.BackButtons[4].transform,.06f,.88f,0,0,250,64,new Vector2(0,.5f));
        foreach(var toggle in new[]{view.Vibration,view.Shake})
        {
            toggle.transform.Find("Label").GetComponent<Text>().text=toggle==view.Vibration?ShellCopy.Vibration:ShellCopy.Shake;
            var check=(RectTransform)toggle.transform.Find("Check");Place(check,.94f,.5f,0,0,72,46);
            var mark=(RectTransform)check.Find("Mark");Stretch(mark,4);
            var state=toggle.transform.Find("State")?.GetComponent<Text>()??Text("State",toggle.transform,"",24,Cyan);
            Place(state.rectTransform,.82f,.5f,0,0,100,60);
            if(toggle==view.Vibration)view.VibrationState=state;else view.ShakeState=state;
        }
        var help=view.Screens[(int)ShellScreen.Help].transform;
        var body=help.Find("Body");if(body!=null)UnityEngine.Object.DestroyImmediate(body.gameObject);
        for(int i=0;i<4;i++)
        {
            var block=help.Find("Block"+i) as RectTransform;
            if(block==null)
            {
                block=Rect("Block"+i,help);block.gameObject.AddComponent<Image>().color=Panel;
                var title=Text("Title",block,ShellCopy.HelpTitles[i],27,Cyan);title.alignment=TextAnchor.MiddleLeft;
                Place(title.rectTransform,.5f,.81f,0,0,600,60);
                var copy=Text("Body",block,ShellCopy.HelpBlocks[i],26,Color.white);copy.alignment=TextAnchor.UpperLeft;
                Place(copy.rectTransform,.5f,.40f,0,0,600,155);
            }
            Place(block,.28f+(i%2)*.44f,.62f-(i/2)*.34f,0,0,660,280);
        }
        foreach(var text in view.GetComponentsInChildren<Text>(true))
        {
            if(text.text=="REGION A")text.text="KHU VỰC A";if(text.text=="REGION B")text.text="KHU VỰC B";
            if(text.text=="VỀ MENU CHÍNH")text.text=GameplayCopy.MainMenu;
        }
        EditorUtility.SetDirty(view);
    }
    static void PolishHud()
    {
        var hud=GameObject.Find("GameplayUI").GetComponent<GameplayHud>();
        var combat=GameObject.Find("GameplayUI").transform.Find("SafeArea/CombatHud");
        void HudText(string field,string copy,float width)
        {var text=(Text)new SerializedObject(hud).FindProperty(field).objectReferenceValue;text.text=copy;var rect=text.rectTransform;rect.sizeDelta=new Vector2(width,rect.sizeDelta.y);}
        HudText("weaponLabel","Pulse · CẤP 1",240);HudText("missileCount","TÊN LỬA 1/3",230);
        var weapon=(RectTransform)combat.Find("Weapon");Place(weapon,1,0,-140,32,264,52);
        var inventory=(Text)new SerializedObject(hud).FindProperty("weaponInventory").objectReferenceValue;Place(inventory.rectTransform,1,0,-285,68,550,28);inventory.text="";
        var missile=(Button)new SerializedObject(hud).FindProperty("missileButton").objectReferenceValue;
        Place((RectTransform)missile.transform,1,0,-140,112,264,72);
        var counters=(RectTransform)combat.Find("Counters");
        var counterLayout=counters.GetComponent<HorizontalLayoutGroup>();if(counterLayout!=null)UnityEngine.Object.DestroyImmediate(counterLayout);
        var counterFitter=counters.GetComponent<ContentSizeFitter>();if(counterFitter!=null)UnityEngine.Object.DestroyImmediate(counterFitter);
        Place(counters,.5f,1,0,-8,590,48);
        foreach(var name in new[]{"Score","Shard"})
        {
            var row=(RectTransform)combat.Find("Counters/"+name);Place(row,.5f,1,name=="Score"?-150:145,-30,280,48);
            var icon=row.Find("Icon").GetComponent<Text>();icon.text=name=="Score"?"ĐIỂM":"Stellar Shard";icon.fontSize=22;Place(icon.rectTransform,0,.5f,name=="Score"?42:80,0,name=="Score"?80:160,46);
            var count=row.Find("Count").GetComponent<Text>();Place(count.rectTransform,1,.5f,-60,0,110,46);count.fontSize=28;
        }
        var heatCaption=combat.Find("HeatGauge/Caption").GetComponent<Text>();heatCaption.text="NHIỆT";heatCaption.rectTransform.sizeDelta=new Vector2(90,26);heatCaption.rectTransform.anchoredPosition=new Vector2(50,-17);
        var effects=(Text)new SerializedObject(hud).FindProperty("effectLabel").objectReferenceValue;
        Place(effects.rectTransform,0,0,450,30,640,40);effects.alignment=TextAnchor.MiddleLeft;effects.fontSize=20;
        var cue=(Text)new SerializedObject(hud).FindProperty("pickupLabel").objectReferenceValue;
        Place(cue.rectTransform,0,0,326,82,620,40);cue.alignment=TextAnchor.MiddleLeft;cue.fontSize=24;
    }
    static void EnsureRecoveryAction()
    {
        var view=UnityEngine.Object.FindAnyObjectByType<ShellView>();
        if(view.ReloadSave==null)
        {
            view.ReloadSave=Button("ReloadSave",view.Screens[(int)ShellScreen.Menu].transform,ShellCopy.RetryRead,0);
            Place((RectTransform)view.ReloadSave.transform,.12f,.37f,0,0,440,64,new Vector2(0,.5f));
            view.ReloadSave.gameObject.SetActive(false);
        }
        if(view.RecoverSave==null)
        {
            view.RecoverSave=Button("RecoverSave",view.Screens[(int)ShellScreen.Menu].transform,ShellCopy.RecoverSave,0);
            Place((RectTransform)view.RecoverSave.transform,.12f,.27f,0,0,590,64,new Vector2(0,.5f));
            view.RecoverSave.gameObject.SetActive(false);
        }
        EditorUtility.SetDirty(view);
    }
    static void RemoveLegacyOverlay()
    {
        var old = GameObject.Find("GameplayUI")?.transform.Find("Overlay");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=GameObject.Find("GameShellUI");
        if(root!=null&&!PrefabUtility.IsPartOfPrefabInstance(root))
        {
            if(!AssetDatabase.IsValidFolder("Assets/StarShift/Prefabs/UI"))AssetDatabase.CreateFolder("Assets/StarShift/Prefabs","UI");
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,"Assets/StarShift/Prefabs/UI/GameShellUI.prefab",InteractionMode.AutomatedAction);
        }
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static RectTransform Screen(string name,Transform parent)
    {var rect=Rect(name,parent);Stretch(rect);rect.gameObject.AddComponent<Image>().color=Ink;return rect;}
    static RectTransform Card(Transform parent,float width,float height)
    {var rect=Rect("Card",parent);Place(rect,.5f,.5f,0,0,width,height);rect.gameObject.AddComponent<Image>().color=Panel;return rect;}
    static void Header(Transform parent,string copy)
    {var text=Text("Title",parent,copy,44,Cyan);Place(text.rectTransform,.06f,.88f,0,0,950,80,new Vector2(0,.5f));text.alignment=TextAnchor.MiddleLeft;}
    static void Back(Transform parent)
    {var button=Button("Back",parent,ShellCopy.Back,0);Place((RectTransform)button.transform,.94f,.88f,0,0,250,64,new Vector2(1,.5f));Backs.Add(button);}
    static void Info(Transform parent,string title,string copy,int size)
    {Header(parent,title);Back(parent);var body=Text("Body",parent,copy,size,Color.white);body.alignment=TextAnchor.UpperLeft;Place(body.rectTransform,.5f,.45f,0,0,1360,600);}
    static Slider Slider(Transform parent,string name,float y)
    {
        var label=Text(name,parent,name,30,Color.white);Place(label.rectTransform,.12f,y,0,0,540,70,new Vector2(0,.5f));label.alignment=TextAnchor.MiddleLeft;
        var rect=Rect(name+"Slider",parent);Place(rect,.73f,y,0,0,520,72);var hit=rect.gameObject.AddComponent<Image>();hit.color=new Color(.10f,.19f,.27f);
        var slider=rect.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.value=1;
        var track=Rect("Track",rect);track.anchorMin=new Vector2(0,.5f);track.anchorMax=new Vector2(1,.5f);track.offsetMin=new Vector2(22,-8);track.offsetMax=new Vector2(-22,8);track.gameObject.AddComponent<Image>().color=Muted;
        var handle=Rect("Handle",track);handle.sizeDelta=new Vector2(38,56);var graphic=handle.gameObject.AddComponent<Image>();graphic.color=Cyan;slider.handleRect=handle;slider.targetGraphic=graphic;
        var nav=slider.navigation;nav.mode=Navigation.Mode.Automatic;slider.navigation=nav;
        return slider;
    }
    static Toggle Toggle(Transform parent,string name,float y)
    {
        var rect=Rect(name,parent);Place(rect,.5f,y,0,0,1200,72);var hit=rect.gameObject.AddComponent<Image>();hit.color=Panel;
        var toggle=rect.gameObject.AddComponent<Toggle>();toggle.targetGraphic=hit;
        var label=Text("Label",rect,name,30,Color.white);Stretch(label.rectTransform,16);label.alignment=TextAnchor.MiddleLeft;
        var check=Rect("Check",rect);Place(check,.95f,.5f,0,0,48,48);check.gameObject.AddComponent<Image>().color=Muted;
        var mark=Rect("Mark",check);Stretch(mark,8);toggle.graphic=mark.gameObject.AddComponent<Image>();toggle.graphic.color=Cyan;
        return toggle;
    }
    static void Stack(RectTransform rect,int padding,float spacing)
    {var stack=rect.gameObject.AddComponent<VerticalLayoutGroup>();stack.padding=new RectOffset(padding,padding,padding,padding);stack.spacing=spacing;stack.childControlWidth=stack.childControlHeight=true;stack.childForceExpandHeight=false;}
    static void Row(RectTransform rect)
    {var row=rect.gameObject.AddComponent<HorizontalLayoutGroup>();row.spacing=16;row.childControlWidth=row.childControlHeight=true;row.childForceExpandWidth=true;}
    static RectTransform Rect(string name,Transform parent)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;}
    static void Stretch(RectTransform rect,float inset=0)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.one*inset;rect.offsetMax=-Vector2.one*inset;}
    static void Place(RectTransform rect,float x,float y,float dx,float dy,float w,float h,Vector2? pivot=null)
    {rect.anchorMin=rect.anchorMax=new Vector2(x,y);rect.pivot=pivot??new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(dx,dy);rect.sizeDelta=new Vector2(w,h);}
    static Text Text(string name,Transform parent,string copy,int size,Color color)
    {var text=Rect(name,parent).gameObject.AddComponent<Text>();text.font=Font;text.text=copy;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;}
    static Button Button(string name,Transform parent,string copy,float height)
    {
        var rect=Rect(name,parent);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.13f,.24f,.32f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.highlightedColor=new Color(.6f,.95f,1);colors.pressedColor=new Color(.3f,.8f,1);colors.selectedColor=new Color(.7f,1,1);colors.disabledColor=new Color(.4f,.45f,.5f,.8f);button.colors=colors;
        var label=Text("Label",rect,copy,28,Color.white);Stretch(label.rectTransform,12);
        if(height>0)Height(rect.gameObject,height);
        return button;
    }
    static void Height(GameObject target,float value)=>target.AddComponent<LayoutElement>().preferredHeight=value;
    static void Bind(UnityEngine.Object target,string field,UnityEngine.Object value)
    {var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    public static object Inspect()
    {
        var scene=SceneManager.GetActiveScene();
        var nodes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        int broken=0;
        foreach(var component in nodes.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null))
        {
            var property=new SerializedObject(component).GetIterator();
            while(property.NextVisible(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue==null&&property.objectReferenceEntityIdValue!=default)broken++;
        }
        var view=UnityEngine.Object.FindAnyObjectByType<ShellView>();
        int missingUi=view.GetType().GetFields().Count(f=>typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType)&&(UnityEngine.Object)f.GetValue(view)==null);
        var catalog=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/StarShift/Data/Cosmetics/ShipCosmetics.asset");
        if(catalog.Options.Select(o=>o.Id).Distinct().Count()!=catalog.Options.Length||catalog.Options.Any(o=>o.ShardCost<0||string.IsNullOrWhiteSpace(o.Id)||o.Slot==CosmeticSlot.Chassis&&o.Sprite==null)||
            Enum.GetValues(typeof(CosmeticSlot)).Cast<CosmeticSlot>().Any(slot=>catalog.Options.Count(o=>o.Slot==slot&&o.IsDefault)!=1))throw new InvalidOperationException("Invalid cosmetic catalog");
        bool playerDamageEnabled=UnityEngine.Object.FindAnyObjectByType<PlayerDamageLifecycle>().enabled;
        if(!playerDamageEnabled)throw new InvalidOperationException("Production player damage lifecycle is disabled");
        bool devMode=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<DevFastStart>()).FindProperty("devModeEnabled").boolValue;
        if(broken!=0||missingUi!=0||devMode||view.Screens.Length!=9||GameObject.Find("GameplayUI").transform.Find("Overlay")!=null)throw new InvalidOperationException("Shell wiring/production entry invalid");
        return new{scene=scene.path,entry=EditorBuildSettings.scenes[0].path,shell=UnityEngine.Object.FindAnyObjectByType<GameShell>()!=null,
            missingReferences=broken,missingUiReferences=missingUi,playerDamageEnabled,devMode,cosmetics=catalog.Options.Length,missingScripts=nodes.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),missions=UnityEngine.Object.FindAnyObjectByType<RegionCampaignFlow>().Missions.Length,scene.isDirty};
    }
}
