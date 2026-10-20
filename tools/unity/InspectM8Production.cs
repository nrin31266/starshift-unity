using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
public static class InspectM8Production
{
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
