using UnityEngine;

namespace StarShift.UI
{
    public sealed class ShellStarfield : MonoBehaviour
    {
        private void Update()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var star = (RectTransform)transform.GetChild(i);
                var anchor = star.anchorMin;
                anchor.y = Mathf.Repeat(anchor.y - Time.unscaledDeltaTime * .007f, 1);
                star.anchorMin = star.anchorMax = anchor;
            }
        }
    }
}
