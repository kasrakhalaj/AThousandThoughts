using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AfterHours;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class TrafficAudioUpgrade
{
    const string Folder = "Assets/Game/Generated/Traffic";
    static readonly string[] Names = { "NormalCar1", "NormalCar2", "SUV", "SportsCar", "SportsCar2", "Taxi", "Cop" };

    [MenuItem("After Hours/Apply recorded audio and 2018 traffic pack")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Game/Generated/GameCatalog.asset");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var models = new GameObject[Names.Length];
            var variants = new GameObject[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                models[i] = BuildModel(Names[i], preview);
                variants[i] = Wrap(models[i], true, i == 0 ? "Assets/Game/Generated/MovingCar.prefab" : Folder + "/Traffic-" + Names[i] + ".prefab", preview);
            }
            catalog.stationaryCar = Wrap(models[2], false, "Assets/Game/Generated/StationaryCar.prefab", preview);
            catalog.movingCar = variants[0]; catalog.movingTrafficVariants = variants;
            catalog.engineSound = Clip("player-idle"); catalog.engineMidSound = Clip("player-mid");
            catalog.engineLoadSound = Clip("player-load"); catalog.trafficEngineSound = Clip("traffic-engine");
            catalog.ignitionSound = Clip("ignition");
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        Debug.Log("RECORDED_AUDIO_AND_SEVEN_TRAFFIC_CARS_APPLIED");
    }

    static AudioClip Clip(string name)
    {
        string path = "Assets/Game/Audio/Recorded/Recorded-" + name + ".wav";
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) throw new InvalidOperationException("Missing recorded audio: " + path);
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.quality = .9f; settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.defaultSampleSettings = settings; importer.forceToMono = true; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    static Material Material(Material source)
    {
        string name = "Traffic-" + source.name + "-" + ColorUtility.ToHtmlStringRGB(source.color);
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.color = source.color; material.SetFloat("_Smoothness", source.name == "Windows" ? .6f : .24f);
        material.enableInstancing = true; EditorUtility.SetDirty(material); return material;
    }
    static Material LampMaterial(bool front)
    {
        return AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Generated/" + (front ? "Headlight lens" : "Rear light lens") + ".mat");
    }
    static Bounds ModelBounds(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        var result = renderers[0].bounds;
        foreach (var renderer in renderers) result.Encapsulate(renderer.bounds);
        return result;
    }

    static GameObject BuildModel(string name, Scene preview)
    {
        var root = new GameObject("TrafficModel-" + name); SceneManager.MoveGameObjectToScene(root, preview);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RealCars2018/" + name + ".fbx");
        if (source == null) throw new InvalidOperationException("Missing traffic FBX: " + name);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source, preview); model.transform.SetParent(root.transform, false);
        // This pack is already Y-up with the front wheels on +Z. Preserve its authored orientation.
        var bounds = ModelBounds(model); model.transform.localScale *= 4.45f / bounds.size.z;
        bounds = ModelBounds(model); model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        var filters = model.GetComponentsInChildren<MeshFilter>();
        var originalMaterials = filters.ToDictionary(f => f, f => f.GetComponent<MeshRenderer>().sharedMaterials);
        foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>()) renderer.sharedMaterials = renderer.sharedMaterials.Select(Material).ToArray();
        var lighting = new GameObject("Vehicle lighting"); lighting.transform.SetParent(root.transform, false);
        var controller = lighting.AddComponent<VehicleLights>();
        var front = new Renderer[2]; var rear = new Renderer[2]; var beams = new Light[2];
        for (int side = 0; side < 2; side++)
        {
            front[side] = ExtractLamp(name, root.transform, lighting.transform, filters, originalMaterials, true, side);
            rear[side] = ExtractLamp(name, root.transform, lighting.transform, filters, originalMaterials, false, side);
            var beam = new GameObject("Headlight beam", typeof(Light)); beam.transform.SetParent(lighting.transform, false);
            beam.transform.localPosition = root.transform.InverseTransformPoint(front[side].bounds.center) + Vector3.forward * .08f;
            beam.transform.localRotation = Quaternion.Euler(7, side == 0 ? -4 : 4, 0);
            beams[side] = beam.GetComponent<Light>(); beams[side].type = LightType.Spot;
            beams[side].color = new Color(1, .91f, .7f); beams[side].range = 30; beams[side].intensity = 9;
            beams[side].spotAngle = 54; beams[side].innerSpotAngle = 28;
        }
        var glow = new GameObject("Rear red glow", typeof(Light)); glow.transform.SetParent(lighting.transform, false);
        glow.transform.localPosition = new Vector3(0, .5f, -2.3f);
        var back = glow.GetComponent<Light>(); back.type = LightType.Point; back.color = new Color(1, .025f, .01f); back.range = 3.5f; back.intensity = .22f;
        controller.Configure(front, rear, Array.Empty<Renderer>(), beams, back);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/TrafficModel-" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root); return prefab;
    }

    static Renderer ExtractLamp(string name, Transform root, Transform parent, MeshFilter[] filters, Dictionary<MeshFilter, Material[]> materials, bool front, int side)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        foreach (var filter in filters)
        {
            var mesh = filter.sharedMesh; var points = mesh.vertices; var normals = mesh.normals;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                if (!materials[filter][sub].name.Equals(front ? "Headlights" : "TailLights", StringComparison.OrdinalIgnoreCase)) continue;
                int[] indices = mesh.GetTriangles(sub);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 center = Vector3.zero;
                    for (int n = 0; n < 3; n++) center += root.InverseTransformPoint(filter.transform.TransformPoint(points[indices[i + n]]));
                    if ((center.x < 0 ? 0 : 1) != side) continue;
                    for (int n = 0; n < 3; n++)
                    {
                        int index = indices[i + n];
                        Vector3 p = root.InverseTransformPoint(filter.transform.TransformPoint(points[index]));
                        Vector3 normal = root.InverseTransformDirection(filter.transform.TransformDirection(normals[index])).normalized;
                        triangles.Add(vertices.Count); vertices.Add(p + normal * .008f);
                    }
                }
            }
        }
        if (vertices.Count == 0) throw new InvalidOperationException("Missing authored lamp geometry: " + name);
        var lampMesh = new Mesh { name = name + (front ? " headlight " : " taillight ") + side };
        lampMesh.SetVertices(vertices); lampMesh.SetTriangles(triangles, 0); lampMesh.RecalculateNormals(); lampMesh.RecalculateBounds();
        string path = Folder + "/" + name + (front ? "-head-" : "-tail-") + side + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(lampMesh, path);
        else { EditorUtility.CopySerialized(lampMesh, existing); UnityEngine.Object.DestroyImmediate(lampMesh); lampMesh = existing; EditorUtility.SetDirty(existing); }
        var go = new GameObject(front ? "Headlight lens" : "Red tail and brake light", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false); go.GetComponent<MeshFilter>().sharedMesh = lampMesh;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = LampMaterial(front); renderer.shadowCastingMode = ShadowCastingMode.Off; return renderer;
    }

    static GameObject Wrap(GameObject model, bool moving, string path, Scene preview)
    {
        var root = new GameObject(Path.GetFileNameWithoutExtension(path)); SceneManager.MoveGameObjectToScene(root, preview);
        if (moving) root.AddComponent<MovingCar>(); else root.AddComponent<StationaryCar>();
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, preview); instance.transform.SetParent(root.transform, false);
        int count = moving ? 1 : 2;
        for (int i = 0; i < count; i++)
        {
            var signal = new GameObject(moving ? "Turn indicator" : "Breakdown hazard " + i, typeof(Light));
            signal.transform.SetParent(root.transform, false); signal.transform.localPosition = new Vector3(i == 0 ? -.8f : .8f, .65f, -2.2f);
            var light = signal.GetComponent<Light>(); light.type = LightType.Point; light.color = new Color(1, .48f, .1f); light.range = 3; light.intensity = 2; light.enabled = false;
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root); return prefab;
    }
}
