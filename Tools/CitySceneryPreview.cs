using System;
using System.IO;
using AfterHours;
using UnityEditor;
using UnityEngine;

// Controlled visual review only. The natural driving probe is a separate check.
public static class CitySceneryPreview
{
    static readonly string[] labels = { "downtown", "cafe", "overpass", "residential", "park", "service-storm", "woodland", "transition", "service-detail" };
    static readonly float[] locations = { 12, 116, 340, 500, 650, 1115, 1450, 1850, 1147 };
    static int index, frame, stage;
    static double until;
    static bool hadScore, savedEnabled;
    static int score;
    static UnityEngine.Random.State random;
    static FollowCamera follow;
    static GameManager game;
    public static object Main()
    {
        game = GameManager.Instance;
        if (game == null || game.State != GameState.Menu) throw new InvalidOperationException("Start at the game menu.");
        hadScore = PlayerPrefs.HasKey("AfterHours.HighScore.v1"); score = PlayerPrefs.GetInt("AfterHours.HighScore.v1"); random = UnityEngine.Random.state;
        index = 0; stage = 0; frame = -1; savedEnabled = game.enabled;
        Directory.CreateDirectory("Docs/Images/City"); game.StartRun();
        EditorApplication.update += Update; return "Capturing nine controlled route views.";
    }
    static void Update()
    {
        if (!EditorApplication.isPlaying || game == null) { Finish(); return; }
        if (frame == Time.frameCount) return; frame = Time.frameCount;
        if (stage == 0)
        {
            if (game.State != GameState.Playing) return;
            game.enabled = false; follow = Camera.main.GetComponent<FollowCamera>(); follow.enabled = false;
            SetView(); stage = 1; return;
        }
        UnityEngine.Object.FindAnyObjectByType<GameHud>().Tick();
        if (EditorApplication.timeSinceStartup < until) return;
        if (stage == 1)
        {
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/Images/City/" + labels[index] + ".png"));
            until = EditorApplication.timeSinceStartup + .6; stage = 2; return;
        }
        if (++index >= labels.Length) { Finish(); return; }
        SetView(); stage = 1;
    }
    static void SetView()
    {
        float z = locations[index]; game.Driver.transform.position = new Vector3(0, .08f, z); game.World.ResetWorld();
        // Remove only the staged foreground occluders for landmarks; traffic still appears in the separate live drive.
        foreach (var car in game.World.Traffic) if (Mathf.Abs(car.transform.position.z - z) < 100) car.gameObject.SetActive(false);
        game.Weather.ResetWeather(); for (int i = 0; i < 100; i++) game.Weather.Tick(.1f);
        if (index == 8) game.Weather.ResetWeather();
        Vector3 offset = index == 1 || index == 4 || index == 5 || index == 8 ? new Vector3(-2.8f, 4.4f, -9) : new Vector3(0, 4.8f, -10.5f);
        Vector3 target = game.Driver.transform.position + new Vector3(index == 1 || index == 4 || index == 5 || index == 8 ? 9 : 0, .8f, 16);
        var camera = Camera.main; camera.transform.position = game.Driver.transform.position + offset; camera.transform.LookAt(target); camera.fieldOfView = 60;
        UnityEngine.Object.FindAnyObjectByType<GameHud>().Tick();
        until = EditorApplication.timeSinceStartup + 1.3;
    }
    static void Finish()
    {
        EditorApplication.update -= Update;
        if (follow != null) follow.enabled = true;
        if (game != null) { game.enabled = savedEnabled; game.MainMenu(); }
        if (hadScore) PlayerPrefs.SetInt("AfterHours.HighScore.v1", score); else PlayerPrefs.DeleteKey("AfterHours.HighScore.v1");
        PlayerPrefs.Save(); UnityEngine.Random.state = random;
        File.WriteAllText("Docs/Validation/city-preview.json", "{\"controlledVisualViews\":9,\"complete\":" + (index >= labels.Length ? "true" : "false") + "}");
    }
}
