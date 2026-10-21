using System;
using System.Linq;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Player;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Batch 3 authoring: bind armor-family sprites, damage states, projectile visual,
// impact presentation và staged destruction vào prefab/data hiện có.
// Không đổi collider, HP, scale, formation hoặc timing gameplay.
public static class ConfigureM8FleetCombat
{
    const string Art = "Assets/StarShift/Art/";
    static string ArtPath(string id) => AssetDatabase.FindAssets(id + " t:Texture2D", new[]{Art.TrimEnd('/')})
        .Select(AssetDatabase.GUIDToAssetPath).Single(path => System.IO.Path.GetFileNameWithoutExtension(path) == id);
    static Sprite S(string id)
    {
        var path=AssetDatabase.FindAssets(id+" t:Texture2D",new[]{Art.TrimEnd('/')}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>System.IO.Path.GetFileNameWithoutExtension(p)==id);
        return path!=null?AssetDatabase.LoadAssetAtPath<Sprite>(path):Frames(id).First();
    }
    static Sprite[] Frames(string id) => AssetDatabase.LoadAllAssetsAtPath(ArtPath(id + "-sheet")).OfType<Sprite>().OrderBy(s => s.name, StringComparer.Ordinal).ToArray();

    static void Set(UnityEngine.Object o, string field, UnityEngine.Object value)
    { var so = new SerializedObject(o); var p = so.FindProperty(field) ?? throw new Exception(o.name + ":" + field); p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }

