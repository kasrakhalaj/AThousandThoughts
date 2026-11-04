using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class LookBackUpgrade
{
    [MenuItem("After Hours/Apply hold-to-look-back controls")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        const string path = "Assets/Game/Generated/AfterHours.inputactions";
        var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        var map = controls.FindActionMap("Driving", true);
        var action = map.FindAction("LookBack") ?? map.AddAction("LookBack", InputActionType.Button);
        action.wantsInitialStateCheck = true;
        foreach (string bindingPath in new[] { "<Keyboard>/c", "<Gamepad>/rightStickPress" })
        {
            bool exists = false;
            foreach (var binding in action.bindings) if (binding.path == bindingPath) exists = true;
            if (!exists) action.AddBinding(bindingPath);
        }
        File.WriteAllText(path, controls.ToJson()); AssetDatabase.ImportAsset(path); AssetDatabase.SaveAssets();
        Debug.Log("HOLD_TO_LOOK_BACK_CONTROLS_APPLIED");
    }
}
