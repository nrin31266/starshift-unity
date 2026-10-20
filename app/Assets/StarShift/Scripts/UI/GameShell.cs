using System;
using System.Collections;
using System.Collections.Generic;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.Rewards;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StarShift.UI
{
    [DisallowMultipleComponent]
    public sealed class GameShell : MonoBehaviour
    {
        [SerializeField] private ShellView view;
        [SerializeField] private GameplaySession session;
        [SerializeField] private InputSystemPointerInput pointer;
        [SerializeField] private SettingsFeedback feedback;
        [SerializeField] private CosmeticCatalog cosmetics;
        [SerializeField] private ShipAppearance appearance;
        private CosmeticSlot cosmeticSlot;
        private int selectedCosmetic;
        private CampaignLifecycle save;
        private RegionCampaignFlow region;
        private RunRewards rewards;
        private EncounterRunner runner;
        private readonly List<(Button button, UnityAction action)> bindings = new List<(Button, UnityAction)>();
        private Action confirmed, errorRetry;
        private ShellScreen settingsOrigin;
        private int selectedMission;
        private bool busy, initialized, previousPending, previousDirty;
        public ShellScreen Screen => view.Screen;
        public bool IsBusy => busy;
        public bool GameplayVisible => initialized && Screen == ShellScreen.Playing && !busy;
        public string LastOperationError { get; private set; }

        private void Awake()
        {
            save = GetComponent<CampaignLifecycle>();
            region = GetComponent<RegionCampaignFlow>();
            rewards = GetComponent<RunRewards>();
            runner = GetComponent<EncounterRunner>();
            session.RequireMissionResults = true;
            view.Show(ShellScreen.Playing);
            view.Confirmation.SetActive(false); view.Error.SetActive(false); view.Loading.SetActive(false);
        }
        private void OnEnable()
        {
            session.RequireMissionResults = true;
            runner.RunStarted += OnRunStarted;
            Bind(view.Continue, () => StartGame(save.Continue));
            Bind(view.ReloadSave, () => { save.ReloadStorage(); Navigate(ShellScreen.Menu); });
            Bind(view.RecoverSave, () => Confirm(ShellCopy.ConfirmRecovery, () => StartGame(save.RecoverNewGame)));
            Bind(view.NewGame, () => {
                if (save.PersistentSnapshot?.HasCampaign == true) Confirm(ShellCopy.ConfirmNew, () => StartGame(save.NewGame));
                else StartGame(save.NewGame);
            });
            Bind(view.Hangar, () => OpenCosmetics(false)); Bind(view.Store, () => OpenCosmetics(true));
            Bind(view.HangarMode, () => OpenCosmetics(false)); Bind(view.StoreMode, () => OpenCosmetics(true));
            for (int i = 0; i < view.CosmeticTabs.Length; i++) { int index = i; Bind(view.CosmeticTabs[i], () => { cosmeticSlot = (CosmeticSlot)index; SelectEquippedCosmetic(); RefreshHangar(); }); }
            for (int i = 0; i < view.CosmeticCards.Length; i++) { int index = i; Bind(view.CosmeticCards[i], () => { selectedCosmetic = index; RefreshHangar(); }); }
            Bind(view.CosmeticAction, EquipCosmetic);
            Bind(view.Missions, OpenMissions); Bind(view.Help, () => Navigate(ShellScreen.Help));
            Bind(view.Credits, () => Navigate(ShellScreen.Credits));
            Bind(view.Settings, () => OpenSettings(ShellScreen.Menu));
            foreach (var button in view.BackButtons) Bind(button, Back);
            for (int i = 0; i < view.MissionCards.Length; i++) { int index = i; Bind(view.MissionCards[i], () => SelectMission(index)); }
            Bind(view.LaunchReplay, () => StartGame(() => save.BeginReplay(region.Missions[selectedMission].Id)));
            Bind(view.SaveSettings, ApplySettings);
            view.Vibration.onValueChanged.AddListener(RefreshSettingStates);
            view.Shake.onValueChanged.AddListener(RefreshSettingStates);
            Bind(view.Resume, Resume); Bind(view.Retry, RequestRetry);
            Bind(view.PauseSettings, () => OpenSettings(ShellScreen.Pause));
            Bind(view.PauseMenu, () => Confirm(ShellCopy.ConfirmExit, () => ReturnToShell(ShellScreen.Menu)));
            Bind(view.ResultReplay, () => StartGame(() => save.BeginReplay(session.Flow.Level.Id)));
            Bind(view.ResultMissions, () => ReturnToShell(ShellScreen.Missions));
            Bind(view.ResultMenu, () => ReturnToShell(ShellScreen.Menu));
            Bind(view.ResultRetrySave, () => RunOperation(save.Flush, () => {
                if (save.Mode == SessionMode.Campaign && region.NextMission != null && session.ContinueAfterResult()) Navigate(ShellScreen.Playing);
                else RefreshResults();
            }, ShellCopy.SaveFailed));
            Bind(view.GameOverRetry, Retry);
            Bind(view.Confirm, () => { var action = confirmed; CancelConfirmation(); action?.Invoke(); });
            Bind(view.Cancel, CancelConfirmation);
            Bind(view.ErrorBack, CloseError);
            Bind(view.ErrorRetry, () => { var action = errorRetry; CloseError(); action?.Invoke(); });
        }
        private void OnRunStarted()
        {
            if (save.Mode == SessionMode.DevTest && !busy) { initialized = true; Navigate(ShellScreen.Playing); }
        }
        private void Bind(Button button, UnityAction action) { button.onClick.AddListener(action); bindings.Add((button, action)); }
        public void Boot()
        {
            initialized = true;
            session.HoldForShell();
            save.EnterFrontend();
            ApplySavedSettings();
            appearance.Apply(save.PersistentSnapshot?.Profile);
            Navigate(ShellScreen.Menu);
        }
        private void ApplySavedSettings() => feedback.Apply(save.PersistentSnapshot?.Settings ?? SettingsData.Defaults());
        private void Navigate(ShellScreen screen)
        {
            pointer.CancelGesture();
            if (screen == ShellScreen.Menu) RefreshMenu();
            if (screen == ShellScreen.Missions) RefreshMissions();
            if (screen == ShellScreen.Pause) RefreshPause();
            if (screen == ShellScreen.Results) RefreshResults();
            if (screen == ShellScreen.Hangar || screen == ShellScreen.Store) RefreshHangar(screen);
            view.Show(screen);
            if (view.Error.activeSelf) ShellView.Focus(view.ErrorRetry);
        }
        private void RefreshMenu()
        {
            var snapshot = save.PersistentSnapshot;
            view.Continue.interactable = snapshot?.HasCampaign == true && snapshot.Campaign.Status != CampaignStatus.MissionCleared;
            view.NewGame.interactable = snapshot != null;
            view.ReloadSave.gameObject.SetActive(snapshot == null);
            view.RecoverSave.gameObject.SetActive(snapshot == null && save.LoadStatus == SaveLoadStatus.Invalid);
            view.Wallet.gameObject.SetActive(snapshot != null);
            view.Store.interactable = snapshot != null;
            view.MenuShip.transform.parent.gameObject.SetActive(snapshot != null);
            view.MenuShipName.gameObject.SetActive(snapshot != null); view.MenuEquipped.gameObject.SetActive(snapshot != null);
            view.Hangar.gameObject.SetActive(snapshot != null);
            Preview(view.MenuShip, view.MenuTrail, snapshot?.Profile);
            view.MenuShipName.text = cosmetics.Equipped(snapshot?.Profile, CosmeticSlot.Chassis).DisplayName;
            view.MenuEquipped.text = ShellCopy.Equipped;
            var statusRect = view.MenuStatus.rectTransform;
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(.28f, snapshot == null ? .53f : .20f);
            view.Missions.interactable = snapshot != null;
            view.Settings.interactable = snapshot != null;
            view.Wallet.text = ShellCopy.Wallet(snapshot?.Profile.StellarShardBalance ?? 0);
            view.MenuStatus.text = snapshot == null ? (save.LoadStatus == SaveLoadStatus.UnsupportedVersion ? ShellCopy.UnsupportedSave : ShellCopy.ReadFailed) :
                !snapshot.HasCampaign ? ShellCopy.NoCampaign : snapshot.Campaign.Status == CampaignStatus.CampaignCompleted ? ShellCopy.ReleaseComplete :
                CampaignLabel(snapshot.Campaign.MissionId);
            view.MenuNotice.text = snapshot != null && save.LoadStatus == SaveLoadStatus.RecoveredBackup ? ShellCopy.Recovered : "";
            statusRect.sizeDelta = new Vector2(660, snapshot == null ? 150 : 70);
            view.ErrorRetry.gameObject.SetActive(true);
            if (snapshot == null) ShowError(view.MenuStatus.text, () => { save.ReloadStorage(); Navigate(ShellScreen.Menu); });
        }
        private string CampaignLabel(string missionId)
        {
            var missions = region.Missions;
            int index = Array.IndexOf(missions, region.FindMission(missionId));
            return (index < 4 ? "KHU VỰC A" : "KHU VỰC B") + " · " + ShellCopy.Mission(index);
        }
        private void Preview(Image ship, Image trail, PlayerProfile profile)
        {
            var chassis = cosmetics.Equipped(profile, CosmeticSlot.Chassis);
            ship.sprite = chassis.Sprite;
            ship.rectTransform.localScale = new Vector3(1, chassis.FlipY ? -1 : 1, 1);
            ship.color = cosmetics.Equipped(profile, CosmeticSlot.Skin).Tint;
            trail.color = cosmetics.Equipped(profile, CosmeticSlot.Trail).Tint;
        }
        private bool inStore;
        private void OpenCosmetics(bool store)
        {
            inStore = store;
            cosmeticSlot = CosmeticSlot.Chassis;
            SelectEquippedCosmetic();
            Navigate(store ? ShellScreen.Store : ShellScreen.Hangar);
        }
        private CosmeticOption[] SlotOptions() => Array.FindAll(cosmetics.Options, o => o.Slot == cosmeticSlot &&
            (inStore || cosmetics.Owns(save.PersistentSnapshot.Profile, o)));
        private void SelectEquippedCosmetic()
        {
            var options = SlotOptions();
            selectedCosmetic = Mathf.Max(0, Array.FindIndex(options, o => inStore ? !cosmetics.Owns(save.PersistentSnapshot.Profile, o) :
                cosmetics.Equipped(save.PersistentSnapshot.Profile, cosmeticSlot).Id == o.Id));
        }
        private void RefreshHangar() => RefreshHangar(Screen);
        private void RefreshHangar(ShellScreen screen)
        {
            inStore = screen == ShellScreen.Store;
            var profile = save.PersistentSnapshot.Profile;
            var options = SlotOptions();
            selectedCosmetic = Mathf.Clamp(selectedCosmetic, 0, options.Length - 1);
            var selected = options[selectedCosmetic];
            var preview = CampaignProgress.Copy(profile);
            preview.EquippedCosmetics = new[] { cosmetics.Equipped(profile, CosmeticSlot.Chassis).Id, cosmetics.Equipped(profile, CosmeticSlot.Skin).Id, cosmetics.Equipped(profile, CosmeticSlot.Trail).Id };
            preview.EquippedCosmetics[(int)cosmeticSlot] = selected.Id;
            preview.OwnedCosmetics = Array.ConvertAll(cosmetics.Options, o => o.Id);
            Preview(view.MenuShip, view.MenuTrail, preview);
            view.MenuShipName.text = cosmetics.Equipped(preview, CosmeticSlot.Chassis).DisplayName;
            bool owned = cosmetics.Owns(profile, selected), equipped = cosmetics.Equipped(profile, cosmeticSlot).Id == selected.Id;
            view.MenuEquipped.text = equipped ? ShellCopy.Equipped : "XEM TRƯỚC";
            view.MenuStatus.text = cosmeticSlot == CosmeticSlot.Chassis ? "Chọn tàu, rồi tùy chỉnh sơn và vệt động cơ." :
                ShellCopy.CosmeticTabs[(int)cosmeticSlot] + " · " + cosmetics.Equipped(profile, CosmeticSlot.Chassis).DisplayName;
            view.Wallet.text = ShellCopy.Wallet(profile.StellarShardBalance);
            view.HangarMode.interactable = inStore;
            view.StoreMode.interactable = !inStore;
            view.CosmeticContext.text = cosmeticSlot == CosmeticSlot.Chassis ? (inStore ? "KHÁM PHÁ TÀU" : "TÀU CỦA BẠN") :
                "NGOẠI HÌNH CỦA " + cosmetics.Equipped(profile, CosmeticSlot.Chassis).DisplayName;
            for (int i = 0; i < view.CosmeticTabs.Length; i++)
            {
                view.CosmeticTabs[i].interactable = i != (int)cosmeticSlot;
                view.CosmeticTabs[i].GetComponent<Outline>().enabled = i == (int)cosmeticSlot;
            }
            for (int i = 0; i < view.CosmeticCards.Length; i++)
            {
                view.CosmeticCards[i].gameObject.SetActive(i < options.Length);
                if (i >= options.Length) continue;
                var option = options[i];
                bool wearing = cosmetics.Equipped(profile, cosmeticSlot).Id == option.Id;
                view.CosmeticLabels[i].text = option.DisplayName + "\n" + (wearing ? "✓ " + ShellCopy.Equipped : cosmetics.Owns(profile, option) ? ShellCopy.Owned : option.ShardCost + " Stellar Shard");
                view.CosmeticCards[i].GetComponent<Outline>().enabled = i == selectedCosmetic;
                var swatch = view.CosmeticSwatches[i];
                swatch.sprite = cosmeticSlot == CosmeticSlot.Chassis ? option.Sprite : cosmetics.Equipped(profile, CosmeticSlot.Chassis).Sprite;
                swatch.rectTransform.localScale = new Vector3(1, (cosmeticSlot == CosmeticSlot.Chassis ? option.FlipY : cosmetics.Equipped(profile, CosmeticSlot.Chassis).FlipY) ? -1 : 1, 1);
                swatch.color = cosmeticSlot == CosmeticSlot.Chassis ? cosmetics.Equipped(profile, CosmeticSlot.Skin).Tint : option.Tint.a == 0 ? Color.white : option.Tint;
            }
            view.CosmeticDetail.text = selected.DisplayName + "\n" + (inStore ? "Mua và trang bị bằng Stellar Shard." : "Đã sở hữu · thay đổi miễn phí.") + " Chỉ thay đổi ngoại hình.";
            view.CosmeticAction.GetComponentInChildren<Text>().text = inStore ? (owned ? ShellCopy.Owned : profile.StellarShardBalance >= selected.ShardCost ? ShellCopy.Purchase(selected.ShardCost) : ShellCopy.NeedShards(selected.ShardCost - profile.StellarShardBalance)) : equipped ? ShellCopy.Equipped : ShellCopy.Equip;
            view.CosmeticAction.interactable = inStore ? !owned && profile.StellarShardBalance >= selected.ShardCost : !equipped;
        }
        private void EquipCosmetic()
        {
            var selected = SlotOptions()[selectedCosmetic];
            void Commit()
            {
                if (!save.EquipCosmetic(cosmetics, selected.Id)) { ShowError(ShellCopy.CosmeticFailed, Commit); return; }
                appearance.Apply(save.PersistentSnapshot.Profile);
                RefreshHangar();
            }
            if (cosmetics.Owns(save.PersistentSnapshot.Profile, selected)) Commit();
            else Confirm(ShellCopy.ConfirmPurchase(selected.DisplayName, selected.ShardCost), Commit);
        }
        private void OpenMissions()
        {
            var c = save.PersistentSnapshot;
            var index = c?.HasCampaign == true ? Array.IndexOf(region.Missions, region.FindMission(c.Campaign.MissionId)) : 0;
            selectedMission = Mathf.Max(0, index);
            Navigate(ShellScreen.Missions);
        }
        private void SelectMission(int index) { selectedMission = index; RefreshMissions(); }
        private void RefreshMissions()
        {
            var data = save.PersistentSnapshot;
            var missions = region.Missions;
            for (int i = 0; i < missions.Length; i++)
            {
                var mission = missions[i];
                bool unlocked = data != null && Array.IndexOf(data.Profile.UnlockedMissions, mission.Id) >= 0;
                var record = data != null ? Array.Find(data.Profile.Records, r => r.MissionId == mission.Id) : null;
                bool current = data?.HasCampaign == true && data.Campaign.Status == CampaignStatus.Active && data.Campaign.MissionId == mission.Id;
                view.MissionCards[i].interactable = unlocked;
                view.MissionCards[i].GetComponent<Outline>().enabled = i == selectedMission;
                view.MissionLabels[i].text = ShellCopy.Mission(i) + "\n" + ShellCopy.MissionState(unlocked, current, record != null) +
                    (record == null ? "" : $"\n{record.BestScore:N0} · {ShellCopy.Time(record.BestTimeSeconds)}");
            }
            var selected = missions[selectedMission];
            var best = data != null ? Array.Find(data.Profile.Records, r => r.MissionId == selected.Id) : null;
            view.MissionDetail.text = data?.Profile.UnlockedMissions.Length == 0 ? ShellCopy.EmptyHub : ShellCopy.Mission(selectedMission) + "  ·  " + ShellCopy.Best(best);
            view.LaunchReplay.interactable = data != null && Array.IndexOf(data.Profile.UnlockedMissions, selected.Id) >= 0;
        }
        private void OpenSettings(ShellScreen origin)
        {
            settingsOrigin = origin;
            var settings = save.PersistentSnapshot?.Settings ?? SettingsData.Defaults();
            view.Music.SetValueWithoutNotify(settings.MusicVolume);
            view.Sfx.SetValueWithoutNotify(settings.SfxVolume);
            view.Vibration.SetIsOnWithoutNotify(settings.VibrationEnabled);
            view.Shake.SetIsOnWithoutNotify(settings.ScreenShakeEnabled);
            RefreshSettingStates(false);
            Navigate(ShellScreen.Settings);
        }
        private void RefreshSettingStates(bool _)
        {
            view.VibrationState.text = view.Vibration.isOn ? "BẬT" : "TẮT";
            view.ShakeState.text = view.Shake.isOn ? "BẬT" : "TẮT";
        }
        private void ApplySettings()
        {
            var settings = new SettingsData { MusicVolume = view.Music.value, SfxVolume = view.Sfx.value,
                VibrationEnabled = view.Vibration.isOn, ScreenShakeEnabled = view.Shake.isOn };
            if (!save.SetSettings(settings)) { ShowError(ShellCopy.SettingsFailed, ApplySettings); return; }
            ApplySavedSettings();
            Navigate(settingsOrigin);
        }
        private void RefreshPause()
        {
            view.Retry.interactable = session.CanRetry;
            var loadout = pointer.GetComponent<WeaponLoadout>();
            view.PauseDetail.text = save.HasPendingRegionTransition || save.Dirty && !string.IsNullOrEmpty(save.Diagnostic) ? ShellCopy.SaveFailed :
                $"{ShellCopy.Mission(Array.IndexOf(region.Missions, session.Flow.Level))} · {GameplayCopy.Encounter(runner.CurrentIndex + 1)}\n{GameplayCopy.WeaponName(loadout.ActiveBranch)} · CẤP {loadout.Level(loadout.ActiveBranch)}";
        }
        private void Resume()
        {
            if (!save.Flush()) { ShowError(ShellCopy.SaveFailed, Resume); return; }
            session.Resume();
            Navigate(ShellScreen.Playing);
        }
        private void RequestRetry() => Confirm(ShellCopy.ConfirmRetry, Retry);
        private void Retry() => RunOperation(save.RetryEncounter, () => Navigate(ShellScreen.Playing), ShellCopy.SaveFailed);
        private void RefreshResults()
        {
            bool clear = session.IsClear, replay = save.Mode == SessionMode.Replay;
            bool victory = clear && !replay && region.NextMission == null;
            view.ResultTitle.text = !clear ? GameplayCopy.GameOver : victory ? ShellCopy.Victory : GameplayCopy.MissionClear;
            view.ResultMission.text = ShellCopy.Mission(Array.IndexOf(region.Missions, session.Flow.Level)) + (replay ? " · CHƠI LẠI" : "") + (victory ? "\n" + ShellCopy.ReleaseComplete : "");
            view.ResultStats.text = ShellCopy.Stats(rewards.Capture());
            var record = Array.Find(save.PersistentSnapshot?.Profile.Records ?? Array.Empty<MissionRecord>(), r => r.MissionId == session.Flow.Level.Id);
            bool committed = !save.HasPendingClear && !save.Dirty;
            view.ResultRecord.text = ShellCopy.Best(record) + (clear && !replay && committed && (save.NewBestScore || save.NewBestTime) ? "\nKỶ LỤC MỚI" : "");
            view.ResultStatus.text = save.HasPendingClear ? ShellCopy.SaveFailed : replay ? "Kết quả chơi lại không thay đổi chiến dịch hoặc ví Shard." : "";
            view.Next.gameObject.SetActive(false);
            view.ResultReplay.gameObject.SetActive(clear && replay);
            view.ResultMissions.gameObject.SetActive(clear && (replay || victory));
            view.ResultRetrySave.gameObject.SetActive(save.HasPendingClear);
            view.GameOverRetry.gameObject.SetActive(!clear);
            view.GameOverRetry.interactable = session.CanRetry;
            previousPending = save.HasPendingClear; previousDirty = save.Dirty;
        }
        private void ReturnToShell(ShellScreen destination)
        {
            RunOperation(() => { save.ExitIsolated(); session.HoldForShell(); save.EnterFrontend(); return true; }, () => Navigate(destination), ShellCopy.SaveFailed);
        }
        private void StartGame(Func<bool> action) => RunOperation(action, () => { initialized = true; appearance.Apply(save.PersistentSnapshot?.Profile); ApplySavedSettings(); Navigate(session.IsTerminal ? ShellScreen.Results : ShellScreen.Playing); }, ShellCopy.StartFailed, true);
        private void RunOperation(Func<bool> action, Action success, string failure, bool returnOnFailure = false)
        {
            if (!busy) StartCoroutine(Transition(action, success, failure, returnOnFailure));
        }
        private IEnumerator Transition(Func<bool> action, Action success, string failure, bool returnOnFailure)
        {
            busy = true;
            session.HoldForTransition();
            pointer.CancelGesture();
            view.Loading.SetActive(true); ShellView.Focus(null);
            for (float t = 0; t < .16f; t += Time.unscaledDeltaTime) { view.Fade.alpha = t / .16f; yield return null; }
            view.Fade.alpha = 1;
            yield return null;
            bool ok = false;
            try { ok = action(); }
            catch (Exception e) { LastOperationError = e.ToString(); }
            if (ok) { LastOperationError = null; success(); }
            else
            {
                if (returnOnFailure) { session.HoldForShell(); Navigate(ShellScreen.Menu); }
                ShowError(failure, () => RunOperation(action, success, failure, returnOnFailure));
            }
            // Giữ blocker qua frame nhận release trước khi giao input cho màn hình đích.
            yield return null;
            for (float t = .16f; t > 0; t -= Time.unscaledDeltaTime) { view.Fade.alpha = t / .16f; yield return null; }
            view.Loading.SetActive(false);
            pointer.CancelGesture();
            busy = false;
            session.ReleaseTransition();
        }
        private void Confirm(string copy, Action action)
        {
            pointer.CancelGesture(); confirmed = action; view.ShowConfirmation(copy);
        }
        private void CancelConfirmation() { confirmed = null; view.CloseConfirmation(); pointer.CancelGesture(); }
        private void ShowError(string copy, Action retry)
        {
            errorRetry = retry; view.ErrorText.text = copy; view.Error.SetActive(true);
            view.ErrorRetry.gameObject.SetActive(retry != null); ShellView.Focus(view.ErrorRetry);
        }
        private void CloseError() { view.Error.SetActive(false); pointer.CancelGesture(); view.Show(view.Screen); }
        private void Back()
        {
            if (busy) return;
            if (view.Error.activeSelf) { CloseError(); return; }
            if (view.Confirmation.activeSelf) { CancelConfirmation(); return; }
            if (Screen == ShellScreen.Playing) { session.Pause(); return; }
            if (Screen == ShellScreen.Pause) { Resume(); return; }
            if (Screen == ShellScreen.Results) { ReturnToShell(save.Mode == SessionMode.Replay ? ShellScreen.Missions : ShellScreen.Menu); return; }
            Navigate(Screen == ShellScreen.Settings ? settingsOrigin : ShellScreen.Menu);
        }
        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) Back();
        }
        private void LateUpdate()
        {
            if (busy) return;
            if (!initialized && session.HasActiveRun) initialized = true;
            if (!initialized) return;
            if (session.IsTerminal && (Screen == ShellScreen.Playing || Screen == ShellScreen.Pause)) Navigate(ShellScreen.Results);
            else if (session.IsPaused && Screen == ShellScreen.Playing) Navigate(ShellScreen.Pause);
            if (Screen == ShellScreen.Results && (previousPending != save.HasPendingClear || previousDirty != save.Dirty)) RefreshResults();
        }
        private void OnDisable()
        {
            view.Vibration.onValueChanged.RemoveListener(RefreshSettingStates);
            view.Shake.onValueChanged.RemoveListener(RefreshSettingStates);
            session.ReleaseTransition();
            runner.RunStarted -= OnRunStarted;
            foreach (var binding in bindings) binding.button.onClick.RemoveListener(binding.action);
            bindings.Clear();
            session.RequireMissionResults = false;
            pointer.CancelGesture();
        }
    }
}
