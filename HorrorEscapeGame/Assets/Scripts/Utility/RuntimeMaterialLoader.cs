#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class RuntimeMaterialLoader
{
    public static Material Load(string assetPath)
    {
        var reg = HorrorAssetRegistry.Instance;
        if (reg != null)
        {
            var resolved = reg.ResolveMaterial(assetPath);
            if (resolved != null)
                return resolved;
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Material>(assetPath);
#else
        return null;
#endif
    }
}
