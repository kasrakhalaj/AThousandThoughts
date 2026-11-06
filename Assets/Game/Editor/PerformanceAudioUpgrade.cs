using AfterHours;
using UnityEditor;
using UnityEngine;
public static class PerformanceAudioUpgrade
{
    [MenuItem("After Hours/Apply performance engine voices")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Game/Generated/GameCatalog.asset");
        Undo.RecordObject(catalog, "Performance engine voices");
        catalog.performanceVoices = new[] { Voice("TRISTAR RACER", "Tristar", 1.12f, 1.1f), Voice("DOCLOREAN", "docLorean", .88f, 1.1f) };
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
    }
    static CarEngineVoice Voice(string car, string prefix, float pitch, float gain)
    {
        return new CarEngineVoice { carName = car, idle = Clip(prefix,"idle"), mid = Clip(prefix,"mid"), load = Clip(prefix,"load"), gasOn = Clip(prefix,"gas-on"), gasOff = Clip(prefix,"gas-off"), downshift = Clip(prefix,"downshift"), pitch = pitch, gain = gain };
    }
    static AudioClip Clip(string prefix, string suffix)
    {
        string path = "Assets/Game/Audio/Performance/" + prefix + "-" + suffix + ".wav";
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new System.InvalidOperationException("Missing performance recording: " + path);
        importer.forceToMono = true; var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = settings; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
