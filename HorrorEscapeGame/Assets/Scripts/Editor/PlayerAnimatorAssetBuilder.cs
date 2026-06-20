#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerAnimatorAssetBuilder
{
    private const string Folder = "Assets/Protection_suite/Animations";
    private const string ControllerPath = Folder + "/PlayerLocomotion.controller";
    private const string IdlePath = Folder + "/Idle.anim";
    private const string WalkPath = Folder + "/Walk.anim";

    static PlayerAnimatorAssetBuilder()
    {
        EditorApplication.delayCall += EnsureAssets;
    }

    [MenuItem("Horror Escape/Build Player Locomotion")]
    public static void BuildFromMenu() => Build();

    private static void EnsureAssets()
    {
        if (!File.Exists(ControllerPath))
            Build();
    }

    public static void Build()
    {
        EnsureFolder();

        if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
        if (File.Exists(IdlePath)) AssetDatabase.DeleteAsset(IdlePath);
        if (File.Exists(WalkPath)) AssetDatabase.DeleteAsset(WalkPath);

        var idle = CreateIdleClip();
        AssetDatabase.CreateAsset(idle, IdlePath);

        var walk = CreateWalkClip();
        AssetDatabase.CreateAsset(walk, WalkPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var stateMachine = controller.layers[0].stateMachine;
        var idleState = stateMachine.AddState("Idle", new Vector3(280f, 60f, 0f));
        idleState.motion = idle;

        var walkState = stateMachine.AddState("Walk", new Vector3(540f, 60f, 0f));
        walkState.motion = walk;

        stateMachine.defaultState = idleState;

        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.12f, "Speed");
        toWalk.hasExitTime = false;
        toWalk.duration = 0.12f;

        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.12f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Player locomotion animations and controller created.");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Protection_suite"))
            return;

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Protection_suite", "Animations");
    }

    private static AnimationClip CreateIdleClip()
    {
        var clip = new AnimationClip { name = "Idle", frameRate = 30f };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    private static AnimationClip CreateWalkClip()
    {
        var clip = new AnimationClip { name = "Walk", frameRate = 30f };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        const float duration = 0.8f;
        const float period = 0.8f;

        AddMuscleSine(clip, "Left Upper Leg", "Front-Back", duration, period, 0.55f, 0f);
        AddMuscleSine(clip, "Right Upper Leg", "Front-Back", duration, period, 0.55f, Mathf.PI);
        AddMuscleSine(clip, "Left Lower Leg", "Stretch", duration, period, 0.7f, Mathf.PI * 0.5f);
        AddMuscleSine(clip, "Right Lower Leg", "Stretch", duration, period, 0.7f, Mathf.PI * 1.5f);
        AddMuscleSine(clip, "Left Upper Arm", "Front-Back", duration, period, 0.25f, Mathf.PI);
        AddMuscleSine(clip, "Right Upper Arm", "Front-Back", duration, period, 0.25f, 0f);
        AddMuscleSine(clip, "Spine", "Front-Back", duration, period, 0.08f, 0f);

        return clip;
    }

    private static void AddMuscleSine(AnimationClip clip, string bodyPart, string dof, float duration,
        float period, float amplitude, float phase)
    {
        int muscleIndex = FindMuscle(bodyPart, dof);
        if (muscleIndex < 0) return;

        string muscleName = HumanTrait.MuscleName[muscleIndex];
        var keys = new List<Keyframe>();
        int frameCount = Mathf.CeilToInt(duration * 30f);

        for (int frame = 0; frame <= frameCount; frame++)
        {
            float time = frame / 30f;
            float value = Mathf.Sin((time / period) * Mathf.PI * 2f + phase) * amplitude;
            keys.Add(new Keyframe(time, value));
        }

        var curve = new AnimationCurve(keys.ToArray());
        var binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), muscleName);
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }

    private static int FindMuscle(string bodyPart, string dof)
    {
        for (int i = 0; i < HumanTrait.MuscleCount; i++)
        {
            string name = HumanTrait.MuscleName[i];
            if (name.Contains(bodyPart) && name.Contains(dof))
                return i;
        }

        return -1;
    }
}
#endif
