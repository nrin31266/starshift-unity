using UnityEngine;

namespace StarShift.Player
{
    public abstract class PlayerPointerInput : MonoBehaviour
    {
        // Một consumer đọc một lần mỗi Update; delta dùng pixel màn hình, không phải vận tốc.
        public abstract Vector2 ReadDelta();
        public abstract void ResetDrag();
    }
}
