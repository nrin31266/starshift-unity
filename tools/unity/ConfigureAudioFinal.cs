using System;
using System.Linq;
using StarShift.Audio;
using StarShift.Enemies;
using StarShift.Encounters;
using StarShift.Player;
using StarShift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

public static class ConfigureAudioFinal
{
    const string Root = "Assets/StarShift/Audio/";
    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static T Add<T>(GameObject root) where T : Component => root.GetComponent<T>() ?? root.AddComponent<T>();
    static string Slug(string name) => System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1-$2").ToLowerInvariant();
    public static object Apply()
    {
        if (Application.isPlaying) throw new Exception("Edit Mode required");
        AssetDatabase.Refresh();
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Root + "StarShift.mixer");
        var groups = mixer.FindMatchingGroups("");
        foreach (var name in new[] {"Music", "SFX", "UI"}) if (groups.Count(g => g.name == name) != 1) throw new Exception("Missing group " + name);
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root.TrimEnd('/') }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path); bool music = path.Contains("/Music/");
            importer.forceToMono = !music;
            var settings = importer.defaultSampleSettings;
            settings.preloadAudioData = !music;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = .8f; settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings; importer.SaveAndReimport();
        }
        var palette = AssetDatabase.LoadAssetAtPath<AudioPalette>(Root + "AudioPalette.asset");
        if (palette == null) { palette = ScriptableObject.CreateInstance<AudioPalette>(); AssetDatabase.CreateAsset(palette, Root + "AudioPalette.asset"); }
        palette.Hub = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Music/bgm-hub.wav");
        palette.Combat = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Music/bgm-combat.wav");
        palette.Boss = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Music/bgm-boss.wav");
        palette.MusicLevel = .2f;
        palette.Cues = Enum.GetValues(typeof(AudioCue)).Cast<AudioCue>().Select(id => {
            bool warning = id == AudioCue.Overheat || id.ToString().Contains("Warning") || id == AudioCue.InterceptorCharge || id == AudioCue.SniperCharge;
            bool player = id == AudioCue.PlayerHit || id == AudioCue.PlayerDeath || id == AudioCue.ShieldAbsorb || id == AudioCue.ShieldBreak || id == AudioCue.Victory || id == AudioCue.GameOver || id == AudioCue.BossFinal;
            bool ui = id == AudioCue.Confirm || id == AudioCue.Back || id == AudioCue.Error;
            int tier = warning ? 0 : player ? 1 : id <= AudioCue.HomingImpact ? 2 : ui || id == AudioCue.Coin || id == AudioCue.PowerUp ? 4 : 3;
            float interval = id == AudioCue.Coin ? .12f : id == AudioCue.Pulse ? .085f : id == AudioCue.Plasma ? .12f : id == AudioCue.HostileShot ? .16f : warning ? .15f : .08f;
            return new AudioPalette.Cue {Id = id, Clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "SFX/" + Slug(id.ToString()) + ".wav"),
                Tier = tier, Volume = warning ? .48f : player ? .45f : id == AudioCue.EnemyDeath ? .13f : id == AudioCue.AsteroidBreak ? .1f : id == AudioCue.HostileShot ? .065f : id == AudioCue.Coin ? .11f : ui ? .22f : tier == 2 ? .19f : .24f,
                Interval = interval, MaxVoices = warning || ui || player ? 1 : 2, Variation = warning || player ? 0 : .035f};
        }).ToArray();
        if (palette.Hub == null || palette.Combat == null || palette.Boss == null || palette.Cues.Any(c => c.Clip == null))
            throw new Exception("Missing audio files: " + string.Join(",", palette.Cues.Where(c => c.Clip == null).Select(c => c.Id)));
        EditorUtility.SetDirty(palette);
        var runner = UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        var director = Add<AudioDirector>(runner.gameObject);
        Set(director, "palette", palette);
        foreach (var pair in new[] { ("musicGroup", "Music"), ("sfxGroup", "SFX"), ("uiGroup", "UI") })
            Set(director, pair.Item1, groups.Single(g => g.name == pair.Item2));
        var presentation = Add<GameAudioPresentation>(runner.gameObject); Set(presentation, "shell", runner.GetComponent<GameShell>());
        foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
            source.outputAudioMixerGroup = groups.Single(g => g.name == (source.name == "MusicChannel" ? "Music" : "SFX"));
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerGameplayGate>();
        int prefabCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/StarShift/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source.GetComponentInChildren<EnemyHealth>(true) == null && source.GetComponent<PlayerGameplayGate>() == null) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<PlayerGameplayGate>() != null) Add<PlayerAudioPresentation>(root);
                foreach (var health in root.GetComponentsInChildren<EnemyHealth>(true)) Add<EnemyAudioPresentation>(health.gameObject);
                foreach (var attack in root.GetComponentsInChildren<DirectedAttack>(true)) Add<EnemyAudioPresentation>(attack.gameObject);
                foreach (var module in root.GetComponentsInChildren<BossAttackModule>(true)) Add<EnemyAudioPresentation>(module.gameObject);
                foreach (var cue in root.GetComponentsInChildren<EnemyAudioPresentation>(true))
                {
                    var directed = cue.GetComponent<DirectedAttack>(); var module = cue.GetComponent<BossAttackModule>();
                    var health = cue.GetComponentInParent<EnemyHealth>();
                    AudioCue warning = AudioCue.HostileShot, shot = AudioCue.HostileShot;
                    if (directed != null) { bool strike = directed.Definition.Kind == DirectedAttackKind.Strike; warning = strike ? AudioCue.InterceptorCharge : AudioCue.SniperCharge; shot = strike ? AudioCue.InterceptorDash : AudioCue.SniperFire; }
                    if (module != null)
                    {
                        var so = new SerializedObject(module);
                        var barrage = so.FindProperty("barrage").objectReferenceValue as BossBarrage;
                        warning = barrage != null ? (barrage.Definition.Kind == BossBarrageKind.IonShelter ? AudioCue.BossShelterWarning : AudioCue.BossCurtainWarning) :
                            so.FindProperty("escorts").objectReferenceValue != null ? AudioCue.BossEscortWarning : so.FindProperty("directed").objectReferenceValue != null || so.FindProperty("beam").objectReferenceValue != null ? AudioCue.BossBeamWarning : AudioCue.BossCannonWarning;
                        shot = warning == AudioCue.BossBeamWarning ? AudioCue.SniperFire : AudioCue.HeavyShot;
                    }
                    else if (directed == null && health != null && health.Definition.MissileTier == MissileDamageTier.Large)
                    { warning = AudioCue.HeavyCharge; shot = AudioCue.HeavyShot; }
                    var serialized = new SerializedObject(cue); serialized.FindProperty("warning").enumValueIndex = (int)warning; serialized.FindProperty("attackCue").enumValueIndex = (int)shot; serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path); prefabCount++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var uiPath = "Assets/StarShift/Prefabs/UI/GameShellUI.prefab";
        var uiRoot = PrefabUtility.LoadPrefabContents(uiPath);
        try
        {
            var view = uiRoot.GetComponent<ShellView>();
            view.Screens[(int)ShellScreen.Credits].GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(t => t.name == "Body").text = ShellCopy.CreditsText;
            PrefabUtility.SaveAsPrefabAsset(uiRoot, uiPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(uiRoot); }
        EditorSceneManager.MarkSceneDirty(runner.gameObject.scene); EditorSceneManager.SaveScene(runner.gameObject.scene);
        AssetDatabase.SaveAssets();
        return new { clips = palette.Cues.Length + 3, prefabCount, groups = groups.Select(g => g.name).ToArray() };
    }
}
