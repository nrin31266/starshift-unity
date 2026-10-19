using System;
using System.IO;
using System.Linq;
using System.Reflection;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class ConfigureFlowPolish
{
    const string R="Assets/StarShift/Data/RegionA/";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Load<T>(string path) where T:UnityEngine.Object => StarShift.Editor.RegionAAssetPaths.Load<T>(path) ?? throw new InvalidOperationException(path);
    static void Set(UnityEngine.Object o,string name,object value){o.GetType().GetField(name,F).SetValue(o,value);EditorUtility.SetDirty(o);}
    static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,F).GetValue(o);
    static void Save(UnityEngine.Object o){EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);}
    public static object PrepareVerification()
    {
        Directory.CreateDirectory("../.design/m5-flow-polish/human-regression/screenshots");Directory.CreateDirectory("../.design/m5-flow-polish/screenshots");Directory.CreateDirectory("../.design/m5-flow-polish/balance-regression");
        var sources=new[]{"VerifyPacingRefinement.cs","VerifyHumanFeel.cs","VerifyBalancePass.cs","VerifyFlowPolish.cs"}.Select(n=>File.ReadAllLines("../tools/unity/"+n)).ToArray();
        var imports=sources.SelectMany(s=>s).Where(s=>s.StartsWith("using ")).Distinct();
        var body=string.Join("\n",sources.Select(s=>string.Join("\n",s.Where(line=>!line.StartsWith("using ")))));
        body=body.Replace("../.design/m5-balance-pass/","../.design/m5-flow-polish/balance-regression/").Replace("../.design/m5-human-feel/","../.design/m5-flow-polish/human-regression/").Replace("../.design/m5-refinement/","../.design/m5-flow-polish/human-regression/");
        var path="../.design/m5-flow-polish/Verification.cs";File.WriteAllText(path,string.Join("\n",imports)+"\n"+body);return path;
    }
    public static object MoveFixtures()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        const string data="Assets/StarShift/Data/";
        if(!AssetDatabase.IsValidFolder(data+"Fixtures"))AssetDatabase.CreateFolder(data.TrimEnd('/'),"Fixtures");
        var fixtureGuid=AssetDatabase.AssetPathToGUID(data+"Levels/L1.asset");
        foreach(var folder in new[]{"Levels","Encounters"})
        {
            if(!AssetDatabase.IsValidFolder(data+folder))continue;
            var error=AssetDatabase.MoveAsset(data+folder,data+"Fixtures/"+folder);
            if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
        }
        var old=data+"Fixtures/Levels/L1.asset";var target=data+"Fixtures/Levels/M4SystemTestbed.asset";
        if(StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>(old)!=null)
        {
            var error=AssetDatabase.MoveAsset(old,target);if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
        }
        var fixture=Load<LevelDefinition>(target);
        if(!string.IsNullOrEmpty(fixtureGuid)&&AssetDatabase.AssetPathToGUID(target)!=fixtureGuid)throw new InvalidOperationException("GUID changed");
        fixture.name="M4SystemTestbed";Save(fixture);
        if(fixture.Id!="M4_FIXTURE"||!fixture.IsValid(out var issue))throw new InvalidOperationException("Fixture invalid");
        return new{fixturePath=target,guid=AssetDatabase.AssetPathToGUID(target),encounters=fixture.Encounters.Length};
    }
    public static object Configure()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var backup=System.IO.Path.GetFullPath("../.design/m5-flow-polish/before-scene.unity");
        if(!System.IO.File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new InvalidOperationException("Scene snapshot failed");
        for(int i=1;i<=4;i++){var level=Load<LevelDefinition>(R+"Mission0"+i+".asset");level.EnemyHealthMultiplier=new[]{1f,1.1f,1.2f,1.25f}[i-1];Save(level);}
        foreach(var item in new[]{("Raider",6),("Bomber",12)}){var e=Load<EnemyDefinition>(R+item.Item1+".asset");Set(e,"maxHp",item.Item2);Save(e);}
        var elite=Load<EnemyModifierDefinition>(R+"Elite.asset");elite.HpMultiplier=1.5f;Save(elite);
        var names=new[]{"M1Foundation","M1Formation","M2RaiderArcs","M2Bomber","M3Sine","M3PincerSupply","M3Elite"};
        var limits=new[]{1,2,2,2,3,3,3};var quotas=new[]{1,2,1,1,1,1,2};
        for(int i=0;i<names.Length;i++){
            var e=Load<EncounterDefinition>(R+names[i]+".asset");
            foreach(var g in e.Groups)if(g.HpOverride==7)g.HpOverride=4;
            e.ActiveShooterLimit=limits[i];e.GuaranteedMajorCount=quotas[i];
            if(!e.IsValid(out var error))throw new InvalidOperationException(error);Save(e);
        }
        foreach(var n in new[]{"M2StormStream","M4ResourceStormStream"}){
            var stream=Load<HazardStreamDefinition>(R+n+".asset");foreach(var v in stream.Variants)v.Hp=new[]{1,2,4,6}[(int)v.Size];Save(stream);
        }
        var resource=Load<EnemyDefinition>("Assets/StarShift/Data/Enemies/ResourceAsteroid.asset");Set(resource,"maxHp",2);Save(resource);
        foreach(var x in new[]{("Assets/StarShift/Data/Rewards/NormalDrops.asset",.25f),(R+"EliteDrops.asset",.6f)}){var table=Load<RewardDropTable>(x.Item1);table.ShardChance=x.Item2;Save(table);}
        foreach(var name in new[]{"PulseLaserLv1","PlasmaSpread","HomingMissile"}){
            var weapon=Load<PulseLaserDefinition>("Assets/StarShift/Data/Weapons/"+name+".asset");
            var patterns=Enumerable.Range(1,5).Select(level=>new WeaponLevelPattern{
                ProjectileCount=weapon.Branch==WeaponBranch.Plasma?level+2:level,
                Damage=weapon.Branch==WeaponBranch.Homing?2:1,
                LateralSpacing=weapon.Branch==WeaponBranch.Pulse?.16f:0,
                FireInterval=weapon.Branch==WeaponBranch.Pulse?new[]{.18f,.18f,.18f,.17f,.16f}[level-1]:weapon.Branch==WeaponBranch.Plasma?.31f-.01f*level:.62f-.02f*level,
                SpreadDegrees=weapon.Branch==WeaponBranch.Plasma?24+6*level:weapon.Branch==WeaponBranch.Homing?new[]{0,16,24,32,40}[level-1]:0,
                SplashRadius=weapon.Branch==WeaponBranch.Homing&&level>=3?.45f+.1f*(level-3):0}).ToArray();
            Set(weapon,"levels",patterns);
            if(weapon.Branch==WeaponBranch.Plasma)Set(weapon,"projectileScale",new Vector2(.30f,.24f));
            if(weapon.Branch==WeaponBranch.Homing){Set(weapon,"trackingDegreesPerSecond",210f);Set(weapon,"projectileSpeed",7.5f);Set(weapon,"projectileLifetime",4f);}
            Save(weapon);
        }
        var goldPath=StarShift.Editor.RegionAAssetPaths.Resolve<PickupDefinition>(R+"BossGold.asset");var gold=StarShift.Editor.RegionAAssetPaths.Load<PickupDefinition>(goldPath);
        if(gold==null){gold=UnityEngine.Object.Instantiate(Load<PickupDefinition>("Assets/StarShift/Data/Rewards/StellarShard.asset"));AssetDatabase.CreateAsset(gold,goldPath);}
        gold.ShortCode="";gold.VisualScale=.55f;gold.Lifetime=12;gold.DriftSpeed=1.2f;gold.FallAcceleration=4.5f;gold.MaximumFallSpeed=3;Save(gold);
        var dropPath=StarShift.Editor.RegionAAssetPaths.Resolve<RewardDropTable>(R+"VanguardDefeatDrops.asset");var drops=StarShift.Editor.RegionAAssetPaths.Load<RewardDropTable>(dropPath);
        if(drops==null){drops=UnityEngine.Object.Instantiate(Load<RewardDropTable>("Assets/StarShift/Data/Rewards/SupplyCrateDrops.asset"));AssetDatabase.CreateAsset(drops,dropPath);}
        drops.BossShardCount=28;drops.Shard=gold;Save(drops);
        var boss=Load<EncounterDefinition>(R+"M4Vanguard.asset");boss.Groups[0].RewardOverride=drops;boss.RewardGraceSeconds=12;Save(boss);
        var hud=UnityEngine.Object.FindAnyObjectByType<GameplayHud>();
        var weaponButton=Get<Button>(hud,"weaponButton");var missile=Get<Button>(hud,"missileButton");
        var wr=weaponButton.GetComponent<RectTransform>();var mr=missile.GetComponent<RectTransform>();
        mr.anchorMin=mr.anchorMax=mr.pivot=new Vector2(1,0);mr.anchoredPosition=new Vector2(-8,96);mr.sizeDelta=new Vector2(96,48);
        Get<Text>(hud,"missileCount").fontSize=22;
        var inventory=wr.parent.Find("WeaponInventory")?.GetComponent<Text>();
        if(inventory==null){var go=new GameObject("WeaponInventory",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(wr.parent,false);inventory=go.GetComponent<Text>();}
        var ir=inventory.rectTransform;ir.anchorMin=ir.anchorMax=ir.pivot=new Vector2(1,0);ir.anchoredPosition=new Vector2(-8,60);ir.sizeDelta=new Vector2(188,26);
        inventory.font=Get<Text>(hud,"weaponLabel").font;inventory.fontSize=16;inventory.alignment=TextAnchor.MiddleRight;inventory.color=new Color(.55f,.8f,.88f);inventory.raycastTarget=false;
        Set(hud,"weaponInventory",inventory);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{configured=true,scout=4,asteroidHp=new[]{1,2,4,6},shooterLimits=limits,bossGold=28,rewardSeconds=12,backup};
    }
}
