#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Map01Bootstrap : MonoBehaviour
{
    [SerializeField] private GameObject protectiveSuitVisualPrefab;
    [SerializeField] private GameObject backroomsLevelPrefab;
    [SerializeField] private GameObject entityVisualPrefab;
    [SerializeField] private RuntimeAnimatorController entityAnimatorController;
    [SerializeField] private AudioClip[] entityChaseClips;

    private Map01Layout _layout;

    private void Awake()
    {
        ResolvePrefabs();
        GameUIBuilder.EnsureCoreSystems();

        var map = BackroomsMapBuilder.Build(backroomsLevelPrefab);
        _layout = Map01Layout.ForBackrooms(map.Bounds, map.MazeWalls);

        var player = SpawnPlayer(_layout.PlayerSpawn);
        SetupFirstPersonCamera(player.transform);

        MapPortalSetup.SpawnForActiveMap(
            _layout.CapsuleSpawn,
            _layout.PlayerSpawn,
            map.MazeWalls);

        var interaction = player.GetComponent<PlayerInteraction>();
        var canvas = GameUIBuilder.CreateScreenCanvas("GameCanvas", Camera.main);
        var joystick = GameUIBuilder.CreateJoystick(canvas.transform);
        player.GetComponent<PlayerController>().SetJoystick(joystick.joystick);

        var hudResult = GameUIBuilder.CreateHUD(canvas.transform, interaction);

        NavMeshBaker.BakeForMap(map.Bounds);
        SpawnMonster(_layout.MonsterSpawn, _layout.MonsterWaypoints);
    }

    private void ResolvePrefabs()
    {
        var reg = HorrorAssetRegistry.Instance;
        if (reg != null)
        {
            if (protectiveSuitVisualPrefab == null) protectiveSuitVisualPrefab = reg.protectiveSuitVisualPrefab;
            if (backroomsLevelPrefab == null) backroomsLevelPrefab = reg.backroomsLevelPrefab;
            if (entityVisualPrefab == null) entityVisualPrefab = reg.mutantVisualPrefab;
            if (entityAnimatorController == null) entityAnimatorController = reg.mutantAnimatorController;
            if (entityChaseClips == null || entityChaseClips.Length == 0) entityChaseClips = reg.entityChaseClips;
        }
#if UNITY_EDITOR
        if (protectiveSuitVisualPrefab == null)
        {
            protectiveSuitVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        }

        if (backroomsLevelPrefab == null)
            backroomsLevelPrefab = BackroomsMapBuilder.LoadDefaultPrefab();

        if (entityVisualPrefab == null)
            entityVisualPrefab = MutantVisualSetup.LoadPrefab();


        if (entityAnimatorController == null)
        {
            entityAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Zombie_Mutant/Animations/EntityLocomotion.controller");
        }

        if (entityChaseClips == null || entityChaseClips.Length == 0)
            entityChaseClips = EntityChaseAudioSetup.LoadChaseClips();

        if (entityChaseClips == null || entityChaseClips.Length == 0)
            Debug.LogWarning("[Map01Bootstrap] Entity chase clips not found. Check Assets/Backrooms Entity SFX folder.");
#endif
    }

    private GameObject SpawnPlayer(Vector3 position)
    {
        position = MapFloor.PlaceOnFloor(position);

        var player = new GameObject("Player");
        player.tag = "Player";
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            player.layer = playerLayer;
        player.transform.position = position;

        if (protectiveSuitVisualPrefab != null)
        {
            var visual = Instantiate(protectiveSuitVisualPrefab, player.transform);
            visual.name = "ProtectiveSuitVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            MaterialURPFixer.FixHierarchy(visual);
            PlayerLocomotionSetup.Apply(visual, player);
            PlayerFirstPersonSetup.HideBodyRenderers(visual);
        }

        var rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = false;

        var capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 1.8f;
        capsule.radius = 0.35f;
        capsule.center = new Vector3(0f, 0.9f, 0f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerInteraction>();
        player.AddComponent<PlayerFootAlignRunner>();

        position = MapFloor.PlaceOnFloor(position);
        player.transform.position = position;
        player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        rb.position = position;

        var visualRoot = player.transform.Find("ProtectiveSuitVisual");
        if (visualRoot != null)
            PlayerFootAlign.AlignVisualToFloor(visualRoot.gameObject);

        var zone = new GameObject("InteractionZone");
        zone.transform.SetParent(player.transform);
        zone.transform.localPosition = Vector3.zero;
        var trigger = zone.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.5f;

        player.AddComponent<InteractionPrompt>();
        return player;
    }

    private void SetupFirstPersonCamera(Transform player)
    {
        var cam = Camera.main;
        if (cam == null) return;

        var pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player, false);
        pivot.transform.localPosition = new Vector3(0f, 1.0f, 0f);

        cam.transform.SetParent(pivot.transform, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 78f;
        cam.nearClipPlane = 0.08f;
        cam.backgroundColor = new Color(0.4f, 0.48f, 0.34f);

        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (listener.gameObject != cam.gameObject)
                listener.enabled = false;
        }

        var fp = pivot.AddComponent<FirstPersonCamera>();
        fp.Configure(player, cam.transform, FirstPersonCamera.DefaultMouseSensitivity);

        PlayerFirstPersonSetup.AttachFlashlight(cam.transform);

        var huntedAudio = cam.gameObject.AddComponent<PlayerHuntedAudio>();
        huntedAudio.Initialize(entityChaseClips);
    }

    private void SpawnMonster(Vector3 pos, Vector3[] patrolPoints)
    {
        pos = MapFloor.PlaceOnFloor(pos);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            pos = EntitySpawnHelper.ResolveSpawnPosition(pos, player.transform.position, patrolPoints);

        var entity = new GameObject("Entity_Map01");
        entity.tag = "Monster";
        entity.transform.position = pos;

        if (player != null)
        {
            Vector3 toPlayer = player.transform.position - pos;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
                entity.transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
        }

        GameObject entityVisual = null;

        if (entityVisualPrefab != null)
        {
            entityVisual = Instantiate(entityVisualPrefab, entity.transform);
            entityVisual.name = "MutantVisual";
            entityVisual.transform.localPosition = Vector3.zero;
            entityVisual.transform.localRotation = Quaternion.identity;
            MutantVisualSetup.Apply(entityVisual, entityAnimatorController);
        }
        else
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "BodyFallback";
            body.transform.SetParent(entity.transform);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
            body.GetComponent<Renderer>().material.color = new Color(0.15f, 0.08f, 0.12f);
            Object.Destroy(body.GetComponent<Collider>());
        }

        var agent = entity.AddComponent<UnityEngine.AI.NavMeshAgent>();
        float entityScale = MutantVisualSetup.VisualScale;
        agent.speed = 0f;
        agent.stoppingDistance = 0.2f;
        agent.angularSpeed = 540f;
        agent.acceleration = 24f;
        agent.height = 2.1f * entityScale;
        agent.radius = 0.55f * entityScale;
        agent.baseOffset = 0f;

        var entityRb = entity.AddComponent<Rigidbody>();
        entityRb.isKinematic = true;
        entityRb.useGravity = false;

        var vision = entity.AddComponent<MonsterVision>();
        vision.Configure(
            LayerMask.GetMask("Player", "Default"),
            LayerMask.GetMask("Default"),
            visionRadius: 20f,
            fov: 130f,
            proximity: 0f);

        var waypoints = new Transform[patrolPoints.Length];
        for (int i = 0; i < patrolPoints.Length; i++)
            waypoints[i] = CreateWaypoint(entity.transform, $"WP_{i}", patrolPoints[i]);

        var ai = entity.AddComponent<MonsterAI>();
        ai.Configure(waypoints);

        if (!EntityNavMeshPlacement.TryWarp(agent, pos))
            Debug.LogWarning("[Entity] Spawned off NavMesh — movement may fail.");

        entity.transform.position = agent.transform.position;
        ai.BeginPatrol();

        if (entityVisual != null)
        {
            var animator = entity.GetComponentInChildren<Animator>();
            var locomotion = entity.AddComponent<MonsterLocomotion>();
            locomotion.Bind(agent, animator);
            MutantVisualSetup.FinishSpawnAlignment(entity.transform, entityVisual);
        }

        var attack = new GameObject("AttackZone");
        attack.transform.SetParent(entity.transform);
        attack.transform.localPosition = new Vector3(0f, 1f * entityScale, 0.35f * entityScale);
        var atkCol = attack.AddComponent<SphereCollider>();
        atkCol.isTrigger = true;
        atkCol.radius = 0.42f * entityScale;
        attack.AddComponent<MonsterAttackZone>();
    }

    private static Transform CreateWaypoint(Transform parent, string name, Vector3 worldPos)
    {
        var wp = new GameObject(name).transform;
        wp.SetParent(parent);
        wp.position = worldPos;
        return wp;
    }
}

