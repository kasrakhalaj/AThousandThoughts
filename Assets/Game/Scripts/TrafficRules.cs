using UnityEngine;

namespace AfterHours
{
    public enum TrafficFlow { Quiet, Sparse, Normal, Busy }

    // Shared safety calculations use metres and seconds for both spawns and driving decisions.
    public static class TrafficRules
    {
        public static TrafficFlow FlowAt(float z)
        {
            int section = Mathf.FloorToInt(z / 60);
            int cycle = Mathf.FloorToInt(section / 24f);
            int local = section - cycle * 24;
            int quietStart = 14 + (int)((uint)(cycle * 17471 + 42013) % 3);
            if (local >= quietStart && local < quietStart + 6) return TrafficFlow.Quiet;
            if (local < 4 || local >= quietStart - 2) return TrafficFlow.Sparse;
            return local >= 7 && local <= 9 ? TrafficFlow.Busy : TrafficFlow.Normal;
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

        public static bool SafeLaneGap(float signedGap, float speed, float otherSpeed, float headway)
        {
            bool ahead = signedGap >= 0;
            float closing = ahead ? Mathf.Max(0, speed - otherSpeed) : Mathf.Max(0, otherSpeed - speed);
            float followingSpeed = ahead ? speed : otherSpeed;
            float needed = 7 + Mathf.Max(0, followingSpeed) * headway * .65f
                + closing * 3.2f + closing * closing / 20;
            return Mathf.Abs(signedGap) > needed;
        }

        public static bool SafePlayerSpawn(float signedGap, float trafficSpeed, float playerSpeed)
        {
            float closing = signedGap >= 0 ? playerSpeed - trafficSpeed : trafficSpeed - playerSpeed;
            return Mathf.Abs(signedGap) >= Mathf.Max(75, 12 + Mathf.Max(0, closing) * 4);
        }
    }
}
