#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Map06Bootstrap : MonoBehaviour
{
    [SerializeField] private GameObject protectiveSuitVisualPrefab;
    [SerializeField] private GameObject entityVisualPrefab;
    [SerializeField] private RuntimeAnimatorController entityAnimatorController;
    [SerializeField] private AudioClip[] entityChaseClips;

    private Map06Layout _layout;
    private Bounds _mapBounds;

    private void Awake()
    {
        ResolveAssets();
        GameUIBuilder.EnsureCoreSystems();

        var map = InteriorsAMapBuilder.Build();
        _mapBounds = map.Bounds;
        _layout = Map06Layout.ForInterior(map.Bounds, map.MazeWalls, map.TileSize, map.WalkSurfaceY, map.MezzanineSurfaceY);

        InteriorsAtmosphere.Apply(map.Bounds);

        var spawn = _layout.PlayerSpawn;
        MapFloor.SetWalkY(map.WalkSurfaceY);
        spawn.y = map.WalkSurfaceY;

        var player = SpawnPlayer(spawn);
        player.GetComponent<PlayerMapBounds>().Configure(map.Bounds, inset: 2f);

        Vector3 lookDir = _layout.CapsuleSpawn - spawn;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
            player.transform.rotation = Quaternion.LookRotation(lookDir.normalized);

        Map06Viewpoint.SetupCamera(player.transform, Camera.main);
        var cam = Camera.main;
        if (cam != null)
        {
            var flashlight = PlayerFirstPersonSetup.AttachFlashlight(cam.transform);
            flashlight.intensity = 0.16f;
            flashlight.range = 16f;
            flashlight.shadows = LightShadows.None;

            var huntedAudio = cam.GetComponent<PlayerHuntedAudio>();
            if (huntedAudio == null)
                huntedAudio = cam.gameObject.AddComponent<PlayerHuntedAudio>();
            huntedAudio.Initialize(entityChaseClips);
        }

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

        SpawnMonster("Entity_Map06_A", _layout.MonsterSpawns[0], _layout.MonsterWaypointsGround);
        SpawnMonster("Entity_Map06_B", _layout.MonsterSpawns[1], _layout.MonsterWaypointsMezz);
    }

    private void ResolveAssets()
    {
#if UNITY_EDITOR
        if (protectiveSuitVisualPrefab == null)
        {
            protectiveSuitVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        }

        if (entityVisualPrefab == null)
            entityVisualPrefab = MutantVisualSetup.LoadPrefab();

        EntityAnimatorAssetBuilder.Build(force: false);

        if (entityAnimatorController == null)
        {
            entityAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Zombie_Mutant/Animations/EntityLocomotion.controller");
        }

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
        player.AddComponent<PlayerMapBounds>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerInteraction>();
        player.AddComponent<PlayerFootAlignRunner>();

        var catchSequence = player.AddComponent<PlayerCatchSequence>();
        Map06Viewpoint.ConfigureCatchView(catchSequence, MutantVisualSetup.VisualScale);

        player.transform.position = position;
        rb.position = position;

        var zone = new GameObject("InteractionZone");
        zone.transform.SetParent(player.transform);
        zone.transform.localPosition = Vector3.zero;
        var trigger = zone.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.5f;

        player.AddComponent<InteractionPrompt>();
        return player;
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
            entityVisual.name = "MutantVisual";
            entityVisual.transform.localPosition = Vector3.zero;
            entityVisual.transform.localRotation = Quaternion.identity;
            MutantVisualSetup.Apply(entityVisual, entityAnimatorController);
        }

        float entityScale = MutantVisualSetup.VisualScale;

        var agent = entity.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = 0f;
        agent.stoppingDistance = 0.2f;
        agent.angularSpeed = 540f;
        agent.acceleration = 24f;
        agent.height = 2.1f * entityScale;
        agent.radius = 0.4f * entityScale;
        agent.baseOffset = 0f;

        var entityRb = entity.AddComponent<Rigidbody>();
        entityRb.isKinematic = true;
        entityRb.useGravity = false;

        var vision = entity.AddComponent<MonsterVision>();
        vision.Configure(
            LayerMask.GetMask("Player", "Default"),
            LayerMask.GetMask("Default"),
            visionRadius: 36f,
            fov: 145f,
            proximity: 12f * entityScale,
            visionEyeHeight: 1.6f * entityScale);

        var waypoints = new Transform[patrolPoints.Length];
        for (int i = 0; i < patrolPoints.Length; i++)
            waypoints[i] = CreateWaypoint(entity.transform, $"WP_{i}", patrolPoints[i]);

        var ai = entity.AddComponent<MonsterAI>();
        ai.Configure(waypoints);
        ai.ConfigureSpeeds(MonsterAI.DefaultPatrolSpeed, MonsterAI.DefaultChaseSpeed, MonsterAI.DefaultSearchSpeed);
        entity.AddComponent<MonsterMapBounds>().Configure(_mapBounds, inset: 2f);

        if (EntityNavMeshPlacement.TryWarp(agent, pos, 20f))
        {
            entity.transform.position = agent.transform.position;
            ai.BeginPatrol();
        }
        else
        {
            Debug.LogWarning($"[{entityName}] Spawned off NavMesh — AI disabled.");
            ai.enabled = false;
            agent.enabled = false;
        }

        if (entityVisual != null)
        {
            var animator = entity.GetComponentInChildren<Animator>();
            var locomotion = entity.AddComponent<MonsterLocomotion>();
            locomotion.Bind(agent, animator);
            MutantVisualSetup.FinishSpawnAlignment(entity.transform, entityVisual);
        }

        var attack = new GameObject("AttackZone");
        attack.transform.SetParent(entity.transform);
        attack.transform.localPosition = new Vector3(0f, 1.0f, 0.35f * entityScale);
        var atkCol = attack.AddComponent<SphereCollider>();
        atkCol.isTrigger = true;
        atkCol.radius = Mathf.Max(0.42f, 0.35f * entityScale);
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

internal struct Map06Layout
{
    public Vector3 PlayerSpawn;
    public Vector3 CapsuleSpawn;
    public Vector3[] MonsterSpawns;
    public Vector3[] MonsterWaypointsGround;
    public Vector3[] MonsterWaypointsMezz;

    public static Map06Layout ForInterior(
        Bounds bounds,
        bool[,] mazeWalls,
        float tileSize,
        float groundY,
        float mezzY)
    {
        float minX = bounds.min.x;
        float maxX = bounds.max.x;
        float minZ = bounds.min.z;
        float maxZ = bounds.max.z;
        float midX = bounds.center.x;
        float midZ = bounds.center.z;

        Vector3 SnapGround(Vector3 hint) => MazeGenerator.SnapToPassage(mazeWalls, hint, tileSize, groundY);
        Vector3 SnapMezz(Vector3 hint) => MazeGenerator.SnapToPassage(mazeWalls, hint, tileSize, mezzY);
        Vector3 OnGround(Vector3 pos) => new Vector3(pos.x, groundY, pos.z);
        Vector3 OnMezz(Vector3 pos) => new Vector3(pos.x, mezzY, pos.z);

        return new Map06Layout
        {
            PlayerSpawn = OnGround(SnapGround(new Vector3(midX, 0f, tileSize * 2f))),
            CapsuleSpawn = OnMezz(SnapMezz(new Vector3(maxX - tileSize * 2f, 0f, maxZ - tileSize * 2f))),
            MonsterSpawns = new[]
            {
                OnGround(SnapGround(new Vector3(minX + tileSize * 2f, 0f, midZ))),
                OnMezz(SnapMezz(new Vector3(maxX - tileSize * 2f, 0f, minZ + tileSize * 2f)))
            },
            MonsterWaypointsGround = new[]
            {
                OnGround(SnapGround(new Vector3(midX, 0f, minZ + tileSize * 3f))),
                OnGround(SnapGround(new Vector3(maxX - tileSize * 3f, 0f, midZ))),
                OnGround(SnapGround(new Vector3(midX, 0f, maxZ - tileSize * 3f))),
                OnGround(SnapGround(new Vector3(minX + tileSize * 3f, 0f, midZ)))
            },
            MonsterWaypointsMezz = new[]
            {
                OnMezz(SnapMezz(new Vector3(maxX - tileSize * 2f, 0f, minZ + tileSize * 2f))),
                OnMezz(SnapMezz(new Vector3(maxX - tileSize * 2f, 0f, maxZ - tileSize * 2f))),
                OnMezz(SnapMezz(new Vector3(minX + tileSize * 2f, 0f, maxZ - tileSize * 2f))),
                OnMezz(SnapMezz(new Vector3(minX + tileSize * 2f, 0f, minZ + tileSize * 2f)))
            }
        };
    }
}
