using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Map00Bootstrap : MonoBehaviour
{
    [SerializeField] private GameObject cityPrefab;

    private void Awake()
    {
        ResolveAssets();
        GameUIBuilder.EnsureCoreSystems();

        var map = DemoCityMapBuilder.Build(cityPrefab);
        if (map.Root == null) return;

        DemoCityAtmosphere.Apply(map.PlayBounds, DemoCityAtmosphere.Mood.DayPrologue);

        var layout = DemoCityLayout.ForCity(map.RoadCollider, map.StreetBaselineY);
        MapFloor.SetWalkY(map.StreetBaselineY);

        var player = RealityCityPlayerSetup.Spawn(
            layout.PlayerSpawn,
            map.PlayBounds,
            map.RoadCollider,
            map.StreetBaselineY,
            boundsInset: 2f);

        Vector3 lookDir = layout.FallTriggerCenter - layout.PlayerSpawn;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
            player.transform.rotation = Quaternion.LookRotation(lookDir.normalized);

        RealityCityPlayerSetup.SetupCityCamera(player.transform, Camera.main);
        ApplyPrologueCameraFeel(Camera.main);

        var interaction = player.GetComponent<PlayerInteraction>();
        var canvas = GameUIBuilder.CreateScreenCanvas("GameCanvas", Camera.main);
        RealityCityPlayerSetup.CreateWalkUi(canvas.transform, interaction, "Walk forward...");
        VillageAnalogOverlay.Attach();

        SpawnProloguePortal(layout);
        StartCoroutine(DemoCityViewpoint.StabilizeAfterPhysics(
            this, player.transform, map.RoadCollider, map.StreetBaselineY));
    }

    private void ResolveAssets()
    {
#if UNITY_EDITOR
        if (cityPrefab == null)
            cityPrefab = DemoCityMapBuilder.LoadDefaultPrefab();
#endif
    }

    private static void ApplyPrologueCameraFeel(Camera cam)
    {
        if (cam == null) return;

        cam.fieldOfView = 64f;
        cam.backgroundColor = new Color(0.78f, 0.74f, 0.66f);
    }

    private static void SpawnProloguePortal(DemoCityLayout layout)
    {
        var triggerGo = new GameObject("DimensionPortal");
        triggerGo.transform.position = layout.FallTriggerCenter + Vector3.up * layout.FallTriggerSize.y * 0.5f;

        var col = triggerGo.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = layout.FallTriggerSize;

        triggerGo.AddComponent<DimensionFallTrigger>();

        var pit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pit.name = "FallPitMarker";
        pit.transform.SetParent(triggerGo.transform, false);
        pit.transform.localPosition = new Vector3(0f, -layout.FallTriggerSize.y * 0.5f + 0.04f, 0f);
        pit.transform.localScale = new Vector3(3.2f, 0.03f, 3.2f);
        Object.Destroy(pit.GetComponent<Collider>());

        var renderer = pit.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.08f, 0.08f, 0.1f, 1f);
    }
}
