using UnityEngine;

namespace AfterHours
{
    public sealed class CarDriver : MonoBehaviour
    {
        public float Speed { get; private set; }
        public float Acceleration { get; private set; }
        public Vector3 PreviousPosition { get; private set; }
        public bool Reversing { get; private set; }
        public bool RearView => Speed < -.1f || Mathf.Abs(Speed) <= .1f && Reversing;
        public bool LookingBack { get; private set; }
        public bool CameraRearView => RearView || LookingBack;
        public int Lane { get; private set; } = 1;
        public float HitCooldown { get; private set; }
        public float Throttle { get; private set; }
        public bool Braking { get; private set; }
        readonly ManualTransmission transmission = new ManualTransmission();
        public int Gear => transmission.Gear;
        public int PendingGear => transmission.PendingGear;
        public bool IsShifting => transmission.IsShifting;
        public bool ShiftRejected => transmission.RejectionRemaining > 0;
        public int ShiftSequence => transmission.ShiftSequence;
        public int LastShiftDirection => transmission.LastShiftDirection;
        public float RpmRatio => Mathf.Clamp01((RearView ? Mathf.Abs(Speed) / VehicleDynamics.ReverseLimit : VehicleDynamics.GearRpmRatio(Speed, Gear)) * (IsShifting ? .65f : 1));
        public int EngineRpm => Mathf.RoundToInt(Mathf.Lerp(850, 7200, Mathf.Max(RpmRatio, Mathf.Abs(Speed) < 2 && !Braking ? Throttle * .15f : 0)));
        public bool RevLimiter => !Reversing && !IsShifting && !Braking && Throttle > .5f && Speed >= VehicleDynamics.GearSpeedLimit(Gear) - .05f;
        public bool Lugging => !Reversing && Gear > 1 && RpmRatio < .25f && Throttle > .2f;
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
            Lane = 1; HitCooldown = 0; Acceleration = 0; Throttle = 0; Braking = false; PreviousPosition = transform.position;
            transmission.Reset();
            LookingBack = false;
            visual.localRotation = Quaternion.identity;
        }
        public void Tick(DriverInput input, float topSpeed, float dt, float power = 1)
        {
            PreviousPosition = transform.position;
            LookingBack = input.LookBack.IsPressed();
            HitCooldown = Mathf.Max(0, HitCooldown - dt);
            transmission.Tick(dt);
            if (input.Reverse.WasPressedThisFrame()) { Reversing = !Reversing; transmission.Reset(); }
            int shift = (input.ShiftUp.WasPressedThisFrame() ? 1 : 0) - (input.ShiftDown.WasPressedThisFrame() ? 1 : 0);
            if (shift != 0) RequestShift(shift);
            Throttle = Mathf.Clamp01(input.Throttle.ReadValue<float>());
            Braking = input.Brake.IsPressed();
            bool changingDirection = Reversing ? Speed > 0 : Speed < 0;
            float previousSpeed = Speed;
            Speed = VehicleDynamics.StepInGear(Speed, Throttle, Braking, Reversing, topSpeed, dt, Gear, IsShifting, power);
            Acceleration = dt > 0 ? (Speed - previousSpeed) / dt : 0;
            if (lamps[selected] != null) lamps[selected].SetDrivingState(Braking || changingDirection, Reversing);
            int steering = (input.Right.WasPressedThisFrame() ? 1 : 0) - (input.Left.WasPressedThisFrame() ? 1 : 0);
            // When the rear-facing camera is active, left/right follow screen space.
            if (RearView) steering = -steering;
            Lane = Mathf.Clamp(Lane + steering, 0, 2);
            Vector3 position = transform.position;
            float targetX = (Lane - 1) * LaneWidth;
            float oldX = position.x;
            position.x = Mathf.MoveTowards(position.x, targetX, LaneWidth / LaneTime * dt);
            position.z += Speed * dt;
            transform.position = position;
            float lean = dt > 0 ? (position.x - oldX) / dt : 0;
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0, lean * .8f, -lean * .25f), dt * 9);
            underglow.intensity = Mathf.Lerp(.1f, 1.8f, Mathf.InverseLerp(0, VehicleDynamics.ForwardLimit, Mathf.Abs(Speed)));
            flash.SetColor("_EmissionColor", HitCooldown > 0 && Mathf.Sin(HitCooldown * 35) > 0 ? new Color(.8f, .12f, .04f) : Color.black);
            foreach (var renderer in renderers) renderer.SetPropertyBlock(flash);
        }
        public bool TakeHit()
        {
            if (HitCooldown > 0) return false;
            HitCooldown = 2.4f; Speed *= .55f; return true;
        }
        public bool RequestShift(int direction) => transmission.Request(direction, Speed, Reversing);
    }
}
