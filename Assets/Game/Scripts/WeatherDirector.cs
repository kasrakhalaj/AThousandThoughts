using UnityEngine;

namespace AfterHours
{
    public sealed class WeatherDirector : MonoBehaviour
    {
        GameManager game;
        ParticleSystem rain;
        float fireworkTimer = 17;
        public float RainAmount { get; private set; }
        public string Conditions { get; private set; } = "CLEAR NIGHT";
        public void Initialize(GameManager manager, GameCatalog catalog)
        {
            game = manager;
            rain = DriveEffects.CreateParticles("Rain", transform, catalog.particleMaterial, new Color(.65f, .75f, .83f, .45f), 800, 1.1f, .025f, 25);
            rain.transform.rotation = Quaternion.Euler(90, 0, 0);
            var shape = rain.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(40, 65, 1);
            var renderer = rain.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 5; renderer.velocityScale = .03f;
            var main = rain.main; main.maxParticles = 2400;
        }
        public void Tick(float dt)
        {
            float z = game.Driver.transform.position.z;
            int zone = ((Mathf.FloorToInt(z / 480) % 5) + 5) % 5;
            float targetRain = zone == 1 || zone == 2 ? 1 : zone == 4 ? .35f : 0;
            RainAmount = Mathf.MoveTowards(RainAmount, targetRain, dt * .12f);
            float density = zone == 2 || zone == 3 ? .013f : .0038f;
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, density, dt * .12f);
            RenderSettings.fogColor = new Color(.065f, .09f, .115f);
            Conditions = zone == 2 ? "RAIN / LOW FOG" : zone == 3 ? "FOG PATCH" : RainAmount > .3f ? "RAIN" : "CLEAR NIGHT";
            rain.transform.position = game.Driver.transform.position + new Vector3(0, 16, 12);
            var emission = rain.emission; emission.rateOverTime = RainAmount * 1700;
            fireworkTimer -= dt;
            if (fireworkTimer <= 0)
            {
                fireworkTimer = Random.Range(20, 38);
                game.Effects.Burst(game.Driver.transform.position + new Vector3(Random.Range(-50, 50), Random.Range(24, 40), Random.Range(90, 150)), Color.HSVToRGB(Random.value, .6f, 1) * 3, 110);
            }
        }
    }
}
