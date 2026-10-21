using System;
using System.Collections.Generic;
using System.Linq;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Rewards;
using UnityEditor;
using UnityEngine;

public static class ConfigureM8Art
{
    public static string Contracts()
    {
        var rows = new List<object>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/StarShift/Prefabs" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var c in root.GetComponentsInChildren<Collider2D>(true))
                rows.Add(new { path, node = AnimationUtility.CalculateTransformPath(c.transform, root.transform), trigger = c.isTrigger, offset = c.offset.ToString("R"), scale = c.transform.lossyScale.ToString("R"), shape = c is BoxCollider2D b ? b.size.ToString("R") : c is CircleCollider2D circle ? circle.radius.ToString("R") : c.GetType().Name });
        }
        foreach (var guid in AssetDatabase.FindAssets("t:PulseLaserDefinition", new[] { "Assets/StarShift/Data" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var d = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>(path);
            rows.Add(new { path, d.Branch, d.Damage, d.FireInterval, d.ProjectileSpeed, d.ProjectileLifetime, d.TrackingDegreesPerSecond, d.TrackingRange, d.TrackingLaunchSeconds, d.TrackingLaunchArcDegrees, levels = Enumerable.Range(1, 5).Select(d.Pattern).ToArray() });
        }
        foreach (var guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/StarShift/Data" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var d = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            rows.Add(new { path, d.MaxHp, d.Score, d.BodyDamagesPlayer, d.MissileTier });
        }
        foreach (var guid in AssetDatabase.FindAssets("t:SingleFireDefinition", new[] { "Assets/StarShift/Data" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var d = AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(path);
            rows.Add(new { path, d.ProjectileSpeed, d.ProjectileLifetime, d.InitialDelay, d.TelegraphDuration, d.Cooldown, d.ProjectileCount, d.SpreadDegrees, d.BurstCount, d.BurstInterval });
        }
        foreach (var guid in AssetDatabase.FindAssets("t:PickupDefinition", new[] { "Assets/StarShift/Data" }).OrderBy(x => x))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var d = AssetDatabase.LoadAssetAtPath<PickupDefinition>(path);
            rows.Add(new { path, d.Kind, d.Amount, d.VisualScale, d.Lifetime, d.DriftSpeed, d.Duration, d.FireRateMultiplier, d.FallAcceleration, d.MaximumFallSpeed, d.ShardBounceHeight });
        }
        return Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented);
    }
    public static object VerifyContracts()
    {
        var after = Contracts();
        var before = System.IO.File.ReadAllText("../.design/m8-rebuild/contracts-before.json");
        if (!Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JToken.Parse(before), Newtonsoft.Json.Linq.JToken.Parse(after)))
            throw new System.Exception("Gameplay/collider contract changed");
        System.IO.File.WriteAllText("../.design/m8-rebuild/contracts-after.json", after);
        System.IO.File.WriteAllText("../.design/m8-rebuild/contracts-verification.json", "{\"status\":\"PASS\",\"gameplayAndCollidersUnchanged\":true}");
        return "PASS: gameplay definitions and collider contracts unchanged";
    }
}
