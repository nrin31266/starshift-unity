using UnityEngine;
namespace StarShift.Enemies
{
    public sealed class AsteroidVisualMotion : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Sprite[] sizeSprites;
        private Quaternion initialRotation;
        private float angularSpeed;
        private void Awake() => initialRotation = visual.localRotation;
        public void Configure(float degreesPerSecond)
        {
            angularSpeed=degreesPerSecond; visual.localRotation=initialRotation;
            if (sizeSprites == null || sizeSprites.Length != 4) return;
            var scale = transform.localScale.x;
            var index = scale < .875f ? 0 : scale < 1.275f ? 1 : scale < 1.8f ? 2 : 3;
            visual.GetComponent<SpriteRenderer>().sprite = sizeSprites[index];
            visual.localScale = Vector3.one / scale;
        }
        private void Update() => visual.Rotate(0,0,angularSpeed*Time.deltaTime);
        private void OnDisable() { angularSpeed=0; if(visual!=null)visual.localRotation=initialRotation; }
    }
}
