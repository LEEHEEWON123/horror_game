public static class RealityEscapeState
{
    private static bool _pendingFall;

    public static void MarkPending() => _pendingFall = true;

    public static bool HasPending() => _pendingFall;

    public static void Clear() => _pendingFall = false;

    public static bool ConsumePending()
    {
        if (!_pendingFall)
            return false;

        _pendingFall = false;
        return true;
    }
}
