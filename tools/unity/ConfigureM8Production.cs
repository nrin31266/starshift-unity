using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;

public static class ConfigureM8Production
{
    const string Art = "Assets/StarShift/Art/";
    public static object Import(string[] only = null, int frameCount = 4)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        AssetDatabase.Refresh();int count=0;
        foreach(var file in Directory.GetFiles(Art,"*.png",SearchOption.AllDirectories).Where(path => !path.Contains("/Placeholders/") && !path.Contains("/Smoke/")))
        {
            var path=file.Replace('\\','/');var id=Path.GetFileNameWithoutExtension(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(only!=null&&!only.Contains(id))continue;
            bool sheet=id.EndsWith("-sheet");bool environment=id.Contains("background")||id.EndsWith("planet")||id=="parallax-debris";
            var pivot = id.StartsWith("rock-shatter") ? new Vector2(19f/47, 1-12f/43) : Vector2.one*.5f;
            var alignment = id.StartsWith("rock-shatter") ? SpriteAlignment.Custom : SpriteAlignment.Center;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=sheet?SpriteImportMode.Multiple:SpriteImportMode.Single;
            importer.spritePixelsPerUnit=environment?21:id.StartsWith("ui-")||id=="hud-ship"?100:56.25f;
            importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=!id.Contains("background");importer.maxTextureSize=2048;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)alignment;settings.spritePivot=pivot;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            if(sheet)
            {
                var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);if(provider==null)throw new Exception("No sprite provider");provider.InitSpriteEditorDataProvider();
                var caps=provider.GetDataProvider<ISpriteFrameEditCapability>();
                foreach(var cap in new[]{EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteRect,EEditCapability.EditPivot,EEditCapability.EditSpriteName})if(caps==null||!caps.GetEditCapability().HasCapability(cap))throw new Exception("Missing capability "+cap);
                var ids=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();if(ids==null)throw new Exception("Missing name/file-ID provider");
                var old=provider.GetSpriteRects().ToDictionary(r=>r.name);importer.GetSourceTextureWidthAndHeight(out var width,out var height);
                int n=id=="explosion-sheet"?8:frameCount;int columns=4;int w=width/columns;int rows=(n+columns-1)/columns;if(width%columns!=0||height%rows!=0)throw new Exception("Invalid sheet grid "+path);int h=height/rows;
                var rects=Enumerable.Range(0,n).Select(i=>{var name=id.Replace("-sheet","")+"_"+i.ToString("D2");return new SpriteRect{name=name,rect=new Rect(i%columns*w,height-(i/columns+1)*h,w,h),alignment=alignment,pivot=pivot,spriteID=old.TryGetValue(name,out var previous)?previous.spriteID:UnityEngine.GUID.Generate()};}).ToArray();
                provider.SetSpriteRects(rects);ids.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));provider.Apply();importer.SaveAndReimport();
                if(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count()!=n)throw new Exception("Frame count mismatch "+path);
            }
            count++;
        }
        return new { imported=count };
    }
}
