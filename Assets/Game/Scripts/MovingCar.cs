using UnityEngine;

namespace AfterHours
{
    public enum TrafficBehavior { Cruising, Following, Signaling, ChangingLane, Recovering }
    public enum TrafficDriverStyle { Cautious, Balanced, Assertive }

    public sealed class MovingCar : StationaryCar
    {
        float targetX, decisionTime, signalTime, recoverTime;
        float cruisingSpeed, speed;
        int targetLane;
        Light signal;
        public override float Speed => speed;
        public TrafficBehavior Behavior { get; private set; }
        public TrafficDriverStyle Style { get; private set; }
        public bool IsFastDriver { get; private set; }
        public float DesiredSpeed => TrafficRules.PaceSpeed(cruisingSpeed, IsFastDriver, TrafficRules.FastPaceAt(transform.position.z)) * game.World.TrafficWeatherFactor;
        public float Headway => Style == TrafficDriverStyle.Cautious ? 1.8f : Style == TrafficDriverStyle.Assertive ? 1.1f : 1.45f;
        public int Lane => Mathf.Clamp(Mathf.RoundToInt(transform.position.x / CarDriver.LaneWidth) + 1, 0, 2);
        public int ReservedLane => IsMerging ? targetLane : Lane;
        public bool IsMerging => Behavior == TrafficBehavior.Signaling || Behavior == TrafficBehavior.ChangingLane;
        public bool OccupiesLane(int lane) => Mathf.Abs(transform.position.x - (lane - 1) * CarDriver.LaneWidth) < 2.05f || IsMerging && lane == targetLane;

        public override void Activate(GameManager owner, Vector3 position)
        {
            base.Activate(owner, position);
            transform.rotation = Quaternion.identity;
            targetLane = Lane; targetX = position.x;
            signalTime = 0; recoverTime = 0; Behavior = TrafficBehavior.Cruising;
            Style = (TrafficDriverStyle)Random.Range(0, 3);
            IsFastDriver = false;
            cruisingSpeed = (Random.Range(15f, 26f) + owner.Run.Difficulty * 6) * (Style == TrafficDriverStyle.Cautious ? .9f : Style == TrafficDriverStyle.Assertive ? 1.07f : 1);
            speed = cruisingSpeed;
            decisionTime = Random.Range(1.5f, 3.5f);
            if (signal == null) signal = transform.Find("Turn indicator").GetComponent<Light>();
            signal.enabled = false;
        }

        public override void Tick(float dt)
        {
            float desired = DesiredSpeed;
            float safeSpeed = game.World.TrafficSpeed(this, desired);
            braking = safeSpeed < speed - .5f;
            speed = Mathf.MoveTowards(speed, safeSpeed, (braking ? safeSpeed < speed - 6 ? 24 : 7 : 3.5f) * dt);
            base.Tick(dt);
            if (dt <= 0) return;

            if (Behavior == TrafficBehavior.Signaling)
            {
                // Other drivers see this reservation. Cancel if traffic or the player enters it.
                if (!game.World.CanMerge(this, targetX)) CancelMerge();
                else if ((signalTime -= dt) <= 0) Behavior = TrafficBehavior.ChangingLane;
            }
            else if (Behavior == TrafficBehavior.Recovering)
            {
                recoverTime -= dt;
                if (recoverTime <= 0) Behavior = TrafficBehavior.Cruising;
            }
            else if (Behavior != TrafficBehavior.ChangingLane)
            {
                Behavior = safeSpeed < desired - 2 ? TrafficBehavior.Following : TrafficBehavior.Cruising;
                if (age >= decisionTime)
                {
                    decisionTime = age + Random.Range(.45f, .8f);
                    if (game.World.TryChooseTrafficLane(this, desired, safeSpeed, out int next))
                    {
                        targetLane = next; targetX = (next - 1) * CarDriver.LaneWidth;
                        signalTime = Style == TrafficDriverStyle.Cautious ? 1.6f : 1.2f;
                        Behavior = TrafficBehavior.Signaling;
                    }
                }
            }

            Vector3 p = transform.position;
            float oldX = p.x;
            if (Behavior == TrafficBehavior.ChangingLane)
            {
                // Both lanes remain occupied while crossing; sense obstacles in both each tick.
                p.x = Mathf.MoveTowards(p.x, targetX, dt * 2.1f);
                if (Mathf.Abs(p.x - targetX) < .01f)
                {
                    p.x = targetX; Behavior = TrafficBehavior.Recovering; recoverTime = Random.Range(4, 7);
                }
            }
            p.z += speed * dt; transform.position = p;
            float yaw = Mathf.Atan2((p.x - oldX) / dt, Mathf.Max(5, speed)) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yaw, 0), dt * 5);
            signal.enabled = IsMerging && Mathf.Sin(age * 12) > 0;
            signal.transform.localPosition = new Vector3(targetLane > Lane || targetX > p.x ? .95f : -.95f, .65f, -1.8f);
        }

        void CancelMerge()
        {
            targetLane = Lane; targetX = (Lane - 1) * CarDriver.LaneWidth;
            Behavior = TrafficBehavior.Recovering; recoverTime = 1.5f; signalTime = 0;
        }

        public void SetCruisingSpeed(float value, bool fastDriver = false)
        {
            cruisingSpeed = Mathf.Clamp(value, 10, 38); IsFastDriver = fastDriver;
            if (fastDriver) Style = TrafficDriverStyle.Assertive;
            speed = DesiredSpeed;
        }
    }
}
