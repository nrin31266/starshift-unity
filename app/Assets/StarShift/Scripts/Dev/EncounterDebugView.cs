using StarShift.Encounters;
using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.Dev
{
    [DisallowMultipleComponent, RequireComponent(typeof(EncounterRunner))]
    public sealed class EncounterDebugView : MonoBehaviour
    {
        [SerializeField] private bool showBounds;
        [SerializeField] private bool showPaths;
        [SerializeField] private bool showSpawnPoints;
        [SerializeField] private bool showHitboxes;
        [SerializeField] private bool showFps;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private float averageDelta;
        private void Update() => averageDelta = Mathf.Lerp(averageDelta, Time.unscaledDeltaTime, .05f);
        private void OnGUI()
        {
            if (showFps && averageDelta > 0) GUI.Label(new Rect(12,70,220,30), $"FPS {1/averageDelta:0}");
        }
        private void OnDrawGizmos()
        {
            var camera = Camera.main;
            var runner = GetComponent<EncounterRunner>();
            var flow = GetComponent<LevelFlow>();
            if (camera == null || flow == null || flow.Level == null || flow.Level.Encounters.Length == 0) return;
            Vector3 World(Vector2 p) => camera.ViewportToWorldPoint(new Vector3(p.x,p.y,10));
            var direction = Application.isPlaying ? flow.Direction : flow.Level.Direction;
            if (showBounds)
            {
                Gizmos.color = Color.cyan;
                var view = camera.GetComponent<GameplayViewController>();
                var region = Application.isPlaying ? view.CurrentPlayableViewport : flow.Level.Camera.View.PlayableViewport;
                var min = World(region.min); var max = World(region.max);
                Gizmos.DrawWireCube((min+max)*.5f,max-min);
            }
            var encounter = Application.isPlaying && runner.HasActiveRun ? runner.Current : flow.Level.Encounters[0];
            foreach (var group in encounter.Groups)
            {
                if (group.Sequence == null) continue;
                for (int i = 0; i < group.Count; i++)
                {
                    var offset = group.Anchor + group.SlotOffset(i);
                    Vector3 Point(Vector2 p) => World(PlayDirectionSpace.Point(p+offset,direction));
                    Gizmos.color = Color.yellow;
                    if (showSpawnPoints)
                    {
                        Gizmos.DrawWireSphere(Point(group.Sequence.Phases[0].From),.12f);
                        foreach (var phase in group.Sequence.Phases) Gizmos.DrawWireSphere(Point(phase.To),.08f);
                    }
                    if (!showPaths) continue;
                    foreach (var phase in group.Sequence.Phases)
                    {
                        var previous = Point(phase.From);
                        for (int step = 1; step <= 32; step++)
                        {
                            var next = Point(phase.Evaluate(step/32f,phase.From,phase.To));
                            Gizmos.DrawLine(previous,next); previous = next;
                        }
                    }
                }
            }
            if (showHitboxes)
            {
                Gizmos.color = Color.red;
                foreach (var shape in FindObjectsByType<Collider2D>())
                    if (shape.gameObject.scene == gameObject.scene) Gizmos.DrawWireCube(shape.bounds.center,shape.bounds.size);
            }
        }
#endif
    }
}
