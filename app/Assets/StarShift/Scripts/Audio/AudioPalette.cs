using System;
using UnityEngine;

namespace StarShift.Audio
{
    public enum AudioCue
    {
        Pulse, Plasma, Homing, HomingImpact, PlayerHit, ShieldAbsorb, ShieldBreak, Overheat, Vent, PlayerDeath,
        HostileShot, InterceptorCharge, InterceptorDash, SniperCharge, SniperFire, HeavyCharge, HeavyShot,
        BossEnter, BossCannonWarning, BossBeamWarning, BossCurtainWarning, BossShelterWarning, BossEscortWarning,
        ArmorBreak, EnemyDeath, HeavyDeath, AsteroidBreak, BossBurst, BossFinal, Coin, PowerUp, ExtraLife,
        Confirm, Back, Error, WarpStart, WarpPass, WarpArrival, Victory, GameOver
    }

    [CreateAssetMenu(menuName = "StarShift/Audio/Palette")]
    public sealed class AudioPalette : ScriptableObject
    {
        [Serializable]
        public sealed class Cue
        {
            public AudioCue Id;
            public AudioClip Clip;
            [Range(0, 1)] public float Volume = .3f;
            [Range(0, 4)] public int Tier = 3;
            [Range(1, 4)] public int MaxVoices = 2;
            [Min(0)] public float Interval = .1f;
            [Range(0, .1f)] public float Variation = .035f;
        }
        public AudioClip Hub, Combat, Boss;
        [Range(0, 1)] public float MusicLevel = .28f;
        public Cue[] Cues = Array.Empty<Cue>();
    }
}
