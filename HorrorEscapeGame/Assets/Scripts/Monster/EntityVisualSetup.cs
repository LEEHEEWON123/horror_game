#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class EntityVisualSetup
{
    public const float VisualScale = 1.3f;

    public const string DefaultPrefabPath =
        "Assets/ZombieMale_AAB/Prefabs/URP/ZombieMale_AAB_URP.prefab";

    public const float AnimationSpeed = 0.82f;

    private const string MatRoot = "Assets/ZombieMale_AAB/Model/Materials/URP";

    // ZombieMale_AAB: root is rotated -90° on X and pelvis sits at local Z ~0.959,
    // which becomes ~0.96m world lift when the visual pivot is left at zero.
    private const float SkeletonBindLift = 0.959f;

    public static float VisualGroundOffsetY => -SkeletonBindLift * VisualScale;

    public static GameObject LoadDefaultPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPrefabPath);
#else
        return null;
#endif
    }

    public static void Apply(GameObject visual, RuntimeAnimatorController controller)
    {
        if (visual == null) return;

        visual.transform.localScale = Vector3.one * VisualScale;
        visual.transform.localPosition = new Vector3(0f, VisualGroundOffsetY, 0f);
        ApplyUrpMaterials(visual);
        MaterialURPFixer.FixHierarchy(visual);
        SuppressModularFootMeshes(visual);

        var animator = visual.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = AnimationSpeed;

            var avatar = LoadAvatar();
            if (avatar != null && avatar.isValid && avatar.isHuman)
                animator.avatar = avatar;
#if UNITY_EDITOR
            else if (avatar != null)
                Debug.LogWarning("[Entity] Zombie avatar is not a valid Humanoid. Select ZombieMale_AAB.fbx → Rig → Configure.");
#endif

            if (controller != null)
                animator.runtimeAnimatorController = controller;
        }
    }

    public static void FinishSpawnAlignment(Transform entityRoot, GameObject visualRoot)
    {
        var pin = entityRoot.gameObject.GetComponent<MonsterGroundPin>();
        if (pin == null)
            pin = entityRoot.gameObject.AddComponent<MonsterGroundPin>();
        pin.Configure(entityRoot, visualRoot);
        SuppressModularFootMeshes(visualRoot);
    }

    private static void SuppressModularFootMeshes(GameObject visual)
    {
        foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!IsModularFootMesh(renderer.gameObject.name)) continue;
            renderer.enabled = false;
            renderer.gameObject.SetActive(false);
        }
    }

    private static bool IsModularFootMesh(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;

        string name = objectName.Replace(" (Clone)", "");
        return name.Equals("Foot_L", System.StringComparison.OrdinalIgnoreCase)
               || name.Equals("Foot_R", System.StringComparison.OrdinalIgnoreCase)
               || name.Equals("Foot_L_GRP", System.StringComparison.OrdinalIgnoreCase)
               || name.Equals("Foot_R_GRP", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyUrpMaterials(GameObject visual)
    {
        foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                var resolved = ResolveUrpMaterial(materials[i]);
                if (resolved == null || resolved == materials[i]) continue;

                materials[i] = resolved;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }
    }

    private static Material ResolveUrpMaterial(Material source)
    {
        if (source == null) return null;

        if (source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline"))
            return source;

        string name = source.name.Replace(" (Instance)", "").ToLowerInvariant();

        if (name.Contains("lowerbody") || name.Contains("lower_body") || name.Contains("trouser"))
            return LoadMaterial($"{MatRoot}/ZombieMale_LowerBody_MAT_URP.mat");
        if (name.Contains("shirt"))
            return LoadMaterial($"{MatRoot}/ZombieMale_Shirt_MAT_URP.mat");
        if (name.Contains("head"))
            return LoadMaterial($"{MatRoot}/ZombieMale_Head_V1_MAT 1.mat");
        if (name.Contains("eye"))
            return LoadMaterial($"{MatRoot}/Zombie_Eyes_MAT_URP.mat");
        if (name.Contains("body"))
            return LoadMaterial($"{MatRoot}/ZombieMale_Body_MAT_URP.mat");

        return null;
    }

    private static Material LoadMaterial(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Material>(path);
#else
        return null;
#endif
    }

    private static Avatar LoadAvatar()
    {
#if UNITY_EDITOR
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/ZombieMale_AAB/Model/ZombieMale_AAB.fbx"))
        {
            if (asset is Avatar avatar)
                return avatar;
        }
#endif
        return null;
    }
}
