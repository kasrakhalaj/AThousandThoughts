using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ValidateGtrAudio
{
    public static object Main()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Exit Play Mode and wait for compilation first.");
        var catalog = AssetDatabase.LoadAssetAtPath<AfterHours.GameCatalog>("Assets/Game/Generated/GameCatalog.asset");
        if (catalog == null || catalog.performanceVoices.Length != 2) throw new InvalidOperationException("Missing performance profiles.");
        var profiles = catalog.performanceVoices.Select(voice =>
        {
            if (Mathf.Abs(voice.pitch - 1) > .001f) throw new InvalidOperationException("Unexpected recording pitch multiplier.");
            var clips = new[] { voice.idle, voice.mid, voice.load, voice.gasOn, voice.gasOff, voice.downshift }.Select(clip =>
            {
                if (clip == null || clip.channels != 1 || clip.frequency != 44100 || clip.length < .25f) throw new InvalidOperationException("Missing or invalid imported recording.");
                clip.LoadAudioData();
                var samples = new float[clip.samples];
                if (!clip.GetData(samples, 0)) throw new InvalidOperationException("Cannot read imported recording: " + clip.name);
                double rms = Math.Sqrt(samples.Average(sample => (double)sample * sample));
                if (rms < .02 || rms > .16 || samples.Max(sample => Math.Abs(sample)) > .83f) throw new InvalidOperationException("Unexpected recording levels: " + clip.name);
                return new { path = AssetDatabase.GetAssetPath(clip), samples = clip.samples, channels = clip.channels, frequency = clip.frequency, length = clip.length, loadState = clip.loadState.ToString(), rms };
            }).ToArray();
            return new { car = voice.carName, pitch = voice.pitch, clips };
        }).ToArray();
        return new { success = true, profiles };
    }
}
