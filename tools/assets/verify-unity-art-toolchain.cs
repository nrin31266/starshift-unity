// Chỉ fixture kỹ thuật; không đặt PPU, pivot hoặc filter cho production.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.Exception("Run in Edit Mode");
var source = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".local/share/starshift/art-toolchain-checks/2026-10-20/helper-sheet.png");
if (!System.IO.File.Exists(source)) throw new System.Exception("Create the synthetic helper-sheet fixture first");
const string folder = "Assets/__StarShiftArtToolchainSmoke";
const string path = folder + "/sheet.png";
if (UnityEditor.AssetDatabase.IsValidFolder(folder) || System.IO.Directory.Exists(folder))
    throw new System.Exception("Scratch folder already exists; refusing overwrite");
UnityEditor.AssetDatabase.CreateFolder("Assets", "__StarShiftArtToolchainSmoke");
try
{
    System.IO.File.Copy(source, path, false);
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
    importer.spritePixelsPerUnit = 16;
    importer.filterMode = UnityEngine.FilterMode.Point;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.mipmapEnabled = false;
    importer.alphaIsTransparency = true;
    importer.SaveAndReimport();
    var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
    factory.Init();
    var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
    if (provider == null) throw new System.Exception("No sprite data provider");
    provider.InitSpriteEditorDataProvider();
    var edit = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if (edit == null) throw new System.Exception("No edit capability provider");
    var capability = edit.GetEditCapability();
    foreach (var required in new[] { UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite, UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect, UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName, UnityEditor.U2D.Sprites.EEditCapability.EditPivot })
        if (!capability.HasCapability(required)) throw new System.Exception("Unsupported capability: " + required);
    var ids = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
    if (ids == null) throw new System.Exception("No name/file-ID provider");
    var rects = new UnityEditor.SpriteRect[3];
    var pairs = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
    for (int i = 0; i < rects.Length; i++)
    {
        rects[i] = new UnityEditor.SpriteRect { name = "frame_" + i.ToString("D2"), rect = new UnityEngine.Rect((i % 2) * 16, 32 - (i / 2 + 1) * 16, 16, 16), alignment = UnityEngine.SpriteAlignment.Custom, pivot = new UnityEngine.Vector2(0.5f, 0.5f), spriteID = UnityEngine.GUID.Generate() };
        pairs.Add(new UnityEditor.SpriteNameFileIdPair(rects[i].name, rects[i].spriteID));
    }
    provider.SetSpriteRects(rects);
    ids.SetNameFileIdPairs(pairs);
    provider.Apply();
    importer.SaveAndReimport();
    var sprites = new System.Collections.Generic.Dictionary<string, UnityEngine.Sprite>();
    foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
        if (asset is UnityEngine.Sprite sprite) sprites.Add(sprite.name, sprite);
    if (sprites.Count != 3) throw new System.Exception("Wrong slice count");
    for (int i = 0; i < 3; i++)
    {
        var sprite = sprites["frame_" + i.ToString("D2")];
        if (sprite.rect != rects[i].rect || sprite.pivot != new UnityEngine.Vector2(8, 8) || sprite.pixelsPerUnit != 16)
            throw new System.Exception("Rect/pivot/PPU read-back failed: " + sprite.name);
    }
    if (importer.filterMode != UnityEngine.FilterMode.Point || importer.textureCompression != UnityEditor.TextureImporterCompression.Uncompressed || importer.mipmapEnabled)
        throw new System.Exception("Importer read-back failed");
    return new { passed = true, frames = 3, grid = "2x2, one trailing empty cell", testOnlyPpu = 16, testOnlyPivot = "center", filter = importer.filterMode.ToString(), productionSettingsChanged = false };
}
finally
{
    if (!UnityEditor.AssetDatabase.DeleteAsset(folder)) throw new System.Exception("Scratch cleanup failed: " + folder);
}
