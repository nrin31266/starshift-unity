using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;

public static class ConfigureM8Production
{
    const string Art = "Assets/StarShift/Art/M8/";
    static Sprite S(string id) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + id + ".png") ?? throw new InvalidOperationException("Missing " + id);
    static Sprite[] Frames(string id) => AssetDatabase.LoadAllAssetsAtPath(Art + id + "-sheet.png").OfType<Sprite>().OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
    static void Set(UnityEngine.Object o, string field, UnityEngine.Object value)
    { var so = new SerializedObject(o); var p = so.FindProperty(field) ?? throw new Exception(o.name + ":" + field); p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Array(UnityEngine.Object o, string field, Sprite[] values)
    { if (values.Length == 0) throw new Exception("Empty frames " + field); var so = new SerializedObject(o); var p = so.FindProperty(field); p.arraySize = values.Length; for (int i=0;i<values.Length;i++) p.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void ColorField(UnityEngine.Object o,string field,Color color)
    { var so=new SerializedObject(o);so.FindProperty(field).colorValue=color;so.ApplyModifiedPropertiesWithoutUndo(); }
    static T Component<T>(GameObject root) where T:Component { var c=root.GetComponent<T>();return c!=null?c:root.AddComponent<T>(); }
    static SpriteRenderer Visual(GameObject root, string name, string id, Vector3 position, int order, float scale=1)
    {
        var t=root.transform.Find(name); if(t==null){t=new GameObject(name).transform;t.SetParent(root.transform,false);}
        var r=Component<SpriteRenderer>(t.gameObject);r.sprite=S(id);r.color=Color.white;r.sortingOrder=order;t.localPosition=position;t.localScale=Vector3.one*scale;return r;
    }
    static void Loop(SpriteRenderer r,string id) { var loop=Component<SpriteFrameLoop>(r.gameObject);Array(loop,"frames",Frames(id)); if(r.transform.parent != null && r.name.StartsWith("M8Engine"))Set(loop,"visibilitySource",r.transform.parent.GetComponent<SpriteRenderer>()); }
    static void Prefab(string path,Action<GameObject> edit)
    {
        path="Assets/StarShift/Prefabs/"+path+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try {edit(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static object Import()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        AssetDatabase.Refresh();int count=0;
        foreach(var file in Directory.GetFiles(Art,"*.png"))
        {
            var path=file.Replace('\\','/');var id=Path.GetFileNameWithoutExtension(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool sheet=id.EndsWith("-sheet");bool environment=id.Contains("background")||id.EndsWith("planet")||id=="parallax-debris";
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=sheet?SpriteImportMode.Multiple:SpriteImportMode.Single;
            importer.spritePixelsPerUnit=environment?21:id.StartsWith("ui-")||id=="hud-ship"?100:56.25f;
            importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=!id.Contains("background");importer.maxTextureSize=2048;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=Vector2.one*.5f;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            if(sheet)
            {
                var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);if(provider==null)throw new Exception("No sprite provider");provider.InitSpriteEditorDataProvider();
                var caps=provider.GetDataProvider<ISpriteFrameEditCapability>();
                foreach(var cap in new[]{EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteRect,EEditCapability.EditPivot,EEditCapability.EditSpriteName})if(caps==null||!caps.GetEditCapability().HasCapability(cap))throw new Exception("Missing capability "+cap);
                var ids=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();if(ids==null)throw new Exception("Missing name/file-ID provider");
                var old=provider.GetSpriteRects().ToDictionary(r=>r.name);importer.GetSourceTextureWidthAndHeight(out var width,out var height);
                int n=id=="explosion-sheet"?8:4;int columns=4;int w=width/columns;int h=id=="explosion-sheet"?height/2:height;
                var rects=Enumerable.Range(0,n).Select(i=>{var name=id.Replace("-sheet","")+"_"+i.ToString("D2");return new SpriteRect{name=name,rect=new Rect(i%columns*w,height-(i/columns+1)*h,w,h),alignment=SpriteAlignment.Center,pivot=Vector2.one*.5f,spriteID=old.TryGetValue(name,out var previous)?previous.spriteID:UnityEngine.GUID.Generate()};}).ToArray();
                provider.SetSpriteRects(rects);ids.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));provider.Apply();importer.SaveAndReimport();
                if(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count()!=n)throw new Exception("Frame count mismatch "+path);
            }
            count++;
        }
        return new { imported=count };
    }
    static void Damage(GameObject root,string id)
    {
        var damage=root.GetComponent<EnemyDamagePresentation>();if(damage==null)return;
        if(new[]{"heavy","bulwark","carrier","vanguard","warden"}.Contains(id))
        {var so=new SerializedObject(damage);so.FindProperty("criticalHealthFraction").floatValue=.25f;so.ApplyModifiedPropertiesWithoutUndo();}
        Set(damage,"damagedSprite",S(id+"-damaged"));Set(damage,"criticalSprite",S(id+"-critical"));ColorField(damage,"damagedTint",Color.white);ColorField(damage,"criticalTint",Color.white);
        var feedback=Component<EnemyHitPresentation>(root);Set(feedback,"sparks",Visual(root,"M8Hit","hit-sparks",Vector3.zero,12));Set(feedback,"smoke",Visual(root,"M8Smoke","smoke-debris",new Vector3(.1f,.3f,0),7,.6f));Array(feedback,"hitFrames",Frames("hit-sparks"));Array(feedback,"smokeFrames",Frames("smoke-debris"));
    }
    static void Hull(GameObject root,string id)
    {
        var all=root.GetComponentsInChildren<SpriteRenderer>(true);var hull=all.First(r=>r.name=="HullPlaceholder");
        hull.sprite=S(id);hull.color=Color.white;hull.transform.localScale=Vector3.one/root.transform.localScale.x;hull.transform.localRotation=Quaternion.identity;
        var fire=root.GetComponent<EnemyFirePresentation>();foreach(var r in all)
        {
            if(r.name=="CorePlaceholder") {r.enabled=fire!=null;if(fire!=null){r.sprite=S("enemy-telegraph");r.color=new Color(1,1,1,0);r.transform.localScale=Vector3.one/root.transform.localScale.x;ColorField(fire,"chargedTint",Color.white);}}
            if(r.name.StartsWith("EnginePod")||r.name=="LongBarrel"||r.name=="HangarRail"||r.name=="EnginePlume")r.enabled=false;
            if(r.name=="EliteAccent"){r.sprite=S("elite-accent");r.transform.localScale=Vector3.one;}
            if(r.name=="EntryWarning"){r.sprite=S("ui-boss-warning");r.transform.localScale=Vector3.one*.7f;r.transform.localRotation=Quaternion.identity;}
        }
        Damage(root,id);
        if(id=="carrier")
        {
            var bay=Component<CarrierBayPresentation>(root);
            Set(bay,"leftBay",Visual(hull.gameObject,"M8LeftBay","carrier-bay-loop",new Vector3(-.65f,-.25f,0),7,.8f));
            Set(bay,"rightBay",Visual(hull.gameObject,"M8RightBay","carrier-bay-loop",new Vector3(.65f,-.25f,0),7,.8f));Array(bay,"frames",Frames("carrier-bay-loop"));
        }
        if(id!="supply-drone"&&id!="warden-drone"&&id!="resource-asteroid"&&id!="asteroid-medium")
        {
            for(int sign=-1;sign<=1;sign+=2)
            {
                float y=S(id).bounds.size.y*.37f;var r=Visual(hull.gameObject,"M8Engine"+sign,"engine-loop",new Vector3(sign*.25f,y,0),4,.45f);r.transform.localRotation=Quaternion.Euler(0,0,180);Loop(r,"engine-loop");
            }
        }
    }
    public static object Fleet()
    {
        EditMode();
        foreach(var pair in new[]{new[]{"Scout","scout"},new[]{"Raider","raider"},new[]{"Bomber","heavy"},new[]{"Bulwark","bulwark"},new[]{"Sniper","sniper"},new[]{"Interceptor","interceptor"},new[]{"Carrier","carrier"},new[]{"WardenDrone","warden-drone"}})Prefab("Enemies/"+pair[0],r=>Hull(r,pair[1]));
        Prefab("Player/Player",Player);
        Player(GameObject.Find("Player"));
        var catalog=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/StarShift/Data/Cosmetics/ShipCosmetics.asset");
        catalog.Find("chassis-swept").Sprite=S("player-swept");catalog.Find("chassis-swept").FlipY=false;
        catalog.Default(CosmeticSlot.Chassis).Sprite=S("player");catalog.Default(CosmeticSlot.Chassis).FlipY=false;
        catalog.Find("skin-cyan").Tint=Color.white;catalog.Find("skin-amber").Tint=new Color(1,.87f,.66f);
        EditorUtility.SetDirty(catalog);Save();return "Fleet and player bound";
    }
    static void Player(GameObject root)
    {
        var hull=root.transform.Find("Visual/HullPlaceholder").GetComponent<SpriteRenderer>();hull.sprite=S("player");hull.color=Color.white;hull.transform.localScale=Vector3.one;
        foreach(int sign in new[]{-1,1}){var r=Visual(hull.gameObject,"M8Engine"+sign,"engine-loop",new Vector3(sign*.23f,-.55f,0),9,.4f);Loop(r,"engine-loop");}
        foreach(var r in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if(r.name=="SupportDrone"){r.sprite=S("support-companion");r.color=Color.white;r.transform.localScale=Vector3.one;}
            if(r.name=="ShieldCue"){r.sprite=S("shield-activate-break");r.color=new Color(1,1,1,.75f);r.transform.localScale=Vector3.one*1.5f;Loop(r,"shield-activate-break");}
            if(r.name=="MissileFlight"){r.sprite=S("reserve-missile");r.color=Color.white;r.transform.localScale=Vector3.one;}
            if(r.name=="MissileFlash")r.sprite=S("missile-splash");
        }
        var weapon=root.GetComponent<PulseLaserWeapon>();var muzzle=(Transform)new SerializedObject(weapon).FindProperty("muzzle").objectReferenceValue;
        var feedback=Component<PlayerCombatPresentation>(root);Set(feedback,"muzzle",Visual(root,"M8Muzzle","muzzle-flash",muzzle.localPosition,12,.65f));Set(feedback,"reward",Visual(root,"M8Collect","pickup-collect-upgrade",Vector3.zero,12,1.5f));Set(feedback,"survival",Visual(root,"M8Survival","death-respawn",Vector3.zero,12,1.5f));Array(feedback,"muzzleFrames",Frames("muzzle-flash"));Array(feedback,"rewardFrames",Frames("pickup-collect-upgrade"));Array(feedback,"survivalFrames",Frames("death-respawn"));
    }
    public static object BossesWorld()
    {
        EditMode();
        foreach(var family in new[]{"vanguard","warden"})Prefab("Enemies/"+(family=="vanguard"?"VanguardFrigate":"NebulaWarden"),root=>{
            var hull=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="HullPlaceholder");hull.sprite=S(family+"-hull");hull.color=Color.white;hull.transform.localScale=Vector3.one/root.transform.localScale.x;Damage(root,family);
            foreach(var r in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string id=null;
                if(r.name.StartsWith("CapitalWing"))id=family+"-wing";
                else if(r.name.StartsWith("EngineMass"))id=family+"-engine";
                else if(r.name=="Reactor")id=family+"-reactor";
                else if(r.name=="BeamPod")id=family+"-beam-pod";
                else if(r.name=="MissilePod")id=family+"-missile-pod";
                else if(r.name=="BeamNodeAttack")id=family+"-beam-node";
                else if(r.name=="DroneNode")id=family+"-drone-node";
                else if(r.name=="PlasmaNode")id=family+"-plasma-node";
                else if(r.name=="ShieldNetwork")id="warden-shield-phase";
                else if(r.name.StartsWith("ShieldBreak"))id="shield-activate-break";
                else if(r.name.StartsWith("EngineGlow"))id="engine-loop";
                else if(r.name.StartsWith("DeathBeat"))id="missile-splash";
                else if(r.name=="PhaseCue"||r.name=="ShelterBarrage"||r.sprite!=null&&r.sprite.name.Contains("impact-glow-placeholder"))id="attack-telegraphs";
                if(id==null)continue;
                if(r.GetComponent<Collider2D>()!=null)
                {
                    r.transform.localScale=r.name.StartsWith("CapitalWing")?new Vector3(4.375f,2.0625f,1):new Vector3(.95f,.9f,1);
                    var child=Visual(r.gameObject,"M8Art",id,Vector3.zero,r.sortingOrder);
                    var world=r.transform.lossyScale;child.transform.localScale=new Vector3(1/world.x,1/world.y,1/world.z);
                    child.flipX=r.name.EndsWith("-1");r.sprite=r.name.StartsWith("CapitalWing")?null:S("enemy-telegraph");r.color=new Color(1,1,1,0);
                    continue;
                }
                r.sprite=S(id);
                if(id.StartsWith(family+"-")||id=="engine-loop"){r.color=Color.white;r.transform.localScale=Vector3.one/root.transform.localScale.x;}
                if(r.name.StartsWith("CapitalWing")){r.flipX=r.name.EndsWith("-1");}
                if(r.name.StartsWith("EngineGlow")){r.transform.localRotation=Quaternion.Euler(0,0,180);Loop(r,"engine-loop");}
                if(r.name=="Reactor"){r.transform.localScale*=.55f;Loop(r,"warden-core-loop");}
                if(r.name.StartsWith("DeathBeat"))Loop(r,"missile-splash");
                if(r.name.StartsWith("ShieldBreak"))Loop(r,"shield-activate-break");
            }
            if(family=="vanguard")
            {
                for(int sign=-1;sign<=1;sign+=2)Visual(root,"M8Truss"+sign,"vanguard-truss",new Vector3(sign*1.7f,.25f,0),2);
            }
            if(family=="warden")
            {
                foreach(var name in new[]{"BeamNodeAttack","DroneNode","PlasmaNode"})
                {
                    var node=root.transform.Find(name);var offset=node.localPosition;
                    var brace=Visual(root,"M8Truss"+name,"warden-truss",offset*.5f,2);
                    brace.transform.localScale=Vector3.one/root.transform.localScale.x;
                    brace.transform.localRotation=Quaternion.FromToRotation(Vector3.right,offset.normalized);
                }
                for(int sign=-1;sign<=1;sign+=2){var r=Visual(root,"M8ArmorWing"+sign,"warden-wing",new Vector3(sign*2.65f,.25f,0),3,.65f);r.flipX=sign<0;}
                var core=Visual(root,"M8Core","warden-core",Vector3.zero,6,.42f);Loop(core,"warden-core-loop");
            }
        });
        Prefab("Enemies/SupplyDrone",r=>{var h=r.GetComponentsInChildren<SpriteRenderer>(true).First(x=>x.name=="HullPlaceholder");h.sprite=S("supply-drone");h.transform.localScale=Vector3.one;h.color=Color.white;foreach(var v in r.GetComponentsInChildren<SpriteRenderer>(true))if(v.name=="CorePlaceholder")v.enabled=false;});
        Prefab("Enemies/StormAsteroid",r=>{Rock(r,"asteroid-medium");Array(r.GetComponent<AsteroidVisualMotion>(),"sizeSprites",new[]{S("asteroid-small"),S("asteroid-medium"),S("asteroid-large"),S("asteroid-huge")});});Prefab("Enemies/ResourceAsteroid",r=>Rock(r,"resource-asteroid"));
        Save();return "Boss modules and world prefabs bound";
    }
    static void Rock(GameObject root,string id)
    {
        foreach(var r in root.GetComponentsInChildren<SpriteRenderer>(true))
        {if(r.name=="HullPlaceholder"){r.sprite=S(id);r.color=Color.white;var size=r.sprite.bounds.size; r.transform.localScale=Vector3.one*(id=="resource-asteroid"?1:1.1f);}else if(r.name=="CorePlaceholder")r.enabled=false;else if(r.name=="EntryWarning"){r.sprite=S("ui-boss-warning");r.transform.localScale=Vector3.one*.8f;r.transform.localRotation=Quaternion.identity;}else if(r.name=="EliteAccent"){r.sprite=S("elite-accent");r.transform.localScale=Vector3.one;}}
    }
    static string PickupId(PickupDefinition d)=>d.Kind==PickupKind.Shard?"stellar-shard":d.Kind==PickupKind.WeaponCore?d.Branch==WeaponBranch.Pulse?"pulse-core":d.Branch==WeaponBranch.Plasma?"plasma-core":"homing-core":d.Kind==PickupKind.Shield?"shield-cell":d.Kind==PickupKind.Overdrive?"overdrive":d.Kind==PickupKind.SupportDrone?"support-drone":d.Kind==PickupKind.MissileAmmo?"missile-ammo":"extra-life";
    public static object CombatRewards()
    {
        EditMode();
        foreach(var guid in AssetDatabase.FindAssets("t:PickupDefinition",new[]{"Assets/StarShift/Data"}))
        {var d=AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(guid));var id=PickupId(d);d.Sprite=S(id);d.AnimationFrames=Frames(id);EditorUtility.SetDirty(d);}
        foreach(var guid in AssetDatabase.FindAssets("t:PulseLaserDefinition",new[]{"Assets/StarShift/Data"}))
        {var d=AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>(AssetDatabase.GUIDToAssetPath(guid));string id=d.Branch==WeaponBranch.Pulse?"player-pulse":d.Branch==WeaponBranch.Plasma?"player-plasma":"player-homing";Set(d,"projectileSprite",S(id));ColorField(d,"projectileColor",Color.white);var so=new SerializedObject(d);so.FindProperty("projectileScale").vector2Value=Vector2.one;so.ApplyModifiedPropertiesWithoutUndo();}
        Prefab("Projectiles/VanguardCurtainProjectile",root=>{var r=root.GetComponentInChildren<SpriteRenderer>(true);r.sprite=S("boss-curtain");r.color=Color.white;r.transform.localScale=Vector3.one;});
        Prefab("Rewards/RewardPickup",root=>{var r=root.GetComponentInChildren<SpriteRenderer>(true);r.sprite=S("legacy-crate");r.color=Color.white;});
        Save();return "Weapon and reward art/animations bound";
    }
    static Image Icon(Transform parent,string name,string id,Vector2 anchor,Vector2 position,Vector2 size)
    {
        var t=parent.Find(name) as RectTransform;if(t==null){t=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();t.SetParent(parent,false);}
        t.anchorMin=t.anchorMax=anchor;t.pivot=Vector2.one*.5f;t.anchoredPosition=position;t.sizeDelta=size;
        var image=t.GetComponent<Image>();image.sprite=S(id);image.color=Color.white;image.raycastTarget=false;image.preserveAspect=true;return image;
    }
    static void Ui(GameObject root)
    {
        foreach(var image in root.GetComponentsInChildren<Image>(true))
        {
            if(image.name=="LifeIcon"){image.sprite=S("ui-life");image.color=Color.white;}
            if(image.name=="ShardIcon"||image.name=="Icon"&&image.transform.parent.name=="Shard"){image.sprite=S("ui-shard");image.color=Color.white;}
            if(image.name=="PauseIcon")image.sprite=S("ui-icon-pause");
            if(image.name=="Card"||image.name.StartsWith("Block")||image.name=="Plate"){image.sprite=S("ui-panel");image.type=Image.Type.Sliced;image.color=Color.white;}
        }
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {if(button.targetGraphic is Image image){image.sprite=S("ui-button");image.type=Image.Type.Sliced;image.color=Color.white;}var c=button.colors;c.normalColor=Color.white;c.selectedColor=new Color(.7f,1,1);c.highlightedColor=new Color(.85f,1,1);c.pressedColor=new Color(.55f,.7f,.8f);c.disabledColor=new Color(.4f,.5f,.6f,.6f);button.colors=c;}
        var hud=root.GetComponentInChildren<GameplayHud>(true);if(hud!=null)
        {
            var so=new SerializedObject(hud);var label=(Text)so.FindProperty("weaponLabel").objectReferenceValue;
            var icon=Icon(label.transform.parent,"M8WeaponIcon","ui-weapon-pulse",new Vector2(0,.5f),new Vector2(28,0),new Vector2(28,28));Set(hud,"weaponIcon",icon);Array(hud,"weaponIcons",new[]{S("ui-weapon-pulse"),S("ui-weapon-plasma"),S("ui-weapon-homing")});
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(42,0);label.rectTransform.offsetMax=new Vector2(-6,0);label.fontSize=24;label.alignment=TextAnchor.MiddleCenter;
            var effects=(Text)so.FindProperty("effectLabel").objectReferenceValue;
            for(int i=0;i<3;i++){var img=Icon(effects.transform,"M8Status"+i,new[]{"ui-shield","ui-overdrive","ui-support"}[i],new Vector2(1,.5f),new Vector2(30+i*32,0),new Vector2(26,26));Set(hud,new[]{"shieldIcon","overdriveIcon","supportIcon"}[i],img);}
            var missile=(Button)so.FindProperty("missileButton").objectReferenceValue;Icon(missile.transform,"M8AmmoIcon","ui-ammo",new Vector2(0,.5f),new Vector2(22,0),new Vector2(26,26));
            var shard=(Text)so.FindProperty("shardLabel").objectReferenceValue;Icon(shard.transform.parent,"M8ShardIcon","ui-shard",new Vector2(0,.5f),new Vector2(16,0),new Vector2(26,26));
        }
        var view=root.GetComponent<ShellView>();if(view!=null)
        {
            view.MenuShip.sprite=S("player");view.MenuShip.color=Color.white;
            foreach(var screen in view.Screens.Where(s=>s!=null))
            {
                var title=screen.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Title"||t.name=="ResultTitle");
                if(title!=null && title.transform.Find("M8HeaderMark")!=null)UnityEngine.Object.DestroyImmediate(title.transform.Find("M8HeaderMark").gameObject);
            }
        }
    }
    public static object FrontendEnvironment()
    {
        EditMode();
        Prefab("UI/GameShellUI",Ui);var ui=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();foreach(var root in ui)if(root.GetComponentInChildren<Canvas>(true)!=null)Ui(root);
        foreach(var root in ui)foreach(var r in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if(r.sprite==null)continue;
            if(r.name=="ApproachShip"){r.sprite=S("player");r.color=Color.white;for(int sign=-1;sign<=1;sign+=2){var engine=Visual(r.gameObject,"M8Engine"+sign,"engine-loop",new Vector3(sign*.23f,-.55f,0),r.sortingOrder-1,.4f);Loop(engine,"engine-loop");}}
            if(r.name=="Planet"){r.sprite=S("astra-planet");r.color=Color.white;}
            if(r.name=="AsteroidLeft"||r.name=="AsteroidRight"){r.sprite=S("parallax-debris");r.color=new Color(.35f,.45f,.6f,.3f);r.transform.localScale=Vector3.one*(r.name=="AsteroidLeft"?.28f:.20f);}
            if(r.name=="Star" || r.sprite.name.Contains("star-placeholder"))r.sprite=S("star");
        }
        var intro=UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>();Set(intro,"session",UnityEngine.Object.FindAnyObjectByType<GameplaySession>());Set(intro,"astraPlanet",S("astra-planet"));Set(intro,"crimsonPlanet",S("crimson-planet"));Set(intro,"farBackground",GameObject.Find("Main Camera/M8Environment").GetComponent<SpriteRenderer>());Set(intro,"astraBackground",S("background"));Set(intro,"crimsonBackground",S("crimson-background"));
        var soIntro=new SerializedObject(intro);soIntro.FindProperty("planetStartScale").floatValue=.8f;soIntro.FindProperty("planetEndScale").floatValue=1.03f;soIntro.ApplyModifiedPropertiesWithoutUndo();
        Camera.main.clearFlags=CameraClearFlags.SolidColor;Camera.main.backgroundColor=new Color(8/255f,15/255f,32/255f,1);
        Save();return "Shared UI and regional environment bound";
    }
    static void EditMode(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");}
    static void Save(){AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();}
}
