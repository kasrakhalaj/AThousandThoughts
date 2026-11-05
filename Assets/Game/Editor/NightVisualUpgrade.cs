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
        foreach (var car in catalog.cars) PlayerLampUpgrade.UpgradeCar(car, white, red);
        if (catalog.movingTrafficVariants == null || catalog.movingTrafficVariants.Length == 0)
        { UpdateTrafficVisual(catalog.stationaryCar, catalog.cars[2]); UpdateTrafficVisual(catalog.movingCar, catalog.cars[0]); }
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
