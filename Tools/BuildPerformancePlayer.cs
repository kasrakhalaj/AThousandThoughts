using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class BuildPerformancePlayer
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building.");
        string folder = Path.GetFullPath("../Build/AfterHours-Performance"); Directory.CreateDirectory(folder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Game/Scenes/AfterHours.unity" }, locationPathName = Path.Combine(folder, "AfterHours.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
        var s = report.summary;
        var result = new { result = s.result.ToString(), errors = s.totalErrors, warnings = s.totalWarnings, seconds = s.totalTime.TotalSeconds, bytes = s.totalSize, path = s.outputPath,
            messages = report.steps.SelectMany(step => step.messages).Where(m => m.type == UnityEngine.LogType.Warning || m.type == UnityEngine.LogType.Error).Select(m => m.content).ToArray() };
        File.WriteAllText("Docs/Validation/reverse-audio-build.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        if (s.result != BuildResult.Succeeded) throw new Exception("Performance build failed: " + s.result);
        string source = Path.GetFullPath("../Music");
        foreach (string file in Directory.GetFiles(source, "*.mp3", SearchOption.AllDirectories))
        {
            string output = Path.Combine(folder, "Music", Path.GetRelativePath(source, file)); Directory.CreateDirectory(Path.GetDirectoryName(output)); File.Copy(file, output, true);
        }
        return result;
    }
}
