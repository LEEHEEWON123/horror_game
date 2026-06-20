using NUnit.Framework;

public class GameStateTests
{
    [Test]
    public void StartsWith1Life()
    {
        var state = new GameState();
        Assert.AreEqual(1, state.Lives);
    }

    [Test]
    public void LoseLife_IsImmediateGameOver()
    {
        var state = new GameState();
        bool gameOver = state.LoseLife();
        Assert.AreEqual(0, state.Lives);
        Assert.IsTrue(gameOver);
    }

    [Test]
    public void LivesNeverGoBelowZero()
    {
        var state = new GameState();
        state.LoseLife();
        state.LoseLife();
        Assert.AreEqual(0, state.Lives);
    }

    [Test]
    public void Reset_Restores1Life()
    {
        var state = new GameState();
        state.LoseLife();
        state.Reset();
        Assert.AreEqual(1, state.Lives);
    }
}
