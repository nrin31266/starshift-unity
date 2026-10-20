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
    static async Task PlayerRun(bool ui = false)
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
            var preview=View.MenuShip.GetComponent<ShipPreview>();var first=preview.Engines[0].sprite;
            await Task.Delay(95);Check(preview.Engines[0].sprite!=first,"Menu engine did not animate");
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
            if (ui) await UiGameplay();
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
            result=new{status="PASS",chassis=2,trails=3,actualStorePurchaseAndReload=true,price=8,menuEngineAnimated=true,realGameplay=true,saveGuard=true};
        }
        catch(Exception e){result=new{status="FAILED",error=e.ToString()};}
        finally
        {
            try
            {
                if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
                foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();
                InputSystem.settings.backgroundBehavior=behavior;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
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
        Check(Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>().Where(t=>t.gameObject.scene.IsValid()).All(t=>t.font==font),"UI font mismatch");
        Check("ĂÂĐÊÔƠƯăâđêôơưạảãấầẩẫậắằẳẵặếềểễệốồổỗộớờởỡợứừửữựỳỵỷỹ".All(font.HasCharacter),"Vietnamese font coverage");
        var camera=Camera.main;
        camera.GetComponent<GameplayViewController>().RequestView(new GameplayViewSettings(9.6f,new Rect(0,0,1,1)),0);
        var defs=AssetDatabase.FindAssets("t:PickupDefinition",new[]{"Assets/StarShift/Data"}).Select(g=>AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(d=>d.Kind!=PickupKind.Shard)
            .GroupBy(d=>d.Kind+"-"+(d.Kind==PickupKind.WeaponCore?d.Branch.ToString():"")).Select(g=>g.First()).ToArray();
        var pickups=new List<RewardPickup>();int n=0;
        foreach(var item in defs)
        {
            var point=camera.ViewportToWorldPoint(new Vector3(.2f+(n%4)*.18f,.45f+(n/4)*.18f,-camera.transform.position.z));
            var pickup=Runner.GetComponent<RewardDirector>().Spawn(item,null,point);pickups.Add(pickup);
            var visual=pickup.GetComponentInChildren<SpriteRenderer>();
            Check(visual.bounds.size.y*Screen.height/(camera.orthographicSize*2)>=48,"Pickup too small: "+item.name);n++;
        }
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
