using UnityEngine;

public static class RealityCityPlayerSetup
{
    public static GameObject Spawn(Vector3 position, Bounds mapBounds, Collider road, float streetBaseline, float boundsInset = 4f)
    {
        position.y = streetBaseline;

        var player = new GameObject("Player");
        player.tag = "Player";
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            player.layer = playerLayer;
        player.transform.position = position;

        var rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = false;

        var capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 1.8f;
        capsule.radius = 0.35f;
        capsule.center = new Vector3(0f, 0.9f, 0f);

        player.AddComponent<PlayerController>().EnableRoadFollow(road, streetBaseline);
        player.AddComponent<PlayerMapBounds>().Configure(mapBounds, inset: boundsInset);
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerInteraction>();

        player.transform.position = position;
        rb.position = position;

        return player;
    }

    public static void SetupCityCamera(Transform player, Camera cam)
    {
        DemoCityViewpoint.SetupCamera(player, cam);
    }

    public static void CreateWalkUi(Transform canvasRoot, PlayerInteraction interaction, string hintText)
    {
        var joystick = GameUIBuilder.CreateJoystick(canvasRoot);
        var controller = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        controller?.SetJoystick(joystick.joystick);

        GameUIBuilder.CreateHUD(canvasRoot, interaction);

        if (!string.IsNullOrEmpty(hintText))
            CreateHintLabel(canvasRoot, hintText);
    }

    private static void CreateHintLabel(Transform canvasRoot, string text)
    {
        var go = new GameObject("ObjectiveHint", typeof(RectTransform));
        go.transform.SetParent(canvasRoot, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 36f);
        rt.sizeDelta = new Vector2(900f, 80f);

        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        TmpFontHelper.ApplyDefaultFont(tmp);
        tmp.text = text;
        tmp.fontSize = 28;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.15f, 0.17f, 0.22f, 0.95f);
    }
}
