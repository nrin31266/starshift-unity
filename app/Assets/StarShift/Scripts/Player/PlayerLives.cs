using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("StarShift/Player/Player Lives")]
    public sealed class PlayerLives : MonoBehaviour
    {
        public int CurrentLives { get; private set; } = 3;
        [SerializeField, Min(3)] private int maximumLives = 5;
        public int MaximumLives => maximumLives;
        public bool TryAddLife()
        {
            if (CurrentLives <= 0 || CurrentLives >= maximumLives) return false;
            CurrentLives++; return true;
        }
        internal void ResetForSession() => CurrentLives = 3;
        internal void LoseLife() => CurrentLives = Mathf.Max(0, CurrentLives - 1);
    }
}
