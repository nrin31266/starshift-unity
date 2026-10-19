using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using StarShift.Editor;
using UnityEditor;
using UnityEngine;
public static class OrganizeRegionA
{
    public static object PrepareHeatVerification()
    {
        var sources=new[]{"VerifyPacingRefinement.cs","VerifyHumanFeel.cs","VerifyHeatLifecycle.cs"}.Select(n=>File.ReadAllLines("../tools/unity/"+n)).ToArray();
        var imports=sources.SelectMany(s=>s).Where(l=>l.StartsWith("using ")).Distinct();
        var body=string.Join("\n",sources.Select(s=>string.Join("\n",s.Where(l=>!l.StartsWith("using ")))));
        Directory.CreateDirectory("../.design/m5-shared-heat/human-regression/screenshots");
        body=body.Replace("../.design/m5-human-feel/","../.design/m5-shared-heat/human-regression/").Replace("../.design/m5-refinement/","../.design/m5-shared-heat/human-regression/");
        var path="../.design/m5-shared-heat/Verification.cs";File.WriteAllText(path,string.Join("\n",imports)+"\n"+body);return path;
    }
    public static object Run()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        const string root=RegionAAssetPaths.Root;
        Directory.CreateDirectory("../.design/m5-heat-cleanup");
        var deleted=new List<string>();var moves=new List<object>();
        var obsolete=new[]{"SineFormation","PincerLeft","PincerRight","DiagonalStorm","EscortLeft","EscortRight"};
        var all=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&!AssetDatabase.IsValidFolder(p)).ToArray();
        var files=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith(root,StringComparison.Ordinal)&&!p.Substring(root.Length).Contains('/')&&(p.EndsWith(".asset")||p.EndsWith(".mat"))).ToArray();
        var audits=new List<object>();
        foreach(var name in obsolete)
        {
            var path=root+name+".asset";if(AssetDatabase.LoadMainAssetAtPath(path)==null)continue;
            var uses=all.Where(p=>p!=path&&AssetDatabase.GetDependencies(p,false).Contains(path)).ToArray();
            audits.Add(new{path,guid=AssetDatabase.AssetPathToGUID(path),references=uses});
            if(uses.Length>0)throw new InvalidOperationException("Asset is still referenced: "+path);
        }
        File.WriteAllText("../.design/m5-heat-cleanup/deletion-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(audits,Newtonsoft.Json.Formatting.Indented));
        foreach(var file in files)
        {
            var name=Path.GetFileNameWithoutExtension(file);
            if(obsolete.Contains(name)){if(!AssetDatabase.DeleteAsset(file))throw new InvalidOperationException("Delete failed: "+file);deleted.Add(file);continue;}
            var asset=AssetDatabase.LoadMainAssetAtPath(file);var guid=AssetDatabase.AssetPathToGUID(file);
            var target=(string)typeof(RegionAAssetPaths).GetMethod("Resolve").MakeGenericMethod(asset.GetType()).Invoke(null,new object[]{file});
            var parent=Path.GetDirectoryName(target).Replace('\\','/');EnsureFolder(parent);
            var error=AssetDatabase.MoveAsset(file,target);if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            if(AssetDatabase.AssetPathToGUID(target)!=guid)throw new InvalidOperationException("GUID changed: "+target);
            moves.Add(new{from=file,to=target,guid});
        }
        var result=new{status="PASS",moves,deleted,guidPreserved=true};
        File.WriteAllText("../.design/m5-heat-cleanup/structure.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{moved=moves.Count,deleted=deleted.Count};
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        var parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
}
