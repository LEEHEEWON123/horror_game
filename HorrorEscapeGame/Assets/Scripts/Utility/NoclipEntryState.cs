public static class NoclipEntryState
{
    private static bool _pendingWakeUp;
    private static bool _autoStand;

    public static void MarkPending(bool autoStand = false)
    {
        _pendingWakeUp = true;
        _autoStand = autoStand;
    }

    public static bool HasPending() => _pendingWakeUp;

    public static bool WillAutoStand => _autoStand;

    public static void Clear()
    {
        _pendingWakeUp = false;
        _autoStand = false;
    }

    public static bool ConsumePending()
    {
        if (!_pendingWakeUp)
            return false;

        _pendingWakeUp = false;
        return true;
    }

    public static bool ConsumeAutoStand()
    {
        bool value = _autoStand;
        _autoStand = false;
        return value;
    }
}
