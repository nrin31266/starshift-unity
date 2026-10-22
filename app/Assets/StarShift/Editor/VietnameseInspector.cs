using System.Collections.Generic;
using StarShift.Combat;
using StarShift.Dev;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEditor;
using UnityEngine;

namespace StarShift.Editor
{
    internal static class VietnameseInspectorLabels
    {
        private static readonly Dictionary<string, string> Fields = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "targetView", "Khung nhìn mục tiêu" },
            { "transitionDuration", "Thời gian đổi khung nhìn (giây)" },
            { "reserveImpactDuration", "Thời gian rung tên lửa (giây)" },
            { "reserveImpactStrength", "Cường độ rung tên lửa" },
            { "targetFrameRate", "Giới hạn FPS mục tiêu" },
            { "movement", "Điều khiển di chuyển" },
            { "hintColor", "Màu báo giới hạn di chuyển" },
            { "holdSeconds", "Chờ hiện báo giới hạn (giây)" },
            { "segmentLength", "Độ dài vạch báo (tỉ lệ màn hình)" },
            { "definition", "Cấu hình" },
            { "pointerInput", "Bộ nhận chuột / cảm ứng" },
            { "muzzle", "Nòng súng" },
            { "sensitivity", "Độ nhạy kéo" },
            { "gameplayCamera", "Camera gameplay" },
            { "movementCamera", "Camera dùng để di chuyển" },
            { "footprintSize", "Kích thước tàu dùng chặn biên" },
            { "weapons", "Các loại vũ khí" },
            { "maximumLives", "Số mạng tối đa" },
            { "enemyFireGate", "Bộ cho phép địch bắn" },
            { "deathDuration", "Thời gian pha chết (giây)" },
            { "invincibilityDuration", "Bất tử sau hồi sinh (giây)" },
            { "enemyFireGrace", "Địch chờ bắn sau hồi sinh (giây)" },
            { "shieldBreakInvincibilityDuration", "Bất tử sau vỡ khiên (giây)" },
            { "branch", "Loại vũ khí" },
            { "levels", "Thông số từng cấp súng" },
            { "projectilePrefab", "Mẫu viên đạn" },
            { "fireInterval", "Chờ giữa hai loạt bắn (giây)" },
            { "projectileSpeed", "Tốc độ đạn" },
            { "damage", "Sát thương" },
            { "projectileLifetime", "Thời gian sống của đạn (giây)" },
            { "thermalProfile", "Thông số nóng / nguội" },
            { "ProjectileCount", "Số viên mỗi loạt" },
            { "SpreadDegrees", "Góc xòe (độ)" },
            { "LateralSpacing", "Khoảng cách ngang giữa viên đạn" },
            { "CenterBonus", "Sát thương thêm của viên giữa" },
            { "SplashRadius", "Bán kính sát thương lan" },
            { "projectileColor", "Màu viên đạn" },
            { "projectileScale", "Kích thước hình viên đạn" },
            { "projectileSprite", "Hình viên đạn" },
            { "trailSeconds", "Thời gian vệt đạn (giây)" },
            { "trackingDegreesPerSecond", "Tốc độ đổi hướng bám địch (độ/giây)" },
            { "trackingRange", "Tầm tìm mục tiêu" },
            { "trackingLaunchSeconds", "Thời gian cong lúc phóng (giây)" },
            { "trackingLaunchArcDegrees", "Độ cong lúc phóng (độ)" },
            { "capacity", "Mức nhiệt gây quá nhiệt" },
            { "heatPerSecond", "Nhiệt tăng mỗi giây" },
            { "coolingDelay", "Chờ trước khi nguội (giây)" },
            { "coolRate", "Tốc độ nguội thường" },
            { "overheatCoolRate", "Tốc độ nguội khi quá nhiệt" },
            { "maxHp", "Máu tối đa" },
            { "score", "Điểm khi hạ địch" },
            { "bodyDamagesPlayer", "Thân địch gây mất mạng" },
            { "missileTier", "Loại địch khi nhận tên lửa" },
            { "initialDelay", "Chờ trước phát đầu (giây)" },
            { "telegraphDuration", "Thời gian báo đòn (giây)" },
            { "cooldown", "Thời gian hồi đòn (giây)" },
            { "burstCount", "Số loạt trong chuỗi" },
            { "burstInterval", "Chờ giữa hai loạt bắn trong chuỗi (giây)" },
            { "Id", "Mã định danh" },
            { "Completion", "Điều kiện hoàn thành" },
            { "CompleteWhenScheduleExhausted", "Kết thúc khi hết lịch và sạch địch" },
            { "EntryBeats", "Nhịp địch vào" },
            { "EntryLeadSeconds", "Thời gian dẫn vào (giây)" },
            { "TitleSeconds", "Thời gian hiện tiêu đề (giây)" },
            { "HazardStream", "Đợt chướng ngại liên tục" },
            { "ScoutPrefab", "Mẫu địch dùng khi nhóm chưa chọn" },
            { "Duration", "Thời lượng (giây)" },
            { "Groups", "Các nhóm địch" },
            { "OptionalEvents", "Sự kiện phụ" },
            { "CameraOverride", "Camera riêng (trống = dùng chung)" },
            { "GuaranteedMajorCount", "Số vật phẩm lớn bảo đảm" },
            { "GuaranteedMajorByKill", "Mốc hạ địch bảo đảm vật phẩm" },
            { "GuaranteeTable", "Bảng vật phẩm bảo đảm" },
            { "ActiveShooterLimit", "Số địch bắn đồng thời" },
            { "ShotSpacing", "Khoảng giữa phát địch (giây)" },
            { "ShotsPerVolley", "Số phát trong một loạt của địch" },
            { "VolleyPause", "Nghỉ giữa loạt địch (giây)" },
            { "ClearPauseSeconds", "Nghỉ sau dọn đợt (giây)" },
            { "HandoffTravelSeconds", "Thời gian bay chuyển đợt (giây)" },
            { "HandoffDelay", "Trễ chuyển đợt dự phòng (giây)" },
            { "ProjectileGraceSeconds", "Thời gian chờ đạn (giây)" },
            { "ProjectileFadeSeconds", "Thời gian mờ đạn (giây)" },
            { "TransitionTitle", "Tiêu đề chuyển cảnh" },
            { "TransitionDescription", "Mô tả chuyển cảnh" },
            { "RewardGraceSeconds", "Thời gian chờ vật phẩm (giây)" },
            { "BossPostDeathSeconds", "Chờ sau boss chết (giây)" },
            { "EntryBeat", "Nhịp vào" },
            { "SpawnScale", "Tỉ lệ địch khi sinh" },
            { "VisualSpin", "Tốc độ xoay hình" },
            { "ContinuousTransit", "Bay xuyên liên tục" },
            { "Count", "Số lượng địch" },
            { "StartTime", "Thời điểm bắt đầu (giây)" },
            { "Stagger", "Khoảng giữa hai địch (giây)" },
            { "EnemyPrefab", "Mẫu loại địch" },
            { "EnemyDefinition", "Cấu hình loại địch" },
            { "RewardOverride", "Bảng thưởng riêng" },
            { "Elite", "Cấu hình địch tinh nhuệ" },
            { "Required", "Mục tiêu bắt buộc" },
            { "GrantsRewards", "Cho phép thưởng" },
            { "Formation", "Kiểu đội hình" },
            { "Anchor", "Vị trí đặt đội hình" },
            { "SlotStep", "Khoảng cách vị trí" },
            { "FormationRise", "Độ cao đội hình" },
            { "Sequence", "Các bước bay vào / giữ / rời" },
            { "Plan", "Bố trí đội hình" },
            { "FirstSlot", "Vị trí bắt đầu trong đội hình" },
            { "HpOverride", "Máu riêng của nhóm (0 = dùng gốc)" },
            { "View", "Khung nhìn và vùng chơi" },
            { "TransitionSeconds", "Thời gian đổi camera (giây)" },
            { "devModeEnabled", "Bật chế độ thử nghiệm" },
            { "startLevel", "Nhiệm vụ bắt đầu" },
            { "startPoint", "Điểm bắt đầu" },
            { "standalone", "Chỉ chạy đợt đã chọn" },
            { "skipIntro", "Bỏ qua giới thiệu" },
            { "startingWeaponLevel", "Cấp súng bắt đầu" },
            { "unlockAllWeapons", "Mở mọi loại súng" },
            { "overrideDirection", "Ghi đè hướng chơi" },
            { "direction", "Hướng chơi" },
            { "deterministicRewards", "Lặp lại cùng kết quả vật phẩm" },
            { "rewardSeed", "Mã tái hiện ngẫu nhiên" },
            { "showBounds", "Hiện vùng giới hạn" },
            { "showPaths", "Hiện đường bay" },
            { "showSpawnPoints", "Hiện điểm sinh địch" },
            { "showHitboxes", "Hiện vùng va chạm" },
            { "showFps", "Hiện FPS" },
            { "showContentLabels", "Hiện nhãn nội dung" },
        };

        private static readonly Dictionary<string, string> Hints = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Completion", "Eliminate = hạ các mục tiêu bắt buộc; Timeline = đợt theo lịch; Survive = sống sót đủ thời gian; Boss = đợt trùm. Điều kiện chi tiết do EncounterRunner xử lý." },
            { "Formation", "Line = hàng ngang; Column = hàng dọc; V = chữ V; Wedge = mũi nêm; Ring = vòng tròn. Nếu có Plan, vị trí trong Plan được ưu tiên." },
            { "Plan", "Danh sách vị trí của các địch trong đội hình. Khi được gán, dùng các vị trí này thay cho công thức Formation." },
            { "Sequence", "Tài nguyên mô tả lần lượt các bước bay: vào màn hình, di chuyển, giữ vị trí, giao chiến và rời màn hình." },
            { "Required", "Đánh dấu mục tiêu phải xử lý để hoàn thành các đợt yêu cầu tiêu diệt. Không có nghĩa địch nào cũng bắt buộc ở mọi chế độ." },
            { "Stagger", "Khoảng chờ giữa hai địch trong cùng nhóm. Ví dụ 0.3: các địch xuất hiện cách nhau 0.3 giây." },
            { "HpOverride", "0: lấy máu từ cấu hình địch. Lớn hơn 0: dùng máu riêng cho nhóm; hệ số của biến thể tinh nhuệ vẫn có thể áp dụng." },
            { "ActiveShooterLimit", "Giới hạn số địch được phối hợp bắn đồng thời; không phải giới hạn tổng số địch xuất hiện." },
            { "GuaranteedMajorByKill", "Mốc hạ địch hợp lệ muộn nhất cho vật phẩm lớn bảo đảm đầu tiên. 0: dùng cách chọn seeded thông thường; còn phụ thuộc cấu hình bảo đảm." },
            { "fireInterval", "Thời gian chờ giữa hai loạt bắn. Số nhỏ hơn thì bắn nhanh hơn; ví dụ 0.2 giây tương đương khoảng 5 loạt mỗi giây." },
            { "SplashRadius", "Vùng gây sát thương lan quanh mục tiêu trúng đạn. 0: không gây sát thương lan." },
            { "projectileScale", "Chỉ thay kích thước hình viên đạn; không tự thay kích thước vùng va chạm." },
            { "footprintSize", "Kích thước dùng để giữ toàn bộ tàu trong vùng chơi. Không phải vùng nhận sát thương của tàu." },
            { "sensitivity", "1: tàu đi theo cùng số pixel được kéo. Tăng để tàu đi xa hơn với cùng một đoạn kéo." },
            { "telegraphDuration", "Thời gian địch báo trước đòn đánh để người chơi có cơ hội né; không phải thời gian đạn bay." },
            { "cooldown", "Khoảng nghỉ để địch chuẩn bị đòn tiếp theo; khác với thời gian báo đòn." },
            { "rewardSeed", "Cùng mã và cùng chuỗi xử lý ngẫu nhiên giúp tái hiện kết quả vật phẩm khi thử lỗi. Không phải xác suất rơi vật phẩm." },
            { "invincibilityDuration", "Sau khi tàu bay vào lại, tạm chặn sát thương nhưng vẫn cho điều khiển và bắn." },
            { "enemyFireGrace", "Sau hồi sinh, địch chờ trước khi được bắn đòn mới. Đây là thời gian riêng, khác với thời gian tàu bất tử." },
            { "capacity", "Nhiệt chạm mức này sẽ khóa bắn. Tàu vẫn di chuyển; súng mở lại sau khi nguội về 0." },
            { "branch", "Pulse = súng xung; Plasma = súng xòe; Homing = tên lửa bám địch." },
            { "projectileLifetime", "Đạn hết thời gian này sẽ kết thúc và được thu hồi. Thời gian sống dài hơn không làm đạn bay nhanh hơn." },
        };

        internal static GUIContent Label(SerializedProperty property)
        {
            var text = Fields.TryGetValue(property.name, out var translated) ? translated : property.displayName;
            string hint = Hints.TryGetValue(property.name, out var explanation) ? explanation + "\n" + property.tooltip : property.tooltip;
            if (property.serializedObject.targetObject is PulseLaserDefinition && property.depth == 0 &&
                (property.name == "fireInterval" || property.name == "damage"))
            {
                text += " (dự phòng)";
                hint = "Chỉ dùng khi không có cấu hình cho cấp súng hiện tại. Thường hãy chỉnh bên trong Thông số từng cấp súng.";
            }
            return new GUIContent(text, text + "\n" + property.displayName + " (" + property.name + ")\n" + hint);
        }

        internal static void Field(SerializedProperty property)
            => EditorGUILayout.PropertyField(property, Label(property), true);
    }

    public abstract class VietnameseInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(VietnameseEditorLabels.Translate(target.GetType().Name) + " · " + target.GetType().Name + "\nRê chuột lên tên trường để xem tên gốc trong code.", MessageType.Info);
            if (target is GameplayViewController || target is CameraProfile)
                EditorGUILayout.HelpBox("Độ thu phóng (Size): tăng để nhìn rộng hơn, giảm để nhìn gần hơn. " +
                    "Size 6 tương ứng vùng nhìn cao 12 đơn vị world. Vùng di chuyển 0–1 là tỉ lệ màn hình, không phải tọa độ world. " +
                    "Trong Play Mode, chỉnh khung nhìn mục tiêu hoặc camera profile của đợt; controller sẽ điều khiển Camera Size.", MessageType.Info);
            if (target is PulseLaserDefinition)
                EditorGUILayout.HelpBox("Asset này chứa cấu hình toàn bộ cấp súng xung. " +
                    "Mỗi mục Cấp 1, Cấp 2… bên dưới là một cấp súng. Pulse = xung; Plasma = xòe; Homing = bám đích. " +
                    "Tốc độ bắn và sát thương ưu tiên cấu hình từng cấp; hai ô dự phòng ở cuối chỉ dùng khi cấp đó chưa có cấu hình.", MessageType.Info);
            var property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                    VietnameseInspectorLabels.Field(property);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    public abstract class VietnameseFieldsDrawer : PropertyDrawer
    {
        internal static GUIContent IndexedLabel(SerializedProperty property, string prefix, GUIContent fallback)
        {
            var path = property.propertyPath;
            int start = path.LastIndexOf('[');
            int end = start >= 0 ? path.IndexOf(']', start) : -1;
            return end > start && int.TryParse(path.Substring(start + 1, end - start - 1), out int index)
                ? new GUIContent(prefix + " " + (index + 1), "Vị trí mảng trong code: " + index) : fallback;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return height;
            var child = property.Copy();
            var end = property.GetEndProperty();
            if (child.NextVisible(true))
                while (!SerializedProperty.EqualContents(child, end))
                {
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);
                    if (!child.NextVisible(false)) break;
                }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            if (this is WeaponLevelPatternVietnameseDrawer) label = IndexedLabel(property, "Cấp", label);
            else if (this is SpawnActionVietnameseDrawer) label = IndexedLabel(property, "Nhóm địch", label);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;
                var child = property.Copy();
                var end = property.GetEndProperty();
                if (child.NextVisible(true))
                    while (!SerializedProperty.EqualContents(child, end))
                    {
                        row.y += row.height + EditorGUIUtility.standardVerticalSpacing;
                        row.height = EditorGUI.GetPropertyHeight(child, true);
                        EditorGUI.PropertyField(row, child, VietnameseInspectorLabels.Label(child), true);
                        if (!child.NextVisible(false)) break;
                    }
                EditorGUI.indentLevel = indent;
            }
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(WeaponLevelPattern))]
    public sealed class WeaponLevelPatternVietnameseDrawer : VietnameseFieldsDrawer { }
    [CustomPropertyDrawer(typeof(SpawnAction), true)]
    public sealed class SpawnActionVietnameseDrawer : VietnameseFieldsDrawer { }
    [CustomEditor(typeof(GameplayViewController)), CanEditMultipleObjects]
    public sealed class GameplayViewControllerVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(FrameRateLimit)), CanEditMultipleObjects]
    public sealed class FrameRateLimitVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(PlayableAreaPresentation)), CanEditMultipleObjects]
    public sealed class PlayableAreaPresentationVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(PulseLaserWeapon)), CanEditMultipleObjects]
    public sealed class PulseLaserWeaponVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(RelativeDragMovement)), CanEditMultipleObjects]
    public sealed class RelativeDragMovementVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(PlayerMovementBounds)), CanEditMultipleObjects]
    public sealed class PlayerMovementBoundsVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(PlayerLives)), CanEditMultipleObjects]
    public sealed class PlayerLivesVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(PlayerDamageLifecycle)), CanEditMultipleObjects]
    public sealed class PlayerDamageLifecycleVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(WeaponLoadout)), CanEditMultipleObjects]
    public sealed class WeaponLoadoutVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(EnemyHealth)), CanEditMultipleObjects]
    public sealed class EnemyHealthVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(EnemyDefinition)), CanEditMultipleObjects]
    public sealed class EnemyDefinitionVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(SingleFireDefinition)), CanEditMultipleObjects]
    public sealed class SingleFireDefinitionVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(PulseLaserDefinition)), CanEditMultipleObjects]
    public sealed class PulseLaserDefinitionVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(WeaponThermalProfile)), CanEditMultipleObjects]
    public sealed class WeaponThermalProfileVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(EncounterDefinition)), CanEditMultipleObjects]
    public sealed class EncounterDefinitionVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(CameraProfile)), CanEditMultipleObjects]
    public sealed class CameraProfileVietnameseEditor : VietnameseInspector { }
    [CustomEditor(typeof(EncounterDebugView)), CanEditMultipleObjects]
    public sealed class EncounterDebugViewVietnameseEditor : VietnameseInspector { }
}
