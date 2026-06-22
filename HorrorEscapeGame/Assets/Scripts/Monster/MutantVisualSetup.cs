#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class MutantVisualSetup
{
    public const float VisualScale = 1.2f;
    public const float AnimationSpeed = 0.85f;

    public const string PrefabPath = "Assets/Zombie_Mutant/prefab/SKM_Zombie_Mutant.prefab";

    private const string TexRoot = "Assets/Zombie_Mutant/textures";

    private static Material _bodyMaterial;
    private static Material _eyeMaterial;

    public static GameObject LoadPrefab()
    {
        var reg = HorrorAssetRegistry.Instance;
        if (reg != null && reg.mutantVisualPrefab != null)
            return reg.mutantVisualPrefab;

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
#else
        return null;
#endif
    }

    public static void Apply(GameObject visual, RuntimeAnimatorController controller)
    {
        if (visual == null) return;

        visual.transform.localScale = Vector3.one * VisualScale;
        visual.transform.localPosition = Vector3.zero;
#if UNITY_EDITOR
        ApplyUrpMaterials(visual);
#endif
        MaterialURPFixer.FixHierarchy(visual);

        var animator = visual.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = AnimationSpeed;

            var avatar = LoadAvatar();
            if (avatar != null && avatar.isValid && avatar.isHuman)
                animator.avatar = avatar;

            if (controller != null)
                animator.runtimeAnimatorController = controller;
        }
    }

    private static void ApplyUrpMaterials(GameObject visual)
    {
        foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                string name = materials[i] != null
                    ? materials[i].name.Replace(" (Instance)", "")
                    : string.Empty;

                Material resolved = null;
                if (name.Contains("eye", System.StringComparison.OrdinalIgnoreCase))
                    resolved = GetEyeMaterial();
                else if (name.Contains("Mutant", System.StringComparison.OrdinalIgnoreCase))
                    resolved = GetBodyMaterial();

                if (resolved == null || resolved == materials[i]) continue;

                materials[i] = resolved;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }
    }

    private static Material GetBodyMaterial()
    {
        if (_bodyMaterial != null) return _bodyMaterial;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;

        _bodyMaterial = new Material(shader);
        _bodyMaterial.name = "MutantBody_URP";

        var baseMap = LoadTexture($"{TexRoot}/body/T_Body_BaseColor.png");
        var normalMap = LoadTexture($"{TexRoot}/body/T_Body_Normal.png");
        var ormMap = LoadTexture($"{TexRoot}/body/T_Body_OcclusionRoughnessMetallic.png");

        if (baseMap != null) _bodyMaterial.SetTexture("_BaseMap", baseMap);
        if (normalMap != null)
        {
            _bodyMaterial.SetTexture("_BumpMap", normalMap);
            _bodyMaterial.EnableKeyword("_NORMALMAP");
        }

        if (ormMap != null)
        {
            _bodyMaterial.SetTexture("_MetallicGlossMap", ormMap);
            _bodyMaterial.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        _bodyMaterial.SetFloat("_Smoothness", 0.35f);
        return _bodyMaterial;
    }

    private static Material GetEyeMaterial()
    {
        if (_eyeMaterial != null) return _eyeMaterial;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;

        _eyeMaterial = new Material(shader);
        _eyeMaterial.name = "MutantEye_URP";

        var eyeMap = LoadTexture($"{TexRoot}/eye/T_eye_BaseColor.png");
        if (eyeMap != null) _eyeMaterial.SetTexture("_BaseMap", eyeMap);

        _eyeMaterial.SetFloat("_Smoothness", 0.65f);
        return _eyeMaterial;
    }

    private static Texture2D LoadTexture(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#else
        return null;
#endif
    }

    public static void FinishSpawnAlignment(Transform entityRoot, GameObject visualRoot)
    {
        var pin = entityRoot.gameObject.GetComponent<MonsterGroundPin>();
        if (pin == null)
            pin = entityRoot.gameObject.AddComponent<MonsterGroundPin>();
        pin.Configure(entityRoot, visualRoot);
    }

    private static Avatar LoadAvatar()
    {
#if UNITY_EDITOR
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Zombie_Mutant/mesh/SKM_Zombie_Mutant.fbx"))
        {
            if (asset is Avatar avatar)
                return avatar;
        }
#endif
        return null;
    }
}
