#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Map07Bootstrap : MonoBehaviour
{
    [SerializeField] private GameObject protectiveSuitVisualPrefab;
    [SerializeField] private GameObject entityVisualPrefab;
    [SerializeField] private RuntimeAnimatorController entityAnimatorController;
    [SerializeField] private AudioClip[] entityChaseClips;

    private Map07Layout _layout;
    private Transform _patrolRoot;
    private Bounds _mapBounds;

    private void Awake()
    {
        ResolvePrefabs();
        GameUIBuilder.EnsureCoreSystems();

        var map = BackroomsLikeMapBuilder.Build();
        _mapBounds = map.Bounds;
        _layout = Map07Layout.ForBackroomsLike(map.Bounds, map.MazeWalls, map.TileSize);
        _patrolRoot = new GameObject("Map07_PatrolRoutes").transform;

        BackroomsAtmosphere.Apply(map.Bounds);

        var player = SpawnPlayer(_layout.PlayerSpawn);
        SetupFirstPersonCamera(player.transform);

        MapPortalSetup.SpawnForActiveMap(
            _layout.CapsuleSpawn,
            _layout.PlayerSpawn,
            map.MazeWalls,
            map.TileSize);

        var interaction = player.GetComponent<PlayerInteraction>();
        var canvas = GameUIBuilder.CreateScreenCanvas("GameCanvas", Camera.main);
        var joystick = GameUIBuilder.CreateJoystick(canvas.transform);
        player.GetComponent<PlayerController>().SetJoystick(joystick.joystick);

        GameUIBuilder.CreateHUD(canvas.transform, interaction);

        NavMeshBaker.BakeForMapRoot(map.Root.transform, carveWalls: false);

        SpawnMonster("Entity_Map07_A", _layout.MonsterSpawns[0], _layout.MonsterWaypoints);
        SpawnMonster("Entity_Map07_B", _layout.MonsterSpawns[1], _layout.MonsterWaypoints);
    }

    private void ResolvePrefabs()
    {
        var reg = HorrorAssetRegistry.Instance;
        if (reg != null)
        {
            if (protectiveSuitVisualPrefab == null) protectiveSuitVisualPrefab = reg.protectiveSuitVisualPrefab;
            if (entityVisualPrefab == null) entityVisualPrefab = reg.insurgentVisualPrefab;
            if (entityAnimatorController == null) entityAnimatorController = reg.insurgentAnimatorController;
            if (entityChaseClips == null || entityChaseClips.Length == 0) entityChaseClips = reg.entityChaseClips;
        }
#if UNITY_EDITOR
        if (protectiveSuitVisualPrefab == null)
        {
            protectiveSuitVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        }

        if (entityVisualPrefab == null)
            entityVisualPrefab = InsurgentVisualSetup.LoadPrefab();

        InsurgentVisualSetup.EnsureHumanoidImport();

        if (entityAnimatorController == null)
            entityAnimatorController = InsurgentVisualSetup.LoadDefaultController();

        if (entityChaseClips == null || entityChaseClips.Length == 0)
            entityChaseClips = EntityChaseAudioSetup.LoadChaseClips();
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

    private void SpawnMonster(string entityName, Vector3 pos, Vector3[] patrolPoints)
    {
        pos = MapFloor.PlaceOnFloor(pos);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            pos = EntitySpawnHelper.ResolveSpawnPosition(pos, player.transform.position, patrolPoints);

        var entity = new GameObject(entityName);
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
            entityVisual.name = "InsurgentVisual";
            entityVisual.transform.localPosition = Vector3.zero;
            entityVisual.transform.localRotation = Quaternion.identity;
            InsurgentVisualSetup.Apply(entityVisual, entityAnimatorController);
        }

        var agent = entity.AddComponent<UnityEngine.AI.NavMeshAgent>();
        float entityScale = InsurgentVisualSetup.VisualScale;
        agent.speed = 0f;
        agent.stoppingDistance = 0.2f;
        agent.angularSpeed = 540f;
        agent.acceleration = 24f;
        agent.height = 2f;
        agent.radius = 0.42f;
        agent.baseOffset = 0f;
        agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;

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
            waypoints[i] = CreateWaypoint($"WP_{entityName}_{i}", patrolPoints[i]);

        var ai = entity.AddComponent<MonsterAI>();
        ai.Configure(waypoints);
        ai.ConfigureSpeeds(MonsterAI.DefaultPatrolSpeed, MonsterAI.DefaultChaseSpeed, MonsterAI.DefaultSearchSpeed);
        entity.AddComponent<MonsterMapBounds>().Configure(_mapBounds, inset: 2f);

        var warpCandidates = new Vector3[patrolPoints.Length + 2];
        warpCandidates[0] = pos;
        warpCandidates[1] = _layout.PlayerSpawn;
        for (int i = 0; i < patrolPoints.Length; i++)
            warpCandidates[i + 2] = patrolPoints[i];

        bool onNavMesh = EntityNavMeshPlacement.TryWarpCandidates(agent, 24f, warpCandidates);
        entity.transform.position = onNavMesh ? agent.transform.position : pos;

        if (!onNavMesh)
            Debug.LogWarning($"[{entityName}] Spawned off NavMesh — patrol/chase may fail.");

        ai.BeginPatrol();

        if (entityVisual != null)
        {
            var animator = entity.GetComponentInChildren<Animator>();
            if (animator != null && entityAnimatorController != null)
            {
                var locomotion = entity.AddComponent<MonsterLocomotion>();
                locomotion.Bind(agent, animator);
            }
            else
            {
                Debug.LogWarning($"[{entityName}] Insurgent animator/controller missing — run Horror Escape > Build Insurgent Locomotion.");
            }

            InsurgentVisualSetup.FinishSpawnAlignment(entity.transform, entityVisual);
        }

        var attack = new GameObject("AttackZone");
        attack.transform.SetParent(entity.transform);
        attack.transform.localPosition = new Vector3(0f, 1f * entityScale, 0.35f * entityScale);
        var atkCol = attack.AddComponent<SphereCollider>();
        atkCol.isTrigger = true;
        atkCol.radius = 0.42f * entityScale;
        attack.AddComponent<MonsterAttackZone>();
    }

    private Transform CreateWaypoint(string name, Vector3 worldPos)
    {
        var wp = new GameObject(name).transform;
        wp.SetParent(_patrolRoot, false);
        wp.position = worldPos;
        return wp;
    }
}

