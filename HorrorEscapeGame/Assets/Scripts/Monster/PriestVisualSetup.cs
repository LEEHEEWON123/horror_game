#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class PriestVisualSetup
{
    public const float VisualScale = 2.3f;

    public const string PrefabPath = "Assets/Cursed Priest/Prefab/Priest.prefab";

    public const string ControllerPath = "Assets/Cursed Priest/Animations/PriestLocomotion.controller";
    public const string RunAnimPath = "Assets/Cursed Priest/Animations/PriestRun.anim";
    public const string FastRunFbxPath = "Assets/Cursed Priest/Animations/fastRun.fbx";
    public const string RunningFbxPath = "Assets/Cursed Priest/Animations/Running.fbx";

    private const string FbxPath = "Assets/Cursed Priest/Mesh/Priest.fbx";

    public const float AnimationSpeed = 1.1f;

    public static GameObject LoadPrefab()
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
        visual.transform.localRotation = Quaternion.identity;

        foreach (var col in visual.GetComponentsInChildren<Collider>(true))
            Object.Destroy(col);

        MaterialURPFixer.FixHierarchy(visual);

        var animator = EnsureAnimator(visual);
        if (animator == null)
        {
            Debug.LogWarning("[PriestVisual] Animator not found on Priest prefab.");
            return;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.speed = AnimationSpeed;
        animator.updateMode = AnimatorUpdateMode.Normal;

        var avatar = LoadAvatar();
        if (avatar != null && avatar.isValid)
            animator.avatar = avatar;
#if UNITY_EDITOR
        else
            Debug.LogWarning("[PriestVisual] Priest Humanoid avatar missing. Run Horror Escape > Build Priest Locomotion.");
#endif

        if (controller == null)
            controller = LoadDefaultController();

        if (controller != null)
            animator.runtimeAnimatorController = controller;

        animator.Rebind();
        animator.Update(0f);
    }

    public static AnimationClip LoadRunClip()
    {
#if UNITY_EDITOR
        var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunAnimPath);
        if (saved != null) return saved;

        if (System.IO.File.Exists(RunningFbxPath))
        {
            var clip = LoadClipFromFbx(RunningFbxPath);
            if (clip != null) return clip;
        }

        if (System.IO.File.Exists(FastRunFbxPath))
            return LoadClipFromFbx(FastRunFbxPath);
#endif
        return null;
    }

    private static AnimationClip LoadClipFromFbx(string path)
    {
#if UNITY_EDITOR
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                return clip;
        }
#endif
        return null;
    }

    private static Animator EnsureAnimator(GameObject visual)
    {
        var animator = visual.GetComponent<Animator>();
        if (animator != null) return animator;

        animator = visual.GetComponentInChildren<Animator>();
        if (animator != null) return animator;

        return visual.AddComponent<Animator>();
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
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
        {
            if (asset is Avatar avatar && avatar.isValid)
                return avatar;
        }
#endif
        return null;
    }
}
