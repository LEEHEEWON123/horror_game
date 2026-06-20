#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PumpkinAnimatorAssetBuilder
{
    private const string Folder = "Assets/pumpkin monster/Animations";
    private const string PumpkinFbx = "Assets/pumpkin monster/Base mesh/pumpkin.fbx";
    private const string RunningFbx = Folder + "/Running.fbx";
    private const string ControllerPath = Folder + "/PumpkinLocomotion.controller";

    [MenuItem("Horror Escape/Build Pumpkin Locomotion")]
    public static void BuildFromMenu() => Build(force: true);

    public static void Build(bool force)
    {
        if (!File.Exists(PumpkinFbx) || !File.Exists(RunningFbx))
        {
            Debug.LogWarning("[PumpkinAnimator] Missing pumpkin mesh or Running.fbx.");
            return;
        }

        EnsureFolder();
        ConfigurePumpkinMeshImport();
        ConfigureRunningImport();

        var runningClip = LoadRunningClip();
        if (runningClip == null)
        {
            Debug.LogWarning("[PumpkinAnimator] Running animation clip not found.");
            return;
        }

        if (!force && File.Exists(ControllerPath) && IsRunningImportValid())
            return;

        if (File.Exists(ControllerPath))
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var stateMachine = controller.layers[0].stateMachine;
        var runState = stateMachine.AddState("Running", new Vector3(320f, 60f, 0f));
        runState.motion = runningClip;
        stateMachine.defaultState = runState;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PumpkinAnimator] Pumpkin locomotion controller created.");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/pumpkin monster"))
            return;

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/pumpkin monster", "Animations");
    }

    private static bool IsRunningImportValid()
    {
        var importer = AssetImporter.GetAtPath(RunningFbx) as ModelImporter;
        if (importer == null) return false;

        var pumpkinAvatar = LoadPumpkinAvatar();
        return importer.animationType == ModelImporterAnimationType.Human
               && importer.avatarSetup == ModelImporterAvatarSetup.CopyFromOther
               && importer.sourceAvatar == pumpkinAvatar;
    }

    private static void ConfigurePumpkinMeshImport()
    {
        var importer = AssetImporter.GetAtPath(PumpkinFbx) as ModelImporter;
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

        var pumpkinAvatar = LoadPumpkinAvatar();
        bool dirty = false;

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            dirty = true;
        }

        if (pumpkinAvatar != null)
        {
            if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                dirty = true;
            }

            if (importer.sourceAvatar != pumpkinAvatar)
            {
                importer.sourceAvatar = pumpkinAvatar;
                dirty = true;
            }
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

    private static Avatar LoadPumpkinAvatar()
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(PumpkinFbx))
        {
            if (asset is Avatar avatar)
                return avatar;
        }

        return null;
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
}
#endif
