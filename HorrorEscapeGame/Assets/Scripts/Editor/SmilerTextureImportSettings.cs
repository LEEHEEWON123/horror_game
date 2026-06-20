#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SmilerTextureImportSettings : AssetPostprocessor
{
    private const string TexturePath = "Assets/Art/Smiler/smiler.png";

    private void OnPreprocessTexture()
    {
        if (assetPath != TexturePath)
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Bilinear;
    }
}
#endif
