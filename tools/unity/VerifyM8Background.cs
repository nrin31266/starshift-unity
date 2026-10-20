using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.UI;
using UnityEditor;
using UnityEngine;

public static class VerifyM8Background
{
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static async Task Until(Func<bool> condition, int ms)
    { var end=Time.realtimeSinceStartup+ms/1000f; while(!condition() && Time.realtimeSinceStartup<end) await Task.Delay(30); Check(condition(),"phase timeout"); }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying,"Play Mode");
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>(); Check(!runner.HasActiveRun,"Menu required");
        var save=runner.GetComponent<CampaignLifecycle>(); var shell=runner.GetComponent<GameShell>(); var session=runner.GetComponent<GameplaySession>();
        var original=save.SavePath; var bytes=new[]{"",".bak",".invalid"}.ToDictionary(s=>s,s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null);
        var temp=Path.Combine(Path.GetTempPath(),"starshift-bg-"+Guid.NewGuid().ToString("N"),"save.json");
        var camera=Camera.main; var view=camera.GetComponent<GameplayViewController>(); var oldView=view.TargetView;
        var bg=camera.transform.Find("M8Environment"); var worldSize=bg.GetComponent<SpriteRenderer>().bounds.size;
        var output="../.design/m8/background/"; Directory.CreateDirectory(output);
        object result=null;
        try
        {
            save.UseTestStorage(temp); shell.Boot(); UnityEngine.Object.FindAnyObjectByType<ShellView>().NewGame.onClick.Invoke();
            await Until(()=>!shell.IsBusy && session.Flow.CanContinue,6000);
            var intro=UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>();
            var stars=(Transform)typeof(MissionIntroPresentation).GetField("stars",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(intro);
            var star=stars.GetComponentsInChildren<SpriteRenderer>().First(r=>r.isVisible && r.color.a>0);
            var pos=star.transform.position; await Task.Delay(200);
            Check(star.transform.position!=pos && star.isVisible,"intro parallax visible/moving");
            session.BeginMission(); await Until(()=>session.Flow.IntroPhase==MissionIntroPhase.Travel,3000);
            Check(bg.localScale==Vector3.one,"intro fixed scale");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(output+"intro-travel.png"));
            await Until(()=>runner.Stage==EncounterStage.Running,10000);
            var scale=bg.localScale;
            foreach(float aspect in new[]{1600f/900,2000f/900}) foreach(float size in new[]{8f,12f,20f})
            {
                camera.aspect=aspect; view.RequestView(new GameplayViewSettings(size,new Rect(0,0,1,1)),0); await Task.Delay(60);
                Check(bg.localScale==scale && bg.GetComponent<SpriteRenderer>().bounds.size==worldSize,"fixed native world extent");
                Check(camera.backgroundColor==(Color)new Color32(8,15,32,255),"navy fill");
            }
            var target=RenderTexture.GetTemporary(800,360,24); var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
            var texture=new Texture2D(800,360,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,800,360),0,0); texture.Apply();
                foreach(var point in new[]{new Vector2Int(0,0),new Vector2Int(799,0),new Vector2Int(0,359),new Vector2Int(799,359)})
                { var c=texture.GetPixel(point.x,point.y); Check(c.b>.02f && c.r+c.g+c.b>.03f,"black exposed corner"); }
                File.WriteAllBytes(output+"wide-zoom20.png",texture.EncodeToPNG());
            }
            finally { camera.targetTexture=oldTarget; RenderTexture.active=oldActive; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture); }
            result=new{status="PASS",fixedWorldExtent=true,navyCorners=true,introParallax=true,aspects=new[]{1600,2000},zooms=new[]{8,12,20}};
        }
        finally
        {
            camera.ResetAspect(); view.RequestView(oldView,0); save.ExitIsolated(); save.UseTestStorage(original); shell.Boot();
            foreach(var p in bytes) { var after=File.Exists(original+p.Key)?File.ReadAllBytes(original+p.Key):null; Check(p.Value==null?after==null:after!=null && p.Value.SequenceEqual(after),"save changed"); }
            if(Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
        }
        File.WriteAllText(output+"verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
