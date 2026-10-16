using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("StarShift/Player/Player Movement Bounds")]
    public sealed class PlayerMovementBounds : MonoBehaviour
    {
        [SerializeField, Tooltip("Envelope gameplay quanh root, đơn vị local. Bao đủ thân tàu; độc lập artwork và damage hitbox.")]
        private Vector2 footprintSize = new Vector2(.8f, 1f);

        public Vector2 ClampPosition(Vector2 proposedPosition, Camera gameplayCamera)
            => ClampPosition(proposedPosition, gameplayCamera, new Rect(0, 0, 1, 1));

        public Vector2 ClampPosition(Vector2 proposedPosition, Camera gameplayCamera, Rect playableViewport)
        {
            var worldPosition = new Vector3(proposedPosition.x, proposedPosition.y, transform.position.z);
            var viewportPosition = gameplayCamera.WorldToViewportPoint(worldPosition);
            var minimumOffset = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var maximumOffset = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var halfSize = footprintSize * .5f;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                var offset = transform.TransformVector(new Vector3(x * halfSize.x, y * halfSize.y, 0));
                var projectedOffset = (Vector2)(gameplayCamera.WorldToViewportPoint(worldPosition + offset) - viewportPosition);
                minimumOffset = Vector2.Min(minimumOffset, projectedOffset);
                maximumOffset = Vector2.Max(maximumOffset, projectedOffset);
            }
            viewportPosition.x = ClampAxis(viewportPosition.x, playableViewport.xMin - minimumOffset.x, playableViewport.xMax - maximumOffset.x);
            viewportPosition.y = ClampAxis(viewportPosition.y, playableViewport.yMin - minimumOffset.y, playableViewport.yMax - maximumOffset.y);
            return gameplayCamera.ViewportToWorldPoint(viewportPosition);
        }

        private static float ClampAxis(float value, float minimum, float maximum)
        {
            // Nếu viewport nhỏ hơn footprint, đặt giữa thay vì clamp với khoảng đảo ngược.
            return minimum <= maximum ? Mathf.Clamp(value, minimum, maximum) : (minimum + maximum) * .5f;
        }

        private void OnValidate()
        {
            footprintSize = Vector2.Max(footprintSize, Vector2.zero);
        }

        private void OnDrawGizmosSelected()
        {
            var previousMatrix = Gizmos.matrix;
            var previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(footprintSize.x, footprintSize.y, 0));
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
