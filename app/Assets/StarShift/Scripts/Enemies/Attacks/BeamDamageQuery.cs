using StarShift.Player;
using UnityEngine;

namespace StarShift.Enemies
{
    internal sealed class BeamDamageQuery
    {
        private readonly Rigidbody2D player;
        private readonly ContactFilter2D filter;
        private readonly Collider2D[] overlaps = new Collider2D[8];
        private readonly RaycastHit2D[] crossings = new RaycastHit2D[8];
        private Vector2 previousCenter, previousPlayer;
        private bool continuous;

        public BeamDamageQuery(PlayerGameplayGate target)
        {
            player = target.GetComponent<Rigidbody2D>();
            filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(1 << target.GetComponentInChildren<Collider2D>().gameObject.layer);
        }

        public void Reset() => continuous = false;

        public void Apply(Vector2 center, Vector2 size, float angle, GameObject owner)
        {
            int count = Physics2D.OverlapBox(center, size, angle, filter, overlaps);
            for (int i = 0; i < count; i++) overlaps[i].GetComponentInParent<PlayerContactReceiver>()?.ReceiveEnemyHit(owner);
            var position = (Vector2)player.position;
            if (continuous)
            {
                // Quy về vị trí player hiện tại để bắt cả tia quét và drag vượt tia trong một bước.
                var from = previousCenter + position - previousPlayer;
                var delta = center - from;
                if (delta.sqrMagnitude > .000001f)
                {
                    count = Physics2D.BoxCast(from, size, angle, delta.normalized, filter, crossings, delta.magnitude);
                    for (int i = 0; i < count; i++) crossings[i].collider.GetComponentInParent<PlayerContactReceiver>()?.ReceiveEnemyHit(owner);
                }
            }
            previousCenter = center; previousPlayer = position; continuous = true;
        }
    }
}
