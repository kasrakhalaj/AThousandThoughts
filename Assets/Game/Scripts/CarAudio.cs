using UnityEngine;

namespace AfterHours
{
    public sealed class CarAudio : MonoBehaviour
    {
        GameManager game;
        GameCatalog catalog;
        AudioSource engine, effects, rain;
        public void Initialize(GameManager manager, GameCatalog data)
        {
            game = manager; catalog = data;
            engine = gameObject.AddComponent<AudioSource>(); engine.clip = catalog.engineSound; engine.loop = true; engine.volume = 0;
            engine.Play();
            effects = gameObject.AddComponent<AudioSource>(); effects.volume = .55f;
            rain = gameObject.AddComponent<AudioSource>(); rain.clip = catalog.rainSound; rain.loop = true; rain.volume = 0; rain.Play();
        }
        public void Ignition() { effects.PlayOneShot(catalog.ignitionSound, .7f); }
        public void Collision() { effects.PlayOneShot(catalog.collisionSound, .8f); }
        public void Coin() { effects.PlayOneShot(catalog.coinSound, .38f); }
        public void Fuel() { effects.PlayOneShot(catalog.fuelSound, .5f); }
        public void Tick()
        {
            float volume = game.State == GameState.Playing || game.State == GameState.Starting ? .16f : 0;
            engine.volume = Mathf.MoveTowards(engine.volume, volume, Time.unscaledDeltaTime);
            engine.pitch = .65f + Mathf.Abs(game.Driver.Speed) / 34f * 1.2f;
            rain.volume = game.Weather.RainAmount * .12f * (game.State == GameState.Paused ? .4f : 1);
        }
    }
}
