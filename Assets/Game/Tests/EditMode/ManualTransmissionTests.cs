using AfterHours;
using NUnit.Framework;

public sealed class ManualTransmissionTests
{
    [Test] public void ShiftsRequireARequestAndAClutchInterval()
    {
        var t = new ManualTransmission(); t.Tick(10); Assert.That(t.Gear, Is.EqualTo(1));
        Assert.That(t.Request(1, 10, false), Is.True);
        Assert.That(t.Gear, Is.EqualTo(1)); Assert.That(t.PendingGear, Is.EqualTo(2));
        Assert.That(t.Request(1, 10, false), Is.False);
        t.Tick(.2f); Assert.That(t.Gear, Is.EqualTo(1)); t.Tick(.2f); Assert.That(t.Gear, Is.EqualTo(2));
        Assert.That(t.ShiftSequence, Is.EqualTo(1));
    }
    [Test] public void BoundsReverseAndResetAreSafe()
    {
        var t = new ManualTransmission(); Assert.That(t.Request(-1, 0, false), Is.False);
        for (int i = 0; i < 5; i++) { Assert.That(t.Request(1, 0, false), Is.True); t.Tick(1); }
        Assert.That(t.Gear, Is.EqualTo(6)); Assert.That(t.Request(1, 0, false), Is.False);
        Assert.That(t.Request(-1, -5, true), Is.False);
        t.Reset(); Assert.That(t.Gear, Is.EqualTo(1)); Assert.That(t.IsShifting, Is.False); Assert.That(t.ShiftSequence, Is.Zero);
    }
    [Test] public void DownshiftCannotOverRevOrInstantlyRemoveSpeed()
    {
        var t = new ManualTransmission(); for (int i = 0; i < 5; i++) { t.Request(1, 0, false); t.Tick(1); }
        Assert.That(t.Request(-1, 300f / 3.6f, false), Is.False); Assert.That(t.RejectionRemaining, Is.GreaterThan(0));
        Assert.That(t.Gear, Is.EqualTo(6)); Assert.That(t.Request(-1, 50, false), Is.True);
        float before = 60;
        float after = VehicleDynamics.StepInGear(before, 1, false, false, VehicleDynamics.ForwardLimit, .02f, 1, false);
        Assert.That(after, Is.GreaterThan(59), "An overspeed rolling car must coast, not snap down to first-gear speed.");
    }
    [Test] public void LowGearHasALimiterAndHighGearLugsAtLowSpeed()
    {
        float first = 0, sixth = 0;
        for (int i = 0; i < 500; i++) first = VehicleDynamics.StepInGear(first, 1, false, false, VehicleDynamics.ForwardLimit, .02f, 1, false);
        Assert.That(first * 3.6f, Is.InRange(44.5f, 45.1f));
        first = 0;
        for (int i = 0; i < 100; i++)
        {
            first = VehicleDynamics.StepInGear(first, 1, false, false, VehicleDynamics.ForwardLimit, .02f, 1, false);
            sixth = VehicleDynamics.StepInGear(sixth, 1, false, false, VehicleDynamics.ForwardLimit, .02f, 6, false);
        }
        Assert.That(first, Is.GreaterThan(sixth * 4));
        Assert.That(VehicleDynamics.StepInGear(20, 1, false, false, VehicleDynamics.ForwardLimit, .2f, 3, true), Is.LessThan(20));
    }
    [Test] public void CorrectManualShiftingCanReach300AndChangesRpm()
    {
        var t = new ManualTransmission(); float speed = 0;
        for (int i = 0; i < 3000; i++)
        {
            t.Tick(.02f);
            if (!t.IsShifting && t.Gear < 6 && VehicleDynamics.GearRpmRatio(speed, t.Gear) > .92f) t.Request(1, speed, false);
            speed = VehicleDynamics.StepInGear(speed, 1, false, false, VehicleDynamics.ForwardLimit, .02f, t.Gear, t.IsShifting);
        }
        Assert.That(speed * 3.6f, Is.InRange(299.5f, 300.01f)); Assert.That(t.Gear, Is.EqualTo(6)); Assert.That(t.ShiftSequence, Is.EqualTo(5));
        Assert.That(VehicleDynamics.GearRpmRatio(20, 3), Is.LessThan(VehicleDynamics.GearRpmRatio(20, 2)));
    }
}
