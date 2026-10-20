using UnityEngine;
namespace StarShift.Enemies
{
    public sealed class AsteroidVisualMotion : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        private Quaternion initialRotation;
        private float angularSpeed;
        private void Awake() => initialRotation = visual.localRotation;
        public void Configure(float degreesPerSecond) { angularSpeed=degreesPerSecond; visual.localRotation=initialRotation; }
        private void Update() => visual.Rotate(0,0,angularSpeed*Time.deltaTime);
        private void OnDisable() { angularSpeed=0; if(visual!=null)visual.localRotation=initialRotation; }
    }
}
