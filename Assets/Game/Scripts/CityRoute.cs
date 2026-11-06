using UnityEngine;

namespace AfterHours
{
    public enum RoadDistrict { Downtown, Residential, Industrial, Woodland }

    public static class CityRoute
    {
        public const int DistrictChunks = 8;
        public const int CycleChunks = 32;
        public static int CycleIndex(int chunk) => (chunk % CycleChunks + CycleChunks) % CycleChunks;
        public static int Phase(int chunk) => CycleIndex(chunk) % DistrictChunks;
        public static RoadDistrict DistrictAt(int chunk) => (RoadDistrict)(CycleIndex(chunk) / DistrictChunks);
        public static RoadDistrict NextDistrict(int chunk) => (RoadDistrict)(((int)DistrictAt(chunk) + 1) % 4);
        public static bool IsTransition(int chunk) => Phase(chunk) == DistrictChunks - 1;
        public static int LandmarkAt(int chunk)
        {
            switch (CycleIndex(chunk))
            {
                case 2: return 0;  // Night Owl cafe.
                case 11: return 1; // Maple pocket park.
                case 19: return 2; // East Works service station.
                case 6: return 3;  // Cross-city overpass.
                default: return -1;
            }
        }
        public static int BlockVariantAt(int chunk)
        {
            int landmark = LandmarkAt(chunk);
            if (landmark >= 0 && landmark <= 2) return 2; // Clear the right-side landmark lot.
            return Phase(chunk) == 1 || Phase(chunk) == 4 || landmark == 3 ? 1 : 0;
        }
        public static string DistrictName(RoadDistrict district)
        {
            switch (district)
            {
                case RoadDistrict.Downtown: return "CITY CENTER";
                case RoadDistrict.Residential: return "MAPLE HEIGHTS";
                case RoadDistrict.Industrial: return "EAST WORKS";
                default: return "PINE RIDGE";
            }
        }
    }
}
