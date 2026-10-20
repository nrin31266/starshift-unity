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
        var plumes=new List<SpriteRenderer>();var tails=new List<SpriteRenderer>();
        for(int i=0;i<2;i++)
        {
            var plume=Visual(hull.gameObject,"M8Engine"+(i==0?-1:1),"engine-loop",Vector3.zero,hull.sortingOrder-1,.6f);
            var old=plume.GetComponent<SpriteFrameLoop>();if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            plume.transform.localRotation=Quaternion.identity;
            plumes.Add(plume);tails.Add(Visual(hull.gameObject,"EngineRibbon"+i,"trail-standard",Vector3.zero,hull.sortingOrder-2,.6f));
        }
        if(appearance==null)return;
        Objects(appearance,approach?"approachPlumes":"enginePlumes",plumes);
        Objects(appearance,approach?"approachRibbons":"ribbons",tails);
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
        var preview=Component<ShipPreview>(image.gameObject);preview.Hull=image;
        preview.Engines=new Image[2];preview.Ribbons=new Image[2];
        for(int i=0;i<2;i++)
        {
            preview.Ribbons[i]=PreviewImage(image.transform,"Ribbon"+i);preview.Ribbons[i].sprite=S("trail-standard");
            preview.Engines[i]=PreviewImage(image.transform,"Engine"+i);preview.Engines[i].sprite=S("engine-loop");
        }
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
        var motionTrail=(TrailRenderer)so.FindProperty("engineTrail").objectReferenceValue;
        motionTrail.startWidth=.07f;motionTrail.endWidth=0;motionTrail.time=.16f;motionTrail.sortingOrder=hull.sortingOrder-3;
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
            bool arrow=spec[0]=="chassis-arrow";
            c.EngineSockets=new[]{new Vector2(arrow?-8.5f/56.25f:-15.5f/56.25f,arrow?-31f/56.25f:-23f/56.25f),new Vector2(arrow?8.5f/56.25f:15.5f/56.25f,arrow?-31f/56.25f:-23f/56.25f)};
            c.MuzzleSocket=new Vector2(0,.75f);c.PlumeScale=arrow?.42f:.48f;
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

}
