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
    static int monitoredFrames, blockedRowFrames;
    static bool releaseSteering, hadScore, captured;
    static int score;
    static AudioSource gearTap, pedalTap;
    static readonly float[] audioTap = new float[512];
    static float maxGearRms, maxPedalRms;
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
        random=UnityEngine.Random.state; UnityEngine.Random.InitState(SessionState.GetInt("AfterHours.Probe.Seed",4821));
        gearTap=(AudioSource)typeof(CarAudio).GetField("shiftEffects", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(g.Audio);
        pedalTap=(AudioSource)typeof(CarAudio).GetField("pedalEffects", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(g.Audio);
        gearTap.GetOutputData(audioTap,0); pedalTap.GetOutputData(audioTap,0); maxGearRms=maxPedalRms=0;
        samples.Clear();captured=false;start=-1;sampleAt=0;steerAt=0;lastFrame=-1;monitoredFrames=blockedRowFrames=0;
        g.StartRun();
        float initialZ=SessionState.GetFloat("AfterHours.Probe.StartZ",0);
        if(initialZ!=0){g.Driver.transform.position=new Vector3(0,.08f,initialZ);g.World.ResetWorld();}
        Time.timeScale=2;
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
        maxGearRms=Mathf.Max(maxGearRms,SampleAudio(gearTap)); maxPedalRms=Mathf.Max(maxPedalRms,SampleAudio(pedalTap));
        monitoredFrames++; if(HasNearFullRow(g))blockedRowFrames++;
        Key steering=Key.None;
        bool shiftUp = !g.Driver.IsShifting && g.Driver.Gear < 6 && g.Driver.RpmRatio > .92f;
        bool shiftDown = !g.Driver.IsShifting && g.Driver.Gear > 1 && g.Driver.RpmRatio < .4f && g.Driver.Speed < VehicleDynamics.GearSpeedLimit(g.Driver.Gear - 1) * .96f;
        Key gearKey = shiftUp ? Key.E : shiftDown ? Key.Q : Key.None;
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
        bool lookingBack = SessionState.GetBool("AfterHours.Probe.LookBack", false) && elapsed >= 10 && elapsed < 14;
        if (lookingBack)
        {
            if (gearKey != Key.None) InputSystem.QueueStateEvent(keyboard, steering==Key.None ? new KeyboardState(Key.W,Key.C,gearKey) : new KeyboardState(Key.W,Key.C,gearKey,steering));
            else InputSystem.QueueStateEvent(keyboard,steering==Key.None ? new KeyboardState(Key.W,Key.C) : new KeyboardState(Key.W,Key.C,steering));
        }
        else if (gearKey != Key.None) InputSystem.QueueStateEvent(keyboard, steering==Key.None ? new KeyboardState(Key.W,gearKey) : new KeyboardState(Key.W,gearKey,steering));
        else InputSystem.QueueStateEvent(keyboard,steering==Key.None?new KeyboardState(Key.W):new KeyboardState(Key.W,steering));
        bool captureReady=SessionState.GetBool("AfterHours.Probe.CaptureStorm",false) ? g.Weather.StormAmount>.65f : elapsed>=12;
        if(!captured && captureReady)
        {
            captured=true;
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(SessionState.GetString("AfterHours.Density.CapturePath", "Docs/Images/density-driving.png")));
        }
        if(elapsed>=sampleAt)
        {
            sampleAt=elapsed+1;
            int ahead=0,near=0,merging=0,visible=0,fastDrivers=0;float maxAiSpeed=0,maxDesiredSpeed=0;
            foreach(var c in g.World.Traffic)
            {
                float gap=c.transform.position.z-g.Driver.transform.position.z;
                maxAiSpeed=Mathf.Max(maxAiSpeed,c.Speed);
                if(c is MovingCar moving){if(moving.IsFastDriver)fastDrivers++;maxDesiredSpeed=Mathf.Max(maxDesiredSpeed,moving.DesiredSpeed);}
                if(gap>8 && gap<180){ahead++; if(g.World.TrafficVisible(c.transform.position))visible++;}
                if(Mathf.Abs(gap)<180)near++;
                if(Mathf.Abs(gap)<180 && c is MovingCar m && m.IsMerging)merging++;
            }
            samples.Add(new {seconds=elapsed,z=g.Driver.transform.position.z,speed=g.Driver.Speed,acceleration=g.Driver.Acceleration,gear=g.Driver.Gear,lookingBack=g.Driver.LookingBack,reversing=g.Driver.Reversing,cameraZ=Camera.main.transform.position.z,cameraForwardZ=Camera.main.transform.forward.z,rpm=g.Driver.EngineRpm,shifting=g.Driver.IsShifting,shiftCues=g.Audio.ShiftCueCount,pedalCues=g.Audio.ThrottleCueCount,flow=g.World.Flow.ToString(),rain=g.Weather.RainAmount,storm=g.Weather.StormAmount,fogDensity=RenderSettings.fogDensity,rainEmission=g.Weather.RainEmissionRate,rainVolume=g.Audio.RainVolume,fastDrivers,maxAiSpeed,maxDesiredSpeed,ahead,near,visible,merging,total=g.World.ActiveTrafficCount,lives=g.Run.Lives});
        }
    }
    static bool HasNearFullRow(GameManager g)
    {
        float playerZ=g.Driver.transform.position.z;
        foreach(var anchor in g.World.Traffic)
        {
            float gap=anchor.transform.position.z-playerZ;
            float closing=gap>=0?g.Driver.Speed-anchor.Speed:anchor.Speed-g.Driver.Speed;
            if(Mathf.Abs(gap)>Mathf.Max(45,20+Mathf.Max(0,closing)*2))continue;
            int occupied=0;
            foreach(var c in g.World.Traffic)
            {
                if(Mathf.Abs(c.transform.position.z-anchor.transform.position.z)>8)continue;
                for(int lane=0;lane<3;lane++)
                    if(Mathf.Abs(c.transform.position.x-(lane-1)*CarDriver.LaneWidth)<1.65f)occupied|=1<<lane;
            }
            if(occupied==7)return true;
        }
        return false;
    }
    static float SampleAudio(AudioSource source)
    {
        if(source==null || !source.isPlaying) return 0;
        source.GetOutputData(audioTap,0); float sum=0; foreach(float value in audioTap)sum+=value*value;
        return Mathf.Sqrt(sum/audioTap.Length);
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
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new {reason,seed=SessionState.GetInt("AfterHours.Probe.Seed",4821),monitoredFrames,blockedRowFrames,scenarioStartZ=SessionState.GetFloat("AfterHours.Probe.StartZ",0),maxGearRms,maxPedalRms,samples},Newtonsoft.Json.Formatting.Indented));
        if(g!=null){g.MainMenu();g.Input.StartCar.actionMap.asset.devices=null;}
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=routing;
        if(hadScore)PlayerPrefs.SetInt("AfterHours.HighScore.v1",score);else PlayerPrefs.DeleteKey("AfterHours.HighScore.v1");PlayerPrefs.Save();
        UnityEngine.Random.state=random;Time.timeScale=1;
    }
}
