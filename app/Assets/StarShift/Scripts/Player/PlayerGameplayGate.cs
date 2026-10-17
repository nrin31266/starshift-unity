using System;
using UnityEngine;

namespace StarShift.Player
{
    public enum PlayerGameplayState
    {
        Entry,
        Active,
        Dying,
        Respawning,
        PostEntryInvincibility,
        GameOver,
        Transition,
        Result
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("StarShift/Player/Player Gameplay Gate")]
    public sealed class PlayerGameplayGate : MonoBehaviour
    {
        [SerializeField, Tooltip("State prototype hiện tại; không tự chạy death/respawn sequence.")]
        private PlayerGameplayState state = PlayerGameplayState.Active;

        private bool cinematicLocked;
        public PlayerGameplayState State => state;
        public bool CanInput => IsRunning && (state == PlayerGameplayState.Active || state == PlayerGameplayState.PostEntryInvincibility);
        public bool CanFire => CanInput;
        public bool CanTakeDamage => IsRunning && state == PlayerGameplayState.Active;
        private bool IsRunning => isActiveAndEnabled && !cinematicLocked && Time.timeScale > 0f;

        public event Action StateChanged;

        public void SetCinematicLock(bool locked)
        {
            if (cinematicLocked == locked) return;
            cinematicLocked = locked;
            StateChanged?.Invoke();
        }

        public void SetState(PlayerGameplayState nextState)
        {
            if (state == nextState) return;
            state = nextState;
            StateChanged?.Invoke();
        }

        private void OnEnable() => StateChanged?.Invoke();
        private void OnDisable() => StateChanged?.Invoke();
    }
}
