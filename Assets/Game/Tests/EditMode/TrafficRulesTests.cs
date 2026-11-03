using AfterHours;
using NUnit.Framework;

public sealed class TrafficRulesTests
{
    [Test] public void FastClosingTrafficNeedsMoreSpaceAheadAndBehind()
    {
        Assert.That(TrafficRules.SafeLaneGap(45, 20, 20, 1.4f), Is.True);
        Assert.That(TrafficRules.SafeLaneGap(45, 30, 10, 1.4f), Is.False);
        Assert.That(TrafficRules.SafeLaneGap(-45, 20, 20, 1.4f), Is.True);
        Assert.That(TrafficRules.SafeLaneGap(-45, 15, 35, 1.4f), Is.False);
    }
    [Test] public void ReverseDrivingUsesCombinedClosingSpeedForSpawnSafety()
    {
        Assert.That(TrafficRules.SafePlayerSpawn(-100, 25, 20), Is.True);
        Assert.That(TrafficRules.SafePlayerSpawn(-100, 25, -10), Is.False);
        Assert.That(TrafficRules.SafePlayerSpawn(-180, 25, -10), Is.True);
        Assert.That(TrafficRules.SafePlayerSpawn(180, 0, 44), Is.False);
    }
    [Test] public void FollowingRespondsToStoppingDistanceAndWeatherHeadway()
    {
        Assert.That(TrafficRules.FollowingSpeed(30, 25, 0, 5, 1.4f), Is.Zero);
        float clear = TrafficRules.FollowingSpeed(30, 25, 15, 55, 1.4f);
        float wet = TrafficRules.FollowingSpeed(30, 25, 15, 55, 2.3f);
        Assert.That(wet, Is.LessThan(clear));
        Assert.That(TrafficRules.FollowingSpeed(30, 25, -10, 20, 1.4f), Is.LessThan(5));
        Assert.That(TrafficRules.FollowingSpeed(30, 25, 15, 250, 1.4f), Is.EqualTo(30));
    }
    [Test] public void RainAndFogReduceSpeedWithoutStoppingAllTraffic()
    {
        Assert.That(TrafficRules.WeatherSpeedFactor(0, 0), Is.EqualTo(1));
        Assert.That(TrafficRules.WeatherSpeedFactor(1, 1), Is.InRange(.55f, .65f));
        Assert.That(TrafficRules.WeatherSpeedFactor(1, 0), Is.LessThan(1));
        Assert.That(TrafficRules.WeatherSpeedFactor(0, 1), Is.LessThan(1));
    }
    [Test] public void EveryRouteCycleContainsQuietAndBusySectionsInBothDirections()
    {
        for (int cycle = -5; cycle <= 5; cycle++)
        {
            int quiet = 0, busy = 0, sparse = 0;
            for (int section = 0; section < 24; section++)
            {
                TrafficFlow flow = TrafficRules.FlowAt((cycle * 24 + section) * 60 + 30);
                if (flow == TrafficFlow.Quiet) quiet++;
                if (flow == TrafficFlow.Busy) busy++;
                if (flow == TrafficFlow.Sparse) sparse++;
            }
            Assert.That(quiet, Is.EqualTo(3)); Assert.That(busy, Is.EqualTo(6)); Assert.That(sparse, Is.EqualTo(3));
        }
    }
    [Test] public void OnlyFullyOpaqueFogCanHideANewCar()
    {
        Assert.That(TrafficRules.HiddenByFog(180, .0025f), Is.False);
        Assert.That(TrafficRules.HiddenByFog(100, .021f), Is.False);
        Assert.That(TrafficRules.HiddenByFog(200, .021f), Is.True);
    }
}
