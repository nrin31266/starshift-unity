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
        [SerializeField, Tooltip("Bật để dùng Start Level/Start Point bên dưới; chạy isolated, không ghi CampaignSave.")] private bool devModeEnabled;
        [SerializeField] private string startPoint = "LEVEL_START";
        [SerializeField] private bool standalone;

        [SerializeField] private LevelDefinition startLevel;
        [SerializeField, Tooltip("Bỏ intro của Start Level khi Dev Mode Enabled bật. Chạy thường: sửa Intro/Enabled trên LevelFlow/Level thực tế.")] private bool skipIntro;
        [SerializeField] private bool overrideDirection;
        [SerializeField] private PlayDirection direction;
        [SerializeField] private CameraProfile cameraOverride;

        [SerializeField] private bool deterministicRewards;
        [SerializeField] private int rewardSeed = 410;
        [SerializeField, Range(1, WeaponLoadout.MaximumLevel)] private int startingWeaponLevel = 1;
        [SerializeField] private bool unlockAllWeapons;

        private bool pendingBossPhaseB;
        private bool pendingVictory;
        private int pendingGeneration;
        private void FixedUpdate()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!pendingBossPhaseB && !pendingVictory) return;
            if (!GetComponent<EncounterRunner>().HasActiveRun || GetComponent<EncounterRunner>().Generation != pendingGeneration || GetComponent<StarShift.Persistence.CampaignLifecycle>().Mode != StarShift.Persistence.SessionMode.DevTest)
            { pendingBossPhaseB = pendingVictory = false; return; }
            foreach (var enemy in StarShift.Enemies.EnemyHealth.Active)
            {
                var boss = enemy.GetComponent<StarShift.Enemies.BossPhaseRunner>();
                if (boss == null) continue;
                if (boss.Shields != null && !boss.Shields.IsExposed)
                {
                    foreach (var node in enemy.GetComponentsInChildren<StarShift.Enemies.EnemyHealth>())
                        if (node != enemy && node.CanReceiveDamage) node.ReceivePlayerHit(node.CurrentHp, gameObject);
                    break;
                }
                if (!enemy.CanReceiveDamage) continue;
                if (pendingVictory) { pendingVictory = pendingBossPhaseB = false; enemy.ReceivePlayerHit(enemy.CurrentHp, gameObject); }
                else { pendingBossPhaseB = false; if (!boss.IsPhaseB && boss.Shields == null) enemy.ReceivePlayerHit(Mathf.CeilToInt(enemy.MaxHp * .51f), gameObject); }
                break;
            }
#endif
        }
        public bool TryStart()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!isActiveAndEnabled || !devModeEnabled) return false;
            return ReplayConfiguredStart();
#else
            return false;
#endif
        }

        [ContextMenu("Replay Configured Start")]
        public void Replay() => ReplayConfiguredStart();

        private bool ReplayConfiguredStart()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying || !isActiveAndEnabled || !devModeEnabled) return false;
            var runner = GetComponent<EncounterRunner>();
            var level = startLevel != null ? startLevel : runner.Flow.Level;
            var transition = startPoint == "TRANSITION";
            var victory = startPoint == "VICTORY";
            var route = transition ? "LEVEL_START" : victory ? "BOSS" : startPoint;
            var region = GetComponent<RegionCampaignFlow>();
            var missions = region != null ? region.Missions : System.Array.Empty<LevelDefinition>();
            int missionIndex = System.Array.IndexOf(missions, level);
            if (level == null || level.ResolveStart(route) < 0 || transition && (missionIndex < 0 || missionIndex + 1 >= missions.Length))
            {
                Debug.LogWarning("Dev Start Point không hợp lệ; chọn lại route trong Inspector.", this);
                return false;
            }
            runner.StopRun();
            foreach (var projectile in FindObjectsByType<PlayerProjectile>())
                if (projectile.gameObject.scene == gameObject.scene) projectile.Release();
            var player = FindAnyObjectByType<PlayerDamageLifecycle>();
            player.enabled = false;
            player.enabled = true;
            player.GetComponent<PlayerPointerInput>().ResetDrag();
            if (startLevel != null) runner.Flow.LoadLevel(startLevel);
            pendingVictory = victory;
            pendingBossPhaseB = startPoint == "BOSS_PHASE_B";
            runner.StartRun(route, pendingVictory ? false : standalone, skipIntro, overrideDirection ? direction : (PlayDirection?)null, cameraOverride);
            pendingGeneration = runner.Generation;
            if (transition) { runner.StopRun(); GetComponent<RegionCampaignFlow>().QueueAdvance(); }
            if (deterministicRewards) GetComponent<StarShift.Rewards.RewardDirector>()?.SetTestSeed(rewardSeed);
            var loadout = player.GetComponent<WeaponLoadout>();
            if (loadout != null)
                for (int i = 0; i < 3; i++)
                {
                    var branch = (WeaponBranch)i;
                    if (i > 0 && !unlockAllWeapons) continue;
                    while (loadout.Level(branch) < Mathf.Clamp(startingWeaponLevel, 1, WeaponLoadout.MaximumLevel))
                        if (!loadout.ApplyCore(branch)) break;
                }
            return true;
#else
            return false;
#endif
        }
    }
}
