public static class DimensionWakeState
{
    public static bool IsActive { get; private set; }

    public static void SetActive(bool active) => IsActive = active;
}
