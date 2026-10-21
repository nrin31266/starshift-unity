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

public static class VerifyTravelPresentation
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
        var player=GameObject.Find("Player");var pointer=player.GetComponent<StarShift.Player.InputSystemPointerInput>();var damage=player.GetComponent<StarShift.Player.PlayerDamageLifecycle>();
        bool oldPointer=pointer.enabled,oldDamage=damage.enabled;
        var temp=Path.Combine(Path.GetTempPath(),"starshift-bg-"+Guid.NewGuid().ToString("N"),"save.json");
        var camera=Camera.main; var view=camera.GetComponent<GameplayViewController>(); var oldView=view.TargetView;
        var bg=camera.transform.Find("SpaceEnvironment"); var worldSize=bg.GetComponent<SpriteRenderer>().bounds.size;
        var output="../.design/presentation-polish/screenshots/"; Directory.CreateDirectory(output);
        object result=null;
        try
        {
            save.UseTestStorage(temp); shell.Boot();pointer.enabled=false;damage.enabled=false; UnityEngine.Object.FindAnyObjectByType<ShellView>().NewGame.onClick.Invoke();
            await Until(()=>!shell.IsBusy && session.Flow.CanContinue,6000);
            var intro=UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>();
            var tiles=camera.transform.Find("TravelBackdrop").GetComponentsInChildren<SpriteRenderer>();
            Check(tiles.Length==50,"Two tiled native-size background layers");
            var departure=tiles.First(r=>r.name=="Departure");
            var tilePosition=departure.transform.localPosition;
            await Task.Delay(150);
            Check(departure.transform.localPosition!=tilePosition,"Establishing background is static");
            var stars=(Transform)typeof(MissionIntroPresentation).GetField("stars",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(intro);
            var star=stars.GetComponentsInChildren<SpriteRenderer>().First(r=>r.isVisible && r.color.a>0);
            var pos=star.transform.position; await Task.Delay(200);
            Check(star.transform.position!=pos && star.isVisible,"intro parallax visible/moving");
            session.BeginMission(); await Until(()=>session.Flow.IntroPhase==MissionIntroPhase.Travel,3000);
            var progress=typeof(MissionIntroPresentation).GetField("progress",BindingFlags.Instance|BindingFlags.NonPublic);
            await Until(()=>session.Flow.IntroPhase==MissionIntroPhase.Travel&&(float)progress.GetValue(intro)>.55f,2500);
            tilePosition=departure.transform.localPosition;await Task.Delay(150);
            Check(Vector3.Distance(tilePosition,departure.transform.localPosition)>.1f,"Travel environment does not accelerate");
            star=stars.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.isVisible).OrderByDescending(r=>r.color.a).First();
            Check(star.sprite.name=="speed-streak"&&star.color.a>.7f,"Speed streak missing or dim");
            Check(star.bounds.size.y/(camera.orthographicSize*2)*900>40,"Travel streak too short");
            Check(bg.localScale==Vector3.one,"intro fixed scale");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(output+"intro-travel.png"));
            await Until(()=>runner.Stage==EncounterStage.Running,10000);
            intro.PresentMissionTravel(.5f,PlayDirection.Up);await Task.Delay(100);
            tilePosition=departure.transform.localPosition;await Task.Delay(100);
            Check(Vector3.Distance(tilePosition,departure.transform.localPosition)>.1f,"Mission background is static");
            Check(stars.GetComponentsInChildren<SpriteRenderer>().Max(r=>r.color.a)>.7f,"Mission transition remains dim");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(output+"mission-speed.png"));
            await Task.Delay(100);intro.Hide();
            intro.PresentHandoff(.5f,PlayDirection.Up);await Task.Delay(100);
            tilePosition=departure.transform.localPosition;await Task.Delay(100);
            Check(Vector3.Distance(tilePosition,departure.transform.localPosition)>.1f,"Encounter background is static");
            Time.timeScale=0;await Task.Delay(60);tilePosition=departure.transform.localPosition;await Task.Delay(100);
            Check(departure.transform.localPosition==tilePosition,"Paused background kept scrolling");
            Time.timeScale=1;intro.Hide();
            runner.StopRun();Time.timeScale=1;
            var journey=runner.GetComponent<PlanetJourney>();
            journey.Begin(runner.GetComponent<RegionCampaignFlow>().FindMission("L5"),()=>{});
            await Until(()=>journey.Phase==PlanetJourneyPhase.Turn,5000);await Task.Delay(250);
            float lastAlpha=-1;int blendSamples=0;
            for(int frame=0;frame<8;frame++)
            {
                var arrival=tiles.First(r=>r.name=="Arrival");
                Check(departure.sprite.name=="background"&&arrival.sprite.name=="crimson-background","Both region artworks must coexist");
                Check(arrival.color.a>=lastAlpha,"Region blend moved backward");lastAlpha=arrival.color.a;
                if(arrival.color.a>0 && arrival.color.a<1)blendSamples++;
                var path=Path.GetFullPath(output+"journey-"+frame+".png");if(File.Exists(path))File.Delete(path);
                ScreenCapture.CaptureScreenshot(path);await Until(()=>File.Exists(path),2000);await Task.Delay(100);
            }
            Check(blendSamples>=3,"No gradual multi-frame region blend");
            Check(stars.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.isVisible&&r.color.a>.7f),"Region streaks remain dim");
            await Until(()=>journey.IsAwaitingTap,6000);
            Check(journey.AcceptTap(),"Destination tap rejected");await Task.Delay(100);
            Check(tiles.First(r=>r.name=="Arrival").color.a==1,"Destination reverted during approach");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(output+"destination-approach.png"));await Task.Delay(100);
            journey.Cancel();intro.ResetEnvironment();
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
            result=new{status="PASS",nativeTileSize=true,navyCorners=true,movingIntroBackground=true,movingMissionBackground=true,regionBlendSamples=blendSamples,destinationRetainedOnApproach=true,aspects=new[]{1600,2000},zooms=new[]{8,12,20}};
        }
        finally
        {
            await Until(()=>!shell.IsBusy,5000);
            runner.GetComponent<PlanetJourney>().Cancel();runner.StopRun();camera.ResetAspect(); view.RequestView(oldView,0); save.ExitIsolated(); save.UseTestStorage(original); shell.Boot();pointer.enabled=oldPointer;damage.enabled=oldDamage;
            foreach(var p in bytes) { var after=File.Exists(original+p.Key)?File.ReadAllBytes(original+p.Key):null; Check(p.Value==null?after==null:after!=null && p.Value.SequenceEqual(after),"save changed"); }
            if(Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
        }
        File.WriteAllText(output+"verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
