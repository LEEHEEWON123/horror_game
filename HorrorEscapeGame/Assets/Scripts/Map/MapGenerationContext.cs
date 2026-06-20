public static class MapGenerationContext
{
    public static int? OverrideSeed;

    public static int ResolveSeed(int defaultSeed) => OverrideSeed ?? defaultSeed;

    public static void Clear() => OverrideSeed = null;
}
