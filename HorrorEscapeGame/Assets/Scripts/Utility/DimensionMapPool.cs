using System.Collections.Generic;
using UnityEngine;

public static class DimensionMapPool
{
    public static readonly string[] RandomMaps =
    {
        "Map_01",
        "Map_02",
        "Map_03",
        "Map_04",
        "Map_05",
        "Map_06",
        "Map_07"
    };

    public static string PickRandom(string currentScene)
    {
        if (string.IsNullOrEmpty(currentScene))
            return RandomMaps[Random.Range(0, RandomMaps.Length)];

        var candidates = new List<string>(RandomMaps.Length);
        foreach (string map in RandomMaps)
        {
            if (map != currentScene)
                candidates.Add(map);
        }

        if (candidates.Count == 0)
            return RandomMaps[Random.Range(0, RandomMaps.Length)];

        return candidates[Random.Range(0, candidates.Count)];
    }

    public static bool IsRandomMap(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        foreach (string map in RandomMaps)
        {
            if (map == sceneName)
                return true;
        }

        return false;
    }

    public static string PickReturnMap() =>
        RandomMaps[Random.Range(0, RandomMaps.Length)];

    public const string RealityScene = "Map_Reality";
}
