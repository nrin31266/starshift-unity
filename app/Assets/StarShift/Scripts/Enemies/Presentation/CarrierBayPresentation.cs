using UnityEngine;

namespace StarShift.Enemies
{
    // Carrier launch PREPARE → LOCK → ATTACK → RECOVERY: cửa bay mở khi spawn thật.
    // Không dùng animation làm điều kiện spawn; chỉ đọc SpawnCount/interval có sẵn.
    [DisallowMultipleComponent, RequireComponent(typeof(EscortSpawner))]
    [AddComponentMenu("StarShift/Enemies/Carrier Bay Presentation")]
    public sealed class CarrierBayPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer leftBay, rightBay;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(.05f)] private float openSeconds = .8f;
        [SerializeField, Tooltip("Cửa sáng dần khi sắp phóng (PREPARE) thay vì đứng im.")]
        private float prepareLeadSeconds = 1.2f;
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
                previousCount = spawner.SpawnCount;
                remaining = openSeconds;
                LaunchCues++;
            }
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            if (frames == null || frames.Length == 0) return;
            float open;
            if (remaining > 0) open = Mathf.Sin((1 - remaining / openSeconds) * Mathf.PI);
            // PREPARE: cửa hé dần theo thời gian còn lại tới lần phóng kế tiếp.
            else if (spawner.ReadyToLaunch && spawner.LaunchRemaining <= prepareLeadSeconds)
                open = .18f * Mathf.Clamp01(1 - spawner.LaunchRemaining / Mathf.Max(.01f, prepareLeadSeconds));
            else open = 0;
            var frame = frames[Mathf.Min(frames.Length - 1, (int)(open * frames.Length))];
            leftBay.sprite = rightBay.sprite = frame;
        }

        private void OnDisable()
        {
            remaining = 0;
            if (frames != null && frames.Length > 0) leftBay.sprite = rightBay.sprite = frames[0];
        }
    }
}
