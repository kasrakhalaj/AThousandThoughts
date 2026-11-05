using System;
using System.Collections.Generic;
using System.IO;
using AfterHours;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class PlayerLampUpgrade
{
    const string Folder = "Assets/Game/Generated/PlayerLamps";

    [MenuItem("After Hours/Use authored player car lamps")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Game/Generated/GameCatalog.asset");
        var white = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Generated/Headlight lens.mat");
        var red = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Generated/Rear light lens.mat");
        foreach (var car in catalog.cars) UpgradeCar(car, white, red);
        AssetDatabase.SaveAssets();
        Debug.Log("AUTHORED_PLAYER_LAMPS_APPLIED");
    }

    public static void UpgradeCar(GameObject prefab, Material white, Material red)
    {
        if (white == null || red == null) throw new InvalidOperationException("Vehicle lens materials are missing.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string name = prefab.name.Substring("Car-".Length);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Vehicles/" + name + ".fbx");
        var sourceMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
        int emissive = Array.FindIndex(sourceMaterials, m => m.name.StartsWith("gradientEmmisive", StringComparison.Ordinal));
        if (emissive < 0) throw new InvalidOperationException("Authored lamp material missing: " + name);
        string path = AssetDatabase.GetAssetPath(prefab);
        var car = PrefabUtility.LoadPrefabContents(path);
        var pending = new List<Mesh>();
        try
        {
            var body = car.transform.GetChild(0).GetComponent<MeshFilter>();
            var meshes = new Mesh[3, 2];
            // Only the main body is inspected: wheel/time-machine accents and covered roof spots are not headlights.
            for (int kind = 0; kind < 3; kind++)
            for (int side = 0; side < 2; side++)
            {
                meshes[kind, side] = Extract(body, car.transform, name, emissive, kind, side);
                if (meshes[kind, side] != null) pending.Add(meshes[kind, side]);
                if (kind < 2 && meshes[kind, side] == null)
                    throw new InvalidOperationException("Missing authored " + (kind == 0 ? "headlight" : "taillight") + ": " + name);
            }
            // Preflight above succeeds before replacing any existing lighting.
            for (int i = car.transform.childCount - 1; i >= 1; i--)
                if (car.transform.GetChild(i).name == "Vehicle lighting" || car.transform.GetChild(i).name == "Tail lamp")
                    UnityEngine.Object.DestroyImmediate(car.transform.GetChild(i).gameObject);
            var lighting = new GameObject("Vehicle lighting"); lighting.transform.SetParent(car.transform, false);
            var controller = lighting.AddComponent<VehicleLights>();
            var fronts = new Renderer[2]; var rears = new Renderer[2]; var reverses = new List<Renderer>(); var beams = new Light[2];
            for (int side = 0; side < 2; side++)
            {
                fronts[side] = Lens(name, 0, side, meshes[0, side], lighting.transform, white);
                rears[side] = Lens(name, 1, side, meshes[1, side], lighting.transform, red);
                if (meshes[2, side] != null) reverses.Add(Lens(name, 2, side, meshes[2, side], lighting.transform, white));
                var beamObject = new GameObject("Headlight beam", typeof(Light)); beamObject.transform.SetParent(lighting.transform, false);
                beamObject.transform.localPosition = fronts[side].GetComponent<MeshFilter>().sharedMesh.bounds.center + Vector3.forward * .03f;
                beamObject.transform.localRotation = Quaternion.Euler(7, side == 0 ? -4 : 4, 0);
                var beam = beamObject.GetComponent<Light>(); beam.type = LightType.Spot; beam.color = new Color(1, .91f, .7f);
                beam.range = 72; beam.intensity = 24; beam.spotAngle = 54; beam.innerSpotAngle = 28; beam.shadows = LightShadows.None;
                beams[side] = beam;
            }
            var glow = new GameObject("Rear red glow", typeof(Light)); glow.transform.SetParent(lighting.transform, false);
            glow.transform.localPosition = (rears[0].GetComponent<MeshFilter>().sharedMesh.bounds.center + rears[1].GetComponent<MeshFilter>().sharedMesh.bounds.center) * .5f - Vector3.forward * .15f;
            glow.transform.localRotation = Quaternion.Euler(35, 180, 0);
            var back = glow.GetComponent<Light>(); back.type = LightType.Spot; back.spotAngle = 110; back.innerSpotAngle = 65;
            back.color = new Color(1, .025f, .01f); back.range = 3.5f; back.intensity = .22f;
            controller.Configure(fronts, rears, reverses.ToArray(), beams, back);
            PrefabUtility.SaveAsPrefabAsset(car, path);
        }
        finally
        {
            foreach (var mesh in pending) if (mesh != null && !AssetDatabase.Contains(mesh)) UnityEngine.Object.DestroyImmediate(mesh);
            PrefabUtility.UnloadPrefabContents(car);
        }
    }

    static bool IsLens(string car, int sub, int emissive, int kind, Vector3 center, Vector2 uv)
    {
        if (kind == 0)
            return sub == emissive && center.z > 1.4f && (uv.x < .04f || car == "Toyoyo Highlight");
        if (kind == 1)
            return sub == emissive && center.z < -1.6f && (car != "Landyroamer" || uv.x > .3f && uv.x < .5f);
        // These three models have distinct white reverse lenses. Beatall and Tristar have none;
        // license plates, reflectors and body panels must never become invented reverse lights.
        if (sub != 0 || center.z > -1.7f || Mathf.Abs(center.x) < .2f) return false;
        if (car == "docLorean") return uv.x < .03f && uv.y > .95f;
        if (car == "Landyroamer") return uv.x < .02f && uv.y > .85f && center.y < .8f;
        if (car == "Toyoyo Highlight") return uv.x > .12f && uv.x < .14f && uv.y > .95f;
        return false;
    }

    static Mesh Extract(MeshFilter body, Transform root, string car, int emissive, int kind, int side)
    {
        var mesh = body.sharedMesh; var points = mesh.vertices; var uvs = mesh.uv;
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        Matrix4x4 toRoot = root.worldToLocalMatrix * body.transform.localToWorldMatrix;
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            int[] indices = mesh.GetTriangles(sub);
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = toRoot.MultiplyPoint3x4(points[indices[i]]), b = toRoot.MultiplyPoint3x4(points[indices[i + 1]]), c = toRoot.MultiplyPoint3x4(points[indices[i + 2]]);
                Vector3 center = (a + b + c) / 3, normal = Vector3.Cross(b - a, c - a).normalized;
                Vector2 uv = (uvs[indices[i]] + uvs[indices[i + 1]] + uvs[indices[i + 2]]) / 3;
                if ((center.x < 0 ? 0 : 1) != side || !IsLens(car, sub, emissive, kind, center, uv)) continue;
                // Ignore inward faces on the reverse-lens discs. Offset by 2mm to prevent z-fighting.
                if (kind == 2 && normal.z > -.3f) continue;
                foreach (var p in new[] { a, b, c }) { triangles.Add(vertices.Count); vertices.Add(p + normal * .002f); }
            }
        }
        if (vertices.Count == 0) return null;
        var lamp = new Mesh { name = car + " authored " + new[] { "head", "tail", "reverse" }[kind] + " " + side };
        lamp.SetVertices(vertices); lamp.SetTriangles(triangles, 0); lamp.RecalculateNormals(); lamp.RecalculateBounds();
        return lamp;
    }

    static Renderer Lens(string car, int kind, int side, Mesh mesh, Transform parent, Material material)
    {
        string path = Folder + "/" + car + "-" + new[] { "head", "tail", "reverse" }[kind] + "-" + side + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(existing); }
        var go = new GameObject(new[] { "Headlight lens", "Red tail and brake light", "Reverse light" }[kind], typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false); go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        return renderer;
    }
}
