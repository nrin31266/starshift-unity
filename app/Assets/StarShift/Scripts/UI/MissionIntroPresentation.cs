using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.UI
{
    [DisallowMultipleComponent]
    public sealed class MissionIntroPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer planet;
        [SerializeField] private Transform stars;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private float planetStartScale = .6f;
        [SerializeField] private float planetEndScale = 10f;
        public void Present(float progress, PlayDirection direction)
        {
            planet.gameObject.SetActive(true);
            stars.gameObject.SetActive(true);
            var t = Mathf.SmoothStep(0,1,progress);
            var position = PlayDirectionSpace.Point(Vector2.Lerp(new Vector2(.5f,.64f),new Vector2(.5f,1.65f),t),direction);
            planet.transform.position = gameplayCamera.ViewportToWorldPoint(new Vector3(position.x,position.y,10));
            planet.transform.localScale = Vector3.one * Mathf.Lerp(planetStartScale,planetEndScale,t);
            var color = planet.color; color.a = 1-Mathf.SmoothStep(.65f,1,progress); planet.color = color;
            stars.localPosition = (Vector3)PlayDirectionSpace.Offset(new Vector2(0,-progress*5),direction);
            stars.localScale = new Vector3(1,1+Mathf.Sin(progress*Mathf.PI)*2,1);
        }
        public void Hide()
        {
            if (planet != null) planet.gameObject.SetActive(false);
            if (stars != null) stars.gameObject.SetActive(false);
        }
    }
}
