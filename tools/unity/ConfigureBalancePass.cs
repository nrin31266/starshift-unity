using System;
using System.Linq;
using System.Reflection;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Player;
using StarShift.Rewards;
using UnityEditor;
using UnityEngine;

public static class ConfigureBalancePass
{
    const string R = "Assets/StarShift/Data/RegionA/";
    static T Load<T>(string path) where T : UnityEngine.Object => StarShift.Editor.RegionAAssetPaths.Load<T>(path);
    static void Set(UnityEngine.Object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target,value);
        EditorUtility.SetDirty(target);
    }
    static void Save(UnityEngine.Object target) { EditorUtility.SetDirty(target); AssetDatabase.SaveAssetIfDirty(target); }
    public static object Configure()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        foreach(var name in new[]{"PulseLaserLv1","PlasmaSpread","HomingMissile"})
        {
            var weapon=Load<PulseLaserDefinition>("Assets/StarShift/Data/Weapons/"+name+".asset");
            var patterns=Enumerable.Range(1,3).Select(i=>weapon.Pattern(i)).ToList();
            if(weapon.Branch==WeaponBranch.Pulse) patterns.AddRange(new[]{
                new WeaponLevelPattern{ProjectileCount=3,Damage=2,FireInterval=.17f},
                new WeaponLevelPattern{ProjectileCount=5,Damage=1,CenterBonus=3,FireInterval=.16f}});
            else if(weapon.Branch==WeaponBranch.Plasma) patterns.AddRange(new[]{
                new WeaponLevelPattern{ProjectileCount=7,SpreadDegrees=66,LateralSpacing=0,CenterBonus=3,FireInterval=.22f},
                new WeaponLevelPattern{ProjectileCount=7,SpreadDegrees=70,LateralSpacing=0,Damage=2,CenterBonus=1,FireInterval=.22f}});
            else patterns.AddRange(new[]{
                new WeaponLevelPattern{ProjectileCount=4,SpreadDegrees=50,Damage=2,FireInterval=.48f,SplashRadius=1},
                new WeaponLevelPattern{ProjectileCount=5,SpreadDegrees=56,Damage=2,FireInterval=.45f,SplashRadius=1.1f}});
            Set(weapon,"levels",patterns.ToArray());Save(weapon);
        }
        foreach(var pair in new[]{("Raider",10),("Bomber",19)})
        {var enemy=Load<EnemyDefinition>(R+pair.Item1+".asset");Set(enemy,"maxHp",pair.Item2);Save(enemy);}
        var names=new[]{"M1Foundation","M1Formation","M2RaiderArcs","M2Bomber","M3Sine","M3PincerSupply","M3Elite"};
        var limits=new[]{1,2,2,3,3,4,4};
        var spacing=new[]{.65f,.22f,.22f,.16f,.16f,.12f,.12f};
        for(int i=0;i<names.Length;i++)
        {
            var e=Load<EncounterDefinition>(R+names[i]+".asset");
            foreach(var group in e.Groups) if(group.HpOverride==8) group.HpOverride=7;
            e.ActiveShooterLimit=limits[i];e.ShotSpacing=spacing[i];e.ShotsPerVolley=i==0?2:i<4?4:6;e.VolleyPause=i==0?2.2f:1.8f;
            e.GuaranteedMajorCount=i==0?1:2;
            if(!e.IsValid(out var error))throw new InvalidOperationException(error);
            Save(e);
        }
        foreach(var name in new[]{"M2StormStream","M4ResourceStormStream"})
        {
            var stream=Load<HazardStreamDefinition>(R+name+".asset");
            var hp=new[]{3,6,11,19};for(int i=0;i<stream.Variants.Length;i++)stream.Variants[i].Hp=hp[(int)stream.Variants[i].Size];Save(stream);
        }
        foreach(var path in new[]{"Assets/StarShift/Data/Rewards/NormalDrops.asset",R+"EliteDrops.asset",R+"ProgressionDrops.asset"})
        {
            var table=Load<RewardDropTable>(path);
            foreach(var entry in table.MajorItems) if(entry.Item.Kind==PickupKind.WeaponCore)entry.Weight=path.Contains("Progression")?90:path.Contains("Elite")?50:30;
            if(path.Contains("Normal"))table.MajorChance=.025f;
            if(path.Contains("Elite"))table.MajorChance=.10f;
            Save(table);
        }
        var cannon=Load<SingleFireDefinition>(R+"VanguardCannon.asset");Set(cannon,"projectileCount",3);Set(cannon,"spreadDegrees",38f);Save(cannon);
        var missiles=Load<SingleFireDefinition>(R+"VanguardMissiles.asset");Set(missiles,"projectileCount",5);Set(missiles,"spreadDegrees",62f);Save(missiles);
        return new{weaponLevels=WeaponLoadout.MaximumLevel,scout=7,raider=10,bomber=19,elite=34,asteroids=new[]{3,6,11,19},shooterLimits=limits,guarantees=13,bossCannon=6,bossMissiles=5,sceneSaved=false};
    }
    public static object PrepareVerification()
    {
        var sources=new[]{"VerifyPacingRefinement.cs","VerifyHumanFeel.cs","VerifyBalancePass.cs"}.Select(n=>System.IO.File.ReadAllText("../tools/unity/"+n).Replace("../.design/m5-human-feel/", "../.design/m5-balance-pass/human-regression/").Split('\n')).ToArray();
        var imports=sources.SelectMany(s=>s).Where(s=>s.StartsWith("using ")).Distinct();
        var bodies=sources.Select(s=>string.Join("\n",s.Where(line=>!line.StartsWith("using "))));
        const string path="../.design/m5-balance-pass/Verification.cs";
        System.IO.Directory.CreateDirectory("../.design/m5-balance-pass/screenshots");
        System.IO.Directory.CreateDirectory("../.design/m5-balance-pass/human-regression/screenshots");
        System.IO.File.WriteAllText(path,string.Join("\n",imports)+"\n"+string.Join("\n",bodies));return path;
    }
}
