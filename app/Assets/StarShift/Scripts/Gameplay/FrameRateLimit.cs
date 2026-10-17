using UnityEngine;

namespace StarShift.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FrameRateLimit : MonoBehaviour
    {
        [SerializeField, Min(1)] private int targetFrameRate = 60;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.Max(1, targetFrameRate);
        }
    }
}
