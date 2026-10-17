using System;
using System.Collections.Generic;
using System.Linq;
using StarShift.Encounters;
using UnityEditor;
using UnityEngine;

namespace StarShift.Editor
{
    public static class EncounterConfigValidation
    {
        public static string[] Errors()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:LevelDefinition",new[] { "Assets/StarShift" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (!ids.Add(level.Id)) errors.Add("Duplicate level ID: " + level.Id);
                if (!level.IsValid(out var error)) errors.Add(path + ": " + (error ?? "Invalid config/reference/ID."));
            }
            return errors.ToArray();
        }
        [MenuItem("StarShift/Validate Encounter Content")]
        public static void Validate()
        {
            var errors = Errors();
            if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n",errors));
            Debug.Log("StarShift encounter content valid.");
        }
        [InitializeOnLoadMethod]
        private static void InstallGuard()
        {
            EditorApplication.playModeStateChanged -= BeforePlay;
            EditorApplication.playModeStateChanged += BeforePlay;
        }
        private static void BeforePlay(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            var errors = Errors();
            if (errors.Length == 0) return;
            EditorApplication.isPlaying = false;
            Debug.LogError(string.Join("\n", errors));
        }
    }
}
