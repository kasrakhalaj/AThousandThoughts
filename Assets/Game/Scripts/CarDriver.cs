using UnityEngine;

namespace AfterHours
{
    public sealed class CarDriver : MonoBehaviour
    {
        public float Speed { get; private set; }
        public bool Reversing { get; private set; }
        public int Lane { get; private set; } = 1;
        public float HitCooldown { get; private set; }
        public float Throttle { get; private set; }
        public bool Braking { get; private set; }
        public int Gear => Mathf.Abs(Speed) < 1 ? 1 : Mathf.Clamp(1 + Mathf.FloorToInt(Mathf.Abs(Speed) / 8), 1, 5);
        public const float LaneWidth = 3.6f;
        public const float LaneTime = .24f;
        GameObject[] models;
        Transform visual;
        VehicleLights[] lamps;
        int selected;
        bool ignition;
        Light underglow;
        ParticleSystem exhaust;
        Renderer[] renderers;
        MaterialPropertyBlock flash;
        float desiredSpeed;

        public void Initialize(GameCatalog catalog)
        {
            flash = new MaterialPropertyBlock();
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            models = new GameObject[catalog.cars.Length];
            for (int i = 0; i < models.Length; i++) models[i] = Instantiate(catalog.cars[i], visual);
            lamps = new VehicleLights[models.Length];
            for (int i = 0; i < models.Length; i++) lamps[i] = models[i].GetComponentInChildren<VehicleLights>();
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
            selected = index;
            for (int i = 0; i < models.Length; i++) models[i].SetActive(i == index);
            var body = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in models[index].GetComponentsInChildren<Renderer>())
                if (renderer.GetComponentInParent<VehicleLights>() == null) body.Add(renderer);
            renderers = body.ToArray();
            if (lamps[index] != null) lamps[index].SetIgnition(ignition);
        }
        public void SetIgnition(bool on)
        {
            ignition = on;
            if (lamps[selected] != null) lamps[selected].SetIgnition(on);
            underglow.enabled = on;
            var emission = exhaust.emission; emission.enabled = on;
        }
        public void Reset()
        {
            transform.position = new Vector3(0, .08f, 0); Speed = 0; Reversing = false;
            Lane = 1; HitCooldown = 0; desiredSpeed = 0; Throttle = 0; Braking = false;
            visual.localRotation = Quaternion.identity;
        }
        public void Tick(DriverInput input, float topSpeed, float dt)
        {
            HitCooldown = Mathf.Max(0, HitCooldown - dt);
            if (input.Reverse.WasPressedThisFrame()) Reversing = !Reversing;
            Throttle = Mathf.Clamp01(input.Throttle.ReadValue<float>());
            Braking = input.Brake.IsPressed();
            desiredSpeed = Braking ? 0 : (Reversing ? -10 : topSpeed) * Throttle;
            bool changingDirection = Speed * desiredSpeed < 0;
            float acceleration = Braking || changingDirection ? 22 : Throttle > .01f ? Mathf.Lerp(9, 5.5f, Mathf.Abs(Speed) / 44) : 2.2f;
            Speed = Mathf.MoveTowards(Speed, desiredSpeed, acceleration * dt);
            if (lamps[selected] != null) lamps[selected].SetDrivingState(Braking || changingDirection, Reversing);
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
