using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;
using UnityEditor;

public static class VerifyEncounterFoundation
{
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static EnemyHealth[] Enemies => UnityEngine.Object.FindObjectsByType<EnemyHealth>().Where(e=>e.IsAlive).ToArray();
    static void Check(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
    static async Task Until(Func<bool> test,int ms,string message)
    {
        var end=Time.realtimeSinceStartup+ms/1000f;
        while(!test() && Time.realtimeSinceStartup<end) await Task.Delay(10);
        Check(test(),message);
    }
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,Fields).SetValue(target,value);
    static async Task<string> Capture(string name)
    {
        var path=System.IO.Path.GetFullPath("Temp/StarShiftM3/"+name+".png");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path); await Task.Delay(100); return path;
    }
    public static object Validate()
    {
        var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/StarShift/Data/Fixtures/Levels/M4SystemTestbed.asset");
        Check(level.IsValid(out var error),error);
        Check(StarShift.Editor.EncounterConfigValidation.Errors().Length==0,"Content scan failed.");
        var copy=UnityEngine.Object.Instantiate(level);
        var encounter=UnityEngine.Object.Instantiate(level.Encounters[0]);
        var sequence=UnityEngine.Object.Instantiate(level.Encounters[0].Groups[0].Sequence);
        int rejected=0;
        try
        {
            copy.Encounters=new[] { encounter,encounter };
            Check(!copy.IsValid(out _),"Duplicate encounter accepted."); rejected++;
            copy.Encounters=new[] { encounter };
            copy.AdditionalStarts=new[] {new LevelStartPoint {Id="BOSS",Encounter=level.Encounters[1]}};
            Check(!copy.IsValid(out _),"Fake start accepted."); rejected++;
            copy.AdditionalStarts=new[] {new LevelStartPoint {Id="BOSS",Encounter=encounter}};
            Check(!copy.IsValid(out _),"Boss alias to ordinary encounter accepted.");rejected++;
            copy.AdditionalStarts=Array.Empty<LevelStartPoint>(); copy.Camera=null;
            Check(!copy.IsValid(out _),"Missing camera accepted."); rejected++;
            copy.Camera=level.Camera;
            var titleSeconds=copy.Intro.MissionTitleSeconds;
            var titleWindow=copy.Intro.ApproachSeconds+copy.Intro.TravelSeconds+copy.Intro.BrakeSeconds;
            foreach(var invalid in new[] {0f,float.NaN,float.PositiveInfinity,-1f})
            {
                copy.Intro.MissionTitleSeconds=invalid;
                Check(!copy.IsValid(out _),"Invalid Mission card duration accepted.");rejected++;
            }
            copy.Intro.MissionTitleSeconds=titleWindow+1;
            Check(copy.IsValid(out _),"Gameplay Mission card duration still constrained by cinematic window.");
            copy.Intro.MissionTitleSeconds=titleSeconds;
            encounter.Groups[0].Sequence=sequence;
            sequence.Phases[0].Duration=float.NaN;
            Check(!encounter.IsValid(out _),"NaN phase accepted."); rejected++;
            sequence.Phases[0].Duration=1;
            sequence.Phases[1].HoldUntilResolved=false;
            Check(!encounter.IsValid(out _),"Required finite exit accepted."); rejected++;
            UnityEngine.Object.DestroyImmediate(encounter);
            encounter=UnityEngine.Object.Instantiate(level.Encounters[1]);
            encounter.OptionalEvents=new[] {new OptionalEvent {Id="same"},new OptionalEvent {Id="same"}};
            Check(!encounter.IsValid(out _),"Duplicate optional ID accepted."); rejected++;
            Check(level.ResolveStart("LEVEL_START")==0 && level.ResolveStart("ENCOUNTER_2")==1 && level.ResolveStart("BOSS")==-1,
                "Real start resolution wrong.");
            return new {passed=true,rejected,stableStarts=new[] {"LEVEL_START","ENCOUNTER_1","ENCOUNTER_2"}};
        }
        finally {UnityEngine.Object.DestroyImmediate(copy);UnityEngine.Object.DestroyImmediate(encounter);UnityEngine.Object.DestroyImmediate(sequence);}
    }
    public static async Task<object> Boundaries()
    {
        using(var rig=new Rig())
        {
            int complete=0,events=0;
            Action<EncounterDefinition> done=_=>complete++;
            Action<OptionalEvent,int> optional=(e,token)=>events++;
            Runner.Completed+=done;Runner.OptionalEventTriggered+=optional;
            try
            {
                var eliminate=rig.Definition(EncounterCompletion.Eliminate);
                rig.Start(eliminate);
                await Until(()=>Enemies.Length==1,800,"Eliminate spawn missing.");
                var enemy=Enemies.Single(); enemy.gameObject.SetActive(false);
                await Task.Delay(80);Check(complete==0 && Runner.IsRunning,"Disable counted as kill.");
                enemy.gameObject.SetActive(true);enemy.ReceivePlayerHit(100,null);
                await Until(()=>complete==1,600,"Required death not completed.");
                await Task.Delay(100);Check(complete==1 && !Runner.HasActiveRun,"Double eliminate completion.");

                var timeline=rig.Definition(EncounterCompletion.Timeline); timeline.Duration=.35f;
                timeline.OptionalEvents=new[] {new OptionalEvent {Id="supply-hook",TimeOffset=.06f,Kind=OptionalEventKind.SupplyDrone},
                    new OptionalEvent {Id="bonus",TimeOffset=.08f,Kind=OptionalEventKind.BonusFormation,
                        Group=new SpawnAction {Count=1,Stagger=0,Sequence=rig.HoldSequence,Anchor=new Vector2(.2f,0),Required=false}}};
                rig.Start(timeline);
                await Until(()=>events==2,500,"Optional hooks missing.");
                Check(Runner.SpawnedCount==2 && Runner.CompletionCount==0,"Optional group changed completion.");
                await Until(()=>!Runner.HasActiveRun,1200,"Timeline did not complete independently.");
                Check(events==2 && complete==2 && Enemies.Length==0,"Timeline/optional cleanup wrong.");

                var survive=rig.Definition(EncounterCompletion.Survive);survive.Duration=.22f;
                survive.RewardGraceSeconds=.18f;
                var hostile=new BoundaryProbe(EncounterObjectCategory.Hostile);
                var temp=new BoundaryProbe(EncounterObjectCategory.Temporary);
                var reward=new BoundaryProbe(EncounterObjectCategory.Reward);
                rig.Start(survive);rig.Cleanup.Register(hostile,Runner.Generation);rig.Cleanup.Register(temp,Runner.Generation);rig.Cleanup.Register(reward,Runner.Generation);
                await Until(()=>Runner.Stage==EncounterStage.RewardGrace,700,"Reward grace missing.");
                Check(!hostile.IsActive && !temp.IsActive && reward.IsActive && !reward.HostilityStopped && reward.Grace==.18f && complete==3,
                    "Grace cleared reward or retained hostility.");
                Check(Enemies.Length==0,"Grace retained enemy or auto granted.");
                var elapsed=Runner.Elapsed;
                rig.Session.Pause();await Task.Delay(120);Check(reward.IsActive && Runner.Elapsed==elapsed,"Grace progressed while paused.");
                rig.Session.Resume();
                await Until(()=>!Runner.HasActiveRun,600,"Grace unbounded.");
                Check(!reward.IsActive && reward.ReleaseCount==1,"Reward did not expire once.");

                var boss=rig.Definition(EncounterCompletion.Boss);boss.Groups=Array.Empty<SpawnAction>();boss.BossPostDeathSeconds=.4f;
                rig.Start(boss);var token=Runner.Generation;
                var bossHazard=new BoundaryProbe(EncounterObjectCategory.Hostile);rig.Cleanup.Register(bossHazard,Runner.Generation);
                Check(Runner.NotifyBossDefeated(token) && !Runner.NotifyBossDefeated(token),"Boss defeat not once-only.");
                Check(!bossHazard.IsActive && Runner.Stage==EncounterStage.BossPostDeath && complete==3,"Boss cleanup/post-death boundary wrong.");
                await Task.Delay(80);Check(complete==3,"Boss completed before post-death.");
                Check(Runner.NotifyBossPresentationComplete(token) && !Runner.NotifyBossPresentationComplete(token),"Boss presentation not once-only.");
                Check(complete==4,"Boss completion missing.");
                rig.Start(boss);Check(!rig.Cleanup.Register(new BoundaryProbe(EncounterObjectCategory.Hostile),token),"Old boundary token accepted.");Check(!Runner.NotifyBossDefeated(token),"Old boss token accepted after reset.");
                token=Runner.Generation;Runner.NotifyBossDefeated(token);
                await Until(()=>!Runner.HasActiveRun,900,"Configured boss post-death duration stuck.");
                Check(complete==5,"Timed boss stub double/missing completion.");
                var lateReward=new BoundaryProbe(EncounterObjectCategory.Reward);
                var graceConfig=rig.Definition(EncounterCompletion.Survive);graceConfig.Duration=.08f;graceConfig.RewardGraceSeconds=.12f;
                Action<EncounterDefinition> eject=_=>rig.Cleanup.Register(lateReward,Runner.Generation);
                Runner.Completed+=eject;
                rig.Start(graceConfig);
                await Until(()=>Runner.Stage==EncounterStage.RewardGrace,500,"Completion-ejected reward skipped grace.");
                Check(lateReward.Grace==.12f,"Completion-ejected reward lifetime not extended.");
                Runner.Completed-=eject;
                await Until(()=>!Runner.HasActiveRun,500,"Completion reward grace did not expire.");
                Check(complete==6 && !lateReward.IsActive,"Late reward cleanup wrong.");

                var schedule=rig.Definition(EncounterCompletion.Timeline);schedule.Duration=1;
                schedule.Groups=new[] {Group(.08f,.3f),Group(0,-.3f),Group(.04f,0)};
                rig.Start(schedule);
                await Until(()=>Runner.SpawnedCount==3,500,"Schedule missing actions.");
                await Task.Delay(200);
                var order=Enemies.OrderBy(e=>e.transform.GetSiblingIndex()).Select(e=>Camera.main.WorldToViewportPoint(e.transform.position).x).ToArray();
                Check(order.Length==3 && order[0]<order[1] && order[1]<order[2],"Schedule ignored timestamps for unsorted actions.");
                Runner.StopRun();await Task.Delay(150);Check(complete==6 && Enemies.Length==0,"Reset emitted completion/stale spawn.");
                return new {passed=true,completions=complete,optionalEvents=events,disableIsNotKill=true,allModes=true,
                    bossStaleTokenRejected=true,rewardGraceBounded=true,unsortedSchedule=true,resetCancels=true};
            }
            finally {Runner.Completed-=done;Runner.OptionalEventTriggered-=optional;}
            SpawnAction Group(float start,float x)=>new SpawnAction {Count=1,Stagger=0,StartTime=start,Anchor=new Vector2(x,0),Sequence=rig.HoldSequence};
        }
    }
    public static async Task<object> LegacyRequiredTargets()
    {
        using (var rig = new Rig())
        {
            int completed = 0;
            Action<EncounterDefinition> done = _ => completed++;
            Runner.Completed += done;
            try
            {
                foreach (var mode in new[] { EncounterCompletion.Eliminate, EncounterCompletion.Boss })
                {
                    var definition = rig.Definition(mode);
                    definition.BossPostDeathSeconds = .04f;
                    definition.Groups = new[] { LegacyGroup(true) };
                    Check(definition.IsValid(out var error), error);
                    rig.Start(definition);
                    await Until(() => Enemies.Length == 1 && Runner.Elapsed >= .3f, 1000, "Legacy required target did not settle.");
                    var enemy = Enemies.Single();
                    var flight = enemy.GetComponent<EntryHoldFlight>();
                    Check(flight.IsHolding && !flight.IsExiting && !flight.HasExited && !flight.IsOutsideView &&
                        Runner.CompletionCount == 0, "Legacy required target exited or completed without death: " + mode);
                    enemy.ReceivePlayerHit(enemy.MaxHp, rig.Player);
                    await Until(() => !Runner.HasActiveRun, 700, "Legacy required death did not complete: " + mode);
                    Check(Runner.CompletionCount == 1, "Legacy required completion not once-only.");
                }
                Check(completed == 2, "Required completion signal count wrong.");
                foreach (var mode in new[] { EncounterCompletion.Timeline, EncounterCompletion.Boss })
                {
                    var definition = rig.Definition(mode);
                    definition.Duration = 1;
                    definition.Groups = new[] { LegacyGroup(mode == EncounterCompletion.Timeline) };
                    rig.Start(definition);
                    await Until(() => Runner.SpawnedCount == 1 && Enemies.Length == 0, 1000, "Timed/non-required legacy target did not exit.");
                    Check(Runner.CompletionCount == 0, "Legacy exit incorrectly counted as completion.");
                }
                return new { passed = true, requiredBossAndEliminateHold = true, deathCompletesOnce = true,
                    timelineAndNonRequiredStillExit = true, completions = completed };
            }
            finally { Runner.Completed -= done; }
        }
        SpawnAction LegacyGroup(bool required) => new SpawnAction { Count = 1, Stagger = 0, Required = required,
            Sequence = null, EntryAnchor = new Vector2(.5f, .8f), HoldAnchor = new Vector2(.5f, .7f),
            ExitAnchor = new Vector2(.5f, -1), EntryDuration = .04f, HoldDuration = .06f, ExitDuration = .08f };
    }

    public static async Task<object> DirectionAndCamera(int width)
    {
        using(var rig=new Rig(width))
        {
            var d=rig.Definition(EncounterCompletion.Timeline);d.Duration=5;
            var sequence=rig.NewSequence(new[] {
                new FlightPhase {Kind=FlightPhaseKind.Entry,From=new Vector2(.3f,1.1f),To=new Vector2(.3f,.7f),Duration=.3f,Path=FlightPath.ArcRight,Curve=new Vector2(.05f,.03f)},
                new FlightPhase {Kind=FlightPhaseKind.Hold,From=new Vector2(.3f,.7f),To=new Vector2(.3f,.7f),Duration=.2f,ClampToView=true,CanShoot=true},
                new FlightPhase {Kind=FlightPhaseKind.Cross,From=new Vector2(.3f,.7f),To=new Vector2(.7f,.65f),Duration=.25f,Path=FlightPath.Cross,CanShoot=true},
                new FlightPhase {Kind=FlightPhaseKind.Exit,From=new Vector2(.7f,.65f),To=new Vector2(1.3f,.3f),Duration=.3f,Path=FlightPath.ExitCurve,Curve=new Vector2(.04f,0),CanShoot=true}
            });
            d.Groups[0].Sequence=sequence;
            var phases=new HashSet<int>();var captures=new List<string>();
            foreach(var direction in new[] {PlayDirection.Up,PlayDirection.Right})
            {
                rig.Start(d,direction);
                await Until(()=>Enemies.Length==1,500,"Direction spawn missing.");
                var flight=Enemies.Single().GetComponent<EntryHoldFlight>();
                var deadline=Time.realtimeSinceStartup+1.4f;
                while(flight.gameObject.activeSelf && Time.realtimeSinceStartup<deadline)
                {
                    phases.Add(flight.PhaseIndex);
                    await Task.Delay(15);
                }
                Check(!flight.gameObject.activeSelf && phases.Count==4,"Multi-phase exit did not resolve.");
                Check(rig.Player.GetComponent<PlayerRespawnEntrance>().Direction==direction,"Respawn direction missing.");
                Check(Vector2.Dot(rig.Player.transform.up,direction==PlayDirection.Up?Vector2.up:Vector2.right)>.99f,"Player/muzzle axis wrong.");
                var life=rig.Player.GetComponent<PlayerDamageLifecycle>();
                life.enabled=true;
                Check(life.TryReceiveHit(null),"Direction lifecycle hit rejected.");
                await Until(()=>rig.Player.GetComponent<PlayerGameplayGate>().State==PlayerGameplayState.Respawning,900,"Direction respawn missing.");
                var entrance=Camera.main.WorldToViewportPoint(rig.Player.transform.position);
                Check(direction==PlayDirection.Up ? entrance.y<0 : entrance.x<0,"Respawn entered from wrong edge: "+direction+" "+entrance);
                await Until(()=>rig.Player.GetComponent<PlayerGameplayGate>().State==PlayerGameplayState.PostEntryInvincibility,1600,"Direction safe entrance stuck.");
                var target=Camera.main.WorldToViewportPoint(rig.Player.transform.position);
                Check(direction==PlayDirection.Up ? target.y<.4f : target.x<.4f,"Respawn target wrong side.");
                Runner.StopRun();
            }
            rig.Level.Camera=rig.NewCamera(6.4f,new Rect(0,0,1,1));
            var wide=rig.NewCamera(7.2f,new Rect(0,0,1,.7f));wide.TransitionSeconds=.15f;
            d=rig.Definition(EncounterCompletion.Eliminate);d.CameraOverride=wide;
            var b=rig.Definition(EncounterCompletion.Timeline);b.Id="ENCOUNTER_2";b.Duration=3;
            rig.Level.Encounters=new[] {d,b};
            Runner.StartRun("ENCOUNTER_1",false,true,null,null);
            await Task.Delay(350);
            Check(Mathf.Abs(Camera.main.orthographicSize-7.2f)<.01f && rig.View.CurrentPlayableViewport.height==.7f,"Camera override/region failed.");
            foreach(var e in Enemies)e.ReceivePlayerHit(100,null);
            await Until(()=>Runner.CurrentIndex==1,800,"Camera test handoff missing.");
            await Task.Delay(800);
            Check(Mathf.Abs(Camera.main.orthographicSize-rig.Level.Camera.View.OrthographicSize)<.01f &&
                rig.View.CurrentPlayableViewport==rig.Level.Camera.View.PlayableViewport,"Camera did not return to level baseline.");
            rig.View.RequestView(wide.View,0);rig.View.ReturnToBaseline();await Task.Delay(750);
            Check(Mathf.Abs(Camera.main.orthographicSize-6.4f)<.01f,"ReturnToBaseline did not reach level baseline: "+Camera.main.orthographicSize+" stage="+Runner.Stage);
            return new {passed=true,width,directions=new[] {"Up","Right"},phaseCount=phases.Count,cameraOverride=true,returnToLevel=true,lowerRegion=true,respawnEdges=true};
        }
    }
    public static async Task<object> PathsAndFormations(int width)
    {
        using(var rig=new Rig(width))
        {
            int runs=0;
            foreach(PlayDirection direction in Enum.GetValues(typeof(PlayDirection)))
            foreach(FlightPath path in Enum.GetValues(typeof(FlightPath)))
            {
                var sequence=rig.NewSequence(new[] {new FlightPhase {Kind=FlightPhaseKind.Move,Path=path,
                    From=new Vector2(.35f,.65f),To=new Vector2(.65f,.65f),Curve=new Vector2(.08f,.06f),Duration=.4f,Smooth=false},
                    new FlightPhase {Kind=FlightPhaseKind.Exit,From=new Vector2(.65f,.65f),To=new Vector2(.65f,1.2f),Duration=.2f}});
                var d=rig.Definition(EncounterCompletion.Timeline);d.Groups[0].Sequence=sequence;d.Duration=.9f;
                rig.Start(d,direction);
                await Until(()=>Enemies.Length==1,800,"Path did not spawn: "+path);
                var root=Enemies.Single();var before=root.transform.position;
                await Task.Delay(45);Check(Vector3.Distance(before,root.transform.position)>.001f,"Path failed motion: "+path);
                Check(float.IsFinite(root.transform.position.x),"Nonfinite path pose.");
                await Until(()=>!Runner.HasActiveRun,1800,"Path timeout/exit stuck: "+path);
                runs++;
            }
            int formations=0;
            foreach(FormationLayout layout in Enum.GetValues(typeof(FormationLayout)))
            foreach(PlayDirection direction in Enum.GetValues(typeof(PlayDirection)))
            {
                var d=rig.Definition(EncounterCompletion.Timeline);d.Duration=.3f;
                d.Groups[0].Count=7;d.Groups[0].Formation=layout;d.Groups[0].SlotStep=new Vector2(.055f,.055f);d.Groups[0].FormationRise=.04f;
                rig.Start(d,direction);
                await Until(()=>Enemies.Length==7,400,"Formation count wrong: "+layout);
                await Task.Delay(80);
                var positions=Enemies.Select(e=>(Vector2)Camera.main.WorldToViewportPoint(e.transform.position)).ToArray();
                Check(positions.All(p=>p.x>0 && p.x<1 && p.y>0 && p.y<1),"Formation outside view: "+layout);
                for(int i=0;i<positions.Length;i++)for(int j=i+1;j<positions.Length;j++)
                    Check(Vector2.Distance(positions[i],positions[j])>.01f,"Formation duplicate slot: "+layout);
                await Until(()=>!Runner.HasActiveRun,700,"Formation cleanup failed.");formations++;
            }
            return new {passed=true,width,pathDirectionRuns=runs,formationDirectionRuns=formations};
        }
    }
    public static async Task<object> CompositionAndDebug(int width)
    {
        using(var rig=new Rig(width))
        {
            var d=rig.Definition(EncounterCompletion.Timeline);d.Duration=2;
            var left=rig.NewSequence(new[] {
                new FlightPhase {Kind=FlightPhaseKind.Entry,Path=FlightPath.Pincer,From=new Vector2(-.2f,.7f),To=new Vector2(.3f,.7f),Curve=new Vector2(.06f,.04f),Duration=.35f},
                new FlightPhase {Kind=FlightPhaseKind.Cross,Path=FlightPath.Cross,From=new Vector2(.3f,.7f),To=new Vector2(.8f,.42f),Duration=.7f},
                new FlightPhase {Kind=FlightPhaseKind.Exit,Path=FlightPath.ExitCurve,From=new Vector2(.8f,.42f),To=new Vector2(1.4f,.35f),Duration=.3f}
            });
            var right=rig.NewSequence(new[] {
                new FlightPhase {Kind=FlightPhaseKind.Entry,Path=FlightPath.Pincer,From=new Vector2(1.2f,.8f),To=new Vector2(.7f,.8f),Curve=new Vector2(-.06f,.04f),Duration=.35f},
                new FlightPhase {Kind=FlightPhaseKind.Cross,Path=FlightPath.Cross,From=new Vector2(.7f,.8f),To=new Vector2(.2f,1),Duration=.7f},
                new FlightPhase {Kind=FlightPhaseKind.Exit,Path=FlightPath.ExitCurve,From=new Vector2(.2f,1),To=new Vector2(-.4f,1.1f),Duration=.3f}
            });
            left.name="DebugLeftSequence";right.name="DebugRightSequence";
            d.Groups=new[] {new SpawnAction {Count=3,Stagger=0,SlotStep=new Vector2(.12f,0),Sequence=left},
                new SpawnAction {Count=3,Stagger=0,SlotStep=new Vector2(.12f,0),Sequence=right}};
            var debug=Runner.GetComponent<StarShift.Dev.EncounterDebugView>();var json=EditorJsonUtility.ToJson(debug);
            try
            {
                var fields=new[] {"showBounds","showPaths","showSpawnPoints","showHitboxes","showFps","showContentLabels"};
                Check(fields.All(f=>!(bool)debug.GetType().GetField(f,Fields).GetValue(debug)),"Debug not default OFF.");
                foreach(var f in fields)Set(debug,f,true);
                var captures=new List<string>();
                foreach(var direction in new[] {PlayDirection.Up,PlayDirection.Right})
                {
                    rig.Start(d,direction);await Task.Delay(420);
                    Check(Enemies.Length==6,"Pincer groups missing.");
                    Check(new[] {"Region / Planet:","Mission 01:","Encounter 01:","Spawn Group 01:","Spawn Group 02:","Formation:","Flight Sequence / Path:",left.name,right.name}.All(label=>debug.ContentLabels.Contains(label)),"Debug content taxonomy/IDs missing.");
                    captures.Add(await Capture("composition-"+width+"-"+direction));
                    var until=Time.realtimeSinceStartup+.55f;
                    while(Time.realtimeSinceStartup<until)
                    {
                        var boxes=Enemies.Select(e=>e.GetComponentInChildren<BoxCollider2D>().bounds).ToArray();
                        for(int i=0;i<boxes.Length;i++)for(int j=i+1;j<boxes.Length;j++)
                            Check(!boxes[i].Intersects(boxes[j]),"Cross/pincer collider lanes overlap: "+direction);
                        await Task.Delay(15);
                    }
                    await Until(()=>!Runner.HasActiveRun,1800,"Composition cleanup stuck.");
                }
                return new {passed=true,width,crossPincerGroups=true,noColliderOverlap=true,debugToggles=true,captures};
            }
            finally {EditorJsonUtility.FromJsonOverwrite(json,debug);}
        }
    }

    public static async Task<object> StartHooksAndReentrancy()
    {
        using(var rig=new Rig())
        {
            var a=rig.Definition(EncounterCompletion.Eliminate);
            var b=rig.Definition(EncounterCompletion.Timeline);b.Id="ENCOUNTER_2";b.Duration=.25f;
            rig.Level.Encounters=new[] {a,b};
            int starts=0;
            Action<EncounterDefinition,int> began=(definition,token)=>
            {
                Check(token==Runner.Generation && Runner.Stage==EncounterStage.Running && Runner.Elapsed==0 && Runner.SpawnedCount==0,
                    "Encounter-start hook was after schedule or had stale token.");starts++;
            };
            Runner.EncounterStarted+=began;
            try
            {
                Runner.StartRun("ENCOUNTER_1",false,true,null,null);
                await Until(()=>Enemies.Length==1,500,"Start hook A missing.");Enemies.Single().ReceivePlayerHit(100,null);
                await Until(()=>rig.Session.IsClear,1000,"Start hook flow did not clear.");
                Check(starts==2,"Encounter start did not signal once per encounter.");
            }
            finally {Runner.EncounterStarted-=began;}
            rig.Session.enabled=false;Time.timeScale=1;rig.Session.enabled=true;
            rig.Player.GetComponent<PlayerGameplayGate>().SetState(PlayerGameplayState.PostEntryInvincibility);
            Action<EncounterDefinition,int> stopAtStart=(_,__)=>Runner.StopRun();
            Runner.EncounterStarted+=stopAtStart;
            Runner.StartRun("ENCOUNTER_1",true,true,null,null);Runner.EncounterStarted-=stopAtStart;
            await Task.Delay(100);Check(!Runner.HasActiveRun && Enemies.Length==0,"Reset in encounter-start callback restarted schedule.");
            Action stopAtRun=()=>Runner.StopRun();Runner.RunStarted+=stopAtRun;
            Runner.StartRun("LEVEL_START",false);Runner.RunStarted-=stopAtRun;
            Check(!Runner.HasActiveRun && !Runner.Flow.IsIntroducing,"Reset in run-start callback restarted intro.");
            Action<string,string,float> stopAtTitle=(_,__,___)=>Runner.StopRun();Runner.Flow.TitleRequested+=stopAtTitle;
            Runner.StartRun("LEVEL_START",false);
            await Until(()=>!Runner.HasActiveRun,3500,"Title callback reset resurrected encounter.");
            Runner.StartRun("ENCOUNTER_1",true,true,null,null);
            Check(!Runner.HasActiveRun,"Reset in skipped-intro title callback spawned a group.");
            Runner.Flow.TitleRequested-=stopAtTitle;
            await Task.Delay(100);Check(Enemies.Length==0 && !Runner.HasActiveRun,"Stale intro completion spawned after reset.");
            return new {passed=true,encounterStartSignals=starts,hookBeforeSpawn=true,runStartReset=true,encounterStartReset=true,titleReset=true};
        }
    }

    public static async Task<object> FiringLimits()
    {
        using(var rig=new Rig())
        {
            var sequence=rig.NewSequence(new[] {
                new FlightPhase {Kind=FlightPhaseKind.Entry,From=new Vector2(.5f,.85f),To=new Vector2(.5f,.75f),Duration=.08f},
                new FlightPhase {Kind=FlightPhaseKind.Combat,From=new Vector2(.5f,.75f),To=new Vector2(.5f,.75f),Duration=1,HoldUntilResolved=true,ClampToView=true,CanShoot=true}
            });
            var d=rig.Definition(EncounterCompletion.Timeline);d.Duration=3;
            d.Groups[0].Count=6;d.Groups[0].Sequence=sequence;d.Groups[0].SlotStep=new Vector2(.12f,0);
            d.ActiveShooterLimit=2;d.ShotSpacing=.05f;d.ShotsPerVolley=4;d.VolleyPause=.18f;
            rig.Start(d);
            int maximum=0;bool shotSeen=false;
            var end=Time.realtimeSinceStartup+2.3f;
            while(Time.realtimeSinceStartup<end)
            {
                var current=Enemies.Count(e=>e.GetComponent<SingleFireAttack>().IsTelegraphing);
                maximum=Mathf.Max(maximum,current);Check(current<=2,"Configured shooter limit exceeded.");
                shotSeen|=UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length>0;
                await Task.Delay(10);
            }
            Check(maximum==2 && shotSeen,"Configured two-shooter policy did not execute.");
            Runner.StopRun();await Task.Delay(100);
            Check(!UnityEngine.Object.FindObjectsByType<SingleFireAttack>().Any(a=>a.IsTelegraphing) &&
                UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length==0,"Pending configured attacks survived reset.");
            return new {passed=true,configuredLimit=2,maximumTelegraphs=maximum,shotSeen,cancelOnReset=true};
        }
    }

    public static async Task<object> PresentationBoundaries()
    {
        using(var rig=new Rig())
        {
            var flow=Runner.Flow;
            var intro=rig.Level.Intro;intro.ContinueDelaySeconds=.04f;
            intro.ApproachSeconds=intro.TravelSeconds=intro.BrakeSeconds=.12f;
            intro.MissionTitleSeconds=.24f;
            rig.Level.Encounters=new[] {rig.Definition(EncounterCompletion.Eliminate)};
            var stages=new[] {MissionIntroPhase.Briefing,MissionIntroPhase.Travel,MissionIntroPhase.Brake,MissionIntroPhase.MissionTitle};
            foreach(var stage in stages)
            {
                Runner.StartRun("LEVEL_START",true);
                if(stage!=MissionIntroPhase.Briefing)
                {
                    await Until(()=>flow.CanContinue,500,"Reset fixture never armed.");
                    Check(flow.BeginApproach(),"Reset fixture could not launch.");
                    await Until(()=>flow.IntroPhase==stage,1800,"Reset fixture phase missing: "+stage);
                }
                var cinematic=stage!=MissionIntroPhase.MissionTitle;
                Check(rig.Player.transform.Find("Visual").gameObject.activeSelf==!cinematic && (GameObject.Find("EnvironmentPlaceholders")!=null)==!cinematic,
                    "Cinematic/gameplay title did not restore the correct visuals.");
                if(!cinematic)
                {
                    bool rejected=false;
                    try {flow.LoadLevel(rig.Level);}catch(InvalidOperationException){rejected=true;}
                    Check(rejected,"Level load replaced an active Mission card without cancellation.");
                }
                Runner.StopRun();
                rig.Session.BeginMission();await Task.Delay(180);
                Check(!Runner.HasActiveRun && !flow.IsCinematic && !flow.BeginApproach() && Enemies.Length==0,"Reset/continue resurrected intro.");
                var announcement=UnityEngine.Object.FindAnyObjectByType<StarShift.UI.GameplayAnnouncement>();
                var hint=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include).Single(t=>t.name=="ContinueHint");
                Check(!hint.gameObject.activeSelf && !announcement.IsPersistent && announcement.GetComponent<CanvasGroup>().alpha==0,
                    "Reset retained establishing UI/title.");
                Check(rig.Player.transform.Find("Visual").gameObject.activeSelf && GameObject.Find("EnvironmentPlaceholders")!=null,"Reset failed to restore visuals.");
            }
            Action<string,string,float> stopAtCard=(_,__,___)=>{if(flow.IntroPhase==MissionIntroPhase.MissionTitle)Runner.StopRun();};
            flow.TitleRequested+=stopAtCard;
            try
            {
                Runner.StartRun("LEVEL_START",true);
                await Until(()=>flow.CanContinue,500,"Title reset fixture never armed.");flow.BeginApproach();
                await Until(()=>!Runner.HasActiveRun,1800,"Reset in gameplay title callback resurrected first encounter.");
                Check(!flow.IsIntroducing && Enemies.Length==0,"Gameplay title reset retained presentation/spawn.");
            }
            finally {flow.TitleRequested-=stopAtCard;}
            var a=rig.Definition(EncounterCompletion.Eliminate);a.HandoffDelay=1.5f;
            var b=rig.Definition(EncounterCompletion.Timeline);b.Id="ENCOUNTER_2";b.Duration=5;
            rig.Level.Encounters=new[] {a,b};
            var life=rig.Player.GetComponent<PlayerDamageLifecycle>();life.enabled=true;
            Runner.StartRun("ENCOUNTER_1",false,true,null,null);
            await Until(()=>Enemies.Length==1,500,"Lifecycle/handoff fixture missing.");
            Check(life.TryReceiveHit(null),"Handoff lifecycle damage rejected.");
            Enemies.Single().ReceivePlayerHit(100,null);
            await Until(()=>Runner.Stage==EncounterStage.Handoff,500,"Handoff did not start during death.");
            rig.Session.Pause();Check(rig.Session.IsPaused && Time.timeScale==0,"Minor handoff rejected Pause.");
            await Task.Delay(120);rig.Session.Resume();
            await Until(()=>rig.Player.GetComponent<PlayerGameplayGate>().State==PlayerGameplayState.Respawning,900,"Handoff overwrote Dying timer/state.");
            await Until(()=>rig.Player.GetComponent<PlayerGameplayGate>().State==PlayerGameplayState.PostEntryInvincibility,1800,"Handoff lost respawn/protection.");
            Check(rig.Player.GetComponent<PlayerGameplayGate>().CanInput,"Minor handoff retained cinematic lock.");
            await Until(()=>Runner.CurrentIndex==1,900,"Handoff did not release into B.");
            Check(rig.Player.GetComponent<PlayerLives>().CurrentLives==2 && rig.Player.GetComponent<PlayerGameplayGate>().CanInput,
                "Handoff reset lives or retained cinematic lock.");
            return new {passed=true,resetPhases=stages.Select(s=>s.ToString()).ToArray(),staleContinueRejected=true,visualRestore=true,
                deathRespawnContinues=true,handoffPauseAllowed=true,lifePreserved=true};
        }
    }

    public static async Task<object> DevAndReset()
    {
        using(var rig=new Rig())
        {
            var dev=Runner.GetComponent<StarShift.Dev.DevFastStart>();var json=EditorJsonUtility.ToJson(dev);
            try
            {
                rig.Level.Encounters=new[] {rig.Definition(EncounterCompletion.Eliminate),rig.Definition(EncounterCompletion.Timeline)};
                rig.Level.Encounters[1].Id="ENCOUNTER_2";
                Set(dev,"devModeEnabled",true);Set(dev,"standalone",true);Set(dev,"startPoint","ENCOUNTER_2");Set(dev,"skipIntro",true);
                Set(dev,"overrideDirection",true);Set(dev,"direction",PlayDirection.Right);
                var view=rig.NewCamera(7.2f,new Rect(0,0,1,1));Set(dev,"cameraOverride",view);
                dev.Replay();await Until(()=>Runner.SpawnedCount==1,500,"Dev jump did not spawn.");
                Check(Runner.CurrentIndex==1 && Runner.Flow.Direction==PlayDirection.Right && Camera.main.orthographicSize==7.2f,"Dev overrides failed.");
                Check(UnityEngine.Object.FindAnyObjectByType<StarShift.UI.GameplayAnnouncement>().transform.Find("Description").GetComponent<UnityEngine.UI.Text>().text=="Encounter 02",
                    "Dev encounter jump displayed wrong Mission subtitle.");
                rig.Session.Pause();dev.Replay();
                Check(!rig.Session.IsPaused && Time.timeScale==1,"Dev replay retained pause.");
                Set(dev,"startPoint","BOSS");bool rejected=false;
                try {dev.Replay();}catch(ArgumentException){rejected=true;}
                Check(rejected && !Runner.HasActiveRun,"Unavailable start did not fail clearly.");
                Set(dev,"startPoint","LEVEL_START");Set(dev,"skipIntro",false);dev.Replay();
                Check(Runner.Stage==EncounterStage.Intro,"Dev mission intro unavailable.");
                Runner.StopRun();await Task.Delay(150);Check(Enemies.Length==0 && !Runner.HasActiveRun,"Reset left intro callbacks.");
                return new {passed=true,encounterJump=true,introJump=true,directionOverride=true,cameraOverride=true,pauseReplay=true,fakeBossRejected=true};
            }
            finally {EditorJsonUtility.FromJsonOverwrite(json,dev);}
        }
    }
    sealed class BoundaryProbe : IEncounterBoundaryObject
    {
        public EncounterObjectCategory Category {get;}
        public bool IsActive {get;private set;}=true;
        public float Grace;public int ReleaseCount;public bool HostilityStopped;
        public BoundaryProbe(EncounterObjectCategory category)=>Category=category;
        public void StopHostility() {HostilityStopped=true;}
        public void PreserveForGrace(float seconds)=>Grace=seconds;
        public void Release(){if(!IsActive)return;IsActive=false;ReleaseCount++;}
    }
    sealed class Rig : IDisposable
    {
        public readonly LevelDefinition Level;
        public readonly GameObject Player=GameObject.Find("Player");
        public readonly GameplaySession Session=UnityEngine.Object.FindAnyObjectByType<GameplaySession>();
        public readonly EncounterCleanup Cleanup=Runner.GetComponent<EncounterCleanup>();
        public readonly GameplayViewController View=Camera.main.GetComponent<GameplayViewController>();
        public readonly FlightSequence HoldSequence;
        readonly LevelDefinition original=Runner.Flow.Level;
        readonly GameplayViewSettings oldView;
        readonly float scale=Time.timeScale;
        readonly bool audio=AudioListener.pause;
        readonly object gameView;
        readonly object gameViewZoom;
        readonly Vector2 oldGameViewZoom, oldGameViewPan;readonly PropertyInfo selected;readonly object oldSize;
        readonly List<UnityEngine.Object> objects=new List<UnityEngine.Object>();
        readonly string originalJson;
        public Rig(int width=1600)
        {
            Check(Application.isPlaying,"Play Mode required.");
            Session.enabled=false;Runner.StopRun();Time.timeScale=1;AudioListener.pause=false;
            Level=UnityEngine.Object.Instantiate(original);objects.Add(Level);originalJson=EditorJsonUtility.ToJson(original);
            Runner.Flow.LoadLevel(Level);oldView=View.TargetView;
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");gameView=EditorWindow.GetWindow(type);
            selected=type.GetProperty("selectedSizeIndex");oldSize=selected.GetValue(gameView);
            gameViewZoom = gameView.GetType().GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameView);
            oldGameViewZoom = (Vector2)gameViewZoom.GetType().GetProperty("scale").GetValue(gameViewZoom);
            oldGameViewPan = (Vector2)gameViewZoom.GetType().GetProperty("translation").GetValue(gameViewZoom);
            type.GetMethod("SetCustomResolution",Fields).Invoke(gameView,new object[]{new Vector2(width,900),"StarShift M3 QA " + width + "x900"});
            ResetPlayer();Session.enabled=true;
            HoldSequence=NewSequence(new[] {new FlightPhase {Kind=FlightPhaseKind.Entry,From=new Vector2(.5f,.8f),To=new Vector2(.5f,.7f),Duration=.04f},
                new FlightPhase {Kind=FlightPhaseKind.Hold,From=new Vector2(.5f,.7f),To=new Vector2(.5f,.7f),Duration=1,HoldUntilResolved=true,ClampToView=true}});
        }
        void ResetPlayer()
        {
            var life=Player.GetComponent<PlayerDamageLifecycle>();life.enabled=false;life.enabled=true;life.enabled=false;
            Player.GetComponent<PlayerGameplayGate>().SetState(PlayerGameplayState.PostEntryInvincibility);
            UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
        }
        public EncounterDefinition Definition(EncounterCompletion mode)
        {
            var d=UnityEngine.Object.Instantiate(original.Encounters[0]);objects.Add(d);d.Id="ENCOUNTER_1";d.Completion=mode;
            d.Duration=.5f;d.HandoffDelay=.05f;d.Groups=new[] {new SpawnAction {Count=1,Stagger=0,Sequence=HoldSequence}};
            d.OptionalEvents=Array.Empty<OptionalEvent>();return d;
        }
        public FlightSequence NewSequence(FlightPhase[] phases)
        {var s=ScriptableObject.CreateInstance<FlightSequence>();objects.Add(s);s.Phases=phases;return s;}
        public CameraProfile NewCamera(float size,Rect region)
        {var p=ScriptableObject.CreateInstance<CameraProfile>();p.View=new GameplayViewSettings(size,region);objects.Add(p);return p;}
        public void Start(EncounterDefinition definition,PlayDirection direction=PlayDirection.Up)
        {
            Runner.StopRun();ResetPlayer();Level.Encounters=new[] {definition};Runner.StartRun(definition.Id,true,true,direction,null);
        }
        public void Dispose()
        {
            Session.enabled=false;Runner.StopRun();Time.timeScale=1;
            Runner.Flow.LoadLevel(original);Player.transform.rotation=Quaternion.identity;
            Player.GetComponent<PlayerRespawnEntrance>().Direction=PlayDirection.Up;
            var life=Player.GetComponent<PlayerDamageLifecycle>();life.enabled=false;life.enabled=true;
            View.SetBaseline(original.Camera.View);View.RequestView(oldView,0);gameView.GetType().GetMethod("SizeSelectionCallback").Invoke(gameView, new object[] { oldSize, null });
            gameViewZoom.GetType().GetMethod("SetTransform").Invoke(gameViewZoom, new object[] { oldGameViewPan, oldGameViewZoom });
            foreach(var o in objects)UnityEngine.Object.Destroy(o);
            Session.enabled=true;Time.timeScale=scale;AudioListener.pause=audio;
            Check(EditorJsonUtility.ToJson(original)==originalJson,"Probe mutated authored level config.");
        }
    }
}
