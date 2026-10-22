using System;
using System.Linq;

public static class VerifyVietnameseInspector
{
    public static object Edit()
    {
        bool sceneWasDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
        var editorAssembly = typeof(StarShift.Editor.DevFastStartEditor).Assembly;
        var labels = editorAssembly.GetType("StarShift.Editor.VietnameseInspectorLabels");
        var labelMethod = labels.GetMethod("Label", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var inputs = new[] {
            new[] { "Assets/StarShift/Data/Weapons/PulseLaser.asset", "levels.Array.data[0].FireInterval", "Chờ giữa hai loạt bắn (giây)" },
            new[] { "Assets/StarShift/Data/Weapons/PulseLaserThermal.asset", "heatPerSecond", "Nhiệt tăng mỗi giây" },
            new[] { "Assets/StarShift/Data/Enemies/Scout.asset", "maxHp", "Máu tối đa" },
            new[] { "Assets/StarShift/Data/RegionA/Encounters/Mission01Foundation.asset", "Groups.Array.data[0].Count", "Số lượng địch" }
        };
        foreach (var input in inputs)
        {
            var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(input[0]);
            if (asset == null) throw new System.Exception("Missing asset: " + input[0]);
            var before = UnityEditor.EditorJsonUtility.ToJson(asset);
            var so = new UnityEditor.SerializedObject(asset);
            var property = so.FindProperty(input[1]);
            if (property == null) throw new System.Exception("Missing property: " + input[1]);
            var label = (UnityEngine.GUIContent)labelMethod.Invoke(null, new object[] { property });
            if (label.text != input[2] || !label.tooltip.Contains(property.name)) throw new System.Exception("Wrong label: " + input[1]);
            var editor = UnityEditor.Editor.CreateEditor(asset);
            try { if (!(editor is StarShift.Editor.VietnameseInspector)) throw new System.Exception("Wrong custom editor: " + input[0]); }
            finally { UnityEngine.Object.DestroyImmediate(editor); }
            if (before != UnityEditor.EditorJsonUtility.ToJson(asset)) throw new System.Exception("Asset changed: " + input[0]);
        }
        var player = UnityEngine.GameObject.Find("Player");
        var systems = UnityEngine.GameObject.Find("EncounterSystems");
        if (player == null || systems == null) throw new System.Exception("Scene objects renamed or missing");
        var dev = systems.GetComponent<StarShift.Dev.DevFastStart>();
        var devSo = new UnityEditor.SerializedObject(dev);
        var devLabel = (UnityEngine.GUIContent)labelMethod.Invoke(null, new object[] { devSo.FindProperty("devModeEnabled") });
        if (devLabel.text != "Bật chế độ thử nghiệm") throw new System.Exception("Dev label missing");
        var clone = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<StarShift.Combat.WeaponThermalProfile>("Assets/StarShift/Data/Weapons/PulseLaserThermal.asset"));
        try
        {
            var so = new UnityEditor.SerializedObject(clone);
            var original = clone.HeatPerSecond;
            so.FindProperty("heatPerSecond").floatValue = original + 1;
            so.ApplyModifiedProperties();
            if (clone.HeatPerSecond != original + 1) throw new System.Exception("Serialized editing broken");
        }
        finally { UnityEngine.Object.DestroyImmediate(clone); }
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty != sceneWasDirty) throw new System.Exception("Scene dirty state changed");

