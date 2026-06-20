#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class PumpkinVisualSetup
{
    public const float VisualScale = 3.45f;

    public const string PrefabPath = "Assets/pumpkin monster/Prefab/pumpkin.prefab";

    public const string ControllerPath = "Assets/pumpkin monster/Animations/PumpkinLocomotion.controller";

    private const string FbxPath = "Assets/pumpkin monster/Base mesh/pumpkin.fbx";

    public const float AnimationSpeed = 1.15f;

    public static GameObject LoadDefaultPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
#else
        return null;
#endif
    }

    public static RuntimeAnimatorController LoadDefaultController()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
#else
        return null;
#endif
    }

    public static void Apply(GameObject visual, RuntimeAnimatorController controller)
    {
        if (visual == null) return;

        visual.transform.localScale = Vector3.one * VisualScale;
        visual.transform.localPosition = Vector3.zero;
        MaterialURPFixer.FixHierarchy(visual);

        var animator = EnsureAnimator(visual);

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.speed = AnimationSpeed;

        var avatar = LoadAvatar();
        if (avatar != null && avatar.isValid && avatar.isHuman)
            animator.avatar = avatar;
#if UNITY_EDITOR
        else
            Debug.LogWarning("[Pumpkin] Humanoid avatar missing on pumpkin.fbx — running animation will not play.");
#endif

        if (controller == null)
            controller = LoadDefaultController();

        if (controller != null)
            animator.runtimeAnimatorController = controller;

        animator.Rebind();
        animator.Update(0f);
        animator.Play("Running", 0, 0f);
    }

    private static Animator EnsureAnimator(GameObject visual)
    {
        var animator = visual.GetComponent<Animator>();
        if (animator != null) return animator;

        animator = visual.GetComponentInChildren<Animator>();
        if (animator != null) return animator;

        var skinned = visual.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinned != null)
        {
            var host = skinned.transform;
            while (host.parent != null && host.parent != visual.transform)
                host = host.parent;

            animator = host.GetComponent<Animator>();
            if (animator != null) return animator;

            return host.gameObject.AddComponent<Animator>();
        }

        return visual.AddComponent<Animator>();
    }

    private static Avatar LoadAvatar()
    {
#if UNITY_EDITOR
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
        {
            if (asset is Avatar avatar)
                return avatar;
        }
#endif
        return null;
    }
}
