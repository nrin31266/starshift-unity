using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("StarShift/Player/Player Lives")]
    public sealed class PlayerLives : MonoBehaviour
    {
        public int CurrentLives { get; private set; } = 3;
        internal void ResetForSession() => CurrentLives = 3;
        internal void LoseLife() => CurrentLives = Mathf.Max(0, CurrentLives - 1);
    }
}
