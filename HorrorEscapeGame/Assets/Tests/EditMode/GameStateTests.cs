// Assets/Tests/EditMode/GameStateTests.cs
using NUnit.Framework;

public class GameStateTests
{
    [Test]
    public void StartsWith3Lives()
    {
        var state = new GameState();
        Assert.AreEqual(3, state.Lives);
    }

    [Test]
    public void LoseLife_DecrementsLives()
    {
        var state = new GameState();
        bool gameOver = state.LoseLife();
        Assert.AreEqual(2, state.Lives);
        Assert.IsFalse(gameOver);
    }

    [Test]
    public void LoseAllLives_ReturnsTrue()
    {
        var state = new GameState();
        state.LoseLife();
        state.LoseLife();
        bool gameOver = state.LoseLife();
        Assert.AreEqual(0, state.Lives);
        Assert.IsTrue(gameOver);
    }

    [Test]
    public void LivesNeverGoBelowZero()
    {
        var state = new GameState();
        state.LoseLife(); state.LoseLife(); state.LoseLife();
        state.LoseLife(); // 4번째
        Assert.AreEqual(0, state.Lives);
    }

    [Test]
    public void Reset_Restores3LivesAndNoKey()
    {
        var state = new GameState();
        state.LoseLife();
        state.PickUpKey();
        state.Reset();
        Assert.AreEqual(3, state.Lives);
        Assert.IsFalse(state.HasKey);
    }

    [Test]
    public void PickUpKey_SetsHasKeyTrue()
    {
        var state = new GameState();
        state.PickUpKey();
        Assert.IsTrue(state.HasKey);
    }

    [Test]
    public void UseKey_SetsHasKeyFalse()
    {
        var state = new GameState();
        state.PickUpKey();
        state.UseKey();
        Assert.IsFalse(state.HasKey);
    }
}
