using AfterHours;
using NUnit.Framework;

public sealed class RunModelTests
{
    [Test] public void ReversingAndRevisitingRoadNeverFarmDistanceScore()
    {
        var run = new RunModel(); run.Advance(200, 1); int score = run.Score;
        run.Advance(-100, 1); run.Advance(150, 1);
        Assert.That(run.Score, Is.EqualTo(score));
        run.Advance(210, 1); Assert.That(run.Score, Is.EqualTo(score + 20));
    }
    [Test] public void FuelRunsOutAndRefillIsCapped()
    {
        var run = new RunModel(); run.Advance(0, 1000); Assert.That(run.Fuel, Is.Zero);
        for (int i = 0; i < 5; i++) run.Refuel(); Assert.That(run.Fuel, Is.EqualTo(100));
    }
    [Test] public void ThreeCollisionsEndLivesWithoutGoingNegative()
    {
        var run = new RunModel(); for (int i = 0; i < 5; i++) run.Hit(); Assert.That(run.Lives, Is.Zero);
    }
    [Test] public void DifficultyRisesAndHasABoundedMaximum()
    {
        var run = new RunModel(); float speed = run.CruiseSpeed;
        run.Advance(3000, 1); Assert.That(run.CruiseSpeed, Is.GreaterThan(speed));
        run.Advance(60000, 1); Assert.That(run.CruiseSpeed, Is.EqualTo(34));
    }
    [Test] public void RestartResetsAllRunState()
    {
        var run = new RunModel(); run.Advance(900, 60); run.AddCoin(); run.Hit(); run.Reset();
        Assert.That(run.Score, Is.Zero); Assert.That(run.Lives, Is.EqualTo(3)); Assert.That(run.Fuel, Is.EqualTo(100)); Assert.That(run.Difficulty, Is.Zero);
    }
}
