using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Player;
using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.Dev
{
    [DisallowMultipleComponent, RequireComponent(typeof(EncounterRunner))]
    public sealed class DevFastStart : MonoBehaviour
    {
        [SerializeField] private bool devModeEnabled;
        [SerializeField] private string startPoint = "LEVEL_START";
        [SerializeField] private bool standalone;

        [SerializeField] private LevelDefinition startLevel;
        [SerializeField] private bool skipIntro;
        [SerializeField] private bool overrideDirection;
        [SerializeField] private PlayDirection direction;
        [SerializeField] private CameraProfile cameraOverride;

        [SerializeField] private bool deterministicRewards;
        [SerializeField] private int rewardSeed = 410;
        [SerializeField, Range(1, 3)] private int startingWeaponLevel = 1;
        [SerializeField] private bool unlockAllWeapons;

        public bool TryStart()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!isActiveAndEnabled || !devModeEnabled) return false;
            Replay();
            return true;
#else
            return false;
#endif
        }

        [ContextMenu("Replay Configured Start")]
        public void Replay()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying || !isActiveAndEnabled || !devModeEnabled) return;
            var runner = GetComponent<EncounterRunner>();
            runner.StopRun();
            foreach (var projectile in FindObjectsByType<PlayerProjectile>())
                if (projectile.gameObject.scene == gameObject.scene) projectile.Release();
            var player = FindAnyObjectByType<PlayerDamageLifecycle>();
            player.enabled = false;
            player.enabled = true;
            player.GetComponent<PlayerPointerInput>().ResetDrag();
            if (startLevel != null) runner.Flow.LoadLevel(startLevel);
            runner.StartRun(startPoint, standalone, skipIntro, overrideDirection ? direction : (PlayDirection?)null, cameraOverride);
            if (deterministicRewards) GetComponent<StarShift.Rewards.RewardDirector>()?.SetTestSeed(rewardSeed);
            var loadout = player.GetComponent<WeaponLoadout>();
            if (loadout != null)
                for (int i = 0; i < 3; i++)
                {
                    var branch = (WeaponBranch)i;
                    if (i > 0 && !unlockAllWeapons) continue;
                    while (loadout.Level(branch) < startingWeaponLevel) loadout.ApplyCore(branch);
                }
#endif
        }
    }
}
