using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Player;
using UnityEngine;

namespace StarShift.Dev
{
    [DisallowMultipleComponent, RequireComponent(typeof(EncounterRunner))]
    public sealed class DevFastStart : MonoBehaviour
    {
        [SerializeField] private bool devModeEnabled;
        [SerializeField] private string startPoint = "LEVEL_START";
        [SerializeField] private bool standalone;

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
            runner.StartRun(startPoint, standalone);
#endif
        }
    }
}
