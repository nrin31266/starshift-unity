using StarShift.Persistence;
using UnityEngine;
using UnityEngine.Audio;

namespace StarShift.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] private AudioPalette palette;
        [SerializeField] private AudioMixerGroup musicGroup, sfxGroup, uiGroup;
        private readonly AudioSource[] music = new AudioSource[2];
        private readonly float[] weights = new float[2];
        private readonly AudioSource[] voices = new AudioSource[16];
        private readonly AudioPalette.Cue[] playing = new AudioPalette.Cue[16];
        private readonly float[] totals = new float[6];
        private readonly float[] levels = new float[16];
        private AudioPalette.Cue[] cues;
        private float[] nextAllowed;
        private float musicSetting = 1, sfxSetting = 1, dangerUntil, duck = 1;
        private int targetChannel;
        public AudioClip CurrentMusic => music[targetChannel] != null ? music[targetChannel].clip : null;
        public float MusicDuck { get; set; } = 1;
        public bool IsPaused { get; set; }
        public int AcceptedCues { get; private set; }

        private void Awake()
        {
            int count = System.Enum.GetValues(typeof(AudioCue)).Length;
            cues = new AudioPalette.Cue[count]; nextAllowed = new float[count];
            foreach (var cue in palette.Cues)
            {
                if (cue == null || cue.Clip == null || (int)cue.Id >= count || cues[(int)cue.Id] != null)
                { Debug.LogError("Audio palette contains a missing or duplicate cue.", this); enabled = false; return; }
                cues[(int)cue.Id] = cue;
            }
            for (int i = 0; i < music.Length; i++)
            { music[i] = CreateSource("Music" + i, musicGroup, 128); music[i].loop = true; }
            for (int i = 0; i < voices.Length; i++)
                voices[i] = CreateSource("Voice" + i, i >= 14 ? uiGroup : sfxGroup, i < 4 ? 0 : i < 6 ? 16 : 160);
        }

        private AudioSource CreateSource(string label, AudioMixerGroup group, int priority)
        {
            var child = new GameObject(label); child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.ignoreListenerPause = true;
            source.outputAudioMixerGroup = group; source.priority = priority; source.volume = 0;
            return source;
        }

        public void Apply(SettingsData settings)
        { musicSetting = settings.MusicVolume; sfxSetting = settings.SfxVolume; }

        public void SetMusic(AudioClip clip)
        {
            if (CurrentMusic == clip) return;
            int channel = 1 - targetChannel;
            if (music[channel].clip != clip || !music[channel].isPlaying)
            { music[channel].Stop(); music[channel].clip = clip; weights[channel] = 0; if (clip != null) music[channel].Play(); }
            targetChannel = channel;
        }
        public void Hub() => SetMusic(palette.Hub);
        public void Combat(bool boss) => SetMusic(boss ? palette.Boss : palette.Combat);

        public bool Play(AudioCue id)
        {
            if (!isActiveAndEnabled || cues == null || (int)id < 0 || (int)id >= cues.Length) return false;
            var cue = cues[(int)id]; var now = Time.unscaledTime;
            if (cue == null || now < nextAllowed[(int)id]) return false;
            bool ui = id == AudioCue.Confirm || id == AudioCue.Back || id == AudioCue.Error;
            int start = ui ? 14 : cue.Tier == 0 ? 0 : cue.Tier == 1 ? 4 : cue.Tier == 2 ? 6 : cue.Tier == 4 ? 12 : 8;
            int end = ui ? 16 : cue.Tier == 0 ? 4 : cue.Tier == 1 ? 6 : cue.Tier == 2 ? 8 : cue.Tier == 4 ? 14 : 12;
            int active = 0, available = -1;
            for (int i = start; i < end; i++)
            {
                if (!voices[i].isPlaying) { if (available < 0) available = i; continue; }
                if (playing[i] == cue) active++;
            }
            if (active >= cue.MaxVoices) return false;
            if (available < 0 && cue.Tier <= 1)
            {
                available = start;
                for (int i = start + 1; i < end; i++) if (voices[i].time > voices[available].time) available = i;
            }
            if (available < 0) return false;
            var voice = voices[available]; playing[available] = cue;
            levels[available] = cue.Volume * (1 + Random.Range(-cue.Variation, cue.Variation));
            voice.clip = cue.Clip; voice.pitch = 1 + Random.Range(-cue.Variation, cue.Variation);
            voice.priority = Mathf.Min(255, cue.Tier * 40);
            voice.volume = 0;
            voice.Play(); AcceptedCues++;
            nextAllowed[(int)id] = now + cue.Interval;
            if (cue.Tier == 0) dangerUntil = Mathf.Max(dangerUntil, now + cue.Clip.length);
            RefreshVoices(true);
            return true;
        }

        private void Update()
        {
            float desired = Mathf.Min(MusicDuck, Mathf.Min(IsPaused ? .3f : 1, Time.unscaledTime < dangerUntil ? .45f : 1));
            duck = Mathf.MoveTowards(duck, desired, Time.unscaledDeltaTime * (desired < duck ? 5 : 1.6f));
            for (int i = 0; i < music.Length; i++)
            {
                weights[i] = Mathf.MoveTowards(weights[i], i == targetChannel ? 1 : 0, Time.unscaledDeltaTime / .85f);
                float level = music[i].clip == palette.Hub ? palette.MusicLevel : palette.GameplayMusicLevel;
                music[i].volume = weights[i] * level * musicSetting * duck;
                if (i != targetChannel && weights[i] <= 0 && music[i].isPlaying) music[i].Stop();
            }
            RefreshVoices(false);
        }

        private static int VoiceGroup(int slot) => slot < 4 ? 0 : slot < 6 ? 1 : slot < 8 ? 2 : slot < 12 ? 3 : slot < 14 ? 4 : 5;

        private void RefreshVoices(bool immediate)
        {
            System.Array.Clear(totals, 0, totals.Length);
            for (int i = 0; i < voices.Length; i++)
                if (voices[i].isPlaying && playing[i] != null) totals[VoiceGroup(i)] += levels[i];
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying || playing[i] == null) continue;
                int group = VoiceGroup(i);
                float budget = group == 0 ? .6f : group == 1 ? .65f : group == 2 ? .65f : group == 3 ? .65f : group == 4 ? .55f : .35f;
                float multiplier = group < 2 || group == 5 ? 1 : IsPaused ? .12f : Time.unscaledTime < dangerUntil ? (group == 2 ? .55f : group == 3 ? .3f : .65f) : 1;
                float target = levels[i] * sfxSetting * multiplier * Mathf.Min(1, budget / totals[group]);
                // Weapon và pickup có budget riêng để tiếng nổ không kéo mất transient.
                voices[i].volume = immediate || target < voices[i].volume ? target : Mathf.MoveTowards(voices[i].volume, target, Time.unscaledDeltaTime * 2);
            }
        }

        public void ClearGameplay()
        {
            for (int i = 0; i < 14; i++) voices[i].Stop();
            System.Array.Clear(nextAllowed, 0, nextAllowed.Length); dangerUntil = 0;
        }
        private void OnDisable()
        {
            foreach (var source in voices) if (source != null) source.Stop();
            foreach (var source in music) if (source != null) source.Stop();
        }
    }
}