    static void Array(UnityEngine.Object o, string field, Sprite[] values)
    {
        var so = new SerializedObject(o); var p = so.FindProperty(field) ?? throw new Exception(o.name + ":" + field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Float(UnityEngine.Object o, string field, float value)
    { var so = new SerializedObject(o); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }

    static void Bool(UnityEngine.Object o, string field, bool value)
    { var so = new SerializedObject(o); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }

    static T Component<T>(GameObject root) where T : Component { var c = root.GetComponent<T>(); return c != null ? c : root.AddComponent<T>(); }

    static SpriteRenderer Visual(GameObject root, string name, string id, Vector3 position, int order, float scale = 1)
    {
        var t = root.transform.Find(name); if (t == null) { t = new GameObject(name).transform; t.SetParent(root.transform, false); }
        var r = Component<SpriteRenderer>(t.gameObject); r.sprite = S(id); r.color = Color.white; r.sortingOrder = order;
        t.localPosition = position; t.localScale = Vector3.one * scale; return r;
    }

    static void Prefab(string path, Action<GameObject> edit)
    {
        path = "Assets/StarShift/Prefabs/" + path + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void EditMode() { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required"); }
    static void Save() { AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); EditorSceneManager.SaveOpenScenes(); }

    static void Impact(GameObject root, string kind)
    {
        var feedback = root.GetComponent<EnemyHitPresentation>();
        if (feedback == null) return;
        var renderer = Visual(root, "M8Impact", "hit-pulse", Vector3.zero, 14, 1f);
        renderer.enabled = false;
        Set(feedback, "impact", renderer);
        Array(feedback, "hullImpactFrames", Frames("hit-pulse"));
        Array(feedback, "shieldImpactFrames", Frames("hit-shield"));
        Array(feedback, "moduleImpactFrames", Frames("hit-homing"));
        Float(feedback, "impactSeconds", .22f);
        // Sprite hull impact đặt tại điểm trúng nhưng không xoay theo root.
        renderer.transform.localRotation = Quaternion.identity;
    }

    static void Damage(GameObject root, string id)
    {
        var damage = root.GetComponent<EnemyDamagePresentation>();
        if (damage != null)
        {
            Set(damage, "damagedSprite", S(id + "-damaged"));
            Set(damage, "criticalSprite", S(id + "-critical"));
            // Không tint đè family: repaint sprite đã mang đúng màu.
            var so = new SerializedObject(damage);
            so.FindProperty("damagedTint").colorValue = Color.white;
            so.FindProperty("criticalTint").colorValue = Color.white;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Impact(root, id);
    }

    static void PhaseCue(GameObject root)
    {
        var module = root.GetComponent<BossAttackModule>();
        var barrage = root.GetComponent<BossBarrage>();
        if (module == null && barrage == null) return;
        var presentation = Component<BossSkillPhasePresentation>(root);
        var cue = Visual(root, "M8PhaseCue", "focus-missile", Vector3.zero, 13, 1f);
        cue.enabled = false;
        Set(presentation, "module", module);
        Set(presentation, "barrage", barrage);
        Set(presentation, "cue", cue);
        Array(presentation, "prepareFrames", Frames("focus-missile"));
        Array(presentation, "lockFrames", Frames("telegraph-special"));
        Array(presentation, "attackFrames", Frames("boss-blast"));
    }

    static void FireCue(GameObject root, bool special, string chargeId)
    {
        var fire = root.GetComponent<EnemyFirePresentation>();
        if (fire == null) return;
        var cue = Visual(root, "M8Charge", chargeId, Vector3.zero, 13, 1f);
        cue.enabled = false;
        Set(fire, "chargeCue", cue);
        Array(fire, "chargeFrames", Frames(chargeId));
        Bool(fire, "specialDanger", special);
    }

    public static object Fleet()
    {
        EditMode();
        foreach (var pair in new[] {
            new[]{"Scout","scout"}, new[]{"Raider","raider"}, new[]{"Bomber","heavy"},
            new[]{"Bulwark","bulwark"}, new[]{"Sniper","sniper"}, new[]{"Interceptor","interceptor"},
            new[]{"Carrier","carrier"}, new[]{"SupplyDrone","supply-drone"} })
        {
            var spriteId = pair[1];
            Prefab("Enemies/" + pair[0], root =>
            {
                var hull = root.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "HullPlaceholder");
                Set(hull, "m_Sprite", S(spriteId));
                Damage(root, spriteId);
                // Normal hostile dùng charge coral/lemon; chỉ Sniper/Interceptor (special) dùng magenta beam.
                bool special = pair[0] == "Sniper" || pair[0] == "Interceptor";
                FireCue(root, special, special ? "focus-sniper" : "focus-gun");
            });
        }
        Prefab("Enemies/SupplyDrone", root =>
        {
            var hull = root.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "HullPlaceholder");
            Set(hull, "m_Sprite", S("supply-drone"));
            Damage(root, "supply-drone");
        });
        Prefab("Enemies/WardenDrone", root =>
        {
            var hull = root.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "HullPlaceholder");
            Set(hull, "m_Sprite", S("warden-drone"));
            Damage(root, "warden-drone");
        });
        Save();
        return new { families = 8, damageStates = 3 };
    }

    public static object Projectiles()
    {
        EditMode();
        // Hostile single-fire: coral cho normal, lemon cho burst/spread phụ, magenta chỉ special.
        foreach (var guid in AssetDatabase.FindAssets("t:SingleFireDefinition", new[] { "Assets/StarShift/Data" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var definition = AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(path);
            var name = definition.name;
            bool special = name.Contains("WardenPlasma") || name.Contains("Curtain");
            string id = special ? "hostile-special" : name.Contains("Bulwark") || name.Contains("BomberSpread") || name.Contains("WardenSweep") ? "hostile-lemon" : "hostile-coral";
            var so = new SerializedObject(definition);
            so.FindProperty("projectileSprite").objectReferenceValue = S(id);
            so.FindProperty("projectileColor").colorValue = Color.white;
            so.FindProperty("projectileScale").vector2Value = Vector2.one;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        // Friendly branch giữ sprite riêng; plasma dùng orb mới, pulse dùng needle mới.
        foreach (var guid in AssetDatabase.FindAssets("t:PulseLaserDefinition", new[] { "Assets/StarShift/Data" }))
        {
            var definition = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            string id = definition.Branch == WeaponBranch.Plasma ? "plasma-orb" : definition.Branch == WeaponBranch.Homing ? "player-homing" : "pulse-bolt";
            Set(definition, "projectileSprite", S(id));
            var so = new SerializedObject(definition);
            so.FindProperty("projectileColor").colorValue = Color.white;
            so.FindProperty("projectileScale").vector2Value = Vector2.one;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Save();
        return new { hostile = new[] { "coral", "lemon", "magenta-special" }, friendly = 3 };
    }

    public static object Feedback()
    {
        EditMode();
        // Player: muzzle theo branch + debris riêng khi hull breakup.
        Prefab("Player/Player", root =>
        {
            var feedback = root.GetComponent<PlayerCombatPresentation>();
            if (feedback == null) return;
            var weapon = root.GetComponent<PulseLaserWeapon>();
            var muzzle = (Transform)new SerializedObject(weapon).FindProperty("muzzle").objectReferenceValue;
            Array(feedback, "pulseMuzzleFrames", Frames("muzzle-pulse"));
            Array(feedback, "plasmaMuzzleFrames", Frames("muzzle-plasma"));
            Array(feedback, "missileMuzzleFrames", Frames("muzzle-missile"));
        });
        // Boss staged destruction: module blast + fragment.
        foreach (var pair in new[] { new[] { "VanguardFrigate", "vanguard" }, new[] { "NebulaWarden", "warden" } })
        {
            Prefab("Enemies/" + pair[0], root =>
            {
                var hull = root.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "HullPlaceholder");
                Set(hull, "m_Sprite", S(pair[1] + "-hull"));
                Damage(root, pair[1]);
                var staged = Component<StagedDestructionPresentation>(root);
                var blastA = Visual(root, "M8BlastA", "boss-blast", new Vector3(-1.1f, .2f, 0), 15, 1.6f);
                var blastB = Visual(root, "M8BlastB", "boss-blast", new Vector3(1.2f, -.3f, 0), 15, 1.9f);
                blastA.enabled = blastB.enabled = false;
                var fragmentA = Visual(root, "M8FragmentA", "debris-heavy", new Vector3(-1.1f, .2f, 0), 14, 1.2f);
                var fragmentB = Visual(root, "M8FragmentB", "debris-heavy", new Vector3(1.2f, -.3f, 0), 14, 1.3f);
                fragmentA.enabled = fragmentB.enabled = false;
                var so = new SerializedObject(staged);
                var stages = so.FindProperty("stages"); stages.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    var stage = stages.GetArrayElementAtIndex(i);
                    stage.FindPropertyRelative("module").objectReferenceValue = null;
                    stage.FindPropertyRelative("blast").objectReferenceValue = i == 0 ? blastA : blastB;
                    var frames = stage.FindPropertyRelative("blastFrames");
                    var source = Frames("boss-blast"); frames.arraySize = source.Length;
                    for (int f = 0; f < source.Length; f++) frames.GetArrayElementAtIndex(f).objectReferenceValue = source[f];
                    stage.FindPropertyRelative("delay").floatValue = i * .55f;
                    stage.FindPropertyRelative("duration").floatValue = .5f;
                    var fragmentFrames = stage.FindPropertyRelative("fragmentFrames");
                    var debrisFrames = Frames("debris-heavy"); fragmentFrames.arraySize = debrisFrames.Length;
                    for (int f = 0; f < debrisFrames.Length; f++) fragmentFrames.GetArrayElementAtIndex(f).objectReferenceValue = debrisFrames[f];
                }
                var fragments = so.FindProperty("fragments"); fragments.arraySize = 2;
                fragments.GetArrayElementAtIndex(0).objectReferenceValue = fragmentA;
                fragments.GetArrayElementAtIndex(1).objectReferenceValue = fragmentB;
                Float(staged, "totalSeconds", 2.4f);
                so.ApplyModifiedPropertiesWithoutUndo();
                foreach (var attackModule in root.GetComponentsInChildren<BossAttackModule>(true)) PhaseCue(attackModule.gameObject);
                var barrage = root.GetComponentInChildren<BossBarrage>(true);
                if (barrage != null) PhaseCue(barrage.gameObject);
            });
        }
        // Asteroid dùng damage repaint theo size; Supply Drone có burst quà riêng.
        Prefab("Enemies/StormAsteroid", root =>
        {
            var hull = root.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "HullPlaceholder");
            Set(hull, "m_Sprite", S("asteroid-medium"));
            var damage = root.GetComponent<EnemyDamagePresentation>();
            if (damage != null) { Set(damage, "damagedSprite", S("asteroid-medium-damaged")); Set(damage, "criticalSprite", S("asteroid-medium-critical")); }
        });
        Prefab("Enemies/ResourceAsteroid", root =>
        {
            var damage = root.GetComponent<EnemyDamagePresentation>();
            if (damage != null) { Set(damage, "damagedSprite", S("resource-asteroid-damaged")); Set(damage, "criticalSprite", S("resource-asteroid-critical")); }
        });
        var runner = UnityEngine.Object.FindAnyObjectByType<AsteroidDestructionPresentation>();
        if (runner != null) Array(runner, "supplyFrames", Frames("supply-burst-polished"));
        Save();
        return new { playerDebris = true, stagedBoss = 2, asteroidStates = 4 };
    }

    public static object Telegraphs()
    {
        EditMode();
        foreach (var name in new[] { "Sniper", "Interceptor" })
        {
            Prefab("Enemies/" + name, root =>
            {
                var directed = root.GetComponent<DirectedAttack>();
                if (directed == null) return;
                var telegraph = Component<DirectedAttackTelegraph>(root);
                var cue = Visual(root, "M8Warning", "beam-warning", Vector3.zero, 13, 1.2f);
                cue.enabled = false;
                Set(telegraph, "warning", cue);
                Array(telegraph, "warningFrames", Frames("beam-warning"));
                Bool(telegraph, "strike", name == "Interceptor");
                Float(telegraph, "frameSeconds", .07f);
            });
        }
        Save();
        return new { telegraphs = 2 };
    }
}
