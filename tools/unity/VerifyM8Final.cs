using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyM8Final
{
    const string Evidence="../.design/m8/final/";
    static EncounterRunner Runner=>UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static CampaignLifecycle Save=>Runner.GetComponent<CampaignLifecycle>();
    static GameShell Shell=>Runner.GetComponent<GameShell>();
    static GameplaySession Session=>Runner.GetComponent<GameplaySession>();
    static ShellView View=>UnityEngine.Object.FindAnyObjectByType<ShellView>();
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static async Task Until(Func<bool> condition,int ms,string message)
    {float end=Time.realtimeSinceStartup+ms/1000f;while(!condition()&&Time.realtimeSinceStartup<end)await Task.Delay(30);Check(condition(),message);}
    static async Task Capture(string name)
    {
        await Task.Delay(120);Canvas.ForceUpdateCanvases();
        var path=Path.GetFullPath(Evidence+name+".png");if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);
        await Until(()=>File.Exists(path),4000,"Screenshot "+name);
    }
    public static object Player()
    {
        Check(Application.isPlaying&&!Runner.HasActiveRun,"Idle Play Mode required");
        Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"player.json","{\"status\":\"RUNNING\"}");
        _=PlayerRun();return "Player checks started";
    }
    public static object Survival()
    {
        Check(Application.isPlaying&&!Runner.HasActiveRun,"Idle Play Mode required");
        Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"player.json","{\"status\":\"RUNNING\"}");
        _=PlayerRun(false,true);return "Survival and protection checks started";
    }
    static async Task SurvivalGameplay()
    {
        var player=GameObject.Find("Player");
        var gate=player.GetComponent<PlayerGameplayGate>();
        var damage=player.GetComponent<PlayerDamageLifecycle>();
        var lives=player.GetComponent<PlayerLives>();
        damage.enabled=true;
        player.GetComponent<PlayerTemporaryEffects>().Clear();
        gate.SetState(PlayerGameplayState.Active);
        player.GetComponent<RelativeDragMovement>().SetCinematicPosition(Camera.main.ViewportToWorldPoint(new Vector3(.5f,.3f,-Camera.main.transform.position.z)));
        int before=lives.CurrentLives;
        Check(damage.TryReceiveHit(null),"Real lost-life trigger failed");
        Check(lives.CurrentLives==before-1,"Life was not deducted exactly once");
        await Task.Delay(130);
        var hull=player.transform.Find("Visual/HullPlaceholder").GetComponent<SpriteRenderer>();
        Check(!hull.enabled,"Hull remains visible during destruction");
        Check(player.transform.Find("DeathExplosion").GetComponent<SpriteRenderer>().enabled,"Death explosion hidden with hull");
        Check(Enumerable.Range(0,4).All(i=>player.transform.Find("DeathFragment"+i).GetComponent<SpriteRenderer>().enabled),"Independent fragments missing");
        await Capture("rebuild-player-lost-life");
        await Until(()=>gate.State==PlayerGameplayState.PostEntryInvincibility,5000,"Respawn did not reach protection");
        var cue=(SpriteRenderer)new SerializedObject(player.GetComponent<PlayerCombatPresentation>()).FindProperty("survival").objectReferenceValue;
        bool on=false,off=false;
        for(int i=0;i<12;i++)
        {
            await Task.Delay(40);
            Check(hull.enabled,"Protected hull blinked");
            on|=cue.enabled;off|=!cue.enabled;
        }
        Check(on&&off,"Protection ring did not blink");
        await Capture("rebuild-protection-ring");
        var trail=player.GetComponentsInChildren<TrailRenderer>(true).Single(t=>t.name=="MovementTrail");
        await Task.Delay(60);
        Check(!trail.emitting,"Stationary ship emits movement trail");
        Check(trail.startWidth<=.1f&&trail.time<=.16f,"Movement trail is oversized");
    }
    static async Task PlayerRun(bool ui = false, bool survival = false)
    {
        var original=Save.SavePath;var hashes=new Dictionary<string,byte[]>();
        foreach(var suffix in new[]{"",".bak",".invalid"})hashes[suffix]=File.Exists(original+suffix)?File.ReadAllBytes(original+suffix):null;
        var disabled=InputSystem.devices.Where(d=>d.native&&d.enabled&&(d is UnityEngine.InputSystem.Pointer||d is Keyboard||d is Gamepad)).ToArray();
        var oldMouse=Mouse.current;Mouse mouse=null;var behavior=InputSystem.settings.backgroundBehavior;var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        var player=GameObject.Find("Player");var damage=player.GetComponent<PlayerDamageLifecycle>();var wasEnabled=damage.enabled;
        var catalog=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/StarShift/Data/Cosmetics/ShipCosmetics.asset");
        object result=null;
        EditorApplication.LockReloadAssemblies();
        try
        {
            foreach(var d in disabled)InputSystem.DisableDevice(d);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
            var fixture=CampaignProgress.Copy(Save.PersistentSnapshot);
            fixture.Profile.StellarShardBalance=100;fixture.Profile.OwnedCosmetics=catalog.Defaults;fixture.Profile.EquippedCosmetics=catalog.Defaults;
            var temp=Path.Combine(Path.GetTempPath(),"starshift-final-"+Guid.NewGuid().ToString("N"),"save.json");
            Directory.CreateDirectory(Path.GetDirectoryName(temp));File.WriteAllText(temp,JsonUtility.ToJson(fixture,true));
            Save.UseTestStorage(temp);Shell.Boot();
            await Capture("menu-player");
            var preview=View.MenuShip.GetComponent<ShipPreview>();
            Check(preview.Engines.Length==2,"Expected one plume for each of two corrected nozzles");
            Check(player.GetComponentsInChildren<SpriteRenderer>(true).Count(r=>r.name.StartsWith("M8Engine"))==2,"Duplicate gameplay plume renderers");
            View.Store.onClick.Invoke();await Task.Delay(100);View.CosmeticTabs[2].onClick.Invoke();View.CosmeticCards[2].onClick.Invoke();
            await Capture("store-rainbow");
            View.CosmeticAction.onClick.Invoke();if(View.Confirmation.activeSelf)View.Confirm.onClick.Invoke();await Task.Delay(100);
            Check(catalog.Owns(Save.PersistentSnapshot.Profile,catalog.Find("trail-rainbow")),"Rainbow purchase failed");
            Check(Save.PersistentSnapshot.Profile.StellarShardBalance==92,"Rainbow price was not 8");
            Check(Save.ReloadStorage(),"Reload cosmetic save");
            Check(catalog.Equipped(Save.PersistentSnapshot.Profile,CosmeticSlot.Trail).Id=="trail-rainbow","Rainbow equip did not persist");
            View.CosmeticTabs[0].onClick.Invoke();View.CosmeticCards[1].onClick.Invoke();await Capture("store-raptor");
            View.CosmeticAction.onClick.Invoke();if(View.Confirmation.activeSelf)View.Confirm.onClick.Invoke();await Task.Delay(100);
            Check(catalog.Owns(Save.PersistentSnapshot.Profile,catalog.Find("chassis-swept")),"Existing chassis purchase failed");
            Check(Save.EquipCosmetic(catalog,"skin-amber"),"Amber purchase");
            Shell.Boot();await Capture("menu-raptor-amber");
            View.NewGame.onClick.Invoke();if(View.Confirmation.activeSelf)View.Confirm.onClick.Invoke();
            await Until(()=>!Shell.IsBusy&&Session.Flow.CanContinue,6000,"Mission briefing");
            await Capture("approach-raptor");Session.BeginMission();
            await Until(()=>Runner.Stage==EncounterStage.Running,15000,"Real combat");
            damage.enabled=false;player.GetComponent<PlayerGameplayGate>().SetState(PlayerGameplayState.Active);
            if (survival) await SurvivalGameplay();
            else if (ui) await UiGameplay();
            else foreach(var chassis in new[]{"chassis-arrow","chassis-swept"})
            foreach(var trail in new[]{"trail-none","trail-ion","trail-rainbow"})
            {
                var visualProfile=CampaignProgress.Copy(Save.PersistentSnapshot.Profile);
                visualProfile.OwnedCosmetics=catalog.Options.Select(o=>o.Id).ToArray();
                visualProfile.EquippedCosmetics=new[]{chassis,"skin-cyan",trail};
                player.GetComponent<ShipAppearance>().Apply(visualProfile);
                var point=new Vector2(Screen.width*.5f,Screen.height*.2f);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left));await Task.Delay(80);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point+new Vector2(80,15)}.WithButton(MouseButton.Left));await Task.Delay(130);
                await Capture(chassis+"-"+trail);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point+new Vector2(80,15)});
            }
            result=new{status="PASS",chassis=2,trails=3,actualStorePurchaseAndReload=true,price=8,singlePlumePerNozzle=true,realGameplay=true,survivalAndRing=survival,saveGuard=true};
        }
        catch(Exception e){result=new{status="FAILED",error=e.ToString()};}
        finally
        {
            try
            {
                if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
                foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();
                InputSystem.settings.backgroundBehavior=behavior;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
                if(View.Confirmation.activeSelf) View.Cancel.onClick.Invoke();
                damage.enabled=wasEnabled;Runner.StopRun();Save.ExitIsolated();Save.UseTestStorage(original);Shell.Boot();
                foreach(var pair in hashes){var after=File.Exists(original+pair.Key)?File.ReadAllBytes(original+pair.Key):null;Check(pair.Value==null?after==null:after!=null&&after.SequenceEqual(pair.Value),"Production save changed: "+pair.Key);}
            }
            catch(Exception e){result=new{status="FAILED",cleanupError=e.ToString()};}
            finally{EditorApplication.UnlockReloadAssemblies();File.WriteAllText(Evidence+(ui?"ui.json":"player.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));}
        }
    }

    public static object Ui()
    {
        Check(Application.isPlaying&&!Runner.HasActiveRun,"Idle Play Mode required");
        Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"ui.json","{\"status\":\"RUNNING\"}");
        _=PlayerRun(true);return "UI checks started";
    }
    static void Resolution(int width)
    {
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var view=Resources.FindObjectsOfTypeAll(type).First();
        type.GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,new object[]{new Vector2(width,900),"StarShift final "+width});
    }
    static async Task UiGameplay()
    {
        var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/StarShift/UI/Fonts/VT323-Regular.ttf");
        var allowed=new[]{"VT323-Regular.ttf","ChakraPetch-Medium.ttf","ChakraPetch-SemiBold.ttf"};
        Check(Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>().Where(t=>t.gameObject.scene.IsValid()).All(t=>t.font!=null&&allowed.Contains(Path.GetFileName(AssetDatabase.GetAssetPath(t.font)))),"UI font mismatch");
        Check("ĂÂĐÊÔƠƯăâđêôơưạảãấầẩẫậắằẳẵặếềểễệốồổỗộớờởỡợứừửữựỳỵỷỹ".All(font.HasCharacter),"Vietnamese font coverage");
        var camera=Camera.main;
        camera.GetComponent<GameplayViewController>().RequestView(new GameplayViewSettings(9.6f,new Rect(0,0,1,1)),0);
        var defs=AssetDatabase.FindAssets("t:PickupDefinition",new[]{"Assets/StarShift/Data"}).Select(g=>AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(d=>d.Kind!=PickupKind.Shard)
            .GroupBy(d=>d.Kind+"-"+(d.Kind==PickupKind.WeaponCore?d.Branch.ToString():"")).Select(g=>g.First()).ToArray();
        var pickups=new List<RewardPickup>();int n=0;
        foreach(var item in defs)
        {
            if (pickups.Count == 4)
            {
                await Capture("hud-pickups-first-four");
                foreach (var previous in pickups) previous.Release();
                pickups.Clear();
            }
            var point=camera.ViewportToWorldPoint(new Vector3(.2f+(n%4)*.18f,.55f,-camera.transform.position.z));
            var pickup=Runner.GetComponent<RewardDirector>().Spawn(item,null,point);pickups.Add(pickup);
            Check(pickup!=null,"Pickup spawn rejected: "+item.name);
            var visual=pickup.GetComponentInChildren<SpriteRenderer>();
            Check(visual.bounds.size.y*Screen.height/(camera.orthographicSize*2)>=48,"Pickup too small: "+item.name);n++;
        }
        var firstFrames=pickups.Select(p=>p.GetComponentInChildren<SpriteRenderer>().sprite).ToArray();
        await Task.Delay(250);
        Check(pickups.Where((p,i)=>p.GetComponentInChildren<SpriteRenderer>().sprite!=firstFrames[i]).Count()==pickups.Count,"Pickup glint playback failed");
        foreach(var kind in new[]{PickupKind.Shield,PickupKind.Overdrive,PickupKind.SupportDrone})GameObject.Find("Player").GetComponent<PlayerPickupReceiver>().TryCollect(defs.First(d=>d.Kind==kind));
        await Capture("hud-pickups-1600");
        Session.Pause();await Capture("pause-final");var pausedFrame=pickups[0].GetComponentInChildren<SpriteRenderer>().sprite;await Task.Delay(180);
        Check(pickups[0].GetComponentInChildren<SpriteRenderer>().sprite==pausedFrame,"Pickup animation ignored Pause");
        View.Resume.onClick.Invoke();
        Resolution(2000);await Until(()=>Screen.width==2000,3000,"2000 UI resolution");await Capture("hud-pickups-2000");
        Session.Pause();View.PauseMenu.onClick.Invoke();if(View.Confirmation.activeSelf)View.Confirm.onClick.Invoke();await Until(()=>Shell.Screen==ShellScreen.Menu,5000,"Pause to Menu");
        await Capture("menu-final-2000");
        Resolution(1600);await Until(()=>Screen.width==1600,3000,"Restore 1600");
        foreach(var action in new[]{View.Missions,View.Settings,View.Help,View.Credits})
        {
            action.onClick.Invoke();await Capture("screen-"+Shell.Screen.ToString().ToLowerInvariant());
            View.BackButtons.First(b=>b.gameObject.activeInHierarchy).onClick.Invoke();await Task.Delay(100);
        }
        View.NewGame.onClick.Invoke();Check(View.Confirmation.activeSelf,"Confirmation missing");await Capture("confirmation-final");View.Cancel.onClick.Invoke();
        Check(Camera.main.transform.Find("M8Environment").localScale==Vector3.one,"Background world scale changed");
    }

}
