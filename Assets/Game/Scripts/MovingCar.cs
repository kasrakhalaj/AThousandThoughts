using UnityEngine;
namespace AfterHours
{
    public sealed class MovingCar : StationaryCar
    {
        float targetX;
        float decisionTime;
        float signalTime;
        Light signal;
        public override void Activate(GameManager owner, Vector3 position)
        {
            base.Activate(owner, position); targetX = position.x; signalTime = 0;
            decisionTime = Random.Range(4, 12);
            if (signal == null) signal = GetComponentInChildren<Light>();
            if (signal != null) signal.enabled = false;
        }
        public override void Tick(float dt)
        {
            base.Tick(dt);
            var p = transform.position; p.z += 7 * dt;
            float distance = Mathf.Abs(p.z - game.Driver.transform.position.z);
            if (age > decisionTime && signalTime == 0 && distance > 35 && distance < 130)
            {
                int lane = Mathf.RoundToInt(p.x / CarDriver.LaneWidth) + 1;
                int next = Mathf.Clamp(lane + (Random.value > .5f ? 1 : -1), 0, 2);
                float proposed = (next - 1) * CarDriver.LaneWidth;
                if (next != lane && game.World.CanMerge(this, proposed)) { targetX = proposed; signalTime = 1.4f; }
                decisionTime = age + 10;
            }
            if (signalTime > 0)
            {
                signalTime = Mathf.Max(0, signalTime - dt);
                if (signal != null) { signal.enabled = Mathf.Sin(age * 14) > 0; signal.transform.localPosition = new Vector3(targetX > p.x ? .95f : -.95f, .65f, -1.8f); }
            }
            else
            {
                p.x = Mathf.MoveTowards(p.x, targetX, dt * 1.9f);
                if (signal != null) signal.enabled = false;
            }
            transform.position = p;
        }
    }
}
