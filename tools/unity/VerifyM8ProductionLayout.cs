using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Encounters;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.UI;
using UnityEditor;
using UnityEngine;

public static class VerifyM8ProductionLayout
{
    static int targetWidth;
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static async Task Until(Func<bool> condition,int ms,string message){var end=Time.realtimeSinceStartup+ms/1000f;while(!condition()&&Time.realtimeSinceStartup<end)await Task.Delay(30);Check(condition(),message);}
    static async Task Capture(string id,int settle=200){if(settle>0)await Task.Delay(settle);var path=Path.GetFullPath("../.design/m8/screenshots/production-"+id+"-"+targetWidth+".png");if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);await Until(()=>File.Exists(path),4000,"screenshot "+id);await Task.Delay(40);}
    public static object Edit()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode required");int textures=0;
        foreach(var path in Directory.GetFiles("Assets/StarShift/Art/M8","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Check(importer.filterMode==FilterMode.Point&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed&&importer.npotScale==TextureImporterNPOTScale.None,"Importer mismatch "+path);
            if(path.EndsWith("-sheet.png"))
            {
                var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(f=>f.name,StringComparer.Ordinal).ToArray();int count=path.EndsWith("explosion-sheet.png")?8:4;
                Check(frames.Length==count,"Frame count "+path);
                foreach(var frame in frames)Check(frame.pivot==frame.rect.size*.5f,"Pivot "+frame.name);
            }
            textures++;
        }
        foreach(var name in new[]{"Scout","Raider","Bomber","Bulwark","Sniper","Interceptor","Carrier","VanguardFrigate","NebulaWarden","WardenDrone","SupplyDrone","StormAsteroid","ResourceAsteroid"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarShift/Prefabs/Enemies/"+name+".prefab");
            foreach(var r in root.GetComponentsInChildren<SpriteRenderer>(true))
                Check(!r.enabled||r.sprite==null||!AssetDatabase.GetAssetPath(r.sprite).Contains("Placeholders"),"Critical proxy remains "+name+"/"+r.name);
        }
        return new{status="PASS",nativeTextures=textures,criticalEnemyPrefabs=13};
    }
    public static async Task<object> Run(int width)
    {
        Check(Application.isPlaying && (width==1600||width==2000),"Play Mode / production resolution required");targetWidth=width;
        var runner=UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();Check(!runner.HasActiveRun,"Menu required");
        var save=runner.GetComponent<CampaignLifecycle>();var shell=runner.GetComponent<GameShell>();var session=runner.GetComponent<GameplaySession>();
        var view=UnityEngine.Object.FindAnyObjectByType<ShellView>();var player=GameObject.Find("Player");var damage=player.GetComponent<PlayerDamageLifecycle>();var pointer=player.GetComponent<InputSystemPointerInput>();
        bool oldDamage=damage.enabled,oldPointer=pointer.enabled;var original=save.SavePath;var bytes=new[]{"",".bak",".invalid"}.ToDictionary(s=>s,s=>File.Exists(original+s)?File.ReadAllBytes(original+s):null);
        var gameType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");var window=Resources.FindObjectsOfTypeAll(gameType).First();var size=gameType.GetProperty("selectedSizeIndex");var previousSize=size.GetValue(window);
        var temp=Path.Combine(Path.GetTempPath(),"starshift-m8-wide-"+Guid.NewGuid().ToString("N"),"save.json");
        object result=null;
        try
        {
            save.UseTestStorage(temp);shell.Boot();pointer.enabled=false;damage.enabled=false;
            gameType.GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(width,900),"StarShift M8 "+width+"x900"});
            await Until(()=>Screen.width==width&&Screen.height==900,3000,"resolution mismatch");await Capture("menu");
            view.Hangar.onClick.Invoke();await Capture("hangar");view.BackButtons.First(b=>b.gameObject.activeInHierarchy).onClick.Invoke();
            view.NewGame.onClick.Invoke();await Until(()=>!shell.IsBusy&&session.Flow.CanContinue,6000,"mission entry");await Capture("approach");
            session.BeginMission();await Until(()=>runner.Stage==EncounterStage.Running,15000,"combat entry");
            player.GetComponent<PlayerGameplayGate>().SetState(PlayerGameplayState.Active);player.GetComponent<PlayerRespawnEntrance>().Cancel();player.GetComponent<RelativeDragMovement>().ResetForCheckpoint(runner.Flow.Direction);await Task.Delay(1500);await Capture("gameplay");
            foreach(var branch in new[]{WeaponBranch.Pulse,WeaponBranch.Plasma,WeaponBranch.Homing})
            {
                var definition=player.GetComponent<WeaponLoadout>().Definition(branch);var projectile=UnityEngine.Object.Instantiate(definition.ProjectilePrefab);
                var position=Camera.main.ViewportToWorldPoint(new Vector3(.38f+(int)branch*.12f,.34f,10));
                projectile.Launch(definition,position,Vector2.up,definition.Damage,0,player,p=>UnityEngine.Object.Destroy(p.gameObject));
                Check(projectile.GetComponentInChildren<SpriteRenderer>().sprite==definition.ProjectileSprite,"Projectile sprite routing "+branch);
            }
            await Capture("weapons");
            var receiver=player.GetComponent<PlayerPickupReceiver>();
            foreach(var id in new[]{"ShieldCell","Overdrive","SupportDrone"})Check(receiver.TryCollect(AssetDatabase.LoadAssetAtPath<StarShift.Rewards.PickupDefinition>("Assets/StarShift/Data/Rewards/"+id+".asset")),"Effect collection "+id);
            await Capture("effects");Check(player.GetComponent<SupportDrone>()!=null||player.GetComponentInChildren<SupportDrone>()!=null,"support owner missing");
            Check(player.GetComponent<EmergencyMissileReserve>().TryUse(),"Reserve launch failed");await Capture("reserve",0);
            await Task.Delay(600);session.Pause();await Capture("pause");view.Resume.onClick.Invoke();
            foreach(var mission in new[]{"L4","L8"})
            {
                runner.StopRun();runner.Flow.LoadLevel(runner.GetComponent<RegionCampaignFlow>().FindMission(mission));
                if(mission=="L8")UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>().SetDestinationPalette();
                runner.StartRun("BOSS",true,true,null,null);player.GetComponent<PlayerGameplayGate>().SetState(PlayerGameplayState.Active);UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
                await Until(()=>EnemyHealth.Active.Any(e=>e.IsAlive&&e.GetComponent<BossPhaseRunner>()!=null&&e.GetComponent<EntryHoldFlight>().IsHolding),15000,"boss entry "+mission);player.GetComponent<PlayerRespawnEntrance>().Cancel();player.GetComponent<RelativeDragMovement>().ResetForCheckpoint(runner.Flow.Direction);await Capture(mission=="L4"?"vanguard":"warden");
                var boss=EnemyHealth.Active.First(e=>e.IsAlive&&e.GetComponent<BossPhaseRunner>()!=null);
                var shield=boss.GetComponent<BossShieldNetwork>();
                if(shield!=null){foreach(var node in boss.GetComponentsInChildren<EnemyHealth>().Where(e=>e!=boss).ToArray())node.ReceivePlayerHit(node.CurrentHp,player);await Until(()=>shield.IsExposed,4000,"shield expose");}
                boss.ReceivePlayerHit(boss.CurrentHp-Mathf.Max(1,Mathf.FloorToInt(boss.MaxHp*.2f)),player);await Task.Delay(160);
                Check(boss.GetComponent<EnemyDamagePresentation>().DamageStage==2,"Critical state "+mission);
                Check(boss.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="M8Smoke"&&r.enabled),"Critical smoke "+mission);
                await Capture(mission=="L4"?"vanguard-critical":"warden-critical");
            }
            var bg=Camera.main.transform.Find("M8Environment");Check(bg.localScale==Vector3.one,"background scale changed");Check(Camera.main.backgroundColor==(Color)new Color32(8,15,32,255),"navy fill missing");
            result=new{status="PASS",resolution=width+"x900",menu=true,hangar=true,approach=true,gameplay=true,pause=true,vanguard=true,warden=true,fixedBackground=true};
        }
        finally
        {
            runner.StopRun();save.ExitIsolated();save.UseTestStorage(original);shell.Boot();damage.enabled=oldDamage;pointer.enabled=oldPointer;size.SetValue(window,previousSize);
            foreach(var p in bytes){var after=File.Exists(original+p.Key)?File.ReadAllBytes(original+p.Key):null;Check(p.Value==null?after==null:after!=null&&p.Value.SequenceEqual(after),"save changed "+p.Key);}
            if(Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);
        }
        File.WriteAllText("../.design/m8/production-layout-"+width+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
