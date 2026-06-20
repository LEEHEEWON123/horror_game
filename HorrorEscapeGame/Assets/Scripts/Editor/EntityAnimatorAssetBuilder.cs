#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EntityAnimatorAssetBuilder
{
    private const string Folder = "Assets/Zombie_Mutant/Animations";
    private const string RunningFbx = Folder + "/Running.fbx";
    private const string AvatarSource = "Assets/ZombieMale_AAB/Model/ZombieMale_AAB.fbx";
    private const string ControllerPath = Folder + "/EntityLocomotion.controller";
    private const string IdlePath = Folder + "/Idle.anim";

    [MenuItem("Horror Escape/Build Entity Locomotion")]
    public static void BuildFromMenu() => Build(force: true);

    public static void Build(bool force)
    {
        if (!File.Exists(RunningFbx))
        {
            Debug.LogWarning($"[EntityAnimator] Missing Mixamo clip: {RunningFbx}");
            return;
        }

        EnsureFolder();
        ConfigureZombieMeshImport();
        ConfigureRunningImport();

        var runningClip = LoadRunningClip();
        if (runningClip == null)
        {
            Debug.LogWarning("[EntityAnimator] Running animation clip not found in FBX.");
            return;
        }

        if (!force && File.Exists(ControllerPath) && File.Exists(IdlePath) && IsRunningImportValid())
            return;

        if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
        if (File.Exists(IdlePath)) AssetDatabase.DeleteAsset(IdlePath);

        var idle = CreateIdleClip();
        AssetDatabase.CreateAsset(idle, IdlePath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var stateMachine = controller.layers[0].stateMachine;
        var idleState = stateMachine.AddState("Idle", new Vector3(280f, 60f, 0f));
        idleState.motion = idle;

        var runState = stateMachine.AddState("Running", new Vector3(540f, 60f, 0f));
        runState.motion = runningClip;

        stateMachine.defaultState = idleState;

        var toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
        toRun.hasExitTime = false;
        toRun.duration = 0.15f;

        var toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EntityAnimator] Entity locomotion controller created.");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Zombie_Mutant"))
            return;

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Zombie_Mutant", "Animations");
    }

    private static bool IsRunningImportValid()
    {
        var importer = AssetImporter.GetAtPath(RunningFbx) as ModelImporter;
        if (importer == null) return false;
        return importer.animationType == ModelImporterAnimationType.Human
               && importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel;
    }

    private static void ConfigureZombieMeshImport()
    {
        var importer = AssetImporter.GetAtPath(AvatarSource) as ModelImporter;
        if (importer == null) return;

        bool dirty = false;

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            dirty = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            dirty = true;
        }

        if (importer.importAnimation)
        {
            importer.importAnimation = false;
            dirty = true;
        }

        if (dirty)
            importer.SaveAndReimport();
    }

    private static void ConfigureRunningImport()
    {
        var importer = AssetImporter.GetAtPath(RunningFbx) as ModelImporter;
        if (importer == null) return;

        bool dirty = false;

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            dirty = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            dirty = true;
        }

        if (!importer.importAnimation)
        {
            importer.importAnimation = true;
            dirty = true;
        }

        var clips = importer.defaultClipAnimations;
        if (clips != null && clips.Length > 0)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                bool changed = false;

                if (!clip.loopTime) { clip.loopTime = true; changed = true; }
                if (!clip.loopPose) { clip.loopPose = true; changed = true; }
                if (!clip.lockRootRotation) { clip.lockRootRotation = true; changed = true; }
                if (!clip.lockRootHeightY) { clip.lockRootHeightY = true; changed = true; }
                if (!clip.lockRootPositionXZ) { clip.lockRootPositionXZ = true; changed = true; }
                if (!clip.keepOriginalOrientation) { clip.keepOriginalOrientation = true; changed = true; }
                if (!clip.keepOriginalPositionY) { clip.keepOriginalPositionY = true; changed = true; }
                if (!clip.keepOriginalPositionXZ) { clip.keepOriginalPositionXZ = true; changed = true; }

                if (changed)
                {
                    clips[i] = clip;
                    dirty = true;
                }
            }

            importer.clipAnimations = clips;
        }

        if (dirty)
            importer.SaveAndReimport();
    }

    private static AnimationClip LoadRunningClip()
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(RunningFbx))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                return clip;
        }

        return null;
    }

    private static AnimationClip CreateIdleClip()
    {
        var clip = new AnimationClip { name = "Idle", frameRate = 30f };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }
}
#endif
