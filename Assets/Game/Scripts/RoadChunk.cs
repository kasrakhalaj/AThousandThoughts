using System;
using UnityEngine;

namespace AfterHours
{
    public sealed class RoadChunk : MonoBehaviour
    {
        public const float Length = 60;
        public int Index { get; private set; } = int.MinValue;
        Transform[] trees, rocks, houses, buildings, props, shrubs, hills;
        public void Initialize(GameCatalog catalog, int seed)
        {
            trees = Create(catalog.trees, 64, seed);
            rocks = Create(catalog.rocks, 10, seed + 1);
            houses = Create(catalog.houses, 8, seed + 2);
            buildings = Create(catalog.buildings, 6, seed + 3);
            props = Create(catalog.roadProps, 8, seed + 4);
            shrubs = Create(catalog.shrubs, 40, seed + 5);
            hills = Create(catalog.hills, 8, seed + 6);
        }
        Transform[] Create(GameObject[] models, int count, int seed)
        {
            if (models.Length == 0) return Array.Empty<Transform>();
            var result = new Transform[count];
            for (int i = 0; i < count; i++) result[i] = Instantiate(models[(i + seed) % models.Length], transform).transform;
            return result;
        }
        public void Place(int index)
        {
            Index = index; transform.position = new Vector3(0, 0, index * Length);
            var random = new System.Random(unchecked(index * 7919 + 51437));
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
