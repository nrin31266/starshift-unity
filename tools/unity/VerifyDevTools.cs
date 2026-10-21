using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StarShift.Dev;
using StarShift.Encounters;
using StarShift.Gameplay;
using UnityEditor;
using UnityEngine;

public static class VerifyDevTools
{
    public static object Edit()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var dev=UnityEngine.Object.FindAnyObjectByType<DevFastStart>();var missions=dev.GetComponent<RegionCampaignFlow>().Missions;
        var method=typeof(StarShift.Editor.DevFastStartEditor).GetMethod("Routes",BindingFlags.Static|BindingFlags.NonPublic);
        int routes=0;
        for(int i=0;i<missions.Length;i++)
        {
            var level=missions[i];var values=(List<KeyValuePair<string,string>>)method.Invoke(null,new object[]{level,i+1<missions.Length});
            if(values.Select(v=>v.Key).Distinct().Count()!=values.Count)throw new Exception("Duplicate route "+level.Id);
            foreach(var option in values)
                if(option.Key!="TRANSITION"&&option.Key!="VICTORY"&&level.ResolveStart(option.Key)<0)throw new Exception("Unresolvable dropdown route "+option.Key);
            if(values.Any(v=>v.Key=="TRANSITION")!=(i+1<missions.Length))throw new Exception("Transition availability "+level.Id);
            if(values.Any(v=>v.Key=="VICTORY")!=(level.ResolveStart("BOSS")>=0))throw new Exception("Boss completion availability "+level.Id);
            routes+=values.Count;
        }
        var debug=dev.GetComponent<EncounterDebugView>();var so=new SerializedObject(debug);
        var flags=new[]{"showBounds","showPaths","showSpawnPoints","showHitboxes","showFps","showContentLabels"};
        var result=new{status="PASS",missions=missions.Length,validDropdownRoutes=routes,debugFlags=flags.ToDictionary(f=>f,f=>so.FindProperty(f).boolValue)};
        System.IO.Directory.CreateDirectory("../.design/art-closure");System.IO.File.WriteAllText("../.design/art-closure/dev-editor.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
