#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class PlayerLocomotionSetup
{
    public const string ControllerPath = "Assets/Protection_suite/Animations/PlayerLocomotion.controller";

    public static void Apply(GameObject visualRoot, GameObject playerRoot)
    {
        var animator = visualRoot.GetComponentInChildren<Animator>();
        if (animator == null) return;

        animator.applyRootMotion = false;

        var controller = LoadController();
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        var locomotion = playerRoot.GetComponent<PlayerLocomotionAnimator>();
        if (locomotion == null)
            locomotion = playerRoot.AddComponent<PlayerLocomotionAnimator>();
        locomotion.Bind(animator);
    }

    private static RuntimeAnimatorController LoadController()
    {
        var reg = HorrorAssetRegistry.Instance;
        if (reg != null && reg.playerLocomotionController != null)
            return reg.playerLocomotionController;

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
#else
        return null;
#endif
    }
}
