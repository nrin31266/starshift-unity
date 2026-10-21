using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class VerifyCampaignScore
{
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static bool Valid(SaveEnvelope e) => SaveValidation.IsValid(e, (m,c) => !string.IsNullOrEmpty(m) && !string.IsNullOrEmpty(c), 5);
    static CheckpointData Entry(int score = 0) => new CheckpointData { EncounterId = "ENCOUNTER_1", Lives = 3, Missiles = 1, WeaponLevels = new[] {1,0,0}, Stats = new RunStatsData { Score = score } };
    static CampaignProgress Open(string path) => new CampaignProgress(new JsonSaveStore(path), Valid);
    public static object Core()
    {
        string dir = Path.Combine(Path.GetTempPath(), "starshift-score-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir); var path = Path.Combine(dir, "save.json");
        try
        {
            var p = Open(path); Check(p.NewGame("L1", Entry()), "New Game");
            p.Capture(Entry(120)); Check(p.Flush(), "checkpoint");
            Check(p.TryCommitClear(300, 20, Entry(300), "L2", Entry(), CampaignStatus.Active), "same-region clear");
            Check(p.ScoreBeforeMission == 300 && p.Snapshot.Campaign.Checkpoint.Stats.Score == 0 && p.Snapshot.Profile.Records.Single().BestScore == 300, "mission total versus record");
            p = Open(path); Check(p.ScoreBeforeMission == 300, "disk reload baseline");
            p.Capture(Entry(50)); Check(p.Flush(), "incoming checkpoint"); p = Open(path);
            Check(p.ScoreBeforeMission + p.Snapshot.Campaign.Checkpoint.Stats.Score == 350, "checkpoint retry/continue total");
            var bytes = File.ReadAllBytes(path); Directory.CreateDirectory(path + ".tmp");
            Check(!p.TryCommitClear(200, 10, Entry(200), "L3", Entry(), CampaignStatus.Active), "failed clear must fail");
            Check(p.ScoreBeforeMission == 300 && p.MissionId == "L2" && p.Snapshot.Profile.Records.Length == 1 && bytes.SequenceEqual(File.ReadAllBytes(path)), "failed clear rollback");
            Directory.Delete(path + ".tmp");
            Check(p.TryCommitClear(200, 10, Entry(200), null, null, CampaignStatus.RegionTransition), "region marker");
            Check(p.ScoreBeforeMission == 300 && p.Snapshot.Campaign.Checkpoint.Stats.Score == 200, "marker keeps outgoing mission score");
            bytes = File.ReadAllBytes(path); Directory.CreateDirectory(path + ".tmp");
            Check(!p.TryAdvanceMission("L3", Entry()) && p.ScoreBeforeMission == 300 && p.MissionId == "L2" && bytes.SequenceEqual(File.ReadAllBytes(path)), "failed destination rollback");
            Directory.Delete(path + ".tmp");
            Check(p.TryAdvanceMission("L3", Entry()) && p.ScoreBeforeMission == 500, "region carry exactly once after retry");
            p = Open(path); Check(p.ScoreBeforeMission == 500 && p.Snapshot.Profile.Records.Single(r=>r.MissionId=="L2").BestScore == 200, "record remains mission local");
            Check(p.TryCommitClear(400, 30, Entry(400), null, null, CampaignStatus.CampaignCompleted), "final clear");
            p = Open(path); Check(p.ScoreBeforeMission + p.Snapshot.Campaign.Checkpoint.Stats.Score == 900, "completed Continue total");
            var records = JsonUtility.ToJson(p.Snapshot.Profile); Check(p.NewGame("L1", Entry()) && p.ScoreBeforeMission == 0 && JsonUtility.ToJson(p.Snapshot.Profile) == records, "new campaign resets only total");
            foreach (int version in new[] { 1, 2 })
            {
                var legacy = p.Snapshot; legacy.SchemaVersion = version;
                var json = Newtonsoft.Json.Linq.JObject.Parse(JsonUtility.ToJson(legacy));
                ((Newtonsoft.Json.Linq.JObject)json["Campaign"]).Remove("ScoreBeforeMission");
                File.WriteAllText(path, json.ToString()); bytes = File.ReadAllBytes(path); p = Open(path);
                Check(p.LoadStatus == SaveLoadStatus.Loaded && p.ScoreBeforeMission == 0 && bytes.SequenceEqual(File.ReadAllBytes(path)), "legacy baseline/no boot rewrite v" + version);
            }
            var invalid = p.Snapshot; invalid.Campaign.ScoreBeforeMission = -1; Check(!Valid(invalid), "negative baseline rejected");
            invalid.Campaign.ScoreBeforeMission = long.MaxValue; Check(!Valid(invalid), "overflow baseline rejected");
            invalid.Campaign.ScoreBeforeMission = (long)int.MaxValue + 900; Check(Valid(invalid), "aggregate supports long");
            Check(new JsonSaveStore(path).Write(invalid, Valid) && Open(path).ScoreBeforeMission == invalid.Campaign.ScoreBeforeMission, "long JSON roundtrip");
            Check(p.NewGame("L1", Entry()), "eight mission campaign");
            long bank = 0;
            for (int mission = 1; mission <= 8; mission++)
            {
                int score = mission * 101;
                Check(p.ScoreBeforeMission == bank, "L" + mission + " baseline");
                if (mission == 4)
                {
                    Check(p.TryCommitClear(score, 20, Entry(score), null, null, CampaignStatus.RegionTransition), "L4 marker");
                    p = Open(path);
                    Check(p.ScoreBeforeMission + p.Snapshot.Campaign.Checkpoint.Stats.Score == bank + score, "L4 Continue");
                    Check(p.TryAdvanceMission("L5", Entry()), "L5 destination");
                }
                else Check(p.TryCommitClear(score, 20, Entry(score), mission < 8 ? "L" + (mission + 1) : null,
                    mission < 8 ? Entry() : null, mission < 8 ? CampaignStatus.Active : CampaignStatus.CampaignCompleted), "clear L" + mission);
                bank += score; p = Open(path);
                Check(p.Snapshot.Profile.Records.Single(r => r.MissionId == "L" + mission).BestScore == Math.Max(score, mission == 1 ? 300 : mission == 2 ? 200 : mission == 3 ? 400 : 0), "mission record L" + mission);
            }
            Check(p.ScoreBeforeMission + p.Snapshot.Campaign.Checkpoint.Stats.Score == bank && bank == 3636, "L1-L8 total");
            return new {status="PASS", sameRegion=true, regionBoundary=true, rollback=true, checkpointTotal=true, recordsLocal=true, finalContinue=true, newGame=true, legacyV1V2=true, validation=true, l1ToL8Total=bank};
        }
        finally { Directory.Delete(dir, true); }
    }

    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static CampaignLifecycle Save => Runner.GetComponent<CampaignLifecycle>();
    static RunRewards Rewards => Runner.GetComponent<RunRewards>();
    static GameplaySession Session => Runner.GetComponent<GameplaySession>();
    static async Task Until(Func<bool> condition, int ms, string label)
    { var end=Time.realtimeSinceStartup+ms/1000f; while(!condition() && Time.realtimeSinceStartup<end) { CheckBackground(); await Task.Delay(30); } Check(condition(),label); }
    static void CheckBackground()
    {
        var camera=Camera.main;
        Check(camera.transform.Find("SpaceEnvironment").localScale==Vector3.one,"fixed background during intro/transition");
        var c=camera.backgroundColor; Check(c.b > .025f && c.a==1 && camera.clearFlags==CameraClearFlags.SolidColor,"nonblack journey fill");
    }
    static string ResultPath => "../.design/score-carry/runtime.json";
    public static object Begin()
    {
        Check(Application.isPlaying && Time.frameCount > 1 && Save.Mode == SessionMode.Frontend && !Runner.HasActiveRun, "Wait for initialized Menu in fresh Play Mode");
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath)); File.WriteAllText(ResultPath,"{\"status\":\"RUNNING\"}");
        _ = Runtime(); return new {started=true};
    }
    static async Task Runtime()
    {
        object result = null; var original = Save.SavePath;
        var snapshots = new[]{"",".bak",".invalid"}.ToDictionary(s=>s,s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null);
        var path=Path.Combine(Path.GetTempPath(),"starshift-score-runtime-"+Guid.NewGuid().ToString("N"),"save.json");
        var damage=GameObject.Find("Player").GetComponent<PlayerDamageLifecycle>(); var oldDamage=damage.enabled;
        var devices=InputSystem.devices.Where(d=>d.native && d.enabled && (d is UnityEngine.InputSystem.Pointer || d is Keyboard || d is Gamepad)).ToArray();
        EditorApplication.LockReloadAssemblies();
        try
        {
            foreach(var d in devices)InputSystem.DisableDevice(d);
            Save.UseTestStorage(path); Save.GetComponent<GameShell>().Boot();
            var camera=Camera.main; var view=camera.GetComponent<GameplayViewController>(); var oldView=view.TargetView;
            var background=camera.transform.Find("SpaceEnvironment").GetComponent<SpriteRenderer>();
            foreach(float aspect in new[]{1600f/900,2000f/900}) foreach(float size in new[]{8f,12f,20f})
            {
                camera.aspect=aspect; view.RequestView(new GameplayViewSettings(size,new Rect(0,0,1,1)),0); await Task.Delay(100);
                Check(background.transform.localScale == Vector3.one, "background fixed world scale");
                Check(camera.clearFlags == CameraClearFlags.SolidColor && camera.backgroundColor == (Color)new Color32(8,15,32,255), "navy clear outside patch");
            }
            camera.ResetAspect(); view.RequestView(oldView,0);
            UnityEngine.Object.FindAnyObjectByType<ShellView>().NewGame.onClick.Invoke();
            await Until(()=>!Save.GetComponent<GameShell>().IsBusy && Save.Mode==SessionMode.Campaign,6000,"new game fixture");
            await Until(()=>Runner.Flow.CanContinue,6000,"intro briefing");
            Session.BeginMission();
            await Until(()=>Runner.Stage==EncounterStage.Running,15000,"intro approach/travel/brake");
            Save.ExitIsolated();
            var fixture=Save.PersistentSnapshot; fixture.Campaign.ScoreBeforeMission=700;
            fixture.Campaign.Checkpoint=Entry(200); fixture.Campaign.Checkpoint.EncounterId="ENCOUNTER_2";
            Check(new JsonSaveStore(path).Write(fixture,Valid),"fixture write"); Save.UseTestStorage(path);
            Check(Save.Continue(),"continue last encounter"); damage.enabled=false;
            await Until(()=>Runner.Stage==EncounterStage.Running,10000,"last encounter starts");
            var end=Time.realtimeSinceStartup+25;
            while(Save.PersistentSnapshot.Campaign.MissionId=="L1" && Time.realtimeSinceStartup<end)
            {
                foreach(var enemy in EnemyHealth.Active.Where(e=>e.IsAlive && e.CanReceiveDamage).ToArray()) enemy.ReceivePlayerHit(enemy.CurrentHp,damage.gameObject);
                await Task.Delay(50);
            }
            Check(Save.PersistentSnapshot.Campaign.MissionId=="L2","real mission clear to incoming checkpoint");
            var missionScore=Save.PersistentSnapshot.Profile.Records.Single(r=>r.MissionId=="L1").BestScore;
            var total=700L+missionScore;
            Check(Save.DisplayScore==total,"travel must not double count outgoing score");
            await Until(()=>Runner.Flow.Level.Id=="L2" && Runner.Stage==EncounterStage.Running,15000,"incoming mission running");
            Check(Rewards.Score==0 && Save.DisplayScore==total,"next mission keeps campaign score");
            Rewards.AddKill(75); await Task.Delay(100);
            var hud=UnityEngine.Object.FindAnyObjectByType<GameplayHud>();
            var label=(Text)typeof(GameplayHud).GetField("scoreLabel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            Check(Save.DisplayScore==total+75 && label.text==(total+75).ToString(),"actual HUD cumulative total");
            Session.Pause(); Check(Save.RetryEncounter(),"retry");
            Check(Rewards.Score==0 && Save.DisplayScore==total,"retry rolls back only current encounter score");
            Check(Save.BeginReplay("L1"),"replay"); Rewards.AddKill(11);
            Check(Save.DisplayScore==11,"replay score isolated"); var saved=File.ReadAllBytes(path);
            Check(Save.Continue() && Save.DisplayScore==total && saved.SequenceEqual(File.ReadAllBytes(path)),"continue restores total without replay writeback");
            Save.ExitIsolated(); fixture=Save.PersistentSnapshot;
            fixture.Campaign.MissionId="L4"; fixture.Campaign.Status=CampaignStatus.RegionTransition;
            fixture.Campaign.ScoreBeforeMission=900; fixture.Campaign.Checkpoint=Entry(300); fixture.Campaign.Checkpoint.EncounterId=Runner.GetComponent<RegionCampaignFlow>().FindMission("L4").Encounters.Last().Id;
            fixture.Profile.UnlockedMissions=new[]{"L1","L2","L3","L4"};
            Check(new JsonSaveStore(path).Write(fixture,Valid),"region fixture"); Save.UseTestStorage(path); Check(Save.Continue(),"transition continue");
            Check(Save.DisplayScore==1200,"region travel total");
            await Until(()=>Runner.GetComponent<PlanetJourney>().IsAwaitingTap,20000,"region planet reveal");
            Check(Runner.GetComponent<PlanetJourney>().AcceptTap(),"region tap");
            await Until(()=>Save.PersistentSnapshot.Campaign.MissionId=="L5",20000,"real Up to Right destination commit");
            Check(Save.DisplayScore==1200 && Save.PersistentSnapshot.Campaign.ScoreBeforeMission==1200,"region transition carry once");
            result=new{status="PASS",missionScore,campaignScore=total,realMissionClear=true,travelNoDoubleCount=true,hud=true,retry=true,continueRestore=true,replayIsolation=true,regionTransition=true,backgroundCoverage="fixed scale + navy fill; both aspects x zoom 8/12/20; real intro/same-region/Up-to-Right transition"};
        }
        catch(Exception e){result=new{status="FAILED",error=e.ToString()};}
        finally
        {
            try
            {
                Save.ExitIsolated(); Save.UseTestStorage(original); Save.GetComponent<GameShell>().Boot(); damage.enabled=oldDamage;
                foreach(var pair in snapshots){var after=File.Exists(original+pair.Key)?File.ReadAllBytes(original+pair.Key):null;Check(pair.Value==null?after==null:after!=null && pair.Value.SequenceEqual(after),"production save changed "+pair.Key);}
            }
            catch(Exception e){result=new{status="FAILED",originalResult=result,cleanupError=e.ToString()};}
            finally{foreach(var d in devices)if(d.added)InputSystem.EnableDevice(d);EditorApplication.UnlockReloadAssemblies();}
            File.WriteAllText(ResultPath,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        }
    }
}
