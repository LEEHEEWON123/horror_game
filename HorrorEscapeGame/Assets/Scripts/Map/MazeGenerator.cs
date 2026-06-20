using System;
using System.Collections.Generic;
using UnityEngine;

public static class MazeGenerator
{
    public const int CellCount = 20;

    public static int GridSize => CellCount * 2 + 1;

    private static readonly (int dx, int dz)[] Directions =
    {
        (0, 2), (2, 0), (0, -2), (-2, 0)
    };

    /// <summary>true = 벽, false = 통로. Recursive Backtracking (DFS) 미로.</summary>
    public static bool[,] Generate(int? seed = null) => Generate(CellCount, seed);

    public static bool[,] Generate(int cellCount, int? seed = null)
    {
        int size = cellCount * 2 + 1;
        var walls = new bool[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
                walls[x, z] = true;
        }

        var rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        int startX = size / 2;
        int startZ = 1;

        walls[startX, 0] = false;
        Carve(walls, startX, startZ, size, rng);

        return walls;
    }

    /// <summary>Opens selected wall cells between passages to add loops and dead ends.</summary>
    public static void AddLoopPassages(bool[,] walls, int extraOpenings, int? seed = null)
    {
        if (extraOpenings <= 0) return;

        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);
        var rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        var candidates = new List<(int x, int z)>();

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < depth - 1; z++)
            {
                if (!walls[x, z]) continue;

                int passageNeighbors = 0;
                if (!walls[x + 1, z]) passageNeighbors++;
                if (!walls[x - 1, z]) passageNeighbors++;
                if (!walls[x, z + 1]) passageNeighbors++;
                if (!walls[x, z - 1]) passageNeighbors++;

                if (passageNeighbors >= 2)
                    candidates.Add((x, z));
            }
        }

        Shuffle(candidates, rng);

        int opened = 0;
        for (int i = 0; i < candidates.Count && opened < extraOpenings; i++)
        {
            var (x, z) = candidates[i];
            walls[x, z] = false;
            opened++;
        }
    }

    /// <summary>Turns open rooms into tighter corridors by adding interior pillars.</summary>
    public static void AddSightPillars(bool[,] walls, int? seed = null, int margin = 2)
    {
        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);
        var rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        int midX = width / 2;
        int midZ = depth / 2;

        for (int x = margin; x < width - margin; x++)
        {
            for (int z = margin; z < depth - margin; z++)
            {
                if (walls[x, z]) continue;
                if (Mathf.Abs(x - midX) <= 1 && Mathf.Abs(z - midZ) <= 1) continue;

                int openNeighbors = 0;
                if (!walls[x + 1, z]) openNeighbors++;
                if (!walls[x - 1, z]) openNeighbors++;
                if (!walls[x, z + 1]) openNeighbors++;
                if (!walls[x, z - 1]) openNeighbors++;
                if (openNeighbors < 3) continue;

                if ((x + z) % 2 != 0) continue;
                if (rng.NextDouble() > 0.58f) continue;

                walls[x, z] = true;
            }
        }
    }

    private static void Carve(bool[,] walls, int x, int z, int size, System.Random rng)
    {
        walls[x, z] = false;

        var order = new List<int> { 0, 1, 2, 3 };
        Shuffle(order, rng);

        foreach (int i in order)
        {
            var (dx, dz) = Directions[i];
            int nx = x + dx;
            int nz = z + dz;

            if (nx <= 0 || nx >= size - 1 || nz <= 0 || nz >= size - 1)
                continue;
            if (!walls[nx, nz])
                continue;

            walls[x + dx / 2, z + dz / 2] = false;
            Carve(walls, nx, nz, size, rng);
        }
    }

    private static void Shuffle(List<int> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static void Shuffle(List<(int x, int z)> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static Vector3 SnapToPassage(bool[,] walls, Vector3 worldHint, float tileSize = 3f, float y = 0f)
    {
        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);
        int cx = Mathf.Clamp(Mathf.RoundToInt(worldHint.x / tileSize), 0, width - 1);
        int cz = Mathf.Clamp(Mathf.RoundToInt(worldHint.z / tileSize), 0, depth - 1);

        if (!walls[cx, cz])
            return new Vector3(cx * tileSize, y, cz * tileSize);

        for (int radius = 1; radius < Mathf.Max(width, depth); radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dz) != radius) continue;
                    int x = cx + dx;
                    int z = cz + dz;
                    if (x < 0 || x >= width || z < 0 || z >= depth) continue;
                    if (!walls[x, z])
                        return new Vector3(x * tileSize, y, z * tileSize);
                }
            }
        }

        return worldHint;
    }

    /// <summary>통로 칸 중 기준점에서 가장 멀고, 최소 거리 이상 떨어진 위치.</summary>
    public static Vector3 FindFarthestPassage(
        Vector3 from,
        bool[,] walls,
        float tileSize,
        float y,
        float minSeparation)
    {
        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);
        Vector3 flatFrom = new Vector3(from.x, 0f, from.z);

        Vector3 best = SnapToPassage(walls, from, tileSize, y);
        float bestDist = -1f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (walls[x, z]) continue;

                var candidate = new Vector3(x * tileSize, y, z * tileSize);
                float dist = Vector3.Distance(flatFrom, candidate);
                if (dist < minSeparation) continue;
                if (dist <= bestDist) continue;

                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }
}
