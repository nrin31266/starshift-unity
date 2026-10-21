using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;
using StarShift.Enemies;
using StarShift.Gameplay;
using UnityEngine.UI;

public sealed class PolishPointer : PlayerPointerInput
{
    public bool Held;
    public override bool FireHeld => Held;
    public override Vector2 ReadDelta()=>Vector2.zero;
    public override void ResetDrag(){}
}

public static class VerifyPresentationPolish
{
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static async Task Until(Func<bool> condition,int milliseconds,string message)
    {float end=Time.realtimeSinceStartup+milliseconds/1000f;while(!condition()&&Time.realtimeSinceStartup<end)await Task.Delay(15);Check(condition(),message);}
    const string Output="../.design/presentation-polish/";
    public static object Thermal()
    {
        var results=AssetDatabase.FindAssets("t:WeaponThermalProfile").Select(guid=>{
            var p=AssetDatabase.LoadAssetAtPath<WeaponThermalProfile>(AssetDatabase.GUIDToAssetPath(guid));
            var heat=new WeaponHeatState();heat.Tick(p,true,100);
            Check(heat.IsOverheated,"MAX did not latch");
            heat.Tick(p,false,p.Capacity*.61f/p.OverheatCoolRate);
            Check(heat.IsOverheated&&heat.Heat>0,"Released at old 40% threshold");
            var before=heat.Heat;heat.Tick(p,true,0);Check(heat.Heat==before,"Zero delta changed heat");
            heat.Tick(p,true,.01f);Check(heat.IsOverheated,"Held input bypassed vent");
            heat.Tick(p,false,100);Check(!heat.IsOverheated&&heat.Heat==0,"Vent did not reach zero");
            heat.Tick(p,true,2);before=heat.Heat;heat.Tick(p,false,p.CoolingDelay+.2f);
            Check(Mathf.Abs(before-heat.Heat-p.CoolRate*.2f)<.001f,"Manual release delay/rate");
            Check(p.OverheatCoolRate>p.CoolRate*3,"Vent not visibly faster");
            return new{profile=p.name,normal=p.CoolRate,vent=p.OverheatCoolRate,recoverySeconds=p.Capacity/p.OverheatCoolRate};
        }).ToArray();
        Check(results.Length==3,"Three branch profiles required");
        Directory.CreateDirectory(Output);var result=new{status="PASS",maxLocksUntilZero=true,releaseCannotBypass=true,zeroDeltaFrozen=true,results};
        File.WriteAllText(Output+"thermal.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static async Task<object> Settings()
    {
        Check(Application.isPlaying,"Play Mode required");
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();Check(!runner.HasActiveRun,"Menu required");
        var save=runner.GetComponent<CampaignLifecycle>();var shell=runner.GetComponent<GameShell>();var view=UnityEngine.Object.FindAnyObjectByType<ShellView>();
        var original=save.SavePath;var originalBytes=new[]{"",".bak",".invalid"}.ToDictionary(s=>s,s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null);
        var temp=Path.Combine(Path.GetTempPath(),"starshift-polish-"+Guid.NewGuid().ToString("N"),"save.json");
        Func<SaveEnvelope,bool> valid=e=>SaveValidation.IsValid(e,(m,c)=>runner.GetComponent<StarShift.Gameplay.RegionCampaignFlow>().FindMission(m)?.ResolveStart(c)>=0,5);
        var seed=new SaveEnvelope{SchemaVersion=SaveValidation.CurrentVersion,Profile=new PlayerProfile{StellarShardBalance=73,HighScore=999,UnlockedMissions=new[]{"L1","L5"},Records=new[]{new MissionRecord{MissionId="L1",BestScore=999}},OwnedCosmetics=new[]{"trail-rainbow"}},Settings=new SettingsData{MusicVolume=.2f,SfxVolume=.3f}};
        var store=new JsonSaveStore(temp);Check(store.Write(seed,valid),"Fixture write");Check(store.Write(seed,valid),"Fixture backup");File.WriteAllText(temp+".invalid","old invalid");
        Directory.CreateDirectory(Output+"screenshots");
        try
        {
            save.UseTestStorage(temp);shell.Boot();await Task.Delay(100);
            Check(view.Wallet.text=="73"&&view.WalletIcon.gameObject.activeInHierarchy&&view.WalletIcon.sprite!=null,"Coin wallet icon/value");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output+"screenshots/menu-coin.png"));await Task.Delay(80);
            view.Settings.onClick.Invoke();Check(view.ResetAll.IsInteractable(),"Reset unavailable in Menu Settings");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output+"screenshots/settings-reset.png"));await Task.Delay(80);
            view.ResetAll.onClick.Invoke();Check(view.Confirmation.activeSelf,"Missing destructive confirmation");
            Check(EventSystem.current.currentSelectedGameObject==view.Cancel.gameObject,"Confirmation must default to Cancel");
            view.Cancel.onClick.Invoke();Check(save.PersistentSnapshot.Profile.StellarShardBalance==73&&File.Exists(temp+".bak"),"Cancel changed data");
            view.ResetAll.onClick.Invoke();view.Confirm.onClick.Invoke();await Task.Delay(100);
            var fresh=save.PersistentSnapshot;
            Check(shell.Screen==ShellScreen.Menu&&!fresh.HasCampaign&&fresh.Profile.StellarShardBalance==0&&fresh.Profile.HighScore==0&&fresh.Profile.UnlockedMissions.Length==0&&fresh.Profile.Records.Length==0&&fresh.Profile.OwnedCosmetics.Length==0,"Reset did not clear progress/profile");
            Check(fresh.Settings.MusicVolume==1&&fresh.Settings.SfxVolume==1&&fresh.Settings.ScreenShakeEnabled&&fresh.Settings.VibrationEnabled,"Settings not reset");
            Check(!File.Exists(temp+".bak")&&!File.Exists(temp+".invalid"),"Old progress survives in recovery history");
            Check(save.ReloadStorage()&&save.PersistentSnapshot.Profile.StellarShardBalance==0,"Reload lost reset");
            view.NewGame.onClick.Invoke();
            var session=runner.GetComponent<GameplaySession>();
            await Until(()=>!shell.IsBusy&&session.Flow.CanContinue,6000,"Reset-from-Pause fixture entry");
            session.BeginMission();await Until(()=>session.CanPause,12000,"Reset-from-Pause fixture combat");
            Check(save.Mode==SessionMode.Campaign&&runner.HasActiveRun,"Reset-from-Pause fixture did not start Campaign");
            runner.GetComponent<GameplaySession>().Pause();await Task.Delay(60);view.PauseSettings.onClick.Invoke();
            Check(shell.Screen==ShellScreen.Settings&&view.ResetAll.IsInteractable(),"Reset inaccessible from Pause Settings");
            view.ResetAll.onClick.Invoke();view.Confirm.onClick.Invoke();await Task.Delay(100);
            Check(shell.Screen==ShellScreen.Menu&&!runner.HasActiveRun&&save.Mode==SessionMode.Frontend&&!save.PersistentSnapshot.HasCampaign,"Reset from Pause left active Campaign");
            var progress=new CampaignProgress(store,valid);Check(store.Write(seed,valid)&&progress.Reload(),"Failure fixture");
            Directory.CreateDirectory(temp+".tmp");var before=File.ReadAllBytes(temp);
            Check(!progress.ResetAll()&&before.SequenceEqual(File.ReadAllBytes(temp))&&progress.Snapshot.Profile.StellarShardBalance==73,"Failed reset mutated state");Directory.Delete(temp+".tmp");
            var future=JsonUtility.ToJson(seed).Replace("\"SchemaVersion\":2","\"SchemaVersion\":999");File.WriteAllText(temp,future);before=File.ReadAllBytes(temp);
            Check(!store.Write(fresh,valid,resetHistory:true)&&before.SequenceEqual(File.ReadAllBytes(temp)),"Reset overwrote unsupported schema");
        }
        finally
        {
            if(view.Confirmation.activeSelf)view.Cancel.onClick.Invoke();
            await Until(()=>!shell.IsBusy,5000,"Pending shell operation must finish before restoring production storage");
            runner.StopRun();save.ExitIsolated();save.UseTestStorage(original);shell.Boot();
            foreach(var p in originalBytes){var after=File.Exists(original+p.Key)?File.ReadAllBytes(original+p.Key):null;Check(p.Value==null?after==null:after!=null&&p.Value.SequenceEqual(after),"Production save changed");}
            if(Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
        }
        var result=new{status="PASS",cancelPreserves=true,confirmationDefaultsCancel=true,resetProfileCampaignAndSettings=true,resetFromPause=true,reloadPersists=true,historyCleared=true,ioFailurePreserves=true,unsupportedPreserved=true,coinWallet=true,productionSaveGuard=true};
        File.WriteAllText(Output+"settings.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static async Task<object> Combat()
    {
        Check(Application.isPlaying,"Play Mode required");
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();Check(!runner.HasActiveRun,"Menu required");
        var save=runner.GetComponent<CampaignLifecycle>();var shell=runner.GetComponent<GameShell>();
        var original=save.SavePath;var bytes=new[]{"",".bak",".invalid"}.ToDictionary(s=>s,s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null);
        var temp=Path.Combine(Path.GetTempPath(),"starshift-combat-polish-"+Guid.NewGuid().ToString("N"),"save.json");
        var damage=UnityEngine.Object.FindAnyObjectByType<PlayerDamageLifecycle>();var weapon=damage.GetComponent<PulseLaserWeapon>();
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.NonPublic;
        var pointerField=typeof(PulseLaserWeapon).GetField("pointerInput",fields);var oldPointer=pointerField.GetValue(weapon);
        var fake=damage.gameObject.AddComponent<PolishPointer>();bool oldDamage=damage.enabled;
        var oldLevel=runner.Flow.Level;
        try
        {
            save.UseTestStorage(temp);shell.Boot();damage.enabled=false;pointerField.SetValue(weapon,fake);
            runner.Flow.LoadLevel(runner.GetComponent<RegionCampaignFlow>().FindMission("L4"));runner.StartRun("BOSS",true,true,null,null);
            var gate=damage.GetComponent<PlayerGameplayGate>();gate.SetCinematicLock(false);gate.SetState(PlayerGameplayState.PostEntryInvincibility);
            var deadline=Time.realtimeSinceStartup+8;BossPhaseRunner boss=null;
            while(Time.realtimeSinceStartup<deadline)
            {boss=runner.GetComponentInChildren<BossPhaseRunner>();if(boss!=null&&boss.Health.CanReceiveDamage)break;await Task.Delay(30);}
            Check(boss!=null&&boss.Health.CanReceiveDamage,"Boss did not reach combat");
            fake.Held=true;await Task.Delay(120);Check(weapon.ShotsFired>0,"Held test input did not fire actual weapon");
            var heat=(WeaponHeatState)typeof(PulseLaserWeapon).GetField("heat",fields).GetValue(weapon);
            heat.Tick(damage.GetComponent<WeaponLoadout>().ActiveDefinition.ThermalProfile,true,100);var shots=weapon.ShotsFired;
            await Task.Delay(100);Check(weapon.IsOverheated&&!weapon.CanFire&&weapon.ShotsFired==shots,"MAX did not block real primary fire");
            var hud=UnityEngine.Object.FindAnyObjectByType<GameplayHud>();
            Check(((Text)typeof(GameplayHud).GetField("heatStatus",fields).GetValue(hud)).text==GameplayCopy.HeatOverheated,"Vent state unreadable");
            boss.Health.ReceivePlayerHit(boss.Health.MaxHp/3,damage.gameObject);await Task.Delay(40);
            var bossHud=UnityEngine.Object.FindAnyObjectByType<BossHud>();var percentage=(Text)typeof(BossHud).GetField("percentage",fields).GetValue(bossHud);
            Check(percentage.text==Mathf.CeilToInt(100f*boss.Health.CurrentHp/boss.Health.MaxHp)+"%","Actual boss health percentage");
            Check(percentage.cachedTextGenerator.vertexCount>=12,"Boss percentage has no rendered glyphs");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output+"screenshots/boss-heat-vent.png"));await Task.Delay(80);
            var vent=(Image)typeof(GameplayHud).GetField("heatVent",fields).GetValue(hud);Check(vent.enabled&&vent.sprite!=null,"Heat vent animation missing");
            Time.timeScale=0;var ventFrozen=vent.sprite;var frozen=heat.Heat;await Task.Delay(120);Check(heat.Heat==frozen&&vent.sprite==ventFrozen,"Paused real weapon vent advanced");Time.timeScale=1;
            deadline=Time.realtimeSinceStartup+3;float minimum=heat.Heat;
            while(weapon.IsOverheated&&Time.realtimeSinceStartup<deadline)
            {Check(weapon.ShotsFired==shots,"Shot fired before vent completed");minimum=Mathf.Min(minimum,heat.Heat);await Task.Delay(5);}
            Check(!weapon.IsOverheated&&weapon.HeatFraction<.01f,"Actual weapon unlocked above zero");
            await Task.Delay(150);Check(weapon.ShotsFired>shots&&!vent.enabled,"Held fire did not automatically resume or vent remained");
            fake.Held=false;damage.enabled=true;gate.SetState(PlayerGameplayState.Active);
            Check(damage.TryReceiveHit(null),"Actual life-loss presentation did not start");await Task.Delay(80);
            var fragments=(SpriteRenderer[])typeof(PlayerDeathPresentation).GetField("fragments",fields).GetValue(damage.GetComponent<PlayerDeathPresentation>());
            Check(fragments.Length==4&&fragments.All(f=>f.enabled)&&fragments.Select(f=>f.sprite).Distinct().Count()==4,"Four generic fragments not visible on life loss");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output+"screenshots/generic-death-fragments.png"));await Task.Delay(80);
        }
        finally
        {
            fake.Held=false;pointerField.SetValue(weapon,oldPointer);UnityEngine.Object.Destroy(fake);damage.enabled=oldDamage;
            runner.StopRun();save.ExitIsolated();runner.Flow.LoadLevel(oldLevel);save.UseTestStorage(original);shell.Boot();
            foreach(var p in bytes){var after=File.Exists(original+p.Key)?File.ReadAllBytes(original+p.Key):null;Check(p.Value==null?after==null:after!=null&&p.Value.SequenceEqual(after),"Production save changed");}
            if(Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
        }
        var result=new{status="PASS",actualWeaponMaxLock=true,zeroRecovery=true,heldAutoResume=true,pauseFrozen=true,actualBossPercentage=true,renderedPercentage=true,actualLifeLossFourFragments=true,productionSaveGuard=true};
        File.WriteAllText(Output+"combat.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
