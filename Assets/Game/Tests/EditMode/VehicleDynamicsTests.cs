using AfterHours;
using NUnit.Framework;
using UnityEngine;

public sealed class VehicleDynamicsTests
{
    [Test] public void FullGasBuildsSpeedGraduallyAndTheLastHundredTakesLonger()
    {
        float speed = 0, hundred = 0, twoHundred = 0, threeHundred = 0;
        for (int i = 1; i <= 3000; i++)
        {
            speed = VehicleDynamics.Step(speed, 1, false, false, VehicleDynamics.ForwardLimit, .02f);
            if (hundred == 0 && speed * 3.6f >= 100) hundred = i * .02f;
            if (twoHundred == 0 && speed * 3.6f >= 200) twoHundred = i * .02f;
            if (threeHundred == 0 && speed >= VehicleDynamics.ForwardLimit) threeHundred = i * .02f;
        }
        Assert.That(hundred, Is.InRange(5, 8));
        Assert.That(threeHundred, Is.InRange(30, 45));
        Assert.That(threeHundred - twoHundred, Is.GreaterThan(hundred * 2));
        Assert.That(speed * 3.6f, Is.EqualTo(300).Within(.01f));
    }
    [Test] public void GasCoastingAndBrakingProduceDifferentAcceleration()
    {
        float gas = VehicleDynamics.Step(20, 1, false, false, VehicleDynamics.ForwardLimit, 1);
        float coast = VehicleDynamics.Step(20, 0, false, false, VehicleDynamics.ForwardLimit, 1);
        float brake = VehicleDynamics.Step(20, 1, true, false, VehicleDynamics.ForwardLimit, 1);
        Assert.That(gas, Is.GreaterThan(20)); Assert.That(coast, Is.InRange(18, 20));
        Assert.That(brake, Is.EqualTo(8).Within(.05f));
        Assert.That(VehicleDynamics.Step(20, 1, true, false, VehicleDynamics.ForwardLimit, 3), Is.Zero);
    }
    [Test] public void ReverseBrakesBeforeChangingDirectionAndKeepsItsOwnLimit()
    {
        Assert.That(VehicleDynamics.Step(20, 1, false, true, VehicleDynamics.ForwardLimit, 1), Is.GreaterThan(0));
        Assert.That(VehicleDynamics.Step(20, 1, false, true, VehicleDynamics.ForwardLimit, 8), Is.EqualTo(-10).Within(.01f));
        Assert.That(VehicleDynamics.Step(-8, 1, true, true, VehicleDynamics.ForwardLimit, 2), Is.Zero);
    }
    [Test] public void AccelerationIsConsistentAtDifferentFrameRates()
    {
        float slow = 0, fast = 0;
        for (int i = 0; i < 30 * 25; i++) slow = VehicleDynamics.Step(slow, 1, false, false, VehicleDynamics.ForwardLimit, 1f / 30);
        for (int i = 0; i < 120 * 25; i++) fast = VehicleDynamics.Step(fast, 1, false, false, VehicleDynamics.ForwardLimit, 1f / 120);
        Assert.That(slow, Is.EqualTo(fast).Within(.15f));
    }
    [Test] public void SixGearsMatchTheNewSpeedRange()
    {
        Assert.That(VehicleDynamics.Gear(0), Is.EqualTo(1));
        Assert.That(VehicleDynamics.Gear(120f / 3.6f), Is.EqualTo(3));
        Assert.That(VehicleDynamics.Gear(VehicleDynamics.ForwardLimit), Is.EqualTo(6));
        Assert.That(VehicleDynamics.GearProgress(VehicleDynamics.ForwardLimit), Is.EqualTo(1));
    }
    [TestCase(35, 10, false)]
    [TestCase(15, -15, true)]
    [TestCase(-10, -35, false)]
    public void HighSpeedSweepOnlyHitsWhenTheVehicleActuallyCrosses(float before, float after, bool expected)
    {
        Assert.That(VehicleDynamics.SweptContact(new Vector2(0, before), new Vector2(0, after), 1.65f, 4.1f), Is.EqualTo(expected));
    }
    [Test] public void DiagonalNearMissDoesNotCombineContactsAtDifferentTimes()
    {
        Assert.That(VehicleDynamics.SweptContact(new Vector2(0, 30), new Vector2(4, 0), 1.65f, 4.1f), Is.False);
        Assert.That(VehicleDynamics.SweptContact(new Vector2(4, 30), new Vector2(0, 0), 1.65f, 4.1f), Is.True);
    }
}
