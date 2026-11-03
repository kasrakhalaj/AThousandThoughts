using System.Collections.Generic;
using UnityEngine;

namespace AfterHours
{
    public sealed class RoadWorld : MonoBehaviour
    {
        const int ChunkCount = 17;
        readonly List<RoadChunk> chunks = new List<RoadChunk>();
        readonly List<Interactable> active = new List<Interactable>(256);
        readonly Dictionary<int, Stack<Interactable>> pools = new Dictionary<int, Stack<Interactable>>();
        readonly Dictionary<Interactable, int> poolKeys = new Dictionary<Interactable, int>();
        GameManager game;
        GameCatalog catalog;
        int furthestGenerated;
        int windowCenter = int.MinValue;
        public int ActiveCount => active.Count;
        public int PooledCount => poolKeys.Count;

        public void Initialize(GameManager owner, GameCatalog data)
        {
            game = owner; catalog = data;
            for (int i = 0; i < ChunkCount; i++)
            {
                var road = Instantiate(catalog.roadSegment, transform).AddComponent<RoadChunk>();
                road.Initialize(catalog, i); chunks.Add(road);
            }
            Warm(0, catalog.stationaryCar, 48); Warm(1, catalog.movingCar, 48);
            Warm(2, catalog.coin, 144); Warm(3, catalog.fuel, 24);
        }
        void Warm(int key, GameObject prefab, int count)
        {
            var pool = new Stack<Interactable>(count); pools.Add(key, pool);
            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(prefab, transform).GetComponent<Interactable>();
                instance.gameObject.SetActive(false); pool.Push(instance); poolKeys.Add(instance, key);
            }
        }
        void Spawn(int key, Vector3 position)
        {
            if (pools[key].Count == 0) return;
            var item = pools[key].Pop(); item.Activate(game, position); active.Add(item);
        }
        void Release(int index)
        {
            var item = active[index]; item.gameObject.SetActive(false); pools[poolKeys[item]].Push(item);
            active.RemoveAt(index);
        }
        public void ResetWorld()
        {
            for (int i = active.Count - 1; i >= 0; i--) Release(i);
            furthestGenerated = -1; windowCenter = int.MinValue;
            foreach (var chunk in chunks) chunk.gameObject.SetActive(false);
            MaintainWindow(0);
        }
        void MaintainWindow(float z)
        {
            int center = Mathf.FloorToInt(z / RoadChunk.Length);
            if (center == windowCenter) return;
            windowCenter = center;
            int min = center - 7, max = min + ChunkCount - 1;
            // Return old occupants before assigning newly visible chunks, keeping pool capacity bounded.
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i].transform.position.z < min * 60 || active[i].transform.position.z > (max + 1) * 60) Release(i);
            for (int index = min; index <= max; index++)
            {
                bool found = false;
                foreach (var chunk in chunks) if (chunk.gameObject.activeSelf && chunk.Index == index) { found = true; break; }
                if (found) continue;
                foreach (var chunk in chunks)
                {
                    if (!chunk.gameObject.activeSelf || chunk.Index < min || chunk.Index > max)
                    {
                        chunk.gameObject.SetActive(true); chunk.Place(index);
                        bool fresh = index > furthestGenerated;
                        Populate(index, fresh);
                        furthestGenerated = Mathf.Max(furthestGenerated, index);
                        break;
                    }
                }
            }
        }
        void Populate(int index, bool fresh)
        {
            var random = new System.Random(unchecked(index * 17471 + 42013));
            // Each eighth stretch is deliberately empty. The first meters are safe for ignition.
            bool quiet = ((index % 16 + 16) % 16 >= 11);
            if (!quiet)
            {
                int rows = game.Run.Difficulty > .45f ? 2 : 1;
                for (int row = 0; row < rows; row++)
                {
                    float z = index * 60 + (rows == 1 ? 36 : 22 + row * 30);
                    int safeLane = ((index + row) % 3 + 3) % 3;
                    int blocked = random.Next(3);
                    if (blocked == safeLane) blocked = (blocked + 1) % 3;
                    Spawn(random.NextDouble() > .45 ? 1 : 0, new Vector3((blocked - 1) * CarDriver.LaneWidth, .08f, z));
                    if (game.Run.Difficulty > .2f)
                    {
                        int second = 3 - safeLane - blocked;
                        Spawn(0, new Vector3((second - 1) * CarDriver.LaneWidth, .08f, z));
                    }
                    if (fresh) for (int c = 0; c < 3; c++) Spawn(2, new Vector3((safeLane - 1) * CarDriver.LaneWidth, .85f, z + c * 4));
                }
            }
            if (fresh && index >= 0 && index % 5 == 2)
                Spawn(3, new Vector3((random.Next(3) - 1) * CarDriver.LaneWidth, .6f, index * 60 + 10));
        }
        public bool CanMerge(Interactable car, float targetX)
        {
            // Reserve an empty target lane plus a second escape lane around the merging car.
            int occupied = 0;
            foreach (var other in active)
            {
                if (other == car || !(other is StationaryCar) || !other.gameObject.activeSelf) continue;
                if (Mathf.Abs(other.transform.position.z - car.transform.position.z) < 24)
                {
                    if (Mathf.Abs(other.transform.position.x - targetX) < 2) return false;
                    occupied++;
                }
            }
            return occupied == 0;
        }
        public void Tick(float dt)
        {
            Vector3 player = game.Driver.transform.position;
            MaintainWindow(player.z);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var item = active[i];
                if (!item.gameObject.activeSelf) { Release(i); continue; }
                item.Tick(dt);
                Vector3 p = item.transform.position;
                float halfLength = item is StationaryCar ? 4.1f : 2.5f;
                // Swept longitudinal test also handles a dropped frame without tunneling through a pickup.
                float sweep = Mathf.Abs(game.Driver.Speed) * dt;
                if (Mathf.Abs(p.x - player.x) < (item is StationaryCar ? 1.65f : 1.3f) && Mathf.Abs(p.z - player.z) < halfLength + sweep)
                    item.Contact();
                if (p.z < (windowCenter - 7) * 60 || p.z > (windowCenter + 10) * 60) Release(i);
                if (game.State != GameState.Playing) break;
            }
        }
    }
}
