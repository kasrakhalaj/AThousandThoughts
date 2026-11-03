using UnityEngine;

namespace AfterHours
{
    public sealed class DriveEffects : MonoBehaviour
    {
        ParticleSystem[] bursts;
        int next;
        public static ParticleSystem CreateParticles(string name, Transform parent, Material material, Color color, float rate, float lifetime, float size, float speed)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.duration = 5; main.loop = true; main.startLifetime = lifetime;
            main.startSize = size; main.startSpeed = speed; main.startColor = color;
            main.maxParticles = 1600; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 18; shape.radius = .08f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            ps.Play(); return ps;
        }
        public void Initialize(GameCatalog catalog)
        {
            bursts = new ParticleSystem[12];
            for (int i = 0; i < bursts.Length; i++)
            {
                var ps = CreateParticles("Pooled spark burst", transform, catalog.particleMaterial, new Color(1, .65f, .14f), 0, .7f, .09f, 6);
                var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere;
                var main = ps.main; main.gravityModifier = .6f;
                bursts[i] = ps;
            }
        }
        public void Burst(Vector3 position, Color color, int count = 24)
        {
            var ps = bursts[next++ % bursts.Length]; ps.transform.position = position;
            bool sky = position.y > 15;
            var main = ps.main; main.startColor = color;
            main.startSize = sky ? .42f : .09f; main.startSpeed = sky ? 12 : 6;
            main.startLifetime = sky ? 1.8f : .7f; main.gravityModifier = sky ? .15f : .6f;
            ps.Emit(count);
        }
    }
}
