using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.UI
{
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class MissionIntroPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer planet;
        [SerializeField] private Sprite astraPlanet, crimsonPlanet;
        [SerializeField] private SpriteRenderer farBackground;
        [SerializeField] private Sprite astraBackground, crimsonBackground;
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
        private bool gameplayApproach;
        public Vector3 ShipPosition => ship.transform.position;
        public void BeginGameplayApproach()
        {
            gameplayApproach = true; playerVisual.SetActive(visualWasActive); ship.gameObject.SetActive(false);
        }
        private bool visualWasActive;
        private bool backdropWasActive;
        private bool cueVisible;
        public bool IsCueVisible => cueVisible;
        private MissionIntroPhase phase;
        private PlayDirection direction;
        private float progress;
        private float starDistance;
        private bool journeyVisible;
        private float journeyHeading, journeySpeed, journeyPalette, journeyReveal;
        private float cueSpeed = .07f;
        private Color planetColor;
        private Color[] environmentTints;
        private Vector3[] environmentPositions;
        private StarShift.Player.RelativeDragMovement movement;
        private float approachStart = .17f;

        private void Awake()
        {
            movement = playerRoot.GetComponent<StarShift.Player.RelativeDragMovement>();
            starRenderers = stars.GetComponentsInChildren<SpriteRenderer>(true);
            backdropRenderers = gameplayBackdrop.GetComponentsInChildren<SpriteRenderer>(true);
            backdropColors = new Color[backdropRenderers.Length];
            planetColor = planet.color;
            environmentTints = new Color[backdropRenderers.Length]; environmentPositions = new Vector3[backdropRenderers.Length];
            for(int i=0;i<backdropRenderers.Length;i++){environmentTints[i]=backdropRenderers[i].color;environmentPositions[i]=backdropRenderers[i].transform.position;}
        }
        public void Present(MissionIntroPhase nextPhase, float normalized, PlayDirection playDirection)
        {
            if (journeyVisible && nextPhase == MissionIntroPhase.Approach)
            {
                var viewport = gameplayCamera.WorldToViewportPoint(playerRoot.position);
                approachStart = playDirection == PlayDirection.Right ? viewport.x : viewport.y;
            }
            else if (nextPhase == MissionIntroPhase.Briefing) approachStart = .17f;
            journeyVisible = false;
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
            if (astraPlanet != null && crimsonPlanet != null)
                planet.sprite = playDirection == PlayDirection.Right ? crimsonPlanet : astraPlanet;
            stars.gameObject.SetActive(true);
            ship.gameObject.SetActive(!gameplayApproach);
            ApplyVisuals(0);
        }
        public void PresentHandoff(float normalized, PlayDirection playDirection)
        {
            journeyVisible = false; cueSpeed = .07f;
            cueVisible = true;
            progress = normalized;
            direction = playDirection;
            stars.gameObject.SetActive(true);
        }
        private void LateUpdate()
        {
            if (journeyVisible) ApplyJourneyVisuals(Time.deltaTime);
            else if (introVisible || cueVisible) ApplyVisuals(Time.deltaTime);
        }
        private void ApplyVisuals(float delta)
        {
            var t = Mathf.SmoothStep(0,1,progress);
            float speed = cueVisible ? cueSpeed : phase switch
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
                star.transform.localScale = star.sprite.name == "star"
                    ? new Vector3(.35f+.25f*depth,.45f+speed*6*depth,1)
                    : new Vector3(.012f+.012f*depth,.02f+speed*.32f*depth,1);
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
                phase == MissionIntroPhase.Briefing ? approachStart : phase == MissionIntroPhase.Approach ? Mathf.Lerp(approachStart,.29f,t) : .29f),direction);
            var shipPosition = World(shipViewport);
            if (phase == MissionIntroPhase.Brake) shipPosition = Vector3.Lerp(shipPosition,movement.CheckpointPosition(direction),t);
            ship.transform.SetPositionAndRotation(shipPosition,Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(direction)));
            var backdropAlpha = phase == MissionIntroPhase.Brake ? t : 0;
            gameplayBackdrop.SetActive(backdropWasActive && backdropAlpha > 0);
            for (int i = 0; i < backdropRenderers.Length; i++)
            {
                var color = backdropColors[i];color.a *= backdropAlpha;backdropRenderers[i].color = color;
            }
        }
        public void PresentMissionTravel(float normalized, PlayDirection playDirection)
        {
            PresentHandoff(normalized,playDirection);
            cueSpeed = .1f + .55f * Mathf.Sin(Mathf.Clamp01(normalized)*Mathf.PI);
        }
        public void PresentPlanetJourney(float heading, float speed, float palette, float reveal)
        {
            journeyVisible=true; cueVisible=false;
            journeyHeading=heading;journeySpeed=speed;journeyPalette=palette;journeyReveal=reveal;
            stars.gameObject.SetActive(true);ship.gameObject.SetActive(false);
        }
        private void ApplyJourneyVisuals(float delta)
        {
            starDistance += journeySpeed*delta;
            var vector=(Vector2)(Quaternion.Euler(0,0,journeyHeading)*Vector3.up);
            for(int i=0;i<starRenderers.Length;i++)
            {
                float depth=.45f+Mathf.Repeat(i*.371f,.55f);
                var seed=new Vector2(Mathf.Repeat(i*.618034f,1),Mathf.Repeat(i*.437f,1));
                var offset=seed-vector*starDistance*depth;
                var star=starRenderers[i];star.transform.position=World(new Vector2(Mathf.Repeat(offset.x+.15f,1.3f)-.15f,Mathf.Repeat(offset.y+.15f,1.3f)-.15f));
                star.transform.rotation=Quaternion.Euler(0,0,journeyHeading);
                star.transform.localScale=star.sprite.name == "star"
                    ? new Vector3(.35f+.25f*depth,.5f+journeySpeed*6*depth,1)
                    : new Vector3(.012f+.012f*depth,.025f+journeySpeed*.3f*depth,1);
                star.color=Color.Lerp(new Color(.4f,.65f,.85f,.65f),new Color(.85f,.3f,.48f,.65f),journeyPalette);
            }
            ApplyEnvironmentPalette(journeyPalette);
            for(int i=0;i<backdropRenderers.Length;i++)backdropRenderers[i].transform.position-=new Vector3(vector.x,vector.y,0)*journeySpeed*delta*2;
            planet.gameObject.SetActive(journeyReveal>0);
            if(journeyReveal>0)
            {
                var target=PlayDirectionSpace.Point(new Vector2(.7f,.76f),PlayDirection.Right);
                planet.transform.position=World(Vector2.Lerp(new Vector2(1.1f,.3f),target,Mathf.SmoothStep(0,1,journeyReveal)));
                planet.transform.localScale=Vector3.one*Mathf.Lerp(.25f,planetStartScale,journeyReveal);
                if (crimsonPlanet != null) planet.sprite = crimsonPlanet;
                planet.color=new Color(1,1,1,Mathf.Clamp01(journeyReveal*4));
            }
        }
        private void ApplyEnvironmentPalette(float amount)
        {
            if (farBackground != null && astraBackground != null && crimsonBackground != null)
                farBackground.sprite = amount < .5f ? astraBackground : crimsonBackground;
            for(int i=0;i<backdropRenderers.Length;i++)backdropRenderers[i].color=Color.Lerp(environmentTints[i],new Color(.45f,.2f,.32f,environmentTints[i].a),amount);
        }
        public void SetDestinationPalette() => ApplyEnvironmentPalette(1);
        public void ResetEnvironment()
        {
            ApplyEnvironmentPalette(0);planet.color=planetColor;
            for(int i=0;i<backdropRenderers.Length;i++)backdropRenderers[i].transform.position=environmentPositions[i];
        }
        private Vector3 World(Vector2 viewport)
        {
            var depth = Vector3.Dot(-gameplayCamera.transform.position,gameplayCamera.transform.forward);
            return gameplayCamera.ViewportToWorldPoint(new Vector3(viewport.x,viewport.y,depth));
        }
        public void Hide()
        {
            journeyVisible=false; gameplayApproach=false;
            if (planet != null) planet.gameObject.SetActive(false);
            if (stars != null) stars.gameObject.SetActive(false);
            if (ship != null) ship.gameObject.SetActive(false);
            cueVisible = false;
            if (!introVisible) return;
            introVisible = false;
            // Teardown scene không đảm bảo thứ tự hủy Player và backdrop.
            if (playerVisual != null) playerVisual.SetActive(visualWasActive);
            if (gameplayBackdrop != null) gameplayBackdrop.SetActive(backdropWasActive);
            for (int i = 0; i < backdropRenderers.Length; i++)
                if (backdropRenderers[i] != null) backdropRenderers[i].color = backdropColors[i];
        }
        private void OnDisable() => Hide();
    }
}
