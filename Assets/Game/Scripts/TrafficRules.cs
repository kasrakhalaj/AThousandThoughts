using UnityEngine;

namespace AfterHours
{
    public enum TrafficFlow { Quiet, Sparse, Normal, Busy }

    // Shared safety calculations use metres and seconds for both spawns and driving decisions.
    public static class TrafficRules
    {
        public const float MaximumTrafficSpeed = 56; // About 200 km/h, before weather/following limits.
        public const float FormationLookahead = 4;
        public static float RowSpacing(float playerSpeed, float rowSpeed) =>
            Mathf.Clamp(16 + Mathf.Abs(playerSpeed - rowSpeed) * (.4f + 2 * CarDriver.LaneTime), 24, 90);
        public static float StaggerSpeed(float desired, float rowSpeed, float gap, float spacing)
        {
            float lead = Mathf.Max(0, rowSpeed);
            float target = lead + (gap - spacing) * .8f;
            float stopping = Mathf.Sqrt(lead * lead + 14 * Mathf.Max(0, gap - spacing));
            return Mathf.Clamp(Mathf.Min(target, stopping), 0, desired);
        }
        public static float FastPaceAt(float z)
        {
            float local = Mathf.Repeat(z, 1440);
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(210, 300, local))
                * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(500, 600, local)));
        }
        public static float PaceSpeed(float cruisingSpeed, bool fastDriver, float pace)
        {
            float baseline = Mathf.Clamp(cruisingSpeed, 10, 38); pace = Mathf.Clamp01(pace);
            return Mathf.Min(MaximumTrafficSpeed, fastDriver ? baseline + 12 + 14 * pace : baseline);
        }

        public static TrafficFlow FlowAt(float z)
        {
            int section = Mathf.FloorToInt(z / 60);
            int cycle = Mathf.FloorToInt(section / 24f);
            int local = section - cycle * 24;
            int quietStart = 14 + (int)((uint)(cycle * 17471 + 42013) % 3);
            if (local >= quietStart && local < quietStart + 3) return TrafficFlow.Quiet;
            if (local == 0 || local == quietStart - 1 || local == quietStart + 3) return TrafficFlow.Sparse;
            return local >= 4 && local <= 9 ? TrafficFlow.Busy : TrafficFlow.Normal;
        }

        public static bool HiddenByFog(float cameraDistance, float density)
        {
            // Match RoadFog's squared exponential, conservatively using the top of a 2m car.
            // At this threshold less than 0.01% of the vehicle color survives the fog pass.
            return Mathf.Max(0, cameraDistance - 20) * density * Mathf.Exp(-.05f) >= 3.1f;
        }

        public static float WeatherSpeedFactor(float rain, float fog) => 1 - .18f * rain - .23f * fog;

        public static float FollowingSpeed(float desired, float speed, float leadSpeed, float gap, float headway)
        {
            if (gap <= 6) return 0;
            float lead = Mathf.Max(0, leadSpeed);
            float following = lead + (gap - 6 - speed * headway) * .65f;
            float stopping = Mathf.Sqrt(lead * lead + 2 * 8 * Mathf.Max(0, gap - 6));
            return Mathf.Clamp(Mathf.Min(following, stopping), 0, desired);
        }

        public static bool SafeLaneGap(float signedGap, float speed, float otherSpeed, float headway, float anticipation = 3.2f)
        {
            bool ahead = signedGap >= 0;
            float closing = ahead ? Mathf.Max(0, speed - otherSpeed) : Mathf.Max(0, otherSpeed - speed);
            float followingSpeed = ahead ? speed : otherSpeed;
            float needed = 7 + Mathf.Max(0, followingSpeed) * headway * .65f
                + closing * anticipation + closing * closing / 20;
            return Mathf.Abs(signedGap) > needed;
        }

        public static bool SafePlayerSpawn(float signedGap, float trafficSpeed, float playerSpeed)
        {
            float closing = signedGap >= 0 ? playerSpeed - trafficSpeed : trafficSpeed - playerSpeed;
            return Mathf.Abs(signedGap) >= Mathf.Max(75, 12 + Mathf.Max(0, closing) * 4);
        }
    }
}
