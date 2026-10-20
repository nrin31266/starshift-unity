using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerGameplayGate))]
    public sealed class PlayerDeathPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer explosion;
        [SerializeField] private Sprite[] explosionFrames;
        [SerializeField] private SpriteRenderer[] fragments;
        [SerializeField, Min(.1f)] private float seconds = .6f;
        private PlayerGameplayGate gate;
        private PlayerGameplayState previousState;
        private Vector3 origin;
        private Quaternion rotation;
        private float elapsed;
        private bool playing;

        private void Awake() => gate = GetComponent<PlayerGameplayGate>();
        private void OnEnable()
        {
            previousState = gate.State;
            gate.StateChanged += OnStateChanged;
            Clear();
        }
        private void OnStateChanged()
        {
            if (gate.State == PlayerGameplayState.Dying && previousState != PlayerGameplayState.Dying)
            {
                origin = transform.position;
                rotation = transform.rotation;
                elapsed = 0;
                playing = true;
            }
            previousState = gate.State;
        }
        private void LateUpdate()
        {
            if (!playing) return;
            elapsed += Time.deltaTime;
            if (elapsed >= seconds) { Clear(); return; }
            float t = elapsed / seconds;
            if (explosion != null && explosionFrames.Length > 0)
            {
                explosion.enabled = true;
                explosion.transform.position = origin;
                explosion.sprite = explosionFrames[Mathf.Min(explosionFrames.Length - 1, (int)(t * explosionFrames.Length))];
            }
            for (int i = 0; fragments != null && i < fragments.Length; i++)
            {
                var fragment = fragments[i];
                if (fragment == null) continue;
                float angle = (45 + i * 360f / fragments.Length) * Mathf.Deg2Rad;
                var direction = rotation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                fragment.enabled = true;
                // Tọa độ world giữ mảnh vỡ tại chỗ chết khi root bắt đầu respawn.
                fragment.transform.position = origin + direction * (.12f + t * .8f);
                fragment.transform.rotation = rotation * Quaternion.Euler(0, 0, i * 90 + t * (i % 2 == 0 ? 160 : -160));
                fragment.color = new Color(1, 1, 1, 1 - t);
            }
        }
        private void Clear()
        {
            playing = false;
            if (explosion != null) explosion.enabled = false;
            if (fragments != null)
                foreach (var fragment in fragments) if (fragment != null) fragment.enabled = false;
        }
        private void OnDisable()
        {
            if (gate != null) gate.StateChanged -= OnStateChanged;
            Clear();
        }
    }
}
