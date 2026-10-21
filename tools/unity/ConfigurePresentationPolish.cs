using System;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using StarShift.Rewards;
using StarShift.Enemies;

public static class ConfigurePresentationPolish
{
    static void Bind(UnityEngine.Object target,string field,UnityEngine.Object value)
    {
        var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static RectTransform Rect(Transform parent,string name)
    {
        var rect=parent.Find(name) as RectTransform;
        if(rect==null){rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);}
        return rect;
    }
    static Text Label(RectTransform rect,Text reference)
    {
        var text=rect.GetComponent<Text>()??rect.gameObject.AddComponent<Text>();
        text.font=reference.font;text.fontSize=24;text.fontStyle=FontStyle.Bold;text.color=Color.white;text.raycastTarget=false;
        return text;
    }
    public static object Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        AssetDatabase.Refresh();
        foreach(var guid in AssetDatabase.FindAssets("t:PickupDefinition",new[]{"Assets/StarShift/Data/Rewards"}))
        {
            var item=AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            item.DriftSpeed=item.Kind==PickupKind.Shard?3.5f:1;
            if(item.Kind==PickupKind.Shard){item.FallAcceleration=12.1f;item.MaximumFallSpeed=7;}
            EditorUtility.SetDirty(item);
        }
        var supply=AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/StarShift/Data/Enemies/SupplyDrone.asset");
        var supplySo=new SerializedObject(supply);supplySo.FindProperty("maxHp").intValue=6;supplySo.ApplyModifiedPropertiesWithoutUndo();
        var path="Assets/StarShift/Art/UI/ui-heat-housing.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteBorder=new Vector4(4,4,4,4);importer.SetTextureSettings(settings);importer.SaveAndReimport();
        var shell=UnityEngine.Object.FindAnyObjectByType<ShellView>(FindObjectsInactive.Include);
        if(shell.ResetAll==null)
        {
            var button=UnityEngine.Object.Instantiate(shell.SaveSettings,shell.SaveSettings.transform.parent);
            button.name="ResetAll";button.onClick=new Button.ButtonClickedEvent();shell.ResetAll=button;
        }
        var reset=shell.ResetAll.GetComponent<RectTransform>();reset.anchorMin=reset.anchorMax=new Vector2(.5f,.1f);
        reset.sizeDelta=new Vector2(350,72);reset.anchoredPosition=new Vector2(225,0);
        var save=shell.SaveSettings.GetComponent<RectTransform>();save.sizeDelta=new Vector2(380,72);save.anchoredPosition=new Vector2(-190,0);
        shell.ResetAll.GetComponentInChildren<Text>().text=ShellCopy.ResetAll;
        shell.ResetAll.GetComponent<Image>().color=new Color(.65f,.28f,.2f);
        shell.Wallet.alignment=TextAnchor.MiddleCenter;shell.Wallet.rectTransform.sizeDelta=new Vector2(180,48);
        shell.Wallet.color=new Color(1,.86f,.3f);
        var icon=Rect(shell.Wallet.transform,"CoinIcon");icon.anchorMin=icon.anchorMax=Vector2.one*.5f;icon.anchoredPosition=new Vector2(-50,0);icon.sizeDelta=new Vector2(40,40);
        shell.WalletIcon=icon.GetComponent<Image>()??icon.gameObject.AddComponent<Image>();
        shell.WalletIcon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/UI/ui-shard.png");shell.WalletIcon.preserveAspect=true;shell.WalletIcon.raycastTarget=false;
        EditorUtility.SetDirty(shell);

