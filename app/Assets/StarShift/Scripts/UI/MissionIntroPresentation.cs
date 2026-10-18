using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.UI
{
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class MissionIntroPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer planet;
        [SerializeField] private Transform stars;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private SpriteRenderer ship;
        [SerializeField] private GameObject playerVisual;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private GameObject gameplayBackdrop;
        [SerializeField, Min(.01f)] private float planetStartScale = 3.2f;
        [SerializeField, Min(.01f)] private float planetEndScale = 4.1f;
        private SpriteRenderer[] starRenderers;
        private SpriteRenderer[] backdropRenderers;
        private Color[] backdropColors;
        private bool introVisible;
        private bool visualWasActive;
        private bool backdropWasActive;
        private bool cueVisible;
        public bool IsCueVisible => cueVisible;
        private MissionIntroPhase phase;
        private PlayDirection direction;
        private float progress;
        private float starDistance;

        private void Awake()
        {
            starRenderers = stars.GetComponentsInChildren<SpriteRenderer>(true);
            backdropRenderers = gameplayBackdrop.GetComponentsInChildren<SpriteRenderer>(true);
            backdropColors = new Color[backdropRenderers.Length];
        }
        public void Present(MissionIntroPhase nextPhase, float normalized, PlayDirection playDirection)
        {
            if (!introVisible)
            {
                introVisible = true;
                visualWasActive = playerVisual.activeSelf;
                backdropWasActive = gameplayBackdrop.activeSelf;
                for (int i = 0; i < backdropColors.Length; i++) backdropColors[i] = backdropRenderers[i].color;
                playerVisual.SetActive(false);
                gameplayBackdrop.SetActive(false);
                starDistance = 0;
            }
            cueVisible = false;
            phase = nextPhase;
            progress = normalized;
            direction = playDirection;
            stars.gameObject.SetActive(true);
            ship.gameObject.SetActive(true);
            ApplyVisuals(0);
        }
        public void PresentHandoff(float normalized, PlayDirection playDirection)
        {
            cueVisible = true;
            progress = normalized;
            direction = playDirection;
            stars.gameObject.SetActive(true);
        }
        private void LateUpdate()
        {
            if (introVisible || cueVisible) ApplyVisuals(Time.deltaTime);
        }
        private void ApplyVisuals(float delta)
        {
            var t = Mathf.SmoothStep(0,1,progress);
            float speed = cueVisible ? .07f : phase switch
            {
                MissionIntroPhase.Briefing => .015f,
                MissionIntroPhase.Approach => Mathf.Lerp(.015f,.12f,t),
                MissionIntroPhase.Travel => Mathf.Lerp(.12f,1.5f,t),
                MissionIntroPhase.Brake => 1.5f * (1-t),
                _ => 0
            };
            starDistance += speed * delta;
            var opacity = cueVisible ? Mathf.Sin(progress * Mathf.PI) * .45f : 1f;
            if (!cueVisible && phase == MissionIntroPhase.Brake) opacity = 1-t;
            stars.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            stars.localScale = Vector3.one;
            for (int i = 0; i < starRenderers.Length; i++)
            {
                var depth = .45f + Mathf.Repeat(i * .371f, .55f);
                var local = new Vector2(Mathf.Repeat(i * .618034f,1), Mathf.Repeat(i * .437f - starDistance * depth,1.4f)-.2f);
                var viewport = PlayDirectionSpace.Point(local,direction);
                var star = starRenderers[i];
                star.transform.position = World(viewport);
                star.transform.rotation = Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(direction));
                star.transform.localScale = new Vector3(.012f+.012f*depth,.02f+speed*.32f*depth,1);
                star.color = new Color(.4f,.65f,.85f,opacity*(.3f+depth*.4f));
            }
            if (cueVisible) return;
            bool showPlanet = phase == MissionIntroPhase.Briefing || phase == MissionIntroPhase.Approach || phase == MissionIntroPhase.Travel;
            planet.gameObject.SetActive(showPlanet);
            if (showPlanet)
            {
                var approach = phase == MissionIntroPhase.Briefing ? 0 : phase == MissionIntroPhase.Approach ? t : 1;
                planet.transform.position = World(PlayDirectionSpace.Point(Vector2.Lerp(new Vector2(.7f,.76f),new Vector2(.67f,.8f),approach),direction));
                planet.transform.localScale = Vector3.one * Mathf.Lerp(planetStartScale,planetEndScale,approach);
                var color = planet.color;
                color.a = phase == MissionIntroPhase.Travel ? 1-Mathf.SmoothStep(0,1,progress/.3f) : 1;
                planet.color = color;
            }
            var shipViewport = PlayDirectionSpace.Point(new Vector2(.5f,
                phase == MissionIntroPhase.Briefing ? .17f : phase == MissionIntroPhase.Approach ? Mathf.Lerp(.17f,.29f,t) : .29f),direction);
            var shipPosition = World(shipViewport);
            if (phase == MissionIntroPhase.Brake) shipPosition = Vector3.Lerp(shipPosition,playerRoot.position,t);
            ship.transform.SetPositionAndRotation(shipPosition,Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(direction)));
            var backdropAlpha = phase == MissionIntroPhase.Brake ? t : 0;
            gameplayBackdrop.SetActive(backdropWasActive && backdropAlpha > 0);
            for (int i = 0; i < backdropRenderers.Length; i++)
            {
                var color = backdropColors[i];color.a *= backdropAlpha;backdropRenderers[i].color = color;
            }
        }
        private Vector3 World(Vector2 viewport)
        {
            var depth = Vector3.Dot(-gameplayCamera.transform.position,gameplayCamera.transform.forward);
            return gameplayCamera.ViewportToWorldPoint(new Vector3(viewport.x,viewport.y,depth));
        }
        public void Hide()
        {
            if (planet != null) planet.gameObject.SetActive(false);
            if (stars != null) stars.gameObject.SetActive(false);
            if (ship != null) ship.gameObject.SetActive(false);
            cueVisible = false;
            if (!introVisible) return;
            introVisible = false;
            playerVisual.SetActive(visualWasActive);
            gameplayBackdrop.SetActive(backdropWasActive);
            for (int i = 0; i < backdropRenderers.Length; i++) backdropRenderers[i].color = backdropColors[i];
        }
        private void OnDisable() => Hide();
    }
}
