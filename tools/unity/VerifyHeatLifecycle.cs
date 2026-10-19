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
using UnityEngine;
using Rig = VerifyPacingRefinement.Rig;
public static class VerifyHeatLifecycle
{
    const string O="../.design/m5-heat-cleanup/";
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
        File.WriteAllText(O+"heat.json","{\"status\":\"RUNNING\"}");
        object result;
        try{using(var rig=new Rig(1600,6)){
            var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();var session=runner.GetComponent<GameplaySession>();
            var weapon=rig.Player.GetComponent<PulseLaserWeapon>();var loadout=rig.Player.GetComponent<WeaponLoadout>();
            var states=(WeaponHeatState[])typeof(PulseLaserWeapon).GetField("heatStates",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(weapon);
            rig.Protect();await rig.MouseEvent(new Vector2(400,300),true);await Task.Delay(800);
            var warm=weapon.Heat;Check(warm>3,"Held fire did not warm");await rig.MouseEvent(new Vector2(400,300),false);var released=weapon.Heat;
            await rig.MouseEvent(new Vector2(400,300),true);Check(weapon.Heat>=released-.2f,"Release/press reset heat");
            states[0].Reset();states[0].Tick(loadout.Definition(WeaponBranch.Pulse).ThermalProfile,true,7);
            var blockedStart=weapon.Heat;rig.Gate.SetState(PlayerGameplayState.Entry);var shots=weapon.ShotsFired;
            await Task.Delay(850);var blockedEnd=weapon.Heat;Check(blockedEnd<blockedStart-8&&weapon.ShotsFired==shots,"Blocked held fire did not cool independently");
            states[0].Reset();states[0].Tick(loadout.Definition(WeaponBranch.Pulse).ThermalProfile,true,7);
            session.Pause();Check(session.IsPaused,"Pause unavailable");var paused=weapon.Heat;await Task.Delay(250);Check(Mathf.Abs(weapon.Heat-paused)<.001f,"Pause cooled heat");session.Resume();rig.Protect();
            loadout.Restore(WeaponBranch.Plasma,new[]{1,1,1});var inactive=states[0].Heat;await Task.Delay(900);Check(states[0].Heat<inactive-10,"Inactive branch froze heat");
            states[1].Reset();states[1].Tick(loadout.ActiveDefinition.ThermalProfile,true,12);Check(weapon.IsOverheated,"Overheat seed");
            rig.Gate.SetState(PlayerGameplayState.Transition);var hot=weapon.Heat;await Task.Delay(350);Check(weapon.Heat<hot-10,"Overheated blocked branch did not cool");
            rig.Protect();await rig.MouseEvent(new Vector2(400,300),false);await rig.MouseEvent(new Vector2(400,300),true);var recoveryEnd=Time.realtimeSinceStartup+4;
            while((weapon.IsOverheated||weapon.ShotsFired<=shots)&&Time.realtimeSinceStartup<recoveryEnd)await Task.Delay(20);
            Check(!weapon.IsOverheated&&weapon.ShotsFired>shots,"Held fire did not recover: heat="+weapon.Heat+" held="+rig.Input.FireHeld+" gate="+rig.Gate.State+" shots="+weapon.ShotsFired+" before="+shots);await rig.MouseEvent(new Vector2(400,300),false);
            for(int i=0;i<3;i++){states[i].Reset();states[i].Tick(loadout.Definition((WeaponBranch)i).ThermalProfile,true,5);}
            var before=states.Select(s=>s.Heat).ToArray();
            var next=StarShift.Editor.RegionAAssetPaths.Load<LevelDefinition>("Assets/StarShift/Data/RegionA/Mission02.asset");
            runner.GetComponent<CampaignLifecycle>().AdvanceMission(next,true);
            Check(states.Select(s=>s.Heat).SequenceEqual(before),"Live mission handoff reset thermal state");
            var after=states.Select(s=>s.Heat).ToArray();await Task.Delay(750);Check(states[0].Heat<after[0],"Heat stopped after handoff");
            weapon.ResetForSession();Check(states.All(s=>s.Heat==0&&!s.IsOverheated),"New session did not reset");
            result=new{status="PASS",warm,releasePressPreserved=true,blockedStart,blockedEnd,pauseFrozen=true,inactiveCooling=true,overheatRecovery=true,liveMissionBefore=before,liveMissionAfter=after,sessionReset=true};
        }}catch(Exception e){result=new{status="FAILED",error=e.ToString()};}
        File.WriteAllText(O+"heat.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
    }
}
