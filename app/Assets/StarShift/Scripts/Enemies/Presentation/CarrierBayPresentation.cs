using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent, RequireComponent(typeof(EscortSpawner))]
    public sealed class CarrierBayPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer leftBay, rightBay;
        [SerializeField] private Sprite[] frames;
        private EscortSpawner spawner;
        private int previousCount;
        private float remaining;
        public int LaunchCues { get; private set; }
        private void Awake() => spawner = GetComponent<EscortSpawner>();
        private void OnEnable() { previousCount = 0; remaining = 0; LaunchCues = 0; }
        private void LateUpdate()
        {
            if (spawner.SpawnCount != previousCount)
            {
                previousCount = spawner.SpawnCount; remaining = .8f; LaunchCues++;
            }
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            if (frames.Length == 0) return;
            var t = 1 - remaining / .8f;
            var open = remaining > 0 ? Mathf.Sin(t * Mathf.PI) : 0;
            leftBay.sprite = rightBay.sprite = frames[Mathf.Min(frames.Length - 1, (int)(open * frames.Length))];
        }
        private void OnDisable()
        {
            remaining = 0;
            if (frames != null && frames.Length > 0) leftBay.sprite = rightBay.sprite = frames[0];
        }
    }
}
