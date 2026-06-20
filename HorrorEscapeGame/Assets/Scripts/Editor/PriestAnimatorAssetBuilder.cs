#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PriestAnimatorAssetBuilder
{
    private const string Folder = "Assets/Cursed Priest/Animations";
    private const string PriestFbx = "Assets/Cursed Priest/Mesh/Priest.fbx";
    private const string FastRunFbx = Folder + "/fastRun.fbx";
    private const string RunningFbx = Folder + "/Running.fbx";
    private const string ControllerPath = Folder + "/PriestLocomotion.controller";
    private const string RunAnimPath = Folder + "/PriestRun.anim";

    [MenuItem("Horror Escape/Build Priest Locomotion (Map 05)")]
    public static void BuildFromMenu() => Build(force: true);

    public static void Build(bool force)
    {
        if (!File.Exists(PriestFbx))
        {
            Debug.LogWarning("[PriestAnimator] Missing Priest mesh FBX.");
            return;
        }

        if (!force && File.Exists(ControllerPath) && File.Exists(RunAnimPath) && IsPriestImportValid())
            return;

        EnsureFolder();
        ConfigurePriestMeshImport();

        string runFbx = ResolveRunFbx();
        if (runFbx == null)
        {
            Debug.LogWarning("[PriestAnimator] Add Running.fbx or fastRun.fbx under Cursed Priest/Animations.");
            return;
        }

        ConfigureRunImport(runFbx);
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(runFbx, ImportAssetOptions.ForceUpdate);

        var runClip = LoadRunClip(runFbx);
        if (runClip == null)
        {
            Debug.LogWarning($"[PriestAnimator] Run clip not found in {runFbx}. Check Rig import (Humanoid).");
            return;
        }

        SaveRunAnimAsset(runClip, force);
        BuildController(runClip, force);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PriestAnimator] Priest locomotion ready ({Path.GetFileName(runFbx)} → PriestRun.anim).");
    }

    private static string ResolveRunFbx()
    {
        if (File.Exists(RunningFbx)) return RunningFbx;
        if (File.Exists(FastRunFbx)) return FastRunFbx;
        return null;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Cursed Priest"))
            return;

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Cursed Priest", "Animations");
    }

    private static void ConfigurePriestMeshImport()
    {
        var importer = AssetImporter.GetAtPath(PriestFbx) as ModelImporter;
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

        var description = importer.humanDescription;
        if (PatchHumanBoneName(ref description, "Head", "Head 1"))
            dirty = true;

        if (dirty)
        {
            importer.humanDescription = description;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureRunImport(string runFbx)
    {
        var importer = AssetImporter.GetAtPath(runFbx) as ModelImporter;
        if (importer == null) return;

        var priestAvatar = LoadPriestAvatar();
        bool dirty = false;

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            dirty = true;
        }

        if (priestAvatar != null)
        {
            if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                dirty = true;
            }

            if (importer.sourceAvatar != priestAvatar)
            {
                importer.sourceAvatar = priestAvatar;
                dirty = true;
            }
        }
        else if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            dirty = true;
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

        var description = importer.humanDescription;
        if (PatchHumanBoneName(ref description, "Head", "Head1"))
            dirty = true;

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
        {
            importer.humanDescription = description;
            importer.SaveAndReimport();
        }
    }

    private static bool PatchHumanBoneName(ref HumanDescription description, string humanName, string boneName)
    {
        if (description.human == null) return false;

        bool changed = false;
        for (int i = 0; i < description.human.Length; i++)
        {
            if (description.human[i].humanName != humanName) continue;
            if (description.human[i].boneName == boneName) continue;

            var bone = description.human[i];
            bone.boneName = boneName;
            description.human[i] = bone;
            changed = true;
        }

        return changed;
    }

    private static void SaveRunAnimAsset(AnimationClip sourceClip, bool force)
    {
        if (!force && File.Exists(RunAnimPath))
            return;

        if (File.Exists(RunAnimPath))
            AssetDatabase.DeleteAsset(RunAnimPath);

        var copy = Object.Instantiate(sourceClip);
        copy.name = "PriestRun";
        AssetDatabase.CreateAsset(copy, RunAnimPath);
    }

    private static void BuildController(AnimationClip runClip, bool force)
    {
        if (!force && File.Exists(ControllerPath))
            return;

        if (File.Exists(ControllerPath))
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var stateMachine = controller.layers[0].stateMachine;
        var runState = stateMachine.AddState("Running", new Vector3(320f, 60f, 0f));
        runState.motion = runClip;
        stateMachine.defaultState = runState;
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

    private static bool IsPriestImportValid()
    {
        var meshImporter = AssetImporter.GetAtPath(PriestFbx) as ModelImporter;
        if (meshImporter == null) return false;
        if (meshImporter.animationType != ModelImporterAnimationType.Human) return false;

        string runFbx = ResolveRunFbx();
        if (runFbx == null) return false;

        var runImporter = AssetImporter.GetAtPath(runFbx) as ModelImporter;
        if (runImporter == null) return false;

        var priestAvatar = LoadPriestAvatar();
        return runImporter.animationType == ModelImporterAnimationType.Human
               && priestAvatar != null
               && runImporter.avatarSetup == ModelImporterAvatarSetup.CopyFromOther
               && runImporter.sourceAvatar == priestAvatar;
    }

    private static Avatar LoadPriestAvatar()
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(PriestFbx))
        {
            if (asset is Avatar avatar)
                return avatar;
        }

        return null;
    }
}
#endif
