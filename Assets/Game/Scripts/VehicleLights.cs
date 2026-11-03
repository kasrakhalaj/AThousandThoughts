using UnityEngine;

namespace AfterHours
{
    public sealed class VehicleLights : MonoBehaviour
    {
        [SerializeField] Renderer[] frontLenses;
        [SerializeField] Renderer[] rearLenses;
        [SerializeField] Renderer[] reverseLenses;
        [SerializeField] Light[] beams;
        [SerializeField] Light rearGlow;
        MaterialPropertyBlock properties;
        bool ignition;
        bool traffic;
        public bool IsOn => ignition;
        public bool Braking { get; private set; }
        public bool Reversing { get; private set; }
        public int HeadlightCount => frontLenses.Length;
        public int TailLightCount => rearLenses.Length;

        public void Configure(Renderer[] front, Renderer[] rear, Renderer[] reverse, Light[] headlights, Light back)
        {
            frontLenses = front; rearLenses = rear; reverseLenses = reverse; beams = headlights; rearGlow = back;
        }
        public void SetIgnition(bool on, bool isTraffic = false)
        {
            ignition = on; traffic = isTraffic; SetDrivingState(false, false);
        }
        public void SetDrivingState(bool braking, bool reversing)
        {
            Braking = braking; Reversing = reversing;
            if (properties == null) properties = new MaterialPropertyBlock();
            Paint(frontLenses, new Color(1, .91f, .7f), ignition ? 6 : 0);
            Paint(rearLenses, new Color(1, .018f, .006f), ignition ? braking ? 7 : 2.8f : .08f);
            Paint(reverseLenses, new Color(.8f, .9f, 1), ignition && reversing ? 5 : 0);
            foreach (var beam in beams) { beam.enabled = ignition; beam.range = traffic ? 30 : 72; beam.intensity = traffic ? 9 : 24; }
            rearGlow.enabled = ignition; rearGlow.intensity = braking ? .8f : .22f;
        }
        void Paint(Renderer[] lenses, Color color, float intensity)
        {
            properties.SetColor("_EmissionColor", color * intensity);
            foreach (var lens in lenses) lens.SetPropertyBlock(properties);
        }
    }
}
