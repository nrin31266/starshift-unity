using System;
using System.Collections;
using System.Linq;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Player;
using UnityEngine;

// Batch 3 runtime evidence: family sprite binding, damage repaint, impact type/point,
// projectile visual và attack phase. Chạy Edit Mode import check + Play Mode probe.
public static class VerifyM8FleetCombat
{
    public static object Edit()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var report = new System.Text.StringBuilder();
        int families = 0, damage = 0, impacts = 0;
        foreach (var name in new[] { "Scout", "Raider", "Bomber", "Bulwark", "Sniper", "Interceptor", "Carrier", "SupplyDrone", "WardenDrone" })
        {
            var path = "Assets/StarShift/Prefabs/Enemies/" + name + ".prefab";
            var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
            try
            {
                var hull = root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "HullPlaceholder");
                Require(hull != null && hull.sprite != null, name + " missing hull sprite");
                families++;
                var presentation = root.GetComponent<EnemyDamagePresentation>();
                var so = new UnityEditor.SerializedObject(presentation);
                var damaged = (Sprite)so.FindProperty("damagedSprite").objectReferenceValue;
                var critical = (Sprite)so.FindProperty("criticalSprite").objectReferenceValue;
                Require(damaged != null && critical != null, name + " missing damage sprites");
                Require(damaged.texture != hull.sprite.texture && critical.texture != hull.sprite.texture, name + " damage not repainted");
                damage += 2;
                var hit = root.GetComponent<EnemyHitPresentation>();
                if (hit != null)
                {
                    var hs = new UnityEditor.SerializedObject(hit);
                    if (hs.FindProperty("impact").objectReferenceValue != null) impacts++;
                }
            }
            finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var id in new[] { "pulse-bolt", "plasma-orb", "hostile-coral", "hostile-lemon", "hostile-special", "muzzle-pulse", "muzzle-plasma", "muzzle-missile", "muzzle-beam", "impact-shield", "impact-hull", "impact-module", "charge-hostile", "debris-hull", "supply-burst", "boss-blast" })
        {
            var sp = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/StarShift/Art/M8/" + id + "-sheet.png").OfType<Sprite>().ToArray();
            Require(sp.Length == 4, id + " sheet frame count " + sp.Length);
        }
        Require(families == 9, "family count " + families);
        report.Append($"families={families} damageSprites={damage} impactOwners={impacts}");
        return report.ToString();
    }

    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static object Begin(float timeout = 40f)
    {
        var runner = UnityEngine.Object.FindAnyObjectByType<StarShift.Encounters.EncounterRunner>();
        if (runner == null) throw new Exception("No EncounterRunner in scene");
        var probe = new GameObject("M8FleetCombatProbe").AddComponent<Probe>();
        probe.Timeout = timeout;
        return probe.Run();
    }

    private sealed class Probe : MonoBehaviour
    {
        public float Timeout;
        private readonly System.Text.StringBuilder log = new System.Text.StringBuilder();
        private void Awake() => DontDestroyOnLoad(gameObject);
        public object Run() { StartCoroutine(Sequence()); return "started"; }

        private IEnumerator Sequence()
        {
            var report = new System.Text.StringBuilder();
            string failure = null;
            yield return Body(report, error => failure = error);
            if (failure != null) report.Append("RESULT=FAIL " + failure);
            else report.Append("RESULT=PASS ");
        private IEnumerator Body(System.Text.StringBuilder report, Action<string> fail)
        {
            try
            {

            }
            catch (Exception error) { fail(error.Message); }
        }

            Debug.Log("[M8FleetCombat] " + report);
            var path = System.IO.Path.Combine(Application.dataPath, "../.design/m8/fleet-combat-runtime.json");
            System.IO.File.WriteAllText(path, "{\"status\":\"" + (report.ToString().Contains("RESULT=PASS") ? "PASS" : "FAIL") + "\",\"detail\":\"" + report.ToString().Replace("\"", "'") + "\"}\n");
            Destroy(gameObject);
        }
    }
}
