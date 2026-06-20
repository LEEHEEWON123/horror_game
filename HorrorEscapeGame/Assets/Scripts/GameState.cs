using System;

// In-memory only — reset each map / on death. No disk save.
public class GameState
{
    public const int MaxLives = 1;

    public int Lives { get; private set; } = MaxLives;

    public bool LoseLife()
    {
        Lives = Math.Max(0, Lives - 1);
        return Lives == 0;
    }

    public void Reset()
    {
        Lives = MaxLives;
    }
}
