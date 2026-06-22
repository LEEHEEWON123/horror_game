#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class RuntimePrefabLoader
{
    public static GameObject Load(string assetPath)
    {
        var reg = HorrorAssetRegistry.Instance;
        if (reg != null)
        {
            var resolved = reg.ResolvePrefab(assetPath);
            if (resolved != null)
                return resolved;
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
        return null;
#endif
    }
}
