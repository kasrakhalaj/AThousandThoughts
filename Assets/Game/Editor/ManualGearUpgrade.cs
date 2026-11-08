using System;
using System.IO;
using AfterHours;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ManualGearUpgrade
{
    [MenuItem("A Thousand Thoughts/Apply manual gearbox and recorded shift audio")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        const string controlsPath = "Assets/Game/Generated/AfterHours.inputactions";
        var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
        var driving = controls.FindActionMap("Driving", true);
        Bind(driving, "ShiftUp", "<Keyboard>/e", "<Gamepad>/rightShoulder");
        Bind(driving, "ShiftDown", "<Keyboard>/q", "<Gamepad>/leftShoulder");
        File.WriteAllText(controlsPath, controls.ToJson()); AssetDatabase.ImportAsset(controlsPath);
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Game/Generated/GameCatalog.asset");
        catalog.gearShiftSounds = new[] { Clip("gear-shift-1"), Clip("gear-shift-2"), Clip("gear-shift-3") };
        catalog.throttleOnSound = Clip("throttle-on"); catalog.throttleOffSound = Clip("throttle-off");
        catalog.downshiftBlipSound = Clip("downshift-blip");
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        Debug.Log("MANUAL_SIX_SPEED_AND_RECORDED_GEAR_GAS_AUDIO_APPLIED");
    }
    static void Bind(InputActionMap map, string name, params string[] paths)
    {
        var action = map.FindAction(name) ?? map.AddAction(name, InputActionType.Button);
        foreach (var path in paths)
        {
            bool exists = false;
            foreach (var binding in action.bindings) if (binding.path == path) exists = true;
            if (!exists) action.AddBinding(path);
        }
    }
    static AudioClip Clip(string name)
    {
        string path = "Assets/Game/Audio/Recorded/Recorded-" + name + ".wav";
        if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null) throw new InvalidOperationException("Missing recording: " + path);
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.forceToMono = true; importer.defaultSampleSettings = settings; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
