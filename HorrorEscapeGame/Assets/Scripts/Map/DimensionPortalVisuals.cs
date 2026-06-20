using UnityEngine;

public static class DimensionPortalVisuals
{
    private static Texture2D _softParticleTexture;

    public struct VisualRefs
    {
        public Light PortalLight;
        public Transform Core;
        public ParticleSystem Particles;
        public AudioSource HumSource;
        public Transform DoorFrame;
        public Renderer PortalView;
    }

    public static VisualRefs Build(Transform root, PortalTransitionKind kind, bool showDoorPreview = false)
    {
        var palette = kind == PortalTransitionKind.Return
            ? new Color(0.82f, 0.88f, 1f)
            : new Color(0.18f, 0.62f, 1f);

        var coreColor = kind == PortalTransitionKind.Return
            ? new Color(0.92f, 0.95f, 1f)
            : new Color(0.55f, 0.86f, 1f);

        var stain = CreateQuad(root, "FloorStain", new Vector3(0f, 0.02f, 0f),
            new Vector3(3.4f, 3.4f, 1f), Quaternion.Euler(90f, 0f, 0f),
            new Color(0.03f, 0.04f, 0.05f, 0.85f));

        var outerRing = CreateCylinder(root, "OuterRing", new Vector3(0f, 1.35f, 0f),
            new Vector3(2.5f, 0.05f, 2.5f), palette * 0.55f);

        var innerRing = CreateCylinder(root, "InnerRing", new Vector3(0f, 1.35f, 0f),
            new Vector3(1.85f, 0.07f, 1.85f), palette * 0.85f);

        var core = CreateCylinder(root, "Core", new Vector3(0f, 1.35f, 0f),
            new Vector3(0.55f, 1.45f, 0.55f), coreColor);

        Transform doorFrame = null;
        Renderer portalView = null;
        if (showDoorPreview)
        {
            core.SetActive(false);
            innerRing.SetActive(false);
            outerRing.SetActive(false);
            (doorFrame, portalView) = BuildDoorFrame(root, palette);
        }

        var lightGo = new GameObject("PortalLight");
        lightGo.transform.SetParent(root, false);
        lightGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 11f;
        light.intensity = 0.35f;
        light.color = palette;
        light.shadows = LightShadows.None;

        var particles = showDoorPreview ? null : CreatePullParticles(root, palette);

        var humGo = new GameObject("PortalHum");
        humGo.transform.SetParent(root, false);
        var hum = humGo.AddComponent<AudioSource>();
        hum.loop = true;
        hum.playOnAwake = false;
        hum.spatialBlend = 1f;
        hum.minDistance = 2f;
        hum.maxDistance = 18f;
        hum.volume = 0f;
        var humClip = PortalAudioSetup.LoadHumLoop();
        if (humClip != null)
        {
            hum.clip = humClip;
            hum.Play();
        }

        Object.Destroy(stain.GetComponent<Collider>());
        Object.Destroy(outerRing.GetComponent<Collider>());
        Object.Destroy(innerRing.GetComponent<Collider>());
        Object.Destroy(core.GetComponent<Collider>());

        return new VisualRefs
        {
            PortalLight = light,
            Core = core.transform,
            Particles = particles,
            HumSource = hum,
            DoorFrame = doorFrame,
            PortalView = portalView
        };
    }

    private static (Transform frame, Renderer view) BuildDoorFrame(Transform root, Color palette)
    {
        var frameRoot = new GameObject("DoorFrame").transform;
        frameRoot.SetParent(root, false);
        frameRoot.localPosition = new Vector3(0f, 1.35f, 0f);

        const float width = 2.2f;
        const float height = 2.6f;
        const float depth = 0.18f;
        var frameColor = new Color(0.08f, 0.09f, 0.1f, 1f);
        var trimColor = palette * 0.35f;

        CreateBox(frameRoot, "LeftJamb", new Vector3(-width * 0.5f, 0f, 0f),
            new Vector3(depth, height, depth), frameColor);
        CreateBox(frameRoot, "RightJamb", new Vector3(width * 0.5f, 0f, 0f),
            new Vector3(depth, height, depth), frameColor);
        CreateBox(frameRoot, "TopLinte", new Vector3(0f, height * 0.5f, 0f),
            new Vector3(width + depth, depth, depth), frameColor);
        CreateBox(frameRoot, "TopTrim", new Vector3(0f, height * 0.5f + 0.08f, -0.02f),
            new Vector3(width + depth * 2f, 0.08f, 0.12f), trimColor);

        var viewGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        viewGo.name = "PortalView";
        viewGo.transform.SetParent(frameRoot, false);
        viewGo.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        viewGo.transform.localRotation = Quaternion.identity;
        viewGo.transform.localScale = new Vector3(width - 0.15f, height - 0.12f, 1f);
        Object.Destroy(viewGo.GetComponent<Collider>());
        ApplyColor(viewGo, new Color(0.04f, 0.05f, 0.04f, 1f));

        return (frameRoot, viewGo.GetComponent<Renderer>());
    }

    private static GameObject CreateBox(
        Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        ApplyColor(go, color);
        Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    private static ParticleSystem CreatePullParticles(Transform root, Color tint)
    {
        var go = new GameObject("PortalParticles");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, 1.35f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.1f;
        main.startSpeed = 0.35f;
        main.startSize = 0.08f;
        main.maxParticles = 48;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new ParticleSystem.MinMaxGradient(tint * 0.9f);

        var emission = ps.emission;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.35f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.8f, -1.6f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ConfigureParticleRenderer(ps, tint);
        return ps;
    }

    private static void ConfigureParticleRenderer(ParticleSystem ps, Color tint)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer == null) return;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) return;

        var mat = new Material(shader);
        mat.SetTexture("_BaseMap", GetSoftParticleTexture());
        mat.SetColor("_BaseColor", tint);
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static Texture2D GetSoftParticleTexture()
    {
        if (_softParticleTexture != null)
            return _softParticleTexture;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (size - 1) * 0.5f;
        float radius = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha *= alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply(false);
        _softParticleTexture = tex;
        return tex;
    }

    private static GameObject CreateCylinder(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        ApplyColor(go, color);
        return go;
    }

    private static GameObject CreateQuad(
        Transform parent,
        string name,
        Vector3 localPos,
        Vector3 scale,
        Quaternion rotation,
        Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        ApplyColor(go, color);
        return go;
    }

    private static void ApplyColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        var mat = renderer.sharedMaterial;
        if (mat == null) return;

        var instance = Object.Instantiate(mat);
        if (instance.HasProperty("_BaseColor"))
            instance.SetColor("_BaseColor", color);
        else if (instance.HasProperty("_Color"))
            instance.SetColor("_Color", color);

        if (instance.HasProperty("_EmissionColor"))
        {
            instance.EnableKeyword("_EMISSION");
            instance.SetColor("_EmissionColor", color * 1.4f);
        }

        renderer.sharedMaterial = instance;
    }
}
