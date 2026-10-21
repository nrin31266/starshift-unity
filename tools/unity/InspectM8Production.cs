using System;
using System.Collections.Generic;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Rewards;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
public static class InspectM8Production
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

    public static object FinalState()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var dev=UnityEngine.Object.FindAnyObjectByType<StarShift.Dev.DevFastStart>();
        var config=new SerializedObject(dev);bool enabled=config.FindProperty("devModeEnabled").boolValue;
        var debug=new SerializedObject(dev.GetComponent<StarShift.Dev.EncounterDebugView>());
        var flags=new[]{"showBounds","showPaths","showSpawnPoints","showHitboxes","showFps","showContentLabels"};
        if(enabled||flags.Any(f=>debug.FindProperty(f).boolValue))throw new InvalidOperationException("Production dev/debug default must be OFF");
        if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Gameplay scene has unsaved changes");
        var result=new{status="PASS",devModeOff=true,debugFlagsOff=true,sceneDirty=false};
        System.IO.File.WriteAllText("../.design/art-closure/final-state.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }

    public static object References()
    {
        var issues=new System.Collections.Generic.List<string>();
        var objects=new System.Collections.Generic.List<UnityEngine.Object>();
        Action<GameObject> collect=root=>{
            foreach(var component in root.GetComponentsInChildren<Component>(true))
                if(component==null)issues.Add(root.name+": missing script");else objects.Add(component);
        };
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())collect(root);
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/StarShift/Prefabs"}))
            collect(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
        foreach(var guid in AssetDatabase.FindAssets("t:ScriptableObject",new[]{"Assets/StarShift/Data"}))
            objects.Add(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        foreach(var target in objects)
        {
            if(target==null)continue;
            var property=new SerializedObject(target).GetIterator();
            while(property.Next(true))
                if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue==null&&property.objectReferenceEntityIdValue!=EntityId.None)
                    issues.Add(AssetDatabase.GetAssetPath(target)+"/"+target.name+"."+property.propertyPath);
        }
        var result=new{status=issues.Count==0?"PASS":"FAIL",owners=objects.Count,issues,sceneDirty=SceneManager.GetActiveScene().isDirty};
        System.IO.File.WriteAllText("../.design/presentation-polish/references.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object Run()
    {
        var prefabs = new[] {"Player/Player", "Enemies/Bulwark", "Enemies/Sniper", "Enemies/Interceptor", "Enemies/Carrier", "Enemies/VanguardFrigate", "Enemies/NebulaWarden", "Enemies/StormAsteroid", "Enemies/SupplyDrone", "UI/GameShellUI"};
        var rows = prefabs.Select(p => {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/" + p + ".prefab");
            return new { path = p, renderers = root.GetComponentsInChildren<SpriteRenderer>(true).Select(r => new {path = AnimationUtility.CalculateTransformPath(r.transform,root.transform), sprite = r.sprite?.name, position=r.transform.localPosition.ToString("R"), scale=r.transform.localScale.ToString("R"), rotation=r.transform.localEulerAngles.ToString("R"), order=r.sortingOrder}), components = root.GetComponents<Component>().Select(c=>c.GetType().Name) };
        });
        var scene = SceneManager.GetActiveScene();
        var output = Newtonsoft.Json.JsonConvert.SerializeObject(new {prefabs = rows, scene = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).Select(r=>new {path=AnimationUtility.CalculateTransformPath(r.transform,null),sprite=r.sprite?.name,scale=r.transform.localScale.ToString("R"), position=r.transform.localPosition.ToString("R")})},Newtonsoft.Json.Formatting.Indented);
        System.IO.Directory.CreateDirectory("../.design/m8"); System.IO.File.WriteAllText("../.design/m8/production-bindings.json",output);
        return "Saved renderer bindings";
    }
}
