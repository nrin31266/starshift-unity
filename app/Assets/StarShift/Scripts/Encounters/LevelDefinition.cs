using System;
using System.Collections.Generic;
using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.Encounters
{
    [Serializable]
    public sealed class MissionIntroSettings
    {
        [Tooltip("Intro đầu mission khi LEVEL_START. Chỉnh đúng asset Level trên LevelFlow; DevFastStart/Skip Intro có thể bỏ qua.")]
        public bool Enabled = true;
        [Min(.01f)] public float ContinueDelaySeconds = 1.8f;
        public string ArrivalTitle;
        [Min(.01f)] public float MissionTitleSeconds = 2.1f;
        [Min(0)] public float MissionEntryLeadSeconds = .75f;
        public GameplayViewSettings ApproachView = new GameplayViewSettings(8, new Rect(0,0,1,1));
        public GameplayViewSettings TravelView = new GameplayViewSettings(9, new Rect(0,0,1,1));
        [Min(.01f)] public float ApproachSeconds = .7f;
        [Min(.01f)] public float TravelSeconds = 1.8f;
        [Min(.01f)] public float BrakeSeconds = .8f;
        public bool IsValid => ConfigValue.Positive(MissionTitleSeconds) && ConfigValue.Nonnegative(MissionEntryLeadSeconds) &&
            ConfigValue.Positive(ContinueDelaySeconds) && ConfigValue.Positive(ApproachSeconds) && ConfigValue.Positive(TravelSeconds) &&
            ConfigValue.Positive(BrakeSeconds) && ConfigValue.Positive(ApproachView.OrthographicSize) && ConfigValue.Positive(TravelView.OrthographicSize);
    }

    [Serializable]
    public sealed class ReplayLoadoutPreset
    {
        public int Lives = 3;
        public int Missiles = 1;
        public StarShift.Combat.WeaponBranch ActiveWeapon;
        public int[] WeaponLevels = { 1, 0, 0 };
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
        [Min(1), Tooltip("Nhân HP quái thường theo mission, làm tròn lên; không áp Boss/thiên thạch.")]
        public float EnemyHealthMultiplier = 1;
        public string RegionId = "astra-frontier";
        [Tooltip("Để trống khi chưa có tên location canonical; không suy diễn từ RegionId.")]
        public string LocationName;
        [TextArea] public string MissionBriefing;
        public PlayDirection Direction;
        public CameraProfile Camera;
        public ReplayLoadoutPreset ReplayPreset;
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
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(RegionId) || MissionNumber < 1 || !ConfigValue.Positive(EnemyHealthMultiplier) || EnemyHealthMultiplier < 1 ||
                !Enum.IsDefined(typeof(PlayDirection), Direction) || Camera == null || !Camera.IsValid ||
                Intro == null || !Intro.IsValid ||
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