internal struct Map07Layout
{
    public Vector3 PlayerSpawn;
    public Vector3 CapsuleSpawn;
    public Vector3[] MonsterSpawns;
    public Vector3[] MonsterWaypoints;

    public static Map07Layout ForBackroomsLike(Bounds bounds, bool[,] mazeWalls, float tileSize)
    {
        float walkY = MapFloor.WalkY;
        float minX = bounds.min.x + tileSize * 0.5f;
        float maxX = bounds.max.x - tileSize * 0.5f;
        float minZ = bounds.min.z + tileSize * 0.5f;
        float maxZ = bounds.max.z - tileSize * 0.5f;
        float midX = bounds.center.x;
        float midZ = bounds.center.z;

        Vector3 Snap(Vector3 hint) => MazeGenerator.SnapToPassage(mazeWalls, hint, tileSize, walkY);

        var playerHint = new Vector3(midX, walkY, minZ + tileSize * 0.25f);
        var monsterAHint = Snap(new Vector3(maxX - tileSize, walkY, midZ));
        var monsterBHint = Snap(new Vector3(minX + tileSize, walkY, maxZ - tileSize));

        return new Map07Layout
        {
            PlayerSpawn = Snap(playerHint),
            CapsuleSpawn = Snap(new Vector3(maxX - tileSize * 0.35f, walkY + 0.2f, maxZ - tileSize * 0.35f)),
            MonsterSpawns = new[] { monsterAHint, monsterBHint },
            MonsterWaypoints = new[]
            {
                Snap(new Vector3(minX + tileSize, walkY, midZ)),
                Snap(new Vector3(midX, walkY, maxZ - tileSize)),
                Snap(new Vector3(maxX - tileSize * 1.5f, walkY, minZ + tileSize)),
                Snap(new Vector3(midX - tileSize, walkY, minZ + tileSize * 1.5f))
            }
        };
    }
}
