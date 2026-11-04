using System;
using System.Collections.Generic;
using System.IO;
using AfterHours;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class LiveDensityProbe
{
    static Keyboard keyboard;
    static InputSettings.BackgroundBehavior background;
    static InputSettings.EditorInputBehaviorInPlayMode routing;
    static UnityEngine.Random.State random;
    static readonly List<object> samples = new List<object>();
    static float start=-1, sampleAt, steerAt;
    static int lastFrame=-1;
    static bool releaseSteering, hadScore, captured;
    static int score;
    public static object Main()
    {
        var g=GameManager.Instance;
        if(g==null || g.Input==null) return "Open the AfterHours scene and wait for GameManager initialization before running the probe.";
        if(g.State!=GameState.Menu && g.State!=GameState.GameOver) return "Return to the menu before starting a repeatable traffic probe.";
        background=InputSystem.settings.backgroundBehavior; routing=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();
        g.Input.StartCar.actionMap.asset.devices=new InputDevice[]{keyboard};
        hadScore=PlayerPrefs.HasKey("AfterHours.HighScore.v1");score=PlayerPrefs.GetInt("AfterHours.HighScore.v1");
        random=UnityEngine.Random.state; UnityEngine.Random.InitState(4821);
        samples.Clear();captured=false;start=-1;sampleAt=0;steerAt=0;lastFrame=-1;
        g.StartRun();Time.timeScale=2;
        EditorApplication.update+=Update;
        return "Started 65-second simulated-time drive using held W and obstacle-avoidance lane keypresses.";
    }
    static void Update()
    {
        var g=GameManager.Instance;
        if(!EditorApplication.isPlaying || g==null) {Finish("stopped");return;}
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        if(g.State==GameState.Starting){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));return;}
        if(start<0)start=Time.time;
        float elapsed=Time.time-start;
        if(g.State!=GameState.Playing || elapsed>=65){Finish(g.State.ToString());return;}
        Key steering=Key.None;
        if(!releaseSteering && elapsed>=steerAt)
        {
            steerAt=elapsed+.35f;
            int current=g.Driver.Lane,best=current;
            float bestGap=LaneGap(g,current);
            if(bestGap<65)
            for(int lane=Mathf.Max(0,current-1);lane<=Mathf.Min(2,current+1);lane++)
            {
                float gap=LaneGap(g,lane);
                if(gap>bestGap+10){best=lane;bestGap=gap;}
            }
            if(best!=current){steering=best>current?Key.D:Key.A;releaseSteering=true;}
        }
        else releaseSteering=false;
        InputSystem.QueueStateEvent(keyboard,steering==Key.None?new KeyboardState(Key.W):new KeyboardState(Key.W,steering));
        if(!captured && elapsed>=12)
        {
            captured=true;
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(SessionState.GetString("AfterHours.Density.CapturePath", "Docs/Images/density-driving.png")));
        }
        if(elapsed>=sampleAt)
        {
            sampleAt=elapsed+1;
            int ahead=0,near=0,merging=0,visible=0;
            foreach(var c in g.World.Traffic)
            {
                float gap=c.transform.position.z-g.Driver.transform.position.z;
                if(gap>8 && gap<180){ahead++; if(g.World.TrafficVisible(c.transform.position))visible++;}
                if(Mathf.Abs(gap)<180)near++;
                if(Mathf.Abs(gap)<180 && c is MovingCar m && m.IsMerging)merging++;
            }
            samples.Add(new {seconds=elapsed,z=g.Driver.transform.position.z,speed=g.Driver.Speed,acceleration=g.Driver.Acceleration,gear=g.Driver.Gear,flow=g.World.Flow.ToString(),ahead,near,visible,merging,total=g.World.ActiveTrafficCount,lives=g.Run.Lives});
        }
    }
    static float LaneGap(GameManager g,int lane)
    {
        float gap=500;
        foreach(var c in g.World.Traffic)
        {
            if(Mathf.Abs(c.transform.position.x-(lane-1)*3.6f)>2.2f)continue;
            float dz=c.transform.position.z-g.Driver.transform.position.z;
            if(dz>-15)gap=Mathf.Min(gap,Mathf.Abs(dz));
        }
        return gap;
    }
    static void Finish(string reason)
    {
        EditorApplication.update-=Update;
        var g=GameManager.Instance;
        string output=SessionState.GetString("AfterHours.Density.Output","Temp/DensityBefore.json");
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new {reason,samples},Newtonsoft.Json.Formatting.Indented));
        if(g!=null){g.MainMenu();g.Input.StartCar.actionMap.asset.devices=null;}
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=routing;
        if(hadScore)PlayerPrefs.SetInt("AfterHours.HighScore.v1",score);else PlayerPrefs.DeleteKey("AfterHours.HighScore.v1");PlayerPrefs.Save();
        UnityEngine.Random.state=random;Time.timeScale=1;
    }
}
