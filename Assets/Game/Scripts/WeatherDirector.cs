using UnityEngine;

namespace AfterHours
{
    public sealed class WeatherDirector : MonoBehaviour
    {
        GameManager game;
        ParticleSystem rain;
        float fireworkTimer = 17;
        public float RainAmount { get; private set; }
        public float FogAmount { get; private set; }
        public float StormAmount { get; private set; }
        public const float ClearFogDensity = .0025f;
        public const float DenseFogDensity = .021f;
        public const float StormFogDensity = .030f;
        public float RainEmissionRate => RainAmount * Mathf.Lerp(1700, 3400, StormAmount);
        public string Conditions { get; private set; } = "CLEAR NIGHT";
        public void Initialize(GameManager manager, GameCatalog catalog)
        {
            game = manager;
            // The depth-aware URP pass owns fog for every imported material and the road.
            RenderSettings.fog = false;
            RenderSettings.fogDensity = ClearFogDensity;
            Shader.SetGlobalFloat("_AfterHoursFogDensity", ClearFogDensity);
            Shader.SetGlobalFloat("_AfterHoursFog", 0);
            Shader.SetGlobalColor("_AfterHoursFogColor", new Color(.065f, .09f, .115f));
            rain = DriveEffects.CreateParticles("Rain", transform, catalog.particleMaterial, new Color(.65f, .75f, .83f, .45f), 800, 1.1f, .025f, 25);
            rain.transform.rotation = Quaternion.Euler(90, 0, 0);
            var shape = rain.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(40, 65, 1);
            var renderer = rain.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 5; renderer.velocityScale = .03f;
            var main = rain.main; main.maxParticles = 5000;
            ResetWeather();
        }
        public static float StormTargetAt(float z)
        {
            int zone = ((Mathf.FloorToInt(z / 480) % 5) + 5) % 5;
            float local = Mathf.Repeat(z, 480);
            return zone == 2 ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(50, 125, local))
                * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(330, 430, local))) : 0;
        }
        public void ResetWeather()
        {
            RainAmount = FogAmount = StormAmount = 0; fireworkTimer = 17;
            Conditions = "CLEAR NIGHT"; RenderSettings.fogDensity = ClearFogDensity;
            Shader.SetGlobalFloat("_AfterHoursFogDensity", ClearFogDensity); Shader.SetGlobalFloat("_AfterHoursFog", 0);
            rain.Clear(); var emission = rain.emission; emission.rateOverTime = 0;
        }
        public void Tick(float dt)
        {
            float z = game.Driver.transform.position.z;
            int zone = ((Mathf.FloorToInt(z / 480) % 5) + 5) % 5;
            float targetRain = zone == 2 ? 1 : zone == 1 ? .65f : zone == 4 ? .35f : 0;
            RainAmount = Mathf.MoveTowards(RainAmount, targetRain, dt * .22f);
            StormAmount = Mathf.Lerp(StormAmount, StormTargetAt(z), 1 - Mathf.Exp(-dt * .6f));
            float local = Mathf.Repeat(z, 480);
            float patch = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(110, 185, local)) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(270, 350, local)));
            float targetFog = zone == 2 || zone == 3 ? 1 : patch;
            FogAmount = Mathf.Lerp(FogAmount, targetFog, 1 - Mathf.Exp(-dt * .6f));
            RenderSettings.fogDensity = Mathf.Lerp(Mathf.Lerp(ClearFogDensity, DenseFogDensity, FogAmount), StormFogDensity, StormAmount);
            RenderSettings.fogColor = new Color(.065f, .09f, .115f);
            Shader.SetGlobalFloat("_AfterHoursFogDensity", RenderSettings.fogDensity);
            Shader.SetGlobalFloat("_AfterHoursFog", FogAmount);
            Conditions = StormAmount > .45f ? "HEAVY RAIN / DENSE FOG" : FogAmount > .4f ? RainAmount > .3f ? "RAIN / LOW FOG" : "FOG PATCH" : RainAmount > .3f ? "RAIN" : "CLEAR NIGHT";
            rain.transform.position = game.Driver.transform.position + new Vector3(0, 16, 12);
            var emission = rain.emission; emission.rateOverTime = RainEmissionRate;
            var main = rain.main; main.startSpeed = Mathf.Lerp(25, 34, StormAmount); main.startSize = Mathf.Lerp(.025f, .035f, StormAmount);
            fireworkTimer -= dt;
            if (fireworkTimer <= 0)
            {
                fireworkTimer = Random.Range(20, 38);
                game.Effects.Burst(game.Driver.transform.position + new Vector3(Random.Range(-50, 50), Random.Range(24, 40), Random.Range(90, 150)), Color.HSVToRGB(Random.value, .6f, 1) * 3, 110);
            }
        }
    }
}
