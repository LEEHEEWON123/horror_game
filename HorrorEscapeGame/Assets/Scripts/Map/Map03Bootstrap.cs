#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Map03Bootstrap : MonoBehaviour
{
    [SerializeField] private GameObject protectiveSuitVisualPrefab;
    [SerializeField] private GameObject sponzaPrefab;
    [SerializeField] private Texture2D smilerTexture;
    [SerializeField] private AudioClip[] entityChaseClips;

    private Map03Layout _layout;

    private void Awake()
    {
        ResolveAssets();
        GameUIBuilder.EnsureCoreSystems();

        var map = SponzaMazeMapBuilder.Build(sponzaPrefab);
        _layout = Map03Layout.ForSponzaMaze(map);

        SponzaAtmosphere.Apply(map.Bounds, map.RoomHeight);

        var player = SpawnPlayer(_layout.PlayerSpawn);
        SetupFirstPersonCamera(player.transform);

        MapPortalSetup.SpawnForActiveMap(
            _layout.CapsuleSpawn + Vector3.up * 0.8f,
            _layout.PlayerSpawn,
            map.MazeWalls,
            map.TileSize,
            snapToFloor: false);

        var interaction = player.GetComponent<PlayerInteraction>();
        var canvas = GameUIBuilder.CreateScreenCanvas("GameCanvas", Camera.main);
        var joystick = GameUIBuilder.CreateJoystick(canvas.transform);
        player.GetComponent<PlayerController>().SetJoystick(joystick.joystick);

        GameUIBuilder.CreateHUD(canvas.transform, interaction);
        NavMeshBaker.BakeForMapRoot(map.Root.transform, carveWalls: false);

        SpawnSmiler("Smiler_Map03_A", _layout.SmilerSpawn, _layout.SmilerWaypoints);
        SpawnSmiler("Smiler_Map03_B", _layout.Smiler2Spawn, _layout.Smiler2Waypoints);
    }

    private void ResolveAssets()
    {
#if UNITY_EDITOR
        if (protectiveSuitVisualPrefab == null)
        {
            protectiveSuitVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        }

        if (sponzaPrefab == null)
            sponzaPrefab = SponzaMazeMapBuilder.LoadDefaultPrefab();

        if (smilerTexture == null)
            smilerTexture = SmilerVisualSetup.LoadTexture();

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
        Map03Viewpoint.SetupCamera(player, Camera.main);
        var cam = Camera.main;
        if (cam == null) return;

        var flashlight = PlayerFirstPersonSetup.AttachFlashlight(cam.transform);
        flashlight.intensity = 0.72f;
        flashlight.range = 24f;
        flashlight.spotAngle = 68f;

        var huntedAudio = cam.gameObject.AddComponent<PlayerHuntedAudio>();
        huntedAudio.Initialize(entityChaseClips);
    }

    private void SpawnSmiler(string entityName, Vector3 pos, Vector3[] patrolPoints)
    {
        var entity = new GameObject(entityName);
        entity.tag = "Monster";
        entity.transform.position = pos;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 toPlayer = player.transform.position - pos;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
                entity.transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
        }

        SmilerVisualSetup.Apply(entity.transform, smilerTexture);

        var agent = entity.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = 0f;
        agent.stoppingDistance = 0.15f;
        agent.angularSpeed = 540f;
        agent.acceleration = 24f;
        agent.height = 2f;
        agent.radius = 0.32f;
        agent.baseOffset = 0f;

        var entityRb = entity.AddComponent<Rigidbody>();
        entityRb.isKinematic = true;
        entityRb.useGravity = false;

        var vision = entity.AddComponent<MonsterVision>();
        vision.Configure(
            LayerMask.GetMask("Player", "Default"),
            LayerMask.GetMask("Default"),
            visionRadius: 18f,
            fov: 140f,
            proximity: 0f);

        var waypoints = new Transform[patrolPoints.Length];
        for (int i = 0; i < patrolPoints.Length; i++)
            waypoints[i] = CreateWaypoint(entity.transform, $"WP_{i}", patrolPoints[i]);

        var ai = entity.AddComponent<MonsterAI>();
        ai.Configure(waypoints);

        if (!EntityNavMeshPlacement.TryWarp(agent, pos))
            Debug.LogWarning($"[{entityName}] Spawned off NavMesh — movement may fail.");

        entity.transform.position = agent.transform.position;
        ai.BeginPatrol();

        var attack = new GameObject("AttackZone");
        attack.transform.SetParent(entity.transform);
        attack.transform.localPosition = new Vector3(0f, 1.0f, 0.25f);
        var atkCol = attack.AddComponent<SphereCollider>();
        atkCol.isTrigger = true;
        atkCol.radius = 0.38f;
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

internal struct Map03Layout
{
    public Vector3 PlayerSpawn;
    public Vector3 CapsuleSpawn;
    public Vector3 SmilerSpawn;
    public Vector3[] SmilerWaypoints;
    public Vector3 Smiler2Spawn;
    public Vector3[] Smiler2Waypoints;

    public static Map03Layout ForSponzaMaze(SponzaMazeMapBuilder.BuildResult map)
    {
        float walkY = map.WalkSurfaceY;
        float tile = map.TileSize;
        var bounds = map.Bounds;
        float minX = bounds.min.x + tile * 0.5f;
        float maxX = bounds.max.x - tile * 0.5f;
        float minZ = bounds.min.z + tile * 0.5f;
        float maxZ = bounds.max.z - tile * 0.5f;
        float midX = bounds.center.x;
        float midZ = bounds.center.z;

        Vector3 Snap(Vector3 hint) => MazeGenerator.SnapToPassage(map.MazeWalls, hint, tile, walkY);

        var playerHint = new Vector3(midX, walkY, minZ + tile * 0.35f);
        var smilerHint  = new Vector3(maxX - tile * 0.8f, walkY, midZ);
        var smiler2Hint = new Vector3(minX + tile * 0.8f, walkY, midZ);

        return new Map03Layout
        {
            PlayerSpawn = Snap(playerHint),
            CapsuleSpawn = Snap(new Vector3(maxX - tile * 0.35f, walkY, maxZ - tile * 0.35f)),
            SmilerSpawn = Snap(smilerHint),
            SmilerWaypoints = new[]
            {
                Snap(new Vector3(minX + tile, walkY, midZ)),
                Snap(smilerHint),
                Snap(new Vector3(midX, walkY, maxZ - tile)),
                Snap(new Vector3(maxX - tile * 1.5f, walkY, minZ + tile * 1.2f))
            },
            Smiler2Spawn = Snap(smiler2Hint),
            Smiler2Waypoints = new[]
            {
                Snap(new Vector3(minX + tile * 1.5f, walkY, minZ + tile * 1.2f)),
                Snap(smiler2Hint),
                Snap(new Vector3(midX, walkY, minZ + tile)),
                Snap(new Vector3(minX + tile, walkY, maxZ - tile))
            }
        };
    }
}
