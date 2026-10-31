// VietnameseEditorLabels — Chú thích tiếng Việt trong cây đối tượng và cửa sổ Project; giữ nguyên tên và GUID.
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
            { "Art", "Hình ảnh và sprite" },
            { "Ships", "Hình tàu" },
            { "Bosses", "Trùm" },
            { "Pickups", "Hình vật phẩm" },
            { "Environments", "Hình nền và môi trường" },
            { "SFX", "Hiệu ứng âm thanh" },
            { "Music", "Nhạc nền" },
            { "Fixtures", "Dữ liệu thử nghiệm" },
            { "Levels", "Nhiệm vụ thử nghiệm" },
            { "RegionAFlights", "Đường bay thử khu vực A" },
            { "Cosmetics", "Ngoại hình tàu" },
            { "Combat", "Đạn và cấu hình chiến đấu" },
            { "Attacks", "Đòn đánh của địch" },
            { "Presentation", "Hiệu ứng hiển thị của địch" },
            { "Gameplay", "Luồng chơi, camera và kho tái sử dụng" },
            { "Persistence", "Lưu tiến độ và hồ sơ" },
            { "UI", "Giao diện" },
            { "Fonts", "Phông chữ" },
            { "VFX", "Hiệu ứng hình ảnh" },
            { "Editor", "Công cụ chỉ chạy trong Unity Editor" },
            { "Bomber", "Địch bắn xòe" },
            { "Bulwark", "Địch hạng nặng" },
            { "Carrier", "Tàu mẹ phóng quân" },
            { "Interceptor", "Địch lao đánh" },
            { "Raider", "Địch tập kích" },
            { "Sniper", "Địch bắn tia ngắm" },
            { "SupplyDrone", "Tàu tiếp tế" },
            { "WardenDrone", "Quân hộ tống Warden" },
            { "VanguardCurtainProjectile", "Đạn màn chắn Vanguard" },
            { "02_Gameplay", "Cảnh chơi chính" },
            { "GameplayViewSettingsDrawer", "Vẽ ô chỉnh độ thu phóng và vùng di chuyển" },
            { "EncounterConfigValidation", "Kiểm tra cấu hình nhiệm vụ và chặn Play khi dữ liệu đợt giao chiến không hợp lệ" },
            { "RegionAAssetPaths", "Tra đường dẫn dữ liệu RegionA cũ sang thư mục hiện tại cho công cụ Editor" },
            { "DevFastStartEditor", "Giao diện chọn nhiệm vụ, điểm bắt đầu và tùy chọn chạy thử trong Unity Editor" },
            { "VietnameseInspector", "Nhãn và gợi ý tiếng Việt cho các Inspector cấu hình và component được hỗ trợ" },
            { "VietnameseEditorLabels", "Chú thích tiếng Việt trong cây đối tượng và cửa sổ Project" },
            { "PlayerPointerInput", "Giao diện nguồn thao tác kéo và giữ bắn để movement và weapon cùng đọc" },
            { "PlayerDamagePresentation", "Hiện chớp trúng đòn và trạng thái bảo vệ của thân tàu" },
            { "PlayerEffectPresentation", "Hiện khiên theo hiệu ứng tạm thời đang có của người chơi" },
            { "EmergencyMissilePresentation", "Hiện tên lửa dự trữ bay và nổ theo sự kiện" },
            { "CosmeticCatalog", "Danh mục tàu, sơn và vệt" },
            { "ShipAppearance", "Áp tàu, sơn và vệt đã trang bị" },
            { "PlayerCombatPresentation", "Hiện hiệu ứng chiến đấu của tàu theo bắn súng, nhặt vật phẩm và trạng thái bảo vệ" },
            { "PlayerDeathPresentation", "Hiện nổ và mảnh vỡ khi mất mạng" },
            { "WeaponBranch", "Các nhánh vũ khí dùng chung khi chọn súng, nâng cấp và xác định loại đạn" },
            { "EnemyModifierDefinition", "Cấu hình biến thể địch như Elite: hệ số máu, nhịp bắn, kích thước và màu nhấn" },
            { "EscortSpawner", "Sinh và theo dõi quân hộ tống qua EncounterRunner" },
            { "EnemyHitInfo", "Thông tin điểm trúng, hướng va chạm và loại đạn để hiệu ứng hiển thị đúng cú đánh" },
            { "EnemyFireGate", "Cho phép hoặc khóa địch bắn, chờ an toàn và thu hồi đạn khi luồng chơi yêu cầu" },
            { "PlanetJourney", "Điều phối hành trình sang hành tinh: tăng tốc, đổi hướng, chờ chạm và tiếp cận" },
            { "PlayDirection", "Hướng chơi và phép đổi tọa độ để đường bay hoạt động theo trục màn hình" },
            { "RegionCampaignFlow", "Nối các nhiệm vụ và chọn chuyển màn trong vùng hoặc hành trình sang vùng mới" },
            { "SettingsFeedback", "Áp âm lượng và tùy chọn rung từ cài đặt người chơi" },
            { "SpriteFrameLoop", "Phát lặp các khung hình sprite và đặt lại khi đối tượng được tái sử dụng" },
            { "EncounterCleanup", "Đăng ký và thu hồi đối tượng theo đợt" },
            { "FlightSequence", "Cấu hình các chặng đường bay của địch và kiểm tra thông số" },
            { "FormationPlan", "Cấu hình vị trí các ô đội hình" },
            { "LevelDefinition", "Cấu hình nhiệm vụ, danh sách đợt, camera và điểm bắt đầu để chơi hoặc thử nhanh" },
            { "SafeAreaLayout", "Đặt giao diện trong vùng an toàn màn hình để tránh tai thỏ và mép thiết bị" },
            { "EncounterCuePresentation", "Hiện thông báo đợt và cơ hội vật phẩm theo sự kiện từ EncounterRunner" },
            { "GameplayAnnouncement", "Hiện thông báo lớn hoặc nhỏ khi chơi, giữ theo thời lượng rồi ẩn" },
            { "ShellCopy", "Nội dung chữ, thông báo lỗi và định dạng số liệu của menu, kết quả và cửa hàng" },
            { "ShellView", "Giữ liên kết các nút và màn giao diện" },
            { "ShellStarfield", "Cuộn nền sao của menu bằng thời gian thực để vẫn chạy khi game tạm dừng" },
            { "UiButtonMotion", "Phóng nhẹ nút khi rê chuột hoặc được chọn và thu nhỏ khi nhấn, kể cả lúc tạm dừng" },
            { "ShipPreview", "Hiện tàu, sơn, động cơ và vệt xem trước trong menu hoặc cửa hàng" },
            { "GameplayCopy", "Nội dung chữ và định dạng hiển thị dùng chung cho HUD khi chơi" },
            { "MissionIntroPresentation", "Hiện nền, vệt tốc độ và chuyển cảnh lúc vào nhiệm vụ hoặc di chuyển giữa các màn" },
            { "TravelBackdrop", "Cuộn nền lặp và hòa hai lớp cảnh trong hành trình" },
            { "EncounterRewardQuota", "Theo dõi mức thưởng tối thiểu của đợt qua số địch bị hạ hoặc thoát" },
            { "RewardRoller", "Chọn vật phẩm theo trọng số, trạng thái người chơi và cơ chế hỗ trợ khi thiếu phòng thủ" },
            { "JsonSaveStore", "Đọc và ghi bản lưu JSON, giữ bản dự phòng và xử lý dữ liệu lỗi để tránh mất tiến độ" },
            { "CampaignProgress", "Xử lý tiến trình, kỷ lục và phần thưởng trên dữ liệu lưu" },
            { "SaveData", "Các cấu trúc dữ liệu lưu: chiến dịch, hồ sơ, cài đặt và thống kê" },
            { "SaveValidation", "Kiểm tra cấu trúc, giá trị và checkpoint trước khi dữ liệu lưu được sử dụng" },
            { "AudioPalette", "Cấu hình nhạc, clip hiệu ứng và giới hạn phát" },
            { "AudioDirector", "Phát nhạc và hiệu ứng âm thanh" },
            { "PlayerAudioPresentation", "Phát âm thanh súng, nhặt vật phẩm, mất mạng và tên lửa của người chơi" },
            { "EnemyAudioPresentation", "Phát âm báo đòn, tấn công và diễn biến boss theo sự kiện chiến đấu" },
            { "GameAudioPresentation", "Đổi nhạc và phát âm thanh theo lượt chơi, hạ địch, hành trình và kết quả" },
            { "AttackBeamPresentation", "Vẽ vùng báo trước, tia và hướng quét" },
            { "DirectedAttack", "Chạy đòn lao hoặc tia có hướng" },
            { "DirectedAttackDefinition", "Cấu hình đòn lao hoặc tia: thời gian báo trước, bám hướng, kích thước và thời gian hoạt động" },
            { "AttackPhase", "Các bước chuẩn bị, khóa mục tiêu, ra đòn và hồi để hiệu ứng đọc trạng thái tấn công" },
            { "BeamDamageQuery", "Kiểm tra tàu trong vùng tia và chuyển va chạm sang bộ nhận sát thương người chơi" },
            { "BossAttackModule", "Thực hiện một đòn của boss hoặc chuyển cho bộ xử lý đòn tương ứng" },
            { "BossBarrage", "Chạy màn đạn hoặc vùng nguy hiểm của boss, gồm báo trước và khe tránh theo cấu hình" },
            { "BossBarrageDefinition", "Cấu hình màn đạn và vùng nguy hiểm của boss" },
            { "BossDeathSequence", "Chạy các nhịp nổ khi boss bị hạ và báo hoàn tất để luồng đợt tiếp tục" },
            { "BossPhaseDefinition", "Cấu hình giai đoạn boss, ngưỡng máu và thứ tự các đòn đánh" },
            { "BossShieldNetwork", "Bảo vệ lõi boss theo trạng thái các nút khiên và điều khiển hình khiên" },
            { "AsteroidDestructionPresentation", "Phát hiệu ứng vỡ thiên thạch bằng pool khi nhận sự kiện bị phá hủy" },
            { "AsteroidVisualMotion", "Xoay hình thiên thạch và đặt lại khi tái sử dụng" },
            { "EnemyDamagePresentation", "Đổi màu hoặc khung hình thân địch theo mức máu còn lại" },
            { "EnemyEntryTelegraph", "Hiện dấu báo địch sắp vào vùng chơi theo đường bay" },
            { "EnemyModifierPresentation", "Hiện kích thước và màu nhấn của biến thể địch" },
            { "EnemyHitPresentation", "Hiện chớp trúng đạn và hiệu ứng va chạm dựa trên thông tin cú đánh" },
            { "EnemyFirePresentation", "Hiện báo bắn và chớp nòng theo trạng thái tấn công của địch" },
            { "DirectedAttackTelegraph", "Vẽ dấu báo ngắm và hướng lao của địch theo trạng thái đòn có hướng" },
            { "StagedDestructionPresentation", "Hiện nổ từng cụm và mảnh văng cho địch lớn" },
            { "CarrierBayPresentation", "Hiện cửa khoang và hiệu ứng phóng quân của tàu mẹ theo chu kỳ hộ tống" },
            { "BossSkillPhasePresentation", "Hiện từng bước chuẩn bị, khóa, đánh và hồi của kỹ năng boss" },
            { "BossArmorBreakPresentation", "Hiện giáp boss nứt vỡ theo máu và phát âm thanh khi vỡ giáp" },
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

        // Ưu tiên chú thích tên riêng; cấu hình chưa có nhãn riêng được giải thích bằng loại asset.
        private static void DrawProject(string guid, Rect rect)
        {
            if (Event.current.type != EventType.Repaint || rect.height > 20) return;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/StarShift/")) return;
            var label = ProjectLabel(path);
            if (label != null) DrawSuffix(rect, Path.GetFileNameWithoutExtension(path), label);
        }

        // Tên riêng có ưu tiên; dùng loại cấu hình làm chú thích cho asset chưa có nhãn riêng.
        internal static string ProjectLabel(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (Names.TryGetValue(name, out var label)) return label;
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type)) return null;
            return Names.TryGetValue(type.Name, out label) ? label : null;
        }

        private static void DrawSuffix(Rect rect, string name)
        {
            if (Names.TryGetValue(name, out var label)) DrawSuffix(rect, name, label);
        }

        private static void DrawSuffix(Rect rect, string name, string label)
        {
            // Chỉ thêm chú thích vào khoảng trống; giữ tên, thao tác chọn và GUID gốc.
            rect.xMin += 18 + EditorStyles.label.CalcSize(new GUIContent(name)).x;
            if (rect.width > 24) GUI.Label(rect, new GUIContent(" · " + label, label + " · " + name), EditorStyles.miniLabel);
        }
    }
}
