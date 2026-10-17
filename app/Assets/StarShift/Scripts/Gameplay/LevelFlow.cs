using System;
using StarShift.Encounters;
using StarShift.Player;
using StarShift.UI;
using UnityEngine;

namespace StarShift.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LevelFlow : MonoBehaviour
    {
        [SerializeField] private LevelDefinition level;
        [SerializeField] private GameplayViewController view;
        [SerializeField] private PlayerGameplayGate player;
        [SerializeField] private EnemyFireGate enemyFireGate;
        [SerializeField] private MissionIntroPresentation presentation;
        private float elapsed;
        private bool announced;
        public LevelDefinition Level => level;
        public bool IsIntroducing { get; private set; }
        public float IntroElapsed => elapsed;
        public event Action<string> TitleRequested;

        public void LoadLevel(LevelDefinition definition)
        {
            if (IsIntroducing) throw new InvalidOperationException("Cancel active level before loading.");
            if (definition == null || !definition.IsValid(out _)) throw new ArgumentException("Invalid level.");
            level = definition;
        }

        public void Begin(bool withIntro, PlayDirection? directionOverride = null, CameraProfile cameraOverride = null)
        {
            var direction = directionOverride ?? level.Direction;
            Direction = direction;
            Baseline = cameraOverride != null ? cameraOverride : level.Camera;
            view.SetBaseline(Baseline.View);
            player.GetComponent<PlayerRespawnEntrance>().Direction = direction;
            player.transform.rotation = Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(direction));
            elapsed = 0; announced = false;
            IsIntroducing = withIntro && level.Intro.Enabled;
            if (IsIntroducing)
            {
                player.SetState(PlayerGameplayState.Entry);
                player.GetComponent<InputSystemPointerInput>().CancelGesture();
                enemyFireGate.Block();
                view.RequestView(level.Intro.ApproachView, 0);
                view.RequestView(Baseline.View, level.Intro.Duration);
                presentation.Present(0, direction);
            }
            else
            {
                presentation.Hide();
                view.RequestView(Baseline.View, 0);
                RequestTitle();
            }
        }
        public PlayDirection Direction { get; private set; }
        public CameraProfile Baseline { get; private set; }
        public void TickIntro(float delta)
        {
            if (!IsIntroducing) return;
            elapsed = Mathf.Min(level.Intro.Duration, elapsed + delta);
            presentation.Present(elapsed / level.Intro.Duration, Direction);
            if (!announced && elapsed >= level.Intro.Duration - level.Intro.AnnounceBeforeEnd) RequestTitle();
            if (!IsIntroducing || elapsed < level.Intro.Duration) return;
            IsIntroducing = false;
            presentation.Hide();
            player.GetComponent<InputSystemPointerInput>().CancelGesture();
            player.SetState(PlayerGameplayState.Active);
            enemyFireGate.ResumeAfter(0);
        }
        private void RequestTitle()
        {
            announced = true;
            var title = GameplayCopy.Mission(level.MissionNumber);
            if (!string.IsNullOrWhiteSpace(level.LocationName)) title = level.LocationName + "\n" + title;
            TitleRequested?.Invoke(title);
        }
        public void RefreshIntroTitle()
        {
            if (IsIntroducing && announced) RequestTitle();
        }
        public void ApplyEncounter(EncounterDefinition encounter)
        {
            var profile = encounter.CameraOverride != null ? encounter.CameraOverride : Baseline;
            view.RequestView(profile.View, profile.TransitionSeconds);
        }
        public void Cancel()
        {
            IsIntroducing = false;
            if (presentation != null) presentation.Hide();
        }
    }
}
