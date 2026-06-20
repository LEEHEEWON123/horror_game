using NUnit.Framework;
using UnityEngine;

public class MonsterVisionTests
{
    [Test]
    public void PlayerDirectlyAhead_IsInFOV()
    {
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, Vector3.forward, 90f);
        Assert.IsTrue(result);
    }

    [Test]
    public void PlayerDirectlyBehind_IsNotInFOV()
    {
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, Vector3.back, 90f);
        Assert.IsFalse(result);
    }

    [Test]
    public void PlayerAt44Degrees_IsInsideFOV90()
    {
        Vector3 dir = Quaternion.Euler(0, 44, 0) * Vector3.forward;
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, dir, 90f);
        Assert.IsTrue(result);
    }

    [Test]
    public void PlayerAt46Degrees_IsOutsideFOV90()
    {
        Vector3 dir = Quaternion.Euler(0, 46, 0) * Vector3.forward;
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, dir, 90f);
        Assert.IsFalse(result);
    }

    [Test]
    public void PlayerAt89Degrees_IsInsideFOV180()
    {
        Vector3 dir = Quaternion.Euler(0, 89, 0) * Vector3.forward;
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, dir, 180f);
        Assert.IsTrue(result);
    }
}
