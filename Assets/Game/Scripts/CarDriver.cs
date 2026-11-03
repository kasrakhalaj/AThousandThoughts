using UnityEngine;

namespace AfterHours
{
    public sealed class CarDriver : MonoBehaviour
    {
        public float Speed { get; private set; }
        public bool Reversing { get; private set; }
        public int Lane { get; private set; } = 1;
        public float HitCooldown { get; private set; }
        public const float LaneWidth = 3.6f;
        public const float LaneTime = .24f;
        GameObject[] models;
        Transform visual;
        Light[] headlights;
        Light underglow;
        ParticleSystem exhaust;
        Renderer[] renderers;
        MaterialPropertyBlock flash;
        float brakeHold;
        float desiredSpeed;

        public void Initialize(GameCatalog catalog)
        {
            flash = new MaterialPropertyBlock();
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            models = new GameObject[catalog.cars.Length];
            for (int i = 0; i < models.Length; i++) models[i] = Instantiate(catalog.cars[i], visual);
            headlights = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Headlight"); go.transform.SetParent(visual, false);
                go.transform.localPosition = new Vector3(i == 0 ? -.7f : .7f, .65f, 2f);
                go.transform.localRotation = Quaternion.Euler(9, i == 0 ? -5 : 5, 0);
                var light = go.AddComponent<Light>(); light.type = LightType.Spot;
                light.range = 65; light.spotAngle = 60; light.innerSpotAngle = 33;
                light.intensity = 16; light.color = new Color(1, .91f, .72f); light.shadows = LightShadows.None;
                headlights[i] = light;
            }
            var glow = new GameObject("Subtle underbody light"); glow.transform.SetParent(visual, false);
            glow.transform.localPosition = new Vector3(0, .22f, 0);
            underglow = glow.AddComponent<Light>(); underglow.type = LightType.Point;
            underglow.range = 3; underglow.color = new Color(.25f, .47f, .52f);
            exhaust = DriveEffects.CreateParticles("Exhaust", visual, catalog.particleMaterial, new Color(.5f, .54f, .56f, .22f), 35, .8f, .15f, .8f);
            exhaust.transform.localPosition = new Vector3(-.65f, .3f, -2f);
            exhaust.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            Select(0); Reset();
        }

        public void Select(int index)
        {
            for (int i = 0; i < models.Length; i++) models[i].SetActive(i == index);
            renderers = models[index].GetComponentsInChildren<Renderer>();
        }
        public void SetIgnition(bool on)
        {
            foreach (var headlight in headlights) headlight.enabled = on;
            underglow.enabled = on;
            var emission = exhaust.emission; emission.enabled = on;
        }
        public void Reset()
        {
            transform.position = new Vector3(0, .08f, 0); Speed = 0; Reversing = false;
            Lane = 1; HitCooldown = 0; brakeHold = 0; desiredSpeed = 0;
            visual.localRotation = Quaternion.identity;
        }
        public void Tick(DriverInput input, float cruise, float dt)
        {
            HitCooldown = Mathf.Max(0, HitCooldown - dt);
            if (input.Reverse.WasPressedThisFrame()) Reversing = !Reversing;
            if (input.Forward.WasPressedThisFrame()) { Reversing = false; brakeHold = 0; }
            bool brake = input.Brake.IsPressed();
            if (brake && !Reversing)
            {
                brakeHold = Mathf.Abs(Speed) < .3f ? brakeHold + dt : 0;
                if (brakeHold > .45f) Reversing = true;
            }
            else brakeHold = 0;
            desiredSpeed = Reversing ? -10 : brake ? 0 : cruise;
            Speed = Mathf.MoveTowards(Speed, desiredSpeed, (brake || Speed * desiredSpeed < 0 ? 22 : 8) * dt);
            int steering = (input.Right.WasPressedThisFrame() ? 1 : 0) - (input.Left.WasPressedThisFrame() ? 1 : 0);
            // When the rear-facing camera is active, left/right follow screen space.
            if (Reversing) steering = -steering;
            Lane = Mathf.Clamp(Lane + steering, 0, 2);
            Vector3 position = transform.position;
            float targetX = (Lane - 1) * LaneWidth;
            float oldX = position.x;
            position.x = Mathf.MoveTowards(position.x, targetX, LaneWidth / LaneTime * dt);
            position.z += Speed * dt;
            transform.position = position;
            float lean = dt > 0 ? (position.x - oldX) / dt : 0;
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0, lean * .8f, -lean * .25f), dt * 9);
            underglow.intensity = Mathf.Lerp(.1f, 1.8f, Mathf.InverseLerp(0, 34, Mathf.Abs(Speed)));
            flash.SetColor("_EmissionColor", HitCooldown > 0 && Mathf.Sin(HitCooldown * 35) > 0 ? new Color(.8f, .12f, .04f) : Color.black);
            foreach (var renderer in renderers) renderer.SetPropertyBlock(flash);
        }
        public bool TakeHit()
        {
            if (HitCooldown > 0) return false;
            HitCooldown = 2.4f; Speed *= .55f; return true;
        }
    }
}
