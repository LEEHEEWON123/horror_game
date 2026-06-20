using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MaterialURPFixer
{
    private static Shader _urpLit;
    private static readonly Dictionary<int, Material> ConvertedCache = new();

    public static void FixHierarchy(GameObject root)
    {
        CacheShaders();
        if (_urpLit == null) return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                var converted = ConvertMaterial(materials[i]);
                if (converted != materials[i])
                {
                    materials[i] = converted;
                    changed = true;
                }
            }

            if (changed)
                renderer.sharedMaterials = materials;

            if (!IsModularFootRenderer(renderer))
                renderer.enabled = true;
        }
    }

    private static bool IsModularFootRenderer(Renderer renderer)
    {
        if (renderer is not SkinnedMeshRenderer) return false;

        string name = renderer.gameObject.name.Replace(" (Clone)", "");
        return name.Equals("Foot_L", System.StringComparison.OrdinalIgnoreCase)
               || name.Equals("Foot_R", System.StringComparison.OrdinalIgnoreCase)
               || name.Equals("Foot_L_GRP", System.StringComparison.OrdinalIgnoreCase)
               || name.Equals("Foot_R_GRP", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void CacheShaders()
    {
        _urpLit ??= Shader.Find("Universal Render Pipeline/Lit");
    }

    public static Material ConvertToUrP(Material source)
    {
        CacheShaders();
        if (_urpLit == null || source == null) return source;
        return ConvertMaterial(source);
    }

    private static Material ConvertMaterial(Material source)
    {
        if (source == null || source.shader == null) return source;
        if (source.shader.name.StartsWith("Universal Render Pipeline")) return source;
        if (source.shader.name.Contains("Shader Graph")) return source;

        int cacheKey = source.GetInstanceID();
        if (ConvertedCache.TryGetValue(cacheKey, out Material cached))
            return cached;

        bool isTransparent = source.renderQueue >= 3000
            || (source.HasProperty("_SurfaceType") && source.GetFloat("_SurfaceType") >= 1f)
            || (source.HasProperty("_Mode") && source.GetFloat("_Mode") >= 2f);
        bool isCutout = !isTransparent && (
            source.renderQueue is >= 2450 and < 3000
            || (source.HasProperty("_AlphaCutoffEnable") && source.GetFloat("_AlphaCutoffEnable") >= 0.5f)
            || (source.HasProperty("_Mode") && Mathf.Approximately(source.GetFloat("_Mode"), 1f))
            || source.IsKeywordEnabled("_ALPHATEST_ON"));

        var mat = new Material(_urpLit);

        var baseMap = GetBaseMapTexture(source);
        if (baseMap != null)
            mat.SetTexture("_BaseMap", baseMap);

        if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null)
            mat.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
        else if (source.HasProperty("_NormalMap") && source.GetTexture("_NormalMap") != null)
            mat.SetTexture("_BumpMap", source.GetTexture("_NormalMap"));

        if (source.HasProperty("_MetallicGlossMap") && source.GetTexture("_MetallicGlossMap") != null)
        {
            mat.SetTexture("_MetallicGlossMap", source.GetTexture("_MetallicGlossMap"));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        else if (source.HasProperty("_MaskMap") && source.GetTexture("_MaskMap") != null)
        {
            mat.SetTexture("_MetallicGlossMap", source.GetTexture("_MaskMap"));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        else if (source.HasProperty("_SpecGlossMap") && source.GetTexture("_SpecGlossMap") != null)
        {
            mat.SetTexture("_MetallicGlossMap", source.GetTexture("_SpecGlossMap"));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        if (source.HasProperty("_OcclusionMap") && source.GetTexture("_OcclusionMap") != null)
            mat.SetTexture("_OcclusionMap", source.GetTexture("_OcclusionMap"));

        if (source.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", source.GetColor("_BaseColor"));
        else if (source.HasProperty("_Color"))
            mat.SetColor("_BaseColor", source.GetColor("_Color"));

        if (source.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", source.GetFloat("_Metallic"));
        if (source.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
        else if (source.HasProperty("_GlossMapScale"))
            mat.SetFloat("_Smoothness", source.GetFloat("_GlossMapScale"));
        else if (source.HasProperty("_Glossiness"))
            mat.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));

        if (source.HasProperty("_EmissionColor"))
        {
            var emission = source.GetColor("_EmissionColor");
            if (emission.maxColorComponent > 0.01f)
            {
                mat.SetColor("_EmissionColor", emission);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }

        if (source.HasProperty("_EmissionMap") && source.GetTexture("_EmissionMap") != null)
            mat.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));

        if (isCutout)
        {
            float cutoff = source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f;
            if (source.HasProperty("_AlphaCutoff"))
                cutoff = source.GetFloat("_AlphaCutoff");
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", cutoff);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else if (isTransparent)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        ConvertedCache[cacheKey] = mat;
        return mat;
    }

    private static Texture GetBaseMapTexture(Material source)
    {
        if (source.HasProperty("_BaseMap") && source.GetTexture("_BaseMap") != null)
            return source.GetTexture("_BaseMap");
        if (source.HasProperty("_BaseColorMap") && source.GetTexture("_BaseColorMap") != null)
            return source.GetTexture("_BaseColorMap");
        if (source.HasProperty("_MainTex") && source.GetTexture("_MainTex") != null)
            return source.GetTexture("_MainTex");
        return null;
    }
}