        var boss=UnityEngine.Object.FindAnyObjectByType<BossHud>(FindObjectsInactive.Include);
        var so=new SerializedObject(boss);var fill=(Image)so.FindProperty("fill").objectReferenceValue;
        var label=(Text)so.FindProperty("label").objectReferenceValue;
        var percent=Rect(boss.transform,"Percentage");percent.anchorMin=percent.anchorMax=new Vector2(1,0);percent.pivot=new Vector2(1,0);
        percent.anchoredPosition=new Vector2(0,-8);percent.sizeDelta=new Vector2(72,28);
        var percentage=Label(percent,label);percentage.alignment=TextAnchor.MiddleRight;percentage.verticalOverflow=VerticalWrapMode.Overflow;percentage.text="100%";Bind(boss,"percentage",percentage);
        var track=fill.transform.parent.GetComponent<RectTransform>();track.offsetMax=new Vector2(-84,track.offsetMax.y);
        track.anchorMax=new Vector2(track.anchorMax.x,.36f);fill.rectTransform.offsetMin=new Vector2(2,2);fill.rectTransform.offsetMax=new Vector2(-2,-2);
        var bossFrame=track.GetComponent<Image>();bossFrame.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);bossFrame.type=Image.Type.Sliced;bossFrame.color=Color.white;
        fill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/UI/ui-heat-fill.png");

        var hud=UnityEngine.Object.FindAnyObjectByType<GameplayHud>(FindObjectsInactive.Include);so=new SerializedObject(hud);
        var frame=(Image)so.FindProperty("heatFrame").objectReferenceValue;
        foreach(var notch in hud.GetComponentsInChildren<Image>(true).Where(i=>i.name.StartsWith("M8HeatNotch",StringComparison.Ordinal)))
            notch.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/UI/ui-heat-notch.png");
        var emptyScoreIcon=hud.transform.Find("SafeArea/CombatHud/Counters/Score/CounterIcon");
        if(emptyScoreIcon!=null)UnityEngine.Object.DestroyImmediate(emptyScoreIcon.gameObject);
        var heat=(Image)so.FindProperty("heatFill").objectReferenceValue;
        var state=(Text)so.FindProperty("heatStatus").objectReferenceValue;
        frame.type=Image.Type.Sliced;
        frame.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        heat.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/StarShift/Art/UI/ui-heat-fill.png");
        var obsolete=frame.transform.Find("RecoveryMarker");if(obsolete!=null)UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
        frame.rectTransform.sizeDelta=new Vector2(256,24);
        state.rectTransform.SetParent(frame.transform,false);state.rectTransform.anchorMin=state.rectTransform.anchorMax=Vector2.one;
        state.rectTransform.pivot=new Vector2(1,0);state.rectTransform.anchoredPosition=new Vector2(0,8);state.rectTransform.sizeDelta=new Vector2(140,32);state.fontSize=20;
        state.verticalOverflow=VerticalWrapMode.Overflow;
        heat.rectTransform.offsetMin=new Vector2(4,5);heat.rectTransform.offsetMax=new Vector2(-4,-5);
        var vent=Rect(frame.transform,"HeatVent");vent.anchorMin=vent.anchorMax=new Vector2(1,.5f);vent.anchoredPosition=new Vector2(20,0);vent.sizeDelta=new Vector2(28,28);
        var ventImage=vent.GetComponent<Image>();if(ventImage==null)ventImage=vent.gameObject.AddComponent<Image>();ventImage.raycastTarget=false;ventImage.preserveAspect=true;ventImage.color=new Color(1,.8f,.45f,.85f);ventImage.enabled=false;Bind(hud,"heatVent",ventImage);
        var ventFrames=AssetDatabase.LoadAllAssetsAtPath("Assets/StarShift/Art/VFX/thermal-vent-sheet.png").OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
        so=new SerializedObject(hud);var frameArray=so.FindProperty("heatVentFrames");frameArray.arraySize=ventFrames.Length;for(int i=0;i<ventFrames.Length;i++)frameArray.GetArrayElementAtIndex(i).objectReferenceValue=ventFrames[i];so.ApplyModifiedPropertiesWithoutUndo();
        var destruction=UnityEngine.Object.FindAnyObjectByType<AsteroidDestructionPresentation>();
        var enemyFrames=AssetDatabase.LoadAllAssetsAtPath("Assets/StarShift/Art/VFX/explosion-sheet.png").OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
        if(enemyFrames.Length!=8)throw new Exception("Expected eight common explosion frames");
        so=new SerializedObject(destruction);frameArray=so.FindProperty("enemyFrames");frameArray.arraySize=enemyFrames.Length;for(int i=0;i<enemyFrames.Length;i++)frameArray.GetArrayElementAtIndex(i).objectReferenceValue=enemyFrames[i];so.ApplyModifiedPropertiesWithoutUndo();
        var supplyFrames=AssetDatabase.LoadAllAssetsAtPath("Assets/StarShift/Art/VFX/supply-burst-polished-sheet.png").OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
        so=new SerializedObject(destruction);frameArray=so.FindProperty("supplyFrames");frameArray.arraySize=supplyFrames.Length;for(int i=0;i<supplyFrames.Length;i++)frameArray.GetArrayElementAtIndex(i).objectReferenceValue=supplyFrames[i];so.ApplyModifiedPropertiesWithoutUndo();
        foreach(var owner in new[]{shell.transform,boss.transform,hud.transform})
            foreach(var component in owner.GetComponentsInChildren<Component>(true))EditorUtility.SetDirty(component);
        EditorSceneManager.MarkSceneDirty(shell.gameObject.scene);EditorSceneManager.SaveScene(shell.gameObject.scene);AssetDatabase.SaveAssets();
        return new{settingsReset=true,walletCoinIcon=true,bossPercentage=true,heatRecoveryMarkerRemoved=true,supplyHp=6,pickupDrift=1};
    }
}
