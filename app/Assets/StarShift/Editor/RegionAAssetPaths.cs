using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StarShift.Editor
{
    public static class RegionAAssetPaths
    {
        public const string Root = "Assets/StarShift/Data/RegionA/";
        public static string Resolve<T>(string path) where T : UnityEngine.Object
        {
            if (!path.StartsWith(Root, StringComparison.Ordinal) || path.Substring(Root.Length).Contains('/')) return path;
            var name = Path.GetFileNameWithoutExtension(path);
            if (name == "TopHold")
                return "Assets/StarShift/Data/Fixtures/RegionAFlights/" + Path.GetFileName(path);
            var folder = name.StartsWith("Vanguard", StringComparison.Ordinal) || name == "BossCamera" || name == "BossGold" || name == "BeamMaterial" ? "Boss" : typeof(T).Name switch
            {
                "LevelDefinition" => "Missions",
                "EncounterDefinition" => "Encounters",
                "CameraProfile" => "Cameras",
                "FlightSequence" => "Flights",
                "FormationPlan" => "Formations",
                "HazardStreamDefinition" => "Hazards",
                "EnemyDefinition" or "EnemyModifierDefinition" => "Enemies",
                "SingleFireDefinition" => "Weapons",
                "PickupDefinition" or "RewardDropTable" => "Rewards",
                _ => throw new ArgumentException("Unsupported Region A asset type: " + typeof(T).Name)
            };
            return Root + folder + "/" + Path.GetFileName(path);
        }
        public static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(Resolve<T>(path));
    }
}
