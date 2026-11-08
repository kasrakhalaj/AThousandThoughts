using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace AfterHours
{
    public sealed class MusicPlayer : MonoBehaviour
    {
        public readonly List<string> Categories = new List<string> { "Singles", "All Music" };
        readonly Dictionary<string, List<string>> library = new Dictionary<string, List<string>>();
        AudioSource source;
        AudioClip loaded;
        Coroutine loading;
        int requestVersion;
        int category;
        int track = -1;
        bool userPaused;
        bool wasPlaying;
        public string Category => Categories[category];
        public string TrackName { get; private set; } = "Choose a track";
        public string Artist { get; private set; } = "Your music, your drive";
        public string Status { get; private set; } = "";
        public bool Shuffle { get; private set; }
        public float Volume { get; private set; } = .45f;
        public bool IsPlaying => source != null && source.isPlaying;
        public int TrackCount => library.TryGetValue(Category, out var tracks) ? tracks.Count : 0;
        public float Progress => loaded != null && loaded.length > 0 ? source.time / loaded.length : 0;
        public readonly float[] Spectrum = new float[64];

        public void Initialize()
        {
            source = gameObject.AddComponent<AudioSource>(); source.volume = Volume;
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Music");
            if (Application.isEditor && !Directory.Exists(folder))
                folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Music"));
            library["All Music"] = new List<string>();
            library["Singles"] = new List<string>();
            try
            {
                if (Directory.Exists(folder))
                    foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                    {
                        if (!string.Equals(Path.GetExtension(file), ".mp3", StringComparison.OrdinalIgnoreCase)) continue;
                        string relative = Path.GetRelativePath(folder, file);
                        string artist = Path.GetDirectoryName(relative);
                        if (string.IsNullOrEmpty(artist)) artist = "Singles";
                        artist = artist.Replace(Path.DirectorySeparatorChar, '/');
                        if (!library.ContainsKey(artist)) { Categories.Add(artist); library[artist] = new List<string>(); }
                        library[artist].Add(file); library["All Music"].Add(file);
                        // Pin the requested opening tracks in Singles while retaining their artist playlists.
                        if (artist != "Singles" && OpeningTrackPriority(file) < 2) library["Singles"].Add(file);
                    }
                foreach (var tracks in library.Values)
                    tracks.Sort((a, b) => { int priority = OpeningTrackPriority(a).CompareTo(OpeningTrackPriority(b)); return priority != 0 ? priority : StringComparer.OrdinalIgnoreCase.Compare(a, b); });
                Categories.Sort(2, Categories.Count - 2, StringComparer.OrdinalIgnoreCase);
                if (library["Singles"].Count == 0) category = 1;
                if (TrackCount > 0) Next(1); else Status = "Add MP3 files to the Music folder";
            }
            catch (IOException e) { Status = "Music folder unavailable"; Debug.LogWarning(e.Message); }
            catch (UnauthorizedAccessException e) { Status = "Music folder unavailable"; Debug.LogWarning(e.Message); }
        }
        static int OpeningTrackPriority(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name.StartsWith("Kavinsky - Nightcall", StringComparison.OrdinalIgnoreCase)) return 0;
            if (name.IndexOf("DVRST", StringComparison.OrdinalIgnoreCase) >= 0 && name.IndexOf("Tell Me What", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            return 2;
        }
        public void ChangeCategory(int step)
        {
            category = (category + step + Categories.Count) % Categories.Count; track = -1; Next(1);
        }
        public void Next(int step)
        {
            if (TrackCount == 0) return;
            var tracks = library[Category];
            int chosen = Shuffle && step > 0 && tracks.Count > 1 ? (track + UnityEngine.Random.Range(1, tracks.Count)) % tracks.Count : (track + step + tracks.Count) % tracks.Count;
            track = chosen; requestVersion++;
            if (loading != null) StopCoroutine(loading);
            loading = StartCoroutine(LoadTrack(tracks[track], requestVersion));
        }
        IEnumerator LoadTrack(string path, int version)
        {
            Status = "Loading track..."; wasPlaying = false; source.Stop();
            using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.MPEG))
            {
                ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = true;
                yield return request.SendWebRequest();
                if (version != requestVersion) yield break;
                if (request.result != UnityWebRequest.Result.Success) { Status = "Cannot play this track"; Debug.LogWarning("Music: " + request.error); loading = null; yield break; }
                source.clip = null;
                if (loaded != null) Destroy(loaded);
                loaded = DownloadHandlerAudioClip.GetContent(request); source.clip = loaded;
                TrackName = Path.GetFileNameWithoutExtension(path);
                Artist = Path.GetFileName(Path.GetDirectoryName(path));
                source.Play(); userPaused = false; wasPlaying = true; Status = ""; loading = null;
            }
        }
        public void Toggle() { userPaused = !userPaused; if (userPaused) source.Pause(); else source.UnPause(); }
        public void ToggleShuffle() { Shuffle = !Shuffle; }
        public void SetVolume(float value) { Volume = value; if (source != null) source.volume = value; }
        void Update()
        {
            if (source == null) return;
            if (source.isPlaying) { source.GetSpectrumData(Spectrum, 0, FFTWindow.BlackmanHarris); wasPlaying = true; }
            else Array.Clear(Spectrum, 0, Spectrum.Length);
            if (wasPlaying && !source.isPlaying && !userPaused && loading == null && loaded != null) { wasPlaying = false; Next(1); }
        }
        void OnDestroy() { if (loaded != null) Destroy(loaded); }
    }
}
