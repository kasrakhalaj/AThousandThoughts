using System.IO;
using AfterHours;
using UnityEditor;
using UnityEngine;

public static class PlayerLampPreview
{
    public static object Main()
    {
        string label=SessionState.GetString("AfterHours.LampPreview.Label","before");
        string folder="Temp/PlayerLamps-"+label;Directory.CreateDirectory(folder);
        var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Game/Generated/GameCatalog.asset");
        foreach(var prefab in catalog.cars)
        for(int rear=0;rear<2;rear++)
        {
            var preview=new PreviewRenderUtility();
            try
            {
                var car=Object.Instantiate(prefab); preview.AddSingleGO(car);
                var lamps=car.GetComponentInChildren<VehicleLights>();
                if(label=="source") Object.DestroyImmediate(lamps.gameObject); else lamps.SetIgnition(true);
                preview.camera.transform.position=new Vector3(4,2.6f,rear==0?6.8f:-6.8f);
                preview.camera.transform.LookAt(new Vector3(0,.8f,0));
                preview.camera.fieldOfView=36;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=40;
                preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.075f,.09f,.11f);
                preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(40,30,0);
                preview.lights[1].intensity=.7f;preview.lights[1].transform.rotation=Quaternion.Euler(20,210,0);
                preview.ambientColor=new Color(.35f,.35f,.35f);
                preview.BeginStaticPreview(new Rect(0,0,850,550));preview.Render(true);
                var image=preview.EndStaticPreview();File.WriteAllBytes(folder+"/"+prefab.name+(rear==0?"-front.png":"-rear.png"),image.EncodeToPNG());Object.DestroyImmediate(image);
            }
            finally{preview.Cleanup();}
        }
        return folder;
    }
}
