using System;
using UnityEngine;

namespace AfterHours
{
    public sealed class RoadChunk : MonoBehaviour
    {
        public const float Length = 60;
        public int Index { get; private set; } = int.MinValue;
        Transform[] trees, rocks, houses, buildings, props;
        public void Initialize(GameCatalog catalog, int seed)
        {
            trees = Create(catalog.trees, 12, seed);
            rocks = Create(catalog.rocks, 5, seed + 1);
            houses = Create(catalog.houses, 3, seed + 2);
            buildings = Create(catalog.buildings, 3, seed + 3);
            props = Create(catalog.roadProps, 4, seed + 4);
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
            PlaceGroup(trees, random, biome != 0, 12, 35, 1);
            PlaceGroup(rocks, random, true, 9, 24, 1);
            PlaceGroup(houses, random, biome == 0, 16, 30, 1);
            PlaceGroup(buildings, random, biome == 0, 36, 48, 1);
            PlaceGroup(props, random, true, 7, 8.5f, .7f);
        }
        void PlaceGroup(Transform[] objects, System.Random random, bool enabled, float minX, float maxX, float scale)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                var item = objects[i]; item.gameObject.SetActive(enabled);
                float x = Mathf.Lerp(minX, maxX, (float)random.NextDouble()) * (i % 2 == 0 ? -1 : 1);
                item.localPosition = new Vector3(x, 0, (i + .2f + (float)random.NextDouble() * .5f) / objects.Length * Length);
                item.localRotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
                float size = scale * (.85f + (float)random.NextDouble() * .35f);
                item.localScale = Vector3.one * size;
            }
        }
    }
}
