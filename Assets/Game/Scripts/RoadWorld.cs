using System.Collections.Generic;
using UnityEngine;

namespace AfterHours
{
    public sealed class RoadWorld : MonoBehaviour
    {
        const int ChunkCount = 17;
        readonly List<RoadChunk> chunks = new List<RoadChunk>();
        readonly List<Interactable> active = new List<Interactable>(120);
        readonly List<StationaryCar> traffic = new List<StationaryCar>(96);
        readonly Dictionary<int, Queue<Interactable>> pools = new Dictionary<int, Queue<Interactable>>();
        readonly Dictionary<Interactable, int> poolKeys = new Dictionary<Interactable, int>();
        GameManager game;
        GameCatalog catalog;
        Camera trafficCamera;
        int furthestGenerated;
        int windowCenter = int.MinValue, windowDirection, minSection, maxSection;
        float trafficTimer;
        bool populatingInitial;
        struct TrafficSample
        {
            public StationaryCar Car;
            public float Z, Speed;
            public int Lanes, Id;
        }
        readonly TrafficSample[] trafficSamples = new TrafficSample[96];
        int sampleCount;
        bool tickingTraffic;
        public int ActiveCount => active.Count;
        public int ActiveTrafficCount => traffic.Count;
        public int PooledCount => poolKeys.Count;
        public IReadOnlyList<StationaryCar> Traffic => traffic;
        public TrafficFlow Flow => TrafficRules.FlowAt(game.Driver.transform.position.z);
        public float TrafficWeatherFactor => game.Weather == null ? 1 : TrafficRules.WeatherSpeedFactor(game.Weather.RainAmount, game.Weather.FogAmount);
        float WeatherHeadway => game.Weather == null ? 1 : 1 + game.Weather.RainAmount * .35f + game.Weather.FogAmount * .5f;
        float SpeedPressure => Mathf.InverseLerp(100, 300, Mathf.Abs(game.Driver.Speed) * 3.6f);
        int TrafficBudget => Flow == TrafficFlow.Quiet ? 0 : Mathf.RoundToInt((Flow == TrafficFlow.Sparse ? 7 : Flow == TrafficFlow.Busy ? 16 : 12) + game.Run.Difficulty * 4 + SpeedPressure * 3);
        public int NearbyTrafficCount
        {
            get
            {
                int count = 0;
                float direction = game.Driver.RearView ? -1 : 1;
                foreach (var car in traffic)
                {
                    float gap = (car.transform.position.z - game.Driver.transform.position.z) * direction;
                    if (gap > -100 && gap < Mathf.Max(300, Mathf.Abs(game.Driver.Speed) * 5)) count++;
                }
                return count;
            }
        }

