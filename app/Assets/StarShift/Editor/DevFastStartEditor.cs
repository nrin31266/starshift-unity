using System.Collections.Generic;
using System.Linq;
using StarShift.Dev;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.UI;
using UnityEditor;
using UnityEngine;

namespace StarShift.Editor
{
    [CustomEditor(typeof(DevFastStart))]
    public sealed class DevFastStartEditor : UnityEditor.Editor
    {
        private bool advanced;

        internal static List<KeyValuePair<string, string>> Routes(LevelDefinition level, bool hasNext)
        {
            var result = new List<KeyValuePair<string, string>>();
            void Add(string id, string label) { if (!result.Any(p => p.Key == id)) result.Add(new KeyValuePair<string, string>(id, label)); }
            Add("LEVEL_START", "Đầu nhiệm vụ · LEVEL_START");
            if (level == null) return result;
            foreach (var encounter in level.Encounters)
                if (encounter != null) Add(encounter.Id, encounter.Id);
            foreach (var start in level.AdditionalStarts)
                if (start != null) Add(start.Id, start.Id == "BOSS_PHASE_B" ? "Boss phase B · BOSS_PHASE_B" : start.Id);
            if (hasNext) Add("TRANSITION", "Chuyển nhiệm vụ / hành tinh · TRANSITION");
            if (level.ResolveStart("BOSS") >= 0) Add("VICTORY", "Hạ boss để xem kết thúc · VICTORY");
            return result;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var dev = (DevFastStart)target;
            EditorGUILayout.HelpBox("DevTest tách CampaignSave/Profile. Tắt Dev Mode để Play vào Menu bình thường.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("devModeEnabled"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startLevel"));
            var level = (LevelDefinition)serializedObject.FindProperty("startLevel").objectReferenceValue;
            if (level == null) level = dev.GetComponent<LevelFlow>()?.Level;
            var missions = dev.GetComponent<RegionCampaignFlow>()?.Missions;
            int index = missions == null ? -1 : System.Array.IndexOf(missions, level);
            var routes = Routes(level, index >= 0 && index + 1 < missions.Length);
            var property = serializedObject.FindProperty("startPoint");
            int selected = routes.FindIndex(p => p.Key == property.stringValue);
            bool valid = selected >= 0;
            if (selected < 0)
            {
                routes.Insert(0, new KeyValuePair<string, string>(property.stringValue, "Giá trị cũ không hợp lệ — hãy chọn route"));
                selected = 0;
            }
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup("Start Point", selected, routes.Select(p => p.Value).ToArray());
            if (EditorGUI.EndChangeCheck()) property.stringValue = routes[next].Key;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("standalone"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skipIntro"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startingWeaponLevel"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unlockAllWeapons"));
            advanced = EditorGUILayout.Foldout(advanced, "Camera / hướng / reward seed", true);
            if (advanced)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideDirection"));
                if (serializedObject.FindProperty("overrideDirection").boolValue) EditorGUILayout.PropertyField(serializedObject.FindProperty("direction"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraOverride"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("deterministicRewards"));
                if (serializedObject.FindProperty("deterministicRewards").boolValue) EditorGUILayout.PropertyField(serializedObject.FindProperty("rewardSeed"));
            }
            serializedObject.ApplyModifiedProperties();
            var shell = dev.GetComponent<GameShell>();
            using (new EditorGUI.DisabledScope(!Application.isPlaying || shell != null && shell.IsBusy))
            {
                using (new EditorGUI.DisabledScope(!serializedObject.FindProperty("devModeEnabled").boolValue || !valid))
                    if (GUILayout.Button("Chạy lại route đã chọn")) dev.Replay();
                var session = dev.GetComponent<GameplaySession>();
                using (new EditorGUI.DisabledScope(!Application.isPlaying || session == null || !(session.IsPaused || session.CanPause)))
                    if (GUILayout.Button(session != null && session.IsPaused ? "Tiếp tục" : "Tạm dừng"))
                    { if (session.IsPaused) session.Resume(); else session.Pause(); }
                if (GUILayout.Button("Về Menu")) shell?.Boot();
            }
        }
    }
}
