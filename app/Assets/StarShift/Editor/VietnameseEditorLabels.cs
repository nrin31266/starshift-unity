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
            { "PlayerGameplayGate", "Quyền di chuyển, bắn và nhận sát thương" },
            { "PlayerDamageLifecycle", "Mất mạng và hồi sinh" },
            { "WeaponLoadout", "Các loại súng và cấp súng" },
            { "EnemyHealth", "Nhận sát thương và máu địch" },
            { "EnemyDefinition", "Cấu hình máu và điểm địch" },
            { "SingleFireDefinition", "Cấu hình địch bắn" },
            { "PulseLaserDefinition", "Cấu hình vũ khí người chơi" },
            { "WeaponThermalProfile", "Cấu hình nhiệt vũ khí" },
            { "EncounterDefinition", "Cấu hình đợt giao chiến" },
            { "CameraProfile", "Cấu hình camera" },
            { "EncounterDebugView", "Hiển thị hỗ trợ kiểm tra" },
            { "InputSystemPointerInput", "Nhận chuột và cảm ứng" },
            { "PlayerContactReceiver", "Nhận va chạm và đạn địch" },
            { "PlayerPickupReceiver", "Nhặt và áp dụng vật phẩm" },
            { "PlayerTemporaryEffects", "Khiên và hiệu ứng tạm thời" },
            { "PlayerRespawnEntrance", "Đường bay hồi sinh an toàn" },
            { "EmergencyMissileReserve", "Tên lửa dự trữ và sát thương diện rộng" },
            { "PlayerProjectile", "Đạn người chơi và xử lý trúng mục tiêu" },
            { "EnemyProjectile", "Đạn địch và xử lý trúng tàu" },
            { "WeaponHeatState", "Tính nhiệt và khóa bắn quá nhiệt" },
            { "GameplayViewSettings", "Độ zoom và vùng được bay" },
            { "BossPhaseRunner", "Chuyển giai đoạn và chọn đòn boss" },
            { "SingleFireAttack", "Địch báo trước và bắn theo loạt" },
            { "EncounterRunner", "Sinh địch và điều phối đợt" },
            { "SpawnAction", "Nhóm địch và lịch xuất hiện" },
            { "RunRewards", "Điểm và thống kê lượt chơi" },
            { "GameplaySession", "Tạm dừng và kết thúc phiên chơi" },
            { "CampaignLifecycle", "Lưu và khôi phục tiến trình" },
            { "DevFastStart", "Chọn màn và boss để thử nhanh" },
            { "HazardStreamSchedule", "Lập lịch sinh thiên thạch" },
            { "HazardStreamDefinition", "Cấu hình dòng thiên thạch" },
            { "RewardDirector", "Sinh và phân phối phần thưởng" },
            { "RewardPickup", "Vật phẩm rơi và phát hiện nhặt" },
            { "ScenePrefabPool", "Kho tái sử dụng địch và vật phẩm" },
            { "EntryHoldFlight", "Đường bay và đội hình địch" },
            { "EncounterFireCoordinator", "Phối hợp nhịp bắn của địch" },
            { "LevelFlow", "Luồng màn, hướng chơi và camera" },
            { "GameShell", "Menu và điều hướng trò chơi" },
            { "GameplayHud", "Hiển thị mạng, súng, điểm và nút chơi" },
            { "BossHud", "Hiển thị máu và giai đoạn boss" },
            { "IPlayerProjectileTarget", "Giao diện mục tiêu nhận đạn người chơi" },
            { "IEnemyProjectileTarget", "Giao diện tàu nhận đạn địch" },
            { "PickupDefinition", "Cấu hình vật phẩm" },
            { "RewardDropTable", "Bảng chọn vật phẩm rơi" },
            { "WeightedPickup", "Vật phẩm và trọng số" },
            { "AsteroidVariant", "Biến thể thiên thạch" },
            { "Overdrive", "Tăng tốc độ bắn tạm thời" },
            { "ShieldCell", "Khiên hấp thụ một hit" },
            { "ExtraLife", "Thêm một mạng" },
            { "MissileAmmo", "Thêm tên lửa dự trữ" },
            { "StellarShard", "Shard tích lũy" },
            { "PulseCore", "Lõi nâng súng xung" },
            { "PlasmaCore", "Lõi nâng súng plasma" },
            { "HomingCore", "Lõi nâng tên lửa bám" },
            { "NormalDrops", "Bảng rơi địch thường" },
            { "ResourceDrops", "Bảng rơi tài nguyên" },
            { "SupplyCrateDrops", "Bảng thưởng thùng tiếp tế" },
            { "Mission02StormStream", "Dòng thiên thạch nhiệm vụ 2" },
            { "Mission04ResourceStormStream", "Dòng thiên thạch nhiệm vụ 4" },
            { "StormAsteroid", "Thiên thạch nguy hiểm" },
            { "ResourceAsteroid", "Thiên thạch tài nguyên" },
            { "Authored", "Phần thưởng bố trí sẵn" },
            { "OverdriveOpportunity", "Cơ hội nhận tăng tốc bắn" },
            { "ShieldCellOpportunity", "Cơ hội nhận khiên" },
            { "ExtraLifeOpportunity", "Cơ hội nhận thêm mạng" },
            { "MissileAmmoOpportunity", "Cơ hội nhận tên lửa dự trữ" },
            { "SupportDroneOpportunity", "Cơ hội nhận tàu hỗ trợ" },
            { "PulseCoreOpportunity", "Cơ hội nhận lõi súng xung" },
            { "PlasmaCoreOpportunity", "Cơ hội nhận lõi súng plasma" },
            { "HomingCoreOpportunity", "Cơ hội nhận lõi tên lửa bám" }
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
