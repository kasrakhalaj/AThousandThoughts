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
    InputSettings.BackgroundBehavior savedBackgroundBehavior;
    const string KeyName = "AfterHours.HighScore.v1";
    [UnitySetUp] public IEnumerator Setup()
    {
        hadBest = PlayerPrefs.HasKey(KeyName); savedBest = PlayerPrefs.GetInt(KeyName);
        savedInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        savedBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        // The Editor may be unfocused while Pipeline runs tests; routing alone still permits device disabling.
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>();
        Assert.That(keyboard.enabled, Is.True, "The synthetic keyboard must stay enabled during background tests.");
        yield return SceneManager.LoadSceneAsync("AfterHours"); yield return new WaitForSecondsRealtime(.06f);
        // Physical keyboards must not compete with the synthetic device during automated checks.
        GameManager.Instance.Input.StartCar.actionMap.asset.devices = new InputDevice[] { keyboard };
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.editorInputBehaviorInPlayMode = savedInputBehavior;
        InputSystem.settings.backgroundBehavior = savedBackgroundBehavior;
        // Reset the test run before restoring preferences, so OnApplicationQuit cannot save it again.
        if (GameManager.Instance != null) GameManager.Instance.MainMenu();
        if (hadBest) PlayerPrefs.SetInt(KeyName, savedBest); else PlayerPrefs.DeleteKey(KeyName);
        PlayerPrefs.Save(); yield return null;
    }
    IEnumerator Press(Key key)
    {
        // Keep each state across several player-loop updates, as a physical keypress would be.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return new WaitForSecondsRealtime(.08f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(.08f);
    }
    IEnumerator Hold(Key key, float seconds)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return new WaitForSecondsRealtime(seconds);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(.08f);
    }
    IEnumerator DriveFor(float seconds)
    {
        float until = Time.realtimeSinceStartup + seconds;
        bool releaseShift = false;
        while (Time.realtimeSinceStartup < until)
        {
            var car = GameManager.Instance.Driver;
            bool up = !releaseShift && !car.IsShifting && car.Gear < 6 && car.RpmRatio > .92f;
            InputSystem.QueueStateEvent(keyboard, up ? new KeyboardState(Key.W, Key.E) : new KeyboardState(Key.W));
            releaseShift = up; yield return null;
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(.08f);
    }
    IEnumerator ShiftWithGas(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, key)); yield return new WaitForSecondsRealtime(.08f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return new WaitForSecondsRealtime(.08f);
    }
    IEnumerator WaitForDriving()
    {
        float deadline = Time.realtimeSinceStartup + 7;
        while (GameManager.Instance.State == GameState.Starting && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Playing));
    }
    [UnityTest] public IEnumerator MenuCameraCutsBetweenAnglesAndReturnsToDrivingView()
    {
        var game = GameManager.Instance;
        var camera = Camera.main;
        Vector3 player = game.Driver.transform.position;
        Assert.That(camera.transform.position.z - player.z, Is.GreaterThan(4));
        yield return new WaitForSecondsRealtime(2.75f);
        Assert.That(camera.transform.position.x - player.x, Is.LessThan(-5));
        Assert.That(Mathf.Abs(camera.transform.position.z - player.z), Is.LessThan(1));
        yield return new WaitForSecondsRealtime(2.6f);
        Assert.That(camera.transform.position.z - player.z, Is.LessThan(-5));

        Assert.That(game.Driver.transform.position, Is.EqualTo(player));
        Vector3 framed = camera.WorldToViewportPoint(player + Vector3.up * .8f);
        Assert.That(framed.x, Is.InRange(.53f, .8f));
        Assert.That(framed.y, Is.InRange(.25f, .6f));

        game.SelectCar(1);
        yield return null;
        yield return null;
        Assert.That(camera.transform.position.z - player.z, Is.GreaterThan(4));
        game.StartRun();
        yield return WaitForDriving();
        Assert.That(Mathf.Abs(camera.transform.position.x - player.x), Is.LessThan(.1f));
        Assert.That(camera.transform.position.z, Is.LessThan(player.z - 9));
    }

    [UnityTest] public IEnumerator PerformanceVoicesFollowCarSelectionAndReturnToTheDefault()
    {
        var g = GameManager.Instance;
        Assert.That(g.Audio.ActiveLoadClip.name, Is.EqualTo("Tristar-load"));
        g.SelectCar(1 - g.SelectedCar); yield return null;
        Assert.That(g.Audio.ActiveLoadClip.name, Is.EqualTo("docLorean-load"));
        g.SelectCar(-g.SelectedCar); yield return null;
        Assert.That(g.Audio.ActiveLoadClip, Is.EqualTo(g.Catalog.engineLoadSound));
        g.SelectCar(4); yield return null; g.StartRun(); yield return WaitForDriving();
        yield return Hold(Key.W, 2);
        Assert.That(g.Audio.EnginePlaying, Is.True);
        Assert.That(g.Audio.EngineVolume, Is.GreaterThan(.05f));
        Assert.That(g.Audio.ThrottleCueCount, Is.GreaterThan(0));
        Assert.That(g.Audio.ActiveLoadClip.name, Is.EqualTo("Tristar-load"));
    }
    [UnityTest] public IEnumerator ReverseFromHighSpeedCrossesZeroWithHeldGas()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving();
        g.enabled = false; // Isolate pedals/camera from traffic contacts while reproducing the transition.
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            for (int i = 0; i < 2250; i++)
            {
                if (!g.Driver.IsShifting && g.Driver.Gear < 6 && g.Driver.RpmRatio > .92f) g.Driver.RequestShift(1);
                g.Driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            }
            Assert.That(g.Driver.Speed, Is.GreaterThan(80));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.R)); yield return null;
            g.Driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            Assert.That(g.Driver.Reversing, Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            for (int i = 0; i < 500; i++) g.Driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            Assert.That(g.Driver.Speed, Is.LessThan(-5), "Held W must accelerate backward after braking through zero.");
            Assert.That(g.Driver.RearView, Is.True);
            Camera.main.transform.position = g.Driver.transform.position + new Vector3(0, 4.8f, -10.5f);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(Camera.main.transform.position.z, Is.GreaterThan(g.Driver.transform.position.z));
        }
        finally { g.enabled = true; }
    }
    [UnityTest] public IEnumerator SelectionIgnitionSteeringReversePausePoolingAndRestart()
    {
        var g = GameManager.Instance;
        Assert.That(g.State, Is.EqualTo(GameState.Menu)); Assert.That(g.Driver.Speed, Is.Zero);
        Assert.That(g.Catalog.cars.Length, Is.EqualTo(5));
        Assert.That(g.Catalog.carNames[g.SelectedCar], Is.EqualTo("TRISTAR RACER"));
        int selected = g.SelectedCar; g.SelectCar(1); Assert.That(g.SelectedCar, Is.EqualTo((selected + 1) % g.Catalog.cars.Length));
        Assert.That(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        yield return Press(Key.Enter); Assert.That(g.State, Is.EqualTo(GameState.Starting));
        Assert.That(g.Driver.Speed, Is.Zero); yield return WaitForDriving();
        Assert.That(g.State, Is.EqualTo(GameState.Playing)); yield return new WaitForSeconds(.5f);
        Assert.That(g.Driver.Speed, Is.Zero, "Ignition must not drive the car automatically.");
        yield return Hold(Key.W, 2.5f); Assert.That(g.Driver.Speed, Is.GreaterThan(10));
        float poweredSpeed = g.Driver.Speed; yield return new WaitForSeconds(.4f);
        Assert.That(g.Driver.Speed, Is.LessThan(poweredSpeed)); Assert.That(g.Driver.Speed, Is.GreaterThan(0));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
        yield return new WaitForSeconds(.35f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
        Assert.That(g.Driver.Speed, Is.LessThan(poweredSpeed - 3));
        var hud = Object.FindAnyObjectByType<GameHud>();
        Assert.That(hud.DisplayedSpeedKph, Is.EqualTo(Mathf.RoundToInt(Mathf.Abs(g.Driver.Speed) * 3.6f)).Within(4));
        yield return Press(Key.D); yield return new WaitForSeconds(1.7f); Assert.That(g.Driver.Lane, Is.EqualTo(2));
        yield return Press(Key.R); yield return Hold(Key.W, 2); Assert.That(g.Driver.Speed, Is.LessThan(0));
        Assert.That(Camera.main.transform.position.z, Is.GreaterThan(g.Driver.transform.position.z));
        float beforeReverseTurn = g.Driver.transform.position.x;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
        yield return new WaitForSeconds(.12f);
        Assert.That(g.Driver.Lane, Is.EqualTo(1)); // Screen-right becomes world-left in reverse.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        yield return new WaitForSeconds(1.8f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
        Assert.That(g.Driver.transform.position.x, Is.LessThan(beforeReverseTurn - .6f), "The reverse maneuver must move toward the requested lane.");
        yield return Press(Key.A); Assert.That(g.Driver.Lane, Is.EqualTo(2));
        yield return Press(Key.Escape); Assert.That(g.State, Is.EqualTo(GameState.Paused));
        Vector3 position = g.Driver.transform.position; float fuel = g.Run.Fuel;
        yield return new WaitForSecondsRealtime(.25f); Assert.That(g.Driver.transform.position, Is.EqualTo(position)); Assert.That(g.Run.Fuel, Is.EqualTo(fuel));
        yield return Press(Key.Escape); Assert.That(g.State, Is.EqualTo(GameState.Playing));
        int capacity = g.World.PooledCount;
        for (int i = 0; i < 12; i++) { g.Driver.transform.position = new Vector3(0, .08f, i * 120); g.World.Tick(0); }
        Assert.That(g.World.PooledCount, Is.EqualTo(capacity)); Assert.That(g.World.ActiveCount, Is.LessThan(capacity));
        g.Restart(); yield return WaitForDriving();
        Assert.That(g.Run.Lives, Is.EqualTo(3)); Assert.That(g.Run.Fuel, Is.GreaterThan(99));
    }
    [UnityTest] public IEnumerator SteeringRequiresRollingAndArcsIntoLaneAtHighSpeed()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving();
        var driver = g.Driver;
        var body = driver.transform.Find("Visual");
        g.enabled = false; // Step the real car and input actions without traffic or frame-rate variance.
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); yield return null;
            driver.Tick(g.Input, g.Run.TopSpeed, .3f);
            Assert.That(driver.Lane, Is.EqualTo(2));
            Assert.That(driver.transform.position.x, Is.Zero, "A stopped car must not slide sideways.");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            for (int i = 0; i < 20; i++) driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            Assert.That(driver.transform.position.x, Is.InRange(.0005f, .025f), "A queued turn should begin gently as the car rolls.");

            driver.Reset();
            for (int i = 0; i < 2250; i++)
            {
                if (!driver.IsShifting && driver.Gear < 6 && driver.RpmRatio > .92f) driver.RequestShift(1);
                driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            }
            Assert.That(driver.Speed, Is.GreaterThan(80));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D)); yield return null;
            driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            float firstStep = driver.transform.position.x;
            Assert.That(firstStep, Is.InRange(.003f, .03f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            Assert.That(driver.transform.position.x - firstStep, Is.GreaterThan(firstStep), "Lateral movement should build after turn-in.");
            Assert.That(body.localRotation.eulerAngles.y, Is.InRange(1f, 5f));
            for (int i = 0; i < 10; i++) driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            Assert.That(driver.transform.position.x, Is.GreaterThan(1.8f), "At highway speed the car must reach the next lane within the dodge window.");
            for (int i = 0; i < 26; i++) driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            Assert.That(driver.transform.position.x, Is.EqualTo(CarDriver.LaneWidth).Within(.01f));
            Assert.That(Quaternion.Angle(body.localRotation, Quaternion.identity), Is.LessThan(.3f));

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.A)); yield return null;
            driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            for (int i = 0; i < 5; i++) driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            float beforeCorrection = driver.transform.position.x;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D)); yield return null;
            driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            Assert.That(driver.transform.position.x, Is.LessThan(beforeCorrection), "A correction should unwind the leftward motion smoothly.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            for (int i = 0; i < 42; i++) driver.Tick(g.Input, g.Run.TopSpeed, .02f);
            Assert.That(driver.transform.position.x, Is.EqualTo(CarDriver.LaneWidth).Within(.01f));
        }
        finally { g.enabled = true; }
    }
    [UnityTest] public IEnumerator DamageCooldownGameOverAndPersistentBest()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving();
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
        Assert.That(g.Music.Categories[0], Is.EqualTo("Singles"));
        Assert.That(g.Music.Categories[1], Is.EqualTo("All Music"));
        if (g.Music.TrackCount == 0)
        {
            Assert.That(g.Music.Status, Is.EqualTo("Add MP3 files to the Music folder"));
            Assert.That(g.Music.IsPlaying, Is.False);
        }
        else
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (!g.Music.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(g.Music.IsPlaying, Is.True, g.Music.Status);
            if (g.Music.TrackCount > 1)
            {
                string title = g.Music.TrackName, artist = g.Music.Artist;
                g.Music.Next(1);
                deadline = Time.realtimeSinceStartup + 10;
                while (!g.Music.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(g.Music.IsPlaying, Is.True, g.Music.Status);
                Assert.That(g.Music.TrackName != title || g.Music.Artist != artist, Is.True);
            }
            g.Music.Toggle(); Assert.That(g.Music.IsPlaying, Is.False);
            g.Music.Toggle(); Assert.That(g.Music.IsPlaying, Is.True);
            if (g.Music.Category == "Singles") g.Music.ChangeCategory(1);
            Assert.That(g.Music.Category, Is.EqualTo("All Music"));
            Assert.That(g.Music.TrackCount, Is.GreaterThan(0));
        }
        g.Driver.transform.position = new Vector3(0, 0, 1100);
        for (int i = 0; i < 50; i++) g.Weather.Tick(.1f);
        Assert.That(g.Weather.RainAmount, Is.GreaterThan(.5f)); Assert.That(g.Weather.Conditions, Is.EqualTo("HEAVY RAIN / DENSE FOG"));
        g.Driver.transform.position = new Vector3(0, 0, 1600);
        for (int i = 0; i < 100; i++) g.Weather.Tick(.1f);
        Assert.That(g.Weather.Conditions, Is.EqualTo("FOG PATCH"));
    }

    [UnityTest] public IEnumerator StormStrengthAudioPauseAndRestartAreConsistent()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving();
        g.Driver.transform.position = new Vector3(0, .08f, 1100);
        for (int i = 0; i < 100; i++) g.Weather.Tick(.1f);
        g.Audio.Tick();
        Assert.That(g.Weather.StormAmount, Is.GreaterThan(.98f));
        Assert.That(RenderSettings.fogDensity, Is.GreaterThan(.029f));
        Assert.That(g.Weather.RainEmissionRate, Is.GreaterThan(3300));
        Assert.That(g.Audio.RainVolume, Is.GreaterThan(.28f));
        Assert.That(Shader.GetGlobalFloat("_AfterHoursCloudCover"), Is.GreaterThan(.98f));
        var thunder = System.Array.Find(g.Audio.GetComponents<AudioSource>(),
            source => System.Array.IndexOf(g.Catalog.thunderSounds, source.clip) >= 0);
        Assert.That(thunder, Is.Not.Null);
        Assert.That(thunder.isPlaying, Is.True);
        Assert.That(Shader.GetGlobalFloat("_AfterHoursFogDensity"), Is.EqualTo(RenderSettings.fogDensity).Within(.00001f));
        g.TogglePause(); float rain = g.Weather.RainAmount, storm = g.Weather.StormAmount;
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That(g.Weather.RainAmount, Is.EqualTo(rain)); Assert.That(g.Weather.StormAmount, Is.EqualTo(storm));
        Assert.That(thunder.isPlaying, Is.False);
        g.MainMenu();
        g.Audio.Tick();
        Assert.That(thunder.isPlaying, Is.False);
        Assert.That(Shader.GetGlobalFloat("_AfterHoursCloudCover"), Is.Zero);
        Assert.That(g.Weather.RainAmount, Is.Zero); Assert.That(g.Weather.StormAmount, Is.Zero);
        Assert.That(RenderSettings.fogDensity, Is.EqualTo(WeatherDirector.ClearFogDensity));
        g.StartRun(); yield return WaitForDriving();
        Assert.That(g.Weather.StormAmount, Is.Zero); Assert.That(g.Weather.RainEmissionRate, Is.Zero);
    }
    [UnityTest] public IEnumerator FastDriverAcceleratesFollowsSlowsForStormAndResetsInPool()
    {
        var g = GameManager.Instance;
        MovingCar car = null;
        foreach (var t in g.World.Traffic) if (t is MovingCar moving) { car = moving; break; }
        Assert.That(car, Is.Not.Null);
        // Move peers away to isolate free cruising, then introduce the player as a real lead vehicle.
        foreach (var t in g.World.Traffic) if (t != car) t.transform.position = new Vector3(3.6f, .08f, -500);
        car.transform.position = new Vector3(-3.6f, .08f, 150); car.SetCruisingSpeed(25, true);
        float entering = car.Speed;
        car.transform.position = new Vector3(-3.6f, .08f, 400);
        float before = car.Speed; car.Tick(.2f);
        Assert.That(car.IsFastDriver, Is.True); Assert.That(car.DesiredSpeed, Is.GreaterThan(50));
        Assert.That(car.Speed, Is.GreaterThan(entering)); Assert.That(car.Speed - before, Is.LessThanOrEqualTo(.71f));
        g.Driver.transform.position = new Vector3(-3.6f, .08f, car.transform.position.z + 18);
        float following = car.Speed; car.Tick(.2f); Assert.That(car.Speed, Is.LessThan(following));
        g.Driver.transform.position = new Vector3(0, .08f, 1100);
        for (int i = 0; i < 100; i++) g.Weather.Tick(.1f);
        Assert.That(car.DesiredSpeed, Is.LessThan(32));
        car.Activate(g, new Vector3(-3.6f, .08f, 100));
        Assert.That(car.IsFastDriver, Is.False, "Pooled activation must not inherit a previous fast-driver assignment.");
        yield return null;
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
        // The docLorean has authored reverse lenses; the classic Beatall has no separate reverse bulbs.
        g.SelectCar(1 - g.SelectedCar); g.StartRun(); yield return WaitForDriving();
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

    [UnityTest] public IEnumerator DistrictSceneryRecyclesWithoutGrowthAndRevisitsTheSameLayout()
    {
        var g = GameManager.Instance;
        var chunk = Object.FindFirstObjectByType<RoadChunk>();
        int descendants = chunk.GetComponentsInChildren<Transform>(true).Length;
        int pool = g.World.PooledCount;
        chunk.Place(2);
        var first = chunk.GetComponentsInChildren<Transform>(true);
        var positions = new Vector3[first.Length]; var active = new bool[first.Length];
        for (int i = 0; i < first.Length; i++) { positions[i] = first[i].localPosition; active[i] = first[i].gameObject.activeSelf; }
        for (int index = -40; index <= 40; index++)
        {
            chunk.Place(index);
            Assert.That(chunk.District, Is.EqualTo(CityRoute.DistrictAt(index)));
            int blocks = 0, features = 0;
            foreach (Transform child in chunk.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                if (child.name.StartsWith("Block-") || child.name.StartsWith("Transition-")) blocks++;
                if (child.name.StartsWith("Landmark-")) features++;
            }
            Assert.That(blocks, Is.EqualTo(1), "Exactly one district or transition should be visible.");
            Assert.That(features, Is.EqualTo(CityRoute.LandmarkAt(index) >= 0 ? 1 : 0));
        }
        chunk.Place(2);
        Assert.That(chunk.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(descendants));
        for (int i = 0; i < first.Length; i++)
        {
            Assert.That(first[i].localPosition, Is.EqualTo(positions[i]));
            Assert.That(first[i].gameObject.activeSelf, Is.EqualTo(active[i]));
        }
        Assert.That(g.World.PooledCount, Is.EqualTo(pool));
        yield return null;
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

    [UnityTest] public IEnumerator RecordedEngineTrafficMovementAndPause()
    {
        var g = GameManager.Instance;
        Assert.That(g.Catalog.movingTrafficVariants.Length, Is.EqualTo(7));
        Assert.That(g.Catalog.engineSound.name, Is.EqualTo("Recorded-player-idle"));
        Assert.That(g.Catalog.trafficEngineSound.name, Is.EqualTo("Recorded-traffic-engine"));
        Assert.That(g.Catalog.rainSound.name, Is.EqualTo("Weather-street-rain"));
        Assert.That(g.Catalog.heavyRainSound.name, Is.EqualTo("Weather-heavy-rain"));
        g.StartRun(); yield return WaitForDriving(); yield return new WaitForSeconds(.4f);
        Assert.That(g.Driver.Speed, Is.Zero);
        Assert.That(g.Audio.EnginePlaying, Is.True); Assert.That(g.Audio.EngineVolume, Is.GreaterThan(.1f));
        MovingCar car = null;
        foreach (var candidate in Object.FindObjectsByType<MovingCar>())
            if (candidate.Speed > 10 && candidate.transform.position.z > 85 && candidate.transform.position.z < 500) { car = candidate; break; }
        Assert.That(car, Is.Not.Null);
        float start = car.transform.position.z; yield return new WaitForSeconds(.7f);
        Assert.That(car.transform.position.z - start, Is.GreaterThan(6));
        Assert.That(car.EnginePlaying, Is.True); Assert.That(car.EngineVolume, Is.LessThan(g.Audio.EngineVolume));
        g.TogglePause(); yield return new WaitForSecondsRealtime(.15f);
        Assert.That(car.EnginePlaying, Is.False); float parked = car.transform.position.z;
        yield return new WaitForSecondsRealtime(.15f); Assert.That(car.transform.position.z, Is.EqualTo(parked));
        g.TogglePause(); yield return new WaitForSeconds(.15f); Assert.That(car.EnginePlaying, Is.True);
    }

    [UnityTest] public IEnumerator GasHeldDuringIgnitionWorksWhenDrivingBecomesEnabled()
    {
        var g = GameManager.Instance; g.StartRun();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        yield return new WaitForSeconds(.3f); Assert.That(g.Driver.Speed, Is.Zero);
        yield return WaitForDriving(); yield return new WaitForSeconds(.8f);
        Assert.That(g.Driver.Speed, Is.GreaterThan(3), "A held gas pedal must be read when the driving map is enabled.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.S)); yield return new WaitForSeconds(1.1f);
        Assert.That(g.Driver.Speed, Is.Zero, "The brake must win when both pedals are held.");
        Assert.That(g.Driver.Reversing, Is.False, "Braking must not select reverse gear.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }

    void MoveTrafficAway(GameManager g)
    {
        int i = 0;
        foreach (var car in g.World.Traffic) car.transform.position = new Vector3(0, .08f, 3000 + i++ * 100);
    }
    MovingCar[] TrafficForScenario(GameManager g)
    {
        MoveTrafficAway(g);
        var cars = new MovingCar[3];
        for (int i = 0; i < cars.Length; i++)
        {
            cars[i] = g.World.TrySpawnTraffic(new Vector3((i - 1) * CarDriver.LaneWidth, .08f, -100 - i * 8), 20) as MovingCar;
            Assert.That(cars[i], Is.Not.Null, "A hidden, spaced traffic scenario should be accepted.");
            cars[i].transform.position = new Vector3(0, .08f, 4000 + i * 100);
        }
        return cars;
    }
    [UnityTest] public IEnumerator ThreeLaneConvergenceKeepsAStaggeredGap()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving(); g.TogglePause();
        var cars = TrafficForScenario(g);
        cars[0].transform.position = new Vector3(-3.6f, .08f, 120); cars[0].SetCruisingSpeed(38);
        cars[1].transform.position = new Vector3(0, .08f, 190); cars[1].SetCruisingSpeed(10);
        cars[2].transform.position = new Vector3(3.6f, .08f, 190); cars[2].SetCruisingSpeed(10);
        for (int i = 0; i < 40; i++)
        {
            foreach (var car in cars) car.Tick(.05f);
            float rowZ = Mathf.Min(cars[1].transform.position.z, cars[2].transform.position.z);
            Assert.That(rowZ - cars[0].transform.position.z, Is.GreaterThan(18),
                "A faster third car must not catch the two-car row and close all three lanes.");
        }
        Assert.That(cars[0].Speed, Is.GreaterThan(0), "Yield by changing pace, not by removing or freezing traffic.");
        yield return null;
    }
    [UnityTest] public IEnumerator ThreeLaneForecastAlsoCatchesConvergingPairs()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving(); g.TogglePause();
        var cars = TrafficForScenario(g);
        cars[0].transform.position = new Vector3(-3.6f, .08f, 60); cars[0].SetCruisingSpeed(38, true);
        cars[1].transform.position = new Vector3(0, .08f, 100); cars[1].SetCruisingSpeed(38);
        cars[2].transform.position = new Vector3(3.6f, .08f, 190); cars[2].SetCruisingSpeed(10);
        for (int i = 0; i < 70; i++)
        {
            foreach (var car in cars) car.Tick(.05f);
            float low = float.PositiveInfinity, high = float.NegativeInfinity; int lanes = 0;
            foreach (var car in cars)
            {
                low = Mathf.Min(low, car.transform.position.z); high = Mathf.Max(high, car.transform.position.z);
                for (int lane = 0; lane < 3; lane++) if (car.OccupiesLane(lane)) lanes |= 1 << lane;
            }
            Assert.That(lanes == 7 && high - low < 18, Is.False,
                "Cars at three different speeds must not converge into a full-width row.");
            Assert.That(cars[2].Speed, Is.GreaterThan(9.5f), "The leading car must keep moving while the trailing car yields.");
        }
        yield return null;
    }
    [UnityTest] public IEnumerator ThreeLaneFutureAlignmentIsRejectedBeforeSpawning()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving(); g.TogglePause();
        var cars = TrafficForScenario(g);
        cars[0].transform.position = new Vector3(-3.6f, .08f, -180); cars[0].SetCruisingSpeed(10);
        cars[1].transform.position = new Vector3(0, .08f, -180); cars[1].SetCruisingSpeed(10);
        Assert.That(g.World.TrySpawnTraffic(new Vector3(3.6f, .08f, -240), 38), Is.Null,
            "An empty tile now is unsafe if its faster car will immediately complete a three-lane wall.");
        yield return null;
    }
    [UnityTest] public IEnumerator TrafficReservesMergesAndCancelsForPlayer()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving(); g.TogglePause();
        var cars = TrafficForScenario(g); var passer = cars[0]; var leader = cars[1]; var rival = cars[2];
        passer.transform.position = new Vector3(-3.6f, .08f, 200); passer.SetCruisingSpeed(26);
        leader.transform.position = new Vector3(-3.6f, .08f, 222); leader.SetCruisingSpeed(10);
        Assert.That(g.World.CanMerge(passer, 0), Is.True, "A leader in the source lane must not block an empty target lane.");
        for (int i = 0; i < 50 && !passer.IsMerging; i++) passer.Tick(.1f);
        Assert.That(passer.Behavior, Is.EqualTo(TrafficBehavior.Signaling)); Assert.That(passer.ReservedLane, Is.EqualTo(1));
        leader.transform.position = new Vector3(-3.6f, .08f, 3000);
        rival.transform.position = new Vector3(3.6f, .08f, passer.transform.position.z + 15); rival.SetCruisingSpeed(30);
        Assert.That(g.World.CanMerge(rival, 0), Is.False, "Two nearby drivers must not reserve the same gap.");
        g.Driver.transform.position = new Vector3(0, .08f, passer.transform.position.z + 10);
        passer.Tick(.1f);
        Assert.That(passer.IsMerging, Is.False, "A player entering the gap during the signal must cancel the maneuver.");
        Assert.That(passer.transform.position.x, Is.EqualTo(-3.6f).Within(.01f));
        passer.Activate(g, new Vector3(-3.6f, .08f, 200));
        Assert.That(passer.IsMerging, Is.False); Assert.That(passer.Behavior, Is.EqualTo(TrafficBehavior.Cruising));
        g.Driver.transform.position = new Vector3(0, .08f, 0);
        rival.transform.position = new Vector3(3.6f, .08f, 4000);
        leader.transform.position = new Vector3(-3.6f, .08f, 240); leader.SetCruisingSpeed(10);
        passer.SetCruisingSpeed(26);
        bool completed = false;
        for (int i = 0; i < 220; i++)
        {
            leader.transform.position += Vector3.forward * leader.Speed * .05f; passer.Tick(.05f);
            if (passer.Behavior == TrafficBehavior.Recovering && Mathf.Abs(passer.transform.position.x) < .01f) { completed = true; break; }
        }
        Assert.That(completed, Is.True, "A safe overtake must finish smoothly in the adjacent lane.");
        yield return null;
    }
    [UnityTest] public IEnumerator TrafficSpawnRejectsVisibleOccupiedAndBlockedRoad()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving(); g.TogglePause(); MoveTrafficAway(g);
        Assert.That(g.World.CanSpawnTraffic(new Vector3(0, .08f, 20), 20), Is.False);
        Assert.That(g.World.TrafficVisible(new Vector3(0, .08f, 200)), Is.True);
        Assert.That(g.World.TrySpawnTraffic(new Vector3(0, .08f, 200), 20), Is.Null, "No visible pop-in.");
        Vector3 hidden = new Vector3(-3.6f, .08f, -100);
        Assert.That(g.World.TrySpawnTraffic(hidden, 20), Is.Not.Null);
        Assert.That(g.World.TrySpawnTraffic(hidden, 20), Is.Null, "No overlapping spawns.");
        Assert.That(g.World.TrySpawnTraffic(new Vector3(0, .08f, -100), 20), Is.Not.Null);
        Assert.That(g.World.TrySpawnTraffic(new Vector3(3.6f, .08f, -100), 20), Is.Null, "An escape lane must remain open.");
        int capacity = g.World.PooledCount;
        g.TogglePause(); yield return Press(Key.R); yield return new WaitForSeconds(1.4f); g.TogglePause();
        MoveTrafficAway(g); g.World.Tick(0);
        Assert.That(g.World.TrafficVisible(new Vector3(0, .08f, -200)), Is.True);
        Assert.That(g.World.TrySpawnTraffic(new Vector3(0, .08f, -200), 25), Is.Null, "Reverse camera must also reject visible spawns.");
        Assert.That(g.World.TrySpawnTraffic(new Vector3(0, .08f, -640), 25), Is.Not.Null);
        Assert.That(g.World.PooledCount, Is.EqualTo(capacity));
        yield return null;
    }
    [UnityTest] public IEnumerator QuietRoadDrainsHiddenCarsAndKeepsVisibleCars()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving(); g.TogglePause();
        var cars = TrafficForScenario(g);
        g.Driver.transform.position = new Vector3(0, .08f, 1000); g.World.Tick(0);
        Assert.That(g.World.Flow, Is.EqualTo(TrafficFlow.Quiet));
        var camera = Camera.main; camera.transform.position = g.Driver.transform.position + new Vector3(0, 4.8f, -10.5f);
        camera.transform.LookAt(g.Driver.transform.position + new Vector3(0, .8f, 9));
        // These cars may have been returned by the window change; start with a fresh run at this location.
        g.World.ResetWorld();
        Assert.That(g.World.ActiveTrafficCount, Is.Zero);
        Assert.That(g.World.TrySpawnTraffic(new Vector3(0, .08f, 1500), 20), Is.Null, "Quiet roads do not replenish traffic.");
        // Return to a populated section, then carry existing cars into quiet road without respawning them.
        g.Driver.transform.position = new Vector3(0, .08f, 0); g.World.ResetWorld(); cars = TrafficForScenario(g);
        g.Driver.transform.position = new Vector3(0, .08f, 1000);
        camera.transform.position = g.Driver.transform.position + new Vector3(0, 4.8f, -10.5f);
        camera.transform.LookAt(g.Driver.transform.position + new Vector3(0, .8f, 9));
        cars[0].transform.position = new Vector3(-3.6f, .08f, 1250);
        cars[1].transform.position = new Vector3(3.6f, .08f, 850);
        g.TogglePause(); g.World.Tick(.02f); g.TogglePause();
        CollectionAssert.Contains(g.World.Traffic, cars[0], "Visible traffic must clear naturally.");
        CollectionAssert.DoesNotContain(g.World.Traffic, cars[1], "Hidden traffic should drain from quiet road.");
        yield return null;
    }
    [UnityTest] public IEnumerator WeatherChangesTrafficSpeedAndFollowingDistance()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving(); g.TogglePause();
        var cars = TrafficForScenario(g); var car = cars[0];
        car.transform.position = new Vector3(0, .08f, 100); car.SetCruisingSpeed(25);
        g.Driver.transform.position = new Vector3(0, .08f, 155);
        float clearSpeed = g.World.TrafficSpeed(car, 30); float clearFactor = g.World.TrafficWeatherFactor;
        g.Driver.transform.position = new Vector3(0, .08f, 1100);
        for (int i = 0; i < 80; i++) g.Weather.Tick(.1f);
        Assert.That(g.World.TrafficWeatherFactor, Is.LessThan(clearFactor - .2f));
        g.Driver.transform.position = new Vector3(0, .08f, 155);
        Assert.That(g.World.TrafficSpeed(car, 30), Is.LessThan(clearSpeed));
        car.Tick(.5f); Assert.That(car.Speed, Is.LessThan(25));
        yield return null;
    }
    [UnityTest] public IEnumerator TrafficIsPresentAheadDuringEarlyAcceleration()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving();
        yield return DriveFor(8);
        int ahead = 0;
        foreach (var car in g.World.Traffic)
        {
            float gap = car.transform.position.z - g.Driver.transform.position.z;
            if (gap > 10 && gap < 220) ahead++;
        }
        Assert.That(g.Driver.Speed, Is.GreaterThan(25));
        Assert.That(ahead, Is.GreaterThanOrEqualTo(4), "The early road must contain visible encounters, not just distant pooled traffic.");
        Assert.That(g.World.PooledCount, Is.EqualTo(264));
    }
    [UnityTest] public IEnumerator DistantPassingDoesNotReserveAnEscapeLaneForAPlayerFarBehind()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving(); g.TogglePause();
        var cars = TrafficForScenario(g);
        cars[0].transform.position = new Vector3(-3.6f, .08f, 150); cars[0].SetCruisingSpeed(24);
        cars[1].transform.position = new Vector3(-3.6f, .08f, 172); cars[1].SetCruisingSpeed(12);
        cars[2].transform.position = new Vector3(3.6f, .08f, 150); cars[2].SetCruisingSpeed(20);
        Assert.That(g.World.CanMerge(cars[0], 0), Is.True, "Traffic in an unrelated lane must not suppress a safe pass far from the player.");
        g.Driver.transform.position = new Vector3(0, .08f, 140);
        Assert.That(g.World.CanMerge(cars[0], 0), Is.False, "The player still owns a safe reaction gap.");
        yield return null;
    }
    [UnityTest] public IEnumerator HeldGasReaches300WithSixGearsAndAccelerationHud()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return new WaitForSeconds(.2f);
        Assert.That(g.Driver.Speed, Is.InRange(.1f, 4));
        Assert.That(g.Driver.Acceleration, Is.GreaterThan(0));
        // Advance this component on a clear test road; traffic survival is validated separately.
        for (int i = 0; i < 2250; i++)
        {
            if (!g.Driver.IsShifting && g.Driver.Gear < 6 && g.Driver.RpmRatio > .92f) g.Driver.RequestShift(1);
            g.Driver.Tick(g.Input, g.Run.TopSpeed, .02f);
        }
        yield return new WaitForSeconds(.15f);
        Assert.That(g.Driver.Speed * 3.6f, Is.InRange(299.5f, 300.01f));
        Assert.That(g.Driver.Gear, Is.EqualTo(6));
        var hud = Object.FindAnyObjectByType<GameHud>();
        Assert.That(hud.DisplayedSpeedKph, Is.EqualTo(300));
        Assert.That(g.Driver.RevLimiter, Is.True);
        Assert.That(Mathf.Abs(hud.DisplayedAcceleration), Is.LessThan(3));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.S)); yield return new WaitForSeconds(.4f);
        Assert.That(g.Driver.Speed, Is.LessThan(VehicleDynamics.ForwardLimit - 3));
        Assert.That(hud.DisplayedAcceleration, Is.LessThan(-8));
        yield return Press(Key.R);
        Assert.That(g.Driver.Speed, Is.GreaterThan(0));
        Assert.That(g.Driver.RearView, Is.False, "Selecting reverse at speed must keep the view in the actual travel direction.");
        Assert.That(Camera.main.transform.position.z, Is.LessThan(g.Driver.transform.position.z));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }
    [UnityTest] public IEnumerator ManualGearKeysAudioAndPauseBehaveAsOneTransmission()
    {
        var g = GameManager.Instance;
        yield return Press(Key.E); Assert.That(g.Driver.Gear, Is.EqualTo(1));
        g.StartRun(); yield return WaitForDriving();
        Assert.That(g.Catalog.gearShiftSounds.Length, Is.EqualTo(3));
        Assert.That(g.Catalog.throttleOnSound.name, Is.EqualTo("Recorded-throttle-on"));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return new WaitForSeconds(4);
        Assert.That(g.Driver.Gear, Is.EqualTo(1), "Gas alone must not shift automatically.");
        Assert.That(g.Driver.Speed * 3.6f, Is.InRange(44.5f, 45.1f));
        Assert.That(g.Audio.ThrottleCueCount, Is.EqualTo(1), "A held gas pedal must not replay a rev every frame.");
        yield return ShiftWithGas(Key.E);
        Assert.That(g.Driver.IsShifting, Is.True); Assert.That(g.Driver.Gear, Is.EqualTo(1));
        Assert.That(g.Audio.ShiftCueCount, Is.EqualTo(1)); Assert.That(g.Audio.GearCuePlaying, Is.True);
        g.TogglePause(); yield return new WaitForSecondsRealtime(.3f);
        Assert.That(g.Driver.IsShifting, Is.True); Assert.That(g.Driver.Gear, Is.EqualTo(1));
        Assert.That(g.Audio.GearCuePlaying, Is.False);
        g.TogglePause(); yield return new WaitForSeconds(.7f);
        Assert.That(g.Driver.Gear, Is.EqualTo(2)); Assert.That(g.Driver.Speed, Is.GreaterThan(13));
        var hud = Object.FindAnyObjectByType<GameHud>(); Assert.That(hud.DisplayedGear, Is.EqualTo(2));
        Assert.That(hud.DisplayedRpm, Is.InRange(850, 7200));
        yield return new WaitForSeconds(3);
        Assert.That(g.Driver.Gear, Is.EqualTo(2));
        yield return ShiftWithGas(Key.E); yield return new WaitForSeconds(.5f);
        Assert.That(g.Driver.Gear, Is.EqualTo(3)); Assert.That(g.Audio.ShiftCueCount, Is.EqualTo(2));
        yield return ShiftWithGas(Key.Q);
        Assert.That(g.Driver.ShiftRejected, Is.True); Assert.That(g.Driver.Gear, Is.EqualTo(3));
        Assert.That(g.Audio.ShiftCueCount, Is.EqualTo(2), "Rejected over-rev downshifts must not play a successful gear change.");
        yield return Hold(Key.S, 1.2f);
        yield return ShiftWithGas(Key.Q);
        Assert.That(g.Driver.IsShifting, Is.True); Assert.That(g.Audio.PedalCuePlaying, Is.True);
        yield return new WaitForSeconds(.5f);
        Assert.That(g.Driver.Gear, Is.EqualTo(2)); Assert.That(g.Audio.ShiftCueCount, Is.EqualTo(3));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }
    [UnityTest] public IEnumerator HoldLookBackWhileDrivingKeepsForwardMotionGearAndSteering()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return new WaitForSeconds(1.5f);
        float before = g.Driver.transform.position.z; int gear = g.Driver.Gear;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.C)); yield return new WaitForSeconds(.2f);
        Assert.That(g.Driver.LookingBack, Is.True); Assert.That(g.Driver.CameraRearView, Is.True);
        Assert.That(g.Driver.Reversing, Is.False); Assert.That(g.Driver.RearView, Is.False);
        Assert.That(g.Driver.Speed, Is.GreaterThan(0)); Assert.That(g.Driver.transform.position.z, Is.GreaterThan(before));
        Assert.That(g.Driver.Gear, Is.EqualTo(gear));
        Assert.That(Camera.main.transform.position.z, Is.GreaterThan(g.Driver.transform.position.z));
        Assert.That(Camera.main.transform.forward.z, Is.LessThan(0));
        Assert.That(g.World.TrafficVisible(g.Driver.transform.position + new Vector3(0, 0, -150)), Is.True);
        Assert.That(g.World.TrySpawnTraffic(g.Driver.transform.position + new Vector3(0, 0, -150), 20), Is.Null);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.C, Key.A)); yield return new WaitForSeconds(.08f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.C)); yield return new WaitForSeconds(.25f);
        Assert.That(g.Driver.Lane, Is.EqualTo(0), "A/D keep their forward-driving meaning during a manual camera glance.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return new WaitForSeconds(.2f);
        Assert.That(g.Driver.LookingBack, Is.False); Assert.That(g.Driver.CameraRearView, Is.False);
        Assert.That(Camera.main.transform.position.z, Is.LessThan(g.Driver.transform.position.z));
        Assert.That(Camera.main.transform.forward.z, Is.GreaterThan(0));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }
    [UnityTest] public IEnumerator LookBackCameraKeepsPlayerInFrontDuringFastForwardTravel()
    {
        var g = GameManager.Instance; g.StartRun(); yield return WaitForDriving();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C)); yield return new WaitForSeconds(.1f);
        // Repeated large travel steps reproduce longitudinal follow lag even at fast test frame rates.
        for (int i = 0; i < 12; i++)
        {
            g.Driver.transform.position += Vector3.forward * 4;
            yield return new WaitForEndOfFrame();
            Assert.That(Camera.main.transform.position.z - g.Driver.transform.position.z, Is.GreaterThan(8),
                "Forward travel must never leave the rear-facing camera behind the player.");
            Assert.That(Camera.main.WorldToViewportPoint(g.Driver.transform.position).z, Is.GreaterThan(0));
        }
    }
    [UnityTest] public IEnumerator LookBackHeldThroughIgnitionPauseAndMenuResetsCorrectly()
    {
        var g = GameManager.Instance;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C)); yield return new WaitForSeconds(.1f);
        Assert.That(g.Driver.LookingBack, Is.False, "The driving action is disabled in the menu.");
        g.StartRun(); yield return WaitForDriving(); yield return new WaitForSeconds(.1f);
        Assert.That(g.Driver.LookingBack, Is.True, "A held C key must be recognized when ignition enables driving.");
        Assert.That(g.Driver.Speed, Is.Zero); Assert.That(g.Driver.Reversing, Is.False);
        Assert.That(Camera.main.transform.position.z, Is.GreaterThan(g.Driver.transform.position.z));
        g.TogglePause(); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(.15f);
        g.TogglePause(); yield return new WaitForSeconds(.15f);
        Assert.That(g.Driver.LookingBack, Is.False); Assert.That(Camera.main.transform.position.z, Is.LessThan(g.Driver.transform.position.z));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C)); yield return new WaitForSeconds(.1f);
        Assert.That(g.Driver.LookingBack, Is.True);
        g.MainMenu(); yield return new WaitForSeconds(.1f); Assert.That(g.Driver.LookingBack, Is.False);
        Assert.That(g.Driver.Gear, Is.EqualTo(1)); Assert.That(g.Driver.Reversing, Is.False);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }
}
