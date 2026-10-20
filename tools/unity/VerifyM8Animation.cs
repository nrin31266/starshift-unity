using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.InputSystem;

public static class VerifyM8Animation
{
    const string Art = "Assets/StarShift/Art/M8/Smoke/";
    const string Evidence = "../.design/m8/animation-smoke/";
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    public static object Import()
    {
        Check(!Application.isPlaying, "Edit Mode required");
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "shard-shimmer.png");
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 56.25f; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); Check(provider != null, "provider");
        provider.InitSpriteEditorDataProvider();
        var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
        Check(edit != null, "edit capability");
        foreach (var c in new[] { EEditCapability.CreateAndDeleteSprite, EEditCapability.EditSpriteRect, EEditCapability.EditPivot, EEditCapability.EditSpriteName })
            Check(edit.GetEditCapability().HasCapability(c), "unsupported " + c);
        var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>(); Check(ids != null, "name/file IDs");
        var old = provider.GetSpriteRects().ToDictionary(r => r.name);
        var rects = Enumerable.Range(0, 4).Select(i => { var name = "shard-shimmer_" + i.ToString("D2"); return new SpriteRect {
            name = name, rect = new Rect(i * 32, 0, 32, 40), pivot = Vector2.one * .5f, alignment = SpriteAlignment.Center,
            spriteID = old.TryGetValue(name, out var r) ? r.spriteID : UnityEngine.GUID.Generate() }; }).ToArray();
        provider.SetSpriteRects(rects); ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        var frames = AssetDatabase.LoadAllAssetsAtPath(Art + "shard-shimmer.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
        Check(frames.Length == 4 && frames.All(f => f.rect.size == new Vector2(32,40) && f.pivot == new Vector2(16,20) && f.pixelsPerUnit == 56.25f), "slice read-back");
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Art + "shard-shimmer.anim");
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, Art + "shard-shimmer.anim"); }
        clip.legacy = false; clip.frameRate = 8; clip.wrapMode = WrapMode.Loop;
        var clipSettings=AnimationUtility.GetAnimationClipSettings(clip); clipSettings.loopTime=true; AnimationUtility.SetAnimationClipSettings(clip,clipSettings);
        var keys = Enumerable.Range(0, 5).Select(i => new ObjectReferenceKeyframe { time = i / 8f, value = frames[i % 4] }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(Art+"shard-shimmer.controller");
        if(controller==null) { controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(Art+"shard-shimmer.controller"); controller.AddMotion(clip); }
        EditorUtility.SetDirty(clip); AssetDatabase.SaveAssets();
        return new { status = "PASS", frames = frames.Length, ppu = 56.25, pivot = "center", fps = 8, productionBinding = false };
    }
    static async Task Until(Func<bool> condition, int ms, string label)
    { var end = Time.realtimeSinceStartup + ms / 1000f; while (!condition() && Time.realtimeSinceStartup < end) await Task.Delay(30); Check(condition(), label); }
    public static object Begin()
    {
        Check(Application.isPlaying && !UnityEngine.Object.FindAnyObjectByType<EncounterRunner>().HasActiveRun, "fresh Play Mode Menu required");
        Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence + "runtime.json", "{\"status\":\"RUNNING\"}");
        _ = Run(); return new { started = true };
    }
    static async Task Run()
    {
        var runner = UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        var save = runner.GetComponent<CampaignLifecycle>(); var shell = runner.GetComponent<GameShell>();
        var session = runner.GetComponent<GameplaySession>(); var original = save.SavePath;
        var snapshots = new[] { "", ".bak", ".invalid" }.ToDictionary(s => s, s => File.Exists(original+s) ? File.ReadAllBytes(original+s) : null);
        var damage = GameObject.Find("Player").GetComponent<PlayerDamageLifecycle>(); var oldDamage = damage.enabled;
        var devices = InputSystem.devices.Where(d => d.native && d.enabled && (d is UnityEngine.InputSystem.Pointer || d is Keyboard || d is Gamepad)).ToArray();
        RewardPickup pickup = null; Animator animation = null; PickupDefinition fixture = null; object result = null;
        var temp = Path.Combine(Path.GetTempPath(), "starshift-animation-" + Guid.NewGuid().ToString("N"), "save.json");
        EditorApplication.LockReloadAssemblies();
        try
        {
            foreach (var d in devices) InputSystem.DisableDevice(d);
            save.UseTestStorage(temp); shell.Boot(); UnityEngine.Object.FindAnyObjectByType<ShellView>().NewGame.onClick.Invoke();
            await Until(() => !shell.IsBusy && session.Flow.CanContinue, 6000, "briefing");
            session.BeginMission(); await Until(() => runner.Stage == EncounterStage.Running, 15000, "combat"); damage.enabled = false;
            fixture=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/StellarShard.asset"));
            // Fixture đứng yên để quan sát đủ chu kỳ, không mutate definition production.
            fixture.Lifetime=60; fixture.FallAcceleration=0; fixture.MaximumFallSpeed=0;
            pickup = runner.GetComponent<RewardDirector>().Spawn(fixture, null,
                Camera.main.ViewportToWorldPoint(new Vector3(.38f,.35f,10)));
            var visual = pickup.GetComponentInChildren<SpriteRenderer>();
            animation = visual.gameObject.AddComponent<Animator>();
            animation.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Art+"shard-shimmer.controller");
            animation.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var seen = new HashSet<string>();
            for (int i = 0; i < 24; i++)
            {
                await Task.Delay(35); seen.Add(visual.sprite.name);
                if (i == 5 || i == 12) { ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence + "gameplay-" + i + ".png")); }
            }
            Check(pickup.IsActive && visual.isVisible && seen.Count == 4, "all four frames visible on real pickup: active="+pickup.IsActive+", visible="+visual.isVisible+", seen="+string.Join(",",seen)+", scale="+Time.timeScale);
            session.Pause(); var paused = visual.sprite; await Task.Delay(200); Check(visual.sprite == paused, "scaled-time pause"); session.Resume();
            result = new { status = "PASS", framesSeen = seen.OrderBy(x => x).ToArray(), realPickup = true, visible = true, pause = true, productionSaveUnchanged = true };
        }
        catch (Exception e) { result = new { status = "FAILED", error = e.ToString() }; }
        finally
        {
            try
            {
                if (animation != null) UnityEngine.Object.DestroyImmediate(animation);
                if (pickup != null && pickup.IsActive) pickup.Release();
                save.ExitIsolated(); save.UseTestStorage(original); shell.Boot(); damage.enabled = oldDamage;
                if(fixture!=null) UnityEngine.Object.DestroyImmediate(fixture);
                foreach (var pair in snapshots) { var after = File.Exists(original+pair.Key) ? File.ReadAllBytes(original+pair.Key) : null;
                    Check(pair.Value == null ? after == null : after != null && pair.Value.SequenceEqual(after), "production bytes " + pair.Key); }
            }
            catch (Exception e) { result = new { status = "FAILED", originalResult = result, cleanupError = e.ToString() }; }
            foreach (var d in devices) if (d.added) InputSystem.EnableDevice(d);
            EditorApplication.UnlockReloadAssemblies();
            File.WriteAllText(Evidence + "runtime.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            if (Directory.Exists(Path.GetDirectoryName(temp))) Directory.Delete(Path.GetDirectoryName(temp), true);
        }
    }
}
