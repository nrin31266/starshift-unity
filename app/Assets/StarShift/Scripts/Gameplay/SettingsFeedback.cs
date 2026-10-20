using StarShift.Persistence;
using UnityEngine;

namespace StarShift.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SettingsFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioSource sfx;
        private bool vibrationEnabled;
        public void Apply(SettingsData value)
        {
            music.volume = value.MusicVolume;
            sfx.volume = value.SfxVolume;
            vibrationEnabled = value.VibrationEnabled;
        }
        public void PlayMusic(AudioClip clip) { music.clip = clip; music.Play(); }
        public void PlaySfx(AudioClip clip) { if (clip != null) sfx.PlayOneShot(clip); }
        public void Vibrate()
        {
            if (!vibrationEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
