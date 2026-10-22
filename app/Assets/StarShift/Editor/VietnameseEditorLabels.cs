using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StarShift.Editor
{
    [InitializeOnLoad]
    internal static class VietnameseEditorLabels
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "Visual", "Hình ảnh tàu" },
            { "Collision", "Vùng va chạm" },
            { "Muzzle", "Nòng súng" },
            { "ShieldCue", "Hiệu ứng khiên" },
            { "SupportDrone", "Tàu hỗ trợ" },
            { "GameplayViewController", "Thu phóng và vùng di chuyển" },
            { "FrameRateLimit", "Giới hạn tốc độ khung hình" },
            { "PlayableAreaPresentation", "Báo khi chạm giới hạn di chuyển" },
            { "Player", "Tàu người chơi" },
            { "Main Camera", "Camera chính" },
            { "Global Light 2D", "Ánh sáng chung" },
            { "Environment", "Môi trường" },
            { "Enemies", "Địch" },
            { "EncounterSystems", "Điều phối màn và thử nghiệm" },
            { "EventSystem", "Nhận thao tác giao diện" },
            { "GameplayUI", "Giao diện khi chơi" },
            { "GameShellUI", "Menu và các màn giao diện" },
            { "MissionApproach", "Giới thiệu nhiệm vụ" },
            { "Data", "Dữ liệu cấu hình" },
            { "Weapons", "Vũ khí" },
            { "Encounters", "Các đợt giao chiến" },
            { "Missions", "Nhiệm vụ" },
            { "Flights", "Đường bay" },
            { "Formations", "Đội hình" },
            { "Cameras", "Khung nhìn" },
            { "Rewards", "Phần thưởng" },
            { "Hazards", "Chướng ngại" },
            { "Boss", "Trùm" },
            { "RegionA", "Khu vực A" },
            { "RegionB", "Khu vực B" },
            { "Prefabs", "Mẫu đối tượng" },
            { "Projectiles", "Đạn" },
            { "Scripts", "Mã nguồn" },
            { "Scenes", "Cảnh chơi" },
            { "Audio", "Âm thanh" },
            { "Dev", "Công cụ thử nghiệm" },
            { "PulseLaser", "Súng xung - toàn bộ cấp" },
            { "PlasmaSpread", "Súng plasma xòe" },
            { "HomingMissile", "Tên lửa bám đích" },
            { "PulseLaserThermal", "Nhiệt súng xung" },
            { "PlasmaThermal", "Nhiệt súng plasma" },
            { "HomingThermal", "Nhiệt tên lửa" },
            { "PlayerPulseProjectile", "Đạn người chơi" },
            { "EnemySingleProjectile", "Đạn địch" },
            { "Scout", "Địch trinh sát" },
            { "VanguardFrigate", "Trùm Vanguard" },
            { "NebulaWarden", "Trùm Warden" },
            { "Mission01Foundation", "Đợt mở đầu nhiệm vụ 1" },
            { "Mission01Formation", "Đợt đội hình nhiệm vụ 1" },
            { "PulseLaserWeapon", "Điều khiển bắn và nhiệt" },
            { "RelativeDragMovement", "Di chuyển bằng kéo tương đối" },
            { "PlayerMovementBounds", "Giới hạn di chuyển tàu" },
            { "PlayerLives", "Số mạng người chơi" },
            { "PlayerDamageLifecycle", "Mất mạng và hồi sinh" },
            { "WeaponLoadout", "Các loại súng và cấp súng" },
            { "EnemyHealth", "Nhận sát thương và máu địch" },
            { "EnemyDefinition", "Cấu hình máu và điểm địch" },
            { "SingleFireDefinition", "Cấu hình địch bắn" },
            { "PulseLaserDefinition", "Cấu hình vũ khí người chơi" },
            { "WeaponThermalProfile", "Cấu hình nhiệt vũ khí" },
            { "EncounterDefinition", "Cấu hình đợt giao chiến" },
            { "CameraProfile", "Cấu hình camera" },
            { "EncounterDebugView", "Hiển thị hỗ trợ kiểm tra" }
        };

        static VietnameseEditorLabels()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += DrawHierarchy;
            EditorApplication.projectWindowItemOnGUI += DrawProject;
        }

        internal static string Translate(string name) => Names.TryGetValue(name, out var label) ? label : name;

        private static void DrawHierarchy(EntityId entityId, Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            var obj = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (obj != null && obj.scene.name == "02_Gameplay") DrawSuffix(rect, obj.name);
        }

        private static void DrawProject(string guid, Rect rect)
        {
            if (Event.current.type != EventType.Repaint || rect.height > 20) return;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/StarShift/")) DrawSuffix(rect, Path.GetFileNameWithoutExtension(path));
        }

        private static void DrawSuffix(Rect rect, string name)
        {
            if (!Names.TryGetValue(name, out var label)) return;
            // Chỉ thêm chú thích vào khoảng trống; giữ tên, thao tác chọn và GUID gốc.
            rect.xMin += 18 + EditorStyles.label.CalcSize(new GUIContent(name)).x;
            if (rect.width > 24) GUI.Label(rect, new GUIContent(" · " + label, label + " · " + name), EditorStyles.miniLabel);
        }
    }
}
