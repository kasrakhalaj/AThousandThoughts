using UnityEngine;

namespace AfterHours
{
    public static class VehicleDynamics
    {
        public const float ForwardLimit = 300f / 3.6f;
        public const float ReverseLimit = 10;
        const float BrakeDeceleration = 12;
        static readonly float[] GearBounds = { 0, 45f / 3.6f, 85f / 3.6f, 130f / 3.6f, 180f / 3.6f, 240f / 3.6f, ForwardLimit };
        public static float GearSpeedLimit(int gear) => GearBounds[Mathf.Clamp(gear, 1, 6)];
        public static float GearRpmRatio(float speed, int gear) => Mathf.Clamp01(Mathf.Abs(speed) / GearSpeedLimit(gear));

        public static float StepInGear(float speed, float throttle, bool braking, bool reverse, float limit, float seconds, int gear, bool shifting, float power = 1)
        {
            float remaining = Mathf.Max(0, seconds);
            while (remaining > 0)
            {
                float dt = Mathf.Min(.05f, remaining); remaining -= dt;
                float gearLimit = GearSpeedLimit(gear);
                float rpm = GearRpmRatio(speed, gear);
                float drive = gear == 1 ? 1 : Mathf.Lerp(.9f / gear, 1, Mathf.InverseLerp(.18f, .6f, rpm));
                float gas = shifting || !reverse && speed >= gearLimit ? 0 : throttle;
                float next = Step(speed, gas, braking, reverse, limit, dt, power * drive);
                // The limiter cuts power, without snapping an already coasting car to a lower speed.
                if (!reverse && speed >= 0 && speed <= gearLimit && next > gearLimit) next = gearLimit;
                speed = next;
            }
            return speed;
        }

        public static float Step(float speed, float throttle, bool braking, bool reverse, float limit, float seconds, float power = 1)
        {
            // Small substeps make drag/torque consistent during both normal and dropped frames.
            float remaining = Mathf.Max(0, seconds);
            throttle = Mathf.Clamp01(throttle);
            while (remaining > 0)
            {
                float dt = Mathf.Min(.05f, remaining); remaining -= dt;
                bool changingDirection = reverse ? speed > 0 : speed < 0;
                if (braking || changingDirection) { speed = Mathf.MoveTowards(speed, 0, BrakeDeceleration * dt); continue; }
                float magnitude = Mathf.Abs(speed);
                float drag = .32f + .00011f * magnitude * magnitude;
                if (throttle <= .001f) { speed = Mathf.MoveTowards(speed, 0, drag * dt); continue; }
                float top = reverse ? ReverseLimit : Mathf.Min(ForwardLimit, Mathf.Max(0, limit));
                float torque = reverse ? 3.8f : Mathf.Lerp(5.6f, 1.6f, Mathf.Pow(Mathf.Clamp01(magnitude / ForwardLimit), 1.15f)) * power;
                magnitude = Mathf.Clamp(magnitude + (torque * throttle - drag) * dt, 0, top);
                speed = (reverse ? -1 : 1) * magnitude;
            }
            return speed;
        }

        public static int Gear(float speed)
        {
            for (int i = 1; i < GearBounds.Length - 1; i++) if (Mathf.Abs(speed) < GearBounds[i]) return i;
            return 6;
        }
        public static float GearProgress(float speed)
        {
            int gear = Gear(speed);
            return Mathf.InverseLerp(GearBounds[gear - 1], GearBounds[gear], Mathf.Abs(speed));
        }

        public static bool SweptContact(Vector2 start, Vector2 end, float halfWidth, float halfLength)
        {
            float enter = 0, exit = 1;
            return AxisContact(start.x, end.x - start.x, halfWidth, ref enter, ref exit)
                && AxisContact(start.y, end.y - start.y, halfLength, ref enter, ref exit);
        }
        static bool AxisContact(float start, float delta, float half, ref float enter, ref float exit)
        {
            if (Mathf.Abs(delta) < .00001f) return Mathf.Abs(start) <= half;
            float first = (-half - start) / delta, last = (half - start) / delta;
            enter = Mathf.Max(enter, Mathf.Min(first, last)); exit = Mathf.Min(exit, Mathf.Max(first, last));
            return enter <= exit;
        }
    }
}