        var weaponAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/StarShift/Data/Weapons/PulseLaser.asset");
        var weaponSo = new UnityEditor.SerializedObject(weaponAsset);
        var indexed = typeof(StarShift.Editor.VietnameseFieldsDrawer).GetMethod("IndexedLabel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        for (int i = 0; i < 5; i++) {
            var element = weaponSo.FindProperty("levels").GetArrayElementAtIndex(i);
            var display = (UnityEngine.GUIContent)indexed.Invoke(null, new object[] { element, "Cấp", UnityEngine.GUIContent.none });
            if (display.text != "Cấp " + (i + 1)) throw new System.Exception("Wrong level display");
        }
        foreach (var fieldName in new[] {"damage", "fireInterval"}) {
            var display = (UnityEngine.GUIContent)labelMethod.Invoke(null, new object[] { weaponSo.FindProperty(fieldName) });
            if (!display.text.Contains("dự phòng")) throw new System.Exception("Fallback label unclear");
        }
        UnityEditor.Selection.activeObject = weaponAsset;
        UnityEditor.EditorApplication.RepaintProjectWindow();
        var splashLabel = (UnityEngine.GUIContent)labelMethod.Invoke(null, new object[] { weaponSo.FindProperty("levels").GetArrayElementAtIndex(0).FindPropertyRelative("SplashRadius") });
        if (!splashLabel.tooltip.Contains("0: không gây sát thương lan")) throw new System.Exception("Splash tooltip missing");
        var encounter = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/StarShift/Data/RegionA/Encounters/Mission01Foundation.asset"));
        var completionLabel = (UnityEngine.GUIContent)labelMethod.Invoke(null, new object[] { encounter.FindProperty("Completion") });
        if (!completionLabel.tooltip.Contains("Survive = sống sót")) throw new System.Exception("Completion glossary missing");
        var cameraAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<StarShift.Gameplay.CameraProfile>("Assets/StarShift/Data/Cameras/Normal.asset");
        var cameraSo = new UnityEditor.SerializedObject(cameraAsset);
        var drawer = new StarShift.Editor.GameplayViewSettingsDrawer();
        var ui = drawer.CreatePropertyGUI(cameraSo.FindProperty("View"));
        var fields = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.FloatField>(ui).ToList();
        if (fields.Count != 5 || fields.Any(f => !f.isDelayed)) throw new System.Exception("Camera delayed fields changed");
        if (fields[0].label != "Độ thu phóng (Size)" || !fields[0].tooltip.Contains("Size × 2")) throw new System.Exception("Camera zoom explanation missing");
        var camera = UnityEngine.Camera.main;
        foreach (var component in new UnityEngine.Object[] { camera.GetComponent<StarShift.Gameplay.GameplayViewController>(), camera.GetComponent<StarShift.Gameplay.FrameRateLimit>(), camera.GetComponent<StarShift.Gameplay.PlayableAreaPresentation>() }) {
            var inspector = UnityEditor.Editor.CreateEditor(component);
            try { if (!(inspector is StarShift.Editor.VietnameseInspector)) throw new System.Exception("Camera inspector not localized"); }
            finally { UnityEngine.Object.DestroyImmediate(inspector); }
        }
        return new { status = "PASS", assets = 4, levels = 5, fallbackLabels = 2, cameraDelayedFields = fields.Count, sceneDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty };
    }

    public static object Data()
    {
        bool sceneWasDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
        var assembly = typeof(StarShift.Editor.DevFastStartEditor).Assembly;
        var labelMethod = assembly.GetType("StarShift.Editor.VietnameseInspectorLabels").GetMethod("Label", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var translate = assembly.GetType("StarShift.Editor.VietnameseEditorLabels").GetMethod("Translate", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        int assetCount = 0, fieldCount = 0;
        foreach (var assetType in new[] { "PickupDefinition", "RewardDropTable", "HazardStreamDefinition" })
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:" + assetType, new[] { "Assets/StarShift/Data" });
            if (guids.Length == 0) throw new System.Exception("Missing data type: " + assetType);
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                string before = UnityEditor.EditorJsonUtility.ToJson(asset);
                var editor = UnityEditor.Editor.CreateEditor(asset);
                try { if (!(editor is StarShift.Editor.VietnameseInspector)) throw new System.Exception("Wrong inspector: " + path); }
                finally { UnityEngine.Object.DestroyImmediate(editor); }
                var property = new UnityEditor.SerializedObject(asset).GetIterator();
                bool enter = true;
                while (property.NextVisible(enter))
                {
                    enter = false;
                    if (property.name == "m_Script") continue;
                    var label = (UnityEngine.GUIContent)labelMethod.Invoke(null, new object[] { property });
                    if (label.text == property.displayName || !label.tooltip.Contains(property.name))
                        throw new System.Exception("Untranslated field: " + path + ":" + property.propertyPath);
                    fieldCount++;
                }
                if (before != UnityEditor.EditorJsonUtility.ToJson(asset)) throw new System.Exception("Data mutated: " + path);
                assetCount++;
            }
        }
        var inputs = new[] {
            new[] { "Assets/StarShift/Data/Rewards/Overdrive.asset", "FireRateMultiplier", "Hệ số tăng tốc độ bắn" },
            new[] { "Assets/StarShift/Data/Rewards/Overdrive.asset", "Duration", "Thời gian hiệu lực buff (giây)" },
            new[] { "Assets/StarShift/Data/Rewards/NormalDrops.asset", "MajorItems.Array.data[0].Weight", "Trọng số lựa chọn" },
            new[] { "Assets/StarShift/Data/RegionA/Hazards/Mission02StormStream.asset", "Count", "Số thiên thạch tối đa" },
            new[] { "Assets/StarShift/Data/RegionA/Hazards/Mission02StormStream.asset", "Variants.Array.data[0].Hp", "Máu biến thể" }
        };
        foreach (var input in inputs)
        {
            var so = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadMainAssetAtPath(input[0]));
            var property = so.FindProperty(input[1]);
            if (property == null) throw new System.Exception("Missing field: " + input[1]);
            var label = (UnityEngine.GUIContent)labelMethod.Invoke(null, new object[] { property });
            if (label.text != input[2]) throw new System.Exception("Wrong field label: " + input[1]);
        }
        foreach (var name in new[] { "Overdrive", "NormalDrops", "Mission02StormStream", "PickupDefinition", "RewardDropTable" })
            if ((string)translate.Invoke(null, new object[] { name }) == name) throw new System.Exception("Missing Project label: " + name);
        foreach (var drawerType in new[] { typeof(StarShift.Editor.WeightedPickupVietnameseDrawer), typeof(StarShift.Editor.AsteroidVariantVietnameseDrawer) })
            if (drawerType.GetCustomAttributes(typeof(UnityEditor.CustomPropertyDrawer), false).Length != 1)
                throw new System.Exception("Missing nested drawer: " + drawerType.Name);
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty != sceneWasDirty) throw new System.Exception("Scene state changed");
        UnityEditor.EditorApplication.RepaintProjectWindow();
        return new { status = "PASS", assets = assetCount, fields = fieldCount, nestedChecks = inputs.Length, drawers = 2 };
    }
}
