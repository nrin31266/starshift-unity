using System.Collections.Generic;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-80)]
    [AddComponentMenu("StarShift/Player/Player Respawn Entrance")]
    public sealed class PlayerRespawnEntrance : MonoBehaviour
    {
        [SerializeField, Min(.02f)] private float duration = .9f;
        [SerializeField] private CircleCollider2D collisionShape;
        [SerializeField] private LayerMask blockingLayers;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private static readonly Vector2[] SafeAnchors = {
            new Vector2(.5f, .18f), new Vector2(.25f, .18f), new Vector2(.75f, .18f),
            new Vector2(.5f, .35f), new Vector2(.25f, .35f), new Vector2(.75f, .35f),
            new Vector2(.5f, .5f), new Vector2(.25f, .5f), new Vector2(.75f, .5f) };
        private float elapsed;
        private int anchor;
        private bool entering;
        public bool IsEntering => isActiveAndEnabled && entering;
        public bool IsComplete => IsEntering && elapsed >= duration && SafeTargetFound;
        public bool SafeTargetFound { get; private set; }

        private void Awake()
        {
            if (collisionShape != null && blockingLayers.value != 0) return;
            Debug.LogError("PlayerRespawnEntrance requires a collision shape and blocking layers.", this);
            enabled = false;
        }

        public void Begin()
        {
            elapsed = 0;
            anchor = 0;
            SafeTargetFound = false;
            entering = true;
        }

        public void Cancel()
        {
            entering = false;
            elapsed = 0;
            SafeTargetFound = false;
        }

        private void FixedUpdate()
        {
            if (entering) elapsed = Mathf.Min(duration, elapsed + Time.fixedDeltaTime);
        }

        public Vector2 GetPosition(Camera camera, PlayerMovementBounds bounds, Rect region)
        {
            var depth = Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward);
            Vector2 Target(int index)
            {
                var normalized = region.min + Vector2.Scale(region.size, SafeAnchors[index]);
                return bounds.ClampPosition(camera.ViewportToWorldPoint(new Vector3(normalized.x, normalized.y, depth)), camera, region);
            }
            bool Clear(Vector2 position)
            {
                var offset = (Vector2)(collisionShape.bounds.center - transform.position);
                var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = blockingLayers };
                return Physics2D.OverlapBox(position + offset, collisionShape.bounds.size, 0, filter, overlaps) == 0;
            }
            SafeTargetFound = Clear(Target(anchor));
            if (!SafeTargetFound)
                for (int i = 0; i < SafeAnchors.Length; i++)
                    if (Clear(Target(i))) { anchor = i; SafeTargetFound = true; break; }
            var destination = Target(anchor);
            var destinationViewport = camera.WorldToViewportPoint(new Vector3(destination.x, destination.y, transform.position.z));
            var entry = camera.ViewportToWorldPoint(new Vector3(destinationViewport.x, -.12f, depth));
            return Vector2.Lerp(entry, destination, Mathf.SmoothStep(0, 1, elapsed / duration));
        }

        private void OnDisable() => Cancel();
        private void OnValidate() => duration = Mathf.Max(.02f, duration);
    }
}