internal struct Map01Layout
{
    public Vector3 PlayerSpawn;
    public Vector3 CapsuleSpawn;
    public Vector3 MonsterSpawn;
    public Vector3[] MonsterWaypoints;

    public static Map01Layout ForBackrooms(Bounds bounds, bool[,] mazeWalls)
    {
        float walkY = MapFloor.WalkY;
        float minX = bounds.min.x + 2f;
        float maxX = bounds.max.x - 2f;
        float minZ = bounds.min.z + 2f;
        float maxZ = bounds.max.z - 2f;
        float midX = bounds.center.x;

        Vector3 Snap(Vector3 hint) => MazeGenerator.SnapToPassage(mazeWalls, hint, 3f, walkY);

        var playerHint = new Vector3(midX, walkY, minZ + 1.5f);
        var monsterHint = playerHint + new Vector3(0f, 0f, 8f);

        return new Map01Layout
        {
            PlayerSpawn = Snap(playerHint),
            CapsuleSpawn = Snap(new Vector3(maxX - 1f, walkY + 0.2f, maxZ - 1f)),
            MonsterSpawn = Snap(monsterHint),
            MonsterWaypoints = new[]
            {
                Snap(new Vector3(minX + 2f, walkY, bounds.center.z - 1f)),
                Snap(monsterHint),
                Snap(new Vector3(maxX - 4f, walkY, bounds.center.z + 2f)),
                Snap(new Vector3(midX - 2f, walkY, maxZ - 5f))
            }
        };
    }
}
