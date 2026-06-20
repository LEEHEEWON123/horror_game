using UnityEngine;

public static class MapBoundaryBuilder
{
    public static void BuildPerimeterWalls(Transform parent, Bounds bounds, float floorY, float inset = 1.25f)
    {
        var root = new GameObject("MapBoundaries");
        root.transform.SetParent(parent, false);

        float height = Mathf.Max(bounds.size.y + 4f, 8f);
        float thickness = 1.5f;
        float minX = bounds.min.x + inset;
        float maxX = bounds.max.x - inset;
        float minZ = bounds.min.z + inset;
        float maxZ = bounds.max.z - inset;
        float width = maxX - minX;
        float depth = maxZ - minZ;
        float centerY = floorY + height * 0.5f;
        float midX = (minX + maxX) * 0.5f;
        float midZ = (minZ + maxZ) * 0.5f;

        CreateWall(root.transform, "Boundary_N", new Vector3(midX, centerY, minZ - thickness * 0.5f),
            new Vector3(width + thickness * 2f, height, thickness));
        CreateWall(root.transform, "Boundary_S", new Vector3(midX, centerY, maxZ + thickness * 0.5f),
            new Vector3(width + thickness * 2f, height, thickness));
        CreateWall(root.transform, "Boundary_W", new Vector3(minX - thickness * 0.5f, centerY, midZ),
            new Vector3(thickness, height, depth + thickness * 2f));
        CreateWall(root.transform, "Boundary_E", new Vector3(maxX + thickness * 0.5f, centerY, midZ),
            new Vector3(thickness, height, depth + thickness * 2f));
    }

    public static Bounds ClampBounds(Bounds bounds, float inset)
    {
        var clamped = bounds;
        clamped.min = new Vector3(bounds.min.x + inset, bounds.min.y, bounds.min.z + inset);
        clamped.max = new Vector3(bounds.max.x - inset, bounds.max.y, bounds.max.z - inset);
        return clamped;
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;

        var renderer = wall.GetComponent<Renderer>();
        if (renderer != null)
            Object.Destroy(renderer);

        wall.isStatic = true;
    }
}
