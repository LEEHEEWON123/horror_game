#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using UnityEngine;

public class Map04Bootstrap : MonoBehaviour
{
    [SerializeField] private GameObject protectiveSuitVisualPrefab;
    [SerializeField] private GameObject pumpkinVisualPrefab;
    [SerializeField] private RuntimeAnimatorController entityAnimatorController;
    [SerializeField] private AudioClip[] entityChaseClips;

    private Map04Layout _layout;
    private Bounds _mapBounds;
    private float _groundFallbackY;

    private void Awake()
    {
        ResolveAssets();
        GameUIBuilder.EnsureCoreSystems();

        var map = PrototypeMapMapBuilder.Build();
        _mapBounds = map.Bounds;
        _groundFallbackY = map.WalkSurfaceY;
        _layout = Map04Layout.ForPrototype(map.Bounds);

        bool randomSpawn = Map04RespawnState.UseRandomSpawn;
        var spawn = randomSpawn
            ? Map04Layout.PickRandomPlayerSpawn(map.Bounds, map.WalkSurfaceY)
            : _layout.PlayerSpawn;
        Map04RespawnState.UseRandomSpawn = false;

        float walkY = randomSpawn
            ? spawn.y
            : Map04Viewpoint.ResolveSpawnY(spawn, map.Bounds, map.WalkSurfaceY);
        MapFloor.SetWalkY(walkY);
        spawn.y = walkY;

        var player = SpawnPlayer(spawn, map.WalkSurfaceY);
        player.GetComponent<PlayerMapBounds>().Configure(map.Bounds);
        Vector3 lookDir = map.Bounds.center - spawn;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
            player.transform.rotation = Quaternion.LookRotation(lookDir.normalized);

        Map04Viewpoint.SetupCamera(player.transform, Camera.main);
        var cam = Camera.main;
        if (cam != null)
        {
            var flashlight = PlayerFirstPersonSetup.AttachFlashlight(cam.transform);
            flashlight.intensity = 0.1f;
            flashlight.range = 14f;

            var huntedAudio = cam.GetComponent<PlayerHuntedAudio>();
            if (huntedAudio == null)
                huntedAudio = cam.gameObject.AddComponent<PlayerHuntedAudio>();
            huntedAudio.Initialize(entityChaseClips);
        }

        StartCoroutine(Map04Viewpoint.StabilizeAfterPhysics(this, player.transform, map.Bounds));

        MapPortalSetup.SpawnForActiveMap(
            _layout.CapsuleSpawn,
            spawn);

        var interaction = player.GetComponent<PlayerInteraction>();
        var canvas = GameUIBuilder.CreateScreenCanvas("GameCanvas", Camera.main);
        var joystick = GameUIBuilder.CreateJoystick(canvas.transform);
        player.GetComponent<PlayerController>().SetJoystick(joystick.joystick);

        GameUIBuilder.CreateHUD(canvas.transform, interaction);

        NavMeshBaker.BakeForMapRoot(map.Root.transform);

        SpawnMonster("Entity_Map04_A", _layout.MonsterSpawns[0], _layout.MonsterWaypoints);
        SpawnMonster("Entity_Map04_B", _layout.MonsterSpawns[1], _layout.MonsterWaypoints);
    }

    private void ResolveAssets()
    {
        var reg = HorrorAssetRegistry.Instance;
        if (reg != null)
        {
            if (protectiveSuitVisualPrefab == null) protectiveSuitVisualPrefab = reg.protectiveSuitVisualPrefab;
            if (pumpkinVisualPrefab == null) pumpkinVisualPrefab = reg.pumpkinVisualPrefab;
            if (entityAnimatorController == null) entityAnimatorController = reg.pumpkinAnimatorController;
            if (entityChaseClips == null || entityChaseClips.Length == 0) entityChaseClips = reg.entityChaseClips;
        }
#if UNITY_EDITOR
        if (protectiveSuitVisualPrefab == null)
        {
            protectiveSuitVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        }

        if (pumpkinVisualPrefab == null)
            pumpkinVisualPrefab = PumpkinVisualSetup.LoadDefaultPrefab();


        if (entityAnimatorController == null)
            entityAnimatorController = PumpkinVisualSetup.LoadDefaultController();

        if (entityChaseClips == null || entityChaseClips.Length == 0)
            entityChaseClips = EntityChaseAudioSetup.LoadChaseClips();
#endif
    }

    private GameObject SpawnPlayer(Vector3 position, float groundFallbackY)
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

        player.AddComponent<PlayerController>().EnableGroundFollow(groundFallbackY, maxStepUp: 0.85f, maxStepDown: 4f);
        player.AddComponent<PlayerMapBounds>();
        var health = player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerInteraction>();

        var catchSequence = player.AddComponent<PlayerCatchSequence>();
        Map04Viewpoint.ConfigureCatchView(catchSequence);

        player.transform.position = position;
        player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
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
        pos = PlaceOnTerrain(pos);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            pos = EntitySpawnHelper.ResolveSpawnPosition(pos, player.transform.position, patrolPoints);
        pos = PlaceOnTerrain(pos);

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

        if (pumpkinVisualPrefab != null)
        {
            entityVisual = Instantiate(pumpkinVisualPrefab, entity.transform);
            entityVisual.name = "PumpkinVisual";
            entityVisual.transform.localPosition = Vector3.zero;
            entityVisual.transform.localRotation = Quaternion.identity;
            PumpkinVisualSetup.Apply(entityVisual, entityAnimatorController);
        }

        float entityScale = PumpkinVisualSetup.VisualScale;