        public void Initialize(GameManager owner, GameCatalog data)
        {
            game = owner; catalog = data; trafficCamera = Camera.main;
            for (int i = 0; i < ChunkCount; i++)
            {
                var road = Instantiate(catalog.roadSegment, transform).AddComponent<RoadChunk>();
                road.Initialize(catalog, i); chunks.Add(road);
            }
            Warm(0, new[] { catalog.stationaryCar }, 48);
            Warm(1, catalog.movingTrafficVariants != null && catalog.movingTrafficVariants.Length > 0 ? catalog.movingTrafficVariants : new[] { catalog.movingCar }, 48);
            Warm(2, new[] { catalog.fuel }, 24);
        }
        void Warm(int key, GameObject[] prefabs, int count)
        {
            var pool = new Queue<Interactable>(count); pools.Add(key, pool);
            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(prefabs[i % prefabs.Length], transform).GetComponent<Interactable>();
                if (instance is StationaryCar car) car.Initialize(catalog);
                instance.gameObject.SetActive(false); pool.Enqueue(instance); poolKeys.Add(instance, key);
            }
        }
        Interactable Spawn(int key, Vector3 position)
        {
            if (pools[key].Count == 0) return null;
            var item = pools[key].Dequeue(); item.Activate(game, position); active.Add(item);
            if (item is StationaryCar car) traffic.Add(car);
            return item;
        }
        void Release(int index)
        {
            var item = active[index];
            if (item is StationaryCar car) traffic.Remove(car);
            item.gameObject.SetActive(false); pools[poolKeys[item]].Enqueue(item); active.RemoveAt(index);
        }
        public void ResetWorld()
        {
            for (int i = active.Count - 1; i >= 0; i--) Release(i);
            furthestGenerated = int.MinValue; windowCenter = int.MinValue; trafficTimer = 3;
            foreach (var chunk in chunks) chunk.gameObject.SetActive(false);
            // Initial traffic is already present before ignition; only later spawns require invisibility.
            populatingInitial = true; MaintainWindow(game.Driver.transform.position.z); populatingInitial = false;
        }
        void MaintainWindow(float z)
        {
            int center = Mathf.FloorToInt(z / RoadChunk.Length);
            int direction = game.Driver.CameraRearView ? -1 : 1;
            if (center == windowCenter && direction == windowDirection) return;
            windowCenter = center; windowDirection = direction;
            minSection = center - (direction > 0 ? 5 : 11); maxSection = minSection + ChunkCount - 1;
            for (int i = active.Count - 1; i >= 0; i--)
                if (OutsideWindow(active[i].transform.position.z) && !TrafficVisible(active[i].transform.position)) Release(i);
            for (int index = minSection; index <= maxSection; index++)
            {
                bool found = false;
                foreach (var chunk in chunks) if (chunk.gameObject.activeSelf && chunk.Index == index) { found = true; break; }
                if (found) continue;
                foreach (var chunk in chunks)
                {
                    if (!chunk.gameObject.activeSelf || chunk.Index < minSection || chunk.Index > maxSection)
                    {
                        chunk.gameObject.SetActive(true); chunk.Place(index);
                        bool fresh = index > furthestGenerated;
                        Populate(index, fresh); furthestGenerated = Mathf.Max(furthestGenerated, index); break;
                    }
                }
            }
        }
        bool OutsideWindow(float z) => z < minSection * 60 || z > (maxSection + 1) * 60;
        void Populate(int index, bool fresh)
        {
            var random = new System.Random(unchecked(index * 17471 + 42013));
            float z = index * 60 + 18 + (float)random.NextDouble() * 30;
            TrafficFlow flow = TrafficRules.FlowAt(z);
            int safeLane = ((index % 3) + 3) % 3;
            if (flow != TrafficFlow.Quiet)
            {
                // Seed enough traffic before ignition; distant cars must not consume the local encounter budget.
                int cars = flow == TrafficFlow.Busy ? 3 : 2;
                for (int slot = 0; slot < cars && traffic.Count < 42; slot++)
                {
                    int lane = (safeLane + 1 + slot % 2) % 3;
                    float carZ = index * 60 + 14 + slot * 20 + (float)random.NextDouble() * 7;
                    bool stopped = slot == 0 && index > 3 && random.NextDouble() < .035;
                    // Preserve nearby opening encounters while the player is still building speed.
                    // A separate hash keeps the original seeded traffic layout/speeds unchanged.
                    bool fastDriver = (!populatingInitial || carZ - game.Driver.transform.position.z > 300)
                        && unchecked((uint)(index * 73856093 + slot * 19349663)) % 100 < 22;
                    SpawnTraffic(new Vector3((lane - 1) * CarDriver.LaneWidth, .08f, carZ), 18 + (float)random.NextDouble() * 8, stopped, populatingInitial, fastDriver);
                }
            }
            if (fresh && index >= 0 && index % 5 == 2)
                Spawn(2, new Vector3((random.Next(3) - 1) * CarDriver.LaneWidth, .6f, index * 60 + 10));
        }

