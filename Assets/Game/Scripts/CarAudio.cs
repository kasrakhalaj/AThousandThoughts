using UnityEngine;

namespace AfterHours
{
    public sealed class CarAudio : MonoBehaviour
    {
        GameManager game;
        GameCatalog catalog;
        AudioSource engine, midEngine, loadEngine, effects, rain;
        float rpm, ignitionRemaining;
        bool trafficAudible, paused;
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
        }
        AudioSource Loop(AudioClip clip, int priority)
        {
            var source = gameObject.AddComponent<AudioSource>(); source.clip = clip; source.loop = true;
            source.volume = 0; source.priority = priority; source.playOnAwake = false; source.Play(); return source;
        }
        public void Ignition() { effects.Stop(); ignitionRemaining = game.IgnitionDuration; effects.PlayOneShot(catalog.ignitionSound, .7f); }
        public void Collision() { effects.PlayOneShot(catalog.collisionSound, .8f); }
        public void Coin() { effects.PlayOneShot(catalog.coinSound, .38f); }
        public void Fuel() { effects.PlayOneShot(catalog.fuelSound, .5f); }
        public void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            ignitionRemaining = Mathf.Max(0, ignitionRemaining - dt);
            float speed = Mathf.Abs(game.Driver.Speed);
            float gearProgress = Mathf.Repeat(speed, 8) / 8;
            float targetRpm = speed < 1 ? .08f + game.Driver.Throttle * .18f : .2f + gearProgress * .62f + game.Driver.Throttle * .15f;
            rpm = Mathf.Lerp(rpm, targetRpm, 1 - Mathf.Exp(-dt * 8));
            float idleWeight = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .42f, rpm));
            float loadWeight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, .95f, rpm));
            float midWeight = 1 - Mathf.Max(idleWeight, loadWeight);
            float running = game.State == GameState.Playing ? 1 : game.State == GameState.Starting ? Mathf.InverseLerp(.65f, 0, ignitionRemaining) : 0;
            float gain = Mathf.Lerp(.22f, .3f, game.Driver.Throttle) * running;
            engine.volume = Mathf.MoveTowards(engine.volume, idleWeight * gain, dt * 1.5f);
            midEngine.volume = Mathf.MoveTowards(midEngine.volume, midWeight * gain, dt * 1.5f);
            loadEngine.volume = Mathf.MoveTowards(loadEngine.volume, loadWeight * gain, dt * 1.5f);
            float character = game.SelectedCar == 2 ? .84f : game.SelectedCar == 3 ? .91f : game.SelectedCar == 4 ? 1.09f : 1;
            engine.pitch = character * Mathf.Lerp(.88f, 1.08f, rpm);
            midEngine.pitch = character * Mathf.Lerp(.8f, 1.15f, rpm);
            loadEngine.pitch = character * Mathf.Lerp(.84f, 1.16f, rpm);
            rain.volume = game.Weather.RainAmount * .18f * (game.State == GameState.Paused ? .4f : 1);
            bool wantTraffic = game.State == GameState.Playing;
            if (wantTraffic != trafficAudible) { trafficAudible = wantTraffic; game.World.SetTrafficAudioPaused(!wantTraffic); }
            bool wantPause = game.State == GameState.Paused;
            if (paused != wantPause) { paused = wantPause; if (paused) effects.Pause(); else effects.UnPause(); }
            if (game.State == GameState.Menu) effects.Stop();
        }
    }
}
