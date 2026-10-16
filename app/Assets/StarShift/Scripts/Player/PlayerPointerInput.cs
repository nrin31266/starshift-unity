using System;
using UnityEngine;

namespace StarShift.Player
{
    public abstract class PlayerPointerInput : MonoBehaviour
    {
        // Snapshot không bị consume: movement và weapon đọc độc lập cùng một nguồn.
        public abstract Vector2 ReadDelta();
        public abstract bool FireHeld { get; }
        public event Action FireIntentChanged;
        protected void NotifyFireIntentChanged() => FireIntentChanged?.Invoke();

        // Chỉ hủy anchor/delta movement, không hủy held intent của pointer còn giữ.
        public abstract void ResetDrag();
    }
}