        bool OccupiesLane(StationaryCar car, int lane) => car is MovingCar moving ? moving.OccupiesLane(lane) : Mathf.Abs(car.transform.position.x - (lane - 1) * CarDriver.LaneWidth) < 2.05f;
        int LaneMask(StationaryCar car)
        {
            int mask = 0;
            for (int lane = 0; lane < 3; lane++) if (OccupiesLane(car, lane)) mask |= 1 << lane;
            return mask;
        }
        void CaptureTraffic()
        {
            sampleCount = traffic.Count;
            for (int i = 0; i < sampleCount; i++)
            {
                var car = traffic[i];
                trafficSamples[i] = new TrafficSample { Car = car, Z = car.transform.position.z, Speed = car.Speed, Lanes = LaneMask(car), Id = i };
            }
        }
        bool PlayerOccupiesLane(int lane) => game.Driver.Lane == lane || Mathf.Abs(game.Driver.transform.position.x - (lane - 1) * CarDriver.LaneWidth) < 2.05f;
        public bool TrafficVisible(Vector3 position)
        {
            if (trafficCamera == null) return true;
            Vector3 view = trafficCamera.WorldToViewportPoint(position + Vector3.up);
            bool inView = view.z > -6 && view.z < trafficCamera.farClipPlane + 8 && view.x > -.18f && view.x < 1.18f && view.y > -.25f && view.y < 1.25f;
            return inView && !TrafficRules.HiddenByFog(Vector3.Distance(trafficCamera.transform.position, position), RenderSettings.fogDensity);
        }
        bool HasEscapeLane(float z, float speed, int proposedLane, StationaryCar ignored)
        {
            int proposed = (1 << proposedLane) | (ignored == null ? 0 : LaneMask(ignored));
            Vector3 player = game.Driver.transform.position;
            // A requested lane can be a full lane away from the car during the turn-in.
            int playerLane = Mathf.RoundToInt(player.x / CarDriver.LaneWidth) + 1;
            for (int step = 0; step <= 8; step++)
            {
                float time = step * (TrafficRules.FormationLookahead / 8);
                float rowZ = z + speed * time;
                float playerGap = rowZ - (player.z + game.Driver.Speed * time);
                float closing = playerGap >= 0 ? game.Driver.Speed - speed : speed - game.Driver.Speed;
                bool nearby = Mathf.Abs(playerGap) < Mathf.Max(45, 20 + Mathf.Max(0, closing) * 1.5f);
                // Distant overtakes can finish before the player arrives; spawning must always leave a gap.
                if (ignored != null && !nearby) continue;
                int occupied = proposed;
                foreach (var car in traffic)
                {
                    if (car == ignored) continue;
                    float spacing = TrafficRules.RowSpacing(game.Driver.Speed, Mathf.Min(speed, car.Speed));
                    if (Mathf.Abs(car.transform.position.z + car.Speed * time - rowZ) > spacing) continue;
                    occupied |= LaneMask(car);
                }
                if (occupied == 7) return false;
                if (nearby)
                {
                    bool reachable = false;
                    for (int lane = Mathf.Max(0, playerLane - 1); lane <= Mathf.Min(2, playerLane + 1); lane++)
                        if ((occupied & (1 << lane)) == 0) reachable = true;
                    if (!reachable) return false;
                }
            }
            return true;
        }
        public bool CanSpawnTraffic(Vector3 position, float speed)
        {
            return CanSpawnTraffic(position, speed, false);
        }
        bool CanSpawnTraffic(Vector3 position, float speed, bool initial)
        {
            int lane = Mathf.RoundToInt(position.x / CarDriver.LaneWidth) + 1;
            if (lane < 0 || lane > 2 || Mathf.Abs(position.x - (lane - 1) * CarDriver.LaneWidth) > .1f) return false;
            if (OutsideWindow(position.z) || Flow == TrafficFlow.Quiet || TrafficRules.FlowAt(position.z) == TrafficFlow.Quiet) return false;
            if (!initial && TrafficVisible(position)) return false;
            float gap = position.z - game.Driver.transform.position.z;
            if (!TrafficRules.SafePlayerSpawn(gap, speed, game.Driver.Speed)) return false;
            foreach (var other in traffic)
                if (OccupiesLane(other, lane) && !TrafficRules.SafeLaneGap(other.transform.position.z - position.z, speed, other.Speed, 1.5f * WeatherHeadway)) return false;
            return HasEscapeLane(position.z, speed, lane, null);
        }
        StationaryCar SpawnTraffic(Vector3 position, float speed, bool stopped, bool initial, bool fastDriver = false)
        {
            float actualSpeed = stopped ? 0 : TrafficRules.PaceSpeed(speed, fastDriver, TrafficRules.FastPaceAt(position.z)) * TrafficWeatherFactor;
            if (!CanSpawnTraffic(position, actualSpeed, initial)) return null;
            var car = Spawn(stopped ? 0 : 1, position) as StationaryCar;
            if (car is MovingCar moving) moving.SetCruisingSpeed(speed, fastDriver);
            return car;
        }
        public StationaryCar TrySpawnTraffic(Vector3 position, float speed, bool stopped = false, bool fastDriver = false) => SpawnTraffic(position, speed, stopped, false, fastDriver);

