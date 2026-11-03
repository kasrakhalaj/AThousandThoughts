using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AfterHours;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AfterHoursBuilder
{
    const string Generated = "Assets/Game/Generated";
    const string ScenePath = "Assets/Game/Scenes/AfterHours.unity";
    static readonly Dictionary<Material, Material> Converted = new Dictionary<Material, Material>();
    static int materialIndex;
    static Material asphalt, verge, marking, metal, amber, red, glass, gold, green, particle;

    [MenuItem("After Hours/Create initial game scene")]
    public static void CreateGame()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("The game scene already exists. Existing authored work is protected.");
        if (UniversalRenderPipeline.asset == null) throw new InvalidOperationException("URP must be active.");
        Directory.CreateDirectory(Generated); Directory.CreateDirectory("Assets/Game/Scenes");
        AssetDatabase.Refresh();
        ConfigureMaterials();
        var catalog = ScriptableObject.CreateInstance<GameCatalog>();
        string[] names = { "Beatall", "docLorean", "Landyroamer", "Toyoyo Highlight", "Tristar Racer" };
        catalog.cars = names.Select(n => Normalize("Assets/Art/Vehicles/" + n + ".fbx", "Car-" + n, 4.45f, true)).ToArray();
        catalog.carNames = new[] { "BEATALL", "DOCLOREAN", "LANDYROAMER", "TOYOYO HIGHLIGHT", "TRISTAR RACER" };
        catalog.trees = Models("nature-kit", new[] { "tree_pineDefaultA", "tree_pineDefaultB", "tree_oak", "tree_pineTallA" }, 8);
        catalog.rocks = Models("nature-kit", new[] { "rock_largeA", "rock_largeB", "rock_largeC" }, 1.8f);
        catalog.houses = Models("city-kit-suburban", new[] { "building-type-a", "building-type-c", "building-type-f", "building-type-j" }, 7);
        catalog.buildings = Models("city-kit-commercial", new[] { "building-a", "building-c", "building-f", "skyscraper-a" }, 14);
        catalog.roadProps = Models("city-kit-roads", new[] { "road-sign-warning", "road-sign-street", "construction-barrier", "electricity-pole" }, 2.5f);
        catalog.roadSegment = CreateRoad();
        catalog.stationaryCar = CreateTraffic(catalog.cars[2], false);
        catalog.movingCar = CreateTraffic(catalog.cars[0], true);
        catalog.coin = CreatePickup(false); catalog.fuel = CreatePickup(true);
        catalog.particleMaterial = particle;
        catalog.engineSound = Clip("engine"); catalog.ignitionSound = Clip("ignition");
        catalog.collisionSound = Clip("collision"); catalog.coinSound = Clip("coin");
        catalog.fuelSound = Clip("fuel"); catalog.rainSound = Clip("rain");
        catalog.controls = CreateControls();
        AssetDatabase.CreateAsset(catalog, Generated + "/GameCatalog.asset");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>(); camera.nearClipPlane = .15f; camera.farClipPlane = 580;
        camera.backgroundColor = new Color(.035f, .06f, .09f); camera.allowHDR = true;
        cameraObject.transform.position = new Vector3(7, 3.6f, -7.8f);
        cameraObject.transform.LookAt(new Vector3(0, .8f, 0));
        var data = cameraObject.GetComponent<UniversalAdditionalCameraData>(); data.renderPostProcessing = true; data.volumeLayerMask = 1;
        data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        UniversalRenderPipeline.asset.supportsHDR = true;
        var moon = new GameObject("Moonlight", typeof(Light)); var light = moon.GetComponent<Light>();
        light.type = LightType.Directional; light.color = new Color(.6f, .72f, .89f); light.intensity = .65f;
        light.shadows = LightShadows.Soft; moon.transform.rotation = Quaternion.Euler(36, -28, 0);
        RenderSettings.sun = light; RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.24f, .3f, .36f); RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .0038f;
        RenderSettings.fogColor = new Color(.065f, .09f, .115f);
        var skyShader = Shader.Find("AfterHours/NightSky");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            AssetDatabase.CreateAsset(sky, Generated + "/NightSky.mat"); RenderSettings.skybox = sky;
        }
        ConfigureVolume();
        var root = new GameObject("After Hours").AddComponent<GameBootstrap>(); root.Configure(catalog, camera);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.companyName = "Kasra Khalaj"; PlayerSettings.productName = "After Hours";
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        var serialized = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        serialized.FindProperty("activeInputHandler").intValue = 1; serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (string path in Directory.GetFiles("Assets/Game/Audio", "*.wav"))
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path); var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .75f;
            importer.defaultSampleSettings = settings; importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("AFTER_HOURS_CREATED: " + ScenePath);
    }
    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/" + name + ".wav");
    static GameObject[] Models(string pack, string[] names, float height)
    {
        return names.Select(n => "Assets/Art/Kenney/" + pack + "/Models/FBX format/" + n + ".fbx")
            .Where(File.Exists).Select(p => Normalize(p, pack + "-" + Path.GetFileNameWithoutExtension(p), height, false)).ToArray();
    }
    static Bounds Bounds(GameObject go)
    {
        var filters = go.GetComponentsInChildren<MeshFilter>();
        if (filters.Length == 0) throw new InvalidOperationException("Model has no meshes: " + go.name);
        var bounds = new Bounds(); bool initialized = false;
        foreach (var filter in filters)
        {
            var local = filter.sharedMesh.bounds;
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                var corner = filter.transform.TransformPoint(local.center + Vector3.Scale(local.extents, new Vector3(x, y, z)));
                if (!initialized) { bounds = new Bounds(corner, Vector3.zero); initialized = true; } else bounds.Encapsulate(corner);
            }
        }
        return bounds;
    }
    static GameObject Normalize(string source, string name, float size, bool car)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(source);
        if (model == null) throw new InvalidOperationException("Missing model: " + source);
        var root = new GameObject(name); var visual = UnityEngine.Object.Instantiate(model, root.transform);
        var b = Bounds(visual);
        if (car)
        {
            if (b.size.y > b.size.x && b.size.y > b.size.z) visual.transform.Rotate(90, 0, 0, Space.World);
            else if (b.size.x > b.size.z) visual.transform.Rotate(0, 90, 0, Space.World);
            visual.transform.Rotate(0, 180, 0, Space.World);
            b = Bounds(visual);
        }
        float scale = size / Mathf.Max(.001f, car ? b.size.z : b.size.y);
        visual.transform.localScale *= scale; b = Bounds(visual);
        visual.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (var collider in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(ConvertMaterial).ToArray();
            renderer.shadowCastingMode = car ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
        if (car)
        {
            for (int i = 0; i < 2; i++) Cube("Tail lamp", root.transform, new Vector3(i == 0 ? -.73f : .73f, .6f, -1.95f), new Vector3(.17f, .08f, .04f), red);
        }
        return SavePrefab(root, name);
    }
    static Material ConvertMaterial(Material original)
    {
        if (original == null) return metal;
        if (Converted.TryGetValue(original, out var converted)) return converted;
        converted = new Material(Shader.Find("Universal Render Pipeline/Lit")); converted.name = original.name;
        converted.color = original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white;
        if (original.mainTexture != null) converted.mainTexture = original.mainTexture;
        converted.SetFloat("_Smoothness", .22f); converted.EnableKeyword("_EMISSION");
        converted.SetColor("_EmissionColor", Color.black);
        AssetDatabase.CreateAsset(converted, Generated + "/ImportedMaterial-" + materialIndex++ + ".mat");
        Converted.Add(original, converted); return converted;
    }
    static void ConfigureMaterials()
    {
        asphalt = Mat("Wet asphalt", new Color(.075f, .09f, .1f), .72f);
        verge = Mat("Grass verge", new Color(.12f, .19f, .15f));
        marking = Mat("Road paint", new Color(.78f, .77f, .64f)); metal = Mat("Road steel", new Color(.25f, .3f, .31f));
        amber = Mat("Amber light", new Color(.9f, .55f, .17f), .3f, 2);
        red = Mat("Tail lights", new Color(.65f, .035f, .02f), .3f, 3);
        gold = Mat("Coin gold", new Color(.85f, .57f, .14f), .65f, .4f);
        green = Mat("Fuel green", new Color(.16f, .47f, .31f), .3f, .15f);
        glass = Mat("Window dark", new Color(.08f, .15f, .19f), .65f);
        particle = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        particle.SetFloat("_Surface", 1); particle.SetFloat("_Blend", 0); particle.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        particle.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); particle.SetFloat("_ZWrite", 0);
        particle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); particle.renderQueue = 3000;
        AssetDatabase.CreateAsset(particle, Generated + "/Particles.mat");
    }
    static Material Mat(string name, Color color, float smoothness = .1f, float emission = 0)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) throw new InvalidOperationException("URP Lit unavailable");
        var mat = new Material(shader); mat.color = color; mat.SetFloat("_Smoothness", smoothness);
        mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * emission);
        AssetDatabase.CreateAsset(mat, Generated + "/" + name + ".mat"); return mat;
    }
    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material mat, PrimitiveType primitive = PrimitiveType.Cube)
    {
        var go = GameObject.CreatePrimitive(primitive); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = mat; return go;
    }
    static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Generated + "/" + name + ".prefab"); UnityEngine.Object.DestroyImmediate(go); return prefab;
    }
    static GameObject CreateRoad()
    {
        var go = new GameObject("Road segment");
        Cube("Ground", go.transform, new Vector3(0, -.24f, 30), new Vector3(200, .25f, 60), verge);
        Cube("Shoulder", go.transform, new Vector3(0, -.12f, 30), new Vector3(15, .2f, 60), metal);
        Cube("Three lane surface", go.transform, new Vector3(0, -.03f, 30), new Vector3(11.7f, .06f, 60), asphalt);
        foreach (float x in new[] { -1.8f, 1.8f }) for (int i = 0; i < 10; i++) Cube("Lane dash", go.transform, new Vector3(x, .008f, i * 6 + 2), new Vector3(.13f, .015f, 3), marking);
        foreach (float side in new[] { -1f, 1f })
        {
            Cube("Edge line", go.transform, new Vector3(side * 5.5f, .008f, 30), new Vector3(.15f, .015f, 60), marking);
            Cube("Guardrail", go.transform, new Vector3(side * 7f, .7f, 30), new Vector3(.12f, .3f, 60), metal);
            for (int i = 0; i < 10; i++)
            {
                Cube("Rail post", go.transform, new Vector3(side * 7f, .45f, i * 6), new Vector3(.13f, .9f, .13f), metal);
                Cube("Reflector", go.transform, new Vector3(side * 6.92f, .85f, i * 6), new Vector3(.06f, .1f, .16f), amber);
            }
            var lamp = new GameObject("Warm streetlight"); lamp.transform.SetParent(go.transform, false); lamp.transform.localPosition = new Vector3(side * 8, 0, side > 0 ? 12 : 42);
            Cube("Pole", lamp.transform, new Vector3(0, 3.5f, 0), new Vector3(.16f, 7, .16f), metal);
            Cube("Arm", lamp.transform, new Vector3(-side, 7, 0), new Vector3(2.1f, .12f, .12f), metal);
            Cube("Lamp housing", lamp.transform, new Vector3(-side * 2, 6.9f, 0), new Vector3(.65f, .15f, 1.2f), amber);
            var lightGo = new GameObject("Road light", typeof(Light)); lightGo.transform.SetParent(lamp.transform, false);
            lightGo.transform.localPosition = new Vector3(-side * 2, 6.75f, 0); lightGo.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var light = lightGo.GetComponent<Light>(); light.type = LightType.Spot; light.intensity = 13; light.range = 21; light.spotAngle = 110; light.innerSpotAngle = 60; light.color = new Color(1, .77f, .48f);
        }
        return SavePrefab(go, "RoadSegment");
    }
    static GameObject CreateTraffic(GameObject model, bool moving)
    {
        var go = new GameObject(moving ? "Moving traffic" : "Stationary traffic"); UnityEngine.Object.Instantiate(model, go.transform);
        if (moving)
        {
            go.AddComponent<MovingCar>(); var lamp = new GameObject("Turn indicator", typeof(Light)); lamp.transform.SetParent(go.transform, false);
            var light = lamp.GetComponent<Light>(); light.type = LightType.Point; light.color = new Color(1, .45f, .05f); light.range = 2.5f; light.intensity = 3; light.enabled = false;
        }
        else go.AddComponent<StationaryCar>();
        return SavePrefab(go, moving ? "MovingCar" : "StationaryCar");
    }
    static GameObject CreatePickup(bool fuel)
    {
        var go = new GameObject(fuel ? "Fuel can" : "Coin");
        if (fuel)
        {
            go.AddComponent<FuelPickup>(); Cube("Can", go.transform, Vector3.zero, new Vector3(.75f, .95f, .45f), green);
            Cube("Handle", go.transform, new Vector3(0, .57f, 0), new Vector3(.4f, .18f, .18f), metal);
            Cube("Label", go.transform, new Vector3(0, 0, -.235f), new Vector3(.34f, .4f, .025f), marking);
        }
        else
        {
            go.AddComponent<CoinPickup>(); var coin = Cube("Coin", go.transform, Vector3.zero, new Vector3(.7f, .09f, .7f), gold, PrimitiveType.Cylinder); coin.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        return SavePrefab(go, fuel ? "FuelCan" : "Coin");
    }
    static InputActionAsset CreateControls()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>(); var drive = asset.AddActionMap("Driving");
        Bind(drive, "Left", "<Keyboard>/a", "<Keyboard>/leftArrow", "<Gamepad>/dpad/left");
        Bind(drive, "Right", "<Keyboard>/d", "<Keyboard>/rightArrow", "<Gamepad>/dpad/right");
        Bind(drive, "Brake", "<Keyboard>/s", "<Keyboard>/downArrow", "<Gamepad>/leftTrigger");
        Bind(drive, "Forward", "<Keyboard>/w", "<Keyboard>/upArrow", "<Gamepad>/rightTrigger");
        Bind(drive, "Reverse", "<Keyboard>/r", "<Gamepad>/buttonWest");
        var system = asset.AddActionMap("System");
        Bind(system, "Pause", "<Keyboard>/escape", "<Gamepad>/start");
        Bind(system, "StartCar", "<Keyboard>/enter");
        Bind(system, "NextSong", "<Keyboard>/n"); Bind(system, "PreviousSong", "<Keyboard>/b"); Bind(system, "ToggleMusic", "<Keyboard>/m");
        string path = Generated + "/AfterHours.inputactions"; File.WriteAllText(path, asset.ToJson());
        AssetDatabase.ImportAsset(path); UnityEngine.Object.DestroyImmediate(asset); return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
    }
    static void Bind(InputActionMap map, string name, params string[] paths) { var action = map.AddAction(name, InputActionType.Button); foreach (string path in paths) action.AddBinding(path); }
    static void ConfigureVolume()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>(true); bloom.intensity.value = .38f; bloom.threshold.value = 1; bloom.scatter.value = .55f;
        var tone = profile.Add<Tonemapping>(true); tone.mode.value = TonemappingMode.ACES;
        var vignette = profile.Add<Vignette>(true); vignette.intensity.value = .2f; vignette.smoothness.value = .5f;
        AssetDatabase.CreateAsset(profile, Generated + "/NightVolume.asset");
        foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
        var volume = new GameObject("Night atmosphere", typeof(Volume)).GetComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = profile;
    }
    [MenuItem("After Hours/Build Windows player")]
    public static void BuildWindows()
    {
        var folder = Path.GetFullPath("../Build/AfterHours"); Directory.CreateDirectory(folder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = Path.Combine(folder, "AfterHours.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
        var source = Path.GetFullPath("../Music");
        foreach (string file in Directory.GetFiles(source, "*.mp3", SearchOption.AllDirectories))
        {
            string output = Path.Combine(folder, "Music", Path.GetRelativePath(source, file)); Directory.CreateDirectory(Path.GetDirectoryName(output)); File.Copy(file, output, true);
        }
        Debug.Log("AFTER_HOURS_BUILD: " + folder);
    }

    public static void RepairCarModels()
    {
        metal = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Road steel.mat");
        red = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Tail lights.mat");
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(Generated + "/GameCatalog.asset");
        string[] names = { "Beatall", "docLorean", "Landyroamer", "Toyoyo Highlight", "Tristar Racer" };
        for (int i = 0; i < names.Length; i++)
        {
            string path = AssetDatabase.GetAssetPath(catalog.cars[i]);
            var root = PrefabUtility.LoadPrefabContents(path);
            var visual = root.transform.GetChild(0);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Vehicles/" + names[i] + ".fbx");
            visual.localPosition = source.transform.localPosition; visual.localRotation = source.transform.localRotation; visual.localScale = source.transform.localScale;
            var b = Bounds(visual.gameObject);
            if (b.size.y > b.size.x && b.size.y > b.size.z) visual.Rotate(90, 0, 0, Space.World);
            else if (b.size.x > b.size.z) visual.Rotate(0, 90, 0, Space.World);
            visual.Rotate(0, 180, 0, Space.World);
            b = Bounds(visual.gameObject); visual.localScale *= 4.45f / b.size.z;
            b = Bounds(visual.gameObject); visual.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            for (int child = 1; child < root.transform.childCount; child++)
            {
                var lamp = root.transform.GetChild(child);
                if (lamp.name != "Tail lamp") continue;
                lamp.localPosition = new Vector3(lamp.localPosition.x, .6f, -1.95f);
                lamp.localScale = new Vector3(.17f, .08f, .04f);
            }
            Debug.Log(names[i] + " corrected bounds: " + Bounds(visual.gameObject).size);
            PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
        }
        CreateTraffic(catalog.cars[2], false); CreateTraffic(catalog.cars[0], true); AssetDatabase.SaveAssets();
    }
}
