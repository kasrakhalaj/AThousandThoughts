using System;
using System.Collections.Generic;
using System.Linq;
using AfterHours;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class NightVisualUpgrade
{
    const string Folder = "Assets/Game/Generated/";
    [MenuItem("After Hours/Apply night scenery and vehicle lights")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(Folder + "GameCatalog.asset");
        var white = Material("Headlight lens", new Color(.85f, .84f, .7f));
        var red = Material("Rear light lens", new Color(.55f, .018f, .008f));
        var hill = Material("Distant hills", new Color(.075f, .14f, .11f));
        Vector2[] front = { new Vector2(.58f,.72f), new Vector2(.57f,.67f), new Vector2(.62f,.87f), new Vector2(.55f,.96f), new Vector2(.68f,.48f) };
        Vector2[] rear = { new Vector2(.62f,.52f), new Vector2(.61f,.76f), new Vector2(.7f,.55f), new Vector2(.6f,.74f), new Vector2(.7f,.5f) };
        for (int i = 0; i < catalog.cars.Length; i++) UpgradeCar(catalog.cars[i], front[i], rear[i], white, red, i == 0 || i == 2 || i == 3);
        UpdateTrafficVisual(catalog.stationaryCar, catalog.cars[2]); UpdateTrafficVisual(catalog.movingCar, catalog.cars[0]);
        if (catalog.shrubs == null || catalog.shrubs.Length == 0) catalog.shrubs = AfterHoursBuilder.BuildRoadsidePlants();
        catalog.hills = new[] { HillPrefab(hill) };
        EditorUtility.SetDirty(catalog);
        InstallFog();
        AssetDatabase.SaveAssets();
        Debug.Log("NIGHT_VISUAL_UPGRADE_APPLIED");
    }
    static Material Material(string name, Color color)
    {
        string path = Folder + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color; mat.SetFloat("_Smoothness", .5f);
        // A non-black shared value keeps Unity's material validation from stripping emission.
        // VehicleLights supplies the actual on/off intensity through property blocks.
        bool lamp = name == "Headlight lens" || name == "Rear light lens";
        mat.SetColor("_EmissionColor", lamp ? color : Color.black);
        mat.globalIlluminationFlags = lamp ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.None;
        MaterialEditor.FixupEmissiveFlag(mat);
        if (lamp) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
        mat.enableInstancing = true; EditorUtility.SetDirty(mat); return mat;
    }
    static void UpgradeCar(GameObject prefab, Vector2 front, Vector2 rear, Material white, Material red, bool round)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        var car = PrefabUtility.LoadPrefabContents(path);
        // Replace only the generated lamps, retaining the authored model, scale and body materials.
        for (int i = car.transform.childCount - 1; i >= 1; i--)
            if (car.transform.GetChild(i).name == "Tail lamp" || car.transform.GetChild(i).name == "Vehicle lighting")
                UnityEngine.Object.DestroyImmediate(car.transform.GetChild(i).gameObject);
        var root = new GameObject("Vehicle lighting"); root.transform.SetParent(car.transform, false);
        var controller = root.AddComponent<VehicleLights>();
        var fronts = new Renderer[2]; var rears = new Renderer[2]; var reverses = new Renderer[2]; var beams = new Light[2];
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1 : 1;
            float fx = side * front.x, rx = side * rear.x;
            float fz = SurfaceZ(car.transform.GetChild(0), fx, front.y, true);
            float rz = SurfaceZ(car.transform.GetChild(0), rx, rear.y, false);
            var f = Lens("Headlight lens", root.transform, new Vector3(fx, front.y, fz + .04f), white, round ? new Vector3(.22f,.22f,.09f) : new Vector3(.3f,.13f,.08f), round);
            fronts[i] = f.GetComponent<Renderer>();
            var light = new GameObject("Headlight beam", typeof(Light)); light.transform.SetParent(root.transform, false);
            light.transform.localPosition = new Vector3(fx, front.y, fz + .12f); light.transform.localRotation = Quaternion.Euler(7, side * 4, 0);
            var beam = light.GetComponent<Light>(); beam.type = LightType.Spot; beam.color = new Color(1,.91f,.7f);
            beam.range = 72; beam.intensity = 24; beam.spotAngle = 54; beam.innerSpotAngle = 28; beam.shadows = LightShadows.None; beams[i] = beam;
            rears[i] = Lens("Red tail and brake light", root.transform, new Vector3(rx,rear.y,rz-.04f), red, new Vector3(.23f,.13f,.08f), false).GetComponent<Renderer>();
            float reverseX = rx - side * .17f;
            float reverseZ = SurfaceZ(car.transform.GetChild(0), reverseX, rear.y, false);
            reverses[i] = Lens("Reverse light", root.transform, new Vector3(reverseX,rear.y,reverseZ-.04f), white, new Vector3(.09f,.1f,.07f), false).GetComponent<Renderer>();
        }
        var back = new GameObject("Rear red glow", typeof(Light)); back.transform.SetParent(root.transform, false); back.transform.localPosition = new Vector3(0,.5f,-2.3f);
        var backlight = back.GetComponent<Light>(); backlight.type=LightType.Point; backlight.color=new Color(1,.025f,.01f); backlight.range=3.5f; backlight.intensity=.22f;
        controller.Configure(fronts,rears,reverses,beams,backlight);
        PrefabUtility.SaveAsPrefabAsset(car,path); PrefabUtility.UnloadPrefabContents(car);
    }
    static GameObject Lens(string name, Transform parent, Vector3 position, Material mat, Vector3 scale, bool round)
    {
        var go = GameObject.CreatePrimitive(round ? PrimitiveType.Sphere : PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent,false);
        go.transform.localPosition=position; go.transform.localScale=scale; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial=mat; go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off; return go;
    }
    // Intersect the model triangles in XY, so each lens sits on its own car's body surface.
    static float SurfaceZ(Transform model, float x, float y, bool front)
    {
        float result = front ? float.NegativeInfinity : float.PositiveInfinity;
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
        {
            var mesh=filter.sharedMesh; var vertices=mesh.vertices; var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector3 a=filter.transform.TransformPoint(vertices[triangles[i]]), b=filter.transform.TransformPoint(vertices[triangles[i+1]]), c=filter.transform.TransformPoint(vertices[triangles[i+2]]);
                float det=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);
                if(Mathf.Abs(det)<.000001f)continue;
                float u=((b.y-c.y)*(x-c.x)+(c.x-b.x)*(y-c.y))/det;
                float v=((c.y-a.y)*(x-c.x)+(a.x-c.x)*(y-c.y))/det;
                if(u<0 || v<0 || u+v>1)continue;
                float z=u*a.z+v*b.z+(1-u-v)*c.z;
                result=front?Mathf.Max(result,z):Mathf.Min(result,z);
            }
        }
        return float.IsInfinity(result) ? (front ? 2.1f : -2.1f) : result;
    }
    static void UpdateTrafficVisual(GameObject prefab, GameObject model)
    {
        string path=AssetDatabase.GetAssetPath(prefab); var root=PrefabUtility.LoadPrefabContents(path);
        for(int i=root.transform.childCount-1;i>=0;i--)
            if(root.transform.GetChild(i).name.StartsWith("Car-",StringComparison.Ordinal)) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        var visual=UnityEngine.Object.Instantiate(model,root.transform); visual.name=model.name;
        PrefabUtility.SaveAsPrefabAsset(root,path); PrefabUtility.UnloadPrefabContents(root);
    }
    static GameObject HillPrefab(Material material)
    {
        string path=Folder+"LandscapeHill.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(prefab!=null)return prefab;
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        Action<Vector3,Vector3,Vector3> triangle=(a,b,c)=>{int first=vertices.Count; vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);};
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
            var lowA=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)); var lowB=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
            var highA=new Vector3(Mathf.Cos(a)*.55f,.55f,Mathf.Sin(a)*.55f);var highB=new Vector3(Mathf.Cos(b)*.55f,.55f,Mathf.Sin(b)*.55f);
            triangle(lowA,highA,lowB);triangle(lowB,highA,highB);triangle(highA,new Vector3(.12f,1,-.1f),highB);
        }
        var mesh=new Mesh{name="Faceted rolling hill"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Folder+"LandscapeHill.asset");
        var go=new GameObject("Landscape hill",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        prefab=PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);return prefab;
    }
    static void InstallFog()
    {
        var shader=Shader.Find("AfterHours/RoadFog"); if(shader==null)throw new InvalidOperationException("Road fog shader missing");
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"RoadFog.mat");
        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,Folder+"RoadFog.mat");}
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        var feature=renderer.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f=>f.name=="Road distance and ground fog");
        if(feature==null){feature=ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();feature.name="Road distance and ground fog";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
        feature.passMaterial=mat;feature.fetchColorBuffer=true;feature.requirements=ScriptableRenderPassInput.Depth;
        feature.injectionPoint=FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;feature.Create();feature.SetActive(true);
        renderer.SetDirty();EditorUtility.SetDirty(feature);EditorUtility.SetDirty(renderer);
    }
}
