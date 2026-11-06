using AfterHours;
using NUnit.Framework;

public sealed class TrafficRulesTests
{
    [Test] public void RowGapsAllowMoreManeuverTimeAtHighClosingSpeedAndInReverse()
    {
        Assert.That(TrafficRules.RowSpacing(83, 20), Is.GreaterThan(TrafficRules.RowSpacing(30, 20) + 40));
        Assert.That(TrafficRules.RowSpacing(-10, 56), Is.GreaterThan(70));
        Assert.That(TrafficRules.RowSpacing(20, 20), Is.EqualTo(24));
        Assert.That(TrafficRules.RowSpacing(83, 0), Is.InRange(85, 90));
    }
    [Test] public void FormationPaceLeavesRoomBehindARowWithoutSlowingFreeTraffic()
    {
        Assert.That(TrafficRules.StaggerSpeed(38, 10, 200, 30), Is.EqualTo(38));
        Assert.That(TrafficRules.StaggerSpeed(38, 10, 70, 30), Is.LessThan(38));
        Assert.That(TrafficRules.StaggerSpeed(38, 10, 30, 30), Is.EqualTo(10));
        Assert.That(TrafficRules.StaggerSpeed(38, 10, 5, 30), Is.Zero);
    }
    [Test] public void StormPocketsStayLocalAndRepeatWhenRevisitingEitherDirection()
    {
        Assert.That(WeatherDirector.StormTargetAt(0), Is.Zero);
        Assert.That(WeatherDirector.StormTargetAt(700), Is.Zero);
        Assert.That(WeatherDirector.StormTargetAt(1100), Is.EqualTo(1));
        Assert.That(WeatherDirector.StormTargetAt(1400), Is.Zero);
        Assert.That(WeatherDirector.StormTargetAt(1600), Is.Zero);
        for (int cycle = -4; cycle <= 4; cycle++)
            Assert.That(WeatherDirector.StormTargetAt(1100 + cycle * 2400), Is.EqualTo(1));
        for (int z = -5000; z <= 5000; z += 11)
            Assert.That(WeatherDirector.StormTargetAt(z), Is.InRange(0, 1));
    }
    [Test] public void FastTrafficIsMixedBoundedAndStillLimitedByWeather()
    {
        Assert.That(TrafficRules.FastPaceAt(400), Is.EqualTo(1));
        Assert.That(TrafficRules.FastPaceAt(900), Is.Zero);
        Assert.That(TrafficRules.FastPaceAt(400 - 1440), Is.EqualTo(1));
        float normal = TrafficRules.PaceSpeed(25, false, 1);
        float fast = TrafficRules.PaceSpeed(25, true, 1);
        Assert.That(fast, Is.GreaterThan(normal + 15));
        Assert.That(TrafficRules.PaceSpeed(25, true, 0), Is.LessThan(fast));
        Assert.That(TrafficRules.PaceSpeed(38, true, 1), Is.EqualTo(TrafficRules.MaximumTrafficSpeed));
        Assert.That(fast * TrafficRules.WeatherSpeedFactor(1, 1), Is.InRange(20, fast - 10));
    }
    [Test] public void FastRearAndReverseEncountersNeedTheirActualClosingDistance()
    {
        Assert.That(TrafficRules.SafePlayerSpawn(-150, 56, 0), Is.False);
        Assert.That(TrafficRules.SafePlayerSpawn(-250, 56, 0), Is.True);
        Assert.That(TrafficRules.SafePlayerSpawn(-220, 56, -10), Is.False);
        Assert.That(TrafficRules.SafePlayerSpawn(-300, 56, -10), Is.True);
    }
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

public sealed class CityRouteTests
{
    [Test] public void DistrictsAndLandmarksRepeatConsistentlyInBothDirections()
    {
        for (int cycle = -4; cycle <= 4; cycle++)
        for (int local = 0; local < CityRoute.CycleChunks; local++)
        {
            int chunk = cycle * CityRoute.CycleChunks + local;
            Assert.That((int)CityRoute.DistrictAt(chunk), Is.EqualTo(local / 8));
            Assert.That(CityRoute.Phase(chunk), Is.EqualTo(local % 8));
            Assert.That(CityRoute.IsTransition(chunk), Is.EqualTo(local % 8 == 7));
            Assert.That(CityRoute.LandmarkAt(chunk), Is.EqualTo(CityRoute.LandmarkAt(local)));
            Assert.That(CityRoute.BlockVariantAt(chunk), Is.EqualTo(CityRoute.BlockVariantAt(local)));
            Assert.That((int)CityRoute.NextDistrict(chunk), Is.EqualTo((local / 8 + 1) % 4));
        }
        foreach (int chunk in new[] { 2, 11, 19 })
            Assert.That(CityRoute.BlockVariantAt(chunk), Is.EqualTo(2), "Landmarks need a vacant frontage lot.");
        Assert.That(CityRoute.BlockVariantAt(6), Is.EqualTo(1));
    }

    [Test] public void BakedCityAssetsHaveNoGameplayComponentsAndKeepTheRoadClear()
    {
        var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Game/Generated/GameCatalog.asset");
        var prefabs = new System.Collections.Generic.List<UnityEngine.GameObject>();
        prefabs.AddRange(catalog.downtownBlocks); prefabs.AddRange(catalog.residentialBlocks); prefabs.AddRange(catalog.industrialBlocks);
        prefabs.Add(catalog.woodlandBlock); prefabs.AddRange(catalog.districtTransitions); prefabs.AddRange(catalog.districtEntrances); prefabs.AddRange(catalog.cityLandmarks);
        Assert.That(prefabs.Count, Is.EqualTo(22)); int occupiedWindowVertices = 0;
        foreach (var prefab in prefabs)
        {
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<UnityEngine.Collider>(true), Is.Empty, prefab.name);
            Assert.That(prefab.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true), Is.Empty, prefab.name);
            Assert.That(prefab.GetComponentsInChildren<UnityEngine.Light>(true).Length, Is.LessThanOrEqualTo(1), prefab.name);
            foreach (var filter in prefab.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
            {
                Assert.That(filter.sharedMesh, Is.Not.Null, prefab.name);
                var renderer = filter.GetComponent<UnityEngine.MeshRenderer>();
                Assert.That(renderer.sharedMaterial, Is.Not.Null, prefab.name);
                Assert.That(renderer.sharedMaterial.shader.isSupported, Is.True, prefab.name);
                if (renderer.sharedMaterial.name.StartsWith("Occupied")) occupiedWindowVertices += filter.sharedMesh.vertexCount;
                foreach (var local in filter.sharedMesh.vertices)
                {
                    var vertex = prefab.transform.InverseTransformPoint(filter.transform.TransformPoint(local));
                    Assert.That(UnityEngine.Mathf.Abs(vertex.x) < 6.1f && vertex.y > .3f && vertex.y < 6.5f, Is.False,
                        prefab.name + " must leave the driving and chase-camera corridor clear.");
                }
            }
        }
        Assert.That(occupiedWindowVertices, Is.GreaterThan(0), "The authored buildings should have some occupied windows.");
    }
}
