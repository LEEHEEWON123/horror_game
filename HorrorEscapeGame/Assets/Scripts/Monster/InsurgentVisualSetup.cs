#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class InsurgentVisualSetup
{
    public const float VisualScale = 2f;
    public const float NavAgentRadius = 0.5f;
    public const float AnimationSpeed = 0.9f;

    public const string PrefabPath =
        "Assets/ArtStore3D/Insurgent Lite(LITE)/Prefab/Insurgent_Lite_Lite.prefab";

    public const string ControllerPath =
        "Assets/ArtStore3D/Insurgent Lite(LITE)/Animations/InsurgentLocomotion.controller";

    public const string RunAnimPath =
        "Assets/ArtStore3D/Insurgent Lite(LITE)/Animations/InsurgentRun.anim";

    private const string FbxPath =
        "Assets/ArtStore3D/Insurgent Lite(LITE)/Model/Insurgent_LiteLite.fbx";

    private const string TexRoot = "Assets/ArtStore3D/Insurgent Lite(LITE)/Texture";

    private static Material _bodyMaterial;

    public static GameObject LoadPrefab()
    {
#if UNITY_EDITOR
        EnsureHumanoidImport();
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    public static void EnsureHumanoidImport()
    {
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
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

        if (importer.sourceAvatar != null)
        {
            importer.sourceAvatar = null;
            dirty = true;
        }

        var description = importer.humanDescription;
        if (!HasValidInsurgentHumanMapping(description))
        {
            description.human = BuildArtStore3DHumanBones();
            importer.humanDescription = description;
            dirty = true;
        }

        if (dirty)
            importer.SaveAndReimport();
    }

    [MenuItem("Horror Escape/Fix Insurgent Humanoid Rig")]
    public static void FixInsurgentHumanoidFromMenu()
    {
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("[InsurgentVisualSetup] FBX not found.");
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.sourceAvatar = null;

        var description = importer.humanDescription;
        description.human = BuildArtStore3DHumanBones();
        importer.humanDescription = description;
        importer.SaveAndReimport();
        AssetDatabase.Refresh();
        Debug.Log("[InsurgentVisualSetup] Insurgent humanoid rig rebuilt.");
    }

    private static bool HasValidInsurgentHumanMapping(HumanDescription description)
    {
        if (description.human == null || description.human.Length == 0)
            return false;

        foreach (var bone in description.human)
        {
            if (bone.boneName == "ArtStore3D_Hips" && bone.humanName == "Hips")
                return true;
        }

        return false;
    }

    private static HumanBone[] BuildArtStore3DHumanBones()
    {
        (string human, string bone)[] map =
        {
            ("Hips", "ArtStore3D_Hips"),
            ("Spine", "ArtStore3D_Spine"),
            ("Chest", "ArtStore3D_Spine1"),
            ("UpperChest", "ArtStore3D_Spine2"),
            ("Neck", "ArtStore3D_Neck"),
            ("Head", "ArtStore3D_Head"),
            ("LeftShoulder", "ArtStore3D_LeftShoulder"),
            ("LeftUpperArm", "ArtStore3D_LeftArm"),
            ("LeftLowerArm", "ArtStore3D_LeftForeArm"),
            ("LeftHand", "ArtStore3D_LeftHand"),
            ("RightShoulder", "ArtStore3D_RightShoulder"),
            ("RightUpperArm", "ArtStore3D_RightArm"),
            ("RightLowerArm", "ArtStore3D_RightForeArm"),
            ("RightHand", "ArtStore3D_RightHand"),
            ("LeftUpperLeg", "ArtStore3D_LeftUpLeg"),
            ("LeftLowerLeg", "ArtStore3D_LeftLeg"),
            ("LeftFoot", "ArtStore3D_LeftFoot"),
            ("LeftToes", "ArtStore3D_LeftToeBase"),
            ("RightUpperLeg", "ArtStore3D_RightUpLeg"),
            ("RightLowerLeg", "ArtStore3D_RightLeg"),
            ("RightFoot", "ArtStore3D_RightFoot"),
            ("RightToes", "ArtStore3D_RightToeBase")
        };

        var bones = new HumanBone[map.Length];
        for (int i = 0; i < map.Length; i++)
        {
            bones[i] = new HumanBone
            {
                boneName = map[i].bone,
                humanName = map[i].human,
                limit = new HumanLimit { useDefaultValues = true }
            };
        }

        return bones;
    }
#endif

    public static void Apply(GameObject visual, RuntimeAnimatorController controller)
    {
        if (visual == null) return;

        visual.transform.localScale = Vector3.one * VisualScale;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        foreach (var col in visual.GetComponentsInChildren<Collider>(true))
            Object.Destroy(col);

        ApplyUrpMaterials(visual);
        MaterialURPFixer.FixHierarchy(visual);

        var animator = visual.GetComponentInChildren<Animator>();
        if (animator == null) return;

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.speed = AnimationSpeed;

        var avatar = LoadAvatar();
        if (avatar != null && avatar.isValid && avatar.isHuman)
            animator.avatar = avatar;

        if (controller == null)
            controller = LoadDefaultController();

        if (controller != null)
            animator.runtimeAnimatorController = controller;

        if (!animator.isHuman)
            Debug.LogWarning("[InsurgentVisualSetup] Avatar is not humanoid — run Horror Escape/Fix Insurgent Humanoid Rig.");

        animator.Rebind();
        animator.Update(0f);
    }

    public static RuntimeAnimatorController LoadDefaultController()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
#else
        return null;
#endif
    }

    public static AnimationClip LoadRunClip()
    {
#if UNITY_EDITOR
        var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunAnimPath);
        if (saved != null) return saved;

        const string fastRunFbx = "Assets/ArtStore3D/Insurgent Lite(LITE)/Animations/FastRun.fbx";
        if (System.IO.File.Exists(fastRunFbx))
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fastRunFbx))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                    return clip;
            }
        }
#endif
        return null;
    }

    private static void ApplyUrpMaterials(GameObject visual)
    {
        foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;

                string name = materials[i].name.Replace(" (Instance)", "");
                if (!name.Contains("Insurgent", System.StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("ArtStore3D", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var resolved = GetBodyMaterial();
                if (resolved == null || resolved == materials[i]) continue;

                materials[i] = resolved;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }
    }

    private static Material GetBodyMaterial()
    {
        if (_bodyMaterial != null) return _bodyMaterial;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;

        _bodyMaterial = new Material(shader);
        _bodyMaterial.name = "InsurgentLite_URP";

        var baseMap = LoadTexture($"{TexRoot}/Insurgent_Lite_Albedo.png");
        var normalMap = LoadTexture($"{TexRoot}/Insurgent_Lite_Normal.png");

        if (baseMap != null) _bodyMaterial.SetTexture("_BaseMap", baseMap);
        if (normalMap != null)
        {
            _bodyMaterial.SetTexture("_BumpMap", normalMap);
            _bodyMaterial.EnableKeyword("_NORMALMAP");
        }

        _bodyMaterial.SetFloat("_Smoothness", 0.2f);
        return _bodyMaterial;
    }

    private static Texture2D LoadTexture(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#else
        return null;
#endif
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
            if (asset is Avatar avatar)
                return avatar;
        }
#endif
        return null;
    }
}
