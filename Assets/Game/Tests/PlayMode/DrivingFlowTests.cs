using System.Collections;
using AfterHours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DrivingFlowTests
{
    Keyboard keyboard;
    bool hadBest;
    int savedBest;
    InputSettings.EditorInputBehaviorInPlayMode savedInputBehavior;
    const string KeyName = "AfterHours.HighScore.v1";
    [UnitySetUp] public IEnumerator Setup()
    {
        hadBest = PlayerPrefs.HasKey(KeyName); savedBest = PlayerPrefs.GetInt(KeyName);
        savedInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("AfterHours"); yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.editorInputBehaviorInPlayMode = savedInputBehavior;
        if (hadBest) PlayerPrefs.SetInt(KeyName, savedBest); else PlayerPrefs.DeleteKey(KeyName);
        PlayerPrefs.Save(); yield return null;
    }
    IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }
    [UnityTest] public IEnumerator SelectionIgnitionSteeringReversePausePoolingAndRestart()
    {
        var g = GameManager.Instance;
        Assert.That(g.State, Is.EqualTo(GameState.Menu)); Assert.That(g.Driver.Speed, Is.Zero);
        Assert.That(g.Catalog.cars.Length, Is.EqualTo(5));
        g.SelectCar(1); Assert.That(g.SelectedCar, Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        yield return Press(Key.Enter); Assert.That(g.State, Is.EqualTo(GameState.Starting));
        Assert.That(g.Driver.Speed, Is.Zero); yield return new WaitForSeconds(2.3f);
        Assert.That(g.State, Is.EqualTo(GameState.Playing)); yield return new WaitForSeconds(.5f);
        Assert.That(g.Driver.Speed, Is.GreaterThan(0));
        yield return Press(Key.D); yield return new WaitForSeconds(.3f); Assert.That(g.Driver.Lane, Is.EqualTo(2));
        yield return Press(Key.R); yield return new WaitForSeconds(2); Assert.That(g.Driver.Speed, Is.LessThan(0));
        Assert.That(Camera.main.transform.position.z, Is.GreaterThan(g.Driver.transform.position.z));
        yield return Press(Key.A); Assert.That(g.Driver.Lane, Is.EqualTo(2)); // Screen-left becomes world-right in reverse.
        yield return Press(Key.Escape); Assert.That(g.State, Is.EqualTo(GameState.Paused));
        Vector3 position = g.Driver.transform.position; float fuel = g.Run.Fuel;
        yield return new WaitForSecondsRealtime(.25f); Assert.That(g.Driver.transform.position, Is.EqualTo(position)); Assert.That(g.Run.Fuel, Is.EqualTo(fuel));
        yield return Press(Key.Escape); Assert.That(g.State, Is.EqualTo(GameState.Playing));
        int capacity = g.World.PooledCount;
        for (int i = 0; i < 12; i++) { g.Driver.transform.position = new Vector3(0, .08f, i * 120); g.World.Tick(0); }
        Assert.That(g.World.PooledCount, Is.EqualTo(capacity)); Assert.That(g.World.ActiveCount, Is.LessThan(capacity));
        g.Restart(); yield return new WaitForSeconds(2.3f);
        Assert.That(g.Run.Lives, Is.EqualTo(3)); Assert.That(g.Run.Fuel, Is.GreaterThan(99));
    }
    [UnityTest] public IEnumerator DamageCooldownGameOverAndPersistentBest()
    {
        var g = GameManager.Instance; g.StartRun(); yield return new WaitForSeconds(2.3f);
        g.Run.Advance(100, 0); g.HitCar(g.Driver.transform.position); g.HitCar(g.Driver.transform.position);
        Assert.That(g.Run.Lives, Is.EqualTo(2));
        for (int i = 0; i < 2; i++) { g.Driver.Tick(g.Input, 0, 3); g.HitCar(g.Driver.transform.position); }
        Assert.That(g.State, Is.EqualTo(GameState.GameOver)); Assert.That(g.Run.Lives, Is.Zero);
        Assert.That(PlayerPrefs.GetInt(KeyName), Is.GreaterThanOrEqualTo(200));
        g.MainMenu(); Assert.That(g.State, Is.EqualTo(GameState.Menu)); Assert.That(g.Run.Score, Is.Zero);
    }
    [UnityTest] public IEnumerator MusicCategoriesPlaybackAndWeatherZones()
    {
        var g = GameManager.Instance;
        Assert.That(g.Music.TrackCount, Is.GreaterThanOrEqualTo(26)); Assert.That(g.Music.Categories, Does.Contain("Pink Floyd"));
        float deadline = Time.realtimeSinceStartup + 10;
        while (!g.Music.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(g.Music.IsPlaying, Is.True, g.Music.Status);
        string original = g.Music.TrackName; g.Music.Next(1);
        yield return new WaitForSecondsRealtime(2); Assert.That(g.Music.TrackName, Is.Not.EqualTo(original));
        g.Music.Toggle(); Assert.That(g.Music.IsPlaying, Is.False); g.Music.Toggle(); Assert.That(g.Music.IsPlaying, Is.True);
        g.Driver.transform.position = new Vector3(0, 0, 1100);
        for (int i = 0; i < 50; i++) g.Weather.Tick(.1f);
        Assert.That(g.Weather.RainAmount, Is.GreaterThan(.5f)); Assert.That(g.Weather.Conditions, Is.EqualTo("RAIN / LOW FOG"));
        g.Driver.transform.position = new Vector3(0, 0, 1600); g.Weather.Tick(1);
        Assert.That(g.Weather.Conditions, Is.EqualTo("FOG PATCH"));
    }

    [UnityTest] public IEnumerator AllCarsKeepTheirLightsThroughDamageAndReverse()
    {
        var g = GameManager.Instance;
        for (int i = 0; i < g.Catalog.cars.Length; i++)
        {
            var authored = g.Catalog.cars[i].GetComponentInChildren<VehicleLights>(true);
            Assert.That(authored, Is.Not.Null, g.Catalog.carNames[i]);
            Assert.That(authored.HeadlightCount, Is.EqualTo(2)); Assert.That(authored.TailLightCount, Is.EqualTo(2));
            foreach (var lens in authored.GetComponentsInChildren<Renderer>())
                Assert.That(lens.sharedMaterial.IsKeywordEnabled("_EMISSION"), Is.True, "Lamp emission shader variant must remain enabled.");
        }
        g.StartRun(); yield return new WaitForSeconds(2.3f);
        var lamps = g.Driver.GetComponentInChildren<VehicleLights>();
        Assert.That(lamps.IsOn, Is.True);
        g.HitCar(g.Driver.transform.position); yield return null;
        var tail = lamps.transform.Find("Red tail and brake light").GetComponent<Renderer>();
        var properties = new MaterialPropertyBlock(); tail.GetPropertyBlock(properties);
        Assert.That(properties.GetColor("_EmissionColor").r, Is.GreaterThan(2), "Damage must not erase tail lamp emission.");
        yield return Press(Key.R); yield return new WaitForSeconds(1.6f);
        Assert.That(lamps.Reversing, Is.True);
        var reverse = lamps.transform.Find("Reverse light").GetComponent<Renderer>(); reverse.GetPropertyBlock(properties);
        Assert.That(properties.GetColor("_EmissionColor").b, Is.GreaterThan(1));
    }

    [UnityTest] public IEnumerator EarlyFogAndDenseRoadsideSurviveRecycling()
    {
        var g = GameManager.Instance;
        g.Driver.transform.position = new Vector3(0,0,220);
        for (int i=0;i<70;i++) g.Weather.Tick(.1f);
        Assert.That(g.Weather.FogAmount, Is.GreaterThan(.9f));
        Assert.That(RenderSettings.fogDensity, Is.GreaterThan(.018f));
        Assert.That(Shader.GetGlobalFloat("_AfterHoursFogDensity"), Is.EqualTo(RenderSettings.fogDensity).Within(.0001f));
        Assert.That(g.Catalog.shrubs.Length, Is.GreaterThan(0)); Assert.That(g.Catalog.hills.Length, Is.GreaterThan(0));
        var chunk = Object.FindFirstObjectByType<RoadChunk>(); int children = chunk.transform.childCount;
        Assert.That(children, Is.GreaterThan(150));
        chunk.Place(14); chunk.Place(-4); Assert.That(chunk.transform.childCount, Is.EqualTo(children));
        yield return null;
    }
}
