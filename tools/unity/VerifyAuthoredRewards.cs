using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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

public static class VerifyAuthoredRewards
{
    const string Output="../.design/content-rewards/";
    static Task current;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Evidence(string name,object data){Directory.CreateDirectory(Output);File.WriteAllText(Output+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));}
    static async Task Until(Func<bool> done,int ms,string message)
    {float end=Time.realtimeSinceStartup+ms/1000f;while(!done()&&Time.realtimeSinceStartup<end)await Task.Delay(10);Check(done(),message);}
    public static object Pure()
    {
        int cases=0;
        var legacyOrdinals=new List<int>();
        foreach(int seed in new[]{3,17,41,67,91,410})
        {
            var a=new EncounterRewardQuota();var b=new EncounterRewardQuota();a.Reset(seed,12,1);b.Reset(seed,12,1,0);
            int drops=0;
            for(int i=1;i<=12;i++){bool hit=a.ResolveKill(false);Check(hit==b.ResolveKill(false),"Legacy reset compatibility");if(hit){drops++;legacyOrdinals.Add(i);}}
            Check(drops==1&&a.Remaining==0,"Legacy quota changed");
        }
        Check(legacyOrdinals.Distinct().Count()>1,"Unscheduled quota became fixed");
        for(int seed=0;seed<100;seed++)foreach(int deadline in new[]{1,2,4,8})
        {
            var a=new EncounterRewardQuota();var b=new EncounterRewardQuota();a.Reset(seed,12,1,deadline);b.Reset(seed,12,1,deadline);
            int drops=0,ordinal=0;
            for(int i=1;i<=12;i++){bool hit=a.ResolveKill(false);Check(hit==b.ResolveKill(false),"Deadline determinism");if(hit){drops++;ordinal=i;}}
            Check(drops==1&&ordinal<=deadline&&a.Remaining==0,"Missed deadline "+deadline);cases++;
            a.Reset(seed,12,1,deadline);Check(!a.ResolveKill(true),"Matching natural item duplicated");
            for(int i=1;i<12;i++)Check(!a.ResolveKill(false),"Floor repeated after credit");
        }
        var quota=new EncounterRewardQuota();quota.Reset(1,5,2,2);quota.ResolveEscape();
        int count=0;for(int i=0;i<4;i++)if(quota.ResolveKill(false))count++;
        Check(count==2,"Escape consumed floor");
        foreach(var bad in new[]{-1,6}){bool threw=false;try{quota.Reset(1,5,1,bad);}catch(ArgumentOutOfRangeException){threw=true;}Check(threw,"Invalid deadline accepted");}
        bool invalid=false;try{quota.Reset(1,5,0,1);}catch(ArgumentOutOfRangeException){invalid=true;}Check(invalid,"Deadline without floor accepted");
        quota.Reset(1,5,1,4);Check(quota.ResolveFallback()&&!quota.ResolveFallback(),"Completion fallback duplicated");
        var errors=StarShift.Editor.EncounterConfigValidation.Errors();Check(errors.Length==0,string.Join("\n",errors));
        var missions=UnityEngine.Object.FindAnyObjectByType<RegionCampaignFlow>().Missions;
        var authored=missions.SelectMany(m=>m.Encounters).Where(e=>e.GuaranteedMajorCount>0).ToArray();
        Check(authored.Length==15&&authored.All(e=>e.GuaranteedMajorCount==1&&e.GuaranteedMajorByKill>0&&e.GuaranteeTable.GuaranteedPickup!=null),"Incomplete authored reward plan");
        Check(authored.Select(e=>e.GuaranteeTable.GuaranteedPickup).Distinct().Count()==8,"Missing item family");
        var result=new{status="PASS",deadlineCases=cases,authoredEncounters=authored.Length,itemFamilies=8,validation=errors};Evidence("pure",result);return result;
    }
    public static object Begin()
    {
        Check(Application.isPlaying,"Play Mode required");Check(current==null||current.IsCompleted,"Probe busy");
        Evidence("runtime",new{status="RUNNING"});current=Run();return new{started=true};
    }
    static async Task Run()
    {
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();var region=runner.GetComponent<RegionCampaignFlow>();
        var save=runner.GetComponent<CampaignLifecycle>();var shell=runner.GetComponent<GameShell>();
        var director=runner.GetComponent<RewardDirector>();var player=UnityEngine.Object.FindAnyObjectByType<PlayerGameplayGate>();
        var loadout=player.GetComponent<WeaponLoadout>();var damage=player.GetComponent<PlayerDamageLifecycle>();
        var view=UnityEngine.Object.FindAnyObjectByType<ShellView>();var originalLevel=runner.Flow.Level;
        string original=null,temp=null;byte[][] before=null;var receipts=new List<object>();
        try
        {
            Check(!runner.HasActiveRun&&!shell.IsBusy,"Idle Menu required");
            original=save.SavePath;before=new[]{"",".bak",".invalid"}.Select(s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null).ToArray();
            temp=Path.Combine(Path.GetTempPath(),"starshift-rewards-"+Guid.NewGuid().ToString("N"),"save.json");save.UseTestStorage(temp);shell.Boot();
            foreach(var mission in region.Missions)foreach(var encounter in mission.Encounters.Where(e=>e.GuaranteedMajorCount>0))
            {
                int runs=mission.Id=="L1"&&encounter==mission.Encounters[0]?2:1;var ordinals=new List<int>();
                for(int run=0;run<runs;run++)
                {
                    region.Cancel();runner.StopRun();runner.Flow.LoadLevel(mission);
                    runner.StartRun(encounter.Id,true,true,null,null);loadout.Restore(StarShift.Combat.WeaponBranch.Pulse,new[]{1,0,0});
                    damage.RestoreCheckpoint(3);damage.GrantBriefProtection(1000);player.SetCinematicLock(false);
                    view.Show(ShellScreen.Playing);Time.timeScale=3;
                    await Until(()=>runner.Stage==EncounterStage.Running,2000,"Encounter start "+encounter.name);
                    int ordinal=0,preferredAt=0;bool capOverflow=false;var preferred=encounter.GuaranteeTable.GuaranteedPickup;
                    Action<PickupDefinition,RewardDropTable> observe=(item,table)=>{if(item==preferred&&preferredAt==0)preferredAt=ordinal;if(item==preferred&&director.ActivePickups>6)capOverflow=true;};
                    director.DropSpawned+=observe;
                    try
                    {
                        if(mission.Id=="L1"&&encounter==mission.Encounters[0])
                        {
                            var shard=AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/StellarShard.asset");
                            for(int i=0;i<6;i++)director.Spawn(shard,null,Camera.main.ViewportToWorldPoint(new Vector3(.05f,.85f,-Camera.main.transform.position.z)));
                        }
                        while(preferredAt==0)
                        {
                            await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&e.CanReceiveDamage&&e.Definition.BodyDamagesPlayer&&e.GetComponent<AsteroidVisualMotion>()==null),5000,"Eligible target "+encounter.name);
                            var enemy=EnemyHealth.Active.First(e=>e.IsAlive&&e.CanReceiveDamage&&e.Definition.BodyDamagesPlayer&&e.GetComponent<AsteroidVisualMotion>()==null);
                            ordinal++;enemy.ReceivePlayerHit(enemy.CurrentHp,player.gameObject);
                            Check(ordinal<=encounter.GuaranteedMajorByKill,"Missed authored item deadline "+encounter.name);
                        }
                        Check(director.GuaranteedMajorsRemaining==0,"Floor not credited");
                        var pickup=UnityEngine.Object.FindObjectsByType<RewardPickup>().First(p=>p.IsActive&&p.Item==preferred);
                        Check(pickup.TryCollect(),"Authored item unusable "+preferred.name);
                        if(mission.Id=="L1"&&encounter==mission.Encounters[0])Check(capOverflow,"Guaranteed pickup lost at normal cap");
                        ordinals.Add(preferredAt);
                        receipts.Add(new{mission=mission.Id,encounter=encounter.Id,item=preferred.name,ordinal=preferredAt,deadline=encounter.GuaranteedMajorByKill,collected=true,capOverflow});
                    }
                    finally{director.DropSpawned-=observe;}
                }
                if(runs==2)Check(ordinals[0]==ordinals[1],"Restart changed authored ordinal");
            }
            // Maxed inventory vẫn trả một reward dùng được, không rơi Core/ammo/life bị khóa.
            loadout.Restore(StarShift.Combat.WeaponBranch.Pulse,new[]{5,5,5});player.GetComponent<PlayerLives>().RestoreCheckpoint(player.GetComponent<PlayerLives>().MaximumLives);
            var reserve=player.GetComponent<EmergencyMissileReserve>();reserve.RestoreCheckpoint(3);
            var receiver=player.GetComponent<PlayerPickupReceiver>();var roller=new RewardRoller();roller.Reset(410);
            foreach(var path in AssetDatabase.FindAssets("t:RewardDropTable",new[]{"Assets/StarShift/Data/Rewards/Authored"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var table=AssetDatabase.LoadAssetAtPath<RewardDropTable>(path);
                var item=roller.SelectMajor(table,receiver,player.GetComponent<PlayerLives>(),loadout,false);
                Check(item!=null&&receiver.CanUse(item),"No useful fallback "+table.name);
            }
            var fallbackReceipts=new List<object>();
            foreach(var choice in new[]{("L1",0),("L3",1),("L8",0)})
            {
                region.Cancel();runner.StopRun();var mission=region.FindMission(choice.Item1);runner.Flow.LoadLevel(mission);
                var encounter=mission.Encounters[choice.Item2];runner.StartRun(encounter.Id,true,true,null,null);
                loadout.Restore(StarShift.Combat.WeaponBranch.Pulse,new[]{5,5,5});damage.RestoreCheckpoint(player.GetComponent<PlayerLives>().MaximumLives);
                reserve.RestoreCheckpoint(3);damage.GrantBriefProtection(1000);player.SetCinematicLock(false);view.Show(ShellScreen.Playing);Time.timeScale=3;
                await Until(()=>runner.Stage==EncounterStage.Running,2000,"Fallback encounter start");
                PickupDefinition fallback=null;int kills=0;
                Action<PickupDefinition,RewardDropTable> observe=(item,table)=>{if(item!=null&&item.Kind!=PickupKind.Shard)fallback=item;};director.DropSpawned+=observe;
                try
                {
                    while(fallback==null)
                    {
                        await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&e.CanReceiveDamage&&e.Definition.BodyDamagesPlayer&&e.GetComponent<AsteroidVisualMotion>()==null),5000,"Fallback target");
                        var enemy=EnemyHealth.Active.First(e=>e.IsAlive&&e.CanReceiveDamage&&e.Definition.BodyDamagesPlayer&&e.GetComponent<AsteroidVisualMotion>()==null);
                        kills++;enemy.ReceivePlayerHit(enemy.CurrentHp,player.gameObject);Check(kills<=encounter.GuaranteedMajorByKill,"Fallback missed deadline");
                    }
                    Check(fallback!=encounter.GuaranteeTable.GuaranteedPickup&&receiver.CanUse(fallback),"Emitted maxed pickup");
                    Check(director.GuaranteedMajorsRemaining==0,"Useful fallback did not credit floor");
                    var pickup=UnityEngine.Object.FindObjectsByType<RewardPickup>().First(p=>p.IsActive&&p.Item==fallback);Check(pickup.TryCollect(),"Fallback collection");
                    fallbackReceipts.Add(new{mission=mission.Id,preferred=encounter.GuaranteeTable.GuaranteedPickup.name,actual=fallback.name,kills,collected=true});
                }
                finally{director.DropSpawned-=observe;}
            }
            region.Cancel();runner.StopRun();save.ExitIsolated();shell.Boot();
            view.NewGame.onClick.Invoke();
            await Until(()=>!shell.IsBusy&&shell.Screen==ShellScreen.Playing,5000,"Campaign NewGame");
            var session=runner.GetComponent<GameplaySession>();Time.timeScale=3;
            await Until(()=>runner.Flow.CanContinue,3000,"Campaign intro");session.BeginMission();
            await Until(()=>runner.Stage==EncounterStage.Running,5000,"Campaign first encounter");damage.GrantBriefProtection(1000);
            bool Eligible(EnemyHealth e)=>e.IsAlive&&e.CanReceiveDamage&&e.Definition.BodyDamagesPlayer&&e.GetComponent<AsteroidVisualMotion>()==null;
            while(runner.CurrentIndex==0)
            {
                await Until(()=>runner.CurrentIndex!=0||EnemyHealth.Active.Any(Eligible),5000,"Foundation target/advance");
                if(runner.CurrentIndex!=0)break;
                var enemy=EnemyHealth.Active.First(Eligible);enemy.ReceivePlayerHit(enemy.CurrentHp,player.gameObject);
                foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive).ToArray())
                {if(pickup.Item!=null&&pickup.Item.name=="PulseCore")Check(pickup.TryCollect(),"Campaign Pulse collection");else pickup.Release();}
            }
            await Until(()=>runner.Stage==EncounterStage.Running,3000,"Formation start");
            Check(loadout.Level(StarShift.Combat.WeaponBranch.Pulse)==2&&loadout.Level(StarShift.Combat.WeaponBranch.Plasma)==0,"Campaign checkpoint loadout");
            async Task<int> CollectPlasma()
            {
                int ordinal=0;
                while(loadout.Level(StarShift.Combat.WeaponBranch.Plasma)==0)
                {
                    await Until(()=>EnemyHealth.Active.Any(Eligible),5000,"Plasma target");
                    var enemy=EnemyHealth.Active.First(Eligible);ordinal++;enemy.ReceivePlayerHit(enemy.CurrentHp,player.gameObject);
                    foreach(var pickup in UnityEngine.Object.FindObjectsByType<RewardPickup>().Where(p=>p.IsActive).ToArray())
                    {if(pickup.Item!=null&&pickup.Item.name=="PlasmaCore")Check(pickup.TryCollect(),"Campaign Plasma collection");else pickup.Release();}
                    Check(ordinal<=runner.Current.GuaranteedMajorByKill,"Campaign Plasma late");
                }
                return ordinal;
            }
            int firstPlasma=await CollectPlasma();int generation=runner.Generation;
            session.Pause();view.Retry.onClick.Invoke();view.Confirm.onClick.Invoke();
            await Until(()=>!shell.IsBusy&&runner.Generation!=generation&&runner.Stage==EncounterStage.Running,5000,"Campaign Retry");
            Check(loadout.Level(StarShift.Combat.WeaponBranch.Plasma)==0&&loadout.Level(StarShift.Combat.WeaponBranch.Pulse)==2,"Retry did not rollback collected Core");
            damage.GrantBriefProtection(1000);Time.timeScale=3;int retryPlasma=await CollectPlasma();
            Check(firstPlasma==retryPlasma,"Retry changed Core ordinal");
            Evidence("runtime",new{status="PASS",receipts,fallbackReceipts,campaignIntroAndCheckpointRetry=true,firstPlasma,retryPlasma,restartDeterminism=true,capOverflow=true,maxedFallback=true,actualEnemyDeathsAndPickups=true});
        }
        catch(Exception e){Evidence("runtime",new{status="FAIL",error=e.ToString(),receipts});}
        finally
        {
            if(original!=null)
            {
                region.Cancel();runner.StopRun();save.ExitIsolated();runner.Flow.LoadLevel(originalLevel);save.UseTestStorage(original);shell.Boot();Time.timeScale=1;
                string[] suffix={"",".bak",".invalid"};bool unchanged=true;
                for(int i=0;i<suffix.Length;i++){var after=File.Exists(original+suffix[i])?File.ReadAllBytes(original+suffix[i]):null;unchanged&=before[i]==null?after==null:after!=null&&before[i].SequenceEqual(after);}
                Evidence("save-guard",new{status=unchanged?"PASS":"FAIL",productionBytesUnchanged=unchanged});
                if(temp!=null&&Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
            }
        }
    }
}