        var agent = entity.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = 0f;
        agent.stoppingDistance = 0.2f;
        agent.angularSpeed = 540f;
        agent.acceleration = 24f;
        agent.height = 2.1f * entityScale;
        agent.radius = 0.65f;
        agent.baseOffset = 0f;
        agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        var entityRb = entity.AddComponent<Rigidbody>();
        entityRb.isKinematic = true;
        entityRb.useGravity = false;

        var vision = entity.AddComponent<MonsterVision>();
        vision.Configure(
            LayerMask.GetMask("Player", "Default"),
            LayerMask.GetMask("Default"),
            visionRadius: 40f,
            fov: 150f,
            proximity: 14f * entityScale,
            visionEyeHeight: 1.6f * entityScale);

        var waypoints = new Transform[patrolPoints.Length];
        for (int i = 0; i < patrolPoints.Length; i++)
            waypoints[i] = CreateWaypoint(entity.transform, $"WP_{i}", patrolPoints[i]);

        var ai = entity.AddComponent<MonsterAI>();
        ai.Configure(waypoints);
        ai.ConfigureSpeeds(MonsterAI.DefaultPatrolSpeed, MonsterAI.DefaultChaseSpeed, MonsterAI.DefaultSearchSpeed);
        entity.AddComponent<MonsterGroundFollow>().Configure(_groundFallbackY, entityScale);
        entity.AddComponent<MonsterMapBounds>().Configure(_mapBounds);

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
            EntityVisualSetup.FinishSpawnAlignment(entity.transform, entityVisual);
        }

        var attack = new GameObject("AttackZone");
        attack.transform.SetParent(entity.transform);
        attack.transform.localPosition = new Vector3(0f, 1.0f, 0.4f * entityScale);
        var atkCol = attack.AddComponent<SphereCollider>();
        atkCol.isTrigger = true;
        atkCol.radius = Mathf.Max(0.45f, 0.35f * entityScale);
        attack.AddComponent<MonsterAttackZone>();
    }

    private Vector3 PlaceOnTerrain(Vector3 worldPos)
    {
        float minY = _mapBounds.min.y + 0.05f;
        float y = SpawnHelper.QueryBoundedFloorY(worldPos, _mapBounds, _groundFallbackY, minY);
        return new Vector3(worldPos.x, y, worldPos.z);
    }

    private static Transform CreateWaypoint(Transform parent, string name, Vector3 worldPos)
    {
        var wp = new GameObject(name).transform;
        wp.SetParent(parent);
        wp.position = worldPos;
        return wp;
    }
}

internal struct Map04Layout
{
    public Vector3 PlayerSpawn;
    public Vector3 CapsuleSpawn;
    public Vector3[] MonsterSpawns;
    public Vector3[] MonsterWaypoints;

    public static Map04Layout ForPrototype(Bounds bounds)
    {
        float groundY = MapFloor.WalkY;
        float minY = bounds.min.y + 0.05f;
        float minX = bounds.min.x;
        float maxX = bounds.max.x;
        float minZ = bounds.min.z;
        float maxZ = bounds.max.z;
        float midX = bounds.center.x;
        float midZ = bounds.center.z;
        float laneInsetX = bounds.size.x * 0.2f;
        float plazaInsetZ = bounds.size.z * 0.1f;

        Vector3 OnFloor(Vector3 xz)
        {
            float y = SpawnHelper.QueryBoundedFloorY(xz, bounds, groundY, minY);
            return new Vector3(xz.x, y, xz.z);
        }

        return new Map04Layout
        {
            PlayerSpawn = OnFloor(new Vector3(midX, 0f, midZ - bounds.size.z * 0.12f)),
            CapsuleSpawn = OnFloor(new Vector3(midX, 0f, maxZ - plazaInsetZ)),
            MonsterSpawns = new[]
            {
                OnFloor(new Vector3(maxX - laneInsetX, 0f, midZ - bounds.size.z * 0.12f)),
                OnFloor(new Vector3(minX + laneInsetX, 0f, midZ + bounds.size.z * 0.08f))
            },
            MonsterWaypoints = new[]
            {
                OnFloor(new Vector3(minX + laneInsetX, 0f, minZ + bounds.size.z * 0.18f)),
                OnFloor(new Vector3(maxX - laneInsetX, 0f, minZ + bounds.size.z * 0.18f)),
                OnFloor(new Vector3(maxX - laneInsetX, 0f, maxZ - bounds.size.z * 0.18f)),
                OnFloor(new Vector3(minX + laneInsetX, 0f, maxZ - bounds.size.z * 0.18f))
            }
        };
    }

    public static Vector3 PickRandomPlayerSpawn(Bounds bounds, float groundY, float inset = 1.25f)
    {
        var clamped = MapBoundaryBuilder.ClampBounds(bounds, inset);
        float minY = bounds.min.y + 0.05f;
        float margin = Mathf.Min(clamped.size.x, clamped.size.z) * 0.06f;
        float minX = clamped.min.x + margin;
        float maxX = clamped.max.x - margin;
        float minZ = clamped.min.z + margin;
        float maxZ = clamped.max.z - margin;

        if (maxX <= minX || maxZ <= minZ)
            return ForPrototype(bounds).PlayerSpawn;

        for (int attempt = 0; attempt < 32; attempt++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            var xz = new Vector3(x, 0f, z);
            float y = SpawnHelper.QueryBoundedFloorY(xz, bounds, groundY, minY);
            return new Vector3(x, y, z);
        }

        return ForPrototype(bounds).PlayerSpawn;
    }
}