        public bool CanMerge(Interactable item, float targetX)
        {
            if (!(item is MovingCar car)) return false;
            int lane = Mathf.RoundToInt(targetX / CarDriver.LaneWidth) + 1;
            if (lane < 0 || lane > 2 || Mathf.Abs(targetX - (lane - 1) * CarDriver.LaneWidth) > .1f) return false;
            float z = car.transform.position.z;
            float headway = car.Headway * WeatherHeadway;
            if (PlayerOccupiesLane(lane) && !TrafficRules.SafeLaneGap(game.Driver.transform.position.z - z, car.Speed, game.Driver.Speed, headway)) return false;
            foreach (var other in traffic)
            {
                if (other == car || !OccupiesLane(other, lane)) continue;
                float gap = other.transform.position.z - z;
                // A signaled maneuver owns its immediate gap even after its driver brakes.
                if (other is MovingCar reserved && reserved.IsMerging && Mathf.Abs(gap) < 24) return false;
                if (!TrafficRules.SafeLaneGap(gap, car.Speed, other.Speed, headway, 1.8f)) return false;
            }
            return HasEscapeLane(z, car.Speed, lane, car);
        }
        public bool TryChooseTrafficLane(MovingCar car, float desired, float currentSafe, out int lane)
        {
            lane = car.Lane;
            float best = currentSafe + (car.Style == TrafficDriverStyle.Assertive ? 1.2f : car.Style == TrafficDriverStyle.Balanced ? 1.8f : 2.5f) * TrafficWeatherFactor;
            for (int candidate = Mathf.Max(0, car.Lane - 1); candidate <= Mathf.Min(2, car.Lane + 1); candidate++)
            {
                if (candidate == car.Lane || !CanMerge(car, (candidate - 1) * CarDriver.LaneWidth)) continue;
                float available = TrafficSpeedInLane(car, desired, candidate);
                bool keepRight = car.Style == TrafficDriverStyle.Cautious && candidate > car.Lane && currentSafe >= desired - 1 && available >= desired - 1;
                if (available > best || keepRight)
                {
                    lane = candidate; best = available;
                }
            }
            return lane != car.Lane;
        }
        public float TrafficSpeed(StationaryCar car, float cruising)
        {
            float result = cruising;
            for (int lane = 0; lane < 3; lane++) if (OccupiesLane(car, lane)) result = Mathf.Min(result, TrafficSpeedInLane(car, cruising, lane));
            return FormationSpeed(car, result);
        }
        float FormationSpeed(StationaryCar car, float desired)
        {
            // One bounded snapshot per world tick avoids native Transform calls inside the forecast loops.
            // Direct queries outside Tick refresh it, including scene/test authoring moves.
            if (!tickingTraffic) CaptureTraffic();
            TrafficSample self = default;
            bool found = false;
            for (int i = 0; i < sampleCount; i++) if (trafficSamples[i].Car == car) { self = trafficSamples[i]; found = true; break; }
            if (!found) return desired;
            float result = desired;
            float playerSpeed = game.Driver.Speed;
            for (int a = 0; a < sampleCount; a++)
            {
                var first = trafficSamples[a];
                if (!CanFormRowAhead(self, first)) continue;
                for (int b = a; b < sampleCount; b++)
                {
                    var second = trafficSamples[b];
                    if ((self.Lanes | first.Lanes | second.Lanes) != 7 || !CanFormRowAhead(self, second)) continue;
                    float rowSpeed = Mathf.Min(first.Speed, second.Speed);
                    float spacing = TrafficRules.RowSpacing(playerSpeed, rowSpeed);
                    float startGap = first.Z - second.Z;
                    float endGap = first.Z + first.Speed * TrafficRules.FormationLookahead - (second.Z + second.Speed * TrafficRules.FormationLookahead);
                    // The forecast uses constant speeds. A pair outside the spacing on the same
                    // side at both ends cannot form a row. Keep a margin for float rounding.
                    float roundingMargin = Mathf.Max(.25f, Mathf.Max(Mathf.Abs(first.Z), Mathf.Abs(second.Z)) * .000001f);
                    float limit = spacing + roundingMargin;
                    if ((startGap > limit && endGap > limit) || (startGap < -limit && endGap < -limit)) continue;
                    for (int step = 0; step <= 8; step++)
                    {
                        float time = step * (TrafficRules.FormationLookahead / 8);
                        float firstZ = first.Z + first.Speed * time, secondZ = second.Z + second.Speed * time;
                        if (Mathf.Abs(firstZ - secondZ) > spacing) continue;
                        float selfZ = self.Z + self.Speed * time;
                        // A pass that clears the row before it forms does not need a pace reduction.
                        if (selfZ > firstZ + .25f || selfZ > secondZ + .25f) continue;
                        float virtualGap = Mathf.Min(firstZ, secondZ) - rowSpeed * time - self.Z;
                        result = Mathf.Min(result, TrafficRules.StaggerSpeed(result, rowSpeed, virtualGap, spacing));
                    }
                }
            }
            return result;
        }
        static bool CanFormRowAhead(TrafficSample self, TrafficSample other)
        {
            // Only the current trailing car yields. List order breaks ties.
            return other.Id != self.Id && self.Z <= other.Z + .25f
                && (self.Id > other.Id || Mathf.Abs(self.Z - other.Z) > .25f);
        }
        float TrafficSpeedInLane(StationaryCar car, float cruising, int lane)
        {
            float result = cruising;
            float headway = (car is MovingCar moving ? moving.Headway : 1.5f) * WeatherHeadway;
            float carZ = car.transform.position.z;
            float carSpeed = car.Speed;
            float playerGap = game.Driver.transform.position.z - carZ;
            if (playerGap > 0 && PlayerOccupiesLane(lane))
                result = Mathf.Min(result, TrafficRules.FollowingSpeed(cruising, carSpeed, game.Driver.Speed, playerGap, headway));
            foreach (var other in traffic)
            {
                if (other == car) continue;
                float gap = other.transform.position.z - carZ;
                if (gap > 0 && OccupiesLane(other, lane))
                    result = Mathf.Min(result, TrafficRules.FollowingSpeed(cruising, carSpeed, other.Speed, gap, headway));
            }
            return result;
        }
        public void SetTrafficAudioPaused(bool paused)
        {
            foreach (var car in traffic) car.SetAudioPaused(paused);
        }
        void StreamTraffic(float dt, Vector3 player)
        {
            trafficTimer -= dt;
            if (trafficTimer > 0) return;
            float interval = Flow == TrafficFlow.Sparse ? 2.5f : Flow == TrafficFlow.Busy ? .8f : 1.4f;
            trafficTimer = interval * Random.Range(.8f, 1.25f) * Mathf.Lerp(1, .75f, game.Run.Difficulty) * Mathf.Lerp(1, .85f, SpeedPressure);
            if (Flow == TrafficFlow.Quiet || NearbyTrafficCount >= TrafficBudget || traffic.Count >= 42) return;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int lane = Random.Range(0, 3);
                bool behind = Random.value < .25f;
                bool fastDriver = Random.value < .22f;
                float speed = Random.Range(16f, 28f) + game.Run.Difficulty * 5;
                if (behind && !game.Driver.RearView) speed = Mathf.Max(speed, Mathf.Min(38, game.Driver.Speed + Random.Range(3f, 7f)));
                // Size the warning corridor for both ends of the upcoming pace change and the actual wet-road speed.
                float upperSpeed = TrafficRules.PaceSpeed(speed, fastDriver, 1) * TrafficWeatherFactor;
                float lowerSpeed = TrafficRules.PaceSpeed(speed, fastDriver, 0) * TrafficWeatherFactor;
                float rearGap = Mathf.Max(Random.Range(115, 175), 20 + Mathf.Max(0, upperSpeed - game.Driver.Speed) * 4.2f);
                float hiddenDistance = Mathf.Min(trafficCamera.farClipPlane + 32, 40 + 3.1f / Mathf.Max(.0001f, RenderSettings.fogDensity * Mathf.Exp(-.05f)));
                float direction = game.Driver.RearView ? -1 : 1;
                float closing = direction > 0 ? game.Driver.Speed - lowerSpeed : upperSpeed - game.Driver.Speed;
                float warningDistance = Mathf.Max(75, 20 + Mathf.Max(0, closing) * 4.2f);
                float z = behind ? player.z - rearGap : player.z + direction * (Mathf.Max(hiddenDistance, warningDistance) + Random.Range(5, 30));
                z = Mathf.Clamp(z, minSection * 60 + 10, (maxSection + 1) * 60 - 10);
                if (SpawnTraffic(new Vector3((lane - 1) * CarDriver.LaneWidth, .08f, z), speed, false, false, fastDriver) != null) return;
            }
            // An unsafe attempt is skipped; never force a car into the camera or an occupied gap.
        }
        public void Tick(float dt, Vector3? previousPlayerPosition = null)
        {
            Vector3 player = game.Driver.transform.position;
            MaintainWindow(player.z);
            if (dt > 0) StreamTraffic(dt, player);
            CaptureTraffic(); tickingTraffic = true;
            try
            {
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    var item = active[i];
                    if (!item.gameObject.activeSelf) { Release(i); continue; }
                    Vector3 previousItemPosition = item.transform.position;
                    item.Tick(dt);
                    Vector3 p = item.transform.position;
                    float halfLength = item is StationaryCar ? 4.1f : 2.5f;
                    Vector3 before = previousItemPosition - (previousPlayerPosition ?? player);
                    Vector3 after = p - player;
                    if (VehicleDynamics.SweptContact(new Vector2(before.x, before.z), new Vector2(after.x, after.z), item is StationaryCar ? 1.65f : 1.3f, halfLength)) item.Contact();
                    // Quiet sections drain traffic only beyond the view; visible cars never disappear to meet a budget.
                    bool drain = item is StationaryCar && Flow == TrafficFlow.Quiet && Mathf.Abs(p.z - player.z) > 75;
                    float travelGap = (p.z - player.z) * (game.Driver.RearView ? -1 : 1);
                    bool passed = item is StationaryCar && travelGap < -180;
                    if ((OutsideWindow(p.z) || drain || passed) && !TrafficVisible(p)) Release(i);
                    if (game.State != GameState.Playing) break;
                }
            }
            finally { tickingTraffic = false; }
        }
    }
}
