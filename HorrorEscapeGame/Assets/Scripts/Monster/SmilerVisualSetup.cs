#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

public static class SmilerVisualSetup
{
    public const string TexturePath = "Assets/Art/Smiler/smiler.png";
    public const float VisualWidth = 2.1f;
    public const float VisualHeight = 2.8f;

    public static Texture2D LoadTexture()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
#else
        return null;
#endif
    }

    public static SmilerFaceAnchor Apply(Transform entityRoot, Texture2D texture)
    {
        var visualGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visualGo.name = "SmilerVisual";
        Object.Destroy(visualGo.GetComponent<Collider>());

        visualGo.transform.SetParent(entityRoot, false);
        visualGo.transform.localPosition = new Vector3(0f, VisualHeight * 0.5f, 0f);
        visualGo.transform.localScale = new Vector3(VisualWidth, VisualHeight, 1f);

        var renderer = visualGo.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = CreateSmilerMaterial(texture);

        visualGo.AddComponent<SmilerBillboard>();

        var anchor = entityRoot.gameObject.GetComponent<SmilerFaceAnchor>();
        if (anchor == null)
            anchor = entityRoot.gameObject.AddComponent<SmilerFaceAnchor>();
        anchor.Configure(visualGo.transform);

        return anchor;
    }

    private static Material CreateSmilerMaterial(Texture2D texture)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");

        var mat = new Material(shader);
        mat.name = "Smiler_Unlit";

        if (texture != null)
            mat.SetTexture("_BaseMap", texture);

        mat.SetColor("_BaseColor", Color.white);

        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;

        return mat;
    }
}
