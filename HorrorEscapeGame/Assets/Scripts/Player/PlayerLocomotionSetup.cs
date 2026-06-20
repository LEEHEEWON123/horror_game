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

#if UNITY_EDITOR
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (controller != null)
            animator.runtimeAnimatorController = controller;
#endif

        var locomotion = playerRoot.GetComponent<PlayerLocomotionAnimator>();
        if (locomotion == null)
            locomotion = playerRoot.AddComponent<PlayerLocomotionAnimator>();
        locomotion.Bind(animator);
    }
}
