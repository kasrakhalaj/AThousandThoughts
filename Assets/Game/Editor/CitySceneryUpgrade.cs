using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AfterHours;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CitySceneryUpgrade
{
    const string Folder = "Assets/Game/Generated/City";
    const string Commercial = "city-kit-commercial", Suburban = "city-kit-suburban", Roads = "city-kit-roads";
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static readonly Dictionary<string, Texture2D> palettes = new Dictionary<string, Texture2D>();
    static readonly List<Mesh> temporaryMeshes = new List<Mesh>();
    static readonly List<object> report = new List<object>();
    static Scene preview;
    static Material pavement, curb, tarmac, paint, soil, steel, wood, cream, rust, glass, window, windowIvory, water;
    static int litWindowGroups;

    [MenuItem("A Thousand Thoughts/Apply district city scenery")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        materials.Clear(); palettes.Clear(); temporaryMeshes.Clear(); report.Clear(); litWindowGroups = 0;
        preview = EditorSceneManager.NewPreviewScene();
        try
        {
            pavement = Mat("Pavement", new Color(.32f, .34f, .33f), .25f);
            curb = Mat("Curb", new Color(.55f, .55f, .49f), .15f);
            tarmac = Mat("Side street asphalt", new Color(.055f, .068f, .075f), .78f);
            paint = Mat("Street paint", new Color(.71f, .68f, .53f), .2f);
            soil = Mat("Park green", new Color(.105f, .19f, .135f), .05f);
            steel = Mat("Street steel", new Color(.105f, .14f, .155f), .5f);
            wood = Mat("Bench timber", new Color(.30f, .19f, .12f), .12f);
            cream = Mat("Station cream", new Color(.69f, .65f, .51f), .35f);
            rust = Mat("Muted rust", new Color(.37f, .15f, .09f), .22f);
            glass = Mat("Dark window", new Color(.045f, .085f, .10f), .82f);
            window = Mat("Occupied amber windows", new Color(.82f, .58f, .29f), .35f, 1.5f);
            windowIvory = Mat("Occupied ivory windows", new Color(.78f, .77f, .64f), .35f, 1.25f);
            water = Mat("Still rainwater", new Color(.035f, .055f, .065f), .96f);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Game/Generated/GameCatalog.asset");
            var blocks = new GameObject[3][];
            for (int district = 0; district < 3; district++)
            {
                blocks[district] = new GameObject[3];
                for (int variant = 0; variant < 3; variant++)
                {
                    var root = Root("Block-" + (RoadDistrict)district + "-" + variant);
                    BuildDistrict(root.transform, (RoadDistrict)district, 0, 60, variant == 1, variant == 2, district * 19 + variant * 7);
                    blocks[district][variant] = Bake(root);
                }
            }
            var woods = Root("Block-Woodland"); PowerLine(woods.transform, 0, 60); var woodland = Bake(woods);
            var transitions = new GameObject[4]; var entrances = new GameObject[4];
            for (int d = 0; d < 4; d++)
            {
                var root = Root("Transition-" + (RoadDistrict)d);
                BuildDistrict(root.transform, (RoadDistrict)d, 0, 30, false, false, 71 + d);
                BuildDistrict(root.transform, (RoadDistrict)((d + 1) % 4), 30, 60, false, false, 81 + d);
                transitions[d] = Bake(root);
                root = Root("Entrance-" + (RoadDistrict)d);
                Sign(root.transform, new Vector3(9.2f, 2.9f, 3), CityRoute.DistrictName((RoadDistrict)d), 4.4f, .9f);
                entrances[d] = Bake(root);
            }
            var landmarks = new[] { Cafe(catalog), Park(), ServiceStation(catalog), Overpass() };
            Undo.RecordObject(catalog, "Apply district city scenery");
            catalog.downtownBlocks = blocks[0]; catalog.residentialBlocks = blocks[1]; catalog.industrialBlocks = blocks[2];
            catalog.woodlandBlock = woodland; catalog.districtTransitions = transitions; catalog.districtEntrances = entrances; catalog.cityLandmarks = landmarks;
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
            // Save only our generated assets; TMP may upgrade a vendor font in memory while baking signs.
            foreach (string guid in AssetDatabase.FindAssets("", new[] { Folder })) AssetDatabase.SaveAssetIfDirty(new GUID(guid));
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/city-authoring.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { litWindowGroups, prefabs = report }, Newtonsoft.Json.Formatting.Indented));
            Debug.Log("DISTRICT_CITY_SCENERY_APPLIED: " + report.Count + " combined prefabs, " + litWindowGroups + " lit authored window groups");
        }
        finally
        {
            foreach (var mesh in temporaryMeshes) if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            foreach (var texture in palettes.Values) UnityEngine.Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    static GameObject Root(string name)
    {
        var root = new GameObject(name); SceneManager.MoveGameObjectToScene(root, preview); return root;
    }
    static Material Mat(string name, Color color, float smoothness, float emission = 0)
    {
        string path = Folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", smoothness); mat.SetColor("_EmissionColor", color * emission);
        if (emission > 0) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
        mat.globalIlluminationFlags = emission > 0 ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.None;
        MaterialEditor.FixupEmissiveFlag(mat);
        mat.enableInstancing = true; EditorUtility.SetDirty(mat); return mat;
    }
    static Material Surface(Material source)
    {
        if (source == null) return steel;
        if (AssetDatabase.GetAssetPath(source).StartsWith(Folder, StringComparison.Ordinal) || source.shader.name.StartsWith("TextMeshPro", StringComparison.Ordinal)) return source;
        Texture texture = source.mainTexture;
        Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
        string guid = texture == null ? "plain" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture));
        string key = "Palette-" + guid + "-" + ColorUtility.ToHtmlStringRGBA(color);
        if (materials.TryGetValue(key, out var existing)) return existing;
        var mat = Mat(key, color, .28f); mat.SetTexture("_BaseMap", texture); materials[key] = mat; return mat;
    }
    static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, PrimitiveType type = PrimitiveType.Cube)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<MeshRenderer>().sharedMaterial = material; return go;
    }
    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<MeshRenderer>();
        var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
    }
    static GameObject Model(Transform parent, string pack, string name, Vector3 position, Vector3 fit, float yaw, int lightingSeed = -1)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Kenney/" + pack + "/Models/FBX format/" + name + ".fbx");
        if (source == null) throw new InvalidOperationException("Missing city model: " + pack + "/" + name);
        var model = UnityEngine.Object.Instantiate(source, parent); model.name = name;
        model.transform.localRotation = Quaternion.Euler(0, yaw, 0) * model.transform.localRotation;
        var bounds = BoundsOf(model); float scale = Mathf.Min(fit.x / bounds.size.x, fit.y / bounds.size.y, fit.z / bounds.size.z);
        model.transform.localScale *= scale; bounds = BoundsOf(model);
        model.transform.position += parent.TransformPoint(position) - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var collider in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        if (lightingSeed >= 0) LightWindows(model, lightingSeed);
        return model;
    }
    static void BuildDistrict(Transform root, RoadDistrict district, float start, float end, bool sideStreet, bool clearRight, int seed)
    {
        if (district == RoadDistrict.Woodland) { PowerLine(root, start, end); return; }
        float length = end - start, center = (start + end) * .5f;
        for (int side = -1; side <= 1; side += 2)
        {
            if (sideStreet)
            {
                Sidewalk(root, side, start, center - 7); Sidewalk(root, side, center + 7, end);
                Box("Side street", root, new Vector3(side * 29, -.003f, center), new Vector3(46, .025f, 13), tarmac);
                for (int z = -5; z <= 5; z += 2) Box("Side street crosswalk", root, new Vector3(side * 10, .014f, center + z), new Vector3(3.2f, .012f, .65f), paint);
                for (int x = 18; x < 50; x += 6) Box("Side street center dash", root, new Vector3(side * x, .016f, center), new Vector3(2.7f, .012f, .12f), paint);
                Model(root, Roads, "traffic-light", new Vector3(side * 8.5f, 0, center - 8), new Vector3(2, 4.2f, 2), side < 0 ? 90 : -90);
                if (side > 0) Sign(root, new Vector3(9, 2.9f, center - 9), district == RoadDistrict.Downtown ? "MARKET STREET" : district == RoadDistrict.Residential ? "LINDEN AVENUE" : "DEPOT ROAD", 3.5f, .6f);
            }
            else Sidewalk(root, side, start, end);
            if (clearRight && side > 0) continue;
            int slots = length > 40 ? district == RoadDistrict.Industrial ? 2 : 3 : 2;
            for (int slot = 0; slot < slots; slot++)
            {
                float z = start + (slot + .5f) * length / slots;
                if (sideStreet && Mathf.Abs(z - center) < 12) continue;
                float frontage = Mathf.Min(17, length / slots - 2);
                if (district == RoadDistrict.Downtown)
                {
                    char type = (char)('a' + (seed + slot * 3 + (side > 0 ? 5 : 0)) % 14);
                    var building = Model(root, Commercial, "building-" + type, new Vector3(side * 18, 0, z), new Vector3(14, 20, frontage), side < 0 ? 90 : -90, seed + slot * 13);
                    // Front doors face the pavement; shop awnings use the authored Kenney details.
                    var facade = BoundsOf(building);
                    float front = side < 0 ? facade.max.x : facade.min.x;
                    Model(root, Commercial, slot % 2 == 0 ? "detail-awning-wide" : "detail-overhang-wide", new Vector3(front - side * .5f, Mathf.Min(3, facade.size.y * .25f), z), new Vector3(1.7f, .7f, frontage * .65f), side < 0 ? 90 : -90);
                }
                else if (district == RoadDistrict.Residential)
                {
                    char type = (char)('a' + (seed + slot * 5 + (side > 0 ? 3 : 0)) % 21);
                    Model(root, Suburban, "building-type-" + type, new Vector3(side * 19, 0, z), new Vector3(12, 9.5f, frontage - 2), side < 0 ? 90 : -90, seed + slot * 17);
                    Box("Front garden", root, new Vector3(side * 13, -.005f, z), new Vector3(5, .025f, frontage), soil);
                    Box("Garden path", root, new Vector3(side * 13.3f, .014f, z), new Vector3(6.2f, .02f, 1.1f), pavement);
                    Box("House driveway", root, new Vector3(side * 14.5f, .014f, z + frontage * .3f), new Vector3(9, .025f, 2.6f), tarmac);
                    Model(root, Suburban, "fence-low", new Vector3(side * 10.7f, 0, z - frontage * .35f), new Vector3(.3f, .9f, frontage * .45f), 90);
                }
                else
                {
                    Warehouse(root, new Vector3(side * 23, 0, z), side, Mathf.Min(24, length / slots - 3), seed + slot);
                    Model(root, Roads, "dumpster", new Vector3(side * 13.5f, 0, z + 5), new Vector3(2.5f, 1.6f, 2), 0);
                    Model(root, Roads, "construction-fence", new Vector3(side * 11, 0, z - 6), new Vector3(.4f, 2, 7), 90);
                }
            }
            if (district == RoadDistrict.Downtown)
            {
                char type = (char)('a' + (seed + (side > 0 ? 2 : 0)) % 5);
                Model(root, Commercial, "building-skyscraper-" + type, new Vector3(side * 40, 0, center + (sideStreet ? -16 : 0)), new Vector3(16, 38, sideStreet ? 16 : 24), side < 0 ? 90 : -90, seed + 101);
            }
            for (int prop = 0; prop < (length > 40 ? 3 : 1); prop++)
            {
                float z = start + 6 + prop * 20;
                if (sideStreet && Mathf.Abs(z - center) < 9) continue;
                if (district != RoadDistrict.Industrial) Model(root, Suburban, "planter", new Vector3(side * 8.9f, .12f, z), new Vector3(1.1f, .75f, 1.3f), 0);
                if (prop == 1 && district != RoadDistrict.Industrial) Bench(root, new Vector3(side * 9.1f, .12f, z + 4), side);
            }
        }
    }
    static void Sidewalk(Transform root, int side, float start, float end)
    {
        if (end <= start) return;
        Box("Sidewalk", root, new Vector3(side * 8.2f, .025f, (start + end) * .5f), new Vector3(3.5f, .18f, end - start), pavement);
        Box("Curb", root, new Vector3(side * 6.5f, .035f, (start + end) * .5f), new Vector3(.18f, .22f, end - start), curb);
        for (float z = start + 2; z < end; z += 4) Box("Pavement joint", root, new Vector3(side * 8.2f, .12f, z), new Vector3(3.4f, .008f, .035f), steel);
    }
    static void Warehouse(Transform root, Vector3 position, int side, float frontage, int seed, bool freight = true)
    {
        float front = position.x - side * 11;
        Box("Service warehouse", root, position + Vector3.up * 3.3f, new Vector3(22, 6.6f, frontage), seed % 2 == 0 ? cream : pavement);
        Box("Warehouse roof", root, position + Vector3.up * 6.7f, new Vector3(22.8f, .3f, frontage + .7f), steel);
        Box("Roller door", root, new Vector3(front - side * .04f, 2.2f, position.z), new Vector3(.06f, 4.4f, frontage * .45f), steel);
        for (int i = 1; i < 9; i++) Box("Door ribs", root, new Vector3(front - side * .08f, i * .48f, position.z), new Vector3(.04f, .035f, frontage * .45f), pavement);
        Box("Office window", root, new Vector3(front - side * .08f, 4.8f, position.z + frontage * .33f), new Vector3(.04f, .9f, 2), seed % 3 == 0 ? windowIvory : glass);
        if (freight) for (int c = 0; c < 2; c++) Box("Freight container", root, new Vector3(side * 40, 1.5f + c * 3, position.z), new Vector3(6, 3, 11), c == 0 ? rust : steel);
    }
    static void Bench(Transform root, Vector3 position, int side)
    {
        Box("Bench seat", root, position + Vector3.up * .5f, new Vector3(.65f, .12f, 2.1f), wood);
        Box("Bench back", root, position + new Vector3(side * .25f, .9f, 0), new Vector3(.1f, .6f, 2.1f), wood);
        foreach (float z in new[] { -.75f, .75f }) Box("Bench feet", root, position + new Vector3(0, .22f, z), new Vector3(.5f, .45f, .1f), steel);
    }
    static void PowerLine(Transform root, float start, float end)
    {
        float center = (start + end) * .5f;
        var pole = Model(root, Roads, "electricity-pole", new Vector3(-13, 0, center), new Vector3(9, 8, 4), 0);
        var bounds = BoundsOf(pole);
        foreach (float offset in new[] { -.36f, 0, .36f })
            Box("Overhead wire", root, new Vector3(bounds.center.x + bounds.size.x * offset, bounds.max.y - .15f, center), new Vector3(.035f, .035f, end - start), steel);
    }
    static void Sign(Transform root, Vector3 position, string words, float width, float height)
    {
        Box("Wayfinding pole", root, new Vector3(position.x, position.y * .5f, position.z), new Vector3(.12f, position.y, .12f), steel);
        Box("Wayfinding board", root, position, new Vector3(width, height, .14f), steel);
        foreach (int direction in new[] { 0, 180 })
        {
            var go = new GameObject("Sign lettering", typeof(TextMeshPro)); go.transform.SetParent(root, false);
            var text = go.GetComponent<TextMeshPro>(); text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            text.fontSharedMaterial = text.font.material; text.text = words; text.fontSize = 36; text.color = new Color(.88f, .84f, .68f);
            text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap; text.rectTransform.sizeDelta = new Vector2(100, 30);
            text.ForceMeshUpdate(true, true); var b = text.mesh.bounds;
            float scale = Mathf.Min(width * .88f / b.size.x, height * .72f / b.size.y);
            go.transform.localRotation = Quaternion.Euler(0, direction, 0); go.transform.localScale = Vector3.one * scale;
            go.transform.localPosition = position + new Vector3(0, 0, direction == 0 ? -.081f : .081f) - go.transform.localRotation * (b.center * scale);
        }
    }
    static void WarmLight(Transform root, Vector3 position, float range, float intensity)
    {
        var go = new GameObject("Warm landmark light", typeof(Light)); go.transform.SetParent(root, false); go.transform.localPosition = position;
        var light = go.GetComponent<Light>(); light.type = LightType.Point; light.color = new Color(1, .77f, .48f); light.range = range; light.intensity = intensity; light.shadows = LightShadows.None;
    }
    static GameObject Cafe(GameCatalog catalog)
    {
        var root = Root("Landmark-Night-Owl-Cafe");
        Box("Cafe forecourt", root.transform, new Vector3(22, -.005f, 30), new Vector3(24, .025f, 48), pavement);
        var cafe = Model(root.transform, Commercial, "building-i", new Vector3(23, 0, 34), new Vector3(15, 10, 18), -90, 732);
        float awningX = BoundsOf(cafe).min.x - 2.3f;
        Box("Cafe awning", root.transform, new Vector3(awningX, 3.1f, 34), new Vector3(5.5f, .18f, 15), cream);
        for (int stripe = 0; stripe < 8; stripe++) Box("Canvas stripe", root.transform, new Vector3(awningX, 3.2f, 27.4f + stripe * 1.8f), new Vector3(5.4f, .025f, .8f), rust);
        foreach (float z in new[] { 22f, 46f })
        {
            Model(root.transform, Commercial, "detail-parasol-a", new Vector3(12, .12f, z), new Vector3(3.6f, 3.1f, 3.6f), 0);
            Box("Cafe table", root.transform, new Vector3(12, .95f, z), new Vector3(1.3f, .08f, 1.3f), wood, PrimitiveType.Cylinder);
            Bench(root.transform, new Vector3(14.3f, .12f, z), 1);
        }
        Sign(root.transform, new Vector3(9.8f, 4.4f, 12), "NIGHT OWL\nCAFE", 3.4f, 1.8f);
        Box("Forecourt rainwater", root.transform, new Vector3(28, .024f, 23), new Vector3(3.3f, .008f, 2), water, PrimitiveType.Cylinder);
        WarmLight(root.transform, new Vector3(13.2f, 2.6f, 32), 10, 16);
        ParkedCar(root.transform, catalog.cars[0], new Vector3(29, .02f, 12), 115);
        return Bake(root);
    }
    static GameObject Park()
    {
        var root = Root("Landmark-Maple-Pocket-Park");
        Box("Park lawn", root.transform, new Vector3(25, -.005f, 30), new Vector3(29, .025f, 52), soil);
        Box("Park promenade", root.transform, new Vector3(13, .008f, 30), new Vector3(3, .035f, 52), pavement);
        Box("Park cross path", root.transform, new Vector3(25, .008f, 30), new Vector3(28, .035f, 2.4f), pavement);
        foreach (float z in new[] { 12f, 24f, 42f, 50f })
        {
            Model(root.transform, "nature-kit", "tree_oak", new Vector3(22 + z % 3 * 5, 0, z), new Vector3(8, 9, 8), z * 7);
            Bench(root.transform, new Vector3(15.5f, .05f, z), 1);
        }
        Model(root.transform, Roads, "light-square", new Vector3(12.5f, 0, 30), new Vector3(2, 5.4f, 2), 90);
        WarmLight(root.transform, new Vector3(12.5f, 4.8f, 30), 12, 12);
        Sign(root.transform, new Vector3(10.2f, 2.2f, 7), "MAPLE PARK", 3.2f, .7f);
        return Bake(root);
    }
    static GameObject ServiceStation(GameCatalog catalog)
    {
        var root = Root("Landmark-East-Works-Service");
        Box("Service forecourt", root.transform, new Vector3(26, -.005f, 30), new Vector3(31, .025f, 52), tarmac);
        Warehouse(root.transform, new Vector3(37, 0, 43), 1, 19, 7, false);
        Box("Service canopy", root.transform, new Vector3(20.5f, 4.8f, 20), new Vector3(18, .35f, 23), cream);
        Box("Canopy fascia", root.transform, new Vector3(11.5f, 4.7f, 20), new Vector3(.15f, .55f, 23), rust);
        foreach (float x in new[] { 13f, 28f }) foreach (float z in new[] { 11f, 29f }) Box("Canopy column", root.transform, new Vector3(x, 2.3f, z), new Vector3(.25f, 4.6f, .25f), steel);
        foreach (float x in new[] { 17f, 24f }) foreach (float z in new[] { 14f, 25f })
        {
            Box("Fuel pump", root.transform, new Vector3(x, .95f, z), new Vector3(.75f, 1.9f, .65f), cream);
            Box("Pump display", root.transform, new Vector3(x, 1.3f, z - .335f), new Vector3(.5f, .36f, .025f), glass);
            Box("Pump hose", root.transform, new Vector3(x + .43f, .9f, z), new Vector3(.08f, 1.5f, .08f), steel);
        }
        WarmLight(root.transform, new Vector3(19, 4.35f, 20), 14, 32);
        Box("Forecourt rainwater", root.transform, new Vector3(32, .024f, 15), new Vector3(4, .008f, 2.3f), water, PrimitiveType.Cylinder);
        Sign(root.transform, new Vector3(10.1f, 4.9f, 6), "FUEL\n24 HOURS", 3.2f, 2.2f);
        ParkedCar(root.transform, catalog.cars[2], new Vector3(18, .02f, 39), 65);
        return Bake(root);
    }
    static void ParkedCar(Transform root, GameObject prefab, Vector3 position, float yaw)
    {
        var car = UnityEngine.Object.Instantiate(prefab, root); car.name = "Parked car"; car.transform.localPosition = position; car.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        foreach (var lamp in car.GetComponentsInChildren<VehicleLights>()) lamp.gameObject.SetActive(false);
    }
    static GameObject Overpass()
    {
        var root = Root("Landmark-Cross-City-Overpass");
        Box("Bridge deck", root.transform, new Vector3(0, 8.1f, 30), new Vector3(90, .8f, 12), pavement);
        Box("Overpass road", root.transform, new Vector3(0, 8.52f, 30), new Vector3(90, .04f, 11), tarmac);
        foreach (int side in new[] { -1, 1 })
        {
            Model(root.transform, Roads, "bridge-pillar-wide", new Vector3(side * 10.4f, 0, 30), new Vector3(3.8f, 7.8f, 7), 0);
            Model(root.transform, Roads, "bridge-pillar-wide", new Vector3(side * 35, 0, 30), new Vector3(3.8f, 7.8f, 7), 0);
            Box("Structural pier", root.transform, new Vector3(side * 10.4f, 3.85f, 30), new Vector3(1.8f, 7.7f, 5.5f), pavement);
            Box("Structural pier", root.transform, new Vector3(side * 35, 3.85f, 30), new Vector3(1.8f, 7.7f, 5.5f), pavement);
            Box("Bridge parapet", root.transform, new Vector3(0, 9, 30 + side * 5.8f), new Vector3(90, 1, .25f), steel);
            var slope = Quaternion.Euler(0, 0, -side * Mathf.Atan2(8, 50) * Mathf.Rad2Deg);
            var center = new Vector3(side * 70, 4.1f, 30);
            Box("Approach ramp", root.transform, center, new Vector3(Mathf.Sqrt(2564), .8f, 12), pavement).transform.localRotation = slope;
            Box("Ramp road", root.transform, center + slope * Vector3.up * .425f, new Vector3(Mathf.Sqrt(2564), .04f, 11), tarmac).transform.localRotation = slope;
            foreach (float edge in new[] { -5.8f, 5.8f }) Box("Ramp parapet", root.transform, center + new Vector3(0, .9f, edge), new Vector3(Mathf.Sqrt(2564), 1, .25f), steel).transform.localRotation = slope;
            Box("Ramp support", root.transform, new Vector3(side * 60, 2.65f, 30), new Vector3(1.8f, 5.3f, 5.5f), pavement);
            Box("Ramp support", root.transform, new Vector3(side * 80, 1.05f, 30), new Vector3(1.8f, 2.1f, 5.5f), pavement);
        }
        for (int x = -42; x <= 42; x += 6) Box("Overpass lane marking", root.transform, new Vector3(x, 8.55f, 30), new Vector3(3, .015f, .13f), paint);
        Sign(root.transform, new Vector3(9.4f, 3, 14), "RING ROAD", 3, .65f);
        return Bake(root);
    }

    static void LightWindows(GameObject building, int seed)
    {
        var random = new System.Random(seed); var filters = building.GetComponentsInChildren<MeshFilter>();
        foreach (var filter in filters)
        {
            var mesh = filter.sharedMesh; var vertices = mesh.vertices; var uv = mesh.uv; var slots = filter.GetComponent<MeshRenderer>().sharedMaterials;
            var candidates = new List<Vector3[]>(); var parent = new List<int>(); var corners = new Dictionary<Vector3Int, int>();
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var texture = slots[Mathf.Min(sub, slots.Length - 1)].mainTexture; if (texture == null) continue;
                string path = AssetDatabase.GetAssetPath(texture);
                if (!palettes.TryGetValue(path, out var palette)) { palette = new Texture2D(2, 2); palette.LoadImage(File.ReadAllBytes(path)); palettes[path] = palette; }
                int[] indices = mesh.GetTriangles(sub);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector2 tex = (uv[indices[i]] + uv[indices[i + 1]] + uv[indices[i + 2]]) / 3;
                    Color color = palette.GetPixelBilinear(tex.x, tex.y);
                    // Kenney's panes use the blue palette; the dark gray strip is the frame and sill.
                    if (color.b < .55f || color.b < color.r + .20f || color.b < color.g + .10f) continue;
                    Vector3[] points = { filter.transform.TransformPoint(vertices[indices[i]]), filter.transform.TransformPoint(vertices[indices[i + 1]]), filter.transform.TransformPoint(vertices[indices[i + 2]]) };
                    Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
                    if (Mathf.Abs(normal.y) > .3f || (points[0].y + points[1].y + points[2].y) / 3 < 1.2f) continue;
                    int id = candidates.Count; candidates.Add(points); parent.Add(id);
                    foreach (var point in points)
                    {
                        var key = Vector3Int.RoundToInt(point * 1000);
                        if (corners.TryGetValue(key, out int neighbor)) parent[Find(parent, id)] = Find(parent, neighbor); else corners[key] = id;
                    }
                }
            }
            var groups = new Dictionary<int, List<Vector3[]>>();
            for (int i = 0; i < candidates.Count; i++) { int id = Find(parent, i); if (!groups.ContainsKey(id)) groups[id] = new List<Vector3[]>(); groups[id].Add(candidates[i]); }
            foreach (var group in groups.Values)
            {
                var bounds = new Bounds(group[0][0], Vector3.zero); foreach (var triangle in group) foreach (var point in triangle) bounds.Encapsulate(point);
                if (bounds.size.y > 4 || Mathf.Max(bounds.size.x, bounds.size.z) > 3.8f || random.NextDouble() > .32) continue;
                var points = new List<Vector3>();
                foreach (var triangle in group)
                {
                    Vector3 normal = Vector3.Cross(triangle[1] - triangle[0], triangle[2] - triangle[0]).normalized;
                    foreach (var point in triangle) points.Add(building.transform.InverseTransformPoint(point + normal * .002f));
                }
                var lit = new Mesh { name = "Authored window faces" }; lit.SetVertices(points); lit.SetTriangles(Enumerable.Range(0, points.Count).ToArray(), 0); lit.RecalculateNormals(); lit.RecalculateBounds(); temporaryMeshes.Add(lit);
                var go = new GameObject("Occupied authored window", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(building.transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = lit; go.GetComponent<MeshRenderer>().sharedMaterial = random.NextDouble() < .7 ? window : windowIvory; litWindowGroups++;
            }
        }
    }
    static int Find(List<int> parent, int id) { while (parent[id] != id) { parent[id] = parent[parent[id]]; id = parent[id]; } return id; }
    static GameObject Bake(GameObject raw)
    {
        var groups = new Dictionary<Material, List<CombineInstance>>(); int sourceRenderers = 0;
        foreach (var filter in raw.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = filter.GetComponent<MeshRenderer>(); if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            sourceRenderers++;
            var b = renderer.bounds;
            if (b.size.y > .3f && b.min.y < 6.5f && b.max.y > .3f && b.min.x < 6.1f && b.max.x > -6.1f)
                throw new InvalidOperationException("City scenery enters driving/camera corridor: " + raw.name + "/" + filter.name + " " + b);
            var slots = renderer.sharedMaterials;
            for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
            {
                Material mat = Surface(slots[Mathf.Min(sub, slots.Length - 1)]);
                if (!groups.ContainsKey(mat)) groups[mat] = new List<CombineInstance>();
                groups[mat].Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = sub, transform = raw.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
            }
        }
        var root = Root(raw.name);
        foreach (var group in groups)
        {
            var mesh = new Mesh { name = raw.name + " " + group.Key.name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(group.Value.ToArray(), true, true); mesh.RecalculateBounds();
            string path = Folder + "/" + raw.name + "--" + group.Key.name.Replace('/', '-') + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(existing); }
            var go = new GameObject(group.Key.name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root.transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = group.Key;
            renderer.shadowCastingMode = group.Key == window || group.Key == windowIvory || group.Key.shader.name.StartsWith("TextMeshPro", StringComparison.Ordinal) ? ShadowCastingMode.Off : ShadowCastingMode.On;
        }
        foreach (var light in raw.GetComponentsInChildren<Light>())
        {
            if (!light.gameObject.activeInHierarchy) continue;
            var go = new GameObject("Warm landmark light", typeof(Light)); go.transform.SetParent(root.transform, false); go.transform.localPosition = raw.transform.InverseTransformPoint(light.transform.position);
            var copy = go.GetComponent<Light>(); copy.type = light.type; copy.color = light.color; copy.range = light.range; copy.intensity = light.intensity; copy.shadows = LightShadows.None;
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + raw.name + ".prefab");
        report.Add(new { name = raw.name, sourceRenderers, combinedRenderers = groups.Count, lights = root.GetComponentsInChildren<Light>().Length });
        UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(raw); return prefab;
    }
}
