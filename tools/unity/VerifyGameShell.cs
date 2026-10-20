using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class VerifyGameShell
{
    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static CampaignLifecycle Save => Runner.GetComponent<CampaignLifecycle>();
    static GameplaySession Session => Runner.GetComponent<GameplaySession>();
    static RegionCampaignFlow Region => Runner.GetComponent<RegionCampaignFlow>();
    static GameShell Shell => Runner.GetComponent<GameShell>();
    static ShellView View => UnityEngine.Object.FindAnyObjectByType<ShellView>();
    static RunRewards Rewards => Runner.GetComponent<RunRewards>();
    static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static Task currentTask;
    static void Check(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
    static void Evidence(string name,object value)
    { Directory.CreateDirectory("../.design/m7");File.WriteAllText("../.design/m7/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented)); }
    static async Task Until(Func<bool> condition,int ms,string message)
    {var end=Time.realtimeSinceStartup+ms/1000f;while(!condition()&&Time.realtimeSinceStartup<end)await Task.Delay(25);Check(condition(),message+" (screen="+Shell.Screen+", stage="+Runner.Stage+", error="+Shell.LastOperationError+")");}
    static object Begin(string name,Func<Task<object>> action)
    {
        Check(Application.isPlaying,"Play Mode required");Check(currentTask==null||currentTask.IsCompleted,"Another M7 probe is live");
        Evidence(name,new{status="RUNNING"});currentTask=Run(name,action);return new{started=name};
    }
    static async Task Run(string name,Func<Task<object>> action)
    {EditorApplication.LockReloadAssemblies();try{Evidence(name,await action());}catch(Exception e){Evidence(name,new{status="FAILED",error=e.ToString()});}finally{EditorApplication.UnlockReloadAssemblies();}}
    public static object BeginCore()=>Begin("core",Core);
    public static object BeginFlow()=>Begin("flow",Flow);
    public static object BeginStats()=>Begin("stats",Stats);
    public static object BeginFailures()=>Begin("failures",Failures);
    public static object BeginSceneRelaunch()=>Begin("scene-relaunch",SceneRelaunch);
    public static object BeginInput()=>Begin("input",Input);
    public static object BeginLoading()=>Begin("loading",Loading);
    public static object BeginRecovery()=>Begin("recovery",Recovery);
    public static object BeginTeardown()=>Begin("teardown",Teardown);
    public static object BeginHangar(int width)=>Begin("hangar-"+width,()=>Hangar(width));
    public static object BeginTerminalLayouts(int width)=>Begin("terminal-layouts-"+width,()=>TerminalLayouts(width));
    public static object BeginLayouts(int width)=>Begin("layouts-"+width,()=>Layouts(width));
    public static object Inspect()=>new{task=currentTask?.Status.ToString(),screen=Shell.Screen.ToString(),busy=Shell.IsBusy,mission=Runner.Flow.Level.Id,stage=Runner.Stage.ToString(),pending=Save.HasPendingClear,Save.Dirty,Save.Diagnostic};
    static async Task Capture(string name)
    {await Task.Delay(120);Canvas.ForceUpdateCanvases();Directory.CreateDirectory("../.design/m7/screenshots");ScreenCapture.CaptureScreenshot(Path.GetFullPath("../.design/m7/screenshots/"+name+".png"));await Task.Delay(180);}
    static async Task Click(Button button)
    {
        await Task.Delay(75);Canvas.ForceUpdateCanvases();
        Check(button.gameObject.activeInHierarchy && button.interactable,"Inactive button "+button.name);
        var rect=(RectTransform)button.transform;
        var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        int clicked=0;UnityEngine.Events.UnityAction observed=()=>clicked++;button.onClick.AddListener(observed);
        try{await activeRig.Mouse(point,false);await activeRig.Mouse(point,true);await activeRig.Mouse(point,false);}
        finally{button.onClick.RemoveListener(observed);}
        if(clicked!=1)
        {
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            throw new InvalidOperationException("UI click not received "+button.name+" at "+point+" screen="+UnityEngine.Screen.width+"x"+UnityEngine.Screen.height+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name)));
        }
        await Until(()=>!Shell.IsBusy,4000,"Transition stuck after "+button.name);
    }
    static UiRig activeRig;
    sealed class UiRig : IDisposable
    {
        public readonly GameObject Player=GameObject.Find("Player");
        public readonly InputSystemPointerInput Input;
        public readonly PlayerGameplayGate Gate;
        readonly Mouse mouse,oldMouse;
        readonly Touchscreen touch;
        readonly List<InputDevice> disabled=new List<InputDevice>();
        readonly InputSettings.EditorInputBehaviorInPlayMode oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        readonly InputSettings.BackgroundBehavior oldBackground=InputSystem.settings.backgroundBehavior;
        readonly object gameView;
        readonly object oldSize;
        readonly PropertyInfo selectedSize;
        readonly string originalPath;
        readonly float oldScale=Time.timeScale>0?Time.timeScale:(float)typeof(GameplaySession).GetField("previousScale",Fields).GetValue(Session);
        readonly bool oldAudio=Time.timeScale>0?AudioListener.pause:(bool)typeof(GameplaySession).GetField("previousAudioPause",Fields).GetValue(Session);
        readonly Dictionary<string,byte[]> production=new Dictionary<string,byte[]>();
        public readonly string Path;
        public int BackgroundResumes;
        public UiRig(int width,string path=null)
        {
            Input=Player.GetComponent<InputSystemPointerInput>();Gate=Player.GetComponent<PlayerGameplayGate>();
            originalPath=Save.SavePath;
            foreach(var suffix in new[]{"",".bak",".invalid"})production[suffix]=File.Exists(originalPath+suffix)?File.ReadAllBytes(originalPath+suffix):null;
            Path=path??System.IO.Path.Combine(System.IO.Path.GetTempPath(),"starshift-m7-"+Guid.NewGuid().ToString("N"),"save.json");
            Save.UseTestStorage(Path);Shell.Boot();
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");gameView=Resources.FindObjectsOfTypeAll(type).FirstOrDefault()??EditorWindow.GetWindow(type);
            selectedSize=type.GetProperty("selectedSizeIndex");oldSize=selectedSize.GetValue(gameView);
            type.GetMethod("SetCustomResolution",Fields).Invoke(gameView,new object[]{new Vector2(width,900),"StarShift M7 QA "+width+"x900"});
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            oldMouse=UnityEngine.InputSystem.Mouse.current;
            foreach(var device in InputSystem.devices)if(device.native&&device.enabled&&(device is UnityEngine.InputSystem.Mouse||device is Touchscreen)){disabled.Add(device);InputSystem.DisableDevice(device);}
            mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();touch=InputSystem.AddDevice<Touchscreen>();activeRig=this;
        }
        public void Protect(){Player.GetComponent<PlayerDamageLifecycle>().enabled=false;Gate.SetState(PlayerGameplayState.PostEntryInvincibility);}
        public async Task Mouse(Vector2 point,bool held)
        {var state=new MouseState{position=point};if(held)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(mouse,state);await Task.Delay(80);}
        public async Task Touch(int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 point)
        {InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=phase,position=point});await Task.Delay(100);}
        public void Dispose()
        {
            if(Directory.Exists(Path+".tmp"))Directory.Delete(Path+".tmp");
            Save.ExitIsolated();Save.UseTestStorage(originalPath);Time.timeScale=oldScale;AudioListener.pause=oldAudio;Shell.Boot();
            Player.GetComponent<PlayerDamageLifecycle>().enabled=true;
            if(mouse.added)InputSystem.RemoveDevice(mouse);
            if(touch.added)InputSystem.RemoveDevice(touch);
            foreach(var device in disabled)if(device.added)InputSystem.EnableDevice(device);
            if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();
            InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;InputSystem.settings.backgroundBehavior=oldBackground;
            selectedSize.SetValue(gameView,oldSize);
            foreach(var item in production){var now=File.Exists(originalPath+item.Key)?File.ReadAllBytes(originalPath+item.Key):null;Check((now==null&&item.Value==null)||(now!=null&&item.Value!=null&&now.SequenceEqual(item.Value)),"Production file changed "+item.Key);}
            activeRig=null;
        }
    }
    static Task<object> Core()
    {
        Check(ShellCopy.Time(3601)=="1:00:01"&&!string.IsNullOrEmpty(ShellCopy.Time(float.MaxValue)),"Time display overflow/wrap");
        var path=Path.Combine(Path.GetTempPath(),"starshift-m7-core-"+Guid.NewGuid().ToString("N"),"save.json");
        bool Valid(SaveEnvelope d)=>SaveValidation.IsValid(d,(m,e)=>Region.FindMission(m)?.Encounters.Any(x=>x.Id==e)==true,3);
        CheckpointData Entry()=>new CheckpointData{EncounterId=Region.Missions[0].Encounters[0].Id,Lives=3,Missiles=1,WeaponLevels=new[]{1,0,0},Stats=new RunStatsData()};
        var progress=new CampaignProgress(new JsonSaveStore(path),Valid);
        Check(progress.LoadStatus==SaveLoadStatus.Missing&&progress.NewGame("L1",Entry()),"Missing/NewGame");
        progress.CreditEncounterShards(7);Check(progress.Flush(),"Wallet flush");
        var before=File.ReadAllBytes(path);
        Directory.CreateDirectory(path+".tmp");
        Check(!progress.NewGame("L1",Entry())&&progress.Snapshot.Profile.StellarShardBalance==7&&File.ReadAllBytes(path).SequenceEqual(before),"NewGame rollback");
        Check(!progress.TryCommitClear(500,30,Entry(),null,null,CampaignStatus.MissionCleared)&&progress.Snapshot.Profile.Records.Length==0&&progress.Snapshot.Campaign.Status==CampaignStatus.Active,"Failed record falsely committed");
        var changed=SettingsData.Defaults();changed.MusicVolume=.2f;
        Check(!progress.SetSettings(changed)&&progress.Snapshot.Settings.MusicVolume==1,"Settings rollback");
        Directory.Delete(path+".tmp");
        Check(progress.TryCommitClear(500,30,Entry(),null,null,CampaignStatus.MissionCleared),"Clear commit");
        Check(progress.NewGame("L1",Entry())&&progress.TryCommitClear(100,40,Entry(),null,null,CampaignStatus.MissionCleared),"Worse clear");
        var record=progress.Snapshot.Profile.Records.Single();Check(record.BestScore==500&&record.BestTimeSeconds==30,"Worse best replacement");
        File.WriteAllText(path,"broken");var backup=new CampaignProgress(new JsonSaveStore(path),Valid);Check(backup.LoadStatus==SaveLoadStatus.RecoveredBackup,"Backup recovery");
        File.Delete(path+".bak");var invalid=new CampaignProgress(new JsonSaveStore(path),Valid);Check(invalid.Snapshot==null&&!invalid.NewGame("L1",Entry())&&File.ReadAllText(path)=="broken","Invalid data destroyed");
        Check(invalid.RecoverNewGame("L1",Entry())&&File.ReadAllText(path+".invalid")=="broken","Explicit malformed recovery lost bytes");
        var salvage=progress.Snapshot;salvage.Campaign.Checkpoint.EncounterId="MISSING";salvage.Settings.MusicVolume=.2f;
        File.WriteAllText(path,JsonUtility.ToJson(salvage));
        var repair=new CampaignProgress(new JsonSaveStore(path),Valid);Check(repair.LoadStatus==SaveLoadStatus.Invalid,"Invalid checkpoint fixture");
        Directory.CreateDirectory(path+".tmp");Check(!repair.RecoverNewGame("L1",Entry())&&repair.Snapshot==null,"Failed recovery exposed state");Directory.Delete(path+".tmp");
        Check(repair.RecoverNewGame("L1",Entry())&&repair.Snapshot.Profile.StellarShardBalance==7&&repair.Snapshot.Profile.Records.Length==1&&repair.Snapshot.Settings.MusicVolume==.2f,"Metadata not preserved");
        var defaults=new[]{"chassis-arrow","skin-cyan","trail-none"};
        Check(repair.EquipCosmetic("skin-amber",6,1,defaults)&&repair.Snapshot.Profile.StellarShardBalance==1,"Cosmetic spend");
        Check(repair.EquipCosmetic("skin-amber",6,1,defaults)&&repair.Snapshot.Profile.StellarShardBalance==1,"Owned cosmetic charged twice");
        var profileBefore=JsonUtility.ToJson(repair.Snapshot.Profile);
        Check(!repair.EquipCosmetic("chassis-swept",12,0,defaults)&&JsonUtility.ToJson(repair.Snapshot.Profile)==profileBefore,"Insufficient Shards changed state");
        Directory.CreateDirectory(path+".tmp");Check(!repair.EquipCosmetic("trail-ion",1,2,defaults)&&JsonUtility.ToJson(repair.Snapshot.Profile)==profileBefore,"Failed cosmetic purchase spent Shards");Directory.Delete(path+".tmp");
        Check(repair.NewGame("L1",Entry())&&JsonUtility.ToJson(repair.Snapshot.Profile)==profileBefore,"NewGame reset cosmetics");
        File.WriteAllText(path,"{\"SchemaVersion\":99}");var unsupported=new CampaignProgress(new JsonSaveStore(path),Valid);
        Check(unsupported.LoadStatus==SaveLoadStatus.UnsupportedVersion&&!unsupported.RecoverNewGame("L1",Entry())&&File.ReadAllText(path)=="{\"SchemaVersion\":99}","Unsupported overwrite");
        return Task.FromResult<object>(new{status="PASS",newGameRollback=true,clearRollback=true,settingsRollback=true,bestRecords=true,backupRecovery=true,invalidPreserved=true,explicitRecovery=true,metadataPreserved=true,unsupportedPreserved=true,cosmeticTransactions=true});
    }
    static async Task EnterGameplay(UiRig rig)
    {
        await Until(()=>Runner.Flow.CanContinue,4500,"Establishing prompt");
        await rig.Mouse(new Vector2(600,400),true);await rig.Mouse(new Vector2(600,400),false);
        var end=Time.realtimeSinceStartup+7;
        while(Runner.Stage!=EncounterStage.Running&&Time.realtimeSinceStartup<end)
        {
            // Desktop có thể mất focus khi người dùng đang chat; phục hồi bằng đúng UI Resume.
            if(Session.IsPaused)
            {
                await Until(()=>Shell.Screen==ShellScreen.Pause,1000,"Background Pause UI");
                await Click(View.Resume);rig.BackgroundResumes++;
            }
            await Task.Delay(25);
        }
        Check(Runner.Stage==EncounterStage.Running,"First combat");rig.Protect();
    }
    static async Task ClearCurrent(UiRig rig,int timeout=50000)
    {
        var end=Time.realtimeSinceStartup+timeout/1000f;Time.timeScale=12;
        while(!Session.IsTerminal&&Time.realtimeSinceStartup<end)
        {
            if(Runner.Flow.CanContinue)Session.BeginMission();
            rig.Protect();
            foreach(var enemy in EnemyHealth.Active.ToArray())
            {
                if(!enemy.IsAlive)continue;
                var boss=enemy.GetComponent<BossPhaseRunner>();
                if(boss?.Shields!=null&&!boss.Shields.IsExposed)
                    foreach(var node in enemy.GetComponentsInChildren<EnemyHealth>().Where(n=>n!=enemy&&n.CanReceiveDamage))node.ReceivePlayerHit(node.CurrentHp,rig.Player);
                if(enemy.CanReceiveDamage)enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
            }
            foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>())pickup.Release();
            await Task.Delay(25);
        }
        Check(Session.IsClear,"Mission clear timeout "+Runner.Flow.Level.Id);await Until(()=>Shell.Screen==ShellScreen.Results,1000,"Result navigation");
    }
    static async Task<object> Flow()
    {
        using(var rig=new UiRig(1600))
        {
            await Task.Delay(300);Check(Shell.Screen==ShellScreen.Menu&&!View.Continue.interactable&&!Runner.HasActiveRun,"No-save boot");await Capture("menu-no-save-1600");
            await Click(View.Help);Check(Shell.Screen==ShellScreen.Help,"Help");await Capture("help-1600");await Click(View.BackButtons[2]);
            await Click(View.Credits);Check(Shell.Screen==ShellScreen.Credits,"Credits");await Click(View.BackButtons[3]);
            await Click(View.Settings);Check(Shell.Screen==ShellScreen.Settings,"Menu Settings");
            View.Music.value=.35f;View.Sfx.value=.6f;View.Vibration.isOn=false;View.Shake.isOn=false;await Click(View.SaveSettings);
            Check(Save.PersistentSnapshot.Settings.MusicVolume==.35f,"Settings not persisted");
            await Click(View.NewGame);await EnterGameplay(rig);Check(!rig.Input.FireHeld,"NewGame phantom fire");await Capture("hud-1600");
            var stats=Rewards.Capture();await Task.Delay(250);Check(Rewards.PlaySeconds>stats.PlaySeconds,"Timer inactive");
            var hud=UnityEngine.Object.FindAnyObjectByType<GameplayHud>();var pause=(Button)typeof(GameplayHud).GetField("pauseButton",Fields).GetValue(hud);
            await Click(pause);Check(Session.IsPaused&&Shell.Screen==ShellScreen.Pause&&!rig.Input.FireHeld,"Pause/UI leak");
            float seconds=Rewards.PlaySeconds;await Task.Delay(250);Check(Rewards.PlaySeconds==seconds,"Pause timer inflated");await Capture("pause-1600");
            await Click(View.PauseSettings);Check(View.Music.value==.35f&&!View.Shake.isOn,"Shared Settings");await Capture("settings-1600");
            var position=rig.Player.transform.position;var slider=(RectTransform)View.Music.transform;var point=RectTransformUtility.WorldToScreenPoint(null,slider.TransformPoint(slider.rect.center));
            await rig.Mouse(point,true);await rig.Mouse(point+new Vector2(80,0),true);await rig.Mouse(point,false);
            Check(!rig.Input.FireHeld&&rig.Player.transform.position==position,"Settings drag moved/fired Player");
            await Click(View.SaveSettings);Check(Shell.Screen==ShellScreen.Pause,"Settings Back origin");await Click(View.Resume);Check(!Session.IsPaused&&!rig.Input.FireHeld,"Resume phantom press");rig.Protect();
            await ClearCurrent(rig);Check(View.ResultStats.text.Contains(Rewards.Score.ToString("N0")),"HUD/result score mismatch");
            Check(Save.PersistentSnapshot.Profile.Records.Any(r=>r.MissionId=="L1")&&View.Next.gameObject.activeSelf,"Clear record/Next");await Capture("clear-1600");
            await Click(View.ResultMenu);Check(Shell.Screen==ShellScreen.Menu,"Result Menu");
            // Tạo lại storage service; màn hình đích vẫn đi qua nút Continue thật.
            Save.UseTestStorage(rig.Path);Shell.Boot();await Click(View.Continue);await Until(()=>Runner.Flow.Level.Id=="L2"&&Runner.HasActiveRun,2000,"Checkpoint Continue L2");rig.Protect();
            await Click(pause);await Click(View.PauseMenu);await Capture("confirmation-exit-1600");await Click(View.Cancel);Check(Shell.Screen==ShellScreen.Pause&&Session.IsPaused,"Cancel restore");
            await Click(View.PauseMenu);await Click(View.Confirm);await Click(View.Missions);Check(Shell.Screen==ShellScreen.Missions,"Mission Hub");
            Check(View.MissionCards.Length==8&&View.MissionCards[0].interactable&&View.MissionCards[1].interactable&&!View.MissionCards[2].interactable,"Unlock catalog");await Capture("missions-1600");
            var production=File.ReadAllBytes(rig.Path);await Click(View.MissionCards[0]);await Click(View.LaunchReplay);rig.Protect();
            Check(Save.Mode==SessionMode.Replay&&Runner.Flow.Level.Id=="L1","Replay launch");
            await ClearCurrent(rig);Check(!View.Next.gameObject.activeSelf&&View.ResultReplay.gameObject.activeSelf,"Replay chaining");await Capture("replay-clear-1600");
            await Click(View.ResultMissions);Check(File.ReadAllBytes(rig.Path).SequenceEqual(production),"Replay wrote campaign/profile");await Click(View.BackButtons[0]);await Click(View.Continue);rig.Protect();Check(Runner.Flow.Level.Id=="L2","Replay→Campaign snapshot");
            var lives=rig.Player.GetComponent<PlayerLives>();rig.Player.GetComponent<PlayerDamageLifecycle>().enabled=true;lives.RestoreCheckpoint(1);rig.Gate.SetState(PlayerGameplayState.Active);Check(rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null),"Fatal hit");
            await Until(()=>Shell.Screen==ShellScreen.Results&&Session.IsTerminal&&!Session.IsClear,3500,"Game Over");await Capture("game-over-1600");await Click(View.GameOverRetry);Check(Runner.HasActiveRun&&lives.CurrentLives>0,"GameOver Retry");rig.Protect();
            // Shell remains the navigation owner; only combat is accelerated below.
            var results=new List<object>();
            for(int mission=2;mission<=8;mission++)
            {
                await ClearCurrent(rig);results.Add(new{mission,score=Rewards.Score,kills=Rewards.EnemiesDestroyed,seconds=Rewards.PlaySeconds});
                Check(Save.PersistentSnapshot.Profile.Records.Any(r=>r.MissionId=="L"+mission),"Missing record L"+mission);
                if(mission<8)
                {
                    await Click(View.Next);
                    var travelEnd=Time.realtimeSinceStartup+12;
                    while((Runner.Flow.Level.Id!="L"+(mission+1)||!Runner.HasActiveRun)&&Time.realtimeSinceStartup<travelEnd)
                    {
                        if(Runner.Flow.CanContinue){await rig.Mouse(new Vector2(600,400),true);await rig.Mouse(new Vector2(600,400),false);}
                        await Task.Delay(25);
                    }
                    Check(Runner.Flow.Level.Id=="L"+(mission+1)&&Runner.HasActiveRun,"Next/journey L"+(mission+1));rig.Protect();
                }
            }
            Check(Save.PersistentSnapshot.Campaign.Status==CampaignStatus.CampaignCompleted&&!View.Next.gameObject.activeSelf&&View.ResultMissions.gameObject.activeSelf,"Victory release boundary");await Capture("victory-1600");
            await Click(View.ResultMenu);Save.UseTestStorage(rig.Path);Shell.Boot();await Click(View.Continue);
            Check(Session.IsCampaignVictory&&!Runner.HasActiveRun&&Save.PersistentSnapshot.Profile.Records.Length==8,"Completed Continue respawn/reward");
            await Capture("victory-continue-1600");await Click(View.ResultMenu);await Click(View.NewGame);await Capture("confirmation-new-1600");await Click(View.Cancel);
            Check(Save.PersistentSnapshot.Campaign.Status==CampaignStatus.CampaignCompleted,"Canceled NewGame mutation");
            return new{status="PASS",productionEntry=true,settings=true,pause=true,uiInput=true,clear=true,relaunch=true,missionHub=true,replayIsolation=true,gameOverRetry=true,regionAToB=true,victoryContinue=true,records=8,results,savePath=rig.Path};
        }
    }
    static async Task<object> Stats()
    {
        using(var rig=new UiRig(1600))
        {
            void Start(int mission,int encounter)
            {
                Runner.StopRun();Session.ResetRuntime();Runner.Flow.LoadLevel(Region.FindMission("L"+mission));
                Runner.StartRun(Runner.Flow.Level.Encounters[encounter].Id,true,true,null,null);rig.Protect();
            }
            Start(1,0);await Until(()=>EnemyHealth.Active.Any(e=>e.CanReceiveDamage&&e.Definition.BodyDamagesPlayer),3500,"Stats spawn");
            var enemy=EnemyHealth.Active.First(e=>e.CanReceiveDamage&&e.Definition.BodyDamagesPlayer);
            var identity=enemy.GetEntityId();int score=enemy.Definition.Score;
            enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);enemy.ReceivePlayerHit(1000,rig.Player);
            Check(Rewards.Score==score&&Rewards.EnemiesDestroyed==1,"Duplicate death score/kill: score="+Rewards.Score+" expected="+score+" kills="+Rewards.EnemiesDestroyed+" hp="+enemy.CurrentHp+" canHit="+enemy.CanReceiveDamage);
            var hud=UnityEngine.Object.FindAnyObjectByType<GameplayHud>();await Task.Delay(100);
            Check(((Text)typeof(GameplayHud).GetField("scoreLabel",Fields).GetValue(hud)).text==score.ToString(),"HUD authority");
            Start(1,0);await Until(()=>EnemyHealth.Active.Any(e=>e.GetEntityId()==identity&&e.CanReceiveDamage),3500,"Pool reuse spawn");
            enemy=EnemyHealth.Active.First(e=>e.GetEntityId()==identity&&e.CanReceiveDamage);
            Check(enemy.GetEntityId()==identity,"Expected pooled reuse");
            enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);Check(Rewards.EnemiesDestroyed==1&&Rewards.Score==score,"Pooled kill stale callback");
            Start(1,1);await Until(()=>Runner.SpawnedCount>0,2500,"Escape spawn");
            enemy=EnemyHealth.Active.First(e=>e.IsAlive);enemy.GetComponent<EntryHoldFlight>().BeginRetreat();Time.timeScale=4;
            await Until(()=>!enemy.IsAlive||!enemy.gameObject.activeInHierarchy,6000,"Escape release");Check(Rewards.EnemiesDestroyed==0&&Rewards.Score==0,"Escape counted as kill");
            var pressure=JsonUtility.FromJson<SpawnAction>(JsonUtility.ToJson(Runner.Current.Groups[0]));pressure.GrantsRewards=false;
            var minion=Runner.SpawnReinforcement(pressure,Runner.Generation);Check(minion!=null,"Pressure spawn");await Until(()=>minion.CanReceiveDamage,3500,"Pressure entry");minion.ReceivePlayerHit(minion.CurrentHp,rig.Player);
            Check(Rewards.EnemiesDestroyed==0&&Rewards.Score==0,"Pressure score/reward");
            Start(2,1);await Until(()=>EnemyHealth.Active.Any(e=>e.CanReceiveDamage&&e.GetComponent<AsteroidVisualMotion>()!=null),4000,"Asteroid spawn");
            enemy=EnemyHealth.Active.First(e=>e.CanReceiveDamage&&e.GetComponent<AsteroidVisualMotion>()!=null);enemy.ReceivePlayerHit(enemy.CurrentHp,rig.Player);
            Check(Rewards.EnemiesDestroyed==0,"Asteroid enemy kill");
            Start(8,1);await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&e.GetComponent<BossPhaseRunner>()!=null),3000,"Warden spawn");
            var boss=EnemyHealth.Active.First(e=>e.IsAlive&&e.GetComponent<BossPhaseRunner>()!=null);
            await Until(()=>boss.GetComponentsInChildren<EnemyHealth>().Any(n=>n!=boss&&n.CanReceiveDamage),3500,"Shield nodes ready");
            var nodes=boss.GetComponentsInChildren<EnemyHealth>().Where(n=>n!=boss&&n.CanReceiveDamage).ToArray();
            foreach(var node in nodes)node.ReceivePlayerHit(node.CurrentHp,rig.Player);
            Check(nodes.Length>0&&nodes.All(n=>n.CurrentHp==0),"Shield nodes not destroyed");
            Check(Rewards.EnemiesDestroyed==0&&Rewards.Score==0,"Shield module scored");
            var timer=Rewards.PlaySeconds;await Task.Delay(200);Check(Rewards.PlaySeconds>timer,"Active timer");Session.Pause();timer=Rewards.PlaySeconds;await Task.Delay(250);Check(Rewards.PlaySeconds==timer,"Pause timer");Session.Resume();
            await Capture("warden-hud-1600");
            return new{status="PASS",duplicateDeath=true,pooledReuse=true,escapeExcluded=true,pressureExcluded=true,asteroidExcluded=true,shieldModuleExcluded=true,hudAuthority=true,timerPause=true};
        }
    }
    static void Seed(UiRig rig,int mission,int encounter=0,int? wallet=null)
    {
        Save.ExitIsolated();var data=Save.PersistentSnapshot;
        data.HasCampaign=true;data.Campaign=new CampaignSave{MissionId="L"+mission,Checkpoint=new CheckpointData{
            EncounterId=Region.FindMission("L"+mission).Encounters[encounter].Id,Lives=3,Missiles=1,WeaponLevels=new[]{1,0,0},Stats=new RunStatsData()}};
        data.Profile.UnlockedMissions=Region.Missions.Select(m=>m.Id).ToArray();
        if(wallet.HasValue)data.Profile.StellarShardBalance=wallet.Value;
        Check(new JsonSaveStore(rig.Path).Write(data,d=>SaveValidation.IsValid(d,(m,e)=>Region.FindMission(m)?.Encounters.Any(x=>x.Id==e)==true,3)),"Seed fixture");
        Save.UseTestStorage(rig.Path);Shell.Boot();
    }
    static async Task<object> TerminalLayouts(int width)
    {
        using(var rig=new UiRig(width))
        {
            Seed(rig,8,1);await Click(View.Continue);rig.Protect();
            await Until(()=>EnemyHealth.Active.Any(e=>e.GetComponent<BossPhaseRunner>()!=null),3000,"Boss HUD");await Capture("boss-hud-"+width);
            rig.Player.GetComponent<PlayerDamageLifecycle>().enabled=true;rig.Player.GetComponent<PlayerLives>().RestoreCheckpoint(1);rig.Gate.SetState(PlayerGameplayState.Active);
            Check(rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null),"GameOver fixture hit");
            await Until(()=>Shell.Screen==ShellScreen.Results&&!Session.IsClear,3000,"GameOver");await Capture("game-over-"+width);
            await Click(View.GameOverRetry);rig.Protect();await ClearCurrent(rig);await Capture("victory-"+width);
            await Click(View.ResultMenu);await Click(View.NewGame);await Capture("confirmation-new-"+width);await Click(View.Cancel);
            await Click(View.Missions);await Click(View.MissionCards[7]);await Click(View.LaunchReplay);rig.Protect();
            rig.Player.GetComponent<PlayerDamageLifecycle>().enabled=true;rig.Player.GetComponent<PlayerLives>().RestoreCheckpoint(1);rig.Gate.SetState(PlayerGameplayState.Active);
            Check(rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null),"Replay fatal hit");await Until(()=>Session.IsTerminal&&!Session.IsClear,3000,"Replay GameOver");
            await Click(View.GameOverRetry);Check(Save.Mode==SessionMode.Replay&&Runner.HasActiveRun,"Replay GameOver Retry");
            return new{status="PASS",width,height=900,victory=true,gameOver=true,replayGameOverRetry=true,confirmation=true};
        }
    }
    static async Task<object> Failures()
    {
        using(var rig=new UiRig(1600))
        {
            Seed(rig,4,1);await Click(View.Continue);rig.Protect();await ClearCurrent(rig);
            var records=Save.PersistentSnapshot.Profile.Records.Length;Directory.CreateDirectory(rig.Path+".tmp");await Click(View.Next);
            var end=Time.realtimeSinceStartup+12;
            while(!Save.HasPendingRegionTransition&&Time.realtimeSinceStartup<end)
            {
                if(Runner.Flow.CanContinue){await rig.Mouse(new Vector2(600,400),true);await rig.Mouse(new Vector2(600,400),false);}
                await Task.Delay(25);
            }
            Check(Save.HasPendingRegionTransition&&Session.IsPaused&&Runner.Flow.Level.Id=="L4"&&Save.PersistentSnapshot.Campaign.MissionId=="L4","Region save failure advanced");
            await Until(()=>Shell.Screen==ShellScreen.Pause,1000,"Region failure Pause");await Capture("region-save-error-1600");
            Directory.Delete(rig.Path+".tmp");await Click(View.Resume);Check(Runner.Flow.Level.Id=="L5"&&Save.PersistentSnapshot.Profile.Records.Length==records,"Region save retry duplicate records");rig.Protect();
            // Background sử dụng clock owner hiện có và đòi fresh press khi quay lại.
            await rig.Mouse(new Vector2(500,400),true);
            typeof(GameplaySession).GetMethod("OnApplicationFocus",Fields).Invoke(Session,new object[]{false});
            Check(Session.IsPaused&&!rig.Input.FireHeld,"Background did not pause/cancel");
            await Until(()=>Shell.Screen==ShellScreen.Pause,1000,"Background UI");await Click(View.Resume);Check(!rig.Input.FireHeld,"Background phantom fire");
            Session.Pause();await Until(()=>Shell.Screen==ShellScreen.Pause,1000,"Pause for menu");await Click(View.PauseMenu);await Click(View.Confirm);
            var bytes=File.ReadAllBytes(rig.Path);File.WriteAllText(rig.Path,"invalid");File.Delete(rig.Path+".bak");Save.UseTestStorage(rig.Path);Shell.Boot();
            Check(!View.NewGame.interactable&&!View.Continue.interactable&&View.Error.activeSelf&&File.ReadAllText(rig.Path)=="invalid","Corrupt boot state");await Capture("invalid-save-1600");
            File.WriteAllBytes(rig.Path,bytes);await Click(View.ErrorRetry);Check(View.Continue.interactable&&!View.Error.activeSelf,"Recover read retry");
            File.Copy(rig.Path,rig.Path+".bak",true);File.WriteAllText(rig.Path,"invalid");Save.UseTestStorage(rig.Path);Shell.Boot();
            Check(Save.LoadStatus==SaveLoadStatus.RecoveredBackup&&View.MenuNotice.text==ShellCopy.Recovered,"Recovery notice");await Capture("backup-recovered-1600");
            File.WriteAllBytes(rig.Path,bytes);Save.UseTestStorage(rig.Path);Shell.Boot();Directory.CreateDirectory(rig.Path+".tmp");
            await Click(View.NewGame);await Click(View.Confirm);Check(View.Error.activeSelf&&Shell.Screen==ShellScreen.Menu&&!Shell.IsBusy,"Start failure stranded");await Capture("start-error-1600");
            Directory.Delete(rig.Path+".tmp");await Click(View.ErrorRetry);await EnterGameplay(rig);
            var feedback=Runner.GetComponent<SettingsFeedback>();var music=(AudioSource)typeof(SettingsFeedback).GetField("music",Fields).GetValue(feedback);var sfx=(AudioSource)typeof(SettingsFeedback).GetField("sfx",Fields).GetValue(feedback);
            Check(music.volume==Save.PersistentSnapshot.Settings.MusicVolume&&sfx.volume==Save.PersistentSnapshot.Settings.SfxVolume,"Audio volumes not applied");
            return new{status="PASS",regionTransactionRetry=true,backgroundPause=true,corruptPreserved=true,recoveryNotice=true,readRetry=true,startFailureRecovery=true,audioChannels=true};
        }
    }
    static async Task<object> SceneRelaunch()
    {
        string path;
        using(var rig=new UiRig(1600)) { Seed(rig,8,1);await Click(View.Continue);rig.Protect();await ClearCurrent(rig);path=rig.Path; }
        var load=UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/StarShift/Scenes/02_Gameplay.unity");
        while(!load.isDone)await Task.Delay(25);await Task.Delay(300);
        using(var rig=new UiRig(1600,path))
        {
            Check(Shell.Screen==ShellScreen.Menu&&View.Continue.interactable,"Recreated scene Menu");var before=File.ReadAllBytes(path);
            await Click(View.Continue);Check(Session.IsCampaignVictory&&!Runner.HasActiveRun&&File.ReadAllBytes(path).SequenceEqual(before),"Scene completed Continue mutated save/spawned");
            await Click(View.ResultMenu);Seed(rig,5,1);
        }
        load=UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/StarShift/Scenes/02_Gameplay.unity");while(!load.isDone)await Task.Delay(25);await Task.Delay(300);
        using(var rig=new UiRig(1600,path))
        {
            await Click(View.Continue);Check(Runner.Flow.Level.Id=="L5"&&Runner.CurrentIndex==1,"Recreated Region B checkpoint");
            return new{status="PASS",sceneRecreations=2,completedContinue=true,regionBCheckpoint=true,productionFilesUnchanged=true};
        }
    }
    static async Task<object> Input()
    {
        using(var rig=new UiRig(1600))
        {
            await Click(View.NewGame);await EnterGameplay(rig);
            var point=new Vector2(600,400);await rig.Touch(31,UnityEngine.InputSystem.TouchPhase.Began,point);Check(rig.Input.FireHeld,"Touch held fire: touches="+UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count+" screen="+Shell.Screen+" gate="+rig.Gate.CanInput+" state="+rig.Gate.State+" scale="+Time.timeScale);
            var hud=UnityEngine.Object.FindAnyObjectByType<GameplayHud>();var pause=(Button)typeof(GameplayHud).GetField("pauseButton",Fields).GetValue(hud);
            var pausePoint=RectTransformUtility.WorldToScreenPoint(null,pause.transform.position);
            await rig.Touch(32,UnityEngine.InputSystem.TouchPhase.Began,pausePoint);await rig.Touch(32,UnityEngine.InputSystem.TouchPhase.Ended,pausePoint);
            await Until(()=>Session.IsPaused&&Shell.Screen==ShellScreen.Pause,1000,"Second touch Pause");Check(!rig.Input.FireHeld,"Pause retained first touch");
            await rig.Touch(31,UnityEngine.InputSystem.TouchPhase.Ended,point);await Click(View.Resume);Check(!rig.Input.FireHeld,"Resume phantom touch");
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Task.Delay(100);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(100);
                Check(Shell.Screen==ShellScreen.Pause,"Escape Pause");
                await Click(View.PauseSettings);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Task.Delay(100);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(100);
                Check(Shell.Screen==ShellScreen.Pause,"Escape Settings origin");
            }
            finally{if(keyboard.added)InputSystem.RemoveDevice(keyboard);}
            await Click(View.Retry);Check(View.Confirmation.activeSelf,"Retry confirmation");await Click(View.Cancel);Check(Session.IsPaused,"Cancel Retry lost Pause");
            await Click(View.Retry);await Click(View.Confirm);Check(Runner.HasActiveRun&&!Session.IsPaused&&!rig.Input.FireHeld,"Pause Retry semantics/input");
            return new{status="PASS",multitouchPause=true,freshTouchAfterResume=true,escapeHierarchy=true,pauseRetry=true};
        }
    }
    static async Task<object> Loading()
    {
        using(var rig=new UiRig(1600))
        {
            Seed(rig,5,1);
            async Task CheckLaunch(Button button,bool background=false)
            {
                bool started=false;float seconds=0,elapsed=0;int samples=0;
                Action observe=()=>{started=true;seconds=Rewards.PlaySeconds;elapsed=Runner.Elapsed;};
                Runner.RunStarted+=observe;
                try
                {
                    var operation=Click(button);
                    while(!operation.IsCompleted)
                    {
                        if(started&&Shell.IsBusy)
                        {
                            Check(View.Loading.activeSelf&&Time.timeScale==0,"Loading released clock early");
                            Check(Rewards.PlaySeconds==seconds&&Runner.Elapsed==elapsed,"Gameplay advanced under loading");
                            samples++;
                            if(background)typeof(GameplaySession).GetMethod("OnApplicationFocus",Fields).Invoke(Session,new object[]{false});
                        }
                        await Task.Delay(10);
                    }
                    await operation;Check(started&&samples>1,"Post-launch loading not observed");
                    Check(!rig.Input.FireHeld,"Loading retained input");
                }
                finally{Runner.RunStarted-=observe;}
            }
            await CheckLaunch(View.Continue);rig.Protect();
            float timer=Rewards.PlaySeconds;await Task.Delay(200);Check(Rewards.PlaySeconds>timer&&!Session.IsPaused,"Continue did not release clock");
            Session.Pause();await Until(()=>Shell.Screen==ShellScreen.Pause,1000,"Pause Retry");await Click(View.Retry);await CheckLaunch(View.Confirm);rig.Protect();
            timer=Rewards.PlaySeconds;await Task.Delay(200);Check(Rewards.PlaySeconds>timer&&!Session.IsPaused,"Retry did not release clock");
            Session.Pause();await Until(()=>Shell.Screen==ShellScreen.Pause,1000,"Background Retry");await Click(View.Retry);await CheckLaunch(View.Confirm,true);
            await Until(()=>Shell.Screen==ShellScreen.Pause,1000,"Loading background pause lost");
            Check(Session.IsPaused&&Time.timeScale==0,"Loading auto-resumed background gameplay");
            await Click(View.Resume);Check(!Session.IsPaused&&!rig.Input.FireHeld,"Background Resume input");
            return new{status="PASS",continueLoadingFrozen=true,retryLoadingFrozen=true,clockResumes=true,backgroundPausePreserved=true,freshInput=true};
        }
    }
    static async Task<object> Recovery()
    {
        using(var rig=new UiRig(1600))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(rig.Path));File.WriteAllText(rig.Path,"broken");Save.UseTestStorage(rig.Path);Shell.Boot();await Task.Delay(200);
            Check(View.Error.activeSelf&&View.RecoverSave.gameObject.activeSelf&&!View.NewGame.interactable,"Recovery entry");await Click(View.ErrorBack);
            await Click(View.RecoverSave);await Capture("recovery-confirm-1600");await Click(View.Cancel);Check(File.ReadAllText(rig.Path)=="broken","Recovery cancel destroyed bytes");
            await Click(View.RecoverSave);await Click(View.Confirm);await EnterGameplay(rig);
            Check(File.ReadAllText(rig.Path+".invalid")=="broken"&&Save.Mode==SessionMode.Campaign,"Recovery launch/archive");
            return new{status="PASS",confirmedRecovery=true,cancelPreservesBytes=true,invalidArchived=true,productionFilesUnchanged=true};
        }
    }
    static async Task<object> Hangar(int width)
    {
        using(var rig=new UiRig(width))
        {
            Seed(rig,1,0,50);await Task.Delay(250);await Capture("menu-ship-"+width);
            var catalog=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/StarShift/Data/Cosmetics/ShipCosmetics.asset");
            var original=JsonUtility.ToJson(Save.PersistentSnapshot.Profile);
            var stage=View.MenuShip.rectTransform.position;
            await Click(View.Hangar);Check(Shell.Screen==ShellScreen.Hangar,"Menu Hangar");
            Check(!View.CosmeticCards[1].gameObject.activeSelf&&!View.CosmeticAction.interactable,"Hangar exposed unowned items");
            Check(View.MenuShip.gameObject.activeInHierarchy&&View.MenuShip.rectTransform.position==stage&&View.MenuShip.GetComponentInParent<Button>()==null,"Stage moved or remains clickable");
            await Capture("workshop-owned-"+width);
            await Click(View.StoreMode);Check(Shell.Screen==ShellScreen.Store,"Workshop Store");
            await Click(View.CosmeticCards[1]);Check(JsonUtility.ToJson(Save.PersistentSnapshot.Profile)==original,"Preview mutated ownership");await Capture("hangar-chassis-"+width);
            await Click(View.CosmeticAction);await Capture("hangar-purchase-"+width);await Click(View.Cancel);Check(Save.PersistentSnapshot.Profile.StellarShardBalance==50,"Canceled purchase spent currency");
            await Click(View.CosmeticAction);await Click(View.Confirm);Check(Save.PersistentSnapshot.Profile.StellarShardBalance==38&&catalog.Equipped(Save.PersistentSnapshot.Profile,CosmeticSlot.Chassis).Id=="chassis-swept","Chassis purchase/equip");
            Check(Save.EquipCosmetic(catalog,"chassis-swept")&&Save.PersistentSnapshot.Profile.StellarShardBalance==38,"Double purchase charge");
            await Click(View.CosmeticTabs[1]);await Click(View.CosmeticCards[1]);await Capture("hangar-skin-"+width);await Click(View.CosmeticAction);await Click(View.Confirm);
            await Click(View.CosmeticTabs[2]);await Click(View.CosmeticCards[1]);await Click(View.CosmeticAction);await Click(View.Confirm);await Capture("hangar-trail-"+width);
            Check(Save.PersistentSnapshot.Profile.StellarShardBalance==24,"Authoritative wallet cost");
            var purchased=JsonUtility.ToJson(Save.PersistentSnapshot.Profile);
            await Click(View.BackButtons[4]);Check(View.MenuShipName.text=="CÁNH QUÉT"&&View.Wallet.text==ShellCopy.Wallet(24),"Menu equipped ship/wallet");await Capture("menu-equipped-"+width);
            Save.UseTestStorage(rig.Path);Shell.Boot();Check(JsonUtility.ToJson(Save.PersistentSnapshot.Profile)==purchased,"Cosmetic reload");
            await Click(View.NewGame);await Click(View.Confirm);Check(((SpriteRenderer)new SerializedObject(UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>()).FindProperty("ship").objectReferenceValue).sprite==catalog.Find("chassis-swept").Sprite,"Intro chassis consumer");
            await Capture("cosmetic-intro-"+width);await EnterGameplay(rig);Check(JsonUtility.ToJson(Save.PersistentSnapshot.Profile)==purchased,"NewGame ownership/equipped loss");
            var hull=rig.Player.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="HullPlaceholder");Check(hull.sprite==catalog.Find("chassis-swept").Sprite&&hull.flipY,"Runtime chassis consumer");
            Time.timeScale=1;var trail=rig.Player.GetComponentsInChildren<TrailRenderer>().First(t=>t.name=="CosmeticEngineTrail");
            await rig.Mouse(new Vector2(600,400),false);await rig.Mouse(new Vector2(600,400),true);await rig.Mouse(new Vector2(700,450),true);
            Check(trail.emitting&&trail.positionCount>1&&((Color32)trail.startColor).Equals((Color32)catalog.Find("trail-ion").Tint),"Runtime trail consumer: emit="+trail.emitting+" count="+trail.positionCount+" color="+trail.startColor+" expected="+catalog.Find("trail-ion").Tint+" gate="+rig.Gate.CanInput+" held="+rig.Input.FireHeld+" pos="+rig.Player.transform.position);await rig.Mouse(new Vector2(700,450),false);
            var stats=rig.Player.GetComponent<PlayerMovementBounds>();Check(stats!=null&&rig.Player.GetComponent<PulseLaserWeapon>().CurrentLevel==1,"Cosmetic changed combat entry");await Capture("cosmetic-gameplay-"+width);
            Session.Pause();await Until(()=>Shell.Screen==ShellScreen.Pause,1000,"Hangar Pause");await Click(View.PauseMenu);await Click(View.Confirm);await Click(View.Hangar);
            Check(!View.CosmeticAction.interactable,"Equipped selection should not offer equip");
            await Click(View.CosmeticCards[0]);Directory.CreateDirectory(rig.Path+".tmp");await Click(View.CosmeticAction);
            Check(View.Error.activeSelf&&catalog.Equipped(Save.PersistentSnapshot.Profile,CosmeticSlot.Chassis).Id=="chassis-swept"&&Save.PersistentSnapshot.Profile.StellarShardBalance==24,"Failed equip rollback");await Capture("hangar-save-error-"+width);
            Directory.Delete(rig.Path+".tmp");await Click(View.ErrorRetry);Check(catalog.Equipped(Save.PersistentSnapshot.Profile,CosmeticSlot.Chassis).Id=="chassis-arrow"&&Save.PersistentSnapshot.Profile.StellarShardBalance==24,"Free equip retry");
            await Click(View.BackButtons[4]);await Click(View.Missions);await Click(View.MissionCards[0]);await Click(View.LaunchReplay);rig.Protect();var before=File.ReadAllBytes(rig.Path);
            Check(!Save.EquipCosmetic(catalog,"skin-cyan")&&Save.Mode==SessionMode.Replay&&File.ReadAllBytes(rig.Path).SequenceEqual(before),"Replay cosmetic mutation");
            Runner.StartRun("ENCOUNTER_1",true,true,null,null);rig.Protect();Check(Save.Mode==SessionMode.DevTest&&!Save.EquipCosmetic(catalog,"skin-cyan")&&File.ReadAllBytes(rig.Path).SequenceEqual(before),"Dev cosmetic mutation");
            return new{status="PASS",width,height=900,backgroundResumes=rig.BackgroundResumes,sharedStage=true,ownedWorkshop=true,separateStore=true,categories=3,options=6,transactionalPurchase=true,persistedEquip=true,newGamePreservation=true,replayDevIsolation=true,saveFailureRollback=true,wallet=24};
        }
    }
    static Task<object> Teardown()
    {
        var root=new GameObject("M7 teardown fixture");root.SetActive(false);
        var presentation=root.AddComponent<MissionIntroPresentation>();
        var gone=new GameObject("Destroyed hierarchy");var renderer=gone.AddComponent<SpriteRenderer>();
        void Set(string name,object value)=>typeof(MissionIntroPresentation).GetField(name,Fields).SetValue(presentation,value);
        Set("introVisible",true);Set("playerVisual",gone);Set("gameplayBackdrop",gone);
        Set("backdropRenderers",new[]{renderer});Set("backdropColors",new[]{Color.white});
        UnityEngine.Object.DestroyImmediate(gone);
        try{presentation.Hide();presentation.Hide();Check(!(bool)typeof(MissionIntroPresentation).GetField("introVisible",Fields).GetValue(presentation),"Teardown state retained");}
        finally{UnityEngine.Object.DestroyImmediate(root);}
        return Task.FromResult<object>(new{status="PASS",destroyedPlayerAndBackdrop=true,idempotentCleanup=true});
    }
    static async Task<object> Layouts(int width)
    {
        using(var rig=new UiRig(width))
        {
            await Task.Delay(350);await Capture("menu-"+width);
            await Click(View.Settings);await Capture("settings-"+width);
            View.Vibration.isOn=false;View.Shake.isOn=false;Check(View.VibrationState.text=="TẮT"&&View.ShakeState.text=="TẮT","Toggle state unreadable");
            await Capture("settings-off-"+width);await Click(View.BackButtons[1]);
            await Click(View.Help);await Capture("help-"+width);await Click(View.BackButtons[2]);
            await Click(View.Missions);await Capture("missions-"+width);await Click(View.BackButtons[0]);
            var transition=Click(View.NewGame);await Until(()=>Shell.IsBusy,1500,"Loading visible");await Task.Delay(110);await Capture("loading-"+width);await transition;
            await EnterGameplay(rig);await Capture("hud-"+width);
            var hud=UnityEngine.Object.FindAnyObjectByType<GameplayHud>();
            var loadout=rig.Player.GetComponent<WeaponLoadout>();
            foreach(var branch in new[]{WeaponBranch.Plasma,WeaponBranch.Homing})loadout.ApplyCore(branch,true);
            var pickup=rig.Player.GetComponent<PlayerPickupReceiver>();
            var shield=AssetDatabase.FindAssets("t:PickupDefinition").Select(id=>AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(id))).First(item=>item.Kind==PickupKind.Shield);
            Check(pickup.TryCollect(shield),"Shield presentation fixture");await Task.Delay(100);
            Text HudLabel(string field)=>(Text)typeof(GameplayHud).GetField(field,Fields).GetValue(hud);
            Check(HudLabel("weaponLabel").text.Contains("Homing · CẤP")&&HudLabel("effectLabel").text.Contains("KHIÊN")&&HudLabel("pickupLabel").text=="KHIÊN"&&HudLabel("missileCount").text.StartsWith("TÊN LỬA"),"HUD retained shorthand");
            await Capture("hud-equipped-"+width);
            Session.Pause();await Until(()=>Shell.Screen==ShellScreen.Pause,1000,"Pause");await Capture("pause-"+width);
            await Click(View.PauseMenu);await Capture("confirmation-"+width);await Click(View.Cancel);await Click(View.Resume);rig.Protect();
            Directory.CreateDirectory(rig.Path+".tmp");await ClearCurrent(rig);
            Check(Save.HasPendingClear&&!View.Next.interactable&&!View.ResultRecord.text.Contains("KỶ LỤC MỚI"),"Save failure false progress");await Capture("clear-save-error-"+width);
            Directory.Delete(rig.Path+".tmp");await Click(View.ResultRetrySave);Check(!Save.HasPendingClear&&View.Next.interactable,"Clear retry save");await Capture("clear-"+width);
            return new{status="PASS",width,height=900,backgroundResumes=rig.BackgroundResumes,saveFailureRecovery=true,screens=9};
        }
    }
}
