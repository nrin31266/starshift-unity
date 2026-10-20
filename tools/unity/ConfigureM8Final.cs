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

public static class ConfigureM8Final
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

    static void Objects(UnityEngine.Object target,string field,IEnumerable<UnityEngine.Object> values)
    { var so=new SerializedObject(target);var p=so.FindProperty(field);var all=values.ToArray();p.arraySize=all.Length;for(int i=0;i<all.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=all[i];so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SpriteEngines(ShipAppearance appearance,SpriteRenderer hull,bool approach)
    {
        foreach(var renderer in hull.GetComponentsInChildren<SpriteRenderer>(true))
            if(renderer!=hull&&renderer.name.StartsWith("M8Engine"))UnityEngine.Object.DestroyImmediate(renderer.gameObject);
        var plumes=new List<SpriteRenderer>();
        for(int i=0;i<2;i++)
        {
            var plume=Visual(hull.gameObject,"M8Engine"+i,"engine-loop",Vector3.zero,hull.sortingOrder-1,.32f);
            plume.transform.localRotation=Quaternion.identity;plumes.Add(plume);
        }
        if(appearance!=null)Objects(appearance,approach?"approachPlumes":"enginePlumes",plumes);
    }
    static Image PreviewImage(Transform parent,string name)
    {
        var t=parent.Find(name)as RectTransform;
        if(t==null){t=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<RectTransform>();t.SetParent(parent,false);}
        t.anchorMin=t.anchorMax=Vector2.one*.5f;t.pivot=Vector2.one*.5f;
        var image=t.GetComponent<Image>();image.raycastTarget=false;image.color=Color.white;return image;
    }
    static void Preview(Image image)
    {
        image.preserveAspect=true;
        var preview=Component<ShipPreview>(image.gameObject);
        foreach(var name in new[]{"Engine0","Engine1","Ribbon0","Ribbon1","MovementRibbon"})
        {
            var old=image.transform.Find(name);
            if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        var engines=new Image[2];
        for(int i=0;i<2;i++){engines[i]=PreviewImage(image.transform,"Engine"+i);engines[i].sprite=S("engine-loop");}
        Set(preview,"Hull",image);
        Objects(preview,"Engines",engines);
        Objects(preview,"Ribbons",System.Array.Empty<Image>());
    }
    static void PlayerRoot(GameObject root)
    {
        var hull=root.transform.Find("Visual/HullPlaceholder").GetComponent<SpriteRenderer>();
        hull.sprite=S("player");hull.color=Color.white;hull.transform.localScale=Vector3.one;hull.transform.localRotation=Quaternion.identity;
        var appearance=root.GetComponent<ShipAppearance>();
        SpriteEngines(appearance,hull,false);
        if(appearance==null)return;
        var so=new SerializedObject(appearance);var approach=(SpriteRenderer)so.FindProperty("approachHull").objectReferenceValue;
        if(approach!=null){approach.sprite=S("player");SpriteEngines(appearance,approach,true);}
        var motionTrail=(TrailRenderer)so.FindProperty("movementTrail").objectReferenceValue;
        if(motionTrail!=null)
        {
            motionTrail.startWidth=.07f;motionTrail.endWidth=0;motionTrail.time=.12f;motionTrail.sortingOrder=hull.sortingOrder-3;
        }
    }
    static void PlayerUi(GameObject root)
    {
        var view=root.GetComponentInChildren<ShellView>(true);if(view==null)return;
        if(view.CosmeticCards.Length<3)
        {
            var copy=UnityEngine.Object.Instantiate(view.CosmeticCards[1],view.CosmeticCards[1].transform.parent);copy.name="Cosmetic2";
            view.CosmeticCards=view.CosmeticCards.Concat(new[]{copy}).ToArray();
            view.CosmeticLabels=view.CosmeticLabels.Concat(new[]{copy.GetComponentInChildren<Text>(true)}).ToArray();
            view.CosmeticSwatches=view.CosmeticSwatches.Concat(new[]{copy.transform.Find("Swatch").GetComponent<Image>()}).ToArray();
        }
        for(int i=0;i<view.CosmeticCards.Length;i++)
        {
            var rect=(RectTransform)view.CosmeticCards[i].transform;rect.anchorMin=rect.anchorMax=new Vector2(.55f+i*.165f,.44f);rect.sizeDelta=new Vector2(240,220);
            view.CosmeticSwatches[i].rectTransform.sizeDelta=new Vector2(80,100);
            Preview(view.CosmeticSwatches[i]);
        }
        view.MenuShip.rectTransform.anchoredPosition=new Vector2(0,60);
        Preview(view.MenuShip);view.MenuTrail.enabled=false;
        EditorUtility.SetDirty(view);
    }
    public static object Player()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        AssetDatabase.Refresh();
        foreach(var id in new[]{"player","player-swept","player-amber","player-swept-amber"})AssetDatabase.ImportAsset(Art+id+".png",ImportAssetOptions.ForceUpdate);
        var catalog=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/StarShift/Data/Cosmetics/ShipCosmetics.asset");
        catalog.EngineFrames=Frames("engine-loop");
        foreach(var spec in new[]{new[]{"chassis-arrow","MŨI GIÁO","player"},new[]{"chassis-swept","ƯNG KÍCH","player-swept"}})
        {
            var c=catalog.Find(spec[0]);c.DisplayName=spec[1];c.Sprite=S(spec[2]);c.AmberSprite=S(spec[2]+"-amber");c.FlipY=false;
            c.EngineSockets=spec[0]=="chassis-arrow"
                ?new[]{new Vector2(-10/56.25f,-28/56.25f),new Vector2(9/56.25f,-28/56.25f)}
                :new[]{new Vector2(-9/56.25f,-32/56.25f),new Vector2(9/56.25f,-32/56.25f)};
            c.MovementTrailSocket=new Vector2(0,-.3f);c.MuzzleSocket=new Vector2(0,.75f);c.PlumeScale=.32f;
        }
        catalog.Find("skin-amber").Tint=Color.white;
        var rainbow=catalog.Find("trail-rainbow");if(rainbow==null){rainbow=new CosmeticOption{Id="trail-rainbow",DisplayName="CẦU VỒNG",Slot=CosmeticSlot.Trail,ShardCost=8};catalog.Options=catalog.Options.Concat(new[]{rainbow}).ToArray();}
        foreach(var id in new[]{"trail-none","trail-ion","trail-rainbow"})
        {var c=catalog.Find(id);c.AnimationFrames=Frames(id=="trail-none"?"trail-standard":id);c.Sprite=c.AnimationFrames[0];c.Tint=id=="trail-rainbow"?new Color(.85f,.65f,1,.5f):new Color(.25f,.85f,1,.6f);c.TrailSeconds=id=="trail-none"?.12f:.22f;}
        EditorUtility.SetDirty(catalog);
        Prefab("Player/Player",PlayerRoot);PlayerRoot(GameObject.Find("Player"));
        Prefab("UI/GameShellUI",PlayerUi);
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())PlayerUi(root);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        return new{chassis=2,trails=3,cards=3,perChassisSockets=true};
    }


    static UnityEngine.Object Ref(UnityEngine.Object target,string field)=>new SerializedObject(target).FindProperty(field).objectReferenceValue;
    static void Position(RectTransform t,Vector2 anchor,Vector2 offset,Vector2 size)
    {t.anchorMin=t.anchorMax=anchor;t.pivot=Vector2.one*.5f;t.anchoredPosition=offset;t.sizeDelta=size;}
    static Image Icon(Transform parent,string name,string id,Vector2 anchor,Vector2 offset,Vector2 size)
    {var r=PreviewImage(parent,name);r.sprite=S(id);r.preserveAspect=true;Position(r.rectTransform,anchor,offset,size);return r;}
    static Text Timer(Transform parent,string name,Vector2 offset)
    {
        var t=parent.Find(name)as RectTransform;if(t==null){t=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<RectTransform>();t.SetParent(parent,false);}
        var label=t.GetComponent<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/StarShift/UI/Fonts/VT323-Regular.ttf");label.fontSize=26;label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;label.raycastTarget=false;
        Position(t,Vector2.one*.5f,offset,new Vector2(64,28));return label;
    }
    static void SkinUi(GameObject root)
    {
        var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/StarShift/UI/Fonts/VT323-Regular.ttf");
        foreach(var text in root.GetComponentsInChildren<Text>(true))
        {
            text.font=font;text.fontStyle=FontStyle.Normal;text.resizeTextForBestFit=false;
            if(text.fontSize<26)text.fontSize=26;
            text.verticalOverflow=VerticalWrapMode.Truncate;
        }
        foreach(var image in root.GetComponentsInChildren<Image>(true))
        {
            if(image.name=="Plate"||image.name=="Card"||image.name.StartsWith("Block"))
            {image.sprite=S("ui-panel");image.type=Image.Type.Sliced;image.color=Color.white;}
        }
        var view=root.GetComponentInChildren<ShellView>(true);
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            if(!(button.targetGraphic is Image image))continue;
            bool card=view!=null&&view.CosmeticCards.Contains(button);
            bool tab=view!=null&&(view.CosmeticTabs.Contains(button)||button==view.HangarMode||button==view.StoreMode);
            bool primary=view!=null&&(button==view.Continue||button==view.NewGame||button==view.CosmeticAction||button==view.Next||button==view.LaunchReplay||button==view.Confirm);
            image.sprite=S(card?"ui-card":tab?"ui-tab":primary?"ui-button":"ui-button-secondary");image.type=Image.Type.Sliced;image.color=Color.white;
            var c=button.colors;c.normalColor=Color.white;c.highlightedColor=new Color(.8f,1,1);c.selectedColor=new Color(.65f,1,1);c.pressedColor=new Color(.45f,.68f,.78f);c.disabledColor=new Color(.43f,.5f,.6f,.6f);button.colors=c;
            Component<UiButtonMotion>(button.gameObject);
        }
        if(view!=null)
        {
            view.MenuShip.rectTransform.anchoredPosition=new Vector2(0,90);
            for(int i=0;i<3;i++)Position((RectTransform)view.CosmeticTabs[i].transform,new Vector2(.565f+i*.165f,.78f),Vector2.zero,new Vector2(240,54));
            foreach(var modal in new[]{view.Confirmation,view.Error,view.Loading})
            {
                foreach(var plate in modal.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Card"||i.name=="Plate"))
                {plate.sprite=S("ui-modal");plate.type=Image.Type.Sliced;}
            }
        }
        var hud=root.GetComponentInChildren<GameplayHud>(true);if(hud==null)return;
        var level=(Text)Ref(hud,"weaponLabel");var weapon=level.transform.parent;
        Position((RectTransform)weapon,new Vector2(1,0),new Vector2(-94,44),new Vector2(164,72));
        var weaponIcon=(Image)Ref(hud,"weaponIcon");Position(weaponIcon.rectTransform,new Vector2(0,.5f),new Vector2(38,0),new Vector2(48,48));
        level.rectTransform.anchorMin=level.rectTransform.anchorMax=new Vector2(1,.5f);level.rectTransform.pivot=Vector2.one*.5f;level.rectTransform.anchoredPosition=new Vector2(-42,0);level.rectTransform.sizeDelta=new Vector2(72,56);level.fontSize=34;
        foreach(var p in weapon.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Plate"))p.color=Color.clear;
        var missile=(Button)Ref(hud,"missileButton");
        Position((RectTransform)missile.transform,new Vector2(1,0),new Vector2(-94,126),new Vector2(164,72));((Image)missile.targetGraphic).color=new Color(1,1,1,0);
        Icon(missile.transform,"M8AmmoIcon","ui-ammo",new Vector2(0,.5f),new Vector2(38,0),new Vector2(48,48));
        var ammo=(Text)Ref(hud,"missileCount");ammo.rectTransform.anchorMin=ammo.rectTransform.anchorMax=new Vector2(1,.5f);ammo.rectTransform.anchoredPosition=new Vector2(-42,0);ammo.rectTransform.sizeDelta=new Vector2(78,56);ammo.fontSize=34;
        var effect=((Image)Ref(hud,"shieldIcon")).transform.parent;
        foreach(var pair in new[]{new[]{"shieldIcon","ui-shield"},new[]{"overdriveIcon","ui-overdrive"},new[]{"supportIcon","ui-support"}}.Select((v,i)=>(v,i)))
        {var img=(Image)Ref(hud,pair.v[0]);img.sprite=S(pair.v[1]);Position(img.rectTransform,Vector2.one*.5f,new Vector2((pair.i-1)*80,10),new Vector2(40,40));}
        Set(hud,"overdriveTimer",Timer(effect,"OverdriveTimer",new Vector2(0,-25)));
        Set(hud,"supportTimer",Timer(effect,"SupportTimer",new Vector2(80,-25)));
        var lives=(Text)Ref(hud,"livesLabel");foreach(var p in lives.transform.parent.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Plate"))p.color=Color.clear;
        var life=lives.transform.parent.Find("LifeIcon").GetComponent<Image>();life.sprite=S("ui-life");Position(life.rectTransform,new Vector2(0,.5f),new Vector2(24,0),new Vector2(40,40));lives.fontSize=36;
        foreach(var field in new[]{"scoreLabel","shardLabel"})
        {
            var count=(Text)Ref(hud,field);var parent=count.transform.parent;
            Position((RectTransform)parent,new Vector2(field=="scoreLabel"?.48f:.65f,1),new Vector2(0,-36),new Vector2(210,56));
            foreach(var p in parent.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Plate"))p.color=Color.clear;
            foreach(var label in parent.GetComponentsInChildren<Text>(true).Where(t=>t!=count))label.text=field=="scoreLabel"?"ĐIỂM":"";
            count.fontSize=34;
            if(field=="shardLabel")Icon(parent,"M8ShardIcon","ui-shard",new Vector2(0,.5f),new Vector2(46,0),new Vector2(36,36));
        }
        var heat=(Image)Ref(hud,"heatFrame");var gauge=heat.transform.parent;
        foreach(var p in gauge.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Plate"))p.color=Color.clear;
        heat.sprite=S("ui-heat-housing");heat.type=Image.Type.Sliced;heat.color=Color.white;
        var fill=(Image)Ref(hud,"heatFill");fill.sprite=S("ui-heat-fill");fill.type=Image.Type.Tiled;
        var boss=root.GetComponentInChildren<BossHud>(true);if(boss!=null)
        {var b=(Image)Ref(boss,"fill");b.sprite=S("ui-heat-fill");b.type=Image.Type.Filled;b.fillMethod=Image.FillMethod.Horizontal;}
    }
    public static object Ui()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        foreach(var id in new[]{"ui-panel","ui-button","ui-button-secondary","ui-tab","ui-card","ui-modal","ui-heat-housing"})
        {var importer=(TextureImporter)AssetImporter.GetAtPath(Art+id+".png");importer.spriteBorder=id=="ui-heat-housing"?new Vector4(6,4,6,4):new Vector4(6,6,6,6);importer.SaveAndReimport();}
        foreach(var guid in AssetDatabase.FindAssets("t:PickupDefinition",new[]{"Assets/StarShift/Data"}))
        {var item=AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(guid));if(item.Kind==PickupKind.Shard)continue;item.VisualScale=1.2f;EditorUtility.SetDirty(item);}
        Prefab("UI/GameShellUI",SkinUi);
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())SkinUi(root);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        return new{ui="mechanical pixel",pickupVisualScale=1.2f,minimumPixelsAt900=52*1.2f*900/(19.2f*56.25f),font="VT323"};
    }

}
