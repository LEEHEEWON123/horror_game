// Assets/Scripts/GameState.cs
using System;

public class GameState
{
    public const int MaxLives = 3;

    public int Lives { get; private set; } = MaxLives;
    public bool HasKey { get; private set; } = false;

    // Returns true if lives reached 0 (game over)
    public bool LoseLife()
    {
        Lives = Math.Max(0, Lives - 1);
        return Lives == 0;
    }

    public void Reset()
    {
        Lives = MaxLives;
        HasKey = false;
    }

    public void PickUpKey() => HasKey = true;
    public void UseKey() => HasKey = false;
}
