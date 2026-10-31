// SettingsFeedback — Áp âm lượng và tùy chọn rung từ cài đặt người chơi.
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
        // Đồng bộ âm lượng cho nguồn cũ và AudioDirector, đồng thời giữ lựa chọn rung.
        public void Apply(SettingsData value)
        {
            music.volume = value.MusicVolume;
            sfx.volume = value.SfxVolume;
            GetComponent<StarShift.Audio.AudioDirector>()?.Apply(value);
            vibrationEnabled = value.VibrationEnabled;
        }
        public void PlayMusic(AudioClip clip) { music.clip = clip; music.Play(); }
        public void PlaySfx(AudioClip clip) { if (clip != null) sfx.PlayOneShot(clip); }
        // Chỉ rung trên Android thật và khi người chơi cho phép; Editor không phát rung thiết bị.
        public void Vibrate()
        {
            if (!vibrationEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
