using UnityEngine;

namespace AfterHours
{
    public sealed class CarAudio : MonoBehaviour
    {
        GameManager game;
        GameCatalog catalog;
        AudioSource engine, midEngine, loadEngine, effects, rain, shiftEffects, pedalEffects;
        AudioSource heavyRain, thunder;
        float thunderCooldown;
        int nextThunder;
        float rpm, ignitionRemaining;
        bool trafficAudible, paused, gasHeld;
        int observedShift;
        float pedalCooldown;
        int voiceCar = -1;
        CarEngineVoice voice;
        public AudioClip ActiveLoadClip => loadEngine.clip;
        public int ShiftCueCount { get; private set; }
        public int ThrottleCueCount { get; private set; }
        public bool GearCuePlaying => shiftEffects.isPlaying;
        public bool PedalCuePlaying => pedalEffects.isPlaying;
        public float EngineVolume => engine.volume + midEngine.volume + loadEngine.volume;
        public float RainVolume => rain.volume + heavyRain.volume;
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
            heavyRain = Loop(catalog.heavyRainSound, 91);
            thunder = gameObject.AddComponent<AudioSource>();
            thunder.playOnAwake = false;
            thunder.priority = 48;
            shiftEffects = gameObject.AddComponent<AudioSource>(); shiftEffects.playOnAwake = false; shiftEffects.volume = .45f; shiftEffects.priority = 22;
            pedalEffects = gameObject.AddComponent<AudioSource>(); pedalEffects.playOnAwake = false; pedalEffects.volume = .23f; pedalEffects.priority = 25;
            SelectVoice();
        }
        void SelectVoice()
        {
            if (voiceCar == game.SelectedCar) return;
            voiceCar = game.SelectedCar; voice = null;
            if (catalog.performanceVoices != null)
                foreach (var candidate in catalog.performanceVoices)
                    if (candidate != null && string.Equals(candidate.carName, catalog.carNames[voiceCar], System.StringComparison.OrdinalIgnoreCase)) { voice = candidate; break; }
            SetLoop(engine, voice != null && voice.idle != null ? voice.idle : catalog.engineSound);
            SetLoop(midEngine, voice != null && voice.mid != null ? voice.mid : catalog.engineMidSound != null ? catalog.engineMidSound : catalog.engineSound);
            SetLoop(loadEngine, voice != null && voice.load != null ? voice.load : catalog.engineLoadSound != null ? catalog.engineLoadSound : catalog.engineSound);
            pedalEffects.Stop(); gasHeld = false;
        }
        static void SetLoop(AudioSource source, AudioClip clip)
        {
            source.Stop(); source.clip = clip; source.volume = 0;
            if (clip != null) source.Play();
        }
        AudioSource Loop(AudioClip clip, int priority)
        {
            var source = gameObject.AddComponent<AudioSource>(); source.clip = clip; source.loop = true;
            source.volume = 0; source.priority = priority; source.playOnAwake = false;
            if (clip != null) source.Play();
            return source;
        }
        public void Ignition()
        {
            effects.Stop(); shiftEffects.Stop(); pedalEffects.Stop();
            thunder.Stop();
            thunderCooldown = 0;
            ShiftCueCount = ThrottleCueCount = observedShift = 0; gasHeld = false; pedalCooldown = 0;
            ignitionRemaining = game.IgnitionDuration; effects.PlayOneShot(catalog.ignitionSound, .7f);
        }
        public void Collision() { effects.PlayOneShot(catalog.collisionSound, .8f); }
        public void Coin() { effects.PlayOneShot(catalog.coinSound, .38f); }
        public void Fuel() { effects.PlayOneShot(catalog.fuelSound, .5f); }
        public void Tick()
        {
            SelectVoice();
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
                var blip = voice != null && voice.downshift != null ? voice.downshift : catalog.downshiftBlipSound;
                if (driver.LastShiftDirection < 0 && blip != null)
                {
                    pedalEffects.Stop(); pedalEffects.PlayOneShot(blip, Mathf.Lerp(.45f, .8f, Mathf.InverseLerp(0, 20, speed)));
                    pedalCooldown = .5f;
                }
            }
            observedShift = driver.ShiftSequence;
            if (playing && gas != gasHeld && pedalCooldown <= 0 && !driver.Braking && !driver.IsShifting)
            {
                var on = voice != null && voice.gasOn != null ? voice.gasOn : catalog.throttleOnSound;
                var off = voice != null && voice.gasOff != null ? voice.gasOff : catalog.throttleOffSound;
                var clip = gas ? on : speed > 3 ? off : null;
                if (clip != null) { pedalEffects.Stop(); pedalEffects.PlayOneShot(clip); ThrottleCueCount++; pedalCooldown = .65f; }
            }
            gasHeld = gas;
            rpm = Mathf.Lerp(rpm, targetRpm, 1 - Mathf.Exp(-dt * 8));
            float idleWeight = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .42f, rpm));
            float loadWeight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, .95f, rpm));
            float midWeight = 1 - Mathf.Max(idleWeight, loadWeight);
            float running = game.State == GameState.Playing ? 1 : game.State == GameState.Starting ? Mathf.InverseLerp(.65f, 0, ignitionRemaining) : 0;
            float limiterPulse = driver.RevLimiter ? .72f + .28f * Mathf.Max(0, Mathf.Sin(Time.time * 42)) : 1;
            float gain = Mathf.Lerp(.22f, .3f, driver.Throttle) * running * (driver.IsShifting ? .55f : 1) * limiterPulse * (voice != null ? voice.gain : 1);
            engine.volume = Mathf.MoveTowards(engine.volume, idleWeight * gain, dt * 1.5f);
            midEngine.volume = Mathf.MoveTowards(midEngine.volume, midWeight * gain, dt * 1.5f);
            loadEngine.volume = Mathf.MoveTowards(loadEngine.volume, loadWeight * gain, dt * 1.5f);
            float character = voice != null ? voice.pitch : game.SelectedCar == 2 ? .84f : game.SelectedCar == 3 ? .91f : game.SelectedCar == 4 ? 1.09f : 1;
            engine.pitch = character * Mathf.Lerp(.88f, 1.08f, rpm);
            midEngine.pitch = character * Mathf.Lerp(.8f, 1.15f, rpm);
            loadEngine.pitch = character * Mathf.Lerp(.84f, 1.16f, rpm);
            bool wantTraffic = game.State == GameState.Playing;
            if (wantTraffic != trafficAudible) { trafficAudible = wantTraffic; game.World.SetTrafficAudioPaused(!wantTraffic); }
            bool wantPause = game.State == GameState.Paused;
            if (paused != wantPause)
            {
                paused = wantPause;
                if (paused) { effects.Pause(); shiftEffects.Pause(); pedalEffects.Pause(); thunder.Pause(); }
                else { effects.UnPause(); shiftEffects.UnPause(); pedalEffects.UnPause(); thunder.UnPause(); }
            }
            TickWeather();
            if (game.State == GameState.Menu) effects.Stop();
            if (game.State == GameState.Menu || game.State == GameState.GameOver) { shiftEffects.Stop(); pedalEffects.Stop(); }
        }

        void TickWeather()
        {
            float storm = game.Weather.StormAmount;
            float rainGain = game.Weather.RainAmount * Mathf.Lerp(.36f, .58f, storm);
            if (paused) rainGain *= .4f;
            float blend = heavyRain.clip != null ? storm : 0;
            rain.volume = rainGain * (1 - blend);
            heavyRain.volume = rainGain * blend;

            if (game.State == GameState.Menu || game.State == GameState.GameOver)
            {
                thunder.Stop();
                return;
            }
            if (game.State != GameState.Playing || catalog.thunderSounds == null || catalog.thunderSounds.Length == 0) return;

            thunderCooldown -= Time.deltaTime;
            if (storm < .35f || game.Weather.RainAmount < .25f || thunderCooldown > 0 || thunder.isPlaying) return;

            thunder.clip = catalog.thunderSounds[nextThunder];
            nextThunder = (nextThunder + 1) % catalog.thunderSounds.Length;
            thunderCooldown = Random.Range(16f, 27f);
            thunder.volume = Mathf.Lerp(.55f, .72f, storm);
            if (thunder.clip != null) thunder.Play();
        }
    }
}
