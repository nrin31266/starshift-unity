using UnityEngine;

namespace StarShift.Gameplay
{
    public enum PlayDirection { Up, Right }

    public static class PlayDirectionSpace
    {
        public static Vector2 Point(Vector2 local, PlayDirection direction)
            => direction == PlayDirection.Right ? new Vector2(local.y, 1 - local.x) : local;
        public static Vector2 Offset(Vector2 local, PlayDirection direction)
            => direction == PlayDirection.Right ? new Vector2(local.y, -local.x) : local;
        public static float Rotation(PlayDirection direction) => direction == PlayDirection.Right ? -90 : 0;
    }
}
