using System;
using UnityEngine;

namespace AfterHours
{
    public sealed class RoadChunk : MonoBehaviour
    {
        public const float Length = 60;
        public int Index { get; private set; } = int.MinValue;
        Transform[] trees, rocks, houses, buildings, props, shrubs, hills;
        Transform[][] cityBlocks;
        Transform[] transitions, entrances, landmarks, rails, streetlights;
        bool cityReady;
        static readonly int[][] NatureCounts = { new[] { 8, 24, 10, 64 }, new[] { 0, 2, 4, 10 }, new[] { 8, 20, 8, 40 }, new[] { 4, 6, 6, 8 } };
        static readonly float[] TreeSetbacks = { 53, 47, 50, 14 };
        public RoadDistrict District => CityRoute.DistrictAt(Index);
        public void Initialize(GameCatalog catalog, int seed)
        {
            var railList = new System.Collections.Generic.List<Transform>();
            var lampList = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name == "Guardrail" || child.name == "Rail post" || child.name == "Reflector") railList.Add(child);
                if (child.name == "Warm streetlight") lampList.Add(child);
            }
            rails = railList.ToArray(); streetlights = lampList.ToArray();
            trees = Create(catalog.trees, 64, seed);
            rocks = Create(catalog.rocks, 10, seed + 1);
            houses = Create(catalog.houses, 8, seed + 2);
            buildings = Create(catalog.buildings, 6, seed + 3);
            props = Create(catalog.roadProps, 8, seed + 4);
            shrubs = Create(catalog.shrubs, 40, seed + 5);
            hills = Create(catalog.hills, 8, seed + 6);
            cityReady = catalog.downtownBlocks != null && catalog.downtownBlocks.Length >= 3
                && catalog.residentialBlocks != null && catalog.residentialBlocks.Length >= 3
                && catalog.industrialBlocks != null && catalog.industrialBlocks.Length >= 3
                && catalog.woodlandBlock != null && catalog.districtTransitions != null && catalog.districtTransitions.Length == 4
                && catalog.districtEntrances != null && catalog.districtEntrances.Length == 4
                && catalog.cityLandmarks != null && catalog.cityLandmarks.Length == 4;
            if (cityReady)
            {
                cityBlocks = new[] { Create(catalog.downtownBlocks, catalog.downtownBlocks.Length, 0),
                    Create(catalog.residentialBlocks, catalog.residentialBlocks.Length, 0),
                    Create(catalog.industrialBlocks, catalog.industrialBlocks.Length, 0), Create(new[] { catalog.woodlandBlock }, 1, 0) };
                transitions = Create(catalog.districtTransitions, catalog.districtTransitions.Length, 0);
                entrances = Create(catalog.districtEntrances, catalog.districtEntrances.Length, 0);
                landmarks = Create(catalog.cityLandmarks, catalog.cityLandmarks.Length, 0);
            }
        }
        Transform[] Create(GameObject[] models, int count, int seed)
        {
            if (models == null || models.Length == 0) return Array.Empty<Transform>();
            var result = new Transform[count];
            for (int i = 0; i < count; i++) result[i] = Instantiate(models[(i + seed) % models.Length], transform).transform;
            return result;
        }
        public void Place(int index)
        {
            Index = index; transform.position = new Vector3(0, 0, index * Length);
            var random = new System.Random(unchecked(index * 7919 + 51437));
            if (cityReady) { PlaceCity(index, random); return; }
            int biome = ((int)Math.Floor(index / 6.0) % 3 + 3) % 3;
            PlaceGroup(trees, random, true, biome == 0 ? 24 : 10, 44, biome == 0 ? .8f : 1);
            PlaceGroup(rocks, random, true, 9, 30, .65f);
            PlaceGroup(houses, random, biome == 0, 13, 24, 1);
            PlaceGroup(buildings, random, biome == 0, 34, 49, 1);
            PlaceGroup(props, random, true, 7.5f, 9, .8f);
            PlaceGroup(shrubs, random, true, 8.5f, 25, 1);
            PlaceGroup(hills, random, true, 58, 94, 1);
            for (int i = 0; i < houses.Length; i++) houses[i].localRotation = Quaternion.Euler(0, houses[i].localPosition.x < 0 ? 90 : -90, 0);
            for (int i = 0; i < hills.Length; i++) hills[i].localScale = new Vector3(25 + (float)random.NextDouble() * 24, 9 + (float)random.NextDouble() * 12, 22 + (float)random.NextDouble() * 18);
        }
        void PlaceCity(int index, System.Random random)
        {
            int district = (int)CityRoute.DistrictAt(index);
            bool transition = CityRoute.IsTransition(index);
            foreach (var bank in cityBlocks) foreach (var block in bank) block.gameObject.SetActive(false);
            foreach (var block in transitions) block.gameObject.SetActive(false);
            foreach (var sign in entrances) sign.gameObject.SetActive(false);
            foreach (var landmark in landmarks) landmark.gameObject.SetActive(false);
            var selected = transition ? transitions[district] : cityBlocks[district][district == 3 ? 0 : CityRoute.BlockVariantAt(index)];
            selected.gameObject.SetActive(true); selected.localPosition = Vector3.zero; selected.localRotation = Quaternion.identity;
            if (CityRoute.Phase(index) == 0 && entrances.Length > district) entrances[district].gameObject.SetActive(true);
            int feature = CityRoute.LandmarkAt(index);
            if (feature >= 0 && feature < landmarks.Length) landmarks[feature].gameObject.SetActive(true);
            foreach (var rail in rails) rail.gameObject.SetActive(district == 3 && !transition);
            for (int i = 0; i < streetlights.Length; i++) streetlights[i].gameObject.SetActive(district != 3 || CityRoute.Phase(index) == 0 && i == 0);
            foreach (var item in houses) item.gameObject.SetActive(false);
            foreach (var item in buildings) item.gameObject.SetActive(false);
            foreach (var item in props) item.gameObject.SetActive(false);
            PlaceNature(trees, random, index, 0);
            PlaceNature(rocks, random, index, 1);
            PlaceNature(shrubs, random, index, 2);
            PlaceNature(hills, random, index, 3);
        }
        void PlaceNature(Transform[] items, System.Random random, int index, int kind)
        {
            for (int i = 0; i < items.Length; i++)
            {
                float z = (i / 2 + .15f + (float)random.NextDouble() * .5f) / ((items.Length + 1) / 2) * Length;
                int district = (int)(CityRoute.IsTransition(index) && z >= 30 ? CityRoute.NextDistrict(index) : CityRoute.DistrictAt(index));
                int count = NatureCounts[kind][district];
                bool enabled = (i * 13 + (index % items.Length + items.Length) % items.Length) % items.Length < count;
                var item = items[i]; item.gameObject.SetActive(enabled);
                float min = kind == 0 ? TreeSetbacks[district] : kind == 1 ? district == 3 ? 9.5f : 50
                    : kind == 2 ? district == 3 ? 8.8f : 48 : district == 3 ? 58 : 80;
                float max = kind == 0 ? district == 3 ? 44 : 68 : kind == 1 ? district == 3 ? 34 : 65
                    : kind == 2 ? district == 3 ? 28 : 65 : district == 3 ? 100 : 110;
                float x = Mathf.Lerp(min, max, (float)random.NextDouble()) * (i % 2 == 0 ? -1 : 1);
                item.localPosition = new Vector3(x, 0, z); item.localRotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
                float scale = .85f + (float)random.NextDouble() * .3f;
                item.localScale = kind == 3 ? new Vector3(28 + (float)random.NextDouble() * 20, 9 + (float)random.NextDouble() * 8, 24 + (float)random.NextDouble() * 16)
                    : Vector3.one * scale * (kind == 1 ? .65f : 1);
            }
        }
        void PlaceGroup(Transform[] objects, System.Random random, bool enabled, float minX, float maxX, float scale)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                var item = objects[i]; item.gameObject.SetActive(enabled);
                float x = Mathf.Lerp(minX, maxX, (float)random.NextDouble()) * (i % 2 == 0 ? -1 : 1);
                // Independent rows on each roadside avoid the old sparse diagonal arrangement.
                int row = i / 2;
                int rows = (objects.Length + 1) / 2;
                item.localPosition = new Vector3(x, 0, (row + .15f + (float)random.NextDouble() * .5f) / rows * Length);
                item.localRotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
                float size = scale * (.85f + (float)random.NextDouble() * .35f);
                item.localScale = Vector3.one * size;
            }
        }
    }
}
