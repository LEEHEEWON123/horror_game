using UnityEngine;
using UnityEngine.Rendering;

public static class PortalViewMaterial
{
    private static readonly string[] ShaderCandidates =
    {
        "Universal Render Pipeline/Unlit",
        "Unlit/Texture",
        "Unlit/Color",
        "Sprites/Default"
    };

    public static bool TryApplyTexture(Renderer portalView, Texture texture)
    {
        if (portalView == null || texture == null)
            return false;

        var shader = FindShader();
        if (shader == null)
            return false;

        var material = new Material(shader);
        AssignTexture(material, texture);
        material.SetColor(GetColorProperty(material), Color.white);
        material.SetInt("_Cull", (int)CullMode.Off);

        portalView.sharedMaterial = material;
        portalView.shadowCastingMode = ShadowCastingMode.Off;
        portalView.receiveShadows = false;
        return true;
    }

    private static Shader FindShader()
    {
        foreach (var name in ShaderCandidates)
        {
            var shader = Shader.Find(name);
            if (shader != null)
                return shader;
        }

        return null;
    }

    private static void AssignTexture(Material material, Texture texture)
    {
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
    }

    private static string GetColorProperty(Material material)
    {
        if (material.HasProperty("_BaseColor"))
            return "_BaseColor";
        return "_Color";
    }
}
