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
        float trafficTimer;
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
            Warm(0, new[] { catalog.stationaryCar }, 48);
            Warm(1, catalog.movingTrafficVariants != null && catalog.movingTrafficVariants.Length > 0 ? catalog.movingTrafficVariants : new[] { catalog.movingCar }, 48);
            Warm(2, new[] { catalog.coin }, 144); Warm(3, new[] { catalog.fuel }, 24);
        }
        void Warm(int key, GameObject[] prefabs, int count)
        {
            var pool = new Stack<Interactable>(count); pools.Add(key, pool);
            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(prefabs[i % prefabs.Length], transform).GetComponent<Interactable>();
                if (instance is StationaryCar traffic) traffic.Initialize(catalog);
                instance.gameObject.SetActive(false); pool.Push(instance); poolKeys.Add(instance, key);
            }
        }
        Interactable Spawn(int key, Vector3 position)
        {
            if (pools[key].Count == 0) return null;
            var item = pools[key].Pop(); item.Activate(game, position); active.Add(item);
            return item;
        }
        void Release(int index)
        {
            var item = active[index]; item.gameObject.SetActive(false); pools[poolKeys[item]].Push(item);
            active.RemoveAt(index);
        }
        public void ResetWorld()
        {
            for (int i = active.Count - 1; i >= 0; i--) Release(i);
            furthestGenerated = -1; windowCenter = int.MinValue; trafficTimer = 4;
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
                    if (z < 0 && z > -80 && blocked == 1) blocked = 0;
                    Spawn(random.NextDouble() < .1 && index > 1 ? 0 : 1, new Vector3((blocked - 1) * CarDriver.LaneWidth, .08f, z));
                    if (game.Run.Difficulty > .2f)
                    {
                        int second = 3 - safeLane - blocked;
                        Spawn(1, new Vector3((second - 1) * CarDriver.LaneWidth, .08f, z));
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
        public float TrafficSpeed(StationaryCar car, float cruising)
        {
            float result = cruising;
            float playerGap = game.Driver.transform.position.z - car.transform.position.z;
            if (playerGap > 0 && playerGap < 32 && Mathf.Abs(game.Driver.transform.position.x - car.transform.position.x) < 1.8f)
                result = Mathf.Min(result, Mathf.Lerp(0, Mathf.Max(0, game.Driver.Speed), Mathf.InverseLerp(5, 25, playerGap)));
            foreach (var other in active)
            {
                if (other == car || !(other is StationaryCar ahead)) continue;
                float gap = ahead.transform.position.z - car.transform.position.z;
                if (gap <= 0 || gap > 30 || Mathf.Abs(ahead.transform.position.x - car.transform.position.x) > 1.8f) continue;
                result = Mathf.Min(result, Mathf.Lerp(0, Mathf.Max(0, ahead.Speed), Mathf.InverseLerp(5, 23, gap)));
            }
            return result;
        }
        public void SetTrafficAudioPaused(bool paused)
        {
            foreach (var item in active) if (item is StationaryCar car) car.SetAudioPaused(paused);
        }
        void StreamTraffic(float dt, Vector3 player)
        {
            trafficTimer -= dt;
            if (trafficTimer > 0) return;
            trafficTimer = Mathf.Lerp(6, 3.4f, game.Run.Difficulty);
            int section = Mathf.FloorToInt(player.z / 60);
            if ((section % 16 + 16) % 16 >= 11) return;
            // Overtakers approach in an adjacent lane, preserving a stopped player's reaction space.
            int lane = (game.Driver.Lane + (Random.value > .5f ? 1 : 2)) % 3;
            float x = (lane - 1) * CarDriver.LaneWidth;
            float z = player.z - 85;
            foreach (var item in active) if (item is StationaryCar && Mathf.Abs(item.transform.position.x - x) < 2 && Mathf.Abs(item.transform.position.z - z) < 30) return;
            var car = Spawn(1, new Vector3(x, .08f, z)) as MovingCar;
            if (car != null) car.SetCruisingSpeed(Mathf.Max(22, Mathf.Abs(game.Driver.Speed) + 5));
        }
        public void Tick(float dt)
        {
            Vector3 player = game.Driver.transform.position;
            MaintainWindow(player.z);
            if (dt > 0) StreamTraffic(dt, player);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var item = active[i];
                if (!item.gameObject.activeSelf) { Release(i); continue; }
                item.Tick(dt);
                Vector3 p = item.transform.position;
                float halfLength = item is StationaryCar ? 4.1f : 2.5f;
                // Swept longitudinal test also handles a dropped frame without tunneling through a pickup.
                float sweep = Mathf.Abs(game.Driver.Speed - (item is StationaryCar car ? car.Speed : 0)) * dt;
                if (Mathf.Abs(p.x - player.x) < (item is StationaryCar ? 1.65f : 1.3f) && Mathf.Abs(p.z - player.z) < halfLength + sweep)
                    item.Contact();
                if (p.z < (windowCenter - 7) * 60 || p.z > (windowCenter + 10) * 60) Release(i);
                if (game.State != GameState.Playing) break;
            }
        }
    }
}
