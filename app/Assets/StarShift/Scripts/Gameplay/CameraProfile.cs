using UnityEngine;

namespace StarShift.Gameplay
{
    [CreateAssetMenu(menuName = "StarShift/Gameplay/Camera Profile")]
    public sealed class CameraProfile : ScriptableObject
    {
        public GameplayViewSettings View = GameplayViewSettings.Baseline;
        [Min(0)] public float TransitionSeconds = .6f;
        public bool IsValid => float.IsFinite(TransitionSeconds) && TransitionSeconds >= 0 &&
            float.IsFinite(View.OrthographicSize) && View.OrthographicSize > 0 &&
            View.PlayableViewport.width > 0 && View.PlayableViewport.height > 0 &&
            View.PlayableViewport == View.Validated().PlayableViewport;
    }
}
