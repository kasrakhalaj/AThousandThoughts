using UnityEngine;
namespace AfterHours
{
    public class StationaryCar : Interactable
    {
        VehicleLights lamps;
        AudioSource engine;
        Transform[] wheels;
        Quaternion[] wheelRotations;
        Light[] hazards;
        protected bool braking;
        public virtual float Speed => 0;
        public float EngineVolume => engine != null ? engine.volume : 0;
        public bool EnginePlaying => engine != null && engine.isPlaying;
        public void Initialize(GameCatalog catalog)
        {
            lamps = GetComponentInChildren<VehicleLights>();
            var list = new System.Collections.Generic.List<Transform>();
            foreach (var t in GetComponentsInChildren<Transform>()) if (t.name.IndexOf("Wheel", System.StringComparison.OrdinalIgnoreCase) >= 0) list.Add(t);
            wheels = list.ToArray(); wheelRotations = new Quaternion[wheels.Length];
            for (int i = 0; i < wheels.Length; i++) wheelRotations[i] = wheels[i].localRotation;
            var hazardList = new System.Collections.Generic.List<Light>();
            foreach (var light in GetComponentsInChildren<Light>()) if (light.name.StartsWith("Breakdown hazard", System.StringComparison.Ordinal)) hazardList.Add(light);
            hazards = hazardList.ToArray();
            engine = gameObject.AddComponent<AudioSource>(); engine.clip = catalog.trafficEngineSound;
            engine.loop = true; engine.playOnAwake = false; engine.spatialBlend = 1; engine.volume = .085f;
            engine.minDistance = 3; engine.maxDistance = 42; engine.rolloffMode = AudioRolloffMode.Linear;
            engine.dopplerLevel = .25f; engine.priority = 220;
        }
        public override void Activate(GameManager owner, Vector3 position)
        {
            base.Activate(owner, position);
            if (lamps != null) lamps.SetIgnition(true, true);
            braking = false;
            for (int i = 0; i < wheels.Length; i++) wheels[i].localRotation = wheelRotations[i];
            if (engine.clip != null) engine.time = Random.Range(0, engine.clip.length * .8f);
            SetAudioPaused(owner.State != GameState.Playing);
        }
        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (lamps != null) lamps.SetDrivingState(braking, false);
            foreach (var wheel in wheels) wheel.Rotate(transform.right, Speed * dt / .3f * Mathf.Rad2Deg, Space.World);
            foreach (var hazard in hazards) hazard.enabled = Mathf.Sin(age * 8) > 0;
            engine.pitch = .86f + Speed / 40 * .35f;
        }
        public void SetAudioPaused(bool paused)
        {
            if (engine == null || engine.clip == null) return;
            if (paused) engine.Pause();
            else { engine.UnPause(); if (!engine.isPlaying) engine.Play(); }
        }
        void OnDisable() { if (engine != null) engine.Stop(); }
        protected override void Interact() { game.HitCar(transform.position); }
    }
}
