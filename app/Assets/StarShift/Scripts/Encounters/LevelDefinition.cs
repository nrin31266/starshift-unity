using System;
using System.Collections.Generic;
using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.Encounters
{
    [Serializable]
    public sealed class MissionIntroSettings
    {
        public bool Enabled = true;
        [Range(2,4)] public float Duration = 3;
        public GameplayViewSettings ApproachView = new GameplayViewSettings(8, new Rect(0,0,1,1));
        [Min(0)] public float AnnounceBeforeEnd = .8f;
    }

    [Serializable]
    public sealed class LevelStartPoint
    {
        public string Id;
        public EncounterDefinition Encounter;
    }

    [CreateAssetMenu(menuName = "StarShift/Encounters/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public string Id = "L1";
        public string DisplayName = "First Contact";
        [Min(1)] public int MissionNumber = 1;
        public string RegionId = "astra-frontier";
        [Tooltip("Để trống khi chưa có tên location canonical; không suy diễn từ RegionId.")]
        public string LocationName;
        public PlayDirection Direction;
        public CameraProfile Camera;
        public MissionIntroSettings Intro = new MissionIntroSettings();
        public EncounterDefinition[] Encounters = Array.Empty<EncounterDefinition>();
        public LevelStartPoint[] AdditionalStarts = Array.Empty<LevelStartPoint>();

        public int ResolveStart(string id)
        {
            if (id == "LEVEL_START") return 0;
            var i = Array.FindIndex(Encounters, e => e != null && e.Id == id);
            if (i >= 0) return i;
            if (AdditionalStarts != null) foreach (var p in AdditionalStarts)
                if (p != null && p.Id == id) return Array.IndexOf(Encounters, p.Encounter);
            return -1;
        }

        public bool IsValid(out string error)
        {
            error = "Invalid level config: " + name;
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(RegionId) || MissionNumber < 1 ||
                !Enum.IsDefined(typeof(PlayDirection), Direction) || Camera == null || !Camera.IsValid ||
                Intro == null || !ConfigValue.Positive(Intro.Duration) || !ConfigValue.Nonnegative(Intro.AnnounceBeforeEnd) ||
                Intro.AnnounceBeforeEnd > Intro.Duration || !ConfigValue.Positive(Intro.ApproachView.OrthographicSize) ||
                Encounters == null || Encounters.Length == 0 || AdditionalStarts == null) return false;
            var ids = new HashSet<string> { "LEVEL_START" };
            foreach (var e in Encounters)
            {
                if (e == null || !ids.Add(e.Id)) { error = "Missing/duplicate encounter ID in level: " + name; return false; }
                if (!e.IsValid(out error)) return false;
            }
            error = "Invalid level start alias: " + name;
            foreach (var p in AdditionalStarts)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.Id) || !ids.Add(p.Id) || p.Encounter == null) return false;
                var index = Array.IndexOf(Encounters,p.Encounter);
                if (index < 0) return false;
                if (p.Id == "BOSS" && p.Encounter.Completion != EncounterCompletion.Boss) return false;
                if (p.Id == "PRE_BOSS" && (index + 1 >= Encounters.Length || Encounters[index+1].Completion != EncounterCompletion.Boss)) return false;
                // Chưa có runtime post-boss/transition start context; không alias vào một wave giả.
                if (p.Id == "POST_BOSS" || p.Id == "TRANSITION") return false;
            }
            error = null;
            return true;
        }
    }
}
