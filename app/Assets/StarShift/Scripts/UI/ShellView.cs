using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarShift.UI
{
    public enum ShellScreen { Playing, Menu, Missions, Settings, Help, Credits, Pause, Results, Hangar, Store }

    [DisallowMultipleComponent]
    public sealed class ShellView : MonoBehaviour
    {
        public GameObject[] Screens;
        public Button Continue, NewGame, Missions, Help, Settings, Credits, ReloadSave, RecoverSave;
        public Button[] BackButtons, MissionCards;
        public Text[] MissionLabels;
        public Button LaunchReplay;
        public Button Hangar, Store, HangarMode, StoreMode, CosmeticAction;
        public GameObject ShipStage;
        public Image MenuShip, MenuTrail;
        public Image[] CosmeticSwatches;
        public Text MenuShipName, MenuEquipped, CosmeticContext, CosmeticDetail;
        public Text VibrationState, ShakeState;
        public Button[] CosmeticTabs, CosmeticCards;
        public Text[] CosmeticLabels;
        public Text MenuStatus, Wallet, MissionDetail, MenuNotice;
        public Slider Music, Sfx;
        public Toggle Vibration, Shake;
        public Button SaveSettings, ResetAll;
        public Image WalletIcon;
        public Button Resume, Retry, PauseSettings, PauseMenu;
        public Text PauseDetail;
        public Text ResultTitle, ResultMission, ResultStats, ResultRecord, ResultStatus;
        public Button Next, ResultReplay, ResultMissions, ResultMenu, ResultRetrySave, GameOverRetry;
        public GameObject Confirmation, Error, Loading;
        public Text ConfirmationText, ErrorText;
        public Button Confirm, Cancel, ErrorRetry, ErrorBack;
        public CanvasGroup Fade;
        public ShellScreen Screen { get; private set; }
        private Button previousSelection;

        public void Show(ShellScreen screen)
        {
            Screen = screen;
            for (int i = 1; i < Screens.Length; i++) Screens[i].SetActive(i == (int)screen || i == (int)ShellScreen.Hangar && screen == ShellScreen.Store);
            ShipStage.SetActive(screen == ShellScreen.Menu || screen == ShellScreen.Hangar || screen == ShellScreen.Store);
            Focus(screen == ShellScreen.Menu ? (Continue.interactable ? Continue : NewGame.interactable ? NewGame : ReloadSave) :
                screen == ShellScreen.Pause ? Resume : screen == ShellScreen.Missions ? (MissionCards[0].interactable ? MissionCards[0] : BackButtons[0]) :
                (screen == ShellScreen.Hangar || screen == ShellScreen.Store) ? CosmeticCards[0] : screen == ShellScreen.Settings ? SaveSettings : screen == ShellScreen.Results ? ResultMenu : null);
        }
        public void ShowConfirmation(string copy)
        {
            previousSelection = EventSystem.current?.currentSelectedGameObject?.GetComponent<Button>();
            ConfirmationText.text = copy;
            Confirmation.SetActive(true);
            Focus(Cancel);
        }
        public void CloseConfirmation()
        {
            Confirmation.SetActive(false);
            Focus(previousSelection);
        }
        public static void Focus(Selectable control)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(control != null && control.IsInteractable() ? control.gameObject : null);
        }
    }
}
