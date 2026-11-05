using UnityEngine;

namespace AfterHours
{
    public sealed class CarAudio : MonoBehaviour
    {
        GameManager game;
        GameCatalog catalog;
        AudioSource engine, midEngine, loadEngine, effects, rain, shiftEffects, pedalEffects;
        float rpm, ignitionRemaining;
        bool trafficAudible, paused, gasHeld;
        int observedShift;
        float pedalCooldown;
        public int ShiftCueCount { get; private set; }
        public int ThrottleCueCount { get; private set; }
        public bool GearCuePlaying => shiftEffects.isPlaying;
        public bool PedalCuePlaying => pedalEffects.isPlaying;
        public float EngineVolume => engine.volume + midEngine.volume + loadEngine.volume;
        public float RainVolume => rain.volume;
        public bool EnginePlaying => engine.isPlaying;
        public void Initialize(GameManager manager, GameCatalog data)
        {
            game = manager; catalog = data;
            engine = Loop(catalog.engineSound, 30);
            midEngine = Loop(catalog.engineMidSound != null ? catalog.engineMidSound : catalog.engineSound, 31);
            loadEngine = Loop(catalog.engineLoadSound != null ? catalog.engineLoadSound : catalog.engineSound, 32);
            effects = gameObject.AddComponent<AudioSource>(); effects.volume = .55f;
            effects.priority = 20;
            rain = Loop(catalog.rainSound, 90);
            shiftEffects = gameObject.AddComponent<AudioSource>(); shiftEffects.playOnAwake = false; shiftEffects.volume = .45f; shiftEffects.priority = 22;
            pedalEffects = gameObject.AddComponent<AudioSource>(); pedalEffects.playOnAwake = false; pedalEffects.volume = .23f; pedalEffects.priority = 25;
        }
        AudioSource Loop(AudioClip clip, int priority)
        {
            var source = gameObject.AddComponent<AudioSource>(); source.clip = clip; source.loop = true;
            source.volume = 0; source.priority = priority; source.playOnAwake = false; source.Play(); return source;
        }
        public void Ignition()
        {
            effects.Stop(); shiftEffects.Stop(); pedalEffects.Stop();
            ShiftCueCount = ThrottleCueCount = observedShift = 0; gasHeld = false; pedalCooldown = 0;
            ignitionRemaining = game.IgnitionDuration; effects.PlayOneShot(catalog.ignitionSound, .7f);
        }
        public void Collision() { effects.PlayOneShot(catalog.collisionSound, .8f); }
        public void Coin() { effects.PlayOneShot(catalog.coinSound, .38f); }
        public void Fuel() { effects.PlayOneShot(catalog.fuelSound, .5f); }
        public void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            ignitionRemaining = Mathf.Max(0, ignitionRemaining - dt);
            float speed = Mathf.Abs(game.Driver.Speed);
            var driver = game.Driver;
            float targetRpm = Mathf.InverseLerp(850, 7200, driver.EngineRpm);
            pedalCooldown = Mathf.Max(0, pedalCooldown - dt);
            bool playing = game.State == GameState.Playing;
            bool gas = driver.Throttle > .15f;
            if (playing && driver.ShiftSequence > observedShift)
            {
                if (catalog.gearShiftSounds != null && catalog.gearShiftSounds.Length > 0)
                {
                    var clip = catalog.gearShiftSounds[(driver.ShiftSequence - 1) % catalog.gearShiftSounds.Length];
                    if (clip != null) { shiftEffects.PlayOneShot(clip); ShiftCueCount++; }
                }
                if (driver.LastShiftDirection < 0 && catalog.downshiftBlipSound != null)
                {
                    pedalEffects.Stop(); pedalEffects.PlayOneShot(catalog.downshiftBlipSound, Mathf.Lerp(.45f, .8f, Mathf.InverseLerp(0, 20, speed)));
                    pedalCooldown = .5f;
                }
            }
            observedShift = driver.ShiftSequence;
            if (playing && gas != gasHeld && pedalCooldown <= 0 && !driver.Braking && !driver.IsShifting)
            {
                var clip = gas ? catalog.throttleOnSound : speed > 3 ? catalog.throttleOffSound : null;
                if (clip != null) { pedalEffects.Stop(); pedalEffects.PlayOneShot(clip); ThrottleCueCount++; pedalCooldown = .65f; }
            }
            gasHeld = gas;
            rpm = Mathf.Lerp(rpm, targetRpm, 1 - Mathf.Exp(-dt * 8));
            float idleWeight = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .42f, rpm));
            float loadWeight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, .95f, rpm));
            float midWeight = 1 - Mathf.Max(idleWeight, loadWeight);
            float running = game.State == GameState.Playing ? 1 : game.State == GameState.Starting ? Mathf.InverseLerp(.65f, 0, ignitionRemaining) : 0;
            float limiterPulse = driver.RevLimiter ? .72f + .28f * Mathf.Max(0, Mathf.Sin(Time.time * 42)) : 1;
            float gain = Mathf.Lerp(.22f, .3f, driver.Throttle) * running * (driver.IsShifting ? .55f : 1) * limiterPulse;
            engine.volume = Mathf.MoveTowards(engine.volume, idleWeight * gain, dt * 1.5f);
            midEngine.volume = Mathf.MoveTowards(midEngine.volume, midWeight * gain, dt * 1.5f);
            loadEngine.volume = Mathf.MoveTowards(loadEngine.volume, loadWeight * gain, dt * 1.5f);
            float character = game.SelectedCar == 2 ? .84f : game.SelectedCar == 3 ? .91f : game.SelectedCar == 4 ? 1.09f : 1;
            engine.pitch = character * Mathf.Lerp(.88f, 1.08f, rpm);
            midEngine.pitch = character * Mathf.Lerp(.8f, 1.15f, rpm);
            loadEngine.pitch = character * Mathf.Lerp(.84f, 1.16f, rpm);
            rain.volume = game.Weather.RainAmount * Mathf.Lerp(.18f, .30f, game.Weather.StormAmount) * (game.State == GameState.Paused ? .4f : 1);
            bool wantTraffic = game.State == GameState.Playing;
            if (wantTraffic != trafficAudible) { trafficAudible = wantTraffic; game.World.SetTrafficAudioPaused(!wantTraffic); }
            bool wantPause = game.State == GameState.Paused;
            if (paused != wantPause)
            {
                paused = wantPause;
                if (paused) { effects.Pause(); shiftEffects.Pause(); pedalEffects.Pause(); }
                else { effects.UnPause(); shiftEffects.UnPause(); pedalEffects.UnPause(); }
            }
            if (game.State == GameState.Menu) effects.Stop();
            if (game.State == GameState.Menu || game.State == GameState.GameOver) { shiftEffects.Stop(); pedalEffects.Stop(); }
        }
    }
}
