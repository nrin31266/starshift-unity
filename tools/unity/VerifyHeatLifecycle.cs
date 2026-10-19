using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.Rewards;
using UnityEngine;
using Rig = VerifyPacingRefinement.Rig;
public static class VerifyHeatLifecycle
{
    const string O="../.design/m5-shared-heat/";
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    public static object BeginFullRegion()
    {
        Check(Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(O+"heat.json"))["status"].ToString()=="PASS","Heat prerequisite");
        Check(!File.Exists(O+"human-regression/full-region.json"),"Inspect the previous full run before repeating");
        typeof(VerifyHumanFeel).GetMethod("FullRegion",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        return new{started=true};
    }
    public static object Begin(){_=Run();return new{started=true};}
    static async Task Run()
    {
        Directory.CreateDirectory(O);
        File.WriteAllText(O+"heat.json","{\"status\":\"RUNNING\"}");
        object result;
        try{using(var rig=new Rig(1600,6)){
            var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();var session=runner.GetComponent<GameplaySession>();
            var weapon=rig.Player.GetComponent<PulseLaserWeapon>();var loadout=rig.Player.GetComponent<WeaponLoadout>();
            var heat=(WeaponHeatState)typeof(PulseLaserWeapon).GetField("heat",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(weapon);
            var receiver=rig.Player.GetComponent<PlayerPickupReceiver>();
            var delay=typeof(WeaponHeatState).GetField("coolingDelayRemaining",BindingFlags.Instance|BindingFlags.NonPublic);
            rig.Protect();await rig.MouseEvent(new Vector2(400,300),true);await Task.Delay(800);
            var warm=weapon.Heat;Check(warm>3,"Held fire did not warm");await rig.MouseEvent(new Vector2(400,300),false);var released=weapon.Heat;
            await rig.MouseEvent(new Vector2(400,300),true);Check(weapon.Heat>=released-.2f,"Release/press reset heat");
            heat.Reset();heat.Tick(loadout.Definition(WeaponBranch.Pulse).ThermalProfile,true,7);
            var blockedStart=weapon.Heat;rig.Gate.SetState(PlayerGameplayState.Entry);var shots=weapon.ShotsFired;
            await Task.Delay(850);var blockedEnd=weapon.Heat;Check(blockedEnd<blockedStart-8&&weapon.ShotsFired==shots,"Blocked held fire did not cool independently");
            heat.Reset();heat.Tick(loadout.Definition(WeaponBranch.Pulse).ThermalProfile,true,7);
            session.Pause();Check(session.IsPaused,"Pause unavailable");var paused=weapon.Heat;await Task.Delay(250);Check(Mathf.Abs(weapon.Heat-paused)<.001f,"Pause cooled heat");session.Resume();rig.Protect();
            var beforePickup=weapon.Heat;var beforeDelay=(float)delay.GetValue(heat);
            Check(receiver.TryCollect(UnityEditor.AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/PlasmaCore.asset")),"New Core pickup rejected");
            Check(loadout.ActiveBranch==WeaponBranch.Plasma&&loadout.ActiveLevel==1&&weapon.Heat==beforePickup&&(float)delay.GetValue(heat)==beforeDelay,"New branch reset shared heat/delay");
            Check(receiver.TryCollect(UnityEditor.AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/PlasmaCore.asset")),"Upgrade Core pickup rejected");
            Check(loadout.ActiveLevel==2&&weapon.Heat==beforePickup&&(float)delay.GetValue(heat)==beforeDelay,"Upgrade reset shared heat/delay");
            Check(receiver.TryCollect(UnityEditor.AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/HomingCore.asset")),"Homing Core pickup rejected");
            for(int i=0;i<3;i++)Check(loadout.Cycle()&&weapon.Heat==beforePickup&&(float)delay.GetValue(heat)==beforeDelay,"Cycle reset shared heat/delay");
            Check(Mathf.Approximately(weapon.HeatFraction,weapon.Heat/100),"HUD heat fraction changed on switch");
            rig.Gate.SetState(PlayerGameplayState.Entry);var switchedStart=weapon.Heat;await Task.Delay(900);Check(weapon.Heat<switchedStart-10,"Shared heat stopped cooling after switch");
            heat.Reset();heat.Tick(loadout.ActiveDefinition.ThermalProfile,true,12);Check(weapon.IsOverheated,"Overheat seed");
            var latchedHeat=weapon.Heat;for(int i=0;i<3;i++)Check(loadout.Cycle()&&weapon.Heat==latchedHeat&&weapon.IsOverheated&&!weapon.CanFire,"Switch bypassed shared overheat");
            rig.Protect();Check(receiver.TryCollect(UnityEditor.AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/PlasmaCore.asset"))&&weapon.Heat==latchedHeat&&weapon.IsOverheated&&!weapon.CanFire,"Core bypassed shared overheat");
            rig.Gate.SetState(PlayerGameplayState.Transition);var hot=weapon.Heat;await Task.Delay(350);Check(weapon.Heat<hot-10,"Overheated blocked branch did not cool");
            rig.Protect();await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);var recoveryEnd=Time.realtimeSinceStartup+4;
            while((weapon.IsOverheated||weapon.ShotsFired<=shots)&&Time.realtimeSinceStartup<recoveryEnd)await Task.Delay(20);
            Check(!weapon.IsOverheated&&weapon.ShotsFired>shots,"Held fire did not recover: heat="+weapon.Heat+" held="+rig.Input.FireHeld+" gate="+rig.Gate.State+" shots="+weapon.ShotsFired+" before="+shots);await rig.MouseEvent(new Vector2(400,300),false);
            heat.Reset();heat.Tick(loadout.ActiveDefinition.ThermalProfile,true,5);
            var before=weapon.Heat;
            var next=StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission02.asset");
            runner.GetComponent<CampaignLifecycle>().AdvanceMission(next,true);
            Check(weapon.Heat==before,"Live mission handoff reset thermal state");
            var after=weapon.Heat;await Task.Delay(750);Check(weapon.Heat<after,"Heat stopped after handoff");
            heat.Reset();heat.Tick(loadout.ActiveDefinition.ThermalProfile,true,20);weapon.ApplyDeathDowngrade();Check(weapon.Heat==0&&!weapon.IsOverheated,"Death did not reset shared heat");
            heat.Tick(loadout.ActiveDefinition.ThermalProfile,true,5);weapon.ResetForSession();Check(weapon.Heat==0&&!weapon.IsOverheated,"New session did not reset");
            result=new{status="PASS",warm,releasePressPreserved=true,blockedStart,blockedEnd,pauseFrozen=true,sharedHeatAcrossPickupsAndCycle=true,coolingDelayPreserved=true,overheatCannotBeBypassed=true,overheatRecovery=true,liveMissionBefore=before,liveMissionAfter=after,sessionReset=true};
        }}catch(Exception e){result=new{status="FAILED",error=e.ToString()};}
        File.WriteAllText(O+"heat.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
    }
}
