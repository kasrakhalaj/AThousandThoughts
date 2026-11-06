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
    [TestCase(55f, true)]
    [TestCase(-8f, false)]
    public void SelectingOppositeDirectionStopsWithoutNeedingGas(float speed, bool reverse)
    {
        float next = VehicleDynamics.StepInGear(speed, 0, false, reverse, VehicleDynamics.ForwardLimit, 5, 1, false);
        Assert.That(next, Is.Zero, "A direction request must brake to rest before W supplies gas in the selected direction.");
        float driven = VehicleDynamics.StepInGear(next, 1, false, reverse, VehicleDynamics.ForwardLimit, 2, 1, false);
        Assert.That(reverse ? driven < 0 : driven > 0, Is.True);
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

public sealed class PlayerLampGeometryTests
{
    [TestCase("Beatall", 0)]
    [TestCase("docLorean", 2)]
    [TestCase("Landyroamer", 2)]
    [TestCase("Toyoyo Highlight", 2)]
    [TestCase("Tristar Racer", 0)]
    public void PlayerLampsFollowAuthoredBodyVertices(string name, int reverseCount)
    {
        var car = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Generated/Car-" + name + ".prefab");
        var lighting = car.GetComponentInChildren<VehicleLights>();
        Assert.That(lighting.HeadlightCount, Is.EqualTo(2)); Assert.That(lighting.TailLightCount, Is.EqualTo(2));
        var source = car.transform.GetChild(0).GetComponent<MeshFilter>();
        var points = source.sharedMesh.vertices;
        var toRoot = car.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
        for (int i = 0; i < points.Length; i++) points[i] = toRoot.MultiplyPoint3x4(points[i]);
        int reverseLenses = 0;
        foreach (var filter in lighting.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = filter.sharedMesh;
            Assert.That(UnityEditor.AssetDatabase.GetAssetPath(mesh), Does.StartWith("Assets/Game/Generated/PlayerLamps/"), "Built-in cube/sphere lamps are forbidden.");
            Assert.That(mesh.vertexCount, Is.GreaterThan(0));
            if (filter.name == "Reverse light") reverseLenses++;
            foreach (var vertex in mesh.vertices)
            {
                float nearest = float.PositiveInfinity;
                foreach (var point in points) nearest = Mathf.Min(nearest, (point - vertex).sqrMagnitude);
                Assert.That(nearest, Is.LessThan(.0022f * .0022f), name + "/" + filter.name + " must match an original FBX vertex within the 2mm surface offset.");
            }
        }
        Assert.That(reverseLenses, Is.EqualTo(reverseCount), "Do not invent lenses on cars with no authored reverse bulb.");
        foreach (var beam in lighting.GetComponentsInChildren<Light>())
            if (beam.name == "Headlight beam") Assert.That(beam.transform.localPosition.z, Is.GreaterThan(1.4f));
    }
}
