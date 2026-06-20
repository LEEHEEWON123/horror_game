#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class InsurgentAnimatorAssetBuilder
{
    private const string AssetRoot = "Assets/ArtStore3D/Insurgent Lite(LITE)";
    private const string Folder = AssetRoot + "/Animations";
    private const string InsurgentFbx = AssetRoot + "/Model/Insurgent_LiteLite.fbx";
    private const string FastRunFbx = Folder + "/FastRun.fbx";
    private const string RunningFbx = Folder + "/Running.fbx";
    private const string ControllerPath = Folder + "/InsurgentLocomotion.controller";
    private const string IdlePath = Folder + "/InsurgentIdle.anim";
    private const string RunAnimPath = Folder + "/InsurgentRun.anim";

    [MenuItem("Horror Escape/Build Insurgent Locomotion (Map 07)")]
    public static void BuildFromMenu() => Build(force: true);

    public static void Build(bool force)
    {
        if (!File.Exists(InsurgentFbx))
        {
            Debug.LogWarning("[InsurgentAnimator] Missing Insurgent mesh FBX.");
            return;
        }

        string runFbx = ResolveRunFbx();
        if (runFbx == null)
        {
            Debug.LogWarning("[InsurgentAnimator] Add FastRun.fbx or Running.fbx under Insurgent Lite(LITE)/Animations.");
            return;
        }

        EnsureFolder();
        InsurgentVisualSetup.EnsureHumanoidImport();
        ConfigureRunImport(runFbx);
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(runFbx, ImportAssetOptions.ForceUpdate);

        var runningClip = LoadRunClip(runFbx);
        if (runningClip == null)
        {
            Debug.LogWarning($"[InsurgentAnimator] Run clip not found in {runFbx}.");
            return;
        }

        if (!force && File.Exists(RunAnimPath) && File.Exists(ControllerPath) && IsRunImportValid(runFbx))
            return;

        SaveRunAnimAsset(runningClip, force);
        BuildController(runningClip, force);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[InsurgentAnimator] Insurgent locomotion ready ({Path.GetFileName(runFbx)}).");
    }

    public static AnimationClip LoadSavedRunClip()
    {
#if UNITY_EDITOR
        var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunAnimPath);
        if (saved != null) return saved;

        string runFbx = ResolveRunFbx();
        return runFbx != null ? LoadRunClip(runFbx) : null;
#else
        return null;
#endif
    }

    private static string ResolveRunFbx()
    {
        if (File.Exists(FastRunFbx)) return FastRunFbx;
        if (File.Exists(RunningFbx)) return RunningFbx;
        return null;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(AssetRoot))
            return;

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder(AssetRoot, "Animations");
    }

    private static void ConfigureRunImport(string runFbx)
    {
        var importer = AssetImporter.GetAtPath(runFbx) as ModelImporter;
        if (importer == null) return;

        var insurgentAvatar = LoadInsurgentAvatar();
        bool dirty = false;

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            dirty = true;
        }

        if (insurgentAvatar != null)
        {
            if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                dirty = true;
            }

            if (importer.sourceAvatar != insurgentAvatar)
            {
                importer.sourceAvatar = insurgentAvatar;
                dirty = true;
            }
        }

        if (!importer.importAnimation)
        {
            importer.importAnimation = true;
            dirty = true;
        }

        if (importer.importCameras)
        {
            importer.importCameras = false;
            dirty = true;
        }

        if (importer.importLights)
        {
            importer.importLights = false;
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

    private static void SaveRunAnimAsset(AnimationClip sourceClip, bool force)
    {
        if (!force && File.Exists(RunAnimPath))
            return;

        if (File.Exists(RunAnimPath))
            AssetDatabase.DeleteAsset(RunAnimPath);

        var copy = Object.Instantiate(sourceClip);
        copy.name = "InsurgentRun";
        AssetDatabase.CreateAsset(copy, RunAnimPath);
    }

    private static void BuildController(AnimationClip runClip, bool force)
    {
        if (!force && File.Exists(ControllerPath) && File.Exists(IdlePath))
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
        runState.motion = runClip;

        stateMachine.defaultState = idleState;

        var toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
        toRun.hasExitTime = false;
        toRun.duration = 0.15f;

        var toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
    }

    private static AnimationClip LoadRunClip(string runFbx)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(runFbx))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                return clip;
        }

        return null;
    }

    private static AnimationClip CreateIdleClip()
    {
        var clip = new AnimationClip { name = "InsurgentIdle", frameRate = 30f };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    private static bool IsRunImportValid(string runFbx)
    {
        var runImporter = AssetImporter.GetAtPath(runFbx) as ModelImporter;
        if (runImporter == null) return false;

        var insurgentAvatar = LoadInsurgentAvatar();
        return runImporter.animationType == ModelImporterAnimationType.Human
               && insurgentAvatar != null
               && runImporter.avatarSetup == ModelImporterAvatarSetup.CopyFromOther
               && runImporter.sourceAvatar == insurgentAvatar
               && File.Exists(RunAnimPath);
    }

    private static Avatar LoadInsurgentAvatar()
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(InsurgentFbx))
        {
            if (asset is Avatar avatar && avatar.isValid)
                return avatar;
        }

        return null;
    }
}
#endif
