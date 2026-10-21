using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEngine;

namespace StarShift.Audio
{
    [DisallowMultipleComponent, RequireComponent(typeof(AudioDirector))]
    public sealed class GameAudioPresentation : MonoBehaviour
    {
        [SerializeField] private GameShell shell;
        private AudioDirector audio;
        private GameplaySession session;
        private EncounterRunner runner;
        private RegionCampaignFlow region;
        private PlanetJourney journey;
        private bool inFrontend = true, wasGameOver, wasBoss, wasTravel, wasHandoff;
        private MissionIntroPhase intro;
        private PlanetJourneyPhase journeyPhase;
        private float launchUntil = -1, gameOverAt = -1;
        private void Awake()
        {
            audio = GetComponent<AudioDirector>(); session = GetComponent<GameplaySession>();
            runner = GetComponent<EncounterRunner>(); region = GetComponent<RegionCampaignFlow>();
            journey = GetComponent<PlanetJourney>();
        }
        private void OnEnable() { runner.EnemyDestroyed += OnDestroyed; runner.RunStarted += OnRun; }
        private void OnRun() { audio.ClearGameplay(); wasGameOver = wasBoss = false; gameOverAt = launchUntil = -1; }
        private void OnDestroyed(EnemyHealth enemy, RewardDropTable table, Vector3 position)
        {
            if (enemy.Definition.MissileTier == MissileDamageTier.Boss) return;
            audio.Play(enemy.GetComponent<AsteroidVisualMotion>() != null ? AudioCue.AsteroidBreak :
                enemy.Definition.MissileTier == MissileDamageTier.Large ? AudioCue.HeavyDeath : AudioCue.EnemyDeath);
        }
        private void LateUpdate()
        {
            bool frontend = !session.HasActiveRun && !session.IsTerminal && shell.Screen != ShellScreen.Playing;
            if (frontend)
            {
                if (!inFrontend) audio.ClearGameplay();
                audio.Hub(); audio.MusicDuck = 1; audio.IsPaused = false;
                launchUntil = -1; gameOverAt = -1; inFrontend = true; wasGameOver = wasBoss = wasTravel = wasHandoff = false;
                intro = MissionIntroPhase.None; journeyPhase = PlanetJourneyPhase.None; return;
            }
            inFrontend = false;
            bool gameOver = session.IsTerminal && !session.IsClear;
            bool boss = runner.Current != null && runner.Current.Completion == EncounterCompletion.Boss &&
                (runner.Stage == EncounterStage.Running || runner.Stage == EncounterStage.BossPostDeath);
            bool travel = region.IsTravelling || (journey != null && journey.IsActive);
            if (boss && !wasBoss) audio.Play(AudioCue.BossEnter);
            if (gameOver && !wasGameOver) gameOverAt = Time.unscaledTime + .3f;
            if (!gameOver) gameOverAt = -1;
            if (gameOverAt >= 0 && Time.unscaledTime >= gameOverAt) { audio.Play(AudioCue.GameOver); gameOverAt = -1; }
            if (travel && !wasTravel) { audio.Play(AudioCue.WarpStart); audio.Play(AudioCue.TransitionWind); }
            bool handoff = runner.IsHandoffTravel;
            if (handoff && !wasHandoff) audio.Play(AudioCue.TransitionWind);
            var nextIntro = runner.Flow != null ? runner.Flow.IntroPhase : MissionIntroPhase.None;
            if (nextIntro != intro)
            {
                if (journey == null || !journey.IsActive)
                {
                    if (nextIntro == MissionIntroPhase.Approach)
                    {
                        audio.Play(AudioCue.WarpStart); audio.Play(AudioCue.TransitionWind);
                        launchUntil = Time.unscaledTime + .85f;
                    }
                    if (nextIntro == MissionIntroPhase.Travel) audio.Play(AudioCue.WarpPass);
                    if (nextIntro == MissionIntroPhase.Brake) audio.Play(AudioCue.WarpArrival);
                }
                intro = nextIntro;
            }
            var nextJourney = journey != null ? journey.Phase : PlanetJourneyPhase.None;
            if (wasTravel && !travel && journeyPhase == PlanetJourneyPhase.None) audio.Play(AudioCue.WarpArrival);
            if (nextJourney != journeyPhase)
            {
                if (nextJourney == PlanetJourneyPhase.Accelerate || nextJourney == PlanetJourneyPhase.Travel) audio.Play(AudioCue.WarpPass);
                // Tap tiếp cận chỉ nối tiếng gió; không kích hỏa lại sau lượt departure.
                if (nextJourney == PlanetJourneyPhase.Approach) audio.Play(AudioCue.TransitionWind);
                if (nextJourney == PlanetJourneyPhase.Decelerate || nextJourney == PlanetJourneyPhase.Brake) audio.Play(AudioCue.WarpArrival);
                journeyPhase = nextJourney;
            }
            if (!gameOver)
            {
                if (nextIntro == MissionIntroPhase.Briefing) audio.Hub();
                else if (Time.unscaledTime < launchUntil) audio.SetMusic(null);
                else audio.Combat(boss);
            }
            audio.MusicDuck = gameOver ? .12f : runner.Stage == EncounterStage.BossPostDeath ? .15f : travel ? .55f : 1;
            audio.IsPaused = session.IsPaused || gameOver;
            wasGameOver = gameOver; wasBoss = boss; wasTravel = travel; wasHandoff = handoff;
        }
        private void OnDisable() { runner.EnemyDestroyed -= OnDestroyed; runner.RunStarted -= OnRun; }
    }
}
